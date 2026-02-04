import { memo } from 'react';
import { Handle, Position, type NodeProps, type Node } from '@xyflow/react';
import type { TreeNode, Pin } from '../types';
import type { NodeDefinition, ConnectorDefinition } from '../types/nodeDefinition';

const NODE_COLORS: Record<string, string> = {
  composite: '#5A8A5E',
  decorator: '#5A7A9A',
  action: '#B08050',
  condition: '#7A5A8A',
};

const PIN_COLORS: Record<string, string> = {
  int: '#6B9BD2',
  float: '#7BC96F',
  bool: '#CC6666',
  string: '#D4A5D9',
  vector3: '#D9C76B',
  entity: '#6BD9D9',
  ulong: '#9999CC',
  enum: '#CC9966',
};

type CustomNodeData = {
  label: string;
  treeNode: TreeNode;
  nodeDefinition?: NodeDefinition;
  isEffectivelyDisabled?: boolean;
};

export type CustomNodeType = Node<CustomNodeData, 'custom'>;

function PinRow({ pin, isInput }: { pin: Pin; isInput: boolean }) {
  const pinColor = PIN_COLORS[pin.valueType] || '#888';

  const getVectorIndexDisplay = () => {
    if (!pin.vectorIndex) return '';
    if (pin.vectorIndex.type === 'const') return `[${pin.vectorIndex.value}]`;
    if (pin.vectorIndex.type === 'pointer') return `[${pin.vectorIndex.variableName}]`;
    return '';
  };

  const isDataConnection = pin.binding.type === 'pointer' && !pin.binding.variableName;

  return (
    <div className={`relative flex items-center gap-1 text-xs py-0.5 ${isInput ? 'pl-2.5' : 'pr-2.5 flex-row-reverse'}`}>
      <Handle
        type={isInput ? 'target' : 'source'}
        position={isInput ? Position.Left : Position.Right}
        id={`pin-${isInput ? 'in' : 'out'}-${pin.name}`}
        isConnectable={isDataConnection}
        className={`!w-2 !h-2 !rounded-full !border-0 hover:!bg-white hover:[--handle-scale:1.5] !cursor-crosshair !transition-[transform,background-color,opacity] duration-200 !origin-center z-20 ${!isDataConnection ? 'opacity-30' : ''}`}
        style={{
          backgroundColor: pinColor,
          [isInput ? 'left' : 'right']: '-4px',
          top: '50%',
          transform: 'translateY(-50%) scale(var(--handle-scale, 1))'
        }}
      />
      <span className="text-gray-300 truncate max-w-20">{pin.name}</span>
      {pin.binding.type === 'const' && (
        <span className="text-gray-500 text-[10px] truncate max-w-12">{pin.binding.value || '-'}</span>
      )}
      {pin.binding.type === 'pointer' && pin.binding.variableName && (
        <span className="text-blue-400 text-[10px] truncate max-w-12">{pin.binding.variableName}{pin.binding.isLocal ? "'" : ""}{getVectorIndexDisplay()}</span>
      )}
    </div>
  );
}

function CustomNode({ data, selected }: NodeProps<CustomNodeType>) {
  const { label, treeNode, nodeDefinition } = data;
  const bgColor = NODE_COLORS[treeNode.category] || '#666';
  const hasChildren = (nodeDefinition?.childConnectors?.length || 0) > 0;
  const isEffectivelyDisabled = data.isEffectivelyDisabled;

  const returnType = treeNode.returnType || 'Default';
  const hasReturnType = returnType !== 'Default';
  const returnTypeColors: Record<string, string> = {
    Invert: 'bg-orange-500',
    Success: 'bg-green-600',
    Failure: 'bg-red-600',
  };

  return (
    <div className="relative">
      {hasReturnType && (
        <div className={`absolute -top-4 left-0 px-2 py-0.5 rounded-t-sm text-[8px] font-bold text-white uppercase tracking-tighter ${returnTypeColors[returnType]}`}>
          {returnType}
        </div>
      )}

      {treeNode.comment && (
        <div className={`absolute bg-yellow-900/40 border border-yellow-600/50 p-1 rounded text-[10px] text-yellow-100 max-w-[150px] break-words whitespace-pre-wrap ${hasChildren ? 'left-[calc(100%+8px)] top-0' : 'top-[calc(100%+8px)] left-0 min-w-full'}`}>
          {treeNode.comment}
        </div>
      )}

      <div className="relative">
        <div
          className={`rounded shadow-lg min-w-32 cursor-move hover:brightness-110 transition-[filter,border-color,box-shadow] duration-200 ${isEffectivelyDisabled ? 'grayscale opacity-60' : ''}`}
          style={{
            backgroundColor: '#1f2937',
            border: `2px solid ${selected ? '#fff' : bgColor}`,
            boxShadow: selected ? '0 0 0 2px rgba(255,255,255,0.3)' : undefined,
          }}
        >
          <div className="px-2 py-1 text-xs font-medium text-white rounded-t flex items-center gap-1" style={{ backgroundColor: bgColor }}>
            <span className="text-white text-[10px]">{treeNode.uid ?? '?'}</span>
            <span className="flex-1 truncate">{treeNode.nickname || label}</span>
            {treeNode.isFolded && <span className="text-[10px] bg-black/30 px-1 rounded">Folded</span>}
            {treeNode.disabled && <span className="text-red-300 text-[10px]">Disabled</span>}
          </div>

          {(treeNode.pins.filter(p => (p.isInput || !p.isInput) && p.enableType !== 'disable').length > 0) && (
            <div className="flex justify-between px-2 py-1 gap-4">
              <div className="flex flex-col">
                {treeNode.pins.filter(p => p.isInput && p.enableType !== 'disable').map((pin, i) => <PinRow key={`in-${i}`} pin={pin} isInput={true} />)}
              </div>
              <div className="flex flex-col items-end">
                {treeNode.pins.filter(p => !p.isInput && p.enableType !== 'disable').map((pin, i) => <PinRow key={`out-${i}`} pin={pin} isInput={false} />)}
              </div>
            </div>
          )}

          {(nodeDefinition?.childConnectors?.length || 0) > 1 && (
            <div className="flex justify-around px-1 pb-1 text-[9px] text-gray-400">
              {nodeDefinition?.childConnectors?.map((conn, i) => <span key={i}>{conn.label || conn.name}</span>)}
            </div>
          )}
        </div>

        {(nodeDefinition?.hasParent ?? true) && (
          <Handle
            type="target"
            position={Position.Top}
            id="tree-target"
            className="!bg-gray-400 !w-3 !h-2 !rounded-sm !border-0 hover:!bg-white hover:[--handle-scale:1.5] !cursor-crosshair !transition-[transform,background-color] duration-200 !origin-center z-20"
            style={{
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
            className="!bg-gray-400 !w-3 !h-2 !rounded-sm !border-0 hover:!bg-white hover:[--handle-scale:1.5] !cursor-crosshair !transition-[transform,background-color] duration-200 !origin-center z-20"
            style={{
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
            className="!bg-gray-400 !w-3 !h-2 !rounded-sm !border-0 hover:!bg-white hover:[--handle-scale:1.5] !cursor-crosshair !transition-[transform,background-color] duration-200 !origin-center z-20"
            style={{
              bottom: 0,
              left: `${((i + 1) / ((nodeDefinition?.childConnectors?.length || 0) + 1)) * 100}%`,
              transform: 'translate(-50%, 50%) scale(var(--handle-scale, 1))'
            }}
          />
        ))}

        {treeNode.hasConditionConnector && (
          <div
            className="absolute top-[24px] w-[16px] h-5 bg-purple-600 border border-purple-400 border-r-0 rounded-l flex flex-col items-center justify-center hover:bg-purple-500 hover:scale-110 transition-[transform,background-color,scale] duration-200 cursor-crosshair z-20 shadow-[-1px_0_1px_rgba(0,0,0,0.3)] origin-right"
            style={{ left: '-15px' }}
            title="Condition Connector"
          >
            <span className="text-[9px] text-white font-black select-none leading-none">IF</span>
            <Handle
              type="source"
              position={Position.Bottom}
              id="condition"
              className="!bg-purple-400 !w-3 !h-2 !rounded-none !border-0 !absolute !bottom-0 !left-1/2 !cursor-crosshair hover:!bg-white hover:[--handle-scale:1.5] !transition-[transform,background-color] duration-200 !origin-center z-20"
              style={{
                bottom: 0,
                left: '50%',
                transform: 'translate(-50%, 50%) scale(var(--handle-scale, 1))'
              }}
            />
          </div>
        )}
      </div>
    </div>
  );
}

export default memo(CustomNode);
