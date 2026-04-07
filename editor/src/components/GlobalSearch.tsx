import { useState, useEffect, useRef, useMemo } from 'react';
import { useEditorStore } from '../stores/editorStore';
import { useEditorMetaStore } from '../stores/editorMetaStore';
import { useFSMStore } from '../stores/fsmStore';
import { isSpecialStateType } from '../types/fsm';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { getTheme } from '../theme/theme';

interface SearchResult {
    type: 'node' | 'pin' | 'variable' | 'io' | 'fsm-state' | 'fsm-transition';
    id: string; // id for jumping
    nodeId?: string; // parent node id for pins
    machineId?: string; // for FSM
    title: string;
    subtitle: string;
    value?: string;
    scope?: string; // 'local' | 'shared' | 'input' | 'output'
}

export function GlobalSearch() {
    const theme = getTheme();
    const isSearchOpen = useEditorMetaStore(state => state.uiMeta.isSearchOpen);
    const setSearchOpen = useEditorMetaStore(state => state.setSearchOpen);
    const setActivePropertiesTab = useEditorMetaStore(state => state.setActivePropertiesTab);
    const setFocusTarget = useEditorMetaStore(state => state.setFocusTarget);
    const setPendingCenterTarget = useEditorMetaStore(state => state.setPendingCenterTarget);

    const currentTree = useEditorStore(state => state.getCurrentTree());
    const selectNodes = useEditorStore(state => state.selectNodes);
    const { getDefinition } = useNodeDefinitionStore();

    const openedFSMFiles = useFSMStore(state => state.openedFSMFiles);
    const activeFSMPath = useFSMStore(state => state.activeFSMPath);
    const navigateToMachine = useFSMStore(state => state.navigateToMachine);
    const setSelectedFSMNodes = useFSMStore(state => state.setSelectedNodes);
    const setSelectedFSMEdges = useFSMStore(state => state.setSelectedEdges);

    const activeFSM = openedFSMFiles.find(f => f.path === activeFSMPath)?.fsm;

    const [query, setQuery] = useState('');
    const [selectedIndex, setSelectedIndex] = useState(0);
    const [scopes, setScopes] = useState({
        node: true,
        pin: true,
        variable: true,
        state: true,
    });

    const inputRef = useRef<HTMLInputElement>(null);
    const [position, setPosition] = useState({ x: window.innerWidth / 2 - 320, y: 100 });
    const isDragging = useRef(false);
    const dragStart = useRef({ x: 0, y: 0 });

    const handleMouseDown = (e: React.MouseEvent) => {
        isDragging.current = true;
        dragStart.current = { x: e.clientX - position.x, y: e.clientY - position.y };
    };

    useEffect(() => {
        const handleMouseMove = (e: MouseEvent) => {
            if (isDragging.current) {
                setPosition({
                    x: e.clientX - dragStart.current.x,
                    y: e.clientY - dragStart.current.y
                });
            }
        };
        const handleMouseUp = () => {
            isDragging.current = false;
        };

        window.addEventListener('mousemove', handleMouseMove);
        window.addEventListener('mouseup', handleMouseUp);
        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
    }, []);

    useEffect(() => {
        if (isSearchOpen) {
            setQuery('');
            setSelectedIndex(0);
            setTimeout(() => inputRef.current?.focus(), 0);
        }
    }, [isSearchOpen]);

    const results = useMemo(() => {
        if (query.length < 1) return [];

        const q = query.toLowerCase();
        const res: SearchResult[] = [];

        // 1. Search Nodes
        if (scopes.node && currentTree) {
            currentTree.nodes.forEach(node => {
                const title = node.nickname || node.type;
                const uidStr = node.uid?.toString() || '';
                const nodeDef = getDefinition(node.type);

                if (node.type.toLowerCase().includes(q) ||
                    (node.nickname && node.nickname.toLowerCase().includes(q)) ||
                    (node.comment && node.comment.toLowerCase().includes(q)) ||
                    (nodeDef?.desc && nodeDef.desc.toLowerCase().includes(q)) ||
                    uidStr.includes(q)) {
                    res.push({
                        type: 'node',
                        id: node.id,
                        title: title,
                        subtitle: `Node: ${node.type}${node.uid ? ` (UID: ${node.uid})` : ''}`,
                        value: node.comment || nodeDef?.desc
                    });
                }
            });
        }

        // 2. Search Pins
        if (scopes.pin && currentTree) {
            currentTree.nodes.forEach(node => {
                node.pins.forEach(pin => {
                    const pinValue = pin.binding.type === 'const' ? pin.binding.value : pin.binding.variableName;
                    if (pin.name.toLowerCase().includes(q) ||
                        (pin.desc && pin.desc.toLowerCase().includes(q)) ||
                        (pinValue && pinValue.toLowerCase().includes(q))) {
                        res.push({
                            type: 'pin',
                            id: `${node.id}-${pin.name}`,
                            nodeId: node.id,
                            title: pin.name,
                            subtitle: `Pin on ${node.nickname || node.type}`,
                            value: pin.binding.type === 'const' ? pin.binding.value : `-> ${pin.binding.variableName}`,
                        });
                    }
                });
            });

            // Tree I/O
            currentTree.inputs.forEach(pin => {
                if (pin.name.toLowerCase().includes(q)) {
                    res.push({
                        type: 'io',
                        id: pin.id,
                        title: pin.name,
                        subtitle: 'Tree Input',
                        scope: 'input'
                    });
                }
            });
            currentTree.outputs.forEach(pin => {
                if (pin.name.toLowerCase().includes(q)) {
                    res.push({
                        type: 'io',
                        id: pin.id,
                        title: pin.name,
                        subtitle: 'Tree Output',
                        scope: 'output'
                    });
                }
            });
        }

        // 3. Search Variables
        if (scopes.variable && currentTree) {
            currentTree.sharedVariables.forEach(v => {
                if (v.name.toLowerCase().includes(q)) {
                    res.push({
                        type: 'variable',
                        id: v.name,
                        title: v.name,
                        subtitle: 'Shared Variable',
                        scope: 'shared'
                    });
                }
            });
            currentTree.localVariables.forEach(v => {
                if (v.name.toLowerCase().includes(q)) {
                    res.push({
                        type: 'variable',
                        id: v.name,
                        title: v.name,
                        subtitle: 'Local Variable',
                        scope: 'local'
                    });
                }
            });
        }

        // 4. Search FSM
        if (scopes.state && activeFSM) {
            const getMachineFriendlyName = (machineId: string) => {
                if (machineId === activeFSM.rootMachineId) return activeFSM.name.replace(/\.fsm$/i, '') || 'Root';
                const machine = activeFSM.machines.get(machineId);
                if (machine?.parentMetaStateId) {
                    for (const m of activeFSM.machines.values()) {
                        const s = m.states.get(machine.parentMetaStateId);
                        if (s) return s.name || s.type;
                    }
                }
                return `Machine ${machineId.slice(-4)}`;
            };

            activeFSM.machines.forEach((machine, mId) => {
                const currentMachineName = getMachineFriendlyName(mId);

                // States
                machine.states.forEach(state => {
                    if (isSpecialStateType(state.type)) return; // 剔除 Entry/Any 等通用状态

                    if (state.name.toLowerCase().includes(q) ||
                        (state.comment && state.comment.toLowerCase().includes(q)) ||
                        (state.tree && state.tree.toLowerCase().includes(q))) {
                        res.push({
                            type: 'fsm-state',
                            id: state.id,
                            machineId: mId,
                            title: state.name,
                            subtitle: `FSM State in ${currentMachineName}${state.tree ? ` (Tree: ${state.tree})` : ''}`,
                            value: state.comment
                        });
                    }
                });

                // Transitions (Conditions)
                machine.transitions.forEach(trans => {
                    const matchingConditions = trans.conditions.filter(c => c.toLowerCase().includes(q));

                    if (matchingConditions.length > 0) {
                        const getStateInfo = (id: string | null) => {
                            if (!id) return { name: 'Any', mId: mId, isSpecial: true };
                            for (const [machineId, m] of activeFSM.machines) {
                                const s = m.states.get(id);
                                if (s) return { name: s.name || s.type, mId: machineId, isSpecial: isSpecialStateType(s.type) };
                            }
                            return { name: 'Unknown', mId: mId, isSpecial: false };
                        };

                        const fromInfo = getStateInfo(trans.fromStateId);
                        const toInfo = getStateInfo(trans.toStateId);

                        // Determine meaningful machine location
                        let displayMachineName = currentMachineName;
                        if (fromInfo.isSpecial && !toInfo.isSpecial) {
                            displayMachineName = getMachineFriendlyName(toInfo.mId);
                        } else if (toInfo.isSpecial && !fromInfo.isSpecial) {
                            displayMachineName = getMachineFriendlyName(fromInfo.mId);
                        }

                        res.push({
                            type: 'fsm-transition',
                            id: trans.id,
                            machineId: mId,
                            title: matchingConditions.join(', '),
                            subtitle: `FSM Transition: ${fromInfo.name} -> ${toInfo.name} in ${displayMachineName}`,
                        });
                    }
                });
            });
        }

        return res;

    }, [currentTree, activeFSM, query, scopes, getDefinition]);

    const handleJump = (result: SearchResult) => {
        // setSearchOpen(false);

        if (result.type === 'node' && currentTree) {
            const node = currentTree.nodes.get(result.id);
            if (node) {
                // Ensure tree mode is active
                const activeFile = useEditorStore.getState().openedFiles.find(f => f.path === useEditorStore.getState().activeFilePath);
                if (!activeFile || useFSMStore.getState().activeFSMPath) {
                    useEditorStore.getState().setActiveFile(useEditorStore.getState().activeFilePath!);
                }

                selectNodes([node.id]);
                setActivePropertiesTab('properties');

                // 使用 store 触发 NodeEditor 内的 setCenter
                setPendingCenterTarget({
                    x: node.position.x + 100,
                    y: node.position.y + 30,
                    zoom: 1.5
                });
            }
        } else if (result.type === 'pin' && currentTree) {
            const node = currentTree.nodes.get(result.nodeId!);
            if (node) {
                // Ensure tree mode is active
                const activeFile = useEditorStore.getState().openedFiles.find(f => f.path === useEditorStore.getState().activeFilePath);
                if (!activeFile || useFSMStore.getState().activeFSMPath) {
                    useEditorStore.getState().setActiveFile(useEditorStore.getState().activeFilePath!);
                }

                selectNodes([node.id]);
                // 使用 store 触发 NodeEditor 内的 setCenter
                setPendingCenterTarget({
                    x: node.position.x + 100,
                    y: node.position.y + 30,
                    zoom: 1.5
                });

                // We could scroll to the pin in the properties panel if we had a focus mechanism there
                setFocusTarget({ type: 'node', id: node.id });
            }
        } else if (result.type === 'variable') {
            setActivePropertiesTab('variables');
            setFocusTarget({ type: 'variable', id: result.id });
        } else if (result.type === 'io') {
            setActivePropertiesTab('io');
            setFocusTarget({ type: 'io', id: result.id });
        } else if (result.type === 'fsm-state' && activeFSM) {
            const machine = activeFSM.machines.get(result.machineId!);
            const state = machine?.states.get(result.id);
            if (state) {
                // Ensure FSM mode is active
                if (!activeFSMPath || useEditorStore.getState().activeFilePath) {
                    useFSMStore.getState().setActiveFSM(useFSMStore.getState().activeFSMPath!);
                }

                navigateToMachine(result.machineId!);
                setSelectedFSMNodes([state.id]);
                setPendingCenterTarget({
                    x: state.position.x + 100,
                    y: state.position.y + 30,
                    zoom: 1.5
                });
                setFocusTarget({ type: 'state' as any, id: state.id });
            }
        } else if (result.type === 'fsm-transition' && activeFSM) {
            const machineId = result.machineId;
            const machine = machineId ? activeFSM.machines.get(machineId) : undefined;
            const trans = machine?.transitions.find(t => t.id === result.id);
            if (trans && machine) {
                // Ensure FSM mode is active
                if (!activeFSMPath || useEditorStore.getState().activeFilePath) {
                    useFSMStore.getState().setActiveFSM(useFSMStore.getState().activeFSMPath!);
                }

                navigateToMachine(machineId!);
                setSelectedFSMEdges([trans.id]);
                // Center on the transition (midpoint or toState)
                const toState = machine.states.get(trans.toStateId);
                if (toState) {
                    setPendingCenterTarget({
                        x: toState.position.x - 50,
                        y: toState.position.y,
                        zoom: 1.5
                    });
                }
                setFocusTarget({ type: 'transition' as any, id: trans.id });
            }
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            setSelectedIndex(prev => Math.min(prev + 1, results.length - 1));
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            setSelectedIndex(prev => Math.max(prev - 1, 0));
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (results[selectedIndex]) {
                handleJump(results[selectedIndex]);
            }
        } else if (e.key === 'Escape') {
            setSearchOpen(false);
        }
    };

    if (!isSearchOpen) return null;

    return (
        <div
            className="fixed z-[100] w-[640px] border rounded-xl shadow-2xl overflow-hidden flex flex-col"
            style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border, color: theme.ui.textMain, left: position.x, top: position.y }}
            onKeyDown={handleKeyDown}
        >
            {/* Draggable Title Bar */}
            <div
                className="flex items-center justify-between px-4 py-2 border-b cursor-move select-none"
                style={{ backgroundColor: theme.ui.background, borderColor: theme.ui.border }}
                onMouseDown={handleMouseDown}
            >
                <div className="flex items-center gap-2">
                    <span className="text-sm" style={{ color: theme.ui.textMain }}>🔍</span>
                    <span className="text-[10px] uppercase font-bold tracking-[0.2em]" style={{ color: theme.ui.textDim }}>Global Search</span>
                </div>
                <button
                    className="w-6 h-6 flex items-center justify-center rounded-full transition-all duration-200"
                    style={{ color: theme.ui.textDim }}
                    onMouseEnter={(e) => {
                        e.currentTarget.style.backgroundColor = theme.ui.danger;
                        e.currentTarget.style.color = theme.ui.tabActiveText;
                    }}
                    onMouseLeave={(e) => {
                        e.currentTarget.style.backgroundColor = 'transparent';
                        e.currentTarget.style.color = theme.ui.textDim;
                    }}
                    onClick={() => setSearchOpen(false)}
                >
                    <span className="text-sm">✕</span>
                </button>
            </div>

            {/* Search Content */}
            <div className="p-4 space-y-3">
                <div className="relative">
                    <input
                        ref={inputRef}
                        type="text"
                        value={query}
                        onChange={e => setQuery(e.target.value)}
                        placeholder="Search nodes, UIDs, pins, values, variables..."
                        className="w-full pl-4 pr-10 py-2.5 border rounded-lg text-sm outline-none transition-all"
                        style={{ backgroundColor: theme.ui.inputBg, borderColor: theme.ui.border, color: theme.ui.textMain }}
                    />
                </div>

                <div className="flex gap-4 px-1">
                    {(['node', 'pin', 'variable', 'state'] as const).map(s => (
                        <label key={s} className="flex items-center gap-2 cursor-pointer group">
                            <input
                                type="checkbox"
                                checked={scopes[s]}
                                onChange={() => setScopes(prev => ({ ...prev, [s]: !prev[s] }))}
                                className="w-4 h-4 rounded"
                                style={{ accentColor: theme.ui.accent }}
                            />
                            <span className="text-xs capitalize transition-colors" style={{ color: scopes[s] ? theme.ui.textMain : theme.ui.textDim }}>
                                {s}
                            </span>
                        </label>
                    ))}
                </div>
            </div>

            {/* Results */}
            <div className="max-h-[400px] overflow-y-auto">
                {query.length === 0 ? (
                    <div className="p-8 text-center text-sm" style={{ color: theme.ui.textDim }}>
                        Type to search for anything in the tree...
                    </div>
                ) : results.length === 0 ? (
                    <div className="p-8 text-center text-sm" style={{ color: theme.ui.textDim }}>
                        No results found for "{query}"
                    </div>
                ) : (
                    <div className="py-2">
                        {results.map((res, index) => (
                            <div
                                key={`${res.type}-${res.id}`}
                                className="px-4 py-3 cursor-pointer flex items-center justify-between transition-colors border-l-4"
                                style={{
                                    backgroundColor: index === selectedIndex ? theme.ui.accentSoft : 'transparent',
                                    borderLeftColor: index === selectedIndex ? theme.ui.accent : 'transparent'
                                }}
                                onClick={() => handleJump(res)}
                                onMouseEnter={() => setSelectedIndex(index)}
                            >
                                <div className="flex flex-col gap-0.5 min-w-0">
                                    <div className="flex items-center gap-2">
                                        <span className="text-[10px] px-1.5 py-0.5 rounded font-bold uppercase" style={{ backgroundColor: theme.ui.buttonBg, color: theme.ui.textMain }}>
                                            {res.type}
                                        </span>
                                        <span className="text-sm font-medium truncate" style={{ color: theme.ui.textMain }}>{res.title}</span>
                                    </div>
                                    <div className="text-xs filename-ellipsis" style={{ color: theme.ui.textDim }}>{res.subtitle}</div>
                                </div>
                                {res.value && (
                                    <div className="ml-4 px-2 py-1 rounded text-xs border max-w-[150px] filename-ellipsis" style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}>
                                        {res.value}
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* Footer */}
            <div className="px-4 py-2 border-t flex justify-between items-center text-[10px]" style={{ backgroundColor: theme.ui.background, borderColor: theme.ui.border, color: theme.ui.textDim }}>
                <div>
                    ↑↓ to navigate · Enter to jump · Esc to close
                </div>
                <div>
                    {results.length} matches
                </div>
            </div>
        </div>
    );
}
