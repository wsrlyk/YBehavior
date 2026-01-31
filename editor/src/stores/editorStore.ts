import { create } from 'zustand';
import type { Tree, TreeNode, TreeConnection, Variable, Pin } from '../types';
import { loadTree, listTreeFiles, saveFile } from '../utils/fileService';
import { loadSettings, type Settings } from '../utils/settings';
import { useNodeDefinitionStore } from './nodeDefinitionStore';
import { serializeTreeForEditor, serializeTreeForRuntime } from '../utils/xmlSerializer';

interface OpenedFile {
  path: string;
  name: string;
  tree: Tree;
  isDirty: boolean;
}

interface EditorState {
  // 设置
  settings: Settings | null;
  
  // 编辑器树目录（从 settings 加载）
  editorTreeDir: string | null;
  
  // 运行时树目录（从 settings 加载）
  runtimeTreeDir: string | null;
  
  // 文件列表（编辑器目录下所有文件）
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
  initSettings: () => Promise<void>;
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
  
  // 变量操作
  addVariable: (isLocal: boolean, variable: Variable) => void;
  removeVariable: (isLocal: boolean, name: string) => void;
  updateVariable: (isLocal: boolean, name: string, updates: Partial<Variable>) => void;
  
  // Pin 操作
  updatePin: (nodeId: string, pinName: string, updates: Partial<Pin>) => void;
  
  // 保存操作
  saveCurrentFile: () => Promise<void>;
  
  // 新建文件
  createNewTree: (name: string) => void;
}

/**
 * 计算节点的 UID（深度优先遍历）
 * Root 节点从 1 开始，森林中其他树从 1001、2001 等开始
 */
function recalculateUIDs(tree: Tree): void {
  let uid = 1;
  
  // 深度优先遍历
  function dfs(nodeId: string) {
    const node = tree.nodes.get(nodeId);
    if (!node) return;
    
    node.uid = uid++;
    
    // 获取子节点（按连接顺序）
    const childConns = tree.connections.filter(c => c.parentNodeId === nodeId);
    for (const conn of childConns) {
      dfs(conn.childNodeId);
    }
  }
  
  // 先清除所有 UID
  for (const node of tree.nodes.values()) {
    node.uid = undefined;
  }
  
  // 从主根节点开始
  if (tree.rootId) {
    dfs(tree.rootId);
  }
  
  // 处理森林中的其他树（从 1001、2001 等开始）
  let forestIndex = 1;
  for (const [nodeId, node] of tree.nodes) {
    if (node.uid === undefined) {
      // 这是一个未被遍历到的根节点（森林中的其他树）
      uid = forestIndex * 1000 + 1;
      dfs(nodeId);
      forestIndex++;
    }
  }
}

// 辅助函数：更新已打开文件的树（自动重新计算 UID）
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
  
  // 重新计算 UID
  recalculateUIDs(newTree);
  
  const newOpenedFiles = [...openedFiles];
  newOpenedFiles[fileIndex] = { ...file, tree: newTree, isDirty: true };
  
  return { openedFiles: newOpenedFiles };
}

export const useEditorStore = create<EditorState>((set, get) => ({
  settings: null,
  editorTreeDir: null,
  runtimeTreeDir: null,
  treeFiles: [],
  openedFiles: [],
  activeFilePath: null,
  isLoading: false,
  error: null,
  selectedNodeIds: [],
  
  initSettings: async () => {
    set({ isLoading: true, error: null });
    try {
      const settings = await loadSettings();
      const files = await listTreeFiles(settings.editorTreeDir);
      set({
        settings,
        editorTreeDir: settings.editorTreeDir,
        runtimeTreeDir: settings.runtimeTreeDir,
        treeFiles: files,
        isLoading: false,
      });
    } catch (e) {
      set({ error: String(e), isLoading: false });
    }
  },
  
  openTree: async (path) => {
    const { editorTreeDir, openedFiles } = get();
    if (!editorTreeDir) return;
    
    // 如果已经打开，直接切换
    const existing = openedFiles.find(f => f.path === path);
    if (existing) {
      set({ activeFilePath: path, selectedNodeIds: [] });
      return;
    }
    
    set({ isLoading: true, error: null });
    try {
      const fullPath = `${editorTreeDir}/${path}`;
      // 获取节点定义查找函数
      const { getDefinition } = useNodeDefinitionStore.getState();
      const tree = await loadTree(fullPath, getDefinition);
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
      // 不允许删除 Root 节点
      const node = tree.nodes.get(nodeId);
      if (node?.type === 'Root') return tree;
      
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
  
  saveCurrentFile: async () => {
    const { openedFiles, activeFilePath, editorTreeDir, runtimeTreeDir } = get();
    if (!activeFilePath || !editorTreeDir || !runtimeTreeDir) return;
    
    const file = openedFiles.find(f => f.path === activeFilePath);
    if (!file) return;
    
    try {
      // 序列化为编辑器版和运行时版
      const editorXml = serializeTreeForEditor(file.tree);
      const runtimeXml = serializeTreeForRuntime(file.tree);
      
      // 保存编辑器版
      const editorPath = `${editorTreeDir}/${activeFilePath}`;
      await saveFile(editorPath, editorXml);
      
      // 保存运行时版
      const runtimePath = `${runtimeTreeDir}/${activeFilePath}`;
      await saveFile(runtimePath, runtimeXml);
      
      // 标记为已保存
      set((state) => ({
        openedFiles: state.openedFiles.map(f => 
          f.path === activeFilePath ? { ...f, isDirty: false } : f
        ),
      }));
      
      console.log('Saved:', editorPath, runtimePath);
    } catch (e) {
      console.error('Save failed:', e);
      set({ error: String(e) });
    }
  },
  
  // 变量操作
  addVariable: (isLocal, variable) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      if (isLocal) {
        return { ...tree, localVariables: [...tree.localVariables, variable] };
      } else {
        return { ...tree, sharedVariables: [...tree.sharedVariables, variable] };
      }
    });
    return result || state;
  }),
  
  removeVariable: (isLocal, name) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      if (isLocal) {
        return { ...tree, localVariables: tree.localVariables.filter(v => v.name !== name) };
      } else {
        return { ...tree, sharedVariables: tree.sharedVariables.filter(v => v.name !== name) };
      }
    });
    return result || state;
  }),
  
  updateVariable: (isLocal, name, updates) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      if (isLocal) {
        return {
          ...tree,
          localVariables: tree.localVariables.map(v => 
            v.name === name ? { ...v, ...updates } : v
          ),
        };
      } else {
        return {
          ...tree,
          sharedVariables: tree.sharedVariables.map(v => 
            v.name === name ? { ...v, ...updates } : v
          ),
        };
      }
    });
    return result || state;
  }),
  
  // Pin 操作
  updatePin: (nodeId, pinName, updates) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const node = tree.nodes.get(nodeId);
      if (!node) return tree;
      
      const newPins = node.pins.map(pin => 
        pin.name === pinName ? { ...pin, ...updates } : pin
      );
      
      const newNodes = new Map(tree.nodes);
      newNodes.set(nodeId, { ...node, pins: newPins });
      
      return { ...tree, nodes: newNodes };
    });
    return result || state;
  }),
  
  // 新建文件
  createNewTree: (name) => set((state) => {
    // 生成唯一名称（如果已存在则加数字后缀）
    let uniqueName = name;
    let counter = 1;
    while (state.openedFiles.some(f => f.path === `${uniqueName}.tree`)) {
      uniqueName = `${name}${counter}`;
      counter++;
    }
    const path = `${uniqueName}.tree`;
    
    // 创建新树，自动添加 Root 节点
    const rootId = 'node-1';
    const rootNode: TreeNode = {
      id: rootId,
      guid: 1,
      uid: 1,
      type: 'Root',
      category: 'decorator',
      position: { x: 0, y: 0 },
      disabled: false,
      pins: [],
      childrenIds: [],
    };
    
    const nodes = new Map<string, TreeNode>();
    nodes.set(rootId, rootNode);
    
    const newTree: Tree = {
      name,
      path,
      nodes,
      rootId,
      connections: [],
      dataConnections: [],
      sharedVariables: [],
      localVariables: [],
      inputPins: [],
      outputPins: [],
      comments: [],
    };
    
    const newFile: OpenedFile = {
      path,
      name,
      tree: newTree,
      isDirty: true,
    };
    
    return {
      openedFiles: [...state.openedFiles, newFile],
      activeFilePath: path,
    };
  }),
}));
