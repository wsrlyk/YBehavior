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
import { listen, UnlistenFn } from '@tauri-apps/api/event';
import { useEditorMetaStore } from './editorMetaStore';
import { useEditorStore } from './editorStore';
import { useNotificationStore } from './notificationStore';
import { readFile } from '../utils/fileService';
import { bkdrHash } from '../utils/hashUtils';
import {
    NodeState,
    BreakpointType,
    type TreeRunInfo,
    type FSMRunInfo,
    type DebugMessage,
    type TickResultData
} from '../types/debug';
import {
    FIELD_DELIMITER,
    SECTION_DELIMITER,
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
    debugTarget: string | null;

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
    getFileRunState: (fileName: string) => NodeState | undefined;
    isFileRunning: (fileName: string) => boolean;

    // Internal handlers
    handleConnectionEvent: (connected: boolean) => void;
    handleMessage: (msg: DebugMessage) => void;
    handleTickResult: (content: string) => void;
    handleSubTrees: (content: string) => void;
    handlePaused: () => void;
    handleLogPoint: (content: string) => void;

    // Keyframe
    displayKeyframe: () => void;
}

// ==================== Store Implementation ====================

export const useDebugStore = create<DebugState>((set, get) => ({
    // Initial state
    isConnected: false,
    connectionIP: '127.0.0.1',
    connectionPort: 8888,
    isDebugging: false,
    isPaused: false,
    debugTarget: null,
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
        const unlistenConnection = await listen<{ connected: boolean }>('debug:connection', (event) => {
            get().handleConnectionEvent(event.payload.connected);
        });

        const unlistenMessage = await listen<DebugMessage>('debug:message', (event) => {
            get().handleMessage(event.payload);
        });

        // Start display timer (1fps)
        const timer = setInterval(() => {
            get().displayKeyframe();
        }, 1000);

        set(state => ({
            unlistenFns: [unlistenConnection, unlistenMessage],
            keyframeState: {
                ...state.keyframeState,
                displayTimer: timer
            }
        }));

        // Load breakpoints from meta
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
            await invoke('debug_send', { message });
        } catch (e) {
            console.error('Send error:', e);
        }
    },

    // Start debugging
    startDebug: (fileName, fileType, agentUID, waitForBegin = false) => {
        set({ debugTarget: fileName });
        const { send } = get();
        let header: string;
        let content: string;

        if (agentUID !== undefined && agentUID !== BigInt(0)) {
            header = '[DebugAgent]';
            content = agentUID.toString();
        } else {
            header = fileType === 'tree' ? '[DebugTree]' : '[DebugFSM]';
            content = fileName;
        }

        const message = `${header}${FIELD_DELIMITER}${content}${FIELD_DELIMITER}${waitForBegin ? 1 : 0}`;
        send(message);
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
            send(`[DebugTreePoint]${FIELD_DELIMITER}${fileName}${FIELD_DELIMITER}${uid}${FIELD_DELIMITER}${count}`);
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
            send(`[DebugTreePoint]${FIELD_DELIMITER}${fileName}${FIELD_DELIMITER}${uid}${FIELD_DELIMITER}${count}`);
        }
    },

    // Get breakpoint type for a node
    getBreakpoint: (fileName, uid) => {
        const { breakpoints } = get();
        return breakpoints.get(fileName)?.get(uid) || BreakpointType.None;
    },

    // Get node run state
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
                data.treeRunDatas.set(parts[i], {
                    name: parts[i],
                    localData: parts[i + 1],
                    runData: parts[i + 2],
                });
            }
        }

        const { isPaused, keyframeState } = get();

        // If paused, display immediately
        if (isPaused) {
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
        const { keyframeState, treeRunInfos, fsmRunInfo } = get();

        if (!keyframeState.pendingFrame) return;

        const data = keyframeState.pendingFrame;
        const newTreeRunInfos = new Map(treeRunInfos);
        const newRunningFiles = new Map<string, NodeState>();
        const currentKeyframe = get().keyframe + 1;

        // Process tree run data
        for (const [treeName, treeData] of data.treeRunDatas) {
            const info = newTreeRunInfos.get(treeName) || {
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
                        const current = newRunningFiles.get(treeName);
                        // Priority: Break > Running
                        if (current !== NodeState.Break) {
                            if (finalState === NodeState.Break) newRunningFiles.set(treeName, NodeState.Break);
                            else if (!current) newRunningFiles.set(treeName, NodeState.Running);
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

            // Parse shared variables from mainData
            const sharedItems = data.mainData.split(LIST_DELIMITER);
            for (const item of sharedItems) {
                const parts = item.split(SEQUENCE_DELIMITER);
                if (parts.length >= 2) {
                    const name = parts[0];
                    const value = parts[1];
                    const oldValue = info.sharedVariables.get(name);
                    if (oldValue !== value) {
                        info.sharedVariableTimestamps.set(name, currentKeyframe);
                    }
                    info.sharedVariables.set(name, value);
                }
            }

            newTreeRunInfos.set(treeName, info);
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
                        // console.log(`[DebugStore] FSM ${newFsmRunInfo.fsmName} State: ${uid} -> ${state}`);

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
        // Format: name\x06hash\x07name\x06hash...
        const items = content.split(LIST_DELIMITER);
        const fileHashes: Array<{ name: string; hash: number }> = [];

        for (const item of items) {
            const parts = item.split(SEQUENCE_DELIMITER);
            if (parts.length >= 2) {
                fileHashes.push({
                    name: parts[0],
                    hash: parseInt(parts[1], 10),
                });
            }
        }

        if (fileHashes.length > 0) {
            // Verify hashes
            const { editorTreeDir } = useEditorStore.getState();
            if (editorTreeDir) {
                // Verify async but don't block
                (async () => {
                    const mismatches: string[] = [];
                    for (const file of fileHashes) {
                        try {
                            // Try tree file first
                            const treePath = `${editorTreeDir}/${file.name}.tree`;
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
                                    content = await readFile(`${editorTreeDir}/${file.name}.fsm`);
                                } catch {
                                    try {
                                        content = await readFile(`${editorTreeDir}/${file.name}`);
                                    } catch {
                                        console.warn(`Could not find file for hash check: ${file.name}`);
                                        continue;
                                    }
                                }
                            }

                            if (content) {
                                const localHash = bkdrHash(content);
                                // JS integers are signed for bitwise, unsigned for logic?
                                // Our bkdrHash returns unsigned >>> 0.
                                // Protocol hash is parsed as int.
                                // We should compare them carefully.
                                if (localHash !== (file.hash >>> 0)) {
                                    mismatches.push(file.name);
                                    console.warn(`Hash mismatch for ${file.name}: Local=${localHash}, Remote=${file.hash}`);
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

            // For now, just mark as debugging
            set({
                isDebugging: true,
                debugTarget: fileHashes[0].name,
            });

            // Build run info for all files
            const newTreeRunInfos = new Map<string, TreeRunInfo>();
            for (const file of fileHashes) {
                // Skip first file if it's FSM (determined by caller)
                newTreeRunInfos.set(file.name, {
                    treeName: file.name,
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
                message += FIELD_DELIMITER + file.name;

                const fileBreakpoints = breakpoints.get(file.name);
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
        // Parse and log the content
        const parts = content.split(FIELD_DELIMITER);
        if (parts.length > 0) {
            console.log('[LogPoint]', parts[0]);
            // TODO: Display in a log panel UI
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
