import "./App.css";
import { Sidebar } from "./components/Sidebar";
import { NodeEditor } from "./components/NodeEditor";
import { PropertiesPanel } from "./components/PropertiesPanel";

function App() {
  return (
    <div className="flex h-screen w-screen overflow-hidden">
      {/* 左侧边栏 - 文件和节点列表 */}
      <Sidebar />
      
      {/* 中间 - 节点编辑器 */}
      <div className="flex-1 flex flex-col">
        {/* 工具栏 */}
        <div className="h-10 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 flex items-center px-4 gap-2">
          <button className="px-3 py-1 text-sm bg-gray-100 dark:bg-gray-700 rounded hover:bg-gray-200 dark:hover:bg-gray-600">
            Save
          </button>
          <button className="px-3 py-1 text-sm bg-gray-100 dark:bg-gray-700 rounded hover:bg-gray-200 dark:hover:bg-gray-600">
            Export
          </button>
          <div className="flex-1" />
          <span className="text-sm text-gray-500 dark:text-gray-400">
            Test.tree
          </span>
        </div>
        
        {/* 节点编辑区域 */}
        <div className="flex-1">
          <NodeEditor />
        </div>
      </div>
      
      {/* 右侧边栏 - 属性面板 */}
      <PropertiesPanel />
    </div>
  );
}

export default App;
