import { create } from 'zustand';
import { writeFile, readFile } from '../utils/fileService';
import { getConfigPath } from '../utils/configPath';

/**
 * 节点元数据（不保存在 .tree 文件中）
 */
interface NodeMeta {
    isFolded?: boolean;
    breakpoints?: any[]; // 预留
}

/**
 * 树的元数据，以文件路径为键
 */
interface TreeMeta {
    nodes: Record<string, NodeMeta>; // nodeId -> meta
}

interface EditorMetaState {
    // filePath -> TreeMeta
    treeMetas: Record<string, TreeMeta>;
    uiMeta: {
        sidebarWidth: number;
        isSearchOpen: boolean;
        activePropertiesTab: 'variables' | 'io' | 'properties';
        focusTarget?: { type: 'node' | 'variable' | 'io' | 'state' | 'transition', id: string };
        pendingCenterTarget?: { x: number, y: number, zoom?: number };
    };

    // Actions
    setNodeFolded: (filePath: string, nodeId: string, isFolded: boolean) => void;
    setSidebarWidth: (width: number) => void;
    setSearchOpen: (open: boolean) => void;
    setActivePropertiesTab: (tab: 'variables' | 'io' | 'properties') => void;
    setFocusTarget: (target?: { type: 'node' | 'variable' | 'io' | 'state' | 'transition', id: string }) => void;
    setPendingCenterTarget: (target?: { x: number, y: number, zoom?: number }) => void;
    getTreeMeta: (filePath: string) => TreeMeta | undefined;
    loadAllMeta: () => Promise<void>;
    saveAllMeta: () => Promise<void>;
}

const META_FILE_NAME = 'editor_meta.local.json';

export const useEditorMetaStore = create<EditorMetaState>((set, get) => ({
    treeMetas: {},
    uiMeta: {
        sidebarWidth: 160,
        isSearchOpen: false,
        activePropertiesTab: 'properties'
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

    getTreeMeta: (filePath) => {
        return get().treeMetas[filePath];
    },

    loadAllMeta: async () => {
        try {
            const path = await getConfigPath(META_FILE_NAME);
            const content = await readFile(path);
            if (content) {
                const data = JSON.parse(content);
                if (data.treeMetas || data.uiMeta) {
                    set({
                        treeMetas: data.treeMetas || {},
                        uiMeta: {
                            ...(data.uiMeta || { sidebarWidth: 160 }),
                            isSearchOpen: false,
                            activePropertiesTab: 'properties',
                            focusTarget: undefined
                        }
                    });
                } else {
                    set({ treeMetas: data });
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
                uiMeta: get().uiMeta
            }, null, 2);
            await writeFile(path, content);
        } catch (e) {
            console.error('Failed to save editor metadata:', e);
        }
    }
}));
