/**
 * FSM Store
 * 
 * Zustand store slice for FSM-specific state and actions.
 */

import { create } from 'zustand';
import type { Variable } from '../types';
import type { FSM, FSMMachine, FSMState, FSMStateType } from '../types/fsm';
import {
    createFSMState,
    createFSMTransition,
    createEmptyFSM,
    isSpecialStateType,
} from '../types/fsm';
import { parseFSMXml } from '../utils/fsmParser';
import { serializeFSMForEditor, serializeFSMForRuntime } from '../utils/fsmSerializer';
import { recalculateFSMUIDs } from '../utils/fsmUtils';
import { writeFile, readFile } from '../utils/fileService';
import { useEditorStore } from './editorStore';
import { useNotificationStore } from './notificationStore';
import { useDebugStore } from './debugStore';

// ==================== Types ====================

interface FSMHistoryState {
    past: FSM[];
    future: FSM[];
}

interface OpenedFSMFile {
    path: string;
    name: string;
    fsm: FSM;
    lastSavedSnapshot: string;
    isDirty: boolean;
    isNew?: boolean;
    history: FSMHistoryState;
    currentMachineId: string; // For navigating nested machines
    viewport?: import('./editorStoreCore').Viewport;
}

interface FSMStoreState {
    openedFSMFiles: OpenedFSMFile[];
    activeFSMPath: string | null;
    isConnecting: boolean;
    selectedNodeIds: string[];
    selectedEdgeIds: string[];

    // Actions
    setIsConnecting: (isConnecting: boolean) => void;
    openFSM: (path: string, content: string) => void;
    openFSMFile: (path: string) => Promise<void>;

    closeFSM: (path: string) => void;
    setActiveFSM: (path: string) => void;
    getCurrentFSM: () => FSM | null;
    getCurrentMachine: () => FSMMachine | null;

    // State actions
    addState: (type: FSMStateType, position: { x: number; y: number }) => void;
    removeState: (stateId: string) => void;
    updateState: (stateId: string, updates: Partial<FSMState>) => void;
    setDefaultState: (stateId: string | null) => void;

    // Transition actions
    addTransition: (fromStateId: string | null, toStateId: string, conditions?: string[]) => void;
    removeTransition: (transitionId: string) => void;
    addConditionToTransition: (transitionId: string, condition: string) => void;
    removeConditionFromTransition: (transitionId: string, condition: string) => void;

    // Variable actions
    addVariable: (isLocal: boolean, variable: Variable) => void;
    removeVariable: (isLocal: boolean, name: string) => void;
    updateVariable: (isLocal: boolean, name: string, updates: Partial<Variable>) => void;

    // Navigation
    navigateToMachine: (machineId: string) => void;
    navigateUp: () => void;

    // Selection actions
    setSelectedNodes: (nodeIds: string[]) => void;
    setSelectedEdges: (edgeIds: string[]) => void;

    // History
    undo: () => void;
    redo: () => void;

    // Save
    saveFSM: () => Promise<string>;
    saveFSMAs: () => Promise<void>;
    createNewFSM: (name: string) => void;

    // Viewport
    setViewport: (path: string, viewport: import('./editorStoreCore').Viewport) => void;
}

// ==================== Helper Functions ====================

function updateFSMFile(
    files: OpenedFSMFile[],
    activePath: string | null,
    updater: (fsm: FSM) => FSM,
    options: { skipHistory?: boolean } = {}
): { openedFSMFiles: OpenedFSMFile[] } | null {
    if (!activePath) return null;

    const fileIndex = files.findIndex(f => f.path === activePath);
    if (fileIndex === -1) return null;

    const file = files[fileIndex];
    const oldFSM = file.fsm;
    const newFSM = updater(oldFSM);

    if (newFSM === oldFSM) return null;

    const currentSnapshot = serializeFSMForEditor(newFSM);
    const isDirty = currentSnapshot !== file.lastSavedSnapshot;

    const newFile: OpenedFSMFile = {
        ...file,
        fsm: newFSM,
        isDirty,
    };

    if (!options.skipHistory) {
        newFile.history = {
            past: [oldFSM, ...file.history.past].slice(0, 50),
            future: [],
        };
    }

    const newFiles = [...files];
    newFiles[fileIndex] = newFile;

    return { openedFSMFiles: newFiles };
}

// ==================== Store ====================

export const useFSMStore = create<FSMStoreState>((set, get) => ({
    openedFSMFiles: [],
    activeFSMPath: null,
    isConnecting: false,
    selectedNodeIds: [],
    selectedEdgeIds: [],

    setIsConnecting: (isConnecting) => set({ isConnecting }),

    setSelectedNodes: (nodeIds) => set({ selectedNodeIds: nodeIds }),
    setSelectedEdges: (edgeIds) => set({ selectedEdgeIds: edgeIds }),



    openFSM: (path, content) => {
        const { openedFSMFiles } = get();
        const normalizedPath = path.replace(/\\/g, '/');

        // Check if already open
        const existing = openedFSMFiles.find(f => f.path.replace(/\\/g, '/') === normalizedPath);
        if (existing) {
            useEditorStore.getState().setActiveFile(null as any);
            set({ activeFSMPath: normalizedPath });
            return;
        }

        const fileName = path.split(/[\\/]/).pop() || 'Unnamed';
        let fsm = parseFSMXml(content, fileName);
        fsm = recalculateFSMUIDs(fsm); // Calculate UIDs matching runtime

        const snapshot = serializeFSMForEditor(fsm);

        const newFile: OpenedFSMFile = {
            path,
            name: fsm.name,
            fsm,
            lastSavedSnapshot: snapshot,
            isDirty: false,
            history: { past: [], future: [] },
            currentMachineId: fsm.rootMachineId,
        };

        set({
            openedFSMFiles: [...openedFSMFiles, newFile],
            activeFSMPath: normalizedPath,
        });
        useEditorStore.getState().setActiveFile(null as any);
    },

    openFSMFile: async (path: string) => {
        const { editorTreeDir } = useEditorStore.getState();
        if (!editorTreeDir) return;

        const fullPath = `${editorTreeDir}/${path}`;
        try {
            const content = await readFile(fullPath);
            get().openFSM(path, content);
        } catch (e) {
            console.error('Failed to open FSM:', e);
            useNotificationStore.getState().notify(`Failed to open FSM: ${e}`, 'error');
        }
    },

    closeFSM: (path) => {
        const { openedFSMFiles, activeFSMPath } = get();
        const newFiles = openedFSMFiles.filter(f => f.path !== path);
        let newActive = activeFSMPath;

        if (activeFSMPath === path) {
            if (newFiles.length > 0) {
                // Select another FSM
                newActive = newFiles[0].path;
            } else {
                // No more FSMs open, fallback to a Tree file if available
                newActive = null;
                const editorState = useEditorStore.getState();
                if (editorState.openedFiles.length > 0) {
                    editorState.setActiveFile(editorState.openedFiles[0].path);
                }
            }
        }

        set({ openedFSMFiles: newFiles, activeFSMPath: newActive });
    },

    setActiveFSM: (path) => {
        const normalized = path ? path.replace(/\\/g, '/') : path;
        if (normalized) useEditorStore.getState().setActiveFile(null as any);
        set({ activeFSMPath: normalized });
    },

    setViewport: (path, viewport) => set((state) => {
        const fileIndex = state.openedFSMFiles.findIndex(f => f.path === path);
        if (fileIndex === -1) return state;

        const newFiles = [...state.openedFSMFiles];
        newFiles[fileIndex] = { ...newFiles[fileIndex], viewport };
        return { openedFSMFiles: newFiles };
    }),

    getCurrentFSM: () => {
        const { openedFSMFiles, activeFSMPath } = get();
        const file = openedFSMFiles.find(f => f.path === activeFSMPath);
        return file?.fsm || null;
    },

    getCurrentMachine: () => {
        const { openedFSMFiles, activeFSMPath } = get();
        const file = openedFSMFiles.find(f => f.path === activeFSMPath);
        if (!file) return null;
        return file.fsm.machines.get(file.currentMachineId) || null;
    },

    // State actions
    addState: (type, position) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
                if (!file) return fsm;

                const machine = fsm.machines.get(file.currentMachineId);
                if (!machine) return fsm;

                const newState = createFSMState(type, '', position);
                const newMachine: FSMMachine = {
                    ...machine,
                    states: new Map([...machine.states, [newState.id, newState]]),
                };

                return recalculateFSMUIDs({
                    ...fsm,
                    machines: new Map([...fsm.machines, [machine.id, newMachine]]),
                });
            });
            return result || state;
        });
    },

    removeState: (stateId) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
                if (!file) return fsm;

                const machine = fsm.machines.get(file.currentMachineId);
                if (!machine) return fsm;

                const stateToRemove = machine.states.get(stateId);
                if (!stateToRemove || isSpecialStateType(stateToRemove.type)) return fsm;

                const newStates = new Map(machine.states);
                newStates.delete(stateId);

                // Remove related transitions
                const newTransitions = machine.transitions.filter(
                    t => t.fromStateId !== stateId && t.toStateId !== stateId
                );

                const newMachine: FSMMachine = {
                    ...machine,
                    states: newStates,
                    transitions: newTransitions,
                    defaultStateId: machine.defaultStateId === stateId ? null : machine.defaultStateId,
                };

                return recalculateFSMUIDs({
                    ...fsm,
                    machines: new Map([...fsm.machines, [machine.id, newMachine]]),
                });
            });
            return result || state;
        });
    },

    updateState: (stateId, updates) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
                if (!file) return fsm;

                const machine = fsm.machines.get(file.currentMachineId);
                if (!machine) return fsm;

                const existingState = machine.states.get(stateId);
                if (!existingState) return fsm;

                const newStates = new Map(machine.states);
                newStates.set(stateId, { ...existingState, ...updates });

                const newMachine: FSMMachine = { ...machine, states: newStates };

                return recalculateFSMUIDs({
                    ...fsm,
                    machines: new Map([...fsm.machines, [machine.id, newMachine]]),
                });
            });
            return result || state;
        });
    },

    setDefaultState: (stateId) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
                if (!file) return fsm;

                const machine = fsm.machines.get(file.currentMachineId);
                if (!machine) return fsm;

                const newMachine: FSMMachine = { ...machine, defaultStateId: stateId };

                return {
                    ...fsm,
                    machines: new Map([...fsm.machines, [machine.id, newMachine]]),
                };
            });
            return result || state;
        });
    },

    // Transition actions
    addTransition: (fromStateId, toStateId, conditions = []) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                // All transitions are stored in the root machine (global storage)
                const rootMachine = fsm.machines.get(fsm.rootMachineId);
                if (!rootMachine) return fsm;

                const trans = createFSMTransition(fromStateId, toStateId, conditions);
                const newRootMachine: FSMMachine = {
                    ...rootMachine,
                    transitions: [...rootMachine.transitions, trans],
                };

                return {
                    ...fsm,
                    machines: new Map([...fsm.machines, [fsm.rootMachineId, newRootMachine]]),
                };
            });
            return result || state;
        });
    },

    removeTransition: (transitionId) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                let targetMachineId = '';
                for (const [mId, m] of fsm.machines) {
                    if (m.transitions.some(t => t.id === transitionId)) {
                        targetMachineId = mId;
                        break;
                    }
                }

                if (!targetMachineId) return fsm;
                const machine = fsm.machines.get(targetMachineId);
                if (!machine) return fsm;

                const newMachine: FSMMachine = {
                    ...machine,
                    transitions: machine.transitions.filter(t => t.id !== transitionId),
                };

                return {
                    ...fsm,
                    machines: new Map([...fsm.machines, [machine.id, newMachine]]),
                };
            });
            return result || state;
        });
    },

    addConditionToTransition: (transitionId, condition) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                // All transitions are in root machine
                const rootMachine = fsm.machines.get(fsm.rootMachineId);
                if (!rootMachine) return fsm;

                const newTransitions = rootMachine.transitions.map(t =>
                    t.id === transitionId
                        ? { ...t, conditions: [...t.conditions, condition] }
                        : t
                );

                const newMachine: FSMMachine = { ...rootMachine, transitions: newTransitions };

                return {
                    ...fsm,
                    machines: new Map([...fsm.machines, [fsm.rootMachineId, newMachine]]),
                };
            });
            return result || state;
        });
    },

    removeConditionFromTransition: (transitionId, condition) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                // All transitions are in root machine
                const rootMachine = fsm.machines.get(fsm.rootMachineId);
                if (!rootMachine) return fsm;

                const newTransitions = rootMachine.transitions.map(t =>
                    t.id === transitionId
                        ? { ...t, conditions: t.conditions.filter(c => c !== condition) }
                        : t
                );

                const newMachine: FSMMachine = { ...rootMachine, transitions: newTransitions };

                return {
                    ...fsm,
                    machines: new Map([...fsm.machines, [fsm.rootMachineId, newMachine]]),
                };
            });
            return result || state;
        });
    },

    // Variable actions
    addVariable: (isLocal, variable) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                if (isLocal) {
                    return { ...fsm, localVariables: [...fsm.localVariables, variable] };
                }
                return { ...fsm, sharedVariables: [...fsm.sharedVariables, variable] };
            });
            return result || state;
        });
    },

    removeVariable: (isLocal, name) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                if (isLocal) {
                    return { ...fsm, localVariables: fsm.localVariables.filter(v => v.name !== name) };
                }
                return { ...fsm, sharedVariables: fsm.sharedVariables.filter(v => v.name !== name) };
            });
            return result || state;
        });
    },

    updateVariable: (isLocal, name, updates) => {
        if (useDebugStore.getState().isConnected) return;
        set((state) => {
            const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
                if (isLocal) {
                    return {
                        ...fsm,
                        localVariables: fsm.localVariables.map(v => v.name === name ? { ...v, ...updates } : v),
                    };
                }
                return {
                    ...fsm,
                    sharedVariables: fsm.sharedVariables.map(v => v.name === name ? { ...v, ...updates } : v),
                };
            });
            return result || state;
        });
    },

    // Navigation
    navigateToMachine: (machineId) => set((state) => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        if (!file || !file.fsm.machines.has(machineId)) return state;

        const newFiles = state.openedFSMFiles.map(f =>
            f.path === state.activeFSMPath
                ? { ...f, currentMachineId: machineId }
                : f
        );

        return { openedFSMFiles: newFiles };
    }),

    navigateUp: () => set((state) => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        if (!file) return state;

        const currentMachine = file.fsm.machines.get(file.currentMachineId);
        if (!currentMachine || !currentMachine.parentMetaStateId) return state;

        // Find the parent machine
        for (const [machineId, machine] of file.fsm.machines) {
            if (machine.states.has(currentMachine.parentMetaStateId)) {
                const newFiles = state.openedFSMFiles.map(f =>
                    f.path === state.activeFSMPath
                        ? { ...f, currentMachineId: machineId }
                        : f
                );
                return { openedFSMFiles: newFiles };
            }
        }

        return state;
    }),

    // History
    undo: () => set((state) => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        if (!file || file.history.past.length === 0) return state;

        const [previous, ...rest] = file.history.past;
        const currentSnapshot = serializeFSMForEditor(previous);

        const newFile: OpenedFSMFile = {
            ...file,
            fsm: previous,
            isDirty: currentSnapshot !== file.lastSavedSnapshot,
            history: {
                past: rest,
                future: [file.fsm, ...file.history.future],
            },
        };

        return {
            openedFSMFiles: state.openedFSMFiles.map(f =>
                f.path === state.activeFSMPath ? newFile : f
            ),
        };
    }),

    redo: () => set((state) => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        if (!file || file.history.future.length === 0) return state;

        const [next, ...rest] = file.history.future;
        const currentSnapshot = serializeFSMForEditor(next);

        const newFile: OpenedFSMFile = {
            ...file,
            fsm: next,
            isDirty: currentSnapshot !== file.lastSavedSnapshot,
            history: {
                past: [file.fsm, ...file.history.past],
                future: rest,
            },
        };

        return {
            openedFSMFiles: state.openedFSMFiles.map(f =>
                f.path === state.activeFSMPath ? newFile : f
            ),
        };
    }),

    // Save
    saveFSM: async () => {
        const { openedFSMFiles, activeFSMPath } = get();
        const file = openedFSMFiles.find(f => f.path === activeFSMPath);
        if (!file || !activeFSMPath) throw new Error('No active FSM file');

        // Handle new files (no path or new://)
        if (file.isNew || activeFSMPath.startsWith('new://')) {
            // We should probably redirect to saveAs or handle it in MainWindow
            // For now, let's just serialize and expect MainWindow to handle the path
            return serializeFSMForEditor(file.fsm);
        }

        const { editorTreeDir, runtimeTreeDir } = useEditorStore.getState();
        if (!editorTreeDir || !runtimeTreeDir) throw new Error('Tree directories not set');

        const content = serializeFSMForEditor(file.fsm);
        const fullPath = `${editorTreeDir}/${activeFSMPath}`;

        const runtimeContent = serializeFSMForRuntime(file.fsm);
        const runtimePath = `${runtimeTreeDir}/${activeFSMPath}`;

        // Cleanup node meta for states that no longer exist
        const allStateIds: string[] = [];
        file.fsm.machines.forEach(machine => {
            machine.states.forEach(state => {
                if (state.uid) allStateIds.push(state.uid.toString()); // Use UID if available for debugging mapping
                if (state.id) allStateIds.push(state.id);
            });
        });
        // Note: FSM meta might be keyed by UID or ID depending on usage. 
        // DebugStore uses UID for runtime mapping, but EditorMetaStore might use ID for folding?
        // Let's check SetNodeBreakpoint: it uses `nodeId`. 
        // In FSM, `nodeId` is usually the string ID (e.g. "StateParameters_1").
        // But DebugStore uses UID (integer).
        // Let's assume ID for now as that's what `editorMetaStore` generally expects from `tree.nodes`.

        // Actually, let's collect BOTH just in case, or check how setNodeBreakpoint is called in FSM context.
        // FSMStateNode uses `state.uid` for debug state.
        // But what about Breakpoints?
        // useDebugStore.getBreakpoint(activeFilePath, treeNode.uid) -> checks meta.
        // So meta is keyed by UID for FSM?
        // editorMetaStore.setNodeBreakpoint(filePath, nodeId, type).
        // debugStore syncs breakpoints using `meta.nodes`.

        // In `editorMetaStore.ts`, keys are just strings.
        // If FSM uses UIDs as keys in meta, then we need to pass UIDs.
        // Let's check `FSMStateNode.tsx`.

        // ... I'll fetch valid IDs from both ID and UID to be safe, or just collect all existing keys from machines.
        const validIds = new Set<string>();
        file.fsm.machines.forEach(m => {
            m.states.forEach(s => {
                validIds.add(s.id);
                if (s.uid) validIds.add(s.uid.toString());
            });
        });

        const { useEditorMetaStore } = await import('./editorMetaStore');
        useEditorMetaStore.getState().cleanNodeMeta(activeFSMPath, Array.from(validIds));


        try {
            await writeFile(fullPath, content);
            await writeFile(runtimePath, runtimeContent);

            set({
                openedFSMFiles: openedFSMFiles.map(f =>
                    f.path === activeFSMPath
                        ? { ...f, lastSavedSnapshot: content, isDirty: false }
                        : f
                ),
            });

            // Refresh file list in editorStore to ensure search visibility
            useEditorStore.getState().refreshFiles();

            useNotificationStore.getState().notify('Save successful', 'success');

            return content;
        } catch (e) {
            console.error('Save failed:', e);
            useNotificationStore.getState().notify(`Save failed: ${e}`, 'error');
            throw e;
        }
    },

    saveFSMAs: async () => {
        const { openedFSMFiles, activeFSMPath } = get();
        const editorTreeDir = useEditorStore.getState().editorTreeDir;
        if (!activeFSMPath || !editorTreeDir) return;

        const file = openedFSMFiles.find(f => f.path === activeFSMPath);
        if (!file) return;

        try {
            const { save } = await import('@tauri-apps/plugin-dialog');
            const newPath = await save({
                title: 'Save FSM As',
                defaultPath: editorTreeDir || undefined,
                filters: [{ name: 'FSM', extensions: ['fsm'] }]
            });

            if (!newPath) return;

            const fileName = newPath.split(/[/\\]/).pop() || '';
            const fsmName = fileName.replace(/\.fsm$/, '');
            if (fsmName.includes(' ')) {
                useNotificationStore.getState().notify('FSM name cannot contain spaces', 'error');
                return;
            }

            const { runtimeTreeDir } = useEditorStore.getState();
            if (!runtimeTreeDir) return;

            let relativePath = newPath;
            const normalizedDir = editorTreeDir.replace(/\\/g, '/');
            const normalizedNewPath = newPath.replace(/\\/g, '/');

            if (normalizedNewPath.startsWith(normalizedDir)) {
                relativePath = normalizedNewPath.slice(normalizedDir.length).replace(/^\//, '');
            } else {
                relativePath = newPath.split(/[/\\]/).pop() || relativePath;
            }

            const content = serializeFSMForEditor({ ...file.fsm, name: fsmName });
            const runtimeContent = serializeFSMForRuntime({ ...file.fsm, name: fsmName });
            const runtimePath = `${runtimeTreeDir}/${relativePath}`;

            await writeFile(newPath, content);
            await writeFile(runtimePath, runtimeContent);

            set(state => ({
                openedFSMFiles: state.openedFSMFiles.map(f =>
                    f.path === activeFSMPath
                        ? {
                            ...f,
                            path: relativePath,
                            name: fsmName,
                            fsm: { ...f.fsm, name: fsmName },
                            lastSavedSnapshot: content,
                            isDirty: false,
                            isNew: false
                        }
                        : f
                ),
                activeFSMPath: relativePath
            }));

            // Refresh file list in editorStore
            useEditorStore.getState().refreshFiles();

            useNotificationStore.getState().notify('Save successful', 'success');

        } catch (e) {
            console.error('Save As failed:', e);
            useNotificationStore.getState().notify(`Save As failed: ${e}`, 'error');
        }
    },

    createNewFSM: (name) => {
        // Generate unique name
        const { openedFSMFiles } = get();
        let uniqueName = name;
        let counter = 1;
        while (openedFSMFiles.some(f => f.path === `new://${uniqueName}.fsm`)) {
            uniqueName = `${name}${counter}`;
            counter++;
        }

        const fsm = createEmptyFSM(uniqueName);
        const path = `new://${uniqueName}.fsm`;
        const snapshot = serializeFSMForEditor(fsm);

        const newFile: OpenedFSMFile = {
            path,
            name: `${uniqueName}.fsm`,
            fsm,
            lastSavedSnapshot: snapshot,
            isDirty: false,
            isNew: true,
            history: { past: [], future: [] },
            currentMachineId: fsm.rootMachineId,
        };

        // Deactivate any active tree file
        useEditorStore.getState().setActiveFile(null as any);

        set((state) => ({
            openedFSMFiles: [...state.openedFSMFiles, newFile],
            activeFSMPath: path,
        }));
    },
}));
