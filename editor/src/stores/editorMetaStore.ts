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

    // Actions
    setNodeFolded: (filePath: string, nodeId: string, isFolded: boolean) => void;
    getTreeMeta: (filePath: string) => TreeMeta | undefined;
    loadAllMeta: () => Promise<void>;
    saveAllMeta: () => Promise<void>;
}

const META_FILE_NAME = 'editor_meta.local.json';

export const useEditorMetaStore = create<EditorMetaState>((set, get) => ({
    treeMetas: {},

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

    getTreeMeta: (filePath) => {
        return get().treeMetas[filePath];
    },

    loadAllMeta: async () => {
        try {
            const path = await getConfigPath(META_FILE_NAME);
            const content = await readFile(path);
            if (content) {
                const data = JSON.parse(content);
                set({ treeMetas: data });
            }
        } catch (e) {
            console.warn('Failed to load editor metadata:', e);
        }
    },

    saveAllMeta: async () => {
        try {
            const path = await getConfigPath(META_FILE_NAME);
            const content = JSON.stringify(get().treeMetas, null, 2);
            await writeFile(path, content);
        } catch (e) {
            console.error('Failed to save editor metadata:', e);
        }
    }
}));
