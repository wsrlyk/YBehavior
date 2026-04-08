/**
 * Debug Store - State management for debugging functionality
 * 
 * Handles:
 * - Network connection to game runtime
 * - Debug session state
 * - Node run states
 * - Variable values during debugging
 * - Breakpoint management
 */

import { create } from 'zustand';
import { invoke } from '@tauri-apps/api/core';
import { getCurrentWindow } from '@tauri-apps/api/window';
import { listen, emit, UnlistenFn } from '@tauri-apps/api/event';
import { useEditorMetaStore } from './editorMetaStore';
import { useEditorStore } from './editorStore';
import { useNotificationStore } from './notificationStore';
import { readFile } from '../utils/fileService';
import { bkdrHash } from '../utils/hashUtils';
import {
    NodeState,
    BreakpointType,
    type NodeRunState,
    type TreeRunInfo,
    type FSMRunInfo,
    type DebugMessage,
    type TickResultData
} from '../types/debug';
import { logger } from '../utils/logger';
import {
    FIELD_DELIMITER,
    SEQUENCE_DELIMITER,
    LIST_DELIMITER
} from '../config/constants';

// ==================== Types ====================

// ==================== Keyframe State ====================

interface KeyframeState {
    pendingFrame: TickResultData | null;
    lastDisplayedFrame: TickResultData | null;
    pendingDiffScore: number;
    displayTimer: ReturnType<typeof setInterval> | null;
}

// Moved to types/debug.ts

// ==================== Store Interface ====================

interface DebugState {
    // Connection state
    isConnected: boolean;
    connectionIP: string;
    connectionPort: number;

    // Debug state
    isDebugging: boolean;
    isPaused: boolean;
    isProcessingSubTree: boolean; // Mutex for handshake
    debugTarget: string | null;
    currentSessionId: string | null; // For multi-window mutual exclusion

    // Run info
    treeRunInfos: Map<string, TreeRunInfo>;
    fsmRunInfo: FSMRunInfo | null;

    // Files with running nodes (for sidebar highlights)
    runningFiles: Map<string, NodeState>;

    // Breakpoints: fileName -> uid -> type
    breakpoints: Map<string, Map<number, BreakpointType>>;

    // Debug variable values (temporary, not persisted)
    debugVariables: Map<string, Map<string, string>>;

    // Keyframe mechanism state
    keyframe: number;
    keyframeState: KeyframeState;

    // Event listener cleanup
    unlistenFns: UnlistenFn[];

    // Actions
    init: () => Promise<void>;
    cleanup: () => void;
    syncBreakpointsFromMeta: () => void;
    connect: (ip: string, port: number) => Promise<void>;
    disconnect: () => Promise<void>;
    send: (message: string) => Promise<void>;

    // Debug commands
    startDebug: (fileName: string, fileType: 'tree' | 'fsm', agentUID?: bigint, waitForBegin?: boolean) => void;
    continueDebug: () => void;
    stepInto: () => void;
    stepOver: () => void;

    // Breakpoint management
    toggleBreakpoint: (fileName: string, uid: number) => void;
    toggleLogpoint: (fileName: string, uid: number) => void;
    getBreakpoint: (fileName: string, uid: number) => BreakpointType;

    // State queries
    getNodeState: (fileName: string, uid: number) => NodeState;
    getNodeRunState: (fileName: string, uid: number) => NodeRunState | undefined;
    getFileRunState: (fileName: string) => NodeState | undefined;
    isFileRunning: (fileName: string) => boolean;

    // Internal handlers
    handleConnectionEvent: (connected: boolean) => void;
    handleMessage: (msg: DebugMessage) => void;
    handleTickResult: (content: string) => void;
    handleSubTrees: (content: string) => void;
    handlePaused: () => void;
    handleLogPoint: (content: string) => void;
    handleSessionStart: (sessionId: string) => void;

    // Keyframe
    displayKeyframe: () => void;
}

// ==================== Store Implementation ====================

// Module-level generation counter for cleanup synchronization
let initGeneration = 0;

export const useDebugStore = create<DebugState>((set, get) => ({
    // Initial state
    isConnected: false,
    connectionIP: '127.0.0.1',
    connectionPort: 8888,
    isDebugging: false,
    isPaused: false,
    isProcessingSubTree: false,
    debugTarget: null,
    currentSessionId: null,
    treeRunInfos: new Map(),
    fsmRunInfo: null,
    runningFiles: new Map(),
    breakpoints: new Map(),
    debugVariables: new Map(),
    keyframe: 0,
    keyframeState: {
        pendingFrame: null,
        lastDisplayedFrame: null,
        pendingDiffScore: 0,
        displayTimer: null,
    },
    unlistenFns: [],

    // Initialize event listeners
    init: async () => {
        get().cleanup(); // Prevent duplicate listeners
        initGeneration++; // Increment generation to invalidate previous pending inits
        const currentGen = initGeneration;

        const unlistenConnection = await listen<{ connected: boolean }>('debug:connection', (event) => {
            get().handleConnectionEvent(event.payload.connected);
        });

        if (currentGen !== initGeneration) {
            unlistenConnection();
            return;
        }

        const unlistenMessage = await listen<DebugMessage>('debug:message', (event) => {
            get().handleMessage(event.payload);
        });

        const unlistenSession = await listen<{ sessionId: string }>('debug:session_start', (event) => {
            get().handleSessionStart(event.payload.sessionId);
        });

        // Check again
        if (currentGen !== initGeneration) {
            unlistenConnection();
            unlistenMessage();
            unlistenSession();
            return;
        }

        // NOTE: display timer is created in handleConnectionEvent(true), not here,
        // to avoid double-timer interference that would make transient highlights permanent.
        set({
            unlistenFns: [unlistenConnection, unlistenMessage, unlistenSession],
        });

        // Initial sync
        get().syncBreakpointsFromMeta();
    },

    // Sync breakpoints from editorMetaStore
    syncBreakpointsFromMeta: () => {
        const { treeMetas } = useEditorMetaStore.getState();
        const initialBreakpoints = new Map<string, Map<number, BreakpointType>>();

        for (const [filePath, treeMeta] of Object.entries(treeMetas)) {
            const fileBps = new Map<number, BreakpointType>();
            for (const [nodeId, nodeMeta] of Object.entries(treeMeta.nodes)) {
                if (nodeMeta.breakpointType) {
                    fileBps.set(parseInt(nodeId, 10), nodeMeta.breakpointType);
                }
            }
            if (fileBps.size > 0) {
                initialBreakpoints.set(filePath, fileBps);
            }
        }
        set({ breakpoints: initialBreakpoints });
    },

    // Cleanup listeners
    cleanup: () => {
        const { unlistenFns, keyframeState } = get();
        unlistenFns.forEach(fn => fn());
        if (keyframeState.displayTimer) {
            clearInterval(keyframeState.displayTimer);
        }
        set({ unlistenFns: [] });
    },

    // Connect to runtime
    connect: async (ip: string, port: number) => {
        try {
            await invoke('debug_connect', { ip, port });
            set({ connectionIP: ip, connectionPort: port });
        } catch (e) {
            throw new Error(String(e));
        }
    },

    // Disconnect from runtime
    disconnect: async () => {
        try {
            await invoke('debug_disconnect');
        } catch (e) {
            console.error('Disconnect error:', e);
        }
    },

    // Send raw message
    send: async (message: string) => {
        try {
            // Append delimiter to separate messages (runtime expects specific splitting)
            const finalMessage = message.endsWith(FIELD_DELIMITER) ? message : message + FIELD_DELIMITER;
            await invoke('debug_send', { message: finalMessage });
        } catch (e) {
            console.error('Send error:', e);
        }
    },

    // Start debugging
    startDebug: (fileName, fileType, agentUID, waitForBegin = false) => {
        const target = (agentUID !== undefined && agentUID !== BigInt(0))
            ? `Agent:${agentUID}`
            : fileName;

        const sessionId = crypto.randomUUID();
        // console.log(`[debugStore] startDebug target=${target} sessionId=${sessionId}`);

        // Reset debug session state so handleSubTrees dedup won't treat the
        // incoming [SubTrees] for the new target as a duplicate of the old one.
        set(state => ({
            debugTarget: target,
            currentSessionId: sessionId,
            isDebugging: false,
            treeRunInfos: new Map(),
            fsmRunInfo: null,
            runningFiles: new Map(),
            // Clear stale frame data so old pendingDiffScore doesn't block new frames.
            // Keep displayTimer so the display loop keeps running.
            keyframeState: {
                ...state.keyframeState,
                pendingFrame: null,
                lastDisplayedFrame: null,
                pendingDiffScore: 0,
            },
        }));

        // Broadcast session start to other windows to clear their state
        emit('debug:session_start', { sessionId });

        // Small delay to ensure other windows receive the event and clear their state
        setTimeout(() => {
            const { send } = get();
            let header: string;
            let content: string;

            if (agentUID !== undefined && agentUID !== BigInt(0)) {
                header = '[DebugAgent]';
                content = agentUID.toString();
            } else {
                header = fileType === 'tree' ? '[DebugTree]' : '[DebugFSM]';
                // Runtime expects pure filename (no path, no extension)
                content = fileName.split(/[\\/]/).pop()?.replace(/\.(tree|fsm)$/, '') || fileName;
            }

            const message = `${header}${FIELD_DELIMITER}${content}${FIELD_DELIMITER}${waitForBegin ? 1 : 0}`;
            send(message);
        }, 50);
    },

    // Handle session start from other windows
    handleSessionStart: (sessionId) => {
        const { currentSessionId } = get();
        if (currentSessionId !== sessionId) {
            set({
                debugTarget: null,
                currentSessionId: null,
                isDebugging: false
            });
        }
    },

    // Continue from breakpoint
    continueDebug: () => {
        const { send, isConnected, isPaused } = get();
        if (isConnected && isPaused) {
            send('[Continue]');
            // Clear run states
            set(state => {
                state.treeRunInfos.forEach(info => info.nodeStates.clear());
                if (state.fsmRunInfo) state.fsmRunInfo.stateInfos.clear();
                return { isPaused: false };
            });
        }
    },

    // Step into
    stepInto: () => {
        const { send, isConnected, isPaused } = get();
        if (isConnected && isPaused) {
            send('[StepInto]');
            set(state => {
                state.treeRunInfos.forEach(info => info.nodeStates.clear());
                if (state.fsmRunInfo) state.fsmRunInfo.stateInfos.clear();
                return { isPaused: false };
            });
        }
    },

    // Step over
    stepOver: () => {
        const { send, isConnected, isPaused } = get();
        if (isConnected && isPaused) {
            send('[StepOver]');
            set(state => {
                state.treeRunInfos.forEach(info => info.nodeStates.clear());
                if (state.fsmRunInfo) state.fsmRunInfo.stateInfos.clear();
                return { isPaused: false };
            });
        }
    },

    // Toggle breakpoint on a node
    toggleBreakpoint: (fileName, uid) => {
        const { breakpoints, isConnected, send } = get();
        const fileBreakpoints = breakpoints.get(fileName) || new Map();
        const current = fileBreakpoints.get(uid) || BreakpointType.None;

        let newType: BreakpointType;
        if (current === BreakpointType.Breakpoint) {
            newType = BreakpointType.None;
        } else {
            newType = BreakpointType.Breakpoint;
        }

        if (newType === BreakpointType.None) {
            fileBreakpoints.delete(uid);
        } else {
            fileBreakpoints.set(uid, newType);
        }

        const newBreakpoints = new Map(breakpoints);
        newBreakpoints.set(fileName, fileBreakpoints);
        set({ breakpoints: newBreakpoints });

        // Persist
        useEditorMetaStore.getState().setNodeBreakpoint(fileName, uid.toString(), newType);

        // Send to runtime if connected
        if (isConnected) {
            const count = newType === BreakpointType.Breakpoint ? 1 : 0;
            // Runtime expects pure filename (no path, no extension)
            const treeName = fileName.split(/[\\/]/).pop()?.replace(/\.(tree|fsm)$/, '') || fileName;
            send(`[DebugTreePoint]${FIELD_DELIMITER}${treeName}${FIELD_DELIMITER}${uid}${FIELD_DELIMITER}${count}`);
        }
    },

    // Toggle logpoint on a node
    toggleLogpoint: (fileName, uid) => {
        const { breakpoints, isConnected, send } = get();
        const fileBreakpoints = breakpoints.get(fileName) || new Map();
        const current = fileBreakpoints.get(uid) || BreakpointType.None;

        let newType: BreakpointType;
        if (current === BreakpointType.Logpoint) {
            newType = BreakpointType.None;
        } else {
            newType = BreakpointType.Logpoint;
        }

        if (newType === BreakpointType.None) {
            fileBreakpoints.delete(uid);
        } else {
            fileBreakpoints.set(uid, newType);
        }

        const newBreakpoints = new Map(breakpoints);
        newBreakpoints.set(fileName, fileBreakpoints);
        set({ breakpoints: newBreakpoints });

        // Persist
        useEditorMetaStore.getState().setNodeBreakpoint(fileName, uid.toString(), newType);

        // Send to runtime if connected
        if (isConnected) {
            const count = newType === BreakpointType.Logpoint ? -1 : 0;
            // Runtime expects pure filename (no path, no extension)
            const treeName = fileName.split(/[\\/]/).pop()?.replace(/\.(tree|fsm)$/, '') || fileName;
            send(`[DebugTreePoint]${FIELD_DELIMITER}${treeName}${FIELD_DELIMITER}${uid}${FIELD_DELIMITER}${count}`);
        }
    },

    // Get breakpoint type for a node
    getBreakpoint: (fileName, uid) => {
        const { breakpoints } = get();
        return breakpoints.get(fileName)?.get(uid) || BreakpointType.None;
    },

    // Get node run state (simple, for backward compatibility or simple status)
    getNodeState: (fileName, uid) => {
        const { treeRunInfos, fsmRunInfo } = get();

        const treeInfo = treeRunInfos.get(fileName);
        if (treeInfo) {
            const state = treeInfo.nodeStates.get(uid);
            return state?.final ?? NodeState.Invalid;
        }

        if (fsmRunInfo?.fsmName === fileName) {
            const state = fsmRunInfo.stateInfos.get(uid);
            return state !== undefined ? state as NodeState : NodeState.Invalid;
        }

        return NodeState.Invalid;
    },

    // Get detailed node run state (self + final)
    getNodeRunState: (fileName: string, uid: number): NodeRunState | undefined => {
        const { treeRunInfos } = get();

        // Exact match first
        const exactInfo = treeRunInfos.get(fileName);
        if (exactInfo) {
            return exactInfo.nodeStates.get(uid);
        }

        // Fuzzy match: basename vs full relative path
        // e.g. query "combat_subtree_forcastskill" matches key "Game/combat_subtree_forcastskill"
        const normQuery = fileName.replace(/\\/g, '/');
        for (const [key, info] of treeRunInfos) {
            const normKey = key.replace(/\\/g, '/');
            if (normKey.endsWith('/' + normQuery) || normQuery.endsWith('/' + normKey)) {
                return info.nodeStates.get(uid);
            }
        }

        return undefined;
    },

    // Check if file has running nodes
    getFileRunState: (fileName: string): NodeState | undefined => {
        const { runningFiles } = get();
        if (runningFiles.has(fileName)) return runningFiles.get(fileName);

        // Check fuzzy match (if fileName is basename "SimpleFSM" and running is "Path/SimpleFSM")
        // runningFiles is Map, iterating entries
        let bestState: NodeState | undefined;

        for (const [runningName, state] of runningFiles) {
            // Case 1: Running (Path) ends with Query (Basename)
            // e.g. Running="A/B/C", Query="C" -> Match
            const runningEndsWithQuery = runningName.endsWith('/' + fileName) || runningName.endsWith('\\' + fileName);

            // Case 2: Query (Path) ends with Running (Basename)
            // e.g. Running="C", Query="A/B/C" -> Match
            const queryEndsWithRunning = fileName.endsWith('/' + runningName) || fileName.endsWith('\\' + runningName);

            if (runningEndsWithQuery || queryEndsWithRunning) {
                // If Break, it wins immediately
                if (state === NodeState.Break) return NodeState.Break;
                bestState = state;
            }
        }
        return bestState;
    },

    // Check if file has running nodes
    isFileRunning: (fileName: string) => {
        const state = get().getFileRunState(fileName);
        return state !== undefined;
    },

    // Handle connection state change
    handleConnectionEvent: (connected) => {
        if (connected) {
            // Start keyframe timer
            const timer = setInterval(() => {
                get().displayKeyframe();
            }, 1000);

            set({
                isConnected: true,
                keyframeState: {
                    ...get().keyframeState,
                    displayTimer: timer,
                },
            });
        } else {
            // Clean up debugging state
            const { keyframeState } = get();
            if (keyframeState.displayTimer) {
                clearInterval(keyframeState.displayTimer);
            }

            set({
                isConnected: false,
                isDebugging: false,
                isPaused: false,
                debugTarget: null,
                treeRunInfos: new Map(),
                fsmRunInfo: null,
                runningFiles: new Map(),
                debugVariables: new Map(),
                keyframeState: {
                    pendingFrame: null,
                    lastDisplayedFrame: null,
                    pendingDiffScore: 0,
                    displayTimer: null,
                },
            });
        }
    },

    // Handle incoming message
    handleMessage: (msg) => {
        const { header, content } = msg;

        switch (header) {
            case '[TickResult]':
                get().handleTickResult(content);
                break;
            case '[SubTrees]':
                get().handleSubTrees(content);
                break;
            case '[Paused]':
                get().handlePaused();
                break;
            case '[LogPoint]':
                get().handleLogPoint(content);
                break;
            default:
                console.log('Unknown message:', header);
        }
    },

    // Handle tick result (keyframe mechanism)
    handleTickResult: (content) => {
        // Parse tick result data
        const parts = content.split(FIELD_DELIMITER);
        if (parts.length < 2) return;

        const data: TickResultData = {
            mainData: parts[0] || '',
            fsmRunData: parts[1] || '',
            treeRunDatas: new Map(),
        };

        // Parse tree data (format: treeName, localData, runData, ...)
        for (let i = 2; i < parts.length; i += 3) {
            if (i + 2 < parts.length) {
                const treeName = parts[i];
                // Ignore empty tree names
                if (!treeName) continue;

                data.treeRunDatas.set(treeName, {
                    name: treeName,
                    localData: parts[i + 1],
                    runData: parts[i + 2],
                });
            }
        }

        const { isPaused, keyframeState } = get();

        // If paused, display immediately
        if (isPaused) {
            set({
                keyframeState: {
                    ...keyframeState,
                    pendingFrame: data,
                    pendingDiffScore: Infinity, // Force update
                },
            });
            get().displayKeyframe();
            return;
        }

        // Calculate diff score
        const score = calculateDiffScore(keyframeState.lastDisplayedFrame, data);

        if (score >= keyframeState.pendingDiffScore) {
            set({
                keyframeState: {
                    ...keyframeState,
                    pendingFrame: data,
                    pendingDiffScore: score,
                },
            });
        }
    },

    // Display the pending keyframe
    displayKeyframe: () => {
        const { keyframeState, treeRunInfos, fsmRunInfo, isPaused } = get();

        if (!keyframeState.pendingFrame) return;

        const data = keyframeState.pendingFrame;
        const newTreeRunInfos = new Map(treeRunInfos);
        const newRunningFiles = new Map<string, NodeState>();
        const currentKeyframe = get().keyframe + 1;

        if (isPaused) {
            // paused mode keeps latest frame visible; avoid heavy logging per frame
        }

        // Parse shared variables once per frame (was repeatedly split for every tree)
        const sharedPairs: Array<[string, string]> = [];
        if (data.mainData) {
            const sharedItems = data.mainData.split(LIST_DELIMITER);
            for (const item of sharedItems) {
                const parts = item.split(SEQUENCE_DELIMITER);
                if (parts.length >= 2) {
                    sharedPairs.push([parts[0], parts[1]]);
                }
            }
        }

        // Process tree run data
        for (const [treeName, treeData] of data.treeRunDatas) {
            // Exact key lookup first
            let existing = newTreeRunInfos.get(treeName);
            let effectiveKey = treeName;

            // Fuzzy fallback: if not found by exact key, find a key that ends with the same path segment.
            // This handles mismatches between full relative paths (SubTrees) and basenames (keyframe).
            if (!existing) {
                const normName = treeName.replace(/\\/g, '/');
                for (const [key, val] of newTreeRunInfos) {
                    const normKey = key.replace(/\\/g, '/');
                    if (normKey.endsWith('/' + normName) || normName.endsWith('/' + normKey) || normKey === normName) {
                        existing = val;
                        effectiveKey = key;
                        break;
                    }
                }
            }

            const info = existing || {
                treeName,
                nodeStates: new Map(),
                sharedVariables: new Map(),
                localVariables: new Map(),
                sharedVariableTimestamps: new Map(),
                localVariableTimestamps: new Map(),
            };

            info.nodeStates.clear();

            // Parse run data: uid\x06selfState\x06finalState\x07...
            const runItems = treeData.runData.split(LIST_DELIMITER);
            for (const item of runItems) {
                const parts = item.split(SEQUENCE_DELIMITER);
                if (parts.length >= 3) {
                    const uid = parseInt(parts[0], 10);
                    const selfState = parseInt(parts[1], 10) as NodeState;
                    const finalState = parseInt(parts[2], 10) as NodeState;
                    info.nodeStates.set(uid, { self: selfState, final: finalState });

                    if (finalState === NodeState.Running || finalState === NodeState.Break) {
                        const current = newRunningFiles.get(effectiveKey);
                        // Priority: Break > Running
                        if (current !== NodeState.Break) {
                            if (finalState === NodeState.Break) newRunningFiles.set(effectiveKey, NodeState.Break);
                            else if (!current) newRunningFiles.set(effectiveKey, NodeState.Running);
                        }
                    }
                }
            }

            // Parse local variables
            const localItems = treeData.localData.split(LIST_DELIMITER);
            for (const item of localItems) {
                const parts = item.split(SEQUENCE_DELIMITER);
                if (parts.length >= 2) {
                    const name = parts[0];
                    const value = parts[1];
                    const oldValue = info.localVariables.get(name);
                    if (oldValue !== value) {
                        info.localVariableTimestamps.set(name, currentKeyframe);
                    }
                    info.localVariables.set(name, value);
                }
            }

            // Apply shared variables parsed once for this frame
            for (const [name, value] of sharedPairs) {
                const oldValue = info.sharedVariables.get(name);
                if (oldValue !== value) {
                    info.sharedVariableTimestamps.set(name, currentKeyframe);
                }
                info.sharedVariables.set(name, value);
            }

            newTreeRunInfos.set(effectiveKey, info);
        }

        // Process FSM run data
        let newFsmRunInfo = fsmRunInfo;
        if (data.fsmRunData) {
            const fsmItems = data.fsmRunData.split(LIST_DELIMITER);
            if (fsmItems.length > 0) {
                if (!newFsmRunInfo) {
                    newFsmRunInfo = { fsmName: get().debugTarget || '', stateInfos: new Map() };
                }
                newFsmRunInfo.stateInfos.clear();

                for (const item of fsmItems) {
                    const parts = item.split(SEQUENCE_DELIMITER);
                    if (parts.length >= 2) {
                        const uid = parseInt(parts[0], 10);
                        const state = parseInt(parts[1], 10);
                        newFsmRunInfo.stateInfos.set(uid, state);

                        if (state === NodeState.Running || state === NodeState.Break) {
                            const current = newRunningFiles.get(newFsmRunInfo.fsmName);
                            if (current !== NodeState.Break) {
                                if (state === NodeState.Break) newRunningFiles.set(newFsmRunInfo.fsmName, NodeState.Break);
                                else if (!current) newRunningFiles.set(newFsmRunInfo.fsmName, NodeState.Running);
                            }
                        }
                    }
                }
            }
        }

        set({
            treeRunInfos: newTreeRunInfos,
            fsmRunInfo: newFsmRunInfo,
            runningFiles: newRunningFiles,
            keyframe: get().keyframe + 1,
            keyframeState: {
                ...keyframeState,
                lastDisplayedFrame: data,
                pendingFrame: null,
                pendingDiffScore: 0,
            },
        });
    },

    // Handle SubTrees message
    handleSubTrees: (content) => {
        const label = getCurrentWindow().label;
        console.log(`[debugStore ${label}] handleSubTrees. CurrentTarget=${get().debugTarget}. Content=${content.substring(0, 50)}...`);
        // Only handle if this instance initiated debugging
        if (!get().debugTarget) return;

        // Format: name\x06hash\x07name\x06hash...
        const items = content.split(LIST_DELIMITER);
        const fileHashes: Array<{ name: string; hash: number }> = [];

        for (const item of items) {
            const parts = item.split(SEQUENCE_DELIMITER);
            if (parts.length >= 2) {
                const name = parts[0];
                if (!name) continue; // Ignore empty tree names

                fileHashes.push({
                    name: name,
                    hash: parseInt(parts[1], 10),
                });
            }
        }

        if (fileHashes.length > 0) {
            // Deduplicate: If we are already debugging this target, ignore
            // This prevents duplicate DebugBegin messages if runtime sends multiple SubTrees
            const newTarget = fileHashes[0].name;
            const currentTarget = get().debugTarget;

            // Deduplication logic:
            // 1. Direct match: target is the same as the new file.
            // 2. Agent match: we are debugging an agent and already have data for this file.
            const isMatch = (currentTarget === newTarget) ||
                (currentTarget?.replace(/\\/g, '/') === newTarget.replace(/\\/g, '/')) || // Normalized match
                (currentTarget?.startsWith('Agent:') && (get().treeRunInfos.has(newTarget) || get().fsmRunInfo?.fsmName === newTarget));

            console.log(`[debugStore] Dedupe Check: '${currentTarget}' vs '${newTarget}' (${isMatch ? 'MATCH' : 'DIFF'})`);

            if (isMatch && get().isDebugging) {
                console.log('[debugStore] Deduplication hit. Ignoring.');
                return;
            }
            // Verify hashes
            const { editorTreeDir, runtimeTreeDir } = useEditorStore.getState();
            const checkDir = runtimeTreeDir || editorTreeDir;
            if (checkDir) {
                // Verify async but don't block
                (async () => {
                    const mismatches: string[] = [];
                    for (const file of fileHashes) {
                        try {
                            // Try tree file first
                            const treePath = `${checkDir}/${file.name}.tree`;
                            // If not exists, maybe .fsm? 
                            // But protocol says name includes full rel path usually?
                            // Actually file.name usually lacks extension in old editor? 
                            // YBehavior uses "Character/Monster/AI_Monster" (no ext).
                            // We need to try both .tree and .fsm?
                            // Or just read file with assumed path logic.
                            // Let's rely on simple trial or just matching name with .tree/.fsm

                            // Using trial:
                            let content = '';
                            try {
                                content = await readFile(treePath);
                            } catch {
                                try {
                                    content = await readFile(`${checkDir}/${file.name}.fsm`);
                                } catch {
                                    try {
                                        content = await readFile(`${checkDir}/${file.name}`);
                                    } catch {
                                        console.warn(`Could not find file for hash check: ${file.name} in ${checkDir}`);
                                        continue;
                                    }
                                }
                            }

                            if (content) {
                                const remoteHash = file.hash >>> 0;
                                let localHash = 0;

                                try {
                                    const parser = new DOMParser();
                                    // Strip BOM for parser
                                    const xmlString = content.replace(/^\uFEFF/, '');
                                    const doc = parser.parseFromString(xmlString, 'text/xml');

                                    if (!doc.querySelector('parsererror')) {
                                        // Runtime hashes the root element's outer XML without any whitespace
                                        const serializer = new XMLSerializer();
                                        const rootXml = serializer.serializeToString(doc.documentElement);
                                        const minifiedXml = rootXml.replace(/\s/g, '');
                                        localHash = bkdrHash(minifiedXml);
                                    } else {
                                        // Fallback to raw string if XML parse fails
                                        localHash = bkdrHash(xmlString.replace(/\r\n/g, '\n'));
                                    }
                                } catch (e) {
                                    console.error('Failed to parse XML for hash check:', e);
                                    localHash = bkdrHash(content.replace(/\r\n/g, '\n'));
                                }

                                if (localHash !== remoteHash) {
                                    mismatches.push(file.name);
                                    console.warn(`Hash mismatch for ${file.name}: Local=${localHash}, Remote=${remoteHash}`);
                                }
                            }
                        } catch (e) {
                            console.error('Error verifying hash:', e);
                        }
                    }

                    if (mismatches.length > 0) {
                        useNotificationStore.getState().notify(
                            `File version mismatch detected for: ${mismatches.join(', ')}. Debugging might be inaccurate.`,
                            'warning'
                        );
                    }
                })();
            }

            // The first entry in SubTrees is always the FSM, the rest are Trees.
            // Use pure basename (no path, no extension) to match runtime protocol
            const fsmName = fileHashes[0].name.split(/[\\/]/).pop() || fileHashes[0].name;

            // Mark as debugging and record the FSM filename definitively
            set(state => ({
                isDebugging: true,
                // Keep Agent: target if it exists, otherwise use the first file name
                debugTarget: (state.debugTarget && state.debugTarget.startsWith('Agent:'))
                    ? state.debugTarget
                    : fsmName,
                // Initialize FSM run info with the first file (which is the FSM/Root)
                fsmRunInfo: {
                    fsmName: fsmName,
                    stateInfos: new Map()
                }
            }));

            // Notify user that debugging has truly started
            {
                const target = get().debugTarget;
                const msg = (target && target.startsWith('Agent:'))
                    ? `开始调试 (UID: ${target.slice(6)})`
                    : `开始调试: ${fsmName}`;
                useNotificationStore.getState().notify(msg, 'success');
            }

            // Only trees (index 1+) go into treeRunInfos; the FSM (index 0) lives in fsmRunInfo
            const newTreeRunInfos = new Map<string, TreeRunInfo>();
            for (let i = 1; i < fileHashes.length; i++) {
                const file = fileHashes[i];
                // Use pure basename (no path, no extension) as key - matches [TickResult] format
                const normName = file.name.split(/[\\/]/).pop() || file.name;
                newTreeRunInfos.set(normName, {
                    treeName: normName,
                    nodeStates: new Map(),
                    sharedVariables: new Map(),
                    localVariables: new Map(),
                    sharedVariableTimestamps: new Map(),
                    localVariableTimestamps: new Map(),
                });
            }

            set({ treeRunInfos: newTreeRunInfos });

            // Send DebugBegin with breakpoints
            const { breakpoints, send } = get();
            let message = '[DebugBegin]';

            for (const file of fileHashes) {
                // Runtime expects pure filename (no path, no extension)
                message += FIELD_DELIMITER + (file.name.split(/[\\/]/).pop() || file.name);

                // Fuzzy lookup for breakpoints (handle Path/File vs File.ext)
                let fileBreakpoints: Map<number, BreakpointType> | undefined;

                // 1. Try exact match
                if (breakpoints.has(file.name)) {
                    fileBreakpoints = breakpoints.get(file.name);
                } else {
                    // 2. Try fuzzy match
                    // Runtime file: "StateMachine/SimpleFSM" or "SimpleFSM"
                    // Store key: "e:/.../SimpleFSM.fsm" or "SimpleFSM"
                    const runtimeBase = file.name.split(/[\\/]/).pop()?.replace(/\.(tree|fsm)$/, '') || file.name;

                    for (const [key, bps] of breakpoints) {
                        const keyBase = key.split(/[\\/]/).pop()?.replace(/\.(tree|fsm)$/, '') || key;
                        if (keyBase === runtimeBase) {
                            fileBreakpoints = bps;
                            break;
                        }
                    }
                }

                if (fileBreakpoints) {
                    for (const [uid, type] of fileBreakpoints) {
                        message += SEQUENCE_DELIMITER + uid + SEQUENCE_DELIMITER + type;
                    }
                }
            }

            send(message);
        }
    },

    // Handle Paused message
    handlePaused: () => {
        set({ isPaused: true });
        // Force display of pending frame
        get().displayKeyframe();
    },

    // Handle LogPoint message
    handleLogPoint: (content) => {
        // Only main window handles logging to prevent duplicates
        if (getCurrentWindow().label !== 'main') return;

        // Parse and log the content: Message(0) RawState(1) FinalState(2) [Detail...]
        const parts = content.split(FIELD_DELIMITER);
        if (parts.length > 0) {
            const message = parts[0];
            const segments: { text: string, color?: string }[] = [];

            // Header: Accent bracket, bright content, Accent bracket
            segments.push({ text: `-------<LogPoint `, color: 'log-accent' });
            segments.push({ text: `${message}`, color: 'log-bright' });
            segments.push({ text: `>-------\n`, color: 'log-accent' });

            let index = 1;
            // Parse states
            let rawState = NodeState.Invalid;
            let finalState = NodeState.Invalid;

            if (index < parts.length) {
                rawState = parseInt(parts[index++], 10) as NodeState;
            }
            if (index < parts.length) {
                finalState = parseInt(parts[index++], 10) as NodeState;
            }

            // Parse details (BEFORE/AFTER)
            while (index < parts.length) {
                const section = parts[index++];
                if (section === 'BEFORE' || section === 'AFTER') {
                    segments.push({ text: `\n${section}:\n`, color: 'log-warn' });
                    if (index < parts.length) {
                        const count = parseInt(parts[index++], 10);
                        if (!isNaN(count)) {
                            for (let i = 0; i < count && index < parts.length; i++) {
                                // Variable: Value
                                segments.push({ text: `  ${parts[index++]}\n`, color: 'log-dim' });
                            }
                        }
                    }
                } else {
                    // Try to handle unexpected tokens gracefully
                    segments.push({ text: `${section}\n`, color: 'log-dim' });
                }
            }

            // Footer with state
            segments.push({ text: `\nResult: `, color: 'log-dim' });

            const getStateColor = (s: NodeState) => {
                switch (s) {
                    case NodeState.Success: return 'log-success';
                    case NodeState.Failure: return 'log-failure';
                    case NodeState.Running: return 'log-running';
                    case NodeState.Break: return 'log-break';
                    default: return 'log-dim';
                }
            };

            segments.push({ text: `${NodeState[finalState]}`, color: getStateColor(finalState) });
            segments.push({ text: ` (Raw: `, color: 'log-dim' });
            segments.push({ text: `${NodeState[rawState]}`, color: getStateColor(rawState) });
            segments.push({ text: `)\n`, color: 'log-dim' });

            segments.push({ text: `-------</LogPoint>-------`, color: 'log-accent' });

            logger.info(segments);
        }
    },
}));

// ==================== Helper Functions ====================

/**
 * Calculate difference score between two tick results.
 * Higher score = more significant change = should be displayed.
 */
function calculateDiffScore(prev: TickResultData | null, curr: TickResultData): number {
    if (!prev) return Infinity; // First frame must be displayed

    let score = 0;

    // Count tree state changes
    for (const [treeName, treeData] of curr.treeRunDatas) {
        const prevTree = prev.treeRunDatas.get(treeName);
        if (!prevTree) {
            score += 50; // New tree
            continue;
        }

        // Count run state differences
        const currRun = treeData.runData;
        const prevRun = prevTree.runData;
        if (currRun !== prevRun) {
            // Quick estimate based on string length difference
            score += Math.abs(currRun.length - prevRun.length) * 2;
            score += 10; // Base change score
        }

        // Variable changes
        if (treeData.localData !== prevTree.localData) {
            score += 5;
        }
    }

    // FSM changes
    if (curr.fsmRunData !== prev.fsmRunData) {
        score += 15;
    }

    // Shared variable changes
    if (curr.mainData !== prev.mainData) {
        score += 5;
    }

    return score;
}

// HMR Cleanup
if (import.meta.hot) {
    import.meta.hot.dispose(() => {
        console.log('[debugStore] HMR Dispose. Cleaning up listeners.');
        useDebugStore.getState().cleanup();
    });
}
