import { useEffect, useState } from "react";
import "./App.css";
import { Sidebar } from "./components/Sidebar";
import { NodeEditor } from "./components/NodeEditor";
import { PropertiesPanel } from "./components/PropertiesPanel";
import { FileTreePopup } from "./components/FileTreePopup";
import { useEditorStore } from "./stores/editorStore";
import { useNodeDefinitionStore } from "./stores/nodeDefinitionStore";

function App() {
  const { setWorkingDir, workingDir, openedFiles, activeFilePath } = useEditorStore();
  const { loadDefinitions, isLoaded } = useNodeDefinitionStore();
  
  const [isFileTreeOpen, setIsFileTreeOpen] = useState(false);
  
  const activeFile = openedFiles.find(f => f.path === activeFilePath);
  
  useEffect(() => {
    // 默认加载 YBehavior/bin 目录
    if (!workingDir) {
      setWorkingDir("E:/Develop/YBehavior/bin");
    }
  }, [workingDir, setWorkingDir]);
  
  useEffect(() => {
    // 加载节点定义
    if (!isLoaded) {
      loadDefinitions(
        "E:/Develop/YBehavior/editor/config/builtin.xml",
        "E:/Develop/YBehavior/projects/YBehaviorEditor/bin/actions.xml"
      );
    }
  }, [isLoaded, loadDefinitions]);
  
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
          <button className="px-3 py-1 text-sm bg-gray-700 rounded hover:bg-gray-600">
            Save
          </button>
          <button className="px-3 py-1 text-sm bg-gray-700 rounded hover:bg-gray-600">
            Export
          </button>
          <div className="flex-1" />
          <span className="text-sm text-gray-400">
            {activeFile ? (activeFile.isDirty ? '* ' : '') + activeFile.name : "No file open"}
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
