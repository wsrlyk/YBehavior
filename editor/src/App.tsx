import { useEffect, useState } from "react";
import "./App.css";
import { Sidebar } from "./components/Sidebar";
import { NodeEditor } from "./components/NodeEditor";
import { PropertiesPanel } from "./components/PropertiesPanel";
import { FileTreePopup } from "./components/FileTreePopup";
import { useEditorStore } from "./stores/editorStore";
import { useNodeDefinitionStore } from "./stores/nodeDefinitionStore";

function App() {
  const { initSettings, settings, openedFiles, activeFilePath, saveCurrentFile } = useEditorStore();
  const { loadDefinitions, isLoaded } = useNodeDefinitionStore();
  
  const [isFileTreeOpen, setIsFileTreeOpen] = useState(false);
  
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
  
  return (
    <div className="flex h-screen w-screen overflow-hidden bg-gray-900 text-white">
      {/* 左侧边栏 - 已打开文件列表 */}
      <Sidebar />
      
      {/* 中间 - 节点编辑器 */}
      <div className="flex-1 flex flex-col">
        {/* 工具栏 */}
        <div className="h-10 bg-gray-800 border-b border-gray-700 flex items-center px-4 gap-2">
          <button 
            onClick={() => setIsFileTreeOpen(!isFileTreeOpen)}
            className="px-2 py-1 text-sm text-gray-300 hover:bg-gray-700 rounded"
            title="Open File"
          >
            ☰
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
          >
            Save
          </button>
          <div className="flex-1" />
          <span className="text-sm text-gray-400">
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
        <div className="flex-1">
          <NodeEditor onPaneClick={() => setIsFileTreeOpen(false)} />
        </div>
      </div>
      
      {/* 右侧边栏 - 变量和属性面板 */}
      <PropertiesPanel />
    </div>
  );
}

export default App;
