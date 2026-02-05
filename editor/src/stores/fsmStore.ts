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
import { writeFile } from '../utils/fileService';
import { useEditorStore } from './editorStore';
import { useNotificationStore } from './notificationStore';

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
    addTransition: (fromStateId: string | null, toStateId: string, events?: string[]) => void;
    removeTransition: (transitionId: string) => void;
    addEventToTransition: (transitionId: string, event: string) => void;
    removeEventFromTransition: (transitionId: string, event: string) => void;

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

        // Check if already open
        if (openedFSMFiles.some(f => f.path === path)) {
            set({ activeFSMPath: path });
            return;
        }

        const fileName = path.split(/[\\/]/).pop() || 'Unnamed';
        const fsm = parseFSMXml(content, fileName);
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
            activeFSMPath: path,
        });
    },

    closeFSM: (path) => {
        const { openedFSMFiles, activeFSMPath } = get();
        const newFiles = openedFSMFiles.filter(f => f.path !== path);
        const newActive = activeFSMPath === path
            ? (newFiles.length > 0 ? newFiles[0].path : null)
            : activeFSMPath;

        set({ openedFSMFiles: newFiles, activeFSMPath: newActive });
    },

    setActiveFSM: (path) => set({ activeFSMPath: path }),

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
    addState: (type, position) => set((state) => {
        console.log('fsmStore.addState called', type, position);
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

            return {
                ...fsm,
                machines: new Map([...fsm.machines, [machine.id, newMachine]]),
            };
        });
        return result || state;
    }),

    removeState: (stateId) => set((state) => {
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

            return {
                ...fsm,
                machines: new Map([...fsm.machines, [machine.id, newMachine]]),
            };
        });
        return result || state;
    }),

    updateState: (stateId, updates) => set((state) => {
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

            return {
                ...fsm,
                machines: new Map([...fsm.machines, [machine.id, newMachine]]),
            };
        });
        return result || state;
    }),

    setDefaultState: (stateId) => set((state) => {
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
    }),

    // Transition actions
    addTransition: (fromStateId, toStateId, events = []) => set((state) => {
        const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
            // All transitions are stored in the root machine (global storage)
            const rootMachine = fsm.machines.get(fsm.rootMachineId);
            if (!rootMachine) return fsm;

            const trans = createFSMTransition(fromStateId, toStateId, events);
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
    }),

    removeTransition: (transitionId) => set((state) => {
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
    }),

    addEventToTransition: (transitionId, event) => set((state) => {
        const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
            // All transitions are in root machine
            const rootMachine = fsm.machines.get(fsm.rootMachineId);
            if (!rootMachine) return fsm;

            const newTransitions = rootMachine.transitions.map(t =>
                t.id === transitionId
                    ? { ...t, events: [...t.events, event] }
                    : t
            );

            const newMachine: FSMMachine = { ...rootMachine, transitions: newTransitions };

            return {
                ...fsm,
                machines: new Map([...fsm.machines, [fsm.rootMachineId, newMachine]]),
            };
        });
        return result || state;
    }),

    removeEventFromTransition: (transitionId, event) => set((state) => {
        const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
            // All transitions are in root machine
            const rootMachine = fsm.machines.get(fsm.rootMachineId);
            if (!rootMachine) return fsm;

            const newTransitions = rootMachine.transitions.map(t =>
                t.id === transitionId
                    ? { ...t, events: t.events.filter(e => e !== event) }
                    : t
            );

            const newMachine: FSMMachine = { ...rootMachine, transitions: newTransitions };

            return {
                ...fsm,
                machines: new Map([...fsm.machines, [fsm.rootMachineId, newMachine]]),
            };
        });
        return result || state;
    }),

    // Variable actions
    addVariable: (isLocal, variable) => set((state) => {
        const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
            if (isLocal) {
                return { ...fsm, localVariables: [...fsm.localVariables, variable] };
            }
            return { ...fsm, sharedVariables: [...fsm.sharedVariables, variable] };
        });
        return result || state;
    }),

    removeVariable: (isLocal, name) => set((state) => {
        const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
            if (isLocal) {
                return { ...fsm, localVariables: fsm.localVariables.filter(v => v.name !== name) };
            }
            return { ...fsm, sharedVariables: fsm.sharedVariables.filter(v => v.name !== name) };
        });
        return result || state;
    }),

    updateVariable: (isLocal, name, updates) => set((state) => {
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
    }),

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
        const fsm = createEmptyFSM(name);
        const path = `new://${name}.fsm`;
        const snapshot = serializeFSMForEditor(fsm);

        const newFile: OpenedFSMFile = {
            path,
            name,
            fsm,
            lastSavedSnapshot: snapshot,
            isDirty: false,
            isNew: true,
            history: { past: [], future: [] },
            currentMachineId: fsm.rootMachineId,
        };

        set((state) => ({
            openedFSMFiles: [...state.openedFSMFiles, newFile],
            activeFSMPath: path,
        }));
    },
}));
