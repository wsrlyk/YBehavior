import { create } from 'zustand';
import type { Tree, TreeNode, TreeConnection } from '../types';
import { loadTree, listTreeFiles } from '../utils/fileService';

interface OpenedFile {
  path: string;
  name: string;
  tree: Tree;
  isDirty: boolean;
}

interface EditorState {
  // 工作目录
  workingDir: string | null;
  
  // 文件列表（工作目录下所有文件）
  treeFiles: string[];
  
  // 已打开的文件列表
  openedFiles: OpenedFile[];
  
  // 当前激活的文件路径
  activeFilePath: string | null;
  
  // 加载状态
  isLoading: boolean;
  error: string | null;
  
  // 选中的节点 ID 列表
  selectedNodeIds: string[];
  
  // 操作方法
  setWorkingDir: (dir: string) => Promise<void>;
  openTree: (path: string) => Promise<void>;
  closeFile: (path: string) => void;
  setActiveFile: (path: string) => void;
  selectNodes: (nodeIds: string[]) => void;
  addSelectedNode: (nodeId: string) => void;
  clearSelection: () => void;
  
  // 获取当前树
  getCurrentTree: () => Tree | null;
  
  // 节点操作
  addNode: (node: TreeNode) => void;
  removeNode: (nodeId: string) => void;
  updateNodePosition: (nodeId: string, x: number, y: number) => void;
  updateNodeProperty: (nodeId: string, updates: Partial<TreeNode>) => void;
  
  // 连接操作
  addConnection: (connection: TreeConnection) => void;
  removeConnection: (connectionId: string) => void;
}

// 辅助函数：更新已打开文件的树
function updateOpenedFileTree(
  openedFiles: OpenedFile[],
  activeFilePath: string | null,
  updater: (tree: Tree) => Tree
): { openedFiles: OpenedFile[] } | null {
  if (!activeFilePath) return null;
  
  const fileIndex = openedFiles.findIndex(f => f.path === activeFilePath);
  if (fileIndex === -1) return null;
  
  const file = openedFiles[fileIndex];
  const newTree = updater(file.tree);
  
  const newOpenedFiles = [...openedFiles];
  newOpenedFiles[fileIndex] = { ...file, tree: newTree, isDirty: true };
  
  return { openedFiles: newOpenedFiles };
}

export const useEditorStore = create<EditorState>((set, get) => ({
  workingDir: null,
  treeFiles: [],
  openedFiles: [],
  activeFilePath: null,
  isLoading: false,
  error: null,
  selectedNodeIds: [],
  
  setWorkingDir: async (dir) => {
    set({ workingDir: dir, isLoading: true, error: null });
    try {
      const files = await listTreeFiles(dir);
      set({ treeFiles: files, isLoading: false });
    } catch (e) {
      set({ error: String(e), isLoading: false });
    }
  },
  
  openTree: async (path) => {
    const { workingDir, openedFiles } = get();
    if (!workingDir) return;
    
    // 如果已经打开，直接切换
    const existing = openedFiles.find(f => f.path === path);
    if (existing) {
      set({ activeFilePath: path, selectedNodeIds: [] });
      return;
    }
    
    set({ isLoading: true, error: null });
    try {
      const fullPath = `${workingDir}/${path}`;
      const tree = await loadTree(fullPath);
      const fileName = path.split('/').pop() || path;
      
      const newFile: OpenedFile = {
        path,
        name: fileName,
        tree,
        isDirty: false,
      };
      
      set({
        openedFiles: [...openedFiles, newFile],
        activeFilePath: path,
        isLoading: false,
        selectedNodeIds: [],
      });
    } catch (e) {
      set({ error: String(e), isLoading: false });
    }
  },
  
  closeFile: (path) => set((state) => {
    const newOpenedFiles = state.openedFiles.filter(f => f.path !== path);
    let newActiveFilePath = state.activeFilePath;
    
    // 如果关闭的是当前文件，切换到其他文件
    if (state.activeFilePath === path) {
      newActiveFilePath = newOpenedFiles.length > 0 ? newOpenedFiles[0].path : null;
    }
    
    return {
      openedFiles: newOpenedFiles,
      activeFilePath: newActiveFilePath,
      selectedNodeIds: [],
    };
  }),
  
  setActiveFile: (path) => set({ activeFilePath: path, selectedNodeIds: [] }),
  
  getCurrentTree: () => {
    const { openedFiles, activeFilePath } = get();
    if (!activeFilePath) return null;
    const file = openedFiles.find(f => f.path === activeFilePath);
    return file?.tree || null;
  },
  
  selectNodes: (nodeIds) => set({ selectedNodeIds: nodeIds }),
  
  addSelectedNode: (nodeId) => set((state) => ({
    selectedNodeIds: state.selectedNodeIds.includes(nodeId)
      ? state.selectedNodeIds
      : [...state.selectedNodeIds, nodeId]
  })),
  
  clearSelection: () => set({ selectedNodeIds: [] }),
  
  updateNodePosition: (nodeId, x, y) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const node = tree.nodes.get(nodeId);
      if (!node) return tree;
      
      const newNodes = new Map(tree.nodes);
      newNodes.set(nodeId, { ...node, position: { x, y } });
      return { ...tree, nodes: newNodes };
    });
    
    return result || state;
  }),
  
  addNode: (node) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const newNodes = new Map(tree.nodes);
      newNodes.set(node.id, node);
      return { ...tree, nodes: newNodes };
    });
    
    return result || state;
  }),
  
  removeNode: (nodeId) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const newNodes = new Map(tree.nodes);
      newNodes.delete(nodeId);
      
      const newConnections = tree.connections.filter(
        (c) => c.parentNodeId !== nodeId && c.childNodeId !== nodeId
      );
      
      return { ...tree, nodes: newNodes, connections: newConnections };
    });
    
    if (!result) return state;
    
    return {
      ...result,
      selectedNodeIds: state.selectedNodeIds.filter((id) => id !== nodeId),
    };
  }),
  
  updateNodeProperty: (nodeId, updates) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const node = tree.nodes.get(nodeId);
      if (!node) return tree;
      
      const newNodes = new Map(tree.nodes);
      newNodes.set(nodeId, { ...node, ...updates });
      return { ...tree, nodes: newNodes };
    });
    
    return result || state;
  }),
  
  addConnection: (connection) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      return {
        ...tree,
        connections: [...tree.connections, connection],
      };
    });
    
    return result || state;
  }),
  
  removeConnection: (connectionId) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      return {
        ...tree,
        connections: tree.connections.filter((c) => c.id !== connectionId),
      };
    });
    
    return result || state;
  }),
}));
