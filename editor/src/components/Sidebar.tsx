export function Sidebar() {
  return (
    <div className="w-64 h-full bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col">
      {/* 文件列表 */}
      <div className="p-3 border-b border-gray-200 dark:border-gray-700">
        <h2 className="text-sm font-semibold text-gray-600 dark:text-gray-300 mb-2">
          Files
        </h2>
        <div className="space-y-1">
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            📁 Trees
          </div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer ml-4">
            📄 Test.tree
          </div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer ml-4">
            📄 Large.tree
          </div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            📁 FSM
          </div>
        </div>
      </div>

      {/* 节点列表 */}
      <div className="p-3 flex-1 overflow-auto">
        <h2 className="text-sm font-semibold text-gray-600 dark:text-gray-300 mb-2">
          Nodes
        </h2>
        <div className="space-y-1">
          <div className="text-xs text-gray-500 dark:text-gray-400 mt-2 mb-1">Composite</div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            ➜➜➜ Sequence
          </div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            ？？？ Selector
          </div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            ↙↓↘ SwitchCase
          </div>
          
          <div className="text-xs text-gray-500 dark:text-gray-400 mt-2 mb-1">Decorator</div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            ↺ Loop
          </div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            ↺ ForEach
          </div>
          
          <div className="text-xs text-gray-500 dark:text-gray-400 mt-2 mb-1">Action</div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            +-×÷ Calculator
          </div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            y≫x SetData
          </div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            🌲 SubTree
          </div>
          
          <div className="text-xs text-gray-500 dark:text-gray-400 mt-2 mb-1">Condition</div>
          <div className="px-2 py-1 text-sm rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">
            x？y Comparer
          </div>
        </div>
      </div>
    </div>
  );
}
