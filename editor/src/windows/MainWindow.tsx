import { useEffect, useState } from "react";
import { listen } from '@tauri-apps/api/event';
// adjust imports since we moved into windows/ folder
import { Sidebar } from "../components/Sidebar";
import { EditorPane } from "../components/EditorPane";
import { PropertiesPanel } from "../components/PropertiesPanel";
import { FSMPropertiesPanel } from "../components/FSMPropertiesPanel";
import { FileTreePopup } from "../components/FileTreePopup";
import { Terminal } from "../components/Terminal";
import { DebugToolbar } from "../components/DebugToolbar";
import { useEditorStore } from "../stores/editorStore";
import { useNodeDefinitionStore } from "../stores/nodeDefinitionStore";
import { useEditorMetaStore } from "../stores/editorMetaStore";
import { useFSMStore } from "../stores/fsmStore";
import { useDebugStore } from "../stores/debugStore";
import { getAllWindows } from "@tauri-apps/api/window";
import Tooltip from "../components/Tooltip";
import { GlobalSearch } from "../components/GlobalSearch";
import { ReactFlowProvider } from '@xyflow/react';
import { getTheme } from '../theme/theme';

export function MainWindow() {
    const theme = getTheme();
    const { initSettings, settings, openedFiles, activeFilePath, saveCurrentFile, saveFileAs, undo, redo, createNewTree } = useEditorStore();
    const { openedFSMFiles, activeFSMPath, saveFSM, saveFSMAs, undo: undoFSM, redo: redoFSM, createNewFSM } = useFSMStore();

    const { loadDefinitions, isLoaded } = useNodeDefinitionStore();
    const loadAllMeta = useEditorMetaStore(state => state.loadAllMeta);
    const isSearchOpen = useEditorMetaStore(state => state.uiMeta.isSearchOpen);
    const setSearchOpen = useEditorMetaStore(state => state.setSearchOpen);

    const setSidebarWidth = useEditorMetaStore(state => state.setSidebarWidth);

    const [isFileTreeOpen, setIsFileTreeOpen] = useState(false);
    const [isNewMenuOpen, setIsNewMenuOpen] = useState(false);

    // Terminal state: true = docked at bottom, false = separate window
    const [isTerminalDocked, setIsTerminalDocked] = useState(true);
    // Terminal visibility (height when docked)
    const [terminalHeight, setTerminalHeight] = useState(200);
    const [isResizingTerminal, setIsResizingTerminal] = useState(false);
    const [isResizingSidebar, setIsResizingSidebar] = useState(false);
    const [isResizingProperties, setIsResizingProperties] = useState(false);
    const propertiesPanelWidth = useEditorMetaStore(state => state.uiMeta.propertiesPanelWidth || 300);
    const setPropertiesPanelWidth = useEditorMetaStore(state => state.setPropertiesPanelWidth);
    const [updateAvailable, setUpdateAvailable] = useState(false);

    // Listen for update-available event from Rust backend
    useEffect(() => {
        const unlisten = listen('update-available', () => {
            setUpdateAvailable(true);
        });
        return () => { unlisten.then(fn => fn()); };
    }, []);

    const activeFile = openedFiles.find(f => f.path === activeFilePath);
    const activeFSM = openedFSMFiles.find(f => f.path === activeFSMPath);

    const currentFile = activeFile || activeFSM;
    const canUndo = activeFile ? activeFile.history.past.length > 0 : activeFSM ? activeFSM.history.past.length > 0 : false;
    const canRedo = activeFile ? activeFile.history.future.length > 0 : activeFSM ? activeFSM.history.future.length > 0 : false;

    useEffect(() => {
        // 加载设置和节点定义
        if (!settings) {
            initSettings();
        }
        if (!isLoaded) {
            loadDefinitions();
        }
        // 加载编辑器元数据
        // 加载编辑器元数据
        loadAllMeta().then(() => {
            useDebugStore.getState().syncBreakpointsFromMeta();
        });
    }, [settings, initSettings, isLoaded, loadDefinitions, loadAllMeta]);

    // Ctrl+S / Ctrl+Z / Ctrl+Y / Ctrl+F 快捷键
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.ctrlKey || e.metaKey) {
                if (e.key === 's') {
                    e.preventDefault();
                    if (activeFSMPath) {
                        saveFSM();
                    } else {
                        saveCurrentFile();
                    }
                } else if (e.key === 'z') {
                    e.preventDefault();
                    if (activeFSMPath) {
                        undoFSM();
                    } else {
                        undo();
                    }
                } else if (e.key === 'y') {
                    e.preventDefault();
                    if (activeFSMPath) {
                        redoFSM();
                    } else {
                        redo();
                    }
                } else if (e.key === 'f') {
                    e.preventDefault();
                    setSearchOpen(!isSearchOpen);
                }
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [saveCurrentFile, saveFSM, undo, undoFSM, redo, redoFSM, activeFSMPath, isSearchOpen, setSearchOpen]);

    // Listen for 'terminal-control' messages (from Terminal Window)
    useEffect(() => {
        const channel = new BroadcastChannel('terminal-control');
        channel.onmessage = (event) => {
            if (event.data.type === 'dock') {
                setIsTerminalDocked(true);
            }
        };

        return () => {
            channel.close();
        };
    }, []);

    // Handle terminal and sidebar resizing
    useEffect(() => {
        const handleMouseMove = (e: MouseEvent) => {
            if (isResizingTerminal) {
                const newHeight = window.innerHeight - e.clientY;
                setTerminalHeight(Math.max(100, Math.min(newHeight, 600)));
            }
            if (isResizingSidebar) {
                const newWidth = e.clientX;
                setSidebarWidth(Math.max(100, Math.min(newWidth, 500)));
            }
            if (isResizingProperties) {
                const newWidth = window.innerWidth - e.clientX;
                setPropertiesPanelWidth(Math.max(200, Math.min(newWidth, 800)));
            }
        };

        const handleMouseUp = () => {
            setIsResizingTerminal(false);
            setIsResizingSidebar(false);
            setIsResizingProperties(false);
        };

        if (isResizingTerminal || isResizingSidebar || isResizingProperties) {
            window.addEventListener('mousemove', handleMouseMove);
            window.addEventListener('mouseup', handleMouseUp);
        }
        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
    }, [isResizingTerminal, isResizingSidebar, isResizingProperties, setSidebarWidth, setPropertiesPanelWidth]);

    const handlePopOut = async () => {
        const windows = await getAllWindows();
        const terminalWin = windows.find(w => w.label === 'terminal');
        if (terminalWin) {
            await terminalWin.show();
            await terminalWin.setFocus();
            setIsTerminalDocked(false);
        }
    };

    return (
        <ReactFlowProvider>
            <div
                className="flex flex-col h-screen overflow-hidden select-none"
                style={{
                    backgroundColor: theme.ui.background,
                    color: theme.ui.textMain,
                    fontFamily: 'Tahoma, Verdana, Segoe UI, sans-serif'
                }}
            >
                <Tooltip />
                <GlobalSearch />
                {/* 顶部区域：侧边栏 + 主编辑区 + 属性面板 */}
                <div className="flex-1 flex overflow-hidden min-h-0">
                    {/* 侧边栏 */}
                    <Sidebar />

                    {/* Sidebar Resize Handle */}
                    {(openedFiles.length > 0 || openedFSMFiles.length > 0) && (
                        <div
                            className="w-1 cursor-col-resize transition-colors flex-shrink-0"
                            style={{ backgroundColor: theme.ui.border }}
                            onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.splitterHover; }}
                            onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.border; }}
                            onMouseDown={() => setIsResizingSidebar(true)}
                        />
                    )}

                    {/* 主编辑区 */}
                    <div className="flex-1 flex flex-col min-h-0 min-w-0">
                        {/* 工具栏 */}
                        <div
                            className="h-10 border-b flex items-center px-4 gap-2 flex-shrink-0"
                            style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}
                        >
                            <button
                                onClick={() => setIsFileTreeOpen(!isFileTreeOpen)}
                                className="px-2 py-1 text-sm rounded border"
                                style={{ color: theme.ui.textMain, borderColor: theme.ui.border, backgroundColor: theme.ui.buttonBg }}
                                onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.buttonHoverBg; }}
                                onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.buttonBg; }}
                            >
                                ☰
                            </button>
                            <div className="relative">
                                <button
                                    onClick={() => setIsNewMenuOpen(!isNewMenuOpen)}
                                    className="px-2 py-1 text-sm rounded border flex items-center gap-1"
                                    style={{ color: theme.ui.textMain, borderColor: theme.ui.border, backgroundColor: theme.ui.buttonBg }}
                                    onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.buttonHoverBg; }}
                                    onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.buttonBg; }}
                                >
                                    + <span className="text-xs">▼</span>
                                </button>
                                {isNewMenuOpen && (
                                    <div
                                        className="absolute top-full left-0 mt-1 border rounded shadow-lg z-50 min-w-[120px]"
                                        style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}
                                    >
                                        <button
                                            onClick={() => { createNewTree('NewTree'); setIsNewMenuOpen(false); }}
                                            className="w-full px-3 py-2 text-sm text-left flex items-center gap-2"
                                            style={{ color: theme.ui.textMain }}
                                            onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
                                            onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
                                        >
                                            🌴 New Tree
                                        </button>
                                        <button
                                            onClick={() => { createNewFSM('NewFSM'); setIsNewMenuOpen(false); }}
                                            className="w-full px-3 py-2 text-sm text-left flex items-center gap-2"
                                            style={{ color: theme.ui.textMain }}
                                            onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
                                            onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
                                        >
                                            🔄 New FSM
                                        </button>
                                    </div>
                                )}
                            </div>

                            <FileTreePopup
                                isOpen={isFileTreeOpen}
                                onClose={() => setIsFileTreeOpen(false)}
                            />
                            <div className="w-px h-5 mx-1" style={{ backgroundColor: theme.ui.border }} />
                            <button
                                onClick={activeFSMPath ? saveFSM : saveCurrentFile}
                                disabled={!currentFile}
                                className="px-3 py-1 text-sm rounded border disabled:opacity-50 disabled:cursor-not-allowed"
                                style={{ color: theme.ui.textMain, borderColor: theme.ui.border, backgroundColor: theme.ui.buttonBg }}
                                onMouseEnter={(e) => { if (!currentFile) return; e.currentTarget.style.backgroundColor = theme.ui.buttonHoverBg; }}
                                onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.buttonBg; }}
                            >
                                Save
                            </button>
                            <button
                                onClick={activeFSMPath ? saveFSMAs : saveFileAs}
                                disabled={!currentFile}
                                className="px-3 py-1 text-sm rounded border disabled:opacity-50 disabled:cursor-not-allowed"
                                style={{ color: theme.ui.textMain, borderColor: theme.ui.border, backgroundColor: theme.ui.buttonBg }}
                                onMouseEnter={(e) => { if (!currentFile) return; e.currentTarget.style.backgroundColor = theme.ui.buttonHoverBg; }}
                                onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.buttonBg; }}
                            >
                                Save As
                            </button>
                            <div className="w-px h-5 mx-1" style={{ backgroundColor: theme.ui.border }} />
                            <button
                                onClick={activeFSMPath ? undoFSM : undo}
                                disabled={!currentFile || !canUndo}
                                className="px-3 py-1 text-sm rounded border disabled:opacity-50 disabled:cursor-not-allowed"
                                style={{ color: theme.ui.textMain, borderColor: theme.ui.border, backgroundColor: theme.ui.buttonBg }}
                                onMouseEnter={(e) => { if (!currentFile || !canUndo) return; e.currentTarget.style.backgroundColor = theme.ui.buttonHoverBg; }}
                                onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.buttonBg; }}
                            >
                                Undo
                            </button>
                            <button
                                onClick={activeFSMPath ? redoFSM : redo}
                                disabled={!currentFile || !canRedo}
                                className="px-3 py-1 text-sm rounded border disabled:opacity-50 disabled:cursor-not-allowed"
                                style={{ color: theme.ui.textMain, borderColor: theme.ui.border, backgroundColor: theme.ui.buttonBg }}
                                onMouseEnter={(e) => { if (!currentFile || !canRedo) return; e.currentTarget.style.backgroundColor = theme.ui.buttonHoverBg; }}
                                onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.buttonBg; }}
                            >
                                Redo
                            </button>
                            <div className="flex-1" />
                            <DebugToolbar />
                        </div>

                        {/* Debug Toolbar moved to header */}

                        {/* 编辑器区域 */}
                        <div className="flex-1 relative">
                            <EditorPane onPaneClick={() => { setIsFileTreeOpen(false); setIsNewMenuOpen(false); }} />
                            {/* Update watermark */}
                            {updateAvailable && (
                                <div className="absolute inset-0 flex items-center justify-center pointer-events-none z-[50]">
                                    <div className="w-full text-center text-3xl font-bold select-none tracking-wide drop-shadow-lg" style={{ color: `${theme.ui.textMain}99` }}>
                                        🔄 新版本已就绪，请关闭本程序后重新启动
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>

                    {/* Resize Handle for Properties Panel */}
                    {(activeFile?.name.endsWith('.tree') || (activeFSMPath && activeFSMPath.endsWith('.fsm'))) && (
                        <div
                            className="w-1 cursor-col-resize transition-colors flex-shrink-0"
                            style={{ backgroundColor: theme.ui.border }}
                            onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.splitterHover; }}
                            onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.border; }}
                            onMouseDown={() => setIsResizingProperties(true)}
                        />
                    )}

                    {/* 属性面板区域 (FSM 面板现在常驻以防止布局跳动) */}
                    <div style={{ width: propertiesPanelWidth }} className="flex-shrink-0 flex flex-col overflow-hidden">
                        {activeFile?.name.endsWith('.tree') ? (
                            <PropertiesPanel />
                        ) : (activeFSMPath && activeFSMPath.endsWith('.fsm')) ? (
                            <FSMPropertiesPanel />
                        ) : null}
                    </div>
                </div>

                {/* 底部：Docked Terminal */}
                {isTerminalDocked && (
                    <div
                        className="flex-shrink-0 flex flex-col border-t"
                        style={{ backgroundColor: theme.ui.terminalBg, borderColor: theme.ui.border, height: terminalHeight }}
                    >
                        {/* 调整高度的手柄 */}
                        <div
                            className="h-1 cursor-ns-resize transition-colors"
                            style={{ backgroundColor: theme.ui.border }}
                            onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.splitterHover; }}
                            onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = theme.ui.border; }}
                            onMouseDown={() => setIsResizingTerminal(true)}
                        />
                        <div className="flex-1 relative overflow-hidden">
                            <Terminal isDocked={isTerminalDocked} onToggleMode={handlePopOut} />
                        </div>
                    </div>
                )}
            </div>
        </ReactFlowProvider>
    );
}
