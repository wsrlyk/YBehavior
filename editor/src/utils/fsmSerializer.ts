/**
 * FSM XML Serializer
 * 
 * Serializes FSM data model to XML format for saving .fsm files.
 */

import type { FSM, FSMMachine, FSMState, FSMTransition } from '../types/fsm';
import { serializeVariable } from './xmlSerializer';
import { stripExtension } from './fileUtils';

// ==================== Helper Functions ====================

function formatXml(xml: string): string {
    const tab = '  ';
    const lines: string[] = [];
    let depth = 0;

    // Split by tag boundaries
    const parts = xml.split(/(<[^>]+>)/);

    for (const part of parts) {
        const trimmed = part.trim();
        if (!trimmed) continue;

        // XML declaration - no indent, no depth change
        if (trimmed.startsWith('<?')) {
            lines.push(trimmed);
            continue;
        }

        // Closing tag - decrease depth first, then write
        if (trimmed.startsWith('</')) {
            depth = Math.max(0, depth - 1);
            lines.push(tab.repeat(depth) + trimmed);
            continue;
        }

        // Self-closing tag - write at current depth, no depth change
        if (trimmed.startsWith('<') && trimmed.endsWith('/>')) {
            lines.push(tab.repeat(depth) + trimmed);
            continue;
        }

        // Opening tag - write at current depth, then increase depth
        if (trimmed.startsWith('<')) {
            lines.push(tab.repeat(depth) + trimmed);
            depth++;
            continue;
        }

        // Text content (shouldn't happen for FSM, but handle it)
        lines.push(tab.repeat(depth) + trimmed);
    }

    return lines.join('\r\n');
}

function positionToString(pos: { x: number; y: number }): string {
    return `${Math.round(pos.x)},${Math.round(pos.y)}`;
}

// ==================== Serializer Functions ====================

function serializeState(
    state: FSMState,
    _machine: FSMMachine,
    fsm: FSM,
    doc: Document,
    forEditor: boolean
): Element {
    const el = doc.createElement('State');

    // Special states always have Type attribute
    if (state.type !== 'Normal') {
        el.setAttribute('Type', state.type);
    }

    // Name for user states
    if (state.type === 'Normal' || state.type === 'Meta') {
        if (state.name) {
            el.setAttribute('Name', state.name);
        }
    }

    // Tree for states that support it
    if (state.tree) {
        el.setAttribute('Tree', stripExtension(state.tree));
    }

    if (forEditor) {
        // Position
        el.setAttribute('Pos', positionToString(state.position));

        // Comment
        if (state.comment) {
            el.setAttribute('Comment', state.comment);
        }
    }

    // Handle Meta state (nested machine)
    if (state.type === 'Meta') {
        const subMachine = [...fsm.machines.values()].find(m => m.parentMetaStateId === state.id);
        if (subMachine) {
            const machineEl = serializeMachine(subMachine, fsm, doc, forEditor);
            el.appendChild(machineEl);
        }
    }

    return el;
}

function serializeTransition(
    trans: FSMTransition,
    fsm: FSM,
    doc: Document
): Element {
    const el = doc.createElement('Trans');

    // Type for Entry/Exit transitions
    if (trans.type === 'Entry') {
        el.setAttribute('Type', 'Entry');
    } else if (trans.type === 'Exit') {
        el.setAttribute('Type', 'Exit');
    }

    // Helper to find state name globally
    const findStateName = (stateId: string): string | null => {
        for (const m of fsm.machines.values()) {
            const state = m.states.get(stateId);
            if (state) {
                if (state.type === 'Normal' || state.type === 'Meta') return state.name;
                return state.type;
            }
        }
        return null;
    };

    // From state (skip for Entry transitions and Any state)
    if (trans.fromStateId && trans.type !== 'Entry') {
        const fromName = findStateName(trans.fromStateId);
        if (fromName && fromName !== 'Any') {
            el.setAttribute('From', fromName);
        }
    }

    // To state (skip for Exit transitions)
    if (trans.type !== 'Exit') {
        const toName = findStateName(trans.toStateId);
        if (toName && toName !== 'Exit') {
            el.setAttribute('To', toName);
        }
    }

    // Conditions as child elements
    for (const condition of trans.conditions) {
        const condEl = doc.createElement(condition);
        el.appendChild(condEl);
    }

    return el;
}

function serializeMachine(
    machine: FSMMachine,
    fsm: FSM,
    doc: Document,
    forEditor: boolean
): Element {
    const el = doc.createElement('Machine');

    // Default state
    if (machine.defaultStateId) {
        const defaultState = machine.states.get(machine.defaultStateId);
        if (defaultState && (defaultState.type === 'Normal' || defaultState.type === 'Meta')) {
            el.setAttribute('Default', defaultState.name);
        }
    }

    // Serialize states (special states first, then user states)
    const specialOrder = ['Entry', 'Exit', 'Any', 'Upper'];
    const sortedStates = [...machine.states.values()].sort((a, b) => {
        const aIdx = specialOrder.indexOf(a.type);
        const bIdx = specialOrder.indexOf(b.type);
        if (aIdx !== -1 && bIdx !== -1) return aIdx - bIdx;
        if (aIdx !== -1) return -1;
        if (bIdx !== -1) return 1;
        return (a.name || '').localeCompare(b.name || '');
    });

    const filteredStates = sortedStates.filter(s => {
        if (forEditor) return true;
        // Runtime version doesn't need Any or Upper
        return s.type !== 'Any' && s.type !== 'Upper';
    });

    for (const state of filteredStates) {
        el.appendChild(serializeState(state, machine, fsm, doc, forEditor));
    }

    // Serialize transitions (Normal transitions are only serialized in the root machine in our global model)
    // Actually, to keep XML hierarchical, we might want to split them, but our current store has them all in rootMachine.
    // Transition's From/To names will resolve correctly even if they belong to sub-machines.
    if (machine.id === fsm.rootMachineId) {
        for (const trans of machine.transitions) {
            el.appendChild(serializeTransition(trans, fsm, doc));
        }
    }

    return el;
}

/**
 * Serialize FSM to editor XML format
 */
export function serializeFSMForEditor(fsm: FSM, forEditor: boolean = true): string {
    const doc = document.implementation.createDocument(null, null, null);
    const root = doc.createElement(fsm.name);

    if (forEditor) {
        root.setAttribute('IsEditor', '');
    }

    // Shared variables
    if (fsm.sharedVariables.length > 0) {
        const sharedEl = doc.createElement('Shared');
        for (const v of fsm.sharedVariables) {
            sharedEl.setAttribute(v.name, serializeVariable(v));
        }
        root.appendChild(sharedEl);
    }

    // Local variables
    if (fsm.localVariables.length > 0) {
        const localEl = doc.createElement('Local');
        for (const v of fsm.localVariables) {
            localEl.setAttribute(v.name, serializeVariable(v));
        }
        root.appendChild(localEl);
    }

    // Root machine
    const rootMachine = fsm.machines.get(fsm.rootMachineId);
    if (rootMachine) {
        root.appendChild(serializeMachine(rootMachine, fsm, doc, forEditor));
    }

    doc.appendChild(root);
    const serializer = new XMLSerializer();
    const xmlStr = '<?xml version="1.0" encoding="utf-8"?>\r\n' + serializer.serializeToString(doc);

    // UTF-8 BOM + formatted XML
    const BOM = '\uFEFF';
    return BOM + formatXml(xmlStr);
}

/**
 * Serialize FSM to runtime XML format
 */
export function serializeFSMForRuntime(fsm: FSM): string {
    return serializeFSMForEditor(fsm, false);
}
