import { useFSMStore } from '../stores/fsmStore';
import { useMemo, useState, useRef, useEffect } from 'react';
import { useEditorStore } from '../stores/editorStore';
import { useEditorMetaStore } from '../stores/editorMetaStore';
import { validateVariableName } from '../utils/validation';
import { TreeFilePicker } from './TreeFilePicker';
import { getTheme } from '../theme/theme';


// TreeFilePicker extracted to separate file

// ==================== Main Panel ====================

export function FSMPropertiesPanel() {
    const theme = getTheme();
    const selectedNodeIds = useFSMStore(state => state.selectedNodeIds);
    const selectedEdgeIds = useFSMStore(state => state.selectedEdgeIds);

    const fsm = useFSMStore(state => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        return file?.fsm || null;
    });

    const machine = useFSMStore(state => {
        const file = state.openedFSMFiles.find(f => f.path === state.activeFSMPath);
        if (!file) return null;
        return file.fsm.machines.get(file.currentMachineId) || null;
    });

    const updateState = useFSMStore(state => state.updateState);
    const removeTransition = useFSMStore(state => state.removeTransition);
    const addCondition = useFSMStore(state => state.addConditionToTransition);
    const removeCondition = useFSMStore(state => state.removeConditionFromTransition);

    const treeFiles = useEditorStore(state => state.treeFiles || []);

    const focusTarget = useEditorMetaStore(state => state.uiMeta.focusTarget);
    const setFocusTarget = useEditorMetaStore(state => state.setFocusTarget);

    const focusedStateRef = useRef<HTMLDivElement>(null);
    const focusedTransRefs = useRef<Map<string, HTMLDivElement>>(new Map());

    // Determine what to show
    const selectedState = useMemo(() => {
        if (selectedNodeIds.length !== 1) return null;
        return machine?.states.get(selectedNodeIds[0]) || null;
    }, [selectedNodeIds, machine]);

    useEffect(() => {
        if (focusTarget) {
            if (focusTarget.type === 'state' && selectedState?.id === focusTarget.id) {
                focusedStateRef.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                const timer = setTimeout(() => setFocusTarget(undefined), 2000);
                return () => clearTimeout(timer);
            }
            if (focusTarget.type === 'transition') {
                const el = focusedTransRefs.current.get(focusTarget.id);
                if (el) {
                    el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                    const timer = setTimeout(() => setFocusTarget(undefined), 2000);
                    return () => clearTimeout(timer);
                }
            }
        }
    }, [focusTarget, selectedState?.id, setFocusTarget]);

    const transitionsForSelectedEdge = useMemo(() => {
        if (selectedEdgeIds.length !== 1 || !fsm) return [];
        const edgeId = selectedEdgeIds[0];
        if (!edgeId.startsWith('edge-') || edgeId === 'edge-default') return [];

        // All transitions are stored in root machine
        const rootMachine = fsm.machines.get(fsm.rootMachineId);
        if (!rootMachine) return [];

        // Parse the edge key (fromId->toId) - these are PROJECTED IDs
        const [projectedSrc, projectedTgt] = edgeId.replace('edge-', '').split('->');

        // Helper to find which machine a state belongs to
        const findMachineIdOfState = (stateId: string): string | null => {
            for (const [mId, m] of fsm.machines) {
                if (m.states.has(stateId)) return mId;
            }
            return null;
        };

        // Helper to get machine path (root to machine)
        const getMachinePath = (mId: string): string[] => {
            const path: string[] = [];
            let cur: string | undefined = mId;
            while (cur) {
                path.unshift(cur);
                const machineObj = fsm.machines.get(cur);
                const parentMetaId = machineObj?.parentMetaStateId;
                cur = parentMetaId ? findMachineIdOfState(parentMetaId) || undefined : undefined;
            }
            return path;
        };

        // Get current machine for context
        const currentFile = useFSMStore.getState().openedFSMFiles.find(f => f.path === useFSMStore.getState().activeFSMPath);
        if (!currentFile) return [];
        const currentMachineId = currentFile.currentMachineId;
        const currentMachine = fsm.machines.get(currentMachineId);
        if (!currentMachine) return [];

        // Get special states
        const localAny = Array.from(currentMachine.states.values()).find(s => s.type === 'Any');
        const localUpper = Array.from(currentMachine.states.values()).find(s => s.type === 'Upper');

        // Check what the projected source/target represent
        const isFromAny = localAny && projectedSrc === localAny.id;
        const isFromUpper = localUpper && projectedSrc === localUpper.id;
        const isToUpper = localUpper && projectedTgt === localUpper.id;

        // Check if projected source/target is a Meta state in current machine
        const srcState = currentMachine.states.get(projectedSrc);
        const tgtState = currentMachine.states.get(projectedTgt);
        const isFromMeta = srcState?.type === 'Meta';
        const isToMeta = tgtState?.type === 'Meta';

        // Filter transitions that would project to this edge
        return rootMachine.transitions.filter(t => {
            const fromMachineId = t.fromStateId ? findMachineIdOfState(t.fromStateId) : null;
            const toMachineId = findMachineIdOfState(t.toStateId);

            // Case 1: Any-sourced transition
            if (isFromAny && t.fromStateId === null) {
                return toMachineId === currentMachineId && t.toStateId === projectedTgt;
            }

            // Case 2: Normal local transition
            if (t.fromStateId === projectedSrc && t.toStateId === projectedTgt) {
                return true;
            }

            // Case 3: From local to Meta (cross-layer into sub-machine)
            if (isToMeta && fromMachineId === currentMachineId && t.fromStateId === projectedSrc) {
                const subMachine = Array.from(fsm.machines.values()).find(sm => sm.parentMetaStateId === projectedTgt);
                if (subMachine) {
                    const toPath = getMachinePath(toMachineId!);
                    if (toPath.includes(subMachine.id)) return true;
                }
            }

            // Case 4: From Meta to local (cross-layer from sub-machine)
            if (isFromMeta && toMachineId === currentMachineId && t.toStateId === projectedTgt) {
                const subMachine = Array.from(fsm.machines.values()).find(sm => sm.parentMetaStateId === projectedSrc);
                if (subMachine) {
                    const fromPath = getMachinePath(fromMachineId!);
                    if (fromPath.includes(subMachine.id)) return true;
                }
            }

            // Case 5: From local to Upper (exit current machine)
            if (isToUpper && fromMachineId === currentMachineId && t.fromStateId === projectedSrc) {
                return toMachineId !== currentMachineId;
            }

            // Case 6: From Upper to local (entry from outside)
            if (isFromUpper && toMachineId === currentMachineId && t.toStateId === projectedTgt) {
                return fromMachineId !== currentMachineId && fromMachineId !== null;
            }

            // Case 7: Meta -> Meta (sibling sub-machines)
            if (isFromMeta && isToMeta) {
                const fromSubMachine = Array.from(fsm.machines.values()).find(sm => sm.parentMetaStateId === projectedSrc);
                const toSubMachine = Array.from(fsm.machines.values()).find(sm => sm.parentMetaStateId === projectedTgt);
                if (fromSubMachine && toSubMachine) {
                    const fromPath = getMachinePath(fromMachineId!);
                    const toPath = getMachinePath(toMachineId!);
                    if (fromPath.includes(fromSubMachine.id) && toPath.includes(toSubMachine.id)) return true;
                }
            }

            // Case 8: Meta -> Upper (from sub-machine to outside)
            if (isFromMeta && isToUpper && t.fromStateId) {
                const subMachine = Array.from(fsm.machines.values()).find(sm => sm.parentMetaStateId === projectedSrc);
                if (subMachine) {
                    const fromPath = getMachinePath(fromMachineId!);
                    if (fromPath.includes(subMachine.id) && toMachineId !== currentMachineId) return true;
                }
            }

            // Case 9: Upper -> Meta (from outside to sub-machine)
            if (isFromUpper && isToMeta) {
                const subMachine = Array.from(fsm.machines.values()).find(sm => sm.parentMetaStateId === projectedTgt);
                if (subMachine) {
                    const toPath = getMachinePath(toMachineId!);
                    if (toPath.includes(subMachine.id) && fromMachineId !== currentMachineId && fromMachineId !== null) return true;
                }
            }

            return false;
        });
    }, [selectedEdgeIds, fsm]);

    const [isAddingCondition, setIsAddingCondition] = useState<string | null>(null);
    const [newConditionName, setNewConditionName] = useState('');
    const addConditionInputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (isAddingCondition && addConditionInputRef.current) {
            addConditionInputRef.current.focus();
        }
    }, [isAddingCondition]);
    const handleAddCondition = (transId: string) => {
        const trimmed = newConditionName.trim();
        const validation = validateVariableName(trimmed);
        if (trimmed && validation.isValid) {
            addCondition(transId, trimmed);
            setNewConditionName('');
            setIsAddingCondition(null);
        }
    };

    const [localName, setLocalName] = useState('');
    const [localComment, setLocalComment] = useState('');

    useEffect(() => {
        setLocalName(selectedState?.name || '');
        setLocalComment(selectedState?.comment || '');
    }, [selectedState?.id]);

    if (!selectedState && transitionsForSelectedEdge.length === 0) {
        return (
            <div className="h-full border-l flex flex-col p-4 items-center justify-center text-center" style={{ backgroundColor: theme.ui.background, borderColor: theme.ui.border }}>
                <div className="mb-2" style={{ color: theme.ui.textDim }}>
                    <svg className="w-12 h-12 mx-auto opacity-20" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122" />
                    </svg>
                </div>
                <div className="text-sm font-medium uppercase tracking-wider mb-1" style={{ color: theme.ui.textDim }}>Selection</div>
                <div className="text-xs" style={{ color: theme.ui.textDim }}>Select a state or transition to view and edit its properties.</div>
            </div>
        );
    }

    return (
        <div className="h-full border-l flex flex-col p-3 overflow-auto scrollbar-thin" style={{ backgroundColor: theme.ui.background, borderColor: theme.ui.border }}>
            {selectedState && (
                <div className={`space-y-4 transition-all duration-500 ${focusTarget?.type === 'state' && focusTarget.id === selectedState.id ? 'rounded-lg p-1' : ''}`} style={focusTarget?.type === 'state' && focusTarget.id === selectedState.id ? { boxShadow: `0 0 0 2px ${theme.ui.border}` } : undefined} ref={focusedStateRef}>
                    <div className="text-xs uppercase tracking-wider font-bold border-b pb-2" style={{ color: theme.ui.textDim, borderColor: theme.ui.border }}>State Properties</div>

                    <div>
                        <label className="text-[10px] block mb-1" style={{ color: theme.ui.textDim }}>Type</label>
                        <div className="text-xs font-medium px-2 py-1 rounded inline-block" style={{ color: theme.ui.textMain, backgroundColor: theme.ui.accentSoft }}>
                            {selectedState.type}
                        </div>
                    </div>

                    {(selectedState.type === 'Normal' || selectedState.type === 'Meta') && (() => {
                        const nameValidation = validateVariableName(localName);
                        const isValid = localName.length === 0 || nameValidation.isValid;
                        return (
                            <div>
                                <label className="text-[10px] block mb-1" style={{ color: theme.ui.textDim }}>Name</label>
                                <input
                                    className={`w-full text-sm px-2 py-1.5 rounded border outline-none transition-colors ${!isValid ? 'border-red-500 focus:border-red-400' : ''
                                        }`}
                                    style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: !isValid ? undefined : theme.ui.border }}
                                    value={localName}
                                    onChange={(e) => setLocalName(e.target.value)}
                                    onBlur={() => {
                                        if (nameValidation.isValid) {
                                            updateState(selectedState.id, { name: localName });
                                        }
                                    }}
                                    placeholder="State name..."
                                />
                                {!isValid && (
                                    <div className="text-[10px] text-red-400 mt-1">{nameValidation.error}</div>
                                )}
                            </div>
                        );
                    })()}

                    {(selectedState.type === 'Normal' || selectedState.type === 'Entry' || selectedState.type === 'Exit') && (
                        <div>
                            <label className="text-[10px] block mb-1" style={{ color: theme.ui.textDim }}>Linked Tree</label>
                            <TreeFilePicker
                                value={selectedState.tree || ''}
                                options={treeFiles}
                                onChange={(val) => updateState(selectedState.id, { tree: val })}
                            />
                        </div>
                    )}

                    <div>
                        <label className="text-[10px] block mb-1" style={{ color: theme.ui.textDim }}>Comment</label>
                        <textarea
                            className="w-full text-sm px-2 py-1.5 rounded border outline-none resize-none transition-colors"
                            style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
                            rows={4}
                            value={localComment}
                            onChange={(e) => setLocalComment(e.target.value)}
                            onBlur={() => updateState(selectedState.id, { comment: localComment })}
                            placeholder="Add a comment..."
                        />
                    </div>
                </div>
            )}

            {transitionsForSelectedEdge.length > 0 && (
                <div className="space-y-4">
                    <div className="text-xs uppercase tracking-wider font-bold border-b pb-2" style={{ color: theme.ui.textDim, borderColor: theme.ui.border }}>Transitions</div>
                    <div className="space-y-3">
                        {transitionsForSelectedEdge.map((trans) => (
                            <div
                                key={trans.id}
                                ref={el => { if (el) focusedTransRefs.current.set(trans.id, el); else focusedTransRefs.current.delete(trans.id); }}
                                className="p-3 rounded border transition-all duration-500 space-y-3 relative group/trans"
                                style={{
                                    backgroundColor: theme.ui.panelBg,
                                    borderColor: focusTarget?.type === 'transition' && focusTarget.id === trans.id ? theme.ui.accent : theme.ui.border,
                                    boxShadow: focusTarget?.type === 'transition' && focusTarget.id === trans.id ? `0 0 0 1px ${theme.ui.accent}` : undefined
                                }}
                            >
                                <div className="flex justify-between items-center">
                                    <span className="text-[10px] font-mono" style={{ color: theme.ui.textDim }}>
                                        {trans.fromStateId ? (() => {
                                            for (const m of fsm!.machines.values()) {
                                                const s = m.states.get(trans.fromStateId!);
                                                if (s) return s.name || s.type;
                                            }
                                            return 'Any';
                                        })() : 'Any'}
                                        {' → '}
                                        {(() => {
                                            for (const m of fsm!.machines.values()) {
                                                const s = m.states.get(trans.toStateId);
                                                if (s) return s.name || s.type;
                                            }
                                            return '?';
                                        })()}
                                    </span>
                                    <button
                                        onClick={() => removeTransition(trans.id)}
                                        className="text-xs transition-colors"
                                        style={{ color: theme.ui.textDim }}
                                    >✕</button>
                                </div>

                                <div className="space-y-2">
                                    <label className="text-[10px] block uppercase tracking-tighter" style={{ color: theme.ui.textDim }}>Conditions</label>
                                    <div className="flex flex-wrap gap-1.5">
                                        {trans.conditions.map(c => (
                                            <span key={c} className="text-[11px] px-2 py-0.5 rounded-full border flex items-center gap-1.5 group/ev" style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}>
                                                {c}
                                                <button
                                                    onClick={() => removeCondition(trans.id, c)}
                                                    className="leading-none"
                                                    style={{ color: theme.ui.textDim }}
                                                >×</button>
                                            </span>
                                        ))}

                                        {isAddingCondition === trans.id ? (() => {
                                            const condValidation = validateVariableName(newConditionName.trim());
                                            const isCondValid = newConditionName.trim().length === 0 || condValidation.isValid;
                                            return (
                                                <div className={`flex items-center gap-1 rounded-full border p-0.5 pr-2 ${!isCondValid ? 'border-red-500' : ''}`}
                                                    style={{ backgroundColor: theme.ui.background, borderColor: !isCondValid ? undefined : theme.ui.border }}
                                                >
                                                    <input
                                                        ref={addConditionInputRef}
                                                        className="bg-transparent text-[11px] px-2 outline-none w-20"
                                                        style={{ color: theme.ui.textMain }}
                                                        value={newConditionName}
                                                        onChange={e => setNewConditionName(e.target.value)}
                                                        onKeyDown={e => {
                                                            if (e.key === 'Enter') handleAddCondition(trans.id);
                                                            if (e.key === 'Escape') setIsAddingCondition(null);
                                                        }}
                                                        onBlur={() => {
                                                            if (!newConditionName) setIsAddingCondition(null);
                                                        }}
                                                        placeholder="Condition..."
                                                    />
                                                    <button
                                                        onClick={() => handleAddCondition(trans.id)}
                                                        className={condValidation.isValid ? '' : 'cursor-not-allowed'}
                                                        style={{ color: condValidation.isValid ? theme.ui.textMain : theme.ui.textDim }}
                                                        disabled={!condValidation.isValid}
                                                    >✓</button>
                                                    <button onClick={() => setIsAddingCondition(null)} style={{ color: theme.ui.textDim }}>✕</button>
                                                </div>
                                            );
                                        })() : (
                                            <button
                                                onClick={() => setIsAddingCondition(trans.id)}
                                                className="text-[11px] px-2 py-0.5 rounded-full border transition-colors"
                                                style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border, color: theme.ui.textDim }}
                                            >+ Add Condition</button>
                                        )}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>

                    <div className="text-[10px] italic leading-tight mt-4" style={{ color: theme.ui.textDim }}>
                        * Multiple transitions in the same direction are collapsed into a single edge.
                    </div>
                </div>
            )}
        </div>
    );
}
