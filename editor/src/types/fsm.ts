/**
 * FSM (Finite State Machine) Data Types
 * 
 * These types define the data model for the FSM editor.
 */

import type { Variable } from './index';

// ==================== FSM State Types ====================

export type FSMStateType = 'Normal' | 'Meta' | 'Entry' | 'Exit' | 'Any' | 'Upper';

export interface FSMState {
    id: string;
    type: FSMStateType;
    name: string;               // User-defined name (required for Normal/Meta)
    tree?: string;              // Path to .tree file (Normal/Entry/Exit)
    position: { x: number; y: number };
    comment?: string;
}

// ==================== FSM Transition Types ====================

export type FSMTransitionType = 'Normal' | 'Default' | 'Entry' | 'Exit';

export interface FSMTransition {
    id: string;
    fromStateId: string | null;  // null = 'Any' state
    toStateId: string;
    conditions: string[];        // Array of condition strings
    type: FSMTransitionType;
}

// ==================== FSM Machine Types ====================

export interface FSMMachine {
    id: string;
    level: number;                   // 0 for root
    defaultStateId: string | null;
    states: Map<string, FSMState>;
    transitions: FSMTransition[];
    parentMetaStateId?: string;      // For sub-machines
}

// ==================== FSM Root Type ====================

export interface FSM {
    name: string;
    rootMachineId: string;
    machines: Map<string, FSMMachine>;
    sharedVariables: Variable[];
    localVariables: Variable[];
}

// ==================== Special State Defaults ====================

export const FSM_SPECIAL_STATE_POSITIONS: Record<FSMStateType, { x: number; y: number }> = {
    Entry: { x: 100, y: 100 },
    Exit: { x: 100, y: 400 },
    Any: { x: 100, y: 250 },
    Upper: { x: 400, y: 100 },
    Normal: { x: 300, y: 200 },  // Default for user-created
    Meta: { x: 300, y: 200 },    // Default for user-created
};

/**
 * Check if a state type is a special (auto-created, non-deletable) state
 */
export function isSpecialStateType(type: FSMStateType): boolean {
    return type === 'Entry' || type === 'Exit' || type === 'Any' || type === 'Upper';
}

/**
 * Check if a state type has a tree selector
 */
export function stateTypeHasTreeSelector(type: FSMStateType): boolean {
    return type === 'Normal' || type === 'Entry' || type === 'Exit';
}

/**
 * Check if a state type has a name field
 */
export function stateTypeHasNameField(type: FSMStateType): boolean {
    return type === 'Normal' || type === 'Meta';
}

/**
 * Check if a state type has a parent connector (can receive transitions)
 */
export function stateTypeHasParentConnector(type: FSMStateType): boolean {
    return type === 'Normal' || type === 'Meta' || type === 'Exit' || type === 'Upper';
}

/**
 * Check if a state type has a children connector (can send transitions)
 */
export function stateTypeHasChildrenConnector(type: FSMStateType): boolean {
    return type === 'Normal' || type === 'Meta' || type === 'Entry' || type === 'Any' || type === 'Upper';
}

// ==================== Factory Functions ====================

let stateIdCounter = 0;
let machineIdCounter = 0;
let transitionIdCounter = 0;

export function generateStateId(): string {
    return `state_${Date.now()}_${++stateIdCounter}`;
}

export function generateMachineId(): string {
    return `machine_${Date.now()}_${++machineIdCounter}`;
}

export function generateTransitionId(): string {
    return `trans_${Date.now()}_${++transitionIdCounter}`;
}

/**
 * Create a new FSM state
 */
export function createFSMState(
    type: FSMStateType,
    name: string = '',
    position?: { x: number; y: number }
): FSMState {
    const defaultName = isSpecialStateType(type)
        ? type
        : (type === 'Meta' ? 'Meta' : 'State');

    return {
        id: generateStateId(),
        type,
        name: name || defaultName,
        position: position || { ...FSM_SPECIAL_STATE_POSITIONS[type] },
    };
}

/**
 * Create a new FSM machine with default special states
 */
export function createFSMMachine(level: number = 0, parentMetaStateId?: string): FSMMachine {
    const machine: FSMMachine = {
        id: generateMachineId(),
        level,
        defaultStateId: null,
        states: new Map(),
        transitions: [],
        parentMetaStateId,
    };

    // Add Entry, Exit, Any (always)
    const entry = createFSMState('Entry');
    const exit = createFSMState('Exit');
    const any = createFSMState('Any');

    machine.states.set(entry.id, entry);
    machine.states.set(exit.id, exit);
    machine.states.set(any.id, any);

    // Add Upper for sub-machines only
    if (level > 0) {
        const upper = createFSMState('Upper');
        machine.states.set(upper.id, upper);
    }

    return machine;
}

/**
 * Create a new empty FSM
 */
export function createEmptyFSM(name: string): FSM {
    const rootMachine = createFSMMachine(0);

    return {
        name,
        rootMachineId: rootMachine.id,
        machines: new Map([[rootMachine.id, rootMachine]]),
        sharedVariables: [],
        localVariables: [],
    };
}

/**
 * Create a new FSM transition
 */
export function createFSMTransition(
    fromStateId: string | null,
    toStateId: string,
    conditions: string[] = [],
    type: FSMTransitionType = 'Normal'
): FSMTransition {
    return {
        id: generateTransitionId(),
        fromStateId,
        toStateId,
        conditions,
        type,
    };
}
