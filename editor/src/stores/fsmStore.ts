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
import { serializeFSMForEditor } from '../utils/fsmSerializer';

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

    // History
    undo: () => void;
    redo: () => void;

    // Save
    saveFSM: () => Promise<string>;
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

    setIsConnecting: (isConnecting) => set({ isConnecting }),

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
            const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
            if (!file) return fsm;

            const machine = fsm.machines.get(file.currentMachineId);
            if (!machine) return fsm;

            const trans = createFSMTransition(fromStateId, toStateId, events);
            const newMachine: FSMMachine = {
                ...machine,
                transitions: [...machine.transitions, trans],
            };

            return {
                ...fsm,
                machines: new Map([...fsm.machines, [machine.id, newMachine]]),
            };
        });
        return result || state;
    }),

    removeTransition: (transitionId) => set((state) => {
        const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
            const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
            if (!file) return fsm;

            const machine = fsm.machines.get(file.currentMachineId);
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
            const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
            if (!file) return fsm;

            const machine = fsm.machines.get(file.currentMachineId);
            if (!machine) return fsm;

            const newTransitions = machine.transitions.map(t =>
                t.id === transitionId
                    ? { ...t, events: [...t.events, event] }
                    : t
            );

            const newMachine: FSMMachine = { ...machine, transitions: newTransitions };

            return {
                ...fsm,
                machines: new Map([...fsm.machines, [machine.id, newMachine]]),
            };
        });
        return result || state;
    }),

    removeEventFromTransition: (transitionId, event) => set((state) => {
        const result = updateFSMFile(state.openedFSMFiles, state.activeFSMPath, (fsm) => {
            const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
            if (!file) return fsm;

            const machine = fsm.machines.get(file.currentMachineId);
            if (!machine) return fsm;

            const newTransitions = machine.transitions.map(t =>
                t.id === transitionId
                    ? { ...t, events: t.events.filter(e => e !== event) }
                    : t
            );

            const newMachine: FSMMachine = { ...machine, transitions: newTransitions };

            return {
                ...fsm,
                machines: new Map([...fsm.machines, [machine.id, newMachine]]),
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
        if (!file) throw new Error('No active FSM file');

        const content = serializeFSMForEditor(file.fsm);

        set({
            openedFSMFiles: openedFSMFiles.map(f =>
                f.path === activeFSMPath
                    ? { ...f, lastSavedSnapshot: content, isDirty: false }
                    : f
            ),
        });

        return content;
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
