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

  return (
    <div
      ref={menuRef}
      style={{ left: position.x, top: position.y, backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}
      className="absolute z-50 w-72 border rounded-lg shadow-xl overflow-hidden"
      onKeyDown={handleKeyDown}
    >
      {node && (
        <>
          <div className="p-1 border-b border-[#404040] bg-[#171717]/50">
            <div className="px-3 py-1 text-[10px] font-bold text-[#737373] uppercase tracking-wider">Node Actions</div>

            <div
              className="px-3 py-2 text-sm text-[#d4d4d4] hover:bg-[#404040] cursor-pointer flex items-center justify-between group"
              onClick={() => { toggleConditionConnector(node.id); onClose(); }}
            >
              <span>Condition Connector</span>
              <span className={`text-[10px] px-1.5 rounded ${node.hasConditionConnector ? 'bg-purple-600 text-white' : 'bg-[#404040] text-[#a3a3a3] group-hover:bg-[#525252]'}`}>
                {node.hasConditionConnector ? 'ON' : 'OFF'}
              </span>
            </div>

            <div
              className="px-3 py-2 text-sm text-[#d4d4d4] hover:bg-[#404040] cursor-pointer flex items-center justify-between group"
              onClick={() => { toggleNodeDisabled(node.id); onClose(); }}
            >
              <span>Node State</span>
              <span className={`text-[10px] px-1.5 rounded ${node.disabled ? 'bg-red-600 text-white' : 'bg-[#404040] text-[#a3a3a3] group-hover:bg-[#525252]'}`}>
                {node.disabled ? 'DISABLED' : 'ENABLED'}
              </span>
            </div>

            <div
              className="px-3 py-2 text-sm text-[#d4d4d4] hover:bg-[#404040] cursor-pointer flex items-center justify-between group"
              onClick={() => { toggleNodeFold(node.id); onClose(); }}
            >
              <span>Folding</span>
              <span className={`text-[10px] px-1.5 rounded ${node.isFolded ? 'bg-blue-600 text-white' : 'bg-[#404040] text-[#a3a3a3] group-hover:bg-[#525252]'}`}>
                {node.isFolded ? 'FOLDED' : 'NORMAL'}
              </span>
            </div>
          </div>

          <div className="p-1 border-b border-[#404040] bg-[#171717]/50">
            <div className="px-3 py-1 text-[10px] font-bold text-[#737373] uppercase tracking-wider">Debug</div>

            <div
              className="px-3 py-2 text-sm text-[#d4d4d4] hover:bg-[#404040] cursor-pointer flex items-center justify-between group"
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
                <span className="text-[10px] text-[#737373]">F9</span>
                {(() => {
                  const { activeFilePath } = useEditorStore.getState();
                  const bpType = activeFilePath && node && node.uid !== undefined ? useDebugStore.getState().getBreakpoint(activeFilePath, node.uid) : BreakpointType.None;
                  const isBp = bpType === BreakpointType.Breakpoint;
                  return (
                    <span className={`text-[10px] px-1.5 rounded ${isBp ? 'bg-red-600 text-white' : 'bg-[#404040] text-[#a3a3a3] group-hover:bg-[#525252]'}`}>
                      {isBp ? 'ON' : 'OFF'}
                    </span>
                  );
                })()}
              </div>
            </div>

            <div
              className="px-3 py-2 text-sm text-[#d4d4d4] hover:bg-[#404040] cursor-pointer flex items-center justify-between group"
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
                <span className="text-[10px] text-[#737373]">F8</span>
                {(() => {
                  const { activeFilePath } = useEditorStore.getState();
                  const bpType = activeFilePath && node && node.uid !== undefined ? useDebugStore.getState().getBreakpoint(activeFilePath, node.uid) : BreakpointType.None;
                  const isLp = bpType === BreakpointType.Logpoint;
                  return (
                    <span className={`text-[10px] px-1.5 rounded ${isLp ? 'bg-purple-600 text-white' : 'bg-[#404040] text-[#a3a3a3] group-hover:bg-[#525252]'}`}>
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
          <div className="p-2 border-b border-[#404040]">
            <div className="px-1 mb-1 text-[10px] font-bold text-[#737373] uppercase tracking-wider">Add Node</div>
            <input
              ref={inputRef}
              type="text"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              placeholder="Search nodes, pins, descriptions..."
              className="w-full px-3 py-2 bg-[#171717] border border-[#525252] rounded text-sm text-white placeholder-[#737373] focus:outline-none focus:border-blue-500"
            />
          </div>

          {/* 节点列表 */}
          <div className="max-h-80 overflow-auto">
            {filteredNodes.length === 0 ? (
              <div className="px-3 py-4 text-sm text-[#737373] text-center">
                No nodes found
              </div>
            ) : (
              filteredNodes.map((node, index) => (
                <div
                  key={node.className}
                  onClick={() => handleNodeClick(node.className)}
                  onMouseEnter={() => node.desc && setTooltip(node.desc)}
                  onMouseLeave={() => setTooltip(null)}
                  className={`px-3 py-2 text-sm cursor-pointer flex items-center gap-2 ${index === selectedIndex
                    ? 'bg-blue-600 text-white'
                    : 'text-[#d4d4d4] hover:bg-[#404040]'
                    }`}
                >
                  <span className="w-2 h-2 rounded-full" style={{ backgroundColor: CATEGORY_COLORS[node.category] || '#888' }} />
                  <span className="flex-1">{node.className}</span>
                  <span className="text-xs text-gray-500 capitalize">{node.category}</span>
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
