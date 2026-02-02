import { useEffect, useState } from "react";
// adjust imports since we moved into windows/ folder
import { Sidebar } from "../components/Sidebar";
import { NodeEditor } from "../components/NodeEditor";
import { PropertiesPanel } from "../components/PropertiesPanel";
import { FileTreePopup } from "../components/FileTreePopup";
import { Terminal } from "../components/Terminal";
import { useEditorStore } from "../stores/editorStore";
import { useNodeDefinitionStore } from "../stores/nodeDefinitionStore";
import { getAllWindows } from "@tauri-apps/api/window";

export function MainWindow() {
    const { initSettings, settings, openedFiles, activeFilePath, saveCurrentFile, createNewTree } = useEditorStore();
    const { loadDefinitions, isLoaded } = useNodeDefinitionStore();

    const [isFileTreeOpen, setIsFileTreeOpen] = useState(false);

    // Terminal state: true = docked at bottom, false = separate window
    const [isTerminalDocked, setIsTerminalDocked] = useState(true);
    // Terminal visibility (height when docked)
    const [terminalHeight, setTerminalHeight] = useState(200);
    const [isResizing, setIsResizing] = useState(false);

    const activeFile = openedFiles.find(f => f.path === activeFilePath);

    useEffect(() => {
        // 加载设置和节点定义
        if (!settings) {
            initSettings();
        }
        if (!isLoaded) {
            loadDefinitions();
        }
    }, [settings, initSettings, isLoaded, loadDefinitions]);

    // Ctrl+S 保存快捷键
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                e.preventDefault();
                saveCurrentFile();
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [saveCurrentFile]);

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

    // No extra close logic needed here, Rust handles it now.

    // Handle terminal resizing
    useEffect(() => {
        const handleMouseMove = (e: MouseEvent) => {
            if (!isResizing) return;
            const newHeight = window.innerHeight - e.clientY;
            // Limit height between 100 and 600
            setTerminalHeight(Math.max(100, Math.min(newHeight, 600)));
        };

        const handleMouseUp = () => {
            setIsResizing(false);
        };

        if (isResizing) {
            window.addEventListener('mousemove', handleMouseMove);
            window.addEventListener('mouseup', handleMouseUp);
        }

        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
    }, [isResizing]);

    const handlePopOut = async () => {
        setIsTerminalDocked(false);

        // Open terminal window
        // We iterate to find 'terminal' label window
        try {
            const windows = await getAllWindows();
            console.log("All windows:", windows.map(w => w.label));
            const termWin = windows.find(w => w.label === 'terminal');
            if (termWin) {
                console.log("Found terminal window, attempting to show...");
                await termWin.show();
                await termWin.setFocus();
            } else {
                console.error("Terminal window not found by label 'terminal'");
            }
        } catch (err) {
            console.error("Failed to pop out terminal:", err);
        }
    };

    return (
        <div className="flex h-screen w-screen overflow-hidden bg-gray-900 text-white">
            {/* 左侧边栏 - 已打开文件列表 */}
            <Sidebar />

            {/* 中间布局：垂直排列 (编辑器 + 终端) */}
            <div className="flex-1 flex flex-col min-w-0">

                {/* 上部：编辑器区域 */}
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
                        <button
                            onClick={() => createNewTree('NewTree')}
                            className="px-2 py-1 text-sm text-gray-300 hover:bg-gray-700 rounded"
                            title="New Tree"
                        >
                            +
                        </button>
                        <FileTreePopup
                            isOpen={isFileTreeOpen}
                            onClose={() => setIsFileTreeOpen(false)}
                        />
                        <div className="w-px h-5 bg-gray-700 mx-1" />
                        <button
                            onClick={saveCurrentFile}
                            disabled={!activeFile}
                            className="px-3 py-1 text-sm bg-gray-700 rounded hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                            title="Ctrl+S"
                        >
                            Save
                        </button>
                        <div className="flex-1" />
                        <span className="text-sm text-gray-400 truncate">
                            {activeFile ? (
                                <>
                                    {activeFile.name.endsWith('.tree') ? '\u{1F334} ' : activeFile.name.endsWith('.fsm') ? '\u{1F504} ' : ''}
                                    {activeFile.isDirty ? '* ' : ''}
                                    {activeFile.name.replace(/\.(tree|fsm)$/, '')}
                                </>
                            ) : "No file open"}
                        </span>
                    </div>

                    {/* 节点编辑区域 */}
                    <div className="flex-1 relative">
                        <NodeEditor onPaneClick={() => setIsFileTreeOpen(false)} />
                    </div>
                </div>

                {/* 底部：Docked Terminal */}
                {isTerminalDocked && (
                    <div
                        className="flex-shrink-0 bg-black flex flex-col"
                        style={{ height: terminalHeight }}
                    >
                        {/* 调整高度的手柄 */}
                        <div
                            className="h-1 w-full bg-gray-700 hover:bg-blue-500 cursor-ns-resize transition-colors flex-shrink-0"
                            onMouseDown={() => setIsResizing(true)}
                        />
                        <div className="flex-1 min-h-0">
                            <Terminal
                                isDocked={true}
                                onToggleMode={handlePopOut}
                            />
                        </div>
                    </div>
                )}
            </div>

            {/* 右侧边栏 - 变量和属性面板 */}
            <PropertiesPanel />
        </div>
    );
}
