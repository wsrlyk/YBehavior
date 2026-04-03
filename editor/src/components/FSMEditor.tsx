/**
 * FSM Editor Component
 * 
 * Main React Flow-based editor for FSM state machines.
 */

import { useCallback, useMemo, useEffect, useState, useRef } from 'react';
import {
    ReactFlow,
    Background,
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
import { useEditorMetaStore } from '../stores/editorMetaStore';
import { useDebugStore } from '../stores/debugStore';
import { useShallow } from 'zustand/react/shallow';
import type { FSMMachine, FSMTransition, FSMState } from '../types/fsm';
import { getTheme } from '../theme/theme';

const theme = getTheme();

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
                : (first.conditions.length > 0 ? first.conditions.join(', ') : undefined),
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
    onPaneClick?: () => void;
}

export default function FSMEditor(props: FSMEditorProps) {
    return (
        <ReactFlowProvider>
            <FSMEditorInner {...props} />
        </ReactFlowProvider>
    );
}

function FSMEditorInner({ onPaneClick: onPaneClickProp }: FSMEditorProps) {
    const { screenToFlowPosition, setCenter } = useReactFlow();
    const pendingCenterTarget = useEditorMetaStore(state => state.uiMeta.pendingCenterTarget);
    const setPendingCenterTarget = useEditorMetaStore(state => state.setPendingCenterTarget);

    useEffect(() => {
        if (pendingCenterTarget) {
            setTimeout(() => {
                setCenter(pendingCenterTarget.x, pendingCenterTarget.y, { zoom: pendingCenterTarget.zoom || 1.2, duration: 0 });
            }, 100);
            setPendingCenterTarget(undefined);
        }
    }, [pendingCenterTarget, setCenter, setPendingCenterTarget]);
    const {
        fsm,
        machine,
        addState,
        updateState,
        removeState,
        addTransition,
        removeTransition,
        navigateToMachine,
        setIsConnecting,
        setSelectedNodes,
        setSelectedEdges,
        setDefaultState,
        selectedNodeIds,
        selectedEdgeIds,
        activeFSMPath,
        activeFile,
        setViewport,
    } = useFSMStore(useShallow(state => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        return {
            fsm: file?.fsm || null,
            machine: file ? file.fsm.machines.get(file.currentMachineId) || null : null,
            addState: state.addState,
            updateState: state.updateState,
            removeState: state.removeState,
            addTransition: state.addTransition,
            removeTransition: state.removeTransition,
            navigateToMachine: state.navigateToMachine,
            setIsConnecting: state.setIsConnecting,
            setSelectedNodes: state.setSelectedNodes,
            setSelectedEdges: state.setSelectedEdges,
            setDefaultState: state.setDefaultState,
            selectedNodeIds: state.selectedNodeIds,
            selectedEdgeIds: state.selectedEdgeIds,
            activeFSMPath: state.activeFSMPath,
            activeFile: file,
            setViewport: state.setViewport,
        };
    }));

    const { setViewport: flowSetViewport } = useReactFlow();
    useEffect(() => {
        if (activeFile?.viewport) {
            flowSetViewport(activeFile.viewport);
        }
    }, [flowSetViewport, activeFile]);

    const onMoveEnd = useCallback((_: any, viewport: any) => {
        if (activeFSMPath) {
            setViewport(activeFSMPath, viewport);
        }
    }, [activeFSMPath, setViewport]);

    const lastSelectedNodesRef = useRef<string[]>([]);
    const lastSelectedEdgesRef = useRef<string[]>([]);

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

    const onPaneClick = useCallback(() => {
        setMenu(null);
        onPaneClickProp?.();
    }, [setMenu, onPaneClickProp]);

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
        return convertStatesToNodes(machine, selectedNodeIds);
    }, [machine, selectedNodeIds]);

    const initialEdges = useMemo(() => {
        if (!machine || !fsm) return [];
        return convertTransitionsToEdges(machine, fsm, selectedEdgeIds);
    }, [machine, fsm, selectedEdgeIds]);

    const [nodes, setNodes, onNodesChangeBase] = useNodesState(initialNodes);
    const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

    // Read-only mode during debugging
    const isDebugConnected = useDebugStore((s) => s.isConnected);
    const onNodesChange = useCallback(
        (changes: import('@xyflow/react').NodeChange<FSMStateNodeType>[]) => {
            // Filter out modification changes when in read-only debug mode
            const filteredChanges = isDebugConnected
                ? changes.filter(c => c.type === 'select' || c.type === 'dimensions')
                : changes;
            onNodesChangeBase(filteredChanges);
        },
        [onNodesChangeBase, isDebugConnected]
    );

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
            const newNodeIds = nodes.map(n => n.id).sort();
            const newEdgeIds = edges.map(e => e.id).sort();

            const currentNodesSorted = [...selectedNodeIds].sort();
            const currentEdgesSorted = [...selectedEdgeIds].sort();

            const isNodesSame = newNodeIds.length === currentNodesSorted.length &&
                newNodeIds.every((id, idx) => id === currentNodesSorted[idx]);
            const isEdgesSame = newEdgeIds.length === currentEdgesSorted.length &&
                newEdgeIds.every((id, idx) => id === currentEdgesSorted[idx]);

            const lastNodesSorted = [...lastSelectedNodesRef.current].sort();
            const lastEdgesSorted = [...lastSelectedEdgesRef.current].sort();

            const isLastNodesSame = newNodeIds.length === lastNodesSorted.length &&
                newNodeIds.every((id, idx) => id === lastNodesSorted[idx]);
            const isLastEdgesSame = newEdgeIds.length === lastEdgesSorted.length &&
                newEdgeIds.every((id, idx) => id === lastEdgesSorted[idx]);

            if (!isNodesSame && !isLastNodesSame) {
                lastSelectedNodesRef.current = newNodeIds;
                setSelectedNodes(newNodeIds);
            }
            if (!isEdgesSame && !isLastEdgesSame) {
                lastSelectedEdgesRef.current = newEdgeIds;
                setSelectedEdges(newEdgeIds);
            }
        },
        [setSelectedNodes, setSelectedEdges, selectedNodeIds, selectedEdgeIds]
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
                onMoveEnd={onMoveEnd}
                onSelectionChange={onSelectionChange}
                onNodeContextMenu={onNodeContextMenu}
                onPaneClick={onPaneClick}

                onNodeDoubleClick={onNodeDoubleClick}
                nodeTypes={nodeTypes}
                edgeTypes={edgeTypes}
                fitView={!activeFile?.viewport}
                snapToGrid
                snapGrid={[20, 20]}
                style={{ backgroundColor: theme.ui.background }}
            >
                <Background color={theme.ui.gridDots} gap={20} />
                <MiniMap
                    style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}
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
                    <Panel position="top-left" style={{ backgroundColor: theme.ui.panelBg, color: theme.ui.textMain }} className="p-2 rounded shadow-lg">
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
                                        {idx > 0 && <span style={{ color: theme.ui.textDim }}>›</span>}
                                        {bc.machineId === machine.id ? (
                                            <span className="text-white font-medium">{bc.name}</span>
                                        ) : (
                                            <button
                                                onClick={() => navigateToMachine(bc.machineId)}
                                                className="hover:underline transition-colors"
                                                style={{ color: theme.ui.textDim }}
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
                <Panel position="top-right" style={{ backgroundColor: theme.ui.panelBg, color: theme.ui.textMain }} className="p-2 rounded shadow-lg flex gap-2">
                    <button
                        onClick={() => handleAddState('Normal')}
                        className="px-3 py-1 text-sm text-white rounded hover:opacity-80 transition-opacity"
                        style={{ backgroundColor: theme.ui.border }}
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
                        style={{ top: menu.top, left: menu.left, backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}
                        className="absolute z-50 border rounded shadow-xl py-1 min-w-[120px]"
                    >
                        <button
                            className="w-full text-left px-3 py-1.5 text-xs hover:bg-blue-600 transition-colors"
                            style={{ color: theme.ui.textMain }}
                            onClick={() => {
                                setDefaultState(menu.id);
                                setMenu(null);
                            }}
                        >
                            Set as Default
                        </button>
                        <div className="border-t my-1" style={{ borderColor: theme.ui.border }} />
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
                        <div className="border rounded-lg shadow-2xl w-full max-w-md flex flex-col max-h-full" style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}>
                            <div className="p-4 border-b flex justify-between items-center" style={{ borderColor: theme.ui.border }}>
                                <h3 className="text-lg font-semibold" style={{ color: theme.ui.textMain }}>Select Target State</h3>
                                <button onClick={() => setPickerData(null)} className="hover:text-white text-xl" style={{ color: theme.ui.textDim }}>✕</button>
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
                                        className="w-full text-left p-3 rounded-md group transition-colors flex flex-col hover:bg-[#404040]"
                                        style={{ color: theme.ui.textMain }}
                                    >
                                        <div className="text-sm font-medium group-hover:text-blue-400">{s.name}</div>
                                        <div className="text-xs" style={{ color: theme.ui.textDim }}>{s.machineName}</div>
                                    </button>
                                ))}
                            </div>
                            <div className="p-3 border-t text-right" style={{ borderColor: theme.ui.border }}>
                                <button
                                    onClick={() => setPickerData(null)}
                                    className="px-4 py-2 text-sm text-white rounded hover:opacity-80 transition-opacity"
                                    style={{ backgroundColor: theme.ui.border }}
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
