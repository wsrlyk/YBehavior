import { useState, useEffect, useRef } from 'react';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { useEditorStore } from '../stores/editorStore';
import { useTooltipStore } from '../stores/tooltipStore';
import type { NodeCategory } from '../types';
import { useDebugStore } from '../stores/debugStore';
import { BreakpointType } from '../types/debug';
import { getTheme } from '../theme/theme';

const theme = getTheme();

interface NodeContextMenuProps {
  isOpen: boolean;
  position: { x: number; y: number };
  onClose: () => void;
  onAddNode: (nodeClass: string, position: { x: number; y: number }) => void;
  nodeId?: string | null;
}

const CATEGORY_ORDER: NodeCategory[] = ['composite', 'decorator', 'action', 'condition'];

export function NodeContextMenu({ isOpen, position, onClose, onAddNode, nodeId }: NodeContextMenuProps) {
  const { getByCategory, isLoaded } = useNodeDefinitionStore();
  const currentTree = useEditorStore((state) => state.getCurrentTree());
  const toggleNodeFold = useEditorStore((state) => state.toggleNodeFold);
  const toggleNodeDisabled = useEditorStore((state) => state.toggleNodeDisabled);
  const toggleConditionConnector = useEditorStore((state) => state.toggleConditionConnector);
  const setTooltip = useTooltipStore((state) => state.setTooltip);

  const node = nodeId ? currentTree?.nodes.get(nodeId) : null;
  const [filter, setFilter] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  const allNodes = CATEGORY_ORDER.flatMap(cat =>
    getByCategory(cat)
      .filter(def => def.className !== 'Root') // Root 不在列表中显示
      .map(def => ({ ...def, category: cat }))
  );

  const filteredNodes = allNodes.filter(node => {
    const searchStr = filter.toLowerCase();

    // Check node name
    if (node.className.toLowerCase().includes(searchStr)) return true;

    // Check node description
    if (node.desc && node.desc.toLowerCase().includes(searchStr)) return true;

    // Check pins and pin descriptions
    if (node.pins && node.pins.some(pin =>
      pin.name.toLowerCase().includes(searchStr) ||
      (pin.desc && pin.desc.toLowerCase().includes(searchStr))
    )) return true;

    return false;
  });

  useEffect(() => {
    if (isOpen) {
      setFilter('');
      setSelectedIndex(0);
      setTimeout(() => inputRef.current?.focus(), 0);
    }
  }, [isOpen]);

  useEffect(() => {
    setSelectedIndex(0);
  }, [filter]);

  useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose();
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen, onClose]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setSelectedIndex(i => Math.min(i + 1, filteredNodes.length - 1));
        break;
      case 'ArrowUp':
        e.preventDefault();
        setSelectedIndex(i => Math.max(i - 1, 0));
        break;
      case 'Enter':
        e.preventDefault();
        if (filteredNodes[selectedIndex]) {
          onAddNode(filteredNodes[selectedIndex].className, position);
          onClose();
        }
        break;
      case 'Escape':
        onClose();
        break;
    }
  };

  const handleNodeClick = (nodeClass: string) => {
    onAddNode(nodeClass, position);
    onClose();
  };

  if (!isOpen || !isLoaded) return null;

  const CATEGORY_COLORS = theme.contextMenu.categoryDots;
  const sectionBg = theme.ui.background;
  const itemHoverBg = theme.ui.border;
  const itemText = theme.ui.textMain;
  const mutedText = theme.ui.textDim;
  const activeAccent = theme.text.variable;

  return (
    <div
      ref={menuRef}
      style={{ left: position.x, top: position.y, backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}
      className="absolute z-50 w-72 border rounded-lg shadow-xl overflow-hidden"
      onKeyDown={handleKeyDown}
    >
      {node && (
        <>
          <div className="p-1 border-b" style={{ borderColor: theme.ui.border, backgroundColor: sectionBg }}>
            <div className="px-3 py-1 text-[10px] font-bold uppercase tracking-wider" style={{ color: mutedText }}>Node Actions</div>

            <div
              className="px-3 py-2 text-sm cursor-pointer flex items-center justify-between group"
              style={{ color: itemText }}
              onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = itemHoverBg; }}
              onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
              onClick={() => { toggleConditionConnector(node.id); onClose(); }}
            >
              <span>Condition Connector</span>
              <span
                className="text-[10px] px-1.5 rounded"
                style={node.hasConditionConnector
                  ? { backgroundColor: theme.returnType.Invert, color: theme.ui.textMain }
                  : { backgroundColor: theme.ui.border, color: mutedText }}
              >
                {node.hasConditionConnector ? 'ON' : 'OFF'}
              </span>
            </div>

            <div
              className="px-3 py-2 text-sm cursor-pointer flex items-center justify-between group"
              style={{ color: itemText }}
              onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = itemHoverBg; }}
              onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
              onClick={() => { toggleNodeDisabled(node.id); onClose(); }}
            >
              <span>Node State</span>
              <span
                className="text-[10px] px-1.5 rounded"
                style={node.disabled
                  ? { backgroundColor: theme.debug.break.border, color: theme.ui.textMain }
                  : { backgroundColor: theme.ui.border, color: mutedText }}
              >
                {node.disabled ? 'DISABLED' : 'ENABLED'}
              </span>
            </div>

            <div
              className="px-3 py-2 text-sm cursor-pointer flex items-center justify-between group"
              style={{ color: itemText }}
              onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = itemHoverBg; }}
              onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
              onClick={() => { toggleNodeFold(node.id); onClose(); }}
            >
              <span>Folding</span>
              <span
                className="text-[10px] px-1.5 rounded"
                style={node.isFolded
                  ? { backgroundColor: activeAccent, color: theme.ui.textMain }
                  : { backgroundColor: theme.ui.border, color: mutedText }}
              >
                {node.isFolded ? 'FOLDED' : 'NORMAL'}
              </span>
            </div>
          </div>

          <div className="p-1 border-b" style={{ borderColor: theme.ui.border, backgroundColor: sectionBg }}>
            <div className="px-3 py-1 text-[10px] font-bold uppercase tracking-wider" style={{ color: mutedText }}>Debug</div>

            <div
              className="px-3 py-2 text-sm cursor-pointer flex items-center justify-between group"
              style={{ color: itemText }}
              onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = itemHoverBg; }}
              onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
              onClick={() => {
                const { activeFilePath } = useEditorStore.getState();
                if (activeFilePath && node && node.uid !== undefined) {
                  // Use basename for breakpoints to match runtime
                  const fileName = activeFilePath.split(/[\\/]/).pop()?.replace(/\.tree$/, '').replace(/\.fsm$/, '') || '';
                  useDebugStore.getState().toggleBreakpoint(fileName, node.uid);
                }
                onClose();
              }}
            >
              <span>Breakpoint</span>
              <div className="flex items-center gap-2">
                <span className="text-[10px]" style={{ color: mutedText }}>F9</span>
                {(() => {
                  const { activeFilePath } = useEditorStore.getState();
                  const bpType = activeFilePath && node && node.uid !== undefined ? useDebugStore.getState().getBreakpoint(activeFilePath, node.uid) : BreakpointType.None;
                  const isBp = bpType === BreakpointType.Breakpoint;
                  return (
                    <span
                      className="text-[10px] px-1.5 rounded"
                      style={isBp
                        ? { backgroundColor: theme.debug.break.border, color: theme.ui.textMain }
                        : { backgroundColor: theme.ui.border, color: mutedText }}
                    >
                      {isBp ? 'ON' : 'OFF'}
                    </span>
                  );
                })()}
              </div>
            </div>

            <div
              className="px-3 py-2 text-sm cursor-pointer flex items-center justify-between group"
              style={{ color: itemText }}
              onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = itemHoverBg; }}
              onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
              onClick={() => {
                const { activeFilePath } = useEditorStore.getState();
                if (activeFilePath && node && node.uid !== undefined) {
                  useDebugStore.getState().toggleLogpoint(activeFilePath, node.uid);
                }
                onClose();
              }}
            >
              <span>Logpoint</span>
              <div className="flex items-center gap-2">
                <span className="text-[10px]" style={{ color: mutedText }}>F8</span>
                {(() => {
                  const { activeFilePath } = useEditorStore.getState();
                  const bpType = activeFilePath && node && node.uid !== undefined ? useDebugStore.getState().getBreakpoint(activeFilePath, node.uid) : BreakpointType.None;
                  const isLp = bpType === BreakpointType.Logpoint;
                  return (
                    <span
                      className="text-[10px] px-1.5 rounded"
                      style={isLp
                        ? { backgroundColor: theme.debug.running.border, color: theme.ui.textMain }
                        : { backgroundColor: theme.ui.border, color: mutedText }}
                    >
                      {isLp ? 'ON' : 'OFF'}
                    </span>
                  );
                })()}
              </div>
            </div>
          </div>
        </>
      )}

      {/* 搜索框 (如果选了节点，就不显示添加节点内容) */}
      {!node && (
        <>
          <div className="p-2 border-b" style={{ borderColor: theme.ui.border }}>
            <div className="px-1 mb-1 text-[10px] font-bold uppercase tracking-wider" style={{ color: mutedText }}>Add Node</div>
            <input
              ref={inputRef}
              type="text"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              placeholder="Search nodes, pins, descriptions..."
              className="w-full px-3 py-2 border rounded text-sm focus:outline-none"
              style={{
                backgroundColor: theme.ui.inputBg,
                borderColor: theme.ui.border,
                color: theme.ui.textMain,
              }}
            />
          </div>

          {/* 节点列表 */}
          <div className="max-h-80 overflow-auto">
            {filteredNodes.length === 0 ? (
              <div className="px-3 py-4 text-sm text-center" style={{ color: mutedText }}>
                No nodes found
              </div>
            ) : (
              filteredNodes.map((node, index) => (
                <div
                  key={node.className}
                  onClick={() => handleNodeClick(node.className)}
                  onMouseEnter={() => node.desc && setTooltip(node.desc)}
                  onMouseLeave={() => setTooltip(null)}
                  className="px-3 py-2 text-sm cursor-pointer flex items-center gap-2"
                  style={{
                    backgroundColor: index === selectedIndex ? activeAccent : 'transparent',
                    color: itemText,
                  }}
                >
                  <span className="w-2 h-2 rounded-full" style={{ backgroundColor: CATEGORY_COLORS[node.category] || theme.ui.border }} />
                  <span className="flex-1">{node.className}</span>
                  <span className="text-xs capitalize" style={{ color: mutedText }}>{node.category}</span>
                </div>
              ))
            )}
          </div>

          {/* 底部提示 */}
          <div className="px-3 py-2 border-t text-xs" style={{ borderColor: theme.ui.border, color: theme.ui.textDim }}>
            ↑↓ Navigate · Enter Add · Esc Close
          </div>
        </>
      )}
    </div>
  );
}
