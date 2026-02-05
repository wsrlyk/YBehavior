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
    const x = parseFloat(parts[0]);
    const y = parseFloat(parts[1]);
    return {
        x: isNaN(x) ? 200 : x,
        y: isNaN(y) ? 200 : y,
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

interface RawTransitionData {
    machineId: string;
    xml: XmlFSMTrans;
    type: FSMTransitionType;
}

/**
 * Parse a single FSM machine from XML (Pass 1: States and Machines)
 */
function parseMachine(
    xmlMachine: XmlFSMMachine,
    level: number,
    machines: Map<string, FSMMachine>,
    globalStateNames: Map<string, string>,
    allRawTransitions: RawTransitionData[],
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

    // Local map for current machine resolution prioritizing local names
    const localStateNames = new Map<string, string>();
    // Pre-populate entries/exits/any for this machine
    for (const s of machine.states.values()) {
        localStateNames.set(s.type, s.id);
    }

    for (const xmlState of xmlStates) {
        const stateType = getStateType(xmlState['@_Type']);
        const isSpecial = ['Entry', 'Exit', 'Any', 'Upper'].includes(stateType);

        if (isSpecial) {
            const existingState = [...machine.states.values()].find(s => s.type === stateType);
            if (existingState) {
                if (xmlState['@_Pos']) existingState.position = parsePosition(xmlState['@_Pos']);
                if (xmlState['@_Tree']) existingState.tree = xmlState['@_Tree'];
                if (xmlState['@_Comment']) existingState.comment = xmlState['@_Comment'];
                localStateNames.set(stateType, existingState.id);
            }
        } else {
            const state: FSMState = {
                id: generateStateId(),
                type: stateType,
                name: xmlState['@_Name'] || '',
                tree: xmlState['@_Tree'],
                position: parsePosition(xmlState['@_Pos']),
                comment: xmlState['@_Comment'],
            };
            machine.states.set(state.id, state);
            if (state.name) {
                localStateNames.set(state.name, state.id);
                // Also add to global map for cross-layer lookups
                if (!globalStateNames.has(state.name)) {
                    globalStateNames.set(state.name, state.id);
                }
            }

            if (stateType === 'Meta' && xmlState.Machine) {
                const subMachine = parseMachine(xmlState.Machine, level + 1, machines, globalStateNames, allRawTransitions, state.id);
                machines.set(subMachine.id, subMachine);
            }
        }
    }

    // Set default state
    if (xmlMachine['@_Default']) {
        const defaultStateId = localStateNames.get(xmlMachine['@_Default']) || globalStateNames.get(xmlMachine['@_Default']);
        if (defaultStateId) {
            machine.defaultStateId = defaultStateId;
        }
    }

    // Collect Raw Transitions
    const collectRaw = (list: XmlFSMTrans | XmlFSMTrans[] | undefined, type: FSMTransitionType) => {
        if (!list) return;
        const items = Array.isArray(list) ? list : [list];
        for (const i of items) {
            allRawTransitions.push({ machineId: machine.id, xml: i, type });
        }
    };

    collectRaw(xmlMachine.Trans, 'Normal');
    collectRaw(xmlMachine.EntryTrans, 'Entry');
    collectRaw(xmlMachine.ExitTrans, 'Exit');

    machines.set(machine.id, machine);
    return machine;
}

function resolveTransitions(
    machines: Map<string, FSMMachine>,
    allRawTransitions: RawTransitionData[],
    globalStateNames: Map<string, string>,
    rootMachineId: string
) {
    const rootMachine = machines.get(rootMachineId);
    if (!rootMachine) return;

    for (const raw of allRawTransitions) {
        const fromName = raw.xml['@_From'];
        const toName = raw.xml['@_To'];
        const events = extractEvents(raw.xml);

        // Detect if this is an Any-sourced transition
        const explicitType = raw.xml['@_Type'];
        const isAnySource = (!fromName && !explicitType && raw.type === 'Normal') || explicitType === 'Any';
        const isEntrySource = raw.type === 'Entry' || explicitType === 'Entry';

        // Resolve From State globally
        let fromStateId: string | null = null;
        if (fromName) {
            fromStateId = globalStateNames.get(fromName) || null;
        }
        // If no fromName: null means Any/Entry source (will be projected per-layer in editor)

        // Resolve To State globally
        let toStateId: string | undefined;
        if (toName) {
            toStateId = globalStateNames.get(toName);
        }

        // Exit transitions target Exit state
        if (!toStateId && (raw.type === 'Exit' || explicitType === 'Exit')) {
            // Find any Exit state (they're all equivalent for global storage)
            for (const m of machines.values()) {
                const exitState = [...m.states.values()].find(s => s.type === 'Exit');
                if (exitState) {
                    toStateId = exitState.id;
                    break;
                }
            }
        }

        if (toStateId) {
            // Store all transitions in root machine (global storage)
            rootMachine.transitions.push({
                id: generateTransitionId(),
                fromStateId, // null = Any or Entry source
                toStateId,
                events,
                type: isAnySource ? 'Normal' : (isEntrySource ? 'Entry' : raw.type),
            });
        }
    }
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

    const sharedVariables: Variable[] = [];
    const localVariables: Variable[] = [];

    if (xmlRoot.Shared) {
        for (const [name, value] of Object.entries(xmlRoot.Shared)) {
            if (typeof value === 'string') sharedVariables.push(parseVariableFromXml(name, value, false));
        }
    }
    if (xmlRoot.Local) {
        for (const [name, value] of Object.entries(xmlRoot.Local)) {
            if (typeof value === 'string') localVariables.push(parseVariableFromXml(name, value, true));
        }
    }

    const machines = new Map<string, FSMMachine>();
    const globalStateNames = new Map<string, string>();
    const allRawTransitions: RawTransitionData[] = [];
    let rootMachineId = '';

    if (xmlRoot.Machine) {
        const rootMachine = parseMachine(xmlRoot.Machine, 0, machines, globalStateNames, allRawTransitions);
        rootMachineId = rootMachine.id;

        // Pass 2: Resolve all transitions
        resolveTransitions(machines, allRawTransitions, globalStateNames, rootMachineId);
    } else {
        const emptyMachine: FSMMachine = {
            id: generateMachineId(),
            level: 0,
            defaultStateId: null,
            states: new Map(),
            transitions: [],
        };
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
