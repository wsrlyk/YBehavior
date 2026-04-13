import { memo, useMemo, useState, useEffect } from 'react';
import { Handle, Position, type NodeProps, type Node } from '@xyflow/react';
import type { TreeNode, Pin } from '../types';
import type { NodeDefinition } from '../types/nodeDefinition';
import { useTooltipStore } from '../stores/tooltipStore';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { NodeState, BreakpointType } from '../types/debug';
import { useDebugStore } from '../stores/debugStore';
import { useEditorStore } from '../stores/editorStore';
import { useShallow } from 'zustand/react/shallow';
import { getTheme } from '../theme/theme';

const theme = getTheme();

const NODE_COLORS = theme.node;
const PIN_COLORS = theme.pin;

import { stripExtension } from '../utils/fileUtils';
import { TRANSIENT_HIGHLIGHT_DURATION } from '../config/constants';

// Debug state colors for node visualization
const DEBUG_STATE_COLORS: Record<NodeState, { border: string; glow: string } | null> = {
  [NodeState.Invalid]: null,
  [NodeState.Success]: theme.debug.success,
  [NodeState.Failure]: theme.debug.failure,
  [NodeState.Break]: theme.debug.break,
  [NodeState.Running]: theme.debug.running,
};

type CustomNodeData = {
  label: string;
  treeNode: TreeNode;
  nodeDefinition?: NodeDefinition;
  isEffectivelyDisabled?: boolean;
};

export type CustomNodeType = Node<CustomNodeData, 'custom'>;

function getVectorIndexDisplay(pin: Pin): string {
  if (!pin.vectorIndex) return '';
  if (pin.vectorIndex.type === 'const') return `[${pin.vectorIndex.value}]`;
  return `[${pin.vectorIndex.variableName}]`;
}

function getPinNoteValue(pin: Pin): string {
  if (pin.enableType === 'disable') return '';

  if (pin.binding.type === 'const') {
    return `${pin.binding.value || ''}${getVectorIndexDisplay(pin)}`;
  }

  const base = pin.binding.variableName || pin.name;
  const localSuffix = pin.binding.variableName && pin.binding.isLocal ? "'" : '';
  return `${base}${localSuffix}${getVectorIndexDisplay(pin)}`;
}

function PinRow({ pin, isInput, dragging }: { pin: Pin; isInput: boolean; dragging?: boolean }) {
  const setTooltip = useTooltipStore((state) => state.setTooltip);
  const pinColor = PIN_COLORS[pin.valueType] || PIN_COLORS.default || theme.ui.border;
  const binding = pin.binding;

  const getVectorIndexDisplay = () => {
    if (!pin.vectorIndex) return '';
    if (pin.vectorIndex.type === 'const') return `[${pin.vectorIndex.value}]`;
    if (pin.vectorIndex.type === 'pointer') return `[${pin.vectorIndex.variableName}]`;
    return '';
  };

  const isDataConnection = binding.type === 'pointer' && !binding.variableName;
  const variableName = (binding.type === 'pointer' && binding.variableName) ? binding.variableName : null;

  return (
    <div
      className={`relative flex items-center gap-1 text-xs py-0.5 ${isInput ? 'pl-2.5' : 'pr-2.5 flex-row-reverse'}`}
      onMouseLeave={() => setTooltip(null)}
    >
      <Handle
        type={isInput ? 'target' : 'source'}
        position={isInput ? Position.Left : Position.Right}
        id={`pin-${isInput ? 'in' : 'out'}-${pin.name}`}
        isConnectable={isDataConnection}
        className={`!w-2 !h-2 !rounded-full !border-0 hover:[--handle-scale:1.5] hover:!opacity-100 !cursor-crosshair !transition-[transform,background-color,opacity] duration-200 !origin-center z-20 ${!isDataConnection ? 'opacity-30' : ''}`}
        style={{
          backgroundColor: pinColor,
          [isInput ? 'left' : 'right']: '-4px',
          top: '50%',
          transform: 'translateY(-50%) scale(var(--handle-scale, 1))'
        }}
      />
      <span
        className="truncate max-w-32 cursor-help"
        style={{ color: theme.text.pinName }}
        onMouseEnter={() => !dragging && setTooltip(pin.desc || null)}
      >{pin.name}</span>
      {binding.type === 'const' && (
        <span
          className={`text-[10px] max-w-40 cursor-help ${['Tree', 'Reference', 'treeNode.Reference'].includes(pin.name)
            ? 'filename-ellipsis'  // 文件路径需要 RTL 末尾省略
            : 'truncate'           // 普通值（enum/数字等）不能用 RTL
            }`}
          style={{ color: theme.text.constant }}
          onMouseEnter={() => !dragging && setTooltip(binding.value || '-')}
        >
          {['Tree', 'Reference', 'treeNode.Reference'].includes(pin.name) ? stripExtension(binding.value || '-') : (binding.value || '-')}
        </span>
      )}
      {variableName && (
        <span
          className="text-[10px] truncate max-w-40 cursor-help"
          style={{ color: theme.text.variable }}
          onMouseEnter={() => !dragging && setTooltip(variableName)}
        >{variableName}{binding.type === 'pointer' && binding.isLocal ? "'" : ""}{getVectorIndexDisplay()}</span>
      )}
    </div>
  );
}

function CustomNode({ data, selected, dragging }: NodeProps<CustomNodeType>) {
  const setTooltip = useTooltipStore((state) => state.setTooltip);
  const getDefinition = useNodeDefinitionStore((state) => state.getDefinition);
  const { label, treeNode } = data;

  // Debug state
  // Debug state
  const activeFilePath = useEditorStore((s) => s.activeFilePath);

  const fileName = useMemo(() => {
    if (!activeFilePath) return '';
    return activeFilePath.split(/[\\/]/).pop()?.replace(/\.tree$/, '').replace(/\.fsm$/, '') || '';
  }, [activeFilePath]);

  const { debugState, isPaused, flashTick, bpType } = useDebugStore(
    useShallow(s => {
      const state = s.isConnected && treeNode.uid !== undefined
        ? s.getNodeRunState(fileName, treeNode.uid) // RunState uses fileName (basename) from Runtime
        : undefined;

      const debugState = state ? state.self : NodeState.Invalid;

      // Breakpoints use activeFilePath (Full Path) as key, matching useGlobalKeyboard.ts
      const bp = activeFilePath && treeNode.uid !== undefined
        ? s.getBreakpoint(activeFilePath, treeNode.uid)
        : BreakpointType.None;

      return {
        debugState,
        isPaused: s.isPaused,
        flashTick: (state && state.self !== NodeState.Invalid) ? s.keyframe : 0,
        bpType: bp
      };
    })
  );
  const debugColors = DEBUG_STATE_COLORS[debugState];

  // Transient highlight: Break = always on, others fade out after TRANSIENT_HIGHLIGHT_DURATION
  const [isTransientVisible, setIsTransientVisible] = useState(false);
  useEffect(() => {
    if (isPaused) {
      setIsTransientVisible(debugState !== NodeState.Invalid);
      return;
    }
    if (debugState === NodeState.Break) {
      setIsTransientVisible(true);
    } else if (debugState !== NodeState.Invalid) {
      setIsTransientVisible(true);
      const timer = setTimeout(() => setIsTransientVisible(false), TRANSIENT_HIGHLIGHT_DURATION);
      return () => clearTimeout(timer);
    } else {
      setIsTransientVisible(false);
    }
  }, [debugState, isPaused, flashTick]);

  const showDebugOverlay = debugColors !== null && (isPaused || isTransientVisible);

  const nodeDefinition = useMemo(() => getDefinition(treeNode.type), [treeNode.type, getDefinition]);

  const bgColor = NODE_COLORS[treeNode.category] || NODE_COLORS.default || theme.ui.border;
  const selectedColor = theme.text.variable;
  const selectedGlow = `${selectedColor}55`;
  const hasChildren = (nodeDefinition?.childConnectors?.length || 0) > 0;
  const isEffectivelyDisabled = data.isEffectivelyDisabled;

  const returnType = treeNode.returnType || 'Default';
  const hasReturnType = returnType !== 'Default';
  // Use theme colors
  const returnTypeColor = theme.returnType[returnType] || theme.returnType['Invert']; // Default fallback
  const nodeNote = useMemo(() => {
    const note = nodeDefinition?.note?.trim();
    if (!note) return '';

    return note.replace(/\{(\d+)\}/g, (_match, indexText) => {
      const index = Number(indexText);
      if (Number.isNaN(index) || index < 0 || index >= treeNode.pins.length) return '';
      return getPinNoteValue(treeNode.pins[index]);
    });
  }, [nodeDefinition?.note, treeNode.pins]);

  return (
    <div className="relative">
      {hasReturnType && (
        <div
          className="absolute -top-4 left-0 px-2 py-0.5 rounded-t-sm text-[8px] font-bold uppercase tracking-tighter"
          style={{ backgroundColor: returnTypeColor, color: theme.ui.textMain }}
        >
          {returnType}
        </div>
      )}

      {/* Breakpoint/Logpoint Indicator */}
      {bpType !== BreakpointType.None && (
        <div
          className="absolute top-1 right-1 w-4 h-4 rounded-full z-[60] shadow-sm border-2"
          style={{
            borderColor: theme.ui.panelBg,
            backgroundColor: bpType === BreakpointType.Breakpoint ? theme.debug.break.border : theme.debug.running.border,
          }}
        />
      )}

      {treeNode.comment && (
        <div
          className={`absolute p-1 rounded text-[10px] break-words whitespace-pre-wrap w-full ${hasChildren ? 'left-[calc(100%+8px)] top-0' : 'top-[calc(100%+8px)] left-0'}`}
          style={{
            backgroundColor: theme.comment.bg,
            border: `1px solid ${theme.comment.border}`,
            color: theme.comment.text,
          }}
        >
          {treeNode.comment}
        </div>
      )}

      <div className="relative">
        <div
          className={`rounded shadow-lg min-w-32 cursor-grab resize-none hover:brightness-110 transition-[filter,border-color,box-shadow] duration-200 ${isEffectivelyDisabled ? 'grayscale opacity-60' : ''}`}
          style={{
            backgroundColor: theme.ui.panelBg,
            border: `1px solid ${selected ? selectedColor : theme.ui.border}`,
            boxShadow: selected ? `0 0 0 2px ${selectedGlow}` : undefined,
          }}
        >
          {/* Debug Overlay */}
          {showDebugOverlay && (
            <div
              className="absolute inset-0 rounded pointer-events-none z-50"
              style={{
                boxShadow: `inset 0 0 0 4px ${debugColors!.border}, ${debugColors!.glow}`
              }}
            />
          )}

          <div
            className="px-2 py-1 text-xs font-medium rounded-t flex items-center gap-1"
            style={{ backgroundColor: bgColor, color: theme.ui.textMain }}
            onMouseEnter={() => !dragging && nodeDefinition?.desc && setTooltip(nodeDefinition.desc)}
            onMouseLeave={() => setTooltip(null)}
          >
            <span className="text-[10px]" style={{ color: theme.ui.textMain }}>{treeNode.uid ?? '?'}</span>
            <span className="flex-1 truncate">{treeNode.nickname || label}</span>
            {treeNode.isFolded && (
              <span className="text-[10px] px-1 rounded" style={{ backgroundColor: theme.ui.border, color: theme.ui.textMain }}>
                Folded
              </span>
            )}
            {treeNode.disabled && <span className="text-[10px]" style={{ color: theme.debug.break.border }}>Disabled</span>}
          </div>

          {nodeNote && (
            <div
              className="px-2 py-1 text-[10px] leading-snug border-b break-words whitespace-pre-wrap"
              style={{ borderColor: theme.ui.border, color: theme.ui.textMain }}
            >
              {nodeNote}
            </div>
          )}

          {(treeNode.pins.filter(p => (p.isInput || !p.isInput) && p.enableType !== 'disable').length > 0) && (
            <div className="flex justify-between px-2 py-1 gap-4">
              <div className="flex flex-col">
                {treeNode.pins.filter(p => p.isInput && p.enableType !== 'disable').map((pin, i) => <PinRow key={`in-${i}`} pin={pin} isInput={true} dragging={dragging} />)}
              </div>
              <div className="flex flex-col items-end">
                {treeNode.pins.filter(p => !p.isInput && p.enableType !== 'disable').map((pin, i) => <PinRow key={`out-${i}`} pin={pin} isInput={false} dragging={dragging} />)}
              </div>
            </div>
          )}

          {(nodeDefinition?.childConnectors?.length || 0) > 1 && (
            <div className="flex justify-around px-1 pb-1 text-[9px]" style={{ color: theme.ui.textDim }}>
              {nodeDefinition?.childConnectors?.map((conn, i) => <span key={i}>{conn.label || conn.name}</span>)}
            </div>
          )}
        </div>

        {(nodeDefinition?.hasParent ?? true) && (
          <Handle
            type="target"
            position={Position.Top}
            id="tree-target"
            className="!w-3 !h-2 !rounded-sm !border-0 hover:[--handle-scale:1.5] hover:!opacity-100 !cursor-crosshair !transition-[transform,background-color,opacity] duration-200 !origin-center z-20"
            style={{
              backgroundColor: theme.handle.bg,
              top: 0,
              left: '50%',
              transform: 'translate(-50%, -50%) scale(var(--handle-scale, 1))'
            }}
          />
        )}

        {hasChildren && nodeDefinition?.childConnectors?.length === 1 && (
          <Handle
            type="source"
            position={Position.Bottom}
            id={nodeDefinition.childConnectors[0].name}
            className="!w-3 !h-2 !rounded-sm !border-0 hover:[--handle-scale:1.5] hover:!opacity-100 !cursor-crosshair !transition-[transform,background-color,opacity] duration-200 !origin-center z-20"
            style={{
              backgroundColor: theme.handle.bg,
              bottom: 0,
              left: '50%',
              transform: 'translate(-50%, 50%) scale(var(--handle-scale, 1))'
            }}
          />
        )}

        {hasChildren && (nodeDefinition?.childConnectors?.length || 0) > 1 && nodeDefinition?.childConnectors?.map((conn, i) => (
          <Handle
            key={conn.name}
            type="source"
            position={Position.Bottom}
            id={conn.name}
            className="!w-3 !h-2 !rounded-sm !border-0 hover:[--handle-scale:1.5] hover:!opacity-100 !cursor-crosshair !transition-[transform,background-color,opacity] duration-200 !origin-center z-20"
            style={{
              backgroundColor: theme.handle.bg,
              bottom: 0,
              left: `${((i + 1) / ((nodeDefinition?.childConnectors?.length || 0) + 1)) * 100}%`,
              transform: 'translate(-50%, 50%) scale(var(--handle-scale, 1))'
            }}
          />
        ))}

        {treeNode.hasConditionConnector && (
          <div
            className="absolute top-0 -left-6 w-6 h-5 flex items-center justify-center z-[60] shadow-sm border"
            style={{
              backgroundColor: theme.handle.bg,
              borderColor: theme.ui.border,
              borderRight: 'none',
              borderRadius: '4px 0 0 4px',
            }}
          >
            <span className="text-[10px] font-black select-none" style={{ color: theme.ui.textMain }}>IF</span>
            <Handle
              type="source"
              position={Position.Bottom}
              id="condition"
              className="!w-2 !h-1 !rounded-none !border-0 !absolute !bottom-0 !left-1/2 !cursor-crosshair z-20"
              style={{
                backgroundColor: theme.handle.bg,
                transform: 'translate(-50%, 50%)'
              }}
            />
          </div>
        )}
      </div>
    </div>
  );
}

export default memo(CustomNode);
