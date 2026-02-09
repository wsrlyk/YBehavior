import { useState, useEffect, useRef } from 'react';
import { useEditorStore } from '../stores/editorStore';
import { useFSMStore } from '../stores/fsmStore';
import { readFile } from '../utils/fileService';
import { getFileDisplay } from '../utils/fileUtils';

interface FileTreePopupProps {
  isOpen: boolean;
  onClose: () => void;
}

interface TreeNode {
  name: string;
  path: string;
  isDir: boolean;
  children: TreeNode[];
}

// 将文件列表转换为树结构
function buildFileTree(files: string[]): TreeNode[] {
  const root: TreeNode[] = [];

  for (const file of files) {
    const parts = file.split(/[/\\]/);
    let current = root;
    let currentPath = '';

    for (let i = 0; i < parts.length; i++) {
      const part = parts[i];
      currentPath = currentPath ? `${currentPath}/${part}` : part;
      const isLast = i === parts.length - 1;

      let node = current.find(n => n.name === part);
      if (!node) {
        node = {
          name: part,
          path: currentPath,
          isDir: !isLast,
          children: [],
        };
        current.push(node);
      }
      current = node.children;
    }
  }

  // 排序：目录在前，文件在后，按名称排序
  const sortNodes = (nodes: TreeNode[]) => {
    nodes.sort((a, b) => {
      if (a.isDir !== b.isDir) return a.isDir ? -1 : 1;
      return a.name.localeCompare(b.name);
    });
    nodes.forEach(n => sortNodes(n.children));
  };
  sortNodes(root);

  return root;
}

// 递归渲染树节点
function FileTreeNode({
  node,
  depth,
  expandedDirs,
  toggleDir,
  onFileClick,
  filter,
}: {
  node: TreeNode;
  depth: number;
  expandedDirs: Set<string>;
  toggleDir: (path: string) => void;
  onFileClick: (path: string) => void;
  filter: string;
}) {
  const isExpanded = expandedDirs.has(node.path);
  const matchesFilter = !filter || node.path.toLowerCase().includes(filter.toLowerCase());

  // 如果有筛选，检查子节点是否匹配
  const hasMatchingChildren = filter && node.children.some(child =>
    child.path.toLowerCase().includes(filter.toLowerCase()) ||
    (child.isDir && hasMatchingDescendants(child, filter))
  );

  if (filter && !matchesFilter && !hasMatchingChildren) {
    return null;
  }

  if (node.isDir) {
    const shouldExpand = filter ? true : isExpanded;
    return (
      <div>
        <div
          onClick={() => toggleDir(node.path)}
          className="flex items-center px-2 py-1 text-sm text-gray-300 hover:bg-gray-700 cursor-pointer"
          style={{ paddingLeft: `${depth * 16 + 8}px` }}
        >
          <span className="mr-1 text-xs text-gray-500">{shouldExpand ? '▼' : '▶'}</span>
          <span className="text-yellow-500 mr-1">📁</span>
          {node.name}
        </div>
        {shouldExpand && node.children.map(child => (
          <FileTreeNode
            key={child.path}
            node={child}
            depth={depth + 1}
            expandedDirs={expandedDirs}
            toggleDir={toggleDir}
            onFileClick={onFileClick}
            filter={filter}
          />
        ))}
      </div>
    );
  }

  // 根据文件类型显示不同图标，隐藏后缀
  const { icon, name: displayName } = getFileDisplay(node.name);

  return (
    <div
      onClick={() => onFileClick(node.path)}
      className="flex items-center px-2 py-1 text-sm text-gray-300 hover:bg-gray-700 cursor-pointer"
      style={{ paddingLeft: `${depth * 16 + 8}px` }}
    >
      <span className="mr-1.5">{icon}</span>
      {displayName}
    </div>
  );
}

function hasMatchingDescendants(node: TreeNode, filter: string): boolean {
  return node.children.some(child =>
    child.path.toLowerCase().includes(filter.toLowerCase()) ||
    (child.isDir && hasMatchingDescendants(child, filter))
  );
}

export function FileTreePopup({ isOpen, onClose }: FileTreePopupProps) {
  const { treeFiles, openTree, setActiveFile } = useEditorStore();
  const { openFSM, setActiveFSM } = useFSMStore();
  const [filter, setFilter] = useState('');
  const [expandedDirs, setExpandedDirs] = useState<Set<string>>(new Set());
  const inputRef = useRef<HTMLInputElement>(null);
  const popupRef = useRef<HTMLDivElement>(null);

  const fileTree = buildFileTree(treeFiles);

  useEffect(() => {
    if (isOpen) {
      setFilter('');
      setTimeout(() => inputRef.current?.focus(), 0);
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (e: MouseEvent) => {
      if (popupRef.current && !popupRef.current.contains(e.target as unknown as Node)) {
        onClose();
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen, onClose]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      onClose();
    }
  };

  const toggleDir = (path: string) => {
    setExpandedDirs((prev: Set<string>) => {
      const next = new Set(prev);
      if (next.has(path)) {
        next.delete(path);
      } else {
        next.add(path);
      }
      return next;
    });
  };

  const handleFileClick = async (path: string) => {
    if (path.endsWith('.fsm')) {
      const { editorTreeDir } = useEditorStore.getState();
      if (editorTreeDir) {
        try {
          const content = await readFile(`${editorTreeDir}/${path}`);
          openFSM(path, content);
          // Deactivate tree
          setActiveFile(null as any);
        } catch (e) {
          console.error('Failed to open FSM:', e);
        }
      }
    } else {
      openTree(path);
      // Deactivate fsm
      setActiveFSM(null as any);
    }
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div
      ref={popupRef}
      className="absolute top-12 left-2 z-50 w-96 bg-gray-800 border border-gray-600 rounded-lg shadow-xl overflow-hidden flex flex-col max-h-[80vh]"
      onKeyDown={handleKeyDown}
    >
      {/* 搜索框 */}
      <div className="p-2 border-b border-gray-700 shrink-0">
        <input
          ref={inputRef}
          type="text"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          placeholder="Search files..."
          className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-sm text-white placeholder-gray-500 focus:outline-none focus:border-blue-500"
        />
      </div>

      {/* 文件树 */}
      <div className="flex-1 overflow-auto py-1">
        {fileTree.length === 0 ? (
          <div className="px-3 py-4 text-sm text-gray-500 text-center">
            No files found
          </div>
        ) : (
          fileTree.map(node => (
            <FileTreeNode
              key={node.path}
              node={node}
              depth={0}
              expandedDirs={expandedDirs}
              toggleDir={toggleDir}
              onFileClick={handleFileClick}
              filter={filter}
            />
          ))
        )}
      </div>

      {/* 底部提示 */}
      <div className="px-3 py-2 border-t border-gray-700 text-xs text-gray-500">
        Click to expand/open · Esc Close
      </div>
    </div>
  );
}
