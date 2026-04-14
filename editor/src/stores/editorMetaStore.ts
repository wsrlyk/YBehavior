import { create } from 'zustand';
import { writeFile, readFile } from '../utils/fileService';
import { getConfigPath } from '../utils/configPath';
import type { Viewport } from './editorStoreCore';

/**
 * 节点元数据（不保存在 .tree 文件中）
 */
interface NodeMeta {
    isFolded?: boolean;
    breakpointType?: number; // 1=Break, -1=Log, undefined/0=None
}

/**
 * 树的元数据，以文件路径为键
 */
interface TreeMeta {
    nodes: Record<string, NodeMeta>; // nodeId -> meta
    viewport?: Viewport;
}

interface EditorMetaState {
    // filePath -> TreeMeta
    treeMetas: Record<string, TreeMeta>;
    uiMeta: {
        sidebarWidth: number;
        propertiesPanelWidth: number;
        currentTheme?: string;
        isSearchOpen: boolean;
        activePropertiesTab: 'variables' | 'io' | 'properties';
        focusTarget?: { type: 'node' | 'variable' | 'io' | 'state' | 'transition', id: string };
        pendingCenterTarget?: { x: number, y: number, zoom?: number };
        searchConfig: {
            isCaseSensitive: boolean;
            isWholeWord: boolean;
        };
    };
    debugMeta: {
        ip: string;
        port: number;
    };

    // Actions
    setNodeFolded: (filePath: string, nodeId: string, isFolded: boolean) => void;
    setSidebarWidth: (width: number) => void;
    setPropertiesPanelWidth: (width: number) => void;
    setSearchOpen: (open: boolean) => void;
    setActivePropertiesTab: (tab: 'variables' | 'io' | 'properties') => void;
    setFocusTarget: (target?: { type: 'node' | 'variable' | 'io' | 'state' | 'transition', id: string }) => void;
    setPendingCenterTarget: (target?: { x: number, y: number, zoom?: number }) => void;
    setDebugIP: (ip: string) => void;
    setDebugPort: (port: number) => void;
    setNodeBreakpoint: (filePath: string, nodeId: string, type: number) => void;
    setTreeViewport: (filePath: string, viewport: Viewport) => void;
    setSearchConfig: (config: { isCaseSensitive?: boolean; isWholeWord?: boolean }) => void;
    getTreeMeta: (filePath: string) => TreeMeta | undefined;

    // Cleanup
    cleanNodeMeta: (filePath: string, validNodeIds: string[]) => void;
    cleanOrphanedMeta: (existingFiles: string[]) => void;

    loadAllMeta: () => Promise<void>;
    saveAllMeta: () => Promise<void>;
}

const META_FILE_NAME = 'editor_meta.local.json';

export const useEditorMetaStore = create<EditorMetaState>((set, get) => ({
    treeMetas: {},
    uiMeta: {
        sidebarWidth: 160,
        propertiesPanelWidth: 300,
        currentTheme: undefined,
        isSearchOpen: false,
        activePropertiesTab: 'properties',
        searchConfig: {
            isCaseSensitive: false,
            isWholeWord: false,
        }
    },
    debugMeta: {
        ip: '127.0.0.1',
        port: 8888
    },

    setNodeFolded: (filePath, nodeId, isFolded) => {
        set((state) => {
            const treeMeta = state.treeMetas[filePath] || { nodes: {} };
            const nodeMeta = treeMeta.nodes[nodeId] || {};

            const newTreeMetas = {
                ...state.treeMetas,
                [filePath]: {
                    ...treeMeta,
                    nodes: {
                        ...treeMeta.nodes,
                        [nodeId]: {
                            ...nodeMeta,
                            isFolded
                        }
                    }
                }
            };

            return { treeMetas: newTreeMetas };
        });

        // 异步保存
        get().saveAllMeta();
    },

    setSidebarWidth: (width) => {
        set((state) => ({
            uiMeta: { ...state.uiMeta, sidebarWidth: width }
        }));
        get().saveAllMeta();
    },

    setPropertiesPanelWidth: (width) => {
        set((state) => ({
            uiMeta: { ...state.uiMeta, propertiesPanelWidth: width }
        }));
        get().saveAllMeta();
    },

    setSearchOpen: (open) => {
        set((state) => ({
            uiMeta: { ...state.uiMeta, isSearchOpen: open }
        }));
    },

    setActivePropertiesTab: (tab) => {
        set((state) => ({
            uiMeta: { ...state.uiMeta, activePropertiesTab: tab }
        }));
    },

    setFocusTarget: (target) => {
        set((state) => ({
            uiMeta: { ...state.uiMeta, focusTarget: target }
        }));
    },

    setPendingCenterTarget: (target) => {
        set((state) => ({
            uiMeta: { ...state.uiMeta, pendingCenterTarget: target }
        }));
    },

    setDebugIP: (ip) => {
        set((state) => ({
            debugMeta: { ...state.debugMeta, ip }
        }));
        get().saveAllMeta();
    },

    setDebugPort: (port) => {
        set((state) => ({
            debugMeta: { ...state.debugMeta, port }
        }));
        get().saveAllMeta();
    },

    setNodeBreakpoint: (filePath, nodeId, type) => {
        set((state) => {
            const treeMeta = state.treeMetas[filePath] || { nodes: {} };
            const nodeMeta = treeMeta.nodes[nodeId] || {};

            const newTreeMetas = {
                ...state.treeMetas,
                [filePath]: {
                    ...treeMeta,
                    nodes: {
                        ...treeMeta.nodes,
                        [nodeId]: {
                            ...nodeMeta,
                            breakpointType: type
                        }
                    }
                }
            };

            return { treeMetas: newTreeMetas };
        });
        get().saveAllMeta();
    },

    setTreeViewport: (filePath, viewport) => {
        set((state) => {
            const normalizedPath = filePath.replace(/\\/g, '/');
            const treeMeta = state.treeMetas[normalizedPath] || { nodes: {} };

            return {
                treeMetas: {
                    ...state.treeMetas,
                    [normalizedPath]: {
                        ...treeMeta,
                        viewport
                    }
                }
            };
        });
        get().saveAllMeta();
    },

    setSearchConfig: (config) => {
        set((state) => ({
            uiMeta: {
                ...state.uiMeta,
                searchConfig: {
                    ...state.uiMeta.searchConfig,
                    ...config
                }
            }
        }));
        get().saveAllMeta();
    },

    getTreeMeta: (filePath) => {
        return get().treeMetas[filePath];
    },

    cleanNodeMeta: (filePath, validNodeIds) => {
        set((state) => {
            const treeMeta = state.treeMetas[filePath];
            if (!treeMeta) return state;

            const validSet = new Set(validNodeIds);
            const currentNodes = treeMeta.nodes;
            const metaNodeIds = Object.keys(currentNodes);

            // Check if we need to remove anything
            const toRemove = metaNodeIds.filter(id => !validSet.has(id));
            if (toRemove.length === 0) return state;

            const newNodes = { ...currentNodes };
            toRemove.forEach(id => delete newNodes[id]);

            return {
                treeMetas: {
                    ...state.treeMetas,
                    [filePath]: {
                        ...treeMeta,
                        nodes: newNodes
                    }
                }
            };
        });
        get().saveAllMeta();
    },

    cleanOrphanedMeta: (existingFiles) => {
        set((state) => {
            const metaFiles = Object.keys(state.treeMetas);
            const normalizedExisting = new Set(existingFiles.map(f => f.replace(/\\/g, '/')));

            const toRemove = metaFiles.filter(f => !normalizedExisting.has(f.replace(/\\/g, '/')));
            if (toRemove.length === 0) return state;

            const newTreeMetas = { ...state.treeMetas };
            toRemove.forEach(f => delete newTreeMetas[f]);

            return { treeMetas: newTreeMetas };
        });
        get().saveAllMeta();
    },

    loadAllMeta: async () => {
        try {
            const path = await getConfigPath(META_FILE_NAME);
            const content = await readFile(path);
            if (content) {
                const data = JSON.parse(content);
                const rawTreeMetas = data.treeMetas || (data.nodes ? undefined : data) || {};
                const normalizedTreeMetas: any = {};

                Object.entries(rawTreeMetas).forEach(([k, v]) => {
                    normalizedTreeMetas[k.replace(/\\/g, '/')] = v;
                });

                if (data.treeMetas || data.uiMeta || data.debugMeta) {
                    set({
                        treeMetas: normalizedTreeMetas,
                        uiMeta: {
                            ...(data.uiMeta || { sidebarWidth: 160, propertiesPanelWidth: 300 }),
                            isSearchOpen: false,
                            activePropertiesTab: 'properties',
                            focusTarget: undefined,
                            searchConfig: data.uiMeta?.searchConfig || { isCaseSensitive: false, isWholeWord: false }
                        },
                        debugMeta: data.debugMeta || { ip: '127.0.0.1', port: 8888 }
                    });
                } else {
                    set({ treeMetas: normalizedTreeMetas });
                }
            }
        } catch (e) {
            console.warn('Failed to load editor metadata:', e);
        }
    },

    saveAllMeta: async () => {
        try {
            const path = await getConfigPath(META_FILE_NAME);
            const content = JSON.stringify({
                treeMetas: get().treeMetas,
                uiMeta: get().uiMeta,
                debugMeta: get().debugMeta
            }, null, 2);
            await writeFile(path, content);
        } catch (e) {
            console.error('Failed to save editor metadata:', e);
        }
    }
}));
