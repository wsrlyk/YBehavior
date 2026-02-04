/**
 * FSM XML Parser
 * 
 * Parses .fsm files in the legacy XML format into the TypeScript FSM data model.
 */

import { XMLParser } from 'fast-xml-parser';
import type { Variable } from '../types';
import type {
    FSM,
    FSMMachine,
    FSMState,
    FSMStateType,
    FSMTransition,
    FSMTransitionType,
} from '../types/fsm';
import {
    generateStateId,
    generateMachineId,
    generateTransitionId,
    FSM_SPECIAL_STATE_POSITIONS,
} from '../types/fsm';
import { parseVariable as parseVariableFromXml } from './xmlParser';

// ==================== XML Types ====================

interface XmlFSMState {
    '@_Type'?: string;
    '@_Name'?: string;
    '@_Tree'?: string;
    '@_Pos'?: string;
    '@_Comment'?: string;
    Machine?: XmlFSMMachine;
}

interface XmlFSMTrans {
    '@_Type'?: string;
    '@_From'?: string;
    '@_To'?: string;
    [key: string]: unknown; // Event children (e.g., <OnDamage/>)
}

interface XmlFSMMachine {
    '@_Default'?: string;
    State?: XmlFSMState | XmlFSMState[];
    Trans?: XmlFSMTrans | XmlFSMTrans[];
    EntryTrans?: XmlFSMTrans | XmlFSMTrans[];
    ExitTrans?: XmlFSMTrans | XmlFSMTrans[];
}

interface XmlFSMRoot {
    Shared?: Record<string, string>;
    Local?: Record<string, string>;
    Machine?: XmlFSMMachine;
}

// ==================== Parser Functions ====================

function parsePosition(posStr?: string): { x: number; y: number } {
    if (!posStr) return { x: 200, y: 200 };
    const parts = posStr.split(',');
    return {
        x: parseInt(parts[0], 10) || 200,
        y: parseInt(parts[1], 10) || 200,
    };
}

function getStateType(typeAttr?: string): FSMStateType {
    if (!typeAttr) return 'Normal';
    switch (typeAttr) {
        case 'Entry': return 'Entry';
        case 'Exit': return 'Exit';
        case 'Any': return 'Any';
        case 'Upper': return 'Upper';
        case 'Meta': return 'Meta';
        default: return 'Normal';
    }
}

function extractEvents(trans: XmlFSMTrans): string[] {
    const events: string[] = [];
    for (const key of Object.keys(trans)) {
        if (!key.startsWith('@_') && key !== '#text') {
            events.push(key);
        }
    }
    return events;
}

/**
 * Parse a single FSM machine from XML
 */
function parseMachine(
    xmlMachine: XmlFSMMachine,
    level: number,
    machines: Map<string, FSMMachine>,
    parentMetaStateId?: string
): FSMMachine {
    const machine: FSMMachine = {
        id: generateMachineId(),
        level,
        defaultStateId: null,
        states: new Map(),
        transitions: [],
        parentMetaStateId,
    };

    // Create special states with default positions
    const specialTypes: FSMStateType[] = ['Entry', 'Exit', 'Any'];
    if (level > 0) specialTypes.push('Upper');

    for (const type of specialTypes) {
        const state: FSMState = {
            id: generateStateId(),
            type,
            name: type,
            position: { ...FSM_SPECIAL_STATE_POSITIONS[type] },
        };
        machine.states.set(state.id, state);
    }

    // Parse states from XML
    const xmlStates = xmlMachine.State
        ? (Array.isArray(xmlMachine.State) ? xmlMachine.State : [xmlMachine.State])
        : [];

    // Map from state name to state ID (for transitions)
    const stateNameToId = new Map<string, string>();

    for (const xmlState of xmlStates) {
        const stateType = getStateType(xmlState['@_Type']);
        const isSpecial = ['Entry', 'Exit', 'Any', 'Upper'].includes(stateType);

        if (isSpecial) {
            // Update existing special state
            const existingState = [...machine.states.values()].find(s => s.type === stateType);
            if (existingState) {
                if (xmlState['@_Pos']) {
                    existingState.position = parsePosition(xmlState['@_Pos']);
                }
                if (xmlState['@_Tree']) {
                    existingState.tree = xmlState['@_Tree'];
                }
                if (xmlState['@_Comment']) {
                    existingState.comment = xmlState['@_Comment'];
                }
                stateNameToId.set(stateType, existingState.id);
            }
        } else {
            // Create new user state
            const state: FSMState = {
                id: generateStateId(),
                type: stateType,
                name: xmlState['@_Name'] || '',
                tree: xmlState['@_Tree'],
                position: parsePosition(xmlState['@_Pos']),
                comment: xmlState['@_Comment'],
            };
            machine.states.set(state.id, state);
            stateNameToId.set(state.name, state.id);

            // Handle Meta state (nested machine)
            if (stateType === 'Meta' && xmlState.Machine) {
                const subMachine = parseMachine(xmlState.Machine, level + 1, machines, state.id);
                machines.set(subMachine.id, subMachine);
            }
        }
    }

    // Set default state
    if (xmlMachine['@_Default']) {
        const defaultStateId = stateNameToId.get(xmlMachine['@_Default']);
        if (defaultStateId) {
            machine.defaultStateId = defaultStateId;
        }
    }

    // Parse transitions
    const xmlTrans = xmlMachine.Trans
        ? (Array.isArray(xmlMachine.Trans) ? xmlMachine.Trans : [xmlMachine.Trans])
        : [];

    for (const trans of xmlTrans) {
        const fromName = trans['@_From'];
        const toName = trans['@_To'];
        const events = extractEvents(trans);

        // Determine transition type
        let transType: FSMTransitionType = 'Normal';
        if (trans['@_Type'] === 'Entry') transType = 'Entry';
        else if (trans['@_Type'] === 'Exit') transType = 'Exit';

        const fromStateId = fromName ? stateNameToId.get(fromName) || null : null;
        const toStateId = toName ? stateNameToId.get(toName) : undefined;

        if (toStateId) {
            const transition: FSMTransition = {
                id: generateTransitionId(),
                fromStateId,
                toStateId,
                events,
                type: transType,
            };
            machine.transitions.push(transition);
        }
    }

    // Parse Entry transitions
    const entryTrans = xmlMachine.EntryTrans
        ? (Array.isArray(xmlMachine.EntryTrans) ? xmlMachine.EntryTrans : [xmlMachine.EntryTrans])
        : [];

    for (const trans of entryTrans) {
        const toName = trans['@_To'];
        const events = extractEvents(trans);
        const entryState = [...machine.states.values()].find(s => s.type === 'Entry');
        const toStateId = toName ? stateNameToId.get(toName) : undefined;

        if (entryState && toStateId) {
            machine.transitions.push({
                id: generateTransitionId(),
                fromStateId: entryState.id,
                toStateId,
                events,
                type: 'Entry',
            });
        }
    }

    // Parse Exit transitions
    const exitTrans = xmlMachine.ExitTrans
        ? (Array.isArray(xmlMachine.ExitTrans) ? xmlMachine.ExitTrans : [xmlMachine.ExitTrans])
        : [];

    for (const trans of exitTrans) {
        const fromName = trans['@_From'];
        const events = extractEvents(trans);
        const exitState = [...machine.states.values()].find(s => s.type === 'Exit');
        const fromStateId = fromName ? stateNameToId.get(fromName) || null : null;

        if (exitState) {
            machine.transitions.push({
                id: generateTransitionId(),
                fromStateId,
                toStateId: exitState.id,
                events,
                type: 'Exit',
            });
        }
    }

    machines.set(machine.id, machine);
    return machine;
}

/**
 * Parse FSM XML content into an FSM data model
 */
export function parseFSMXml(xmlContent: string, fileName: string): FSM {
    const parser = new XMLParser({
        ignoreAttributes: false,
        attributeNamePrefix: '@_',
        allowBooleanAttributes: true,
        parseAttributeValue: false,
    });

    const parsed = parser.parse(xmlContent);
    const rootTagName = Object.keys(parsed).find(k => k !== '?xml') || 'FSM';
    const xmlRoot: XmlFSMRoot = parsed[rootTagName] || {};

    // Parse variables
    const sharedVariables: Variable[] = [];
    const localVariables: Variable[] = [];

    if (xmlRoot.Shared) {
        for (const [name, value] of Object.entries(xmlRoot.Shared)) {
            if (typeof value === 'string') {
                sharedVariables.push(parseVariableFromXml(name, value, false));
            }
        }
    }

    if (xmlRoot.Local) {
        for (const [name, value] of Object.entries(xmlRoot.Local)) {
            if (typeof value === 'string') {
                localVariables.push(parseVariableFromXml(name, value, true));
            }
        }
    }

    // Parse machines
    const machines = new Map<string, FSMMachine>();
    let rootMachineId = '';

    if (xmlRoot.Machine) {
        const rootMachine = parseMachine(xmlRoot.Machine, 0, machines);
        rootMachineId = rootMachine.id;
    } else {
        // Create empty root machine if none exists
        const emptyMachine: FSMMachine = {
            id: generateMachineId(),
            level: 0,
            defaultStateId: null,
            states: new Map(),
            transitions: [],
        };

        // Add default special states
        for (const type of ['Entry', 'Exit', 'Any'] as FSMStateType[]) {
            const state: FSMState = {
                id: generateStateId(),
                type,
                name: type,
                position: { ...FSM_SPECIAL_STATE_POSITIONS[type] },
            };
            emptyMachine.states.set(state.id, state);
        }

        machines.set(emptyMachine.id, emptyMachine);
        rootMachineId = emptyMachine.id;
    }

    return {
        name: fileName.replace(/\.fsm$/i, ''),
        rootMachineId,
        machines,
        sharedVariables,
        localVariables,
    };
}
