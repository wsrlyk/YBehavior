import { useEffect, useState } from "react";
// adjust imports since we moved into windows/ folder
import { Sidebar } from "../components/Sidebar";
import { EditorPane } from "../components/EditorPane";
import { PropertiesPanel } from "../components/PropertiesPanel";
import { FSMPropertiesPanel } from "../components/FSMPropertiesPanel";
import { FileTreePopup } from "../components/FileTreePopup";
import { Terminal } from "../components/Terminal";
import { useEditorStore } from "../stores/editorStore";
import { useNodeDefinitionStore } from "../stores/nodeDefinitionStore";
import { useEditorMetaStore } from "../stores/editorMetaStore";
import { useFSMStore } from "../stores/fsmStore";
import { getAllWindows } from "@tauri-apps/api/window";
import Tooltip from "../components/Tooltip";

export function MainWindow() {
    const { initSettings, settings, openedFiles, activeFilePath, saveCurrentFile, saveFileAs, undo, redo, createNewTree } = useEditorStore();
    const { openedFSMFiles, activeFSMPath, saveFSM, saveFSMAs, undo: undoFSM, redo: redoFSM, createNewFSM } = useFSMStore();

    const { loadDefinitions, isLoaded } = useNodeDefinitionStore();
    const loadAllMeta = useEditorMetaStore(state => state.loadAllMeta);

    const setSidebarWidth = useEditorMetaStore(state => state.setSidebarWidth);

    const [isFileTreeOpen, setIsFileTreeOpen] = useState(false);
    const [isNewMenuOpen, setIsNewMenuOpen] = useState(false);

    // Terminal state: true = docked at bottom, false = separate window
    const [isTerminalDocked, setIsTerminalDocked] = useState(true);
    // Terminal visibility (height when docked)
    const [terminalHeight, setTerminalHeight] = useState(200);
    const [isResizingTerminal, setIsResizingTerminal] = useState(false);
    const [isResizingSidebar, setIsResizingSidebar] = useState(false);

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
        loadAllMeta();
    }, [settings, initSettings, isLoaded, loadDefinitions, loadAllMeta]);

    // Ctrl+S / Ctrl+Z / Ctrl+Y 快捷键
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
                }
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [saveCurrentFile, saveFSM, undo, undoFSM, redo, redoFSM, activeFSMPath]);

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
        };

        const handleMouseUp = () => {
            setIsResizingTerminal(false);
            setIsResizingSidebar(false);
        };

        if (isResizingTerminal || isResizingSidebar) {
            window.addEventListener('mousemove', handleMouseMove);
            window.addEventListener('mouseup', handleMouseUp);
        }
        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
    }, [isResizingTerminal, isResizingSidebar, setSidebarWidth]);

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
        <div className="flex flex-col h-screen bg-gray-900 text-white overflow-hidden select-none font-sans">
            <Tooltip />
            {/* 顶部区域：侧边栏 + 主编辑区 + 属性面板 */}
            <div className="flex-1 flex overflow-hidden min-h-0">
                {/* 侧边栏 */}
                <Sidebar />

                {/* Sidebar Resize Handle */}
                {(openedFiles.length > 0 || openedFSMFiles.length > 0) && (
                    <div
                        className="w-1 bg-gray-800 hover:bg-blue-500 cursor-col-resize transition-colors flex-shrink-0"
                        onMouseDown={() => setIsResizingSidebar(true)}
                    />
                )}

                {/* 主编辑区 */}
                <div className="flex-1 flex flex-col min-h-0">
                    {/* 工具栏 */}
                    <div className="h-10 bg-gray-800 border-b border-gray-700 flex items-center px-4 gap-2 flex-shrink-0">
                        <button
                            onClick={() => setIsFileTreeOpen(!isFileTreeOpen)}
                            className="px-2 py-1 text-sm text-gray-300 hover:bg-gray-700 rounded"
                            title="Open File"
                        >
                            ☰
                        </button>
                        <div className="relative">
                            <button
                                onClick={() => setIsNewMenuOpen(!isNewMenuOpen)}
                                className="px-2 py-1 text-sm text-gray-300 hover:bg-gray-700 rounded flex items-center gap-1"
                                title="New"
                            >
                                + <span className="text-xs">▼</span>
                            </button>
                            {isNewMenuOpen && (
                                <div className="absolute top-full left-0 mt-1 bg-gray-800 border border-gray-600 rounded shadow-lg z-50 min-w-[120px]">
                                    <button
                                        onClick={() => { createNewTree('NewTree'); setIsNewMenuOpen(false); }}
                                        className="w-full px-3 py-2 text-sm text-left text-gray-300 hover:bg-gray-700 flex items-center gap-2"
                                    >
                                        🌴 New Tree
                                    </button>
                                    <button
                                        onClick={() => { createNewFSM('NewFSM'); setIsNewMenuOpen(false); }}
                                        className="w-full px-3 py-2 text-sm text-left text-gray-300 hover:bg-gray-700 flex items-center gap-2"
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
                        <div className="w-px h-5 bg-gray-700 mx-1" />
                        <button
                            onClick={activeFSMPath ? saveFSM : saveCurrentFile}
                            disabled={!currentFile}
                            className="px-3 py-1 text-sm bg-gray-700 rounded hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                            title="Ctrl+S"
                        >
                            Save
                        </button>
                        <button
                            onClick={activeFSMPath ? saveFSMAs : saveFileAs}
                            disabled={!currentFile}
                            className="px-3 py-1 text-sm bg-gray-700 rounded hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                            title="Save As..."
                        >
                            Save As
                        </button>
                        <div className="w-px h-5 bg-gray-700 mx-1" />
                        <button
                            onClick={activeFSMPath ? undoFSM : undo}
                            disabled={!currentFile || !canUndo}
                            className="px-3 py-1 text-sm bg-gray-700 rounded hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                            title="Undo (Ctrl+Z)"
                        >
                            Undo
                        </button>
                        <button
                            onClick={activeFSMPath ? redoFSM : redo}
                            disabled={!currentFile || !canRedo}
                            className="px-3 py-1 text-sm bg-gray-700 rounded hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                            title="Redo (Ctrl+Y)"
                        >
                            Redo
                        </button>
                        <div className="flex-1" />
                        <span className="text-sm text-gray-400 truncate">
                            {currentFile ? (
                                <>
                                    {activeFSM ? '🔄 ' : activeFile ? '🌴 ' : ''}
                                    {currentFile.isDirty ? '* ' : ''}
                                    {currentFile.name.replace(/\.(tree|fsm)$/, '')}
                                </>
                            ) : "No file open"}
                        </span>
                    </div>

                    {/* 编辑器区域 */}
                    <div className="flex-1 relative">
                        <EditorPane onPaneClick={() => { setIsFileTreeOpen(false); setIsNewMenuOpen(false); }} />
                    </div>
                </div>

                {/* 属性面板区域 (FSM 面板现在常驻以防止布局跳动) */}
                {activeFile?.name.endsWith('.tree') ? (
                    <PropertiesPanel />
                ) : (activeFSMPath && activeFSMPath.endsWith('.fsm')) ? (
                    <FSMPropertiesPanel />
                ) : null}
            </div>

            {/* 底部：Docked Terminal */}
            {isTerminalDocked && (
                <div
                    className="flex-shrink-0 bg-black flex flex-col"
                    style={{ height: terminalHeight }}
                >
                    {/* 调整高度的手柄 */}
                    <div
                        className="h-1 bg-gray-800 hover:bg-blue-500 cursor-ns-resize transition-colors"
                        onMouseDown={() => setIsResizingTerminal(true)}
                    />
                    <div className="flex-1 relative overflow-hidden">
                        <Terminal isDocked={isTerminalDocked} onToggleMode={handlePopOut} />
                    </div>
                </div>
            )}
        </div>
    );
}
