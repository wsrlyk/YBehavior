/**
 * FSM Editor Component
 * 
 * Main React Flow-based editor for FSM state machines.
 */

import { useCallback, useMemo, useEffect } from 'react';
import {
    ReactFlow,
    Background,
    Controls,
    MiniMap,
    useNodesState,
    useEdgesState,
    type Node,
    type Edge,
    type Connection,
    type OnConnect,
    type NodeTypes,
    Panel,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import FSMStateNode, { type FSMStateNodeData, type FSMStateNodeType } from './FSMStateNode';
import FSMTransitionEdge from './FSMTransitionEdge';
import { useFSMStore } from '../stores/fsmStore';
import type { FSMMachine, FSMTransition } from '../types/fsm';

// ==================== Node & Edge Types ====================

const nodeTypes: NodeTypes = {
    fsmState: FSMStateNode,
};

const edgeTypes = {
    fsmTransition: FSMTransitionEdge,
};


// ==================== Conversion Functions ====================

function convertStatesToNodes(machine: FSMMachine): FSMStateNodeType[] {
    const nodes: FSMStateNodeType[] = [];

    for (const [stateId, state] of machine.states) {
        nodes.push({
            id: stateId,
            type: 'fsmState',
            position: state.position,
            data: {
                state,
                isDefault: machine.defaultStateId === stateId,
            },
        });
    }

    return nodes;
}

function convertTransitionsToEdges(machine: FSMMachine): Edge[] {
    const edges: Edge[] = [];
    // Key: sourceId->targetId
    const groups = new Map<string, FSMTransition[]>();

    for (const trans of machine.transitions) {
        if (!trans.fromStateId) continue;
        const key = `${trans.fromStateId}->${trans.toStateId}`;
        if (!groups.has(key)) {
            groups.set(key, []);
        }
        groups.get(key)!.push(trans);
    }

    for (const [key, transitions] of groups.entries()) {
        const [sourceId, targetId] = key.split('->');
        const first = transitions[0];

        edges.push({
            id: `edge-${key}`, // Stable ID for the directed pair
            source: sourceId,
            target: targetId,
            sourceHandle: 'child',
            targetHandle: 'parent',
            type: 'fsmTransition',
            // Show first event or count if multiple
            label: transitions.length > 1
                ? `${transitions.length} Transitions`
                : (first.events.length > 0 ? first.events.join(', ') : undefined),
            data: {
                transitions: transitions, // Pass all transitions to the edge
            },
            // We'll move arrow rendering to inside FSMTransitionEdge
        });
    }

    return edges;
}

// ==================== Component ====================

interface FSMEditorProps {
    machineId?: string;
}

export default function FSMEditor({ }: FSMEditorProps) {
    const machine = useFSMStore(state => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        if (!file) return null;
        return file.fsm.machines.get(file.currentMachineId) || null;
    });

    const addState = useFSMStore(state => state.addState);
    const updateState = useFSMStore(state => state.updateState);
    const removeState = useFSMStore(state => state.removeState);
    const addTransition = useFSMStore(state => state.addTransition);
    const removeTransition = useFSMStore(state => state.removeTransition);
    const navigateToMachine = useFSMStore(state => state.navigateToMachine);
    const navigateUp = useFSMStore(state => state.navigateUp);
    const setIsConnecting = useFSMStore(state => state.setIsConnecting);

    // Handle connection events for handle visibility
    const onConnectStart = useCallback(() => {
        setIsConnecting(true);
    }, [setIsConnecting]);

    const onConnectEnd = useCallback(() => {
        setIsConnecting(false);
    }, [setIsConnecting]);

    // Convert FSM data to React Flow format
    const initialNodes = useMemo(() => {
        if (!machine) return [];
        return convertStatesToNodes(machine);
    }, [machine]);

    const initialEdges = useMemo(() => {
        if (!machine) return [];
        return convertTransitionsToEdges(machine);
    }, [machine]);

    const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
    const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

    // Sync store changes to React Flow
    useEffect(() => {
        setNodes(initialNodes);
    }, [initialNodes, setNodes]);

    useEffect(() => {
        setEdges(initialEdges);
    }, [initialEdges, setEdges]);

    // Handle new connections
    const onConnect: OnConnect = useCallback(
        (params: Connection) => {
            if (params.source && params.target) {
                addTransition(params.source, params.target);
            }
        },
        [addTransition]
    );

    // Handle node position changes
    const onNodeDragStop = useCallback(
        (_: React.MouseEvent, node: Node) => {
            updateState(node.id, { position: node.position });
        },
        [updateState]
    );

    // Handle node double-click (for entering Meta states)
    const onNodeDoubleClick = useCallback(
        (_: React.MouseEvent, node: Node) => {
            const state = machine?.states.get(node.id);
            if (state?.type === 'Meta') {
                // Find the sub-machine
                const fsmStore = useFSMStore.getState();
                const fsm = fsmStore.getCurrentFSM();
                if (fsm) {
                    for (const [subMachineId, subMachine] of fsm.machines) {
                        if (subMachine.parentMetaStateId === node.id) {
                            navigateToMachine(subMachineId);
                            break;
                        }
                    }
                }
            }
        },
        [machine, navigateToMachine]
    );

    // Handle keyboard shortcuts
    const onKeyDown = useCallback(
        (event: React.KeyboardEvent) => {
            if (event.key === 'Delete' || event.key === 'Backspace') {
                // Remove selected nodes/edges
                nodes.filter(n => n.selected).forEach(n => removeState(n.id));
                edges.filter(e => e.selected).forEach(e => removeTransition(e.id));
            }
        },
        [nodes, edges, removeState, removeTransition]
    );

    if (!machine) {
        return (
            <div className="h-full flex items-center justify-center text-gray-500">
                No FSM loaded
            </div>
        );
    }

    return (
        <div className="h-full w-full" onKeyDown={onKeyDown} tabIndex={0}>
            <ReactFlow
                nodes={nodes}
                edges={edges}
                onNodesChange={onNodesChange}
                onEdgesChange={onEdgesChange}
                onConnect={onConnect}
                onConnectStart={onConnectStart}
                onConnectEnd={onConnectEnd}
                onNodeDragStop={onNodeDragStop}

                onNodeDoubleClick={onNodeDoubleClick}
                nodeTypes={nodeTypes}
                edgeTypes={edgeTypes}
                fitView
                snapToGrid
                snapGrid={[20, 20]}
                className="bg-gray-900"
            >
                <Background color="#333" gap={20} />
                <Controls className="!bg-gray-800 !border-gray-700" />
                <MiniMap
                    className="!bg-gray-800 !border-gray-700"
                    nodeColor={(node) => {
                        const data = node.data as FSMStateNodeData;
                        const colors: Record<string, string> = {
                            Normal: '#4A5568',
                            Meta: '#553C9A',
                            Entry: '#276749',
                            Exit: '#9B2C2C',
                            Any: '#744210',
                            Upper: '#2C5282',
                        };
                        return colors[data.state.type] || '#666';
                    }}
                />

                {/* Navigation breadcrumb */}
                {machine.level > 0 && (
                    <Panel position="top-left" className="bg-gray-800 p-2 rounded shadow-lg">
                        <button
                            onClick={navigateUp}
                            className="flex items-center gap-2 text-sm text-gray-300 hover:text-white"
                        >
                            <span>← Back to parent</span>
                        </button>
                    </Panel>
                )}

                {/* Add state buttons */}
                <Panel position="top-right" className="bg-gray-800 p-2 rounded shadow-lg flex gap-2">
                    <button
                        onClick={() => addState('Normal', { x: 300, y: 200 })}
                        className="px-3 py-1 text-sm bg-gray-700 hover:bg-gray-600 text-white rounded"
                    >
                        + State
                    </button>
                    <button
                        onClick={() => addState('Meta', { x: 300, y: 200 })}
                        className="px-3 py-1 text-sm bg-purple-700 hover:bg-purple-600 text-white rounded"
                    >
                        + Meta
                    </button>
                </Panel>
            </ReactFlow>
        </div>
    );
}
