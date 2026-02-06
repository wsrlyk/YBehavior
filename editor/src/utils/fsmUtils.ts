import type { FSM, FSMMachine } from '../types/fsm';

/**
 * Recalculate runtime UIDs for FSM states.
 * Matches legacy YBehaviorEditor logic:
 * - Depth-first traversal of machines (but assigns UIDs level-by-level within a machine first)
 * - Order: Machine.States iteration -> then SubMachines
 */
export function recalculateFSMUIDs(fsm: FSM): FSM {
    // Deep clone FSM structure (Machines and States) to avoid mutating history
    const newFSM: FSM = { ...fsm, machines: new Map() };

    for (const [id, machine] of fsm.machines) {
        const newMachine = { ...machine, states: new Map() };
        for (const [sId, state] of machine.states) {
            newMachine.states.set(sId, { ...state });
        }
        newFSM.machines.set(id, newMachine);
    }

    let currentUid = 0;

    const traverseMachine = (machine: FSMMachine) => {
        // 1. Assign UIDs to all states in this machine
        // Sorted by type (Special) then Name (User) to match serializer/runtime order
        const specialOrder = ['Entry', 'Exit', 'Any', 'Upper'];
        const sortedStates = [...machine.states.values()].sort((a, b) => {
            const aIdx = specialOrder.indexOf(a.type);
            const bIdx = specialOrder.indexOf(b.type);
            if (aIdx !== -1 && bIdx !== -1) return aIdx - bIdx;
            if (aIdx !== -1) return -1;
            if (bIdx !== -1) return 1;
            return (a.name || '').localeCompare(b.name || '');
        });

        for (const state of sortedStates) {
            state.uid = ++currentUid;
        }

        // 2. Recurse into Meta states
        for (const state of sortedStates) {
            if (state.type === 'Meta') {
                const subMachine = Array.from(newFSM.machines.values()).find(m => m.parentMetaStateId === state.id);
                if (subMachine) {
                    traverseMachine(subMachine);
                }
            }
        }
    };

    const root = newFSM.machines.get(newFSM.rootMachineId);
    if (root) {
        traverseMachine(root);
    }

    return newFSM;
}
