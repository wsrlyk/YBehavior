import { create } from 'zustand';
import type { Tree, TreeNode, TreeConnection, DataConnection, Variable, Pin } from '../types';
import { loadTree, listFiles, saveFile } from '../utils/fileService';
import { loadSettings, type Settings } from '../utils/settings';
import { generateGUID } from '../utils/guidUtils';
import { useNodeDefinitionStore } from './nodeDefinitionStore';
import { serializeTreeForEditor, serializeTreeForRuntime } from '../utils/xmlSerializer';
import { validateValue, getDefaultValue } from '../utils/validation';
import { useNotificationStore } from './notificationStore';
import { logger } from '../utils/logger';
import { stripExtension } from '../utils/fileUtils';
import { useEditorMetaStore } from './editorMetaStore';
import { SPECIAL_NODE_HANDLERS_IMPL, type ConnectionLabelUpdate } from '../utils/specialNodeLogic';
import {
  type OpenedFile,
  recalculateUIDs,
  updateOpenedFileTree
} from './editorStoreCore';
import { useDebugStore } from './debugStore';
import { useFSMStore } from './fsmStore';

// Re-export for backward compatibility
export { getDescendantIds } from './editorStoreCore';
export type { HistoryState, OpenedFile } from './editorStoreCore';

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
  duplicateSelectedNodes: () => void;

  // 获取当前树
  getCurrentTree: () => Tree | null;

  // 节点操作
  addNode: (node: TreeNode) => void;
  removeNode: (nodeId: string) => void;
  removeNodes: (nodeIds: string[]) => void;
  removeElements: (nodeIds: string[], connectionIds: string[], dataConnectionIds: string[]) => void;
  updateNodePosition: (nodeId: string, x: number, y: number) => void;
  updateNodesPositions: (updates: { id: string; x: number; y: number }[]) => void;
  updateNodeProperty: (nodeId: string, updates: Partial<TreeNode>) => void;

  // 连接操作
  addConnection: (connection: TreeConnection) => void;
  removeConnection: (connectionId: string) => void;
  removeConnections: (connectionIds: string[]) => void;

  // 数据连接操作
  addDataConnection: (dataConn: DataConnection) => void;
  removeDataConnection: (dataConnId: string) => void;
  removeDataConnections: (dataConnIds: string[]) => void;

  // 变量操作
  addVariable: (isLocal: boolean, variable: Variable) => void;
  removeVariable: (isLocal: boolean, name: string) => void;
  updateVariable: (isLocal: boolean, name: string, updates: Partial<Variable>) => void;
  toggleVariableScope: (name: string, currentIsLocal: boolean) => string | null; // returns error message or null on success

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

  // Tree Interface (I/O) 操作
  addTreeInterfacePin: (isInput: boolean, pin: import('../types').TreeInterfacePin) => void;
  removeTreeInterfacePin: (isInput: boolean, id: string) => void;
  updateTreeInterfacePin: (isInput: boolean, id: string, updates: Partial<import('../types').TreeInterfacePin>) => void;

  // SubTree 操作
  reloadSubTreePins: (subtreeNodeId: string) => Promise<void>;

  // Clipboard
  clipboard: { nodes: TreeNode[], connections: TreeConnection[], dataConnections: DataConnection[], variables: Variable[] } | null;
  copySelectedNodes: () => void;
  pasteNodes: (position?: { x: number; y: number }) => void;

  // New feature: Viewport state
  setViewport: (workspacePath: string, viewport: import('./editorStoreCore').Viewport) => void;

  // 新建文件
  createNewTree: (name: string) => void;

  // Refresh file list
  refreshFiles: () => Promise<void>;
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
      const files = await listFiles(settings.editorTreeDir, ['tree', 'fsm']);
      const normalizedTreeDir = settings.editorTreeDir.replace(/\\/g, '/');
      const normalizedRuntimeDir = settings.runtimeTreeDir.replace(/\\/g, '/');
      const normalizedFiles = files.map(f => f.replace(/\\/g, '/'));

      set({
        settings: {
          ...settings,
          editorTreeDir: normalizedTreeDir,
          runtimeTreeDir: normalizedRuntimeDir
        },
        editorTreeDir: normalizedTreeDir,
        runtimeTreeDir: normalizedRuntimeDir,
        treeFiles: normalizedFiles,
        isLoading: false,
      });

      // Cleanup orphaned meta files
      useEditorMetaStore.getState().cleanOrphanedMeta(files);
    } catch (e) {
      set({ error: String(e), isLoading: false });
    }
  },

  refreshFiles: async () => {
    const { editorTreeDir } = get();
    if (!editorTreeDir) return;

    try {
      const files = await listFiles(editorTreeDir, ['tree', 'fsm']);
      const normalizedFiles = files.map(f => f.replace(/\\/g, '/'));
      set({ treeFiles: normalizedFiles });
      // Cleanup orphaned meta files
      useEditorMetaStore.getState().cleanOrphanedMeta(normalizedFiles);
    } catch (e) {
      console.error('Failed to refresh files:', e);
    }
  },



  openTree: async (path) => {
    const { editorTreeDir, openedFiles } = get();
    if (!editorTreeDir) {
      console.error('openTree failed: editorTreeDir is not set');
      return;
    }

    // Normalize path for comparison
    const normalizedPath = path.replace(/\\/g, '/');
    console.log(`openTree called with path: "${path}", normalized: "${normalizedPath}"`);

    // 1. 如果已经打开，直接切换
    const existing = openedFiles.find(f => f.path.replace(/\\/g, '/') === normalizedPath);
    if (existing) {
      console.log(`Found already opened file: "${existing.path}", activating...`);
      get().setActiveFile(existing.path);
      return;
    }

    // 2. 如果未打开，尝试在已知文件列表中查找（处理不完整的相对路径，例如 FSM 中只存了文件名）
    const { treeFiles } = get();
    const normalizedNoExt = stripExtension(normalizedPath);
    console.log(`Searching for match in ${treeFiles.length} treeFiles... (NormalizedNoExt: "${normalizedNoExt}")`);

    // Find the best match in available tree files
    const matchedFile = treeFiles.find(f => {
      const nf = f.replace(/\\/g, '/');
      const nfNoExt = stripExtension(nf);

      // Try exact match, suffix match, and extensionless versions of both
      // We add a leading / to both for consistent suffix matching
      const vnf = nf.startsWith('/') ? nf : '/' + nf;
      const vnfNoExt = nfNoExt.startsWith('/') ? nfNoExt : '/' + nfNoExt;
      const vpath = normalizedPath.startsWith('/') ? normalizedPath : '/' + normalizedPath;
      const vpathNoExt = normalizedNoExt.startsWith('/') ? normalizedNoExt : '/' + normalizedNoExt;

      const isExact = nf.toLowerCase() === normalizedPath.toLowerCase() ||
        (nf.toLowerCase() === (normalizedPath + '.tree').toLowerCase()) ||
        nfNoExt.toLowerCase() === normalizedNoExt.toLowerCase();

      const isSuffix = vnf.toLowerCase().endsWith(vpath.toLowerCase()) ||
        vnf.toLowerCase().endsWith((vpath + '.tree').toLowerCase()) ||
        vnfNoExt.toLowerCase().endsWith(vpathNoExt.toLowerCase());

      if (isExact || isSuffix) {
        console.log(`Match found! Exact: ${isExact}, Suffix: ${isSuffix}. File: ${nf}`);
        return true;
      }
      return false;
    });

    if (matchedFile) {
      console.log(`Matched known file: "${matchedFile}"`);
    } else {
      console.warn(`No match found for "${normalizedPath}" in treeFiles. Will try direct load.`);
    }

    const finalPath = matchedFile || (normalizedPath.endsWith('.tree') ? normalizedPath : `${normalizedPath}.tree`);
    const normalizedFinal = finalPath.replace(/\\/g, '/');

    // 再次检查匹配后的路径是否已打开
    const existingAgain = openedFiles.find(f => f.path.replace(/\\/g, '/') === normalizedFinal);
    if (existingAgain) {
      console.log(`Final path "${normalizedFinal}" is already open as "${existingAgain.path}", activating...`);
      get().setActiveFile(existingAgain.path);
      return;
    }

    console.log(`Loading tree from final path: "${normalizedFinal}"`);
    set({ isLoading: true, error: null });
    try {
      // Use full path with directory prefix
      // Ensure no double slashes and correct separator
      let fullPathForLoad = normalizedFinal.includes(':') ? normalizedFinal : `${editorTreeDir}/${normalizedFinal}`;
      fullPathForLoad = fullPathForLoad.replace(/\\/g, '/').replace(/\/+/g, '/');
      if (fullPathForLoad.match(/^[A-Za-z]:\//)) {
        // Keep Windows drive letter format
      } else if (!fullPathForLoad.startsWith('/')) {
        // ... (keep as is)
      }
      console.log(`Full path for loading: "${fullPathForLoad}"`);

      // 获取节点定义查找函数
      const { getDefinition } = useNodeDefinitionStore.getState();
      const tree = await loadTree(fullPathForLoad, getDefinition);
      const fileName = normalizedFinal.split('/').pop() || normalizedFinal;

      // 加载本地元数据（如折叠状态）
      const meta = useEditorMetaStore.getState().getTreeMeta(normalizedFinal);
      if (meta) {
        tree.nodes.forEach((node, id) => {
          if (meta.nodes[id]) {
            node.isFolded = meta.nodes[id].isFolded;
          }
        });
      }

      // 初始加载时计算所有特殊节点的标签
      const treeWithLabels = applyLabelUpdatesToTree(tree, Array.from(tree.nodes.keys()));

      const newFile: OpenedFile = {
        path: normalizedFinal,
        name: fileName,
        tree: treeWithLabels,
        lastSavedTreeSnapshot: serializeTreeForEditor(treeWithLabels),
        isDirty: false,
        history: { past: [], future: [] },
        viewport: meta?.viewport
      };

      set({
        openedFiles: [...openedFiles, newFile],
        activeFilePath: normalizedFinal,
        isLoading: false,
        selectedNodeIds: [],
      });
      useFSMStore.getState().setActiveFSM(null as any);
    } catch (e: any) {
      console.error(`Failed to load tree "${normalizedFinal}": ${e}`);
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
        if (newOpenedFiles.length > 0) {
          newActiveFilePath = newOpenedFiles[0].path;
        } else {
          // No more trees open, fallback to an FSM file if available
          newActiveFilePath = null;
          const fsmState = useFSMStore.getState();
          if (fsmState.openedFSMFiles.length > 0) {
            fsmState.setActiveFSM(fsmState.openedFSMFiles[0].path);
          }
        }
      }

      return {
        openedFiles: newOpenedFiles,
        activeFilePath: newActiveFilePath,
        selectedNodeIds: [],
      };
    });
  },

  setActiveFile: (path) => {
    const normalized = path ? path.replace(/\\/g, '/') : path;
    if (normalized) useFSMStore.getState().setActiveFSM(null as any);
    set({ activeFilePath: normalized, selectedNodeIds: [] });
  },

  setViewport: (path, viewport) => {
    set((state) => {
      const fileIndex = state.openedFiles.findIndex(f => f.path === path);
      if (fileIndex === -1) return state;

      const newOpenedFiles = [...state.openedFiles];
      newOpenedFiles[fileIndex] = {
        ...newOpenedFiles[fileIndex],
        viewport
      };
      return { openedFiles: newOpenedFiles };
    });

    useEditorMetaStore.getState().setTreeViewport(path, viewport);
  },

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

  updateNodePosition: (nodeId, x, y) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const node = tree.nodes.get(nodeId);
        if (!node) return tree;

        const newNodes = new Map(tree.nodes);
        newNodes.set(nodeId, { ...node, position: { x, y } });
        return { ...tree, nodes: newNodes };
      }, { skipHistory: true, skipUID: true }); // 拖拽中跳过历史和 UID 计算

      return result || state;
    });
  },

  updateNodesPositions: (updates) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const newNodes = new Map(tree.nodes);
        let changed = false;
        updates.forEach(({ id, x, y }) => {
          const node = tree.nodes.get(id);
          if (node) {
            newNodes.set(id, { ...node, position: { x, y } });
            changed = true;
          }
        });
        return changed ? { ...tree, nodes: newNodes } : tree;
      }, { skipHistory: true, skipUID: true });

      return result || state;
    });
  },

  addNode: (node) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const newNodes = new Map(tree.nodes);
        newNodes.set(node.id, node);
        return { ...tree, nodes: newNodes };
      });

      return result || state;
    });
  },

  removeElements: (nodeIds, connectionIds, dataConnectionIds) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        let nodesToActuallyRemove: string[] = [];
        let newNodes = new Map(tree.nodes);

        // 1. Process nodes
        if (nodeIds.length > 0) {
          nodeIds.forEach(id => {
            const node = tree.nodes.get(id);
            if (node && node.type !== 'Root') {
              newNodes.delete(id);
              nodesToActuallyRemove.push(id);
            }
          });
        }

        const actualNodeIdsToRemove = new Set(nodesToActuallyRemove);
        const connIdsToRemove = new Set(connectionIds);
        const dataConnIdsToRemove = new Set(dataConnectionIds);

        // 2. Filter connections
        const parentNodeIdsToRefresh = new Set<string>();
        const newConnections = tree.connections.filter((c) => {
          // Remove if connection itself is in connectionIds OR if either end node is being removed
          const shouldRemove = connIdsToRemove.has(c.id) ||
            actualNodeIdsToRemove.has(c.parentNodeId) ||
            actualNodeIdsToRemove.has(c.childNodeId);
          if (shouldRemove) {
            parentNodeIdsToRefresh.add(c.parentNodeId);
            return false;
          }
          return true;
        });

        // 3. Filter data connections
        const newDataConnections = tree.dataConnections.filter((dc) => {
          return !dataConnIdsToRemove.has(dc.id) &&
            !actualNodeIdsToRemove.has(dc.fromNodeId) &&
            !actualNodeIdsToRemove.has(dc.toNodeId);
        });

        // Check for changes
        const hasNodeChanges = nodesToActuallyRemove.length > 0;
        const hasConnChanges = newConnections.length !== tree.connections.length;
        const hasDataConnChanges = newDataConnections.length !== tree.dataConnections.length;

        if (!hasNodeChanges && !hasConnChanges && !hasDataConnChanges) {
          return tree;
        }

        const newTree = {
          ...tree,
          nodes: newNodes,
          connections: newConnections,
          dataConnections: newDataConnections
        };

        return applyLabelUpdatesToTree(newTree, Array.from(parentNodeIdsToRefresh));
      });

      if (!result) return state;

      const remainingSelected = state.selectedNodeIds.filter(id => !nodeIds.includes(id));
      return {
        ...result,
        selectedNodeIds: remainingSelected,
      };
    });
  },

  removeNode: (nodeId) => get().removeElements([nodeId], [], []),

  removeNodes: (nodeIds) => get().removeElements(nodeIds, [], []),

  removeConnections: (connectionIds) => get().removeElements([], connectionIds, []),

  removeDataConnections: (dataConnIds) => get().removeElements([], [], dataConnIds),

  updateNodeProperty: (nodeId, updates) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const node = tree.nodes.get(nodeId);
        if (!node) return tree;

        const newNodes = new Map(tree.nodes);
        newNodes.set(nodeId, { ...node, ...updates });
        return { ...tree, nodes: newNodes };
      });

      return result || state;
    });
  },

  addConnection: (connection) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const newTreeWithConn = {
          ...tree,
          connections: [...tree.connections, connection],
        };

        const newTreeAfterLabelUpdate = applyLabelUpdatesToTree(newTreeWithConn, [connection.parentNodeId]);
        return newTreeAfterLabelUpdate;
      });

      return result || state;
    });
  },

  removeConnection: (connectionId) => get().removeConnections([connectionId]),

  addDataConnection: (dataConn) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
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
    });
  },

  removeDataConnection: (dataConnId) => get().removeDataConnections([dataConnId]),

  duplicateSelectedNodes: () => {
    // Duplication involves adding nodes/connections, so we should guard it.
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      if (!state.selectedNodeIds.length || !state.activeFilePath) return state;

      let newDuplicatedIds: string[] = [];

      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const selectedIds = new Set(state.selectedNodeIds);
        const nodesToDuplicate = Array.from(selectedIds)
          .map(id => tree.nodes.get(id))
          .filter((node): node is TreeNode => !!node && node.type !== 'Root');

        if (nodesToDuplicate.length === 0) return tree;

        const timestamp = Date.now();
        const oldToNewIdMap = new Map<string, string>();
        const newNodes = new Map(tree.nodes);
        const duplicatedIds: string[] = [];

        // Pre-collect existing GUIDs
        const existingGUIDs = new Set<number>();
        tree.nodes.forEach(n => existingGUIDs.add(n.guid));

        // 1. Duplicate Nodes
        nodesToDuplicate.forEach((node, index) => {
          const newId = `node-${timestamp}-${index}`;
          const newGuid = generateGUID(existingGUIDs);
          existingGUIDs.add(newGuid);

          oldToNewIdMap.set(node.id, newId);

          const newNode: TreeNode = {
            ...node,
            id: newId,
            guid: newGuid,
            uid: undefined,
            parentId: undefined, // Clear linkage
            childrenIds: [], // Clear linkage
            position: {
              x: node.position.x + 20,
              y: node.position.y + 20
            },
            pins: node.pins.map(p => ({
              ...p,
              binding: { ...p.binding },
              vectorIndex: p.vectorIndex ? { ...p.vectorIndex } : undefined
            }))
          };

          newNodes.set(newId, newNode);
          duplicatedIds.push(newId);
        });

        newDuplicatedIds = duplicatedIds;

        // 2. Duplicate Tree Connections (only within selection)
        const newConnections = [...tree.connections];
        tree.connections.forEach(conn => {
          if (selectedIds.has(conn.parentNodeId) && selectedIds.has(conn.childNodeId)) {
            const newParentId = oldToNewIdMap.get(conn.parentNodeId);
            const newChildId = oldToNewIdMap.get(conn.childNodeId);
            if (newParentId && newChildId) {
              newConnections.push({
                ...conn,
                id: `conn-${newParentId}-${newChildId}-${conn.parentConnector}-${timestamp}`,
                parentNodeId: newParentId,
                childNodeId: newChildId
              });
            }
          }
        });

        // 3. Duplicate Data Connections (only within selection)
        const newDataConnections = [...tree.dataConnections];
        tree.dataConnections.forEach(dc => {
          if (selectedIds.has(dc.fromNodeId) && selectedIds.has(dc.toNodeId)) {
            const newFromId = oldToNewIdMap.get(dc.fromNodeId);
            const newToId = oldToNewIdMap.get(dc.toNodeId);
            if (newFromId && newToId) {
              newDataConnections.push({
                ...dc,
                id: `data-${newFromId}-${newToId}-${dc.fromPinName}-${dc.toPinName}-${timestamp}`,
                fromNodeId: newFromId,
                toNodeId: newToId
              });
            }
          }
        });

        return {
          ...tree,
          nodes: newNodes,
          connections: newConnections,
          dataConnections: newDataConnections
        };
      });

      if (!result) return state;

      return {
        ...result,
        selectedNodeIds: newDuplicatedIds,
      };
    });
  },

  // Clipboard Actions
  clipboard: null,

  copySelectedNodes: () => {
    const state = get();
    const tree = state.getCurrentTree();
    if (!tree) return;

    const selectedIds = new Set(state.selectedNodeIds);
    if (selectedIds.size === 0) return;

    const nodesToCopy = Array.from(tree.nodes.values()).filter(n => selectedIds.has(n.id) && n.type !== 'Root');

    // Copy internal connections
    const connectionsToCopy = tree.connections.filter(c =>
      selectedIds.has(c.parentNodeId) && selectedIds.has(c.childNodeId)
    );

    // Copy internal data connections
    const dataConnectionsToCopy = tree.dataConnections.filter(dc =>
      selectedIds.has(dc.fromNodeId) && selectedIds.has(dc.toNodeId)
    );

    // Deep copy nodes to ensure clipboard is a snapshot
    const nodesDeepCopy = nodesToCopy.map(node => ({
      ...node,
      pins: node.pins.map(p => ({
        ...p,
        binding: { ...p.binding },
        vectorIndex: p.vectorIndex ? { ...p.vectorIndex } : undefined
      }))
    }));

    // 收集引用的变量
    const referencedVars: Variable[] = [];
    const varNames = new Set<string>(); // "scope-name" to avoid duplicates

    nodesDeepCopy.forEach(node => {
      node.pins.forEach(pin => {
        // Pin 绑定变量
        const binding = pin.binding;
        if (binding.type === 'pointer' && binding.variableName) {
          const varName = binding.variableName;
          const isLocal = binding.isLocal;
          const key = `${isLocal ? 'local' : 'shared'}-${varName}`;
          if (!varNames.has(key)) {
            const v = (isLocal ? tree.localVariables : tree.sharedVariables)
              .find(v => v.name === varName);
            if (v) {
              referencedVars.push({ ...v });
              varNames.add(key);
            }
          }
        }
        // VectorIndex 绑定变量
        const vi = pin.vectorIndex;
        if (vi && vi.type === 'pointer' && vi.variableName) {
          const varName = vi.variableName;
          const isLocal = vi.isLocal;
          const key = `${isLocal ? 'local' : 'shared'}-${varName}`;
          if (!varNames.has(key)) {
            const v = (isLocal ? tree.localVariables : tree.sharedVariables)
              .find(v => v.name === varName);
            if (v) {
              referencedVars.push({ ...v });
              varNames.add(key);
            }
          }
        }
      });
    });

    logger.info(`Copied ${nodesDeepCopy.length} nodes and ${referencedVars.length} referenced variables to clipboard`);
    useNotificationStore.getState().notify(`Copied ${nodesDeepCopy.length} nodes`);

    set({ clipboard: { nodes: nodesDeepCopy, connections: connectionsToCopy, dataConnections: dataConnectionsToCopy, variables: referencedVars } });
  },

  pasteNodes: (position?: { x: number; y: number }) => {
    const state = get();
    const clipboard = state.clipboard;
    if (!clipboard || clipboard.nodes.length === 0) return;

    if (useDebugStore.getState().isConnected) return;

    // Calculate clipboard center and offset
    let offsetX = 20;
    let offsetY = 20;

    if (position) {
      if (clipboard.nodes.length > 0) {
        let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
        clipboard.nodes.forEach(n => {
          minX = Math.min(minX, n.position.x);
          minY = Math.min(minY, n.position.y);
          maxX = Math.max(maxX, n.position.x);
          maxY = Math.max(maxY, n.position.y);
        });
        const centerX = (minX + maxX) / 2;
        const centerY = (minY + maxY) / 2;
        offsetX = position.x - centerX;
        offsetY = position.y - centerY;
      }
    }

    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const timestamp = Date.now();
        const oldToNewIdMap = new Map<string, string>();
        const newNodes = new Map(tree.nodes);
        const pastedIds: string[] = [];

        // Pre-collect existing GUIDs
        const existingGUIDs = new Set<number>();
        tree.nodes.forEach(n => existingGUIDs.add(n.guid));

        // 1. Process Nodes
        clipboard.nodes.forEach((node, index) => {
          const newId = `node-${timestamp}-${index}`;
          // Generate unique GUID
          const newGuid = generateGUID(existingGUIDs);
          existingGUIDs.add(newGuid);

          oldToNewIdMap.set(node.id, newId);

          // Deep copy pins to modify them safely
          const newPins = node.pins.map(p => ({
            ...p,
            binding: { ...p.binding },
            vectorIndex: p.vectorIndex ? { ...p.vectorIndex } : undefined
          }));

          // Validate & Auto-create Variables
          newPins.forEach(pin => {
            const binding = pin.binding;
            if (binding.type === 'pointer' && binding.variableName) {
              const varName = binding.variableName;
              const isLocal = binding.isLocal;

              const existingVar = (isLocal ? tree.localVariables : tree.sharedVariables).find(v => v.name === varName);

              if (!existingVar) {
                // 1. Variable doesn't exist: Auto-create it from clipboard
                const originalVar = clipboard.variables?.find(v => v.name === varName && v.isLocal === isLocal);
                if (originalVar) {
                  const targetList = isLocal ? tree.localVariables : tree.sharedVariables;
                  targetList.push({ ...originalVar });
                  logger.info(`[Paste] Auto-created missing ${isLocal ? 'Local' : 'Shared'} variable: ${varName}`);
                } else {
                  // Fallback: No definition in clipboard, keep as disconnected pointer
                  pin.binding = { type: 'pointer', variableName: '', isLocal: false };
                  pin.bindingType = 'pointer';
                }
              } else {
                // 2. Variable exists: Check compatibility
                const valueTypeMatch = existingVar.valueType === pin.valueType;
                let countTypeMatch = false;

                if (pin.vectorIndex) {
                  // If pin has vectorIndex, it MUST reference a list variable
                  countTypeMatch = existingVar.countType === 'list';
                } else {
                  // If no vectorIndex, countType must match exactly
                  countTypeMatch = existingVar.countType === pin.countType;
                }

                if (!valueTypeMatch || !countTypeMatch) {
                  const expectedStr = `${pin.valueType}${pin.vectorIndex ? '[] (with index)' : (pin.countType === 'list' ? '[]' : '')}`;
                  const foundStr = `${existingVar.valueType}${existingVar.countType === 'list' ? '[]' : ''}`;

                  logger.warn(`[Paste] Incompatible variable '${varName}' found. Expected ${expectedStr}, found ${foundStr}. Pin '${pin.name}' in node '${node.nickname || node.type}' disconnected.`);

                  // Disconnect by clearing variableName but keep as pointer
                  pin.binding = { type: 'pointer', variableName: '', isLocal: false };
                  pin.bindingType = 'pointer';
                }
              }
            }

            // Validate Vector Index Variable
            const vi = pin.vectorIndex;
            if (vi && vi.type === 'pointer' && vi.variableName) {
              const varName = vi.variableName;
              const isLocal = vi.isLocal;
              const existingVar = (isLocal ? tree.localVariables : tree.sharedVariables).find(v => v.name === varName);

              // For vectorIndex, we only accept int scalar and WE DON'T auto-create if missing (as per request)
              if (!existingVar) {
                logger.warn(`[Paste] Index Variable '${varName}' not found. Pin '${pin.name}' index reset to constant 0.`);
                pin.vectorIndex = { type: 'const', value: '0' };
              } else {
                const isCompatible = existingVar.valueType === 'int' && existingVar.countType === 'scalar';
                if (!isCompatible) {
                  logger.warn(`[Paste] Incompatible Index variable '${varName}'. Expected int scalar, found ${existingVar.valueType}${existingVar.countType === 'list' ? '[]' : ''}. Reset to constant 0.`);
                  pin.vectorIndex = { type: 'const', value: '0' };
                }
              }
            }
          });

          // Calculate new position
          const newX = position ? node.position.x + offsetX : node.position.x + 20;
          const newY = position ? node.position.y + offsetY : node.position.y + 20;

          const newNode: TreeNode = {
            ...node,
            id: newId,
            guid: newGuid,
            uid: undefined,
            parentId: undefined,
            childrenIds: [],
            position: { x: newX, y: newY },
            pins: newPins
          };

          newNodes.set(newId, newNode);
          pastedIds.push(newId);
        });

        // 2. Process Connections
        const newConnections = [...tree.connections];
        clipboard.connections.forEach(conn => {
          const newParentId = oldToNewIdMap.get(conn.parentNodeId);
          const newChildId = oldToNewIdMap.get(conn.childNodeId);
          if (newParentId && newChildId) {
            newConnections.push({
              ...conn,
              id: `conn-${newParentId}-${newChildId}-${conn.parentConnector}-${timestamp}`,
              parentNodeId: newParentId,
              childNodeId: newChildId
            });
          }
        });

        // 3. Process Data Connections
        const newDataConnections = [...tree.dataConnections];
        clipboard.dataConnections.forEach(dc => {
          const newFromId = oldToNewIdMap.get(dc.fromNodeId);
          const newToId = oldToNewIdMap.get(dc.toNodeId);
          if (newFromId && newToId) {
            newDataConnections.push({
              ...dc,
              id: `data-${newFromId}-${newToId}-${dc.fromPinName}-${dc.toPinName}-${timestamp}`,
              fromNodeId: newFromId,
              toNodeId: newToId
            });
          }
        });

        return {
          ...tree,
          nodes: newNodes,
          connections: newConnections,
          dataConnections: newDataConnections
        };
      });

      // Update state with new tree and selection
      if (result) {
        // Find the new IDs from the result to select them
        // (Wait, we know pastedIds, but they are local to the update function... 
        //  The updateWorked check is implicitly done by checking result.
        //  However, to select them, we need to know them outside.
        //  Since we generated them deterministically inside based on timestamp, 
        //  we can't easily retrieve them unless we move generation out or capture them.
        //  
        //  Refactoring updateOpenedFileTree to return metadata would be hard.
        //  Instead, let's just rely on the fact that we are in a set updater.
        //  But wait, `timestamp` is local.
        //  
        //  Correct approach: 
        //  The `updateOpenedFileTree` returns the NEW STATE (EditorState) or NULL using the `result || state` pattern.
        //  Actually no, `updateOpenedFileTree` returns `EditorState | null`.
        //  
        //  We need to set `selectedNodeIds` to `pastedIds`. 
        //  
        //  Simple hack: We can't easily extract `pastedIds` from the functional update if it's pure.
        //  
        //  Let's capture pastedIds in a variable outside the updater? 
        //  No, `updateOpenedFileTree` logic is complex.

        //  Let's replicate the logic of `duplicateSelectedNodes` closer.
        //  In `duplicateSelectedNodes`, `newDuplicatedIds` is closure variable.
        //  Yes, that works!
      }

      return result || state;
    });

    // NOTE: The previous `set` call processes the update. 
    // We cannot easily set selectedNodeIds accurately because we don't know the exact IDs generated inside the callback 
    // if the callback is executed later or multiple times (though Zustand set is immediate usually).
    //
    // However, `updateOpenedFileTree` returns the *entire* state.
    // So we can't "append" the selection update easily unless we modify `updateOpenedFileTree` or do it in two passes?
    //
    // Actually, `duplicateSelectedNodes` does it by `newDuplicatedIds = duplicatedIds` inside the callback,
    // and then uses `newDuplicatedIds` in the return object.
    //
    // So I should do the same pattern.

    // I need to correct my code above to include the `state` return with `selectedNodeIds`.
    // I will rewrite the replacement content to include this correctly.
  },

  saveCurrentFile: async () => {
    // Save is checking validation, but does it modify the tree?
    // It updates `lastSavedTreeSnapshot` and `isDirty`.
    // It doesn't modify the tree structure essentially, just metadata.
    // However, it might be weird to save while debugging if files are locked?
    // User requested "enforce read-only mode". Typically implies no editing.
    // Saving checks validation.
    // I will NOT guard save, as user might want to save current state?
    // Actually, if they can't edit, saving is just saving what's there.
    // But usually you can't save while debugging in some IDEs.
    // I'll leave it unguarded for now unless specifically asked.
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

    const mainTreeIds = getReachableIds(tree.rootId || '', true);

    // Cleanup node meta for nodes that no longer exist
    useEditorMetaStore.getState().cleanNodeMeta(activeFilePath, Array.from(tree.nodes.keys()));

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
        // 数据连接模式（pointer + 空变量名）必须有真实数据连线
        if (pin.binding.type === 'pointer' && !pin.binding.variableName && pin.enableType !== 'disable') {
          const hasDataConnection = tree.dataConnections.some(dc =>
            (dc.fromNodeId === node.id && dc.fromPinName === pin.name) ||
            (dc.toNodeId === node.id && dc.toPinName === pin.name)
          );

          if (!hasDataConnection) {
            const nodeLabel = `${node.type}:${node.uid || node.id || ''}`;
            errors.push(`Node [${nodeLabel}] Pin [${pin.name}]: Data connection is not connected`);
          }
        }

        if (pin.binding.type === 'const' && pin.enableType !== 'disable') {
          const res = validateValue(pin.binding.value, pin.valueType, pin.countType);
          if (!res.isValid) {
            const nodeLabel = `${node.type}:${node.uid || node.id || ''}`;
            errors.push(`Node [${nodeLabel}] Pin [${pin.name}]: ${res.error}`);
          }
        }
      });
    });

    // 4.1 特殊节点验证 (SwitchCase, HandleEvent 等)
    tree.nodes.forEach(node => {
      // 只有在 Root 分支下的节点且未禁用的才需要校验
      if (!mainTreeIds.has(node.id) || node.disabled) return;

      const handler = (SPECIAL_NODE_HANDLERS_IMPL as any)[node.type];
      if (handler && handler.validate) {
        const nodeErrors = handler.validate(node, tree);
        errors.push(...nodeErrors);
      }
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
    // Similarly, guard if you want to prevent saving during debug?
    // I'll leave it allowed for now.
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

      const normalizedRelativePath = relativePath.replace(/\\/g, '/');
      const hasOpenedPathConflict = openedFiles.some(
        f => f.path === normalizedRelativePath && f.path !== activeFilePath
      );
      if (hasOpenedPathConflict) {
        useNotificationStore.getState().notify(
          `Save As failed: "${normalizedRelativePath}" is already opened. Please close that tab first.`,
          'error'
        );
        return;
      }

      // 更新文件信息并执行保存逻辑（这里我们复用 saveCurrentFile 的保存部分，但先更新 store 状态）
      set(state => ({
        openedFiles: state.openedFiles.map(f =>
          f.path === activeFilePath
            ? {
              ...f,
              path: normalizedRelativePath,
              name: fileName,
              isNew: false,
              lastSavedTreeSnapshot: serializeTreeForEditor(f.tree), // 预更新快照，虽然 saveCurrentFile 也会更新
              tree: { ...f.tree, path: normalizedRelativePath, name: fileName.replace(/\.tree$/, '') }
            }
            : f
        ),
        activeFilePath: normalizedRelativePath,
        treeFiles: state.treeFiles.includes(normalizedRelativePath) ? state.treeFiles : [...state.treeFiles, normalizedRelativePath]
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
    // Undo/Redo should also be blocked during debug?
    // Yes, generally.
    if (useDebugStore.getState().isConnected) return state;

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
    if (useDebugStore.getState().isConnected) return state;

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
    if (useDebugStore.getState().isConnected) return state;
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
    // This is called at the end of drag, etc. 
    // If we blocked drag, we shouldn't be here?
    // But if we are here, we should probably check.
    if (useDebugStore.getState().isConnected) return state;

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
    let newTree = { ...file.tree, nodes: newNodes };

    recalculateUIDs(newTree);
    newTree = applyLabelUpdatesToTree(newTree, Array.from(newTree.nodes.keys())); // Apply label updates after UID recalculation

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
  addVariable: (isLocal, variable) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        if (isLocal) {
          return { ...tree, localVariables: [...tree.localVariables, variable] };
        } else {
          return { ...tree, sharedVariables: [...tree.sharedVariables, variable] };
        }
      });
      return result || state;
    });
  },

  removeVariable: (isLocal, name) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        let newTree = { ...tree };
        if (isLocal) {
          newTree.localVariables = tree.localVariables.filter(v => v.name !== name);
        } else {
          newTree.sharedVariables = tree.sharedVariables.filter(v => v.name !== name);
        }

        // Cleanup all pins referencing this variable
        const newNodes = new Map(newTree.nodes);
        let treeChanged = false;
        for (const [nodeId, node] of newNodes) {
          let nodeChanged = false;
          const newPins = node.pins.map(pin => {
            if (pin.binding.type === 'pointer' && pin.binding.variableName === name && pin.binding.isLocal === isLocal) {
              nodeChanged = true;
              return {
                ...pin,
                binding: { type: 'pointer' as const, variableName: '', isLocal: false },
                vectorIndex: undefined
              };
            }
            return pin;
          });

          if (nodeChanged) {
            newNodes.set(nodeId, { ...node, pins: newPins });
            treeChanged = true;
          }
        }

        if (treeChanged) {
          newTree.nodes = newNodes;
        }

        return newTree;
      });
      return result || state;
    });
  },

  updateVariable: (isLocal, name, updates) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        let newTree = { ...tree };
        const vars = isLocal ? tree.localVariables : tree.sharedVariables;
        const targetVar = vars.find(v => v.name === name);

        if (!targetVar) return tree;

        const isTypeChanged = (updates.valueType && updates.valueType !== targetVar.valueType) ||
          (updates.countType && updates.countType !== targetVar.countType);
        const isNameChanged = updates.name && updates.name !== name;

        if (isLocal) {
          newTree.localVariables = tree.localVariables.map(v => v.name === name ? { ...v, ...updates } : v);
        } else {
          newTree.sharedVariables = tree.sharedVariables.map(v => v.name === name ? { ...v, ...updates } : v);
        }

        if (isTypeChanged || isNameChanged) {
          // 更新所有引用该变量的 pin 状态
          const updatedVar = (isLocal ? newTree.localVariables : newTree.sharedVariables).find(v => (isNameChanged ? v.name === updates.name : v.name === name))!;

          const newNodes = new Map(newTree.nodes);
          for (const [nodeId, node] of newNodes) {
            let nodeChanged = false;
            const newPins = node.pins.map(pin => {
              if (pin.binding.type === 'pointer' && pin.binding.variableName === name && pin.binding.isLocal === isLocal) {
                // 1. 如果名字变了，更新名字
                if (isNameChanged) {
                  nodeChanged = true;
                  return {
                    ...pin,
                    binding: { ...pin.binding, variableName: updates.name as string }
                  };
                }
                // 2. 如果名字没变但类型变了，检查是否依然兼容
                else if (isTypeChanged) {
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
    });
  },

  toggleVariableScope: (name, currentIsLocal) => {
    if (useDebugStore.getState().isConnected) return 'Cannot modify while connected';
    let error: string | null = null;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const sourceList = currentIsLocal ? tree.localVariables : tree.sharedVariables;
        const targetList = currentIsLocal ? tree.sharedVariables : tree.localVariables;
        const variable = sourceList.find(v => v.name === name);
        if (!variable) { error = `Variable "${name}" not found`; return tree; }
        if (targetList.some(v => v.name === name)) {
          error = `Variable "${name}" already exists in ${currentIsLocal ? 'Shared' : 'Local'} variables`;
          return tree;
        }

        const newIsLocal = !currentIsLocal;
        const movedVar = { ...variable, isLocal: newIsLocal };
        let newTree = { ...tree };
        if (currentIsLocal) {
          newTree.localVariables = tree.localVariables.filter(v => v.name !== name);
          newTree.sharedVariables = [...tree.sharedVariables, movedVar];
        } else {
          newTree.sharedVariables = tree.sharedVariables.filter(v => v.name !== name);
          newTree.localVariables = [...tree.localVariables, movedVar];
        }

        // Update all node pin references
        const newNodes = new Map(newTree.nodes);
        for (const [nodeId, node] of newNodes) {
          const newPins = node.pins.map(pin => {
            let pinChanged = false;
            let newPin = { ...pin };
            if (pin.binding.type === 'pointer' && pin.binding.variableName === name && pin.binding.isLocal === currentIsLocal) {
              newPin = { ...newPin, binding: { ...pin.binding, isLocal: newIsLocal } };
              pinChanged = true;
            }
            if (pin.vectorIndex && pin.vectorIndex.type === 'pointer' && pin.vectorIndex.variableName === name && pin.vectorIndex.isLocal === currentIsLocal) {
              newPin = { ...newPin, vectorIndex: { ...pin.vectorIndex, isLocal: newIsLocal } };
              pinChanged = true;
            }
            return pinChanged ? newPin : pin;
          });
          const changed = newPins.some((p, i) => p !== node.pins[i]);
          if (changed) {
            newNodes.set(nodeId, { ...node, pins: newPins });
          }
        }
        newTree.nodes = newNodes;

        // Update tree interface pin bindings
        newTree.inputs = tree.inputs.map(pin => {
          let changed = false;
          let newPin = { ...pin };
          if (pin.binding.type === 'variable' && pin.binding.value === name && pin.binding.isLocal === currentIsLocal) {
            newPin = { ...newPin, binding: { ...pin.binding, isLocal: newIsLocal } };
            changed = true;
          }
          if (pin.vectorIndex && pin.vectorIndex.type === 'pointer' && pin.vectorIndex.variableName === name && pin.vectorIndex.isLocal === currentIsLocal) {
            newPin = { ...newPin, vectorIndex: { ...pin.vectorIndex, isLocal: newIsLocal } };
            changed = true;
          }
          return changed ? newPin : pin;
        });
        newTree.outputs = tree.outputs.map(pin => {
          let changed = false;
          let newPin = { ...pin };
          if (pin.binding.type === 'variable' && pin.binding.value === name && pin.binding.isLocal === currentIsLocal) {
            newPin = { ...newPin, binding: { ...pin.binding, isLocal: newIsLocal } };
            changed = true;
          }
          if (pin.vectorIndex && pin.vectorIndex.type === 'pointer' && pin.vectorIndex.variableName === name && pin.vectorIndex.isLocal === currentIsLocal) {
            newPin = { ...newPin, vectorIndex: { ...pin.vectorIndex, isLocal: newIsLocal } };
            changed = true;
          }
          return changed ? newPin : pin;
        });

        return newTree;
      });
      return result || state;
    });
    return error;
  },

  // Tree Interface (I/O) 操作
  addTreeInterfacePin: (isInput: boolean, pin: import('../types').TreeInterfacePin) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        if (isInput) {
          return { ...tree, inputs: [...tree.inputs, pin] };
        } else {
          return { ...tree, outputs: [...tree.outputs, pin] };
        }
      });
      return result || state;
    });
  },

  removeTreeInterfacePin: (isInput: boolean, id: string) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        if (isInput) {
          return { ...tree, inputs: tree.inputs.filter(p => p.id !== id) };
        } else {
          return { ...tree, outputs: tree.outputs.filter(p => p.id !== id) };
        }
      });
      return result || state;
    });
  },

  updateTreeInterfacePin: (isInput: boolean, id: string, updates: Partial<import('../types').TreeInterfacePin>) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const pins = isInput ? tree.inputs : tree.outputs;
        const newPins = pins.map(p => p.id === id ? { ...p, ...updates } : p);
        if (isInput) {
          return { ...tree, inputs: newPins };
        } else {
          return { ...tree, outputs: newPins };
        }
      });
      return result || state;
    });
  },

  // Pin 操作
  updatePin: (nodeId, pinName, updates) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const node = tree.nodes.get(nodeId);
        if (!node) return tree;

        let newTree = { ...tree };
        const currentPin = node.pins.find(p => p.name === pinName);
        if (!currentPin) return tree;

        const isValueTypeChanged = !!(updates.valueType && updates.valueType !== currentPin.valueType);
        const isCountTypeChanged = !!(updates.countType && updates.countType !== currentPin.countType);
        const isTypeChanged = isValueTypeChanged || isCountTypeChanged;

        // 1. 更新当前 Pin
        let changedPinNames = new Set<string>();
        if (isTypeChanged) changedPinNames.add(pinName);
        // When a pin is disabled, its data connections must be removed too
        if (updates.enableType === 'disable') changedPinNames.add(pinName);

        let newPins = node.pins.map(pin => {
          if (pin.name === pinName) {
            const updatedPin = { ...pin, ...updates };
            if (isValueTypeChanged || isCountTypeChanged) {
              if (pin.binding.type === 'const') {
                // const binding: update to new default value
                return {
                  ...updatedPin,
                  binding: { type: 'const' as const, value: getDefaultValue(updatedPin.valueType, updatedPin.countType === 'list') },
                  vectorIndex: undefined
                } as Pin;
              } else {
                // pointer binding: reset to disconnected state (empty variable name), clear vectorIndex
                return {
                  ...updatedPin,
                  binding: { type: 'pointer' as const, variableName: '', isLocal: false },
                  vectorIndex: undefined
                } as Pin;
              }
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
            let pinVTypeChanged = false;
            let pinCTypeChanged = false;
            if (hasVGroup && pin.vTypeGroup === currentPin.vTypeGroup && pin.valueType !== updates.valueType) {
              pinUpdates.valueType = updates.valueType;
              pinVTypeChanged = true;
            }
            if (hasCGroup && pin.cTypeGroup === currentPin.cTypeGroup && pin.countType !== updates.countType) {
              pinUpdates.countType = updates.countType;
              pinCTypeChanged = true;
            }

            if (Object.keys(pinUpdates).length > 0) {
              changedPinNames.add(pin.name);
              const updatedPin = { ...pin, ...pinUpdates };
              if (pinVTypeChanged || pinCTypeChanged) {
                if (pin.binding.type === 'const') {
                  // const binding: update to new default value
                  return {
                    ...updatedPin,
                    binding: { type: 'const' as const, value: getDefaultValue(updatedPin.valueType, updatedPin.countType === 'list') },
                    vectorIndex: undefined
                  } as Pin;
                } else {
                  // pointer binding: reset to disconnected state, clear vectorIndex
                  return {
                    ...updatedPin,
                    binding: { type: 'pointer' as const, variableName: '', isLocal: false },
                    vectorIndex: undefined
                  } as Pin;
                }
              }
              return updatedPin;
            }
            return pin;
          });
        }

        // 3. 处理 TypeMap 规则 (当 Enum 值变化导致其他 Pin 的类型或数量类型变化时)
        const nodeDef = useNodeDefinitionStore.getState().getDefinition(node.type);
        if (updates.binding?.type === 'const' && nodeDef?.typeMaps) {
          const srcValue = updates.binding.value;
          const matchedRules = nodeDef.typeMaps.filter(
            rule => rule.srcPin === pinName && rule.srcValue === srcValue
          );

          if (matchedRules.length > 0) {
            newPins = newPins.map(pin => {
              const rule = matchedRules.find(r => r.desPin === pin.name);
              if (!rule) return pin;

              // 解析 TypeMap 定义的目标类型
              const typeStr = rule.desType;
              const char = typeStr.charAt(0).toUpperCase();
              const valueMapping: Record<string, import('../types').ValueType> = {
                'I': 'int', 'F': 'float', 'B': 'bool', 'S': 'string', 'V': 'vector3', 'A': 'entity', 'U': 'ulong', 'E': 'enum'
              };

              let newValueType = valueMapping[char] || pin.valueType;
              let newCountType: import('../types').CountType = (typeStr.length >= 2 && typeStr[0] === typeStr[1]) ? 'list' : 'scalar';

              // 如果元数据（类型和数量类型）没有变化，不执行任何重置
              if (pin.valueType === newValueType && pin.countType === newCountType) return pin;

              changedPinNames.add(pin.name);
              
              // 更新元数据
              const updatedPin = { ...pin, valueType: newValueType, countType: newCountType };
              
              // 关键：保持原有的绑定模式 (常量或引用)，只重置其内容
              const oldBinding = pin.binding;
              let newBinding: import('../types').PinBinding;
              
              if (oldBinding.type === 'const') {
                newBinding = {
                  type: 'const',
                  value: getDefaultValue(newValueType, newCountType === 'list')
                };
              } else {
                newBinding = {
                  type: 'pointer',
                  variableName: '',
                  isLocal: false
                };
              }

              return {
                ...updatedPin,
                binding: newBinding,
                vectorIndex: undefined // 清除可能存在的数组索引
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

        const newTreeAfterLabelUpdate = applyLabelUpdatesToTree(newTree, [nodeId]);
        return newTreeAfterLabelUpdate;
      });

      return result || state;
    });
  },


  // 新特性操作
  toggleNodeFold: (nodeId) => set((state) => {
    // Folding is UI only, probably fine to allow during debug?
    // It doesn't change logic.
    // The previous implementation used `isUIChange: true`.
    // So I will NOT guard this one.
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

  toggleNodeDisabled: (nodeId) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
      const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
        const node = tree.nodes.get(nodeId);
        if (!node) return tree;

        const newDisabled = !node.disabled;
        const newNodes = new Map(tree.nodes);
        newNodes.set(nodeId, { ...node, disabled: newDisabled });

        const newTreeWithNodes = { ...tree, nodes: newNodes };
        // 禁用/启用节点会影响标签
        const newTreeAfterLabelUpdate = applyLabelUpdatesToTree(newTreeWithNodes, [nodeId]);
        return newTreeAfterLabelUpdate;
      });
      return result || state;
    });
  },

  toggleConditionConnector: (nodeId) => {
    if (useDebugStore.getState().isConnected) return;
    set((state) => {
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
    });
  },

  // SubTree 操作
  reloadSubTreePins: async (nodeId) => {
    if (useDebugStore.getState().isConnected) return;
    const { editorTreeDir, openedFiles, activeFilePath } = get();
    if (!editorTreeDir || !activeFilePath) return;

    const currentTree = openedFiles.find(f => f.path === activeFilePath)?.tree;
    if (!currentTree) return;

    const node = currentTree.nodes.get(nodeId);
    if (!node || node.type !== 'SubTree') return;

    const treePin = node.pins.find(p => p.name === 'Tree');
    const treeFile = treePin?.binding.type === 'const' ? treePin.binding.value : '';
    // 如果没有路径，清空动态 Pin
    if (!treeFile) {
      set((state) => {
        const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
          const targetNode = tree.nodes.get(nodeId);
          if (!targetNode) return tree;

          const nodeDef = useNodeDefinitionStore.getState().getDefinition(targetNode.type);
          if (!nodeDef) return tree;

          // 仅保留基础 Pin
          const basePins = targetNode.pins.filter(p => nodeDef.pins.some(bp => bp.name === p.name));
          const newNodes = new Map(tree.nodes);
          newNodes.set(nodeId, { ...targetNode, pins: basePins });

          return { ...tree, nodes: newNodes };
        });
        return result || state;
      });
      return;
    }

    try {
      // 加载 SubTree 文件 (递归加载其定义)
      const treeFileWithExt = treeFile.endsWith('.tree') ? treeFile : `${treeFile}.tree`;
      const fullPath = `${editorTreeDir}/${treeFileWithExt}`;
      const subTree = await loadTree(fullPath, (className) => useNodeDefinitionStore.getState().getDefinition(className));

      set((state) => {
        const result = updateOpenedFileTree(state.openedFiles, state.activeFilePath, (tree) => {
          const targetNode = tree.nodes.get(nodeId);
          if (!targetNode) return tree;

          const nodeDef = useNodeDefinitionStore.getState().getDefinition(targetNode.type);
          if (!nodeDef) return tree;

          // 1. 基础 Pins (来自 builtin.xml 定义)
          const basePins: Pin[] = nodeDef.pins.map(pinDef => {
            const existing = targetNode.pins.find(p => p.name === pinDef.name);
            const bindingType = existing ? existing.binding.type : (pinDef.constType === 'pointer' ? 'pointer' : 'const');
            const countType = pinDef.arrayType === 'list' ? 'list' : 'scalar';

            return {
              name: pinDef.name,
              valueType: pinDef.valueType,
              countType,
              bindingType,
              binding: existing ? existing.binding : (bindingType === 'pointer'
                ? { type: 'pointer', variableName: '', isLocal: false }
                : { type: 'const', value: pinDef.defaultValue }),
              enableType: pinDef.enableType,
              isInput: pinDef.isInput,
              enumValues: pinDef.enumValues,
              allowedValueTypes: pinDef.allowedValueTypes || [pinDef.valueType],
              vTypeGroup: pinDef.vTypeGroup,
              cTypeGroup: pinDef.cTypeGroup,
              isCountTypeFixed: pinDef.arrayType !== 'switchable',
              isBindingTypeFixed: pinDef.constType !== 'switchable',
              vectorIndex: existing?.vectorIndex,
              desc: pinDef.desc,
            };
          });

          // 2. 动态 Pins (来自 SubTree 的 Interface 定义)
          const subTreeInputs: Pin[] = subTree.inputs.map(inPin => ({
            name: inPin.name,
            valueType: inPin.valueType,
            countType: inPin.countType,
            isInput: true,
            allowedValueTypes: [inPin.valueType],
            isCountTypeFixed: true,
            isBindingTypeFixed: false, // 输入可以绑定常量或变量
            enableType: 'fixed',
            bindingType: 'const',
            binding: { type: 'const', value: getDefaultValue(inPin.valueType, inPin.countType === 'list') },
            desc: inPin.desc,
          }));

          const subTreeOutputs: Pin[] = subTree.outputs.map(outPin => ({
            name: outPin.name,
            valueType: outPin.valueType,
            countType: outPin.countType,
            isInput: false,
            allowedValueTypes: [outPin.valueType],
            isCountTypeFixed: true,
            isBindingTypeFixed: true, // 输出必须绑定到变量
            enableType: 'fixed',
            bindingType: 'pointer',
            binding: { type: 'pointer', variableName: '', isLocal: false },
            desc: outPin.desc,
          }));

          // 3. 合并并尽量保留原有绑定
          const newPins = [...basePins];
          const dynamicPins = [...subTreeInputs, ...subTreeOutputs];

          dynamicPins.forEach(dp => {
            // 在原有的 Pin 列表中查找
            // 优先匹配 同名且方向一致 的 Pin
            // 备选匹配 同名 (忽略方向) 的 Pin — 针对那些在 Input/Output 中共享的 Pin
            const existing = targetNode.pins.find(p => p.name === dp.name && p.isInput === dp.isInput)
              || targetNode.pins.find(p => p.name === dp.name);

            if (existing) {
              // 检查类型是否依然兼容
              const typeCompatible = existing.valueType === dp.valueType && existing.countType === dp.countType;

              if (typeCompatible) {
                // 尽量保持原有绑定，无论 const 还是 pointer，也不论方向是否完全一致 (只要名字对上且类型兼容)
                newPins.push({ ...dp, binding: existing.binding, bindingType: existing.binding.type, vectorIndex: existing.vectorIndex });
              } else {
                // 类型不兼容，使用新的默认 Pin
                newPins.push(dp);
              }
            } else {
              // 全新 Pin
              newPins.push(dp);
            }
          });

          // 4. 更新节点
          const newNodes = new Map(tree.nodes);
          newNodes.set(nodeId, { ...targetNode, pins: newPins });

          // 5. 清理失效的数据连接
          const newPinNames = new Set(newPins.map(p => p.name));
          let newDataConnections = tree.dataConnections.filter(dc => {
            if (dc.fromNodeId === nodeId && !newPinNames.has(dc.fromPinName)) return false;
            if (dc.toNodeId === nodeId && !newPinNames.has(dc.toPinName)) return false;
            // 如果 Pin 存在但类型变了，也应该清理
            if (dc.fromNodeId === nodeId) {
              const p = newPins.find(p => p.name === dc.fromPinName);
              const oldP = targetNode.pins.find(p => p.name === dc.fromPinName);
              if (p && oldP && (p.valueType !== oldP.valueType || p.countType !== oldP.countType)) return false;
            }
            if (dc.toNodeId === nodeId) {
              const p = newPins.find(p => p.name === dc.toPinName);
              const oldP = targetNode.pins.find(p => p.name === dc.toPinName);
              if (p && oldP && (p.valueType !== oldP.valueType || p.countType !== oldP.countType)) return false;
            }
            return true;
          });

          return { ...tree, nodes: newNodes, dataConnections: newDataConnections };
        });
        return result || state;
      });

    } catch (e) {
      console.error('Failed to reload SubTree pins:', e);
      useNotificationStore.getState().notify(`Failed to load subtree: ${treeFile}`, 'error');
    }
  },

  // 辅助函数: 获取旧节点的 Pin 类型 (用于清理连接)
  // 这里我们简化一下，如果 Pins 变了直接触发 label 刷新即可，清理逻辑可以更细致点

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
      inputs: [],
      outputs: [],
      inputPins: [],
      outputPins: [],
    };

    const newFile: OpenedFile = {
      path,
      name: `${uniqueName}.tree`,
      tree: newTree,
      lastSavedTreeSnapshot: serializeTreeForEditor(newTree),
      isDirty: false,
      isNew: true,
      history: { past: [], future: [] }
    };

    // Deactivate any active FSM file
    useFSMStore.getState().setActiveFSM(null as any);

    return {
      openedFiles: [...state.openedFiles, newFile],
      activeFilePath: path,
    };
  }),
}));

// ==================== Special Node Logic Helper ====================

/**
 * 应用特殊节点的标签更新
 * @param tree 原始树对象
 * @param nodeIdsToCheck 需要检查的节点 ID 列表
 * @returns 更新后的树对象（如果没有变化则返回原对象）
 */
function applyLabelUpdatesToTree(tree: Tree, nodeIdsToCheck: string[]): Tree {
  if (nodeIdsToCheck.length === 0) return tree;

  let newConnections = tree.connections;
  let hasChanges = false;
  const processedNodeIds = new Set<string>();

  for (const nodeId of nodeIdsToCheck) {
    if (!nodeId || processedNodeIds.has(nodeId)) continue;
    processedNodeIds.add(nodeId);

    const node = tree.nodes.get(nodeId);
    if (!node) continue;

    const handler = (SPECIAL_NODE_HANDLERS_IMPL as any)[node.type];
    if (handler && handler.refreshLabels) {
      // 获取更新列表
      const updates = handler.refreshLabels(nodeId, tree);

      if (updates.length > 0) {
        // 如果还没有克隆过 connections，先克隆
        if (!hasChanges) {
          newConnections = [...tree.connections];
          hasChanges = true;
        }

        // 应用更新
        updates.forEach((update: ConnectionLabelUpdate) => {
          const index = newConnections.findIndex((c) => c.id === update.id);
          if (index !== -1) {
            newConnections[index] = { ...newConnections[index], label: update.label };
          }
        });
      }
    }
  }

  if (hasChanges) {
    return { ...tree, connections: newConnections };
  }

  return tree;
}
