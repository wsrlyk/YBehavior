/**
 * Debug Toolbar Component
 * 
 * Provides UI for:
 * - Connecting/disconnecting to game runtime
 * - Starting debug sessions
 * - Debug controls (continue, step into, step over)
 * - Connection status display
 */

import { useState, useEffect } from 'react';
import { useDebugStore } from '../stores/debugStore';
import { useEditorMetaStore } from '../stores/editorMetaStore';
import { useEditorStore } from '../stores/editorStore';
import { useFSMStore } from '../stores/fsmStore';
import { useNotificationStore } from '../stores/notificationStore';
import { getTheme } from '../theme/theme';

// Icons as simple SVG components
const PlayIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
);

const ContinueIcon = () => (
    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
        <path d="M8 5v14l11-7z" />
    </svg>
);

const StepIntoIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 14l-7 7m0 0l-7-7m7 7V3" />
    </svg>
);

const StepOverIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 5l7 7-7 7M5 5l7 7-7 7" />
    </svg>
);

const ConnectIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
    </svg>
);

const DisconnectIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
    </svg>
);

export function DebugToolbar() {
    const theme = getTheme();
    const { isConnected, isPaused, isDebugging, connect, disconnect, startDebug, continueDebug, stepInto, stepOver } = useDebugStore();
    const { debugMeta, setDebugIP, setDebugPort } = useEditorMetaStore();
    const { notify } = useNotificationStore();

    // Get active file info from editor stores
    const activeFilePath = useEditorStore(s => s.activeFilePath);
    const activeFSMPath = useFSMStore(s => s.activeFSMPath);
    const openedFiles = useEditorStore(s => s.openedFiles);
    const openedFSMFiles = useFSMStore(s => s.openedFSMFiles);

    // Check for unsaved files
    const hasUnsavedFiles = [...openedFiles, ...openedFSMFiles].some(f => f.isDirty);

    const [localIP, setLocalIP] = useState(debugMeta.ip);
    const [localPort, setLocalPort] = useState(debugMeta.port.toString());
    const [agentUID, setAgentUID] = useState('');
    const [waitForBegin, setWaitForBegin] = useState(false);
    const [isConnecting, setIsConnecting] = useState(false);

    // Sync local state with store on load
    useEffect(() => {
        setLocalIP(debugMeta.ip);
        setLocalPort(debugMeta.port.toString());
    }, [debugMeta.ip, debugMeta.port]);

    const handleConnect = async () => {
        // Check for unsaved files
        if (hasUnsavedFiles) {
            notify('请先保存所有文件后再连接调试', 'warning');
            return;
        }

        // Save settings
        setDebugIP(localIP);
        const port = parseInt(localPort, 10);
        if (isNaN(port) || port < 1 || port > 65535) {
            notify('端口号无效', 'error');
            return;
        }
        setDebugPort(port);

        setIsConnecting(true);
        try {
            await connect(localIP, port);
            notify(`已连接到 ${localIP}:${port}`, 'success');
        } catch (e) {
            notify(`连接失败: ${e}`, 'error');
        } finally {
            setIsConnecting(false);
        }
    };

    const handleDisconnect = async () => {
        await disconnect();
        notify('已断开连接', 'info');
    };

    const handleStartDebug = () => {
        const activeFile = activeFilePath || activeFSMPath;
        const uid = agentUID ? BigInt(agentUID) : undefined;

        if (!activeFile && !uid) {
            notify('请先打开一个树/状态机文件，或输入Agent UID', 'warning');
            return;
        }

        // Check if explicitly FSM (only if FSM is active and Tree is not, or user clicked FSM tab)
        // If no file open, default to Tree
        const fileType = activeFSMPath ? 'fsm' : 'tree';

        // Extract filename from path
        let fileName = '';
        if (activeFile) {
            fileName = activeFile.split(/[\\/]/).pop()?.replace(/\.(tree|fsm)$/, '') || activeFile;
        }

        startDebug(fileName, fileType as 'tree' | 'fsm', uid, waitForBegin);
        notify(uid ? `等待调试 (UID: ${agentUID})` : `等待调试: ${fileName}`, 'info');
    };

    return (
        <div className="flex items-center gap-2 text-xs">
            {!isConnected ? (
                // Connection form
                <>
                    <div className="flex items-center gap-1">
                        <span style={{ color: theme.ui.textDim }}>IP:</span>
                        <input
                            type="text"
                            value={localIP}
                            onChange={(e) => setLocalIP(e.target.value)}
                            className="w-24 px-1.5 py-0.5 border rounded text-xs focus:outline-none"
                            style={{ backgroundColor: theme.ui.inputBg, borderColor: theme.ui.border, color: theme.ui.textMain }}
                            placeholder="127.0.0.1"
                        />
                    </div>
                    <div className="flex items-center gap-1">
                        <span style={{ color: theme.ui.textDim }}>Port:</span>
                        <input
                            type="text"
                            value={localPort}
                            onChange={(e) => setLocalPort(e.target.value)}
                            className="w-14 px-1.5 py-0.5 border rounded text-xs focus:outline-none"
                            style={{ backgroundColor: theme.ui.inputBg, borderColor: theme.ui.border, color: theme.ui.textMain }}
                            placeholder="8888"
                        />
                    </div>
                    <button
                        onClick={handleConnect}
                        disabled={isConnecting}
                        className={`flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-colors ${isConnecting ? 'cursor-wait' : ''}`}
                        style={{
                            backgroundColor: isConnecting ? theme.ui.buttonBg : theme.ui.success,
                            color: isConnecting ? theme.ui.textDim : theme.ui.tabActiveText
                        }}
                    >
                        <ConnectIcon />
                        <span>{isConnecting ? 'Connecting...' : 'Connect'}</span>
                    </button>
                </>
            ) : (
                // Connected controls
                <>
                    <div className="flex items-center gap-1" style={{ color: theme.ui.success }}>
                        <span className="w-2 h-2 rounded-full animate-pulse" style={{ backgroundColor: theme.ui.success }}></span>
                        <span>Connected</span>
                    </div>

                    <div className="w-px h-4 mx-1" style={{ backgroundColor: theme.ui.border }}></div>

                    {/* Agent UID input */}
                    <div className="flex items-center gap-1">
                        <span style={{ color: theme.ui.textDim }}>Agent:</span>
                        <input
                            type="text"
                            value={agentUID}
                            onChange={(e) => setAgentUID(e.target.value)}
                            className="w-20 px-1.5 py-0.5 border rounded text-xs focus:outline-none"
                            style={{ backgroundColor: theme.ui.inputBg, borderColor: theme.ui.border, color: theme.ui.textMain }}
                            placeholder="UID"
                        />
                    </div>

                    {/* Wait for begin checkbox */}
                    <label className="flex items-center gap-1 cursor-pointer" style={{ color: theme.ui.textDim }}>
                        <input
                            type="checkbox"
                            checked={waitForBegin}
                            onChange={(e) => setWaitForBegin(e.target.checked)}
                            className="w-3 h-3"
                            style={{ accentColor: theme.ui.accent }}
                        />
                        <span>Wait Init</span>
                    </label>

                    {/* Start debug button */}
                    <button
                        onClick={handleStartDebug}
                        className="flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-colors"
                        style={{ backgroundColor: isDebugging ? theme.ui.warning : theme.ui.buttonBg, color: theme.ui.tabActiveText }}
                    >
                        <PlayIcon />
                        <span>Debug</span>
                    </button>

                    <div className="w-px h-4 mx-1" style={{ backgroundColor: theme.ui.border }}></div>

                    {/* Debug control buttons - only enabled when paused */}
                    <button
                        onClick={continueDebug}
                        disabled={!isPaused}
                        className={`flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-colors ${!isPaused ? 'cursor-not-allowed' : ''}`}
                        style={{ backgroundColor: isPaused ? theme.ui.success : theme.ui.inputBg, color: isPaused ? theme.ui.tabActiveText : theme.ui.textDim }}
                    >
                        <ContinueIcon />
                        <span>Continue</span>
                    </button>

                    <button
                        onClick={stepOver}
                        disabled={!isPaused}
                        className={`flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-colors ${!isPaused ? 'cursor-not-allowed' : ''}`}
                        style={{ backgroundColor: isPaused ? theme.ui.warning : theme.ui.inputBg, color: isPaused ? theme.ui.tabActiveText : theme.ui.textDim }}
                    >
                        <StepOverIcon />
                        <span>Step Over</span>
                    </button>

                    <button
                        onClick={stepInto}
                        disabled={!isPaused}
                        className={`flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-colors ${!isPaused ? 'cursor-not-allowed' : ''}`}
                        style={{ backgroundColor: isPaused ? theme.ui.warning : theme.ui.inputBg, color: isPaused ? theme.ui.tabActiveText : theme.ui.textDim }}
                    >
                        <StepIntoIcon />
                        <span>Step Into</span>
                    </button>

                    {/* Paused indicator */}
                    {isPaused && (
                        <span className="px-2 py-0.5 text-xs rounded animate-pulse" style={{ backgroundColor: theme.ui.danger, color: theme.ui.tabActiveText }}>
                            Paused
                        </span>
                    )}

                    <div className="flex-1"></div>

                    {/* Disconnect button */}
                    <button
                        onClick={handleDisconnect}
                        className="flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-colors"
                        style={{ backgroundColor: theme.ui.danger, color: theme.ui.tabActiveText }}
                    >
                        <DisconnectIcon />
                        <span>Disconnect</span>
                    </button>
                </>
            )}
        </div>
    );
}
