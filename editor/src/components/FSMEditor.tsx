/**
 * FSM Editor Component
 * 
 * Main React Flow-based editor for FSM state machines.
 */

import { useCallback, useMemo, useEffect, useState } from 'react';
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
    useReactFlow,
    ReactFlowProvider,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import FSMStateNode, { type FSMStateNodeData, type FSMStateNodeType } from './FSMStateNode';
import FSMTransitionEdge from './FSMTransitionEdge';
import { useFSMStore } from '../stores/fsmStore';
import type { FSMMachine, FSMTransition, FSMState } from '../types/fsm';

// ==================== Node & Edge Types ====================

const nodeTypes: NodeTypes = {
    fsmState: FSMStateNode,
};

const edgeTypes = {
    fsmTransition: FSMTransitionEdge,
};


// ==================== Conversion Functions ====================

function convertStatesToNodes(machine: FSMMachine, selectedNodeIds: string[]): FSMStateNodeType[] {
    const nodes: FSMStateNodeType[] = [];

    for (const [stateId, state] of machine.states) {
        nodes.push({
            id: stateId,
            type: 'fsmState',
            position: state.position,
            selected: selectedNodeIds.includes(stateId),
            data: {
                state,
                isDefault: machine.defaultStateId === stateId,
            },
        });
    }

    return nodes;
}

function convertTransitionsToEdges(machine: FSMMachine, fsm: { machines: Map<string, FSMMachine>, rootMachineId: string }, selectedEdgeIds: string[]): Edge[] {
    const edges: Edge[] = [];
    const rootMachine = fsm.machines.get(fsm.rootMachineId);
    if (!rootMachine) return edges;

    // Helper to find which machine a state belongs to
    const findMachineIdOfState = (stateId: string): string | null => {
        for (const [mId, m] of fsm.machines) {
            if (m.states.has(stateId)) return mId;
        }
        return null;
    };

    // Helper to find the path from root to a machine
    const getMachinePath = (mId: string): string[] => {
        const path: string[] = [];
        let cur: string | undefined = mId;
        while (cur) {
            path.unshift(cur);
            const machineObj = fsm.machines.get(cur);
            const parentMetaId: string | undefined = machineObj?.parentMetaStateId;
            cur = parentMetaId ? findMachineIdOfState(parentMetaId) || undefined : undefined;
        }
        return path;
    };

    // Add virtual edge for default state
    if (machine.defaultStateId) {
        const entryState = Array.from(machine.states.values()).find(s => s.type === 'Entry');
        if (entryState) {
            edges.push({
                id: `edge-default`,
                source: entryState.id,
                target: machine.defaultStateId,
                type: 'straight',
                animated: false,
                style: { stroke: '#F6E05E', strokeWidth: 2 },
                selectable: false,
                focusable: false,
                deletable: false,
                markerEnd: { type: 'arrow' as any },
            });
        }
    }

    // Key: sourceId->targetId
    const groups = new Map<string, { transitions: FSMTransition[], isVirtual: boolean }>();

    // Get local Any node for projecting Any-sourced transitions
    const localAny = Array.from(machine.states.values()).find(s => s.type === 'Any');
    const localEntry = Array.from(machine.states.values()).find(s => s.type === 'Entry');

    // Scan ALL global transitions from root machine
    for (const trans of rootMachine.transitions) {
        let fromId = trans.fromStateId;
        let toId = trans.toStateId;
        let isVirtual = false;

        const toMachineId = findMachineIdOfState(toId);
        const fromMachineId = fromId ? findMachineIdOfState(fromId) : null;

        // Case 1: Any-sourced transition (fromStateId is null)
        if (!fromId) {
            // Project Any transitions through local Any node only if target is local
            if (toMachineId === machine.id) {
                // Target is in this layer - project through local Any
                if (localAny) {
                    fromId = localAny.id;
                } else {
                    continue; // No local Any, skip
                }
            } else {
                // Target is not in this layer - don't show
                continue;
            }
        }
        // Case 2: Entry-sourced transition
        else if (trans.type === 'Entry') {
            if (toMachineId === machine.id && localEntry) {
                fromId = localEntry.id;
            } else {
                continue;
            }
        }
        // Case 3: Normal transitions
        else {
            // Both in current machine - show directly
            if (fromMachineId === machine.id && toMachineId === machine.id) {
                // Direct edge
            }
            // From local to external - project to Meta or Upper
            else if (fromMachineId === machine.id) {
                isVirtual = true;
                const toPath = toMachineId ? getMachinePath(toMachineId) : [];
                const subMachineInThisLayer = Array.from(fsm.machines.values()).find(sm =>
                    sm.id !== machine.id &&
                    sm.parentMetaStateId && machine.states.has(sm.parentMetaStateId) &&
                    toPath.includes(sm.id)
                );

                if (subMachineInThisLayer) {
                    toId = subMachineInThisLayer.parentMetaStateId!;
                } else {
                    const upperState = Array.from(machine.states.values()).find(s => s.type === 'Upper');
                    if (upperState) toId = upperState.id;
                    else continue;
                }
            }
            // From external to local - project from Meta or Upper
            else if (toMachineId === machine.id) {
                isVirtual = true;
                const fromPath = fromMachineId ? getMachinePath(fromMachineId) : [];
                const subMachineInThisLayer = Array.from(fsm.machines.values()).find(sm =>
                    sm.id !== machine.id &&
                    sm.parentMetaStateId && machine.states.has(sm.parentMetaStateId) &&
                    fromPath.includes(sm.id)
                );

                if (subMachineInThisLayer) {
                    fromId = subMachineInThisLayer.parentMetaStateId!;
                } else {
                    const upperState = Array.from(machine.states.values()).find(s => s.type === 'Upper');
                    if (upperState) fromId = upperState.id;
                    else continue;
                }
            }
            // Case 4: Both external - check if they're in different child sub-machines (siblings)
            else {
                isVirtual = true;
                const fromPath = fromMachineId ? getMachinePath(fromMachineId) : [];
                const toPath = toMachineId ? getMachinePath(toMachineId) : [];

                // Find if source is in a child sub-machine of current layer
                const fromSubMachine = Array.from(fsm.machines.values()).find(sm =>
                    sm.id !== machine.id &&
                    sm.parentMetaStateId && machine.states.has(sm.parentMetaStateId) &&
                    fromPath.includes(sm.id)
                );

                // Find if target is in a child sub-machine of current layer
                const toSubMachine = Array.from(fsm.machines.values()).find(sm =>
                    sm.id !== machine.id &&
                    sm.parentMetaStateId && machine.states.has(sm.parentMetaStateId) &&
                    toPath.includes(sm.id)
                );

                if (fromSubMachine && toSubMachine) {
                    // Both in child sub-machines - project as Meta → Meta
                    fromId = fromSubMachine.parentMetaStateId!;
                    toId = toSubMachine.parentMetaStateId!;
                } else {
                    // Not both in child sub-machines - don't show in this layer
                    continue;
                }
            }
        }

        if (!fromId || !toId) continue;

        const key = `${fromId}->${toId}`;
        if (!groups.has(key)) {
            groups.set(key, { transitions: [], isVirtual });
        }
        groups.get(key)!.transitions.push(trans);
    }

    for (const [key, group] of groups.entries()) {
        const [sourceId, targetId] = key.split('->');
        const first = group.transitions[0];

        edges.push({
            id: `edge-${key}`,
            source: sourceId,
            target: targetId,
            sourceHandle: 'child',
            targetHandle: 'parent',
            type: 'fsmTransition',
            selected: selectedEdgeIds.includes(`edge-${key}`),
            label: group.transitions.length > 1
                ? `${group.transitions.length} Transitions`
                : (first.events.length > 0 ? first.events.join(', ') : undefined),
            data: {
                transitions: group.transitions,
                isVirtual: group.isVirtual,
            },
            style: group.isVirtual ? { opacity: 0.8 } : undefined,
        });
    }

    return edges;
}

// ==================== Component ====================

interface FSMEditorProps {
    machineId?: string;
}

export default function FSMEditor(props: FSMEditorProps) {
    return (
        <ReactFlowProvider>
            <FSMEditorInner {...props} />
        </ReactFlowProvider>
    );
}

function FSMEditorInner({ }: FSMEditorProps) {
    const { screenToFlowPosition } = useReactFlow();
    const fsm = useFSMStore(state => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        return file?.fsm || null;
    });

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
    const setIsConnecting = useFSMStore(state => state.setIsConnecting);
    const setSelectedNodes = useFSMStore(state => state.setSelectedNodes);
    const setSelectedEdges = useFSMStore(state => state.setSelectedEdges);
    const setDefaultState = useFSMStore(state => state.setDefaultState);

    const [pickerData, setPickerData] = useState<{
        sourceId: string;
        targetNodeId: string;
        states: { id: string; name: string; machineName: string }[];
    } | null>(null);

    const [menu, setMenu] = useState<{ id: string; top?: number; left?: number; right?: number; bottom?: number } | null>(null);

    const onNodeContextMenu = useCallback(
        (event: React.MouseEvent, node: Node) => {
            event.preventDefault();
            const pane = document.querySelector('.react-flow__pane');
            if (!pane) return;
            const rect = pane.getBoundingClientRect();

            setMenu({
                id: node.id,
                top: event.clientY - rect.top,
                left: event.clientX - rect.left,
            });
        },
        [setMenu]
    );

    const onPaneClick = useCallback(() => setMenu(null), [setMenu]);

    // Handle connection events for handle visibility
    const onConnectStart = useCallback(() => {
        setIsConnecting(true);
    }, [setIsConnecting]);

    const onConnectEnd = useCallback(() => {
        setIsConnecting(false);
    }, [setIsConnecting]);

    const selectedNodeIds = useFSMStore(state => state.selectedNodeIds);
    const selectedEdgeIds = useFSMStore(state => state.selectedEdgeIds);

    // Convert FSM data to React Flow format
    const initialNodes = useMemo(() => {
        if (!machine) return [];
        return convertStatesToNodes(machine, selectedNodeIds);
    }, [machine, selectedNodeIds]);

    const initialEdges = useMemo(() => {
        if (!machine || !fsm) return [];
        return convertTransitionsToEdges(machine, fsm, selectedEdgeIds);
    }, [machine, fsm, selectedEdgeIds]);

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
            if (!params.source || !params.target) return;

            const targetNode = machine?.states.get(params.target);
            if (targetNode?.type === 'Meta' || targetNode?.type === 'Upper') {
                // Show picker
                const availableStates: { id: string; name: string; machineName: string }[] = [];

                if (targetNode.type === 'Meta') {
                    // Find the sub-machine recursively
                    const findSubMachineStates = (mId: string, prefix: string = '') => {
                        const m = fsm?.machines.get(mId);
                        if (!m) return;
                        for (const s of m.states.values()) {
                            if (s.type === 'Normal' || s.type === 'Meta') {
                                availableStates.push({
                                    id: s.id,
                                    name: s.name || s.type,
                                    machineName: prefix || fsm?.name || 'Root'
                                });
                            }
                            if (s.type === 'Meta') {
                                const subM = Array.from(fsm?.machines.values() || []).find(sm => sm.parentMetaStateId === s.id);
                                if (subM) findSubMachineStates(subM.id, (prefix ? prefix + ' > ' : '') + (s.name || 'Meta'));
                            }
                        }
                    };

                    const subM = Array.from(fsm?.machines.values() || []).find(sm => sm.parentMetaStateId === targetNode.id);
                    if (subM) findSubMachineStates(subM.id, targetNode.name || 'Meta');

                    // Also include the meta node itself
                    availableStates.unshift({ id: targetNode.id, name: targetNode.name || 'Meta', machineName: 'This Machine' });
                } else if (targetNode.type === 'Upper') {
                    // Find all states outside current machine subtree
                    // Helper: check if machineId is the current machine or a descendant of it
                    const isCurrentOrDescendant = (mId: string): boolean => {
                        if (mId === machine?.id) return true;
                        const m = fsm?.machines.get(mId);
                        if (!m?.parentMetaStateId) return false;
                        // Find parent machine
                        for (const [pId, pm] of fsm?.machines || []) {
                            if (pm.states.has(m.parentMetaStateId)) {
                                return isCurrentOrDescendant(pId);
                            }
                        }
                        return false;
                    };

                    // Collect states from machines that are NOT current or descendants
                    fsm?.machines.forEach((m, mId) => {
                        if (!isCurrentOrDescendant(mId)) {
                            m.states.forEach(s => {
                                if (s.type === 'Normal' || s.type === 'Meta') {
                                    // Determine display name for machine
                                    let machineName = 'Root';
                                    if (mId !== fsm?.rootMachineId) {
                                        // Find the Meta state that contains this machine
                                        const parentMeta = Array.from(fsm?.machines.values() || [])
                                            .flatMap(pm => Array.from(pm.states.values()))
                                            .find(ps => ps.id === m.parentMetaStateId);
                                        machineName = parentMeta?.name || 'Parent';
                                    }
                                    availableStates.push({ id: s.id, name: s.name || s.type, machineName });
                                }
                            });
                        }
                    });
                }

                if (availableStates.length > 0) {
                    setPickerData({
                        sourceId: params.source,
                        targetNodeId: params.target,
                        states: availableStates,
                    });
                }
                return;
            }

            addTransition(params.source, params.target);
        },
        [addTransition, machine, fsm]
    );

    // Handle selection changes
    const onSelectionChange = useCallback(
        ({ nodes, edges }: { nodes: Node[]; edges: Edge[] }) => {
            setSelectedNodes(nodes.map(n => n.id));
            setSelectedEdges(edges.map(e => e.id));
        },
        [setSelectedNodes, setSelectedEdges]
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
                edges.filter(e => e.selected && e.id !== 'edge-default').forEach(e => removeTransition(e.id));
            }
        },
        [nodes, edges, removeState, removeTransition]
    );

    // Handle new state with better placement
    const handleAddState = useCallback((type: FSMState['type']) => {
        const pane = document.querySelector('.react-flow__pane');
        if (!pane) return;
        const rect = pane.getBoundingClientRect();

        // Get center of viewport
        const center = screenToFlowPosition({
            x: rect.left + rect.width / 2,
            y: rect.top + rect.height / 2,
        });

        // Add some jitter to avoid perfect overlap
        const position = {
            x: center.x - 60 + (Math.random() * 40),
            y: center.y - 25 + (Math.random() * 40),
        };

        addState(type, position);
    }, [addState, screenToFlowPosition]);

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
                onSelectionChange={onSelectionChange}
                onNodeContextMenu={onNodeContextMenu}
                onPaneClick={onPaneClick}

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
                {fsm && (
                    <Panel position="top-left" className="bg-gray-800 p-2 rounded shadow-lg">
                        <div className="flex items-center gap-1 text-sm">
                            {(() => {
                                // Build breadcrumb path from root to current machine
                                const breadcrumbs: { machineId: string; name: string }[] = [];
                                let currentId = machine.id;

                                while (currentId) {
                                    const m = fsm.machines.get(currentId);
                                    if (!m) break;

                                    // Find the name of this machine (from parent's Meta state)
                                    let name = currentId === fsm.rootMachineId ? fsm.name || 'Root' : '';
                                    if (m.parentMetaStateId) {
                                        for (const pm of fsm.machines.values()) {
                                            const metaState = pm.states.get(m.parentMetaStateId);
                                            if (metaState) {
                                                name = metaState.name || 'Meta';
                                                break;
                                            }
                                        }
                                    }

                                    breadcrumbs.unshift({ machineId: currentId, name });

                                    // Find parent machine
                                    if (m.parentMetaStateId) {
                                        let found = false;
                                        for (const [pId, pm] of fsm.machines) {
                                            if (pm.states.has(m.parentMetaStateId)) {
                                                currentId = pId;
                                                found = true;
                                                break;
                                            }
                                        }
                                        if (!found) break;
                                    } else {
                                        break;
                                    }
                                }

                                return breadcrumbs.map((bc, idx) => (
                                    <span key={bc.machineId} className="flex items-center gap-1">
                                        {idx > 0 && <span className="text-gray-500">›</span>}
                                        {bc.machineId === machine.id ? (
                                            <span className="text-white font-medium">{bc.name}</span>
                                        ) : (
                                            <button
                                                onClick={() => navigateToMachine(bc.machineId)}
                                                className="text-gray-400 hover:text-blue-400 hover:underline transition-colors"
                                            >
                                                {bc.name}
                                            </button>
                                        )}
                                    </span>
                                ));
                            })()}
                        </div>
                    </Panel>
                )}

                {/* Add state buttons */}
                <Panel position="top-right" className="bg-gray-800 p-2 rounded shadow-lg flex gap-2">
                    <button
                        onClick={() => handleAddState('Normal')}
                        className="px-3 py-1 text-sm bg-gray-700 hover:bg-gray-600 text-white rounded"
                    >
                        + State
                    </button>
                    <button
                        onClick={() => handleAddState('Meta')}
                        className="px-3 py-1 text-sm bg-purple-700 hover:bg-purple-600 text-white rounded"
                    >
                        + Meta
                    </button>
                </Panel>
                {/* Context Menu */}
                {menu && (
                    <div
                        style={{ top: menu.top, left: menu.left }}
                        className="absolute z-50 bg-gray-800 border border-gray-700 rounded shadow-xl py-1 min-w-[120px]"
                    >
                        <button
                            className="w-full text-left px-3 py-1.5 text-xs text-gray-200 hover:bg-blue-600 transition-colors"
                            onClick={() => {
                                setDefaultState(menu.id);
                                setMenu(null);
                            }}
                        >
                            Set as Default
                        </button>
                        <div className="border-t border-gray-700 my-1" />
                        <button
                            className="w-full text-left px-3 py-1.5 text-xs text-red-400 hover:bg-red-600 hover:text-white transition-colors"
                            onClick={() => {
                                removeState(menu.id);
                                setMenu(null);
                            }}
                        >
                            Delete
                        </button>
                    </div>
                )}

                {/* State Picker Modal */}
                {pickerData && (
                    <div className="absolute inset-0 z-[100] flex items-center justify-center bg-black/60 backdrop-blur-sm p-8">
                        <div className="bg-gray-800 border border-gray-700 rounded-lg shadow-2xl w-full max-w-md flex flex-col max-h-full">
                            <div className="p-4 border-b border-gray-700 flex justify-between items-center">
                                <h3 className="text-lg font-semibold text-white">Select Target State</h3>
                                <button onClick={() => setPickerData(null)} className="text-gray-400 hover:text-white text-xl">✕</button>
                            </div>
                            <div className="flex-1 overflow-auto p-2 scrollbar-thin scrollbar-thumb-gray-700">
                                {pickerData.states.map(s => (
                                    <button
                                        key={s.id}
                                        onClick={() => {
                                            if (pickerData.sourceId && s.id) {
                                                addTransition(pickerData.sourceId, s.id);
                                            }
                                            setPickerData(null);
                                        }}
                                        className="w-full text-left p-3 hover:bg-gray-700 rounded-md group transition-colors flex flex-col"
                                    >
                                        <div className="text-sm font-medium text-white group-hover:text-blue-400">{s.name}</div>
                                        <div className="text-xs text-gray-500">{s.machineName}</div>
                                    </button>
                                ))}
                            </div>
                            <div className="p-3 border-t border-gray-700 text-right">
                                <button
                                    onClick={() => setPickerData(null)}
                                    className="px-4 py-2 text-sm bg-gray-700 hover:bg-gray-600 text-white rounded"
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>
                    </div>
                )}
            </ReactFlow>
        </div>
    );
}
