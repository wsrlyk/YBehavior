import { create } from 'zustand';
import type { Tree, TreeNode, TreeConnection, DataConnection, Variable, Pin } from '../types';
import { loadTree, listTreeFiles, saveFile } from '../utils/fileService';
import { loadSettings, type Settings } from '../utils/settings';
import { useNodeDefinitionStore } from './nodeDefinitionStore';
import { serializeTreeForEditor, serializeTreeForRuntime } from '../utils/xmlSerializer';
import { validateValue, getDefaultValue } from '../utils/validation';
import { useNotificationStore } from './notificationStore';
import { logger } from '../utils/logger';
import { useEditorMetaStore } from './editorMetaStore';

interface HistoryState {
  past: Tree[];
  future: Tree[];
}

interface OpenedFile {
  path: string;
  name: string;
  tree: Tree;
  lastSavedTreeSnapshot: string; // 用于判断 isDirty 的快照
  isDirty: boolean;
  isNew?: boolean; // 是否是新创建未保存的文件
  history: HistoryState;
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

  // 数据连接操作
  addDataConnection: (dataConn: DataConnection) => void;
  removeDataConnection: (dataConnId: string) => void;

  // 变量操作
  addVariable: (isLocal: boolean, variable: Variable) => void;
  removeVariable: (isLocal: boolean, name: string) => void;
  updateVariable: (isLocal: boolean, name: string, updates: Partial<Variable>) => void;

  // Pin 操作
  updatePin: (nodeId: string, pinName: string, updates: Partial<Pin>) => void;

  // 保存操作
  saveCurrentFile: () => Promise<void>;
  saveFileAs: () => Promise<void>;

  // 历史操作
  undo: () => void;
  redo: () => void;
  recordHistoryStart: () => void; // 连续操作前记录快照
  finalizeContinuousAction: () => void; // 连续操作后刷新 UID 和 Dirty

  // 新特性操作
  toggleNodeFold: (nodeId: string) => void;
  toggleNodeDisabled: (nodeId: string) => void;
  toggleConditionConnector: (nodeId: string) => void;

  // 新建文件
  createNewTree: (name: string) => void;
}

/**
 * 计算节点的 UID（深度优先遍历）
 * Root 节点从 1 开始，森林中其他树从 1001、2001 等开始
 * 注意：必须传入可修改的 tree 对象（通常是 updater 返回的新 tree）
 */
/**
 * 计算节点的 UID（深度优先遍历）
 * 注意：此函数会【直接修改】tree.nodes 中的节点对象。
 * 调用者必须确保 tree.nodes 是新克隆的副本，以免污染历史记录。
 */
function recalculateUIDs(tree: Tree): void {
  const nodeDefStore = useNodeDefinitionStore.getState();
  let uid = 1;
  const visited = new Set<string>();

  // 预先建立父子关系索引
  const connectionsByParent = new Map<string, TreeConnection[]>();
  for (const conn of tree.connections) {
    const list = connectionsByParent.get(conn.parentNodeId) || [];
    list.push(conn);
    connectionsByParent.set(conn.parentNodeId, list);
  }

  // 深度优先遍历
  function dfs(nodeId: string, isAncestorDisabled: boolean) {
    if (visited.has(nodeId)) return;
    visited.add(nodeId);

    const node = tree.nodes.get(nodeId);
    if (!node) return;

    const effectivelyDisabled = isAncestorDisabled || node.disabled;
    const childConns = connectionsByParent.get(nodeId) || [];

    // 1. 如果有 condition 连接，先递归处理该分支
    const conditionConn = childConns.find(c => c.parentConnector === 'condition');
    if (conditionConn) {
      dfs(conditionConn.childNodeId, effectivelyDisabled);
    }

    // 2. 采样当前节点的 UID
    node.uid = effectivelyDisabled ? undefined : uid++;

    // 3. 处理其他子节点
    const nodeDef = nodeDefStore.getDefinition(node.type);
    const connectors = nodeDef?.childConnectors || [];

    for (const connectorDef of connectors) {
      const connsForThisType = childConns.filter(c => c.parentConnector === connectorDef.name);

      const sortedConns = [...connsForThisType].sort((a, b) => {
        const nodeA = tree.nodes.get(a.childNodeId);
        const nodeB = tree.nodes.get(b.childNodeId);
        return (nodeA?.position.x || 0) - (nodeB?.position.x || 0);
      });

      for (const conn of sortedConns) {
        dfs(conn.childNodeId, effectivelyDisabled);
      }
    }

    // 处理不在定义中的连接器
    const knownConnectorNames = new Set(connectors.map(c => c.name));
    knownConnectorNames.add('condition');

    const extraConns = childConns.filter(c => !knownConnectorNames.has(c.parentConnector));
    const sortedExtraConns = [...extraConns].sort((a, b) => {
      const nodeA = tree.nodes.get(a.childNodeId);
      const nodeB = tree.nodes.get(b.childNodeId);
      return (nodeA?.position.x || 0) - (nodeB?.position.x || 0);
    });

    for (const conn of sortedExtraConns) {
      dfs(conn.childNodeId, effectivelyDisabled);
    }
  }

  tree.nodes.forEach(node => { node.uid = undefined; });
  if (tree.rootId) dfs(tree.rootId, false);

  let forestIndex = 1;
  const childIds = new Set(tree.connections.map(c => c.childNodeId));

  for (const nodeId of tree.nodes.keys()) {
    if (!childIds.has(nodeId) && !visited.has(nodeId)) {
      uid = forestIndex * 1000 + 1;
      dfs(nodeId, false);
      forestIndex++;
    }
  }
}

// 辅助函数：更新已打开文件的树（自动重新计算 UID 和处理历史）
function updateOpenedFileTree(
  openedFiles: OpenedFile[],
  activeFilePath: string | null,
  updater: (tree: Tree) => Tree,
  options: { skipHistory?: boolean; isUIChange?: boolean; skipUID?: boolean } = {}
): { openedFiles: OpenedFile[] } | null {
  if (!activeFilePath) return null;

  const fileIndex = openedFiles.findIndex(f => f.path === activeFilePath);
  if (fileIndex === -1) return null;

  const file = openedFiles[fileIndex];
  const oldTree = file.tree;
  const newTree = updater(oldTree);

  // 重新计算 UID
  if (!options.skipUID) {
    recalculateUIDs(newTree);
  }

  // 计算新的 Dirty 状态
  let isDirty = file.isDirty;
  if (options.isUIChange) {
    // UI 变化不改变 Dirty 状态
    isDirty = file.isDirty;
  } else if (!options.skipHistory) {
    // 标准操作：通过序列化对比判断
    const currentSnapshot = serializeTreeForEditor(newTree);
    isDirty = currentSnapshot !== file.lastSavedTreeSnapshot;
  } else {
    // 拖拽等操作：直接设为 dirty
    isDirty = true;
  }

  const newFile: OpenedFile = {
    ...file,
    tree: newTree,
    isDirty
  };

  // 处理历史记录
  if (!options.skipHistory) {
    newFile.history = {
      past: [oldTree, ...file.history.past].slice(0, 50),
      future: [] // 产生新变化时清空前进栈
    };
  }

  const newOpenedFiles = [...openedFiles];
  newOpenedFiles[fileIndex] = newFile;

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

      // 加载本地元数据（如折叠状态）
      const meta = useEditorMetaStore.getState().getTreeMeta(path);
      if (meta) {
        tree.nodes.forEach((node, id) => {
          if (meta.nodes[id]) {
            node.isFolded = meta.nodes[id].isFolded;
          }
        });
      }

      const newFile: OpenedFile = {
        path,
        name: fileName,
        tree,
        lastSavedTreeSnapshot: serializeTreeForEditor(tree),
        isDirty: false,
        history: { past: [], future: [] }
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

  closeFile: async (path) => {
    const { openedFiles } = get();
    const file = openedFiles.find(f => f.path === path);

    if (file && file.isDirty) {
      try {
        const { ask } = await import('@tauri-apps/plugin-dialog');
        const answer = await ask(
          `The file "${file.name}" has unsaved changes. Do you want to close it?`,
          { title: 'Confirm Close', kind: 'warning', okLabel: 'Close Without Saving', cancelLabel: 'Cancel' }
        );
        if (!answer) return;
      } catch (e) {
        console.error('Dialog failed:', e);
      }
    }

    set((state) => {
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
    });
  },

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
    }, { skipHistory: true, skipUID: true }); // 拖拽中跳过历史和 UID 计算

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

  addDataConnection: (dataConn) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      // 辅助函数：检查节点是否被任何祖先禁用
      const isEffectivelyDisabled = (nodeId: string): boolean => {
        let current = nodeId;
        while (current) {
          const n = tree.nodes.get(current);
          if (n?.disabled) return true;
          const parentConn = tree.connections.find(c => c.childNodeId === current);
          if (!parentConn) break;
          current = parentConn.parentNodeId;
        }
        return false;
      };

      const fromDisabled = isEffectivelyDisabled(dataConn.fromNodeId);
      const toDisabled = isEffectivelyDisabled(dataConn.toNodeId);

      if (fromDisabled !== toDisabled) {
        useNotificationStore.getState().notify('Cannot connect enabled node with disabled node', 'error');
        return tree;
      }

      return {
        ...tree,
        dataConnections: [...tree.dataConnections, dataConn],
      };
    });

    return result || state;
  }),

  removeDataConnection: (dataConnId) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      return {
        ...tree,
        dataConnections: tree.dataConnections.filter((c) => c.id !== dataConnId),
      };
    });

    return result || state;
  }),

  saveCurrentFile: async () => {
    const { openedFiles, activeFilePath } = get();
    if (!activeFilePath) return;

    const file = openedFiles.find(f => f.path === activeFilePath);
    if (!file) return;

    // 如果是新文件，重定向到另存为
    if (file.isNew) {
      return get().saveFileAs();
    }

    const { editorTreeDir, runtimeTreeDir } = get();
    if (!editorTreeDir || !runtimeTreeDir) {
      useNotificationStore.getState().notify('Save failed: Tree directories not initialized in settings', 'error');
      return;
    }

    const tree = file.tree;

    // 辅助函数：获取从某个节点可达的所有节点 ID (可选择是否跳过禁用分支)
    const getReachableIds = (startId: string, skipDisabled: boolean): Set<string> => {
      const reachable = new Set<string>();
      const dfs = (id: string) => {
        if (reachable.has(id)) return;
        const node = tree.nodes.get(id);
        if (skipDisabled && node?.disabled) return;

        reachable.add(id);
        tree.connections.filter(c => c.parentNodeId === id).forEach(c => dfs(c.childNodeId));
      };
      if (startId) dfs(startId);
      return reachable;
    };

    // 辅助函数：获取节点的顶层根节点 ID
    const getNodeRootId = (nodeId: string): string => {
      let current = nodeId;
      while (true) {
        const parentConn = tree.connections.find(c => c.childNodeId === current);
        if (!parentConn) break;
        current = parentConn.parentNodeId;
      }
      return current;
    };

    const mainTreeIds = getReachableIds(tree.rootId, true);

    // 全量数据校验
    const errors: string[] = [];

    // 1. 校验全局变量
    file.tree.sharedVariables.forEach(v => {
      const res = validateValue(v.defaultValue, v.valueType, v.countType);
      if (!res.isValid) errors.push(`Shared Variable [${v.name}]: ${res.error}`);
    });

    // 2. 校验局部变量
    file.tree.localVariables.forEach(v => {
      const res = validateValue(v.defaultValue, v.valueType, v.countType);
      if (!res.isValid) errors.push(`Local Variable [${v.name}]: ${res.error}`);
    });

    // 3. 校验 Root 分支下节点的 Pin
    tree.nodes.forEach(node => {
      // 只有在 Root 分支下的节点且未禁用的才需要校验
      if (!mainTreeIds.has(node.id) || node.disabled) return;

      node.pins.forEach(pin => {
        if (pin.binding.type === 'const') {
          const res = validateValue(pin.binding.value, pin.valueType, pin.countType);
          if (!res.isValid) {
            const nodeLabel = `${node.type}:${node.uid || node.id}`;
            errors.push(`Node [${nodeLabel}] Pin [${pin.name}]: ${res.error}`);
          }
        }
      });
    });

    // 4. 校验数据连接：必须在同一个根节点下，且不能跨越禁用边界
    tree.dataConnections.forEach(dc => {
      const fromNode = tree.nodes.get(dc.fromNodeId);
      const toNode = tree.nodes.get(dc.toNodeId);

      // 注意：这里的 mainTreeIds 是不跳过禁用的（用于同树校验）
      const fromRootId = getNodeRootId(dc.fromNodeId);
      const toRootId = getNodeRootId(dc.toNodeId);
      if (fromRootId !== toRootId) {
        errors.push(`DataConnection [${dc.fromPinName} -> ${dc.toPinName}]: Nodes [${fromNode?.type}:${fromNode?.uid}] and [${toNode?.type}:${toNode?.uid}] must be in the same tree`);
        return;
      }

      // 检查有效禁用状态（如果两边一个是有效启用，一个是有效禁用，则报错）
      // 辅助函数：检查节点是否被任何祖先禁用
      const isEffectivelyDisabled = (nodeId: string): boolean => {
        let current = nodeId;
        while (current) {
          const n = tree.nodes.get(current);
          if (n?.disabled) return true;
          const parentConn = tree.connections.find(c => c.childNodeId === current);
          current = parentConn?.parentNodeId || '';
        }
        return false;
      };

      const fromDisabled = isEffectivelyDisabled(dc.fromNodeId);
      const toDisabled = isEffectivelyDisabled(dc.toNodeId);
      if (fromDisabled !== toDisabled) {
        errors.push(`DataConnection [${dc.fromPinName} -> ${dc.toPinName}]: Cannot connect enabled node with disabled node`);
      }
    });

    if (errors.length > 0) {
      const { notify } = useNotificationStore.getState();
      notify(`Save with ${errors.length} validation errors`, 'warning');
      errors.forEach(err => logger.error(err));
    }

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
          f.path === activeFilePath ? {
            ...f,
            isDirty: false,
            lastSavedTreeSnapshot: editorXml // 更新快照
          } : f
        ),
      }));

      if (errors.length === 0) {
        useNotificationStore.getState().notify('Save successful', 'success');
      }

      console.log('Saved:', editorPath, runtimePath);
    } catch (e) {
      console.error('Save failed:', e);
      set({ error: String(e) });
      useNotificationStore.getState().notify(`Save failed: ${e}`, 'error');
    }
  },

  saveFileAs: async () => {
    const { openedFiles, activeFilePath, editorTreeDir, runtimeTreeDir } = get();
    if (!activeFilePath || !editorTreeDir || !runtimeTreeDir) return;

    const file = openedFiles.find(f => f.path === activeFilePath);
    if (!file) return;

    try {
      const { save } = await import('@tauri-apps/plugin-dialog');
      const newPath = await save({
        title: 'Save Behavior Tree As',
        defaultPath: editorTreeDir,
        filters: [{ name: 'Behavior Tree', extensions: ['tree'] }]
      });

      if (!newPath) return; // 用户取消

      // 验证文件名不能包含空格
      const fileName = newPath.split(/[/\\]/).pop() || '';
      const treeName = fileName.replace(/\.tree$/, '');
      if (treeName.includes(' ')) {
        useNotificationStore.getState().notify('Tree name cannot contain spaces', 'error');
        return;
      }

      // 处理新路径，确保它是相对于 editorTreeDir 的相对路径
      let relativePath = newPath;
      const normalizedDir = editorTreeDir.replace(/\\/g, '/');
      const normalizedNewPath = newPath.replace(/\\/g, '/');

      if (normalizedNewPath.startsWith(normalizedDir)) {
        relativePath = normalizedNewPath.slice(normalizedDir.length).replace(/^\//, '');
      } else {
        // 如果保存在目录外，给出提示（虽然最好能支持任意路径，但目前系统架构依赖 root dir）
        useNotificationStore.getState().notify('Warning: Saving outside the predefined tree directory.', 'warning');
        // 提取文件名作为相对路径名
        relativePath = newPath.split(/[/\\]/).pop() || relativePath;
      }

      // 更新文件信息并执行保存逻辑（这里我们复用 saveCurrentFile 的保存部分，但先更新 store 状态）
      set(state => ({
        openedFiles: state.openedFiles.map(f =>
          f.path === activeFilePath
            ? {
              ...f,
              path: relativePath,
              name: fileName,
              isNew: false,
              lastSavedTreeSnapshot: serializeTreeForEditor(f.tree), // 预更新快照，虽然 saveCurrentFile 也会更新
              tree: { ...f.tree, path: relativePath, name: fileName.replace(/\.tree$/, '') }
            }
            : f
        ),
        activeFilePath: relativePath,
        treeFiles: state.treeFiles.includes(relativePath) ? state.treeFiles : [...state.treeFiles, relativePath]
      }));

      // 执行实际保存
      await get().saveCurrentFile();

    } catch (e) {
      console.error('Save As failed:', e);
      set({ error: String(e) });
      useNotificationStore.getState().notify(`Save As failed: ${e}`, 'error');
    }
  },

  undo: () => set(state => {
    const { openedFiles, activeFilePath } = state;
    if (!activeFilePath) return state;

    const fileIndex = openedFiles.findIndex(f => f.path === activeFilePath);
    if (fileIndex === -1) return state;

    const file = openedFiles[fileIndex];
    if (file.history.past.length === 0) return state;

    const [previousTree, ...remainingPast] = file.history.past;
    const currentTree = file.tree;

    const newFile: OpenedFile = {
      ...file,
      tree: previousTree,
      isDirty: serializeTreeForEditor(previousTree) !== file.lastSavedTreeSnapshot,
      history: {
        past: remainingPast,
        future: [currentTree, ...file.history.future].slice(0, 50)
      }
    };

    const newOpenedFiles = [...openedFiles];
    newOpenedFiles[fileIndex] = newFile;
    return { openedFiles: newOpenedFiles };
  }),

  redo: () => set(state => {
    const { openedFiles, activeFilePath } = state;
    if (!activeFilePath) return state;

    const fileIndex = openedFiles.findIndex(f => f.path === activeFilePath);
    if (fileIndex === -1) return state;

    const file = openedFiles[fileIndex];
    if (file.history.future.length === 0) return state;

    const [nextTree, ...remainingFuture] = file.history.future;
    const currentTree = file.tree;

    const newFile: OpenedFile = {
      ...file,
      tree: nextTree,
      isDirty: serializeTreeForEditor(nextTree) !== file.lastSavedTreeSnapshot,
      history: {
        past: [currentTree, ...file.history.past].slice(0, 50),
        future: remainingFuture
      }
    };

    const newOpenedFiles = [...openedFiles];
    newOpenedFiles[fileIndex] = newFile;
    return { openedFiles: newOpenedFiles };
  }),

  // 用于连续操作（如拖拽）开始前，先拍一个快照存入 past
  recordHistoryStart: () => set(state => {
    const { openedFiles, activeFilePath } = state;
    if (!activeFilePath) return state;

    const fileIndex = openedFiles.findIndex(f => f.path === activeFilePath);
    if (fileIndex === -1) return state;

    const file = openedFiles[fileIndex];

    const newOpenedFiles = [...openedFiles];
    newOpenedFiles[fileIndex] = {
      ...file,
      history: {
        past: [file.tree, ...file.history.past].slice(0, 50),
        future: [] // 新操作开始，清空前进栈
      }
    };
    return { openedFiles: newOpenedFiles };
  }),

  // 用于连续操作结束后，同步 UID 和 Dirty 状态，但不再次推送 history
  finalizeContinuousAction: () => set(state => {
    const { openedFiles, activeFilePath } = state;
    if (!activeFilePath) return state;

    const fileIndex = openedFiles.findIndex(f => f.path === activeFilePath);
    if (fileIndex === -1) return state;

    const file = openedFiles[fileIndex];

    // 强制深度克隆一次 Node Map 及其内节点，确保 UID 刷新不污染前一个快照
    const newNodes = new Map<string, TreeNode>();
    file.tree.nodes.forEach((node, id) => {
      newNodes.set(id, { ...node });
    });
    const newTree = { ...file.tree, nodes: newNodes };

    recalculateUIDs(newTree);

    const currentSnapshot = serializeTreeForEditor(newTree);
    const isDirty = currentSnapshot !== file.lastSavedTreeSnapshot;

    const newOpenedFiles = [...openedFiles];
    newOpenedFiles[fileIndex] = {
      ...file,
      tree: newTree,
      isDirty,
    };
    return { openedFiles: newOpenedFiles };
  }),

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
      let newTree = { ...tree };
      const vars = isLocal ? tree.localVariables : tree.sharedVariables;
      const targetVar = vars.find(v => v.name === name);

      if (!targetVar) return tree;

      // 如果更新了类型，需要检查引用它的 pins 是否失效
      const isTypeChanged = (updates.valueType && updates.valueType !== targetVar.valueType) ||
        (updates.countType && updates.countType !== targetVar.countType);

      if (isLocal) {
        newTree.localVariables = tree.localVariables.map(v => v.name === name ? { ...v, ...updates } : v);
      } else {
        newTree.sharedVariables = tree.sharedVariables.map(v => v.name === name ? { ...v, ...updates } : v);
      }

      if (isTypeChanged) {
        // 更新所有引用该变量的 pin 状态
        const updatedVar = (isLocal ? newTree.localVariables : newTree.sharedVariables).find(v => v.name === name)!;

        const newNodes = new Map(newTree.nodes);
        for (const [nodeId, node] of newNodes) {
          let nodeChanged = false;
          const newPins = node.pins.map(pin => {
            if (pin.binding.type === 'pointer' && pin.binding.variableName === name && pin.binding.isLocal === isLocal) {
              // 检查是否依然兼容
              const typeMatch = updatedVar.valueType === pin.valueType;
              const countMatch = pin.countType === 'list' ? updatedVar.countType === 'list' : true;

              if (!typeMatch || !countMatch) {
                nodeChanged = true;
                // 不再兼容，重置为数据连接模式（pointer + empty variableName）
                return {
                  ...pin,
                  binding: { type: 'pointer' as const, variableName: '', isLocal: false },
                  vectorIndex: undefined
                } as Pin;
              }
            }
            return pin;
          });

          if (nodeChanged) {
            newNodes.set(nodeId, { ...node, pins: newPins });
          }
        }
        newTree.nodes = newNodes;
      }

      return newTree;
    });
    return result || state;
  }),

  // Pin 操作
  updatePin: (nodeId, pinName, updates) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const node = tree.nodes.get(nodeId);
      if (!node) return tree;

      let newTree = { ...tree };
      const currentPin = node.pins.find(p => p.name === pinName);
      if (!currentPin) return tree;

      const isTypeChanged = (updates.valueType && updates.valueType !== currentPin.valueType) ||
        (updates.countType && updates.countType !== currentPin.countType);

      // 1. 更新当前 Pin
      let changedPinNames = new Set<string>();
      if (isTypeChanged) changedPinNames.add(pinName);

      let newPins = node.pins.map(pin => {
        if (pin.name === pinName) {
          const updatedPin = { ...pin, ...updates };
          if (isTypeChanged) {
            return {
              ...updatedPin,
              binding: { type: 'const' as const, value: getDefaultValue(updatedPin.valueType, updatedPin.countType === 'list') },
              vectorIndex: undefined
            } as Pin;
          }
          return updatedPin;
        }
        return pin;
      });

      // 2. 处理 Group 同步 (类型/数量)
      const hasVGroup = currentPin.vTypeGroup !== undefined && updates.valueType;
      const hasCGroup = currentPin.cTypeGroup !== undefined && updates.countType;

      if (hasVGroup || hasCGroup) {
        newPins = newPins.map(pin => {
          let pinUpdates: Partial<Pin> = {};
          if (hasVGroup && pin.vTypeGroup === currentPin.vTypeGroup && pin.valueType !== updates.valueType) {
            pinUpdates.valueType = updates.valueType;
          }
          if (hasCGroup && pin.cTypeGroup === currentPin.cTypeGroup && pin.countType !== updates.countType) {
            pinUpdates.countType = updates.countType;
          }

          if (Object.keys(pinUpdates).length > 0) {
            changedPinNames.add(pin.name);
            const updatedPin = { ...pin, ...pinUpdates };
            return {
              ...updatedPin,
              binding: { type: 'const' as const, value: getDefaultValue(updatedPin.valueType, updatedPin.countType === 'list') },
              vectorIndex: undefined
            } as Pin;
          }
          return pin;
        });
      }

      // 3. 处理 TypeMap 规则 (仅当 Enum 值变化且是常量绑定时)
      const nodeDef = useNodeDefinitionStore.getState().getDefinition(node.type);
      if (updates.binding?.type === 'const' && nodeDef?.typeMaps) {
        const srcValue = updates.binding.value;
        const matchedRules = nodeDef.typeMaps.filter(
          rule => rule.srcVariable === pinName && rule.srcValue === srcValue
        );

        if (matchedRules.length > 0) {
          newPins = newPins.map(pin => {
            const rule = matchedRules.find(r => r.desVariable === pin.name);
            if (!rule) return pin;

            const typeStr = rule.desType;
            const char = typeStr.charAt(0).toUpperCase();
            const valueMapping: Record<string, import('../types').ValueType> = {
              'I': 'int', 'F': 'float', 'B': 'bool', 'S': 'string', 'V': 'vector3', 'A': 'entity', 'U': 'ulong', 'E': 'enum'
            };

            let newValueType = valueMapping[char] || pin.valueType;
            let newCountType: import('../types').CountType = (typeStr.length >= 2 && typeStr[0] === typeStr[1]) ? 'list' : 'scalar';

            if (pin.valueType === newValueType && pin.countType === newCountType) return pin;

            changedPinNames.add(pin.name);
            const updatedPin = { ...pin, valueType: newValueType, countType: newCountType };
            return {
              ...updatedPin,
              binding: { type: 'const' as const, value: getDefaultValue(updatedPin.valueType, updatedPin.countType === 'list') },
              vectorIndex: undefined
            } as Pin;
          });
        }
      }

      // 4. 更新节点 Pins
      const newNodes = new Map(tree.nodes);
      newNodes.set(nodeId, { ...node, pins: newPins });
      newTree.nodes = newNodes;

      // 5. 清除失效的数据连接
      if (changedPinNames.size > 0) {
        newTree.dataConnections = tree.dataConnections.filter(dc =>
          !((dc.fromNodeId === nodeId && changedPinNames.has(dc.fromPinName)) ||
            (dc.toNodeId === nodeId && changedPinNames.has(dc.toPinName)))
        );
      }

      return newTree;
    });

    return result || state;
  }),


  // 新特性操作
  toggleNodeFold: (nodeId) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const node = tree.nodes.get(nodeId);
      if (!node) return tree;

      const newFolded = !node.isFolded;
      const newNodes = new Map(tree.nodes);
      newNodes.set(nodeId, { ...node, isFolded: newFolded });

      // 保存到本地元数据
      if (state.activeFilePath) {
        useEditorMetaStore.getState().setNodeFolded(state.activeFilePath, nodeId, newFolded);
      }

      return { ...tree, nodes: newNodes };
    }, { skipHistory: true, isUIChange: true });
    return result || state;
  }),

  toggleNodeDisabled: (nodeId) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const node = tree.nodes.get(nodeId);
      if (!node) return tree;

      const newDisabled = !node.disabled;
      const newNodes = new Map(tree.nodes);
      newNodes.set(nodeId, { ...node, disabled: newDisabled });

      return { ...tree, nodes: newNodes };
    });
    return result || state;
  }),

  toggleConditionConnector: (nodeId) => set((state) => {
    const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
      const node = tree.nodes.get(nodeId);
      if (!node) return tree;

      const hasCondition = !node.hasConditionConnector;
      const newNodes = new Map(tree.nodes);
      newNodes.set(nodeId, { ...node, hasConditionConnector: hasCondition });

      let newConnections = [...tree.connections];
      if (!hasCondition) {
        newConnections = newConnections.filter(c =>
          !(c.parentNodeId === nodeId && c.parentConnector === 'condition')
        );
      }

      return { ...tree, nodes: newNodes, connections: newConnections };
    });
    return result || state;
  }),

  // 新建文件
  createNewTree: (name) => set((state) => {
    let uniqueName = name;
    let counter = 1;
    while (state.openedFiles.some(f => f.path === `${uniqueName}.tree`)) {
      uniqueName = `${name}${counter}`;
      counter++;
    }
    const path = `${uniqueName}.tree`;

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
      lastSavedTreeSnapshot: serializeTreeForEditor(newTree),
      isDirty: false,
      isNew: true,
      history: { past: [], future: [] }
    };

    return {
      openedFiles: [...state.openedFiles, newFile],
      activeFilePath: path,
    };
  }),
}));
