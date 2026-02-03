import { useState, useEffect, useRef } from 'react';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { useEditorStore } from '../stores/editorStore';
import type { NodeCategory } from '../types';

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

  const filteredNodes = allNodes.filter(node =>
    node.className.toLowerCase().includes(filter.toLowerCase())
  );

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

  const CATEGORY_COLORS: Record<NodeCategory, string> = {
    composite: 'bg-green-600',
    decorator: 'bg-blue-600',
    action: 'bg-orange-600',
    condition: 'bg-purple-600',
  };

  return (
    <div
      ref={menuRef}
      style={{ left: position.x, top: position.y }}
      className="absolute z-50 w-72 bg-gray-800 border border-gray-600 rounded-lg shadow-xl overflow-hidden"
      onKeyDown={handleKeyDown}
    >
      {node && (
        <div className="p-1 border-b border-gray-700 bg-gray-900/50">
          <div className="px-3 py-1 text-[10px] font-bold text-gray-500 uppercase tracking-wider">Node Actions</div>

          <div
            className="px-3 py-2 text-sm text-gray-300 hover:bg-gray-700 cursor-pointer flex items-center justify-between group"
            onClick={() => { toggleConditionConnector(node.id); onClose(); }}
          >
            <span>Condition Connector</span>
            <span className={`text-[10px] px-1.5 rounded ${node.hasConditionConnector ? 'bg-purple-600 text-white' : 'bg-gray-700 text-gray-400 group-hover:bg-gray-600'}`}>
              {node.hasConditionConnector ? 'ON' : 'OFF'}
            </span>
          </div>

          <div
            className="px-3 py-2 text-sm text-gray-300 hover:bg-gray-700 cursor-pointer flex items-center justify-between group"
            onClick={() => { toggleNodeDisabled(node.id); onClose(); }}
          >
            <span>Node State</span>
            <span className={`text-[10px] px-1.5 rounded ${node.disabled ? 'bg-red-600 text-white' : 'bg-gray-700 text-gray-400 group-hover:bg-gray-600'}`}>
              {node.disabled ? 'DISABLED' : 'ENABLED'}
            </span>
          </div>

          <div
            className="px-3 py-2 text-sm text-gray-300 hover:bg-gray-700 cursor-pointer flex items-center justify-between group"
            onClick={() => { toggleNodeFold(node.id); onClose(); }}
          >
            <span>Folding</span>
            <span className={`text-[10px] px-1.5 rounded ${node.isFolded ? 'bg-blue-600 text-white' : 'bg-gray-700 text-gray-400 group-hover:bg-gray-600'}`}>
              {node.isFolded ? 'FOLDED' : 'NORMAL'}
            </span>
          </div>
        </div>
      )}

      {/* 搜索框 (如果选了节点，就不显示添加节点内容) */}
      {!node && (
        <>
          <div className="p-2 border-b border-gray-700">
            <div className="px-1 mb-1 text-[10px] font-bold text-gray-500 uppercase tracking-wider">Add Node</div>
            <input
              ref={inputRef}
              type="text"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              placeholder="Search nodes..."
              className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-sm text-white placeholder-gray-500 focus:outline-none focus:border-blue-500"
            />
          </div>

          {/* 节点列表 */}
          <div className="max-h-80 overflow-auto">
            {filteredNodes.length === 0 ? (
              <div className="px-3 py-4 text-sm text-gray-500 text-center">
                No nodes found
              </div>
            ) : (
              filteredNodes.map((node, index) => (
                <div
                  key={node.className}
                  onClick={() => handleNodeClick(node.className)}
                  className={`px-3 py-2 text-sm cursor-pointer flex items-center gap-2 ${index === selectedIndex
                      ? 'bg-blue-600 text-white'
                      : 'text-gray-300 hover:bg-gray-700'
                    }`}
                >
                  <span className={`w-2 h-2 rounded-full ${CATEGORY_COLORS[node.category]}`} />
                  <span className="flex-1">{node.className}</span>
                  <span className="text-xs text-gray-500 capitalize">{node.category}</span>
                </div>
              ))
            )}
          </div>

          {/* 底部提示 */}
          <div className="px-3 py-2 border-t border-gray-700 text-xs text-gray-500">
            ↑↓ Navigate · Enter Add · Esc Close
          </div>
        </>
      )}
    </div>
  );
}
