/**
 * FSM XML Serializer
 * 
 * Serializes FSM data model to XML format for saving .fsm files.
 */

import type { Variable } from '../types';
import type { FSM, FSMMachine, FSMState, FSMTransition } from '../types/fsm';
import { serializeVariable } from './xmlSerializer';

// ==================== Helper Functions ====================

function formatXml(xml: string): string {
    let formatted = '';
    let indent = '';
    const tab = '  ';

    xml.split(/>\s*</).forEach((node) => {
        if (node.match(/^\/\w/)) {
            indent = indent.substring(tab.length);
        }
        formatted += indent + '<' + node + '>\r\n';
        if (node.match(/^<?\w[^>]*[^\/]$/)) {
            indent += tab;
        }
    });

    return formatted.substring(1, formatted.length - 3);
}

function positionToString(pos: { x: number; y: number }): string {
    return `${Math.round(pos.x)},${Math.round(pos.y)}`;
}

// ==================== Serializer Functions ====================

function serializeState(
    state: FSMState,
    machine: FSMMachine,
    fsm: FSM,
    doc: Document
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
        el.setAttribute('Tree', state.tree);
    }

    // Position
    el.setAttribute('Pos', positionToString(state.position));

    // Comment
    if (state.comment) {
        el.setAttribute('Comment', state.comment);
    }

    // Handle Meta state (nested machine)
    if (state.type === 'Meta') {
        const subMachine = [...fsm.machines.values()].find(m => m.parentMetaStateId === state.id);
        if (subMachine) {
            const machineEl = serializeMachine(subMachine, fsm, doc);
            el.appendChild(machineEl);
        }
    }

    return el;
}

function serializeTransition(
    trans: FSMTransition,
    machine: FSMMachine,
    doc: Document
): Element {
    const el = doc.createElement('Trans');

    // Type for Entry/Exit transitions
    if (trans.type === 'Entry') {
        el.setAttribute('Type', 'Entry');
    } else if (trans.type === 'Exit') {
        el.setAttribute('Type', 'Exit');
    }

    // From state (skip for Entry transitions and Any state)
    if (trans.fromStateId && trans.type !== 'Entry') {
        const fromState = machine.states.get(trans.fromStateId);
        if (fromState && fromState.type !== 'Any') {
            el.setAttribute('From', fromState.type === 'Normal' || fromState.type === 'Meta' ? fromState.name : fromState.type);
        }
    }

    // To state (skip for Exit transitions)
    if (trans.type !== 'Exit') {
        const toState = machine.states.get(trans.toStateId);
        if (toState) {
            el.setAttribute('To', toState.type === 'Normal' || toState.type === 'Meta' ? toState.name : toState.type);
        }
    }

    // Events as child elements
    for (const event of trans.events) {
        const eventEl = doc.createElement(event);
        el.appendChild(eventEl);
    }

    return el;
}

function serializeMachine(
    machine: FSMMachine,
    fsm: FSM,
    doc: Document
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

    for (const state of sortedStates) {
        el.appendChild(serializeState(state, machine, fsm, doc));
    }

    // Serialize transitions
    for (const trans of machine.transitions) {
        el.appendChild(serializeTransition(trans, machine, doc));
    }

    return el;
}

/**
 * Serialize FSM to editor XML format
 */
export function serializeFSMForEditor(fsm: FSM): string {
    const doc = document.implementation.createDocument(null, null, null);
    const root = doc.createElement(fsm.name);

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
        root.appendChild(serializeMachine(rootMachine, fsm, doc));
    }

    doc.appendChild(root);
    const serializer = new XMLSerializer();
    const xmlStr = '<?xml version="1.0" encoding="utf-8"?>\r\n' + serializer.serializeToString(doc);

    return formatXml(xmlStr);
}

/**
 * Serialize FSM to runtime XML format (same as editor for FSM)
 */
export function serializeFSMForRuntime(fsm: FSM): string {
    // For FSM, runtime format is the same as editor format
    return serializeFSMForEditor(fsm);
}
