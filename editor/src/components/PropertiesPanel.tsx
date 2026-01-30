export function PropertiesPanel() {
  return (
    <div className="w-72 h-full bg-white dark:bg-gray-800 border-l border-gray-200 dark:border-gray-700 flex flex-col">
      <div className="p-3 border-b border-gray-200 dark:border-gray-700">
        <h2 className="text-sm font-semibold text-gray-600 dark:text-gray-300">
          Properties
        </h2>
      </div>
      
      <div className="p-3 flex-1 overflow-auto">
        <div className="text-sm text-gray-500 dark:text-gray-400">
          Select a node to view properties
        </div>
      </div>

      {/* Variables 面板 */}
      <div className="border-t border-gray-200 dark:border-gray-700">
        <div className="p-3">
          <h2 className="text-sm font-semibold text-gray-600 dark:text-gray-300 mb-2">
            Variables
          </h2>
          <div className="space-y-2">
            <div className="text-xs text-gray-500 dark:text-gray-400">Shared</div>
            <div className="px-2 py-1 text-sm bg-gray-50 dark:bg-gray-700 rounded">
              <span className="text-blue-600 dark:text-blue-400">Int</span> counter = 0
            </div>
            <div className="px-2 py-1 text-sm bg-gray-50 dark:bg-gray-700 rounded">
              <span className="text-green-600 dark:text-green-400">Bool</span> isActive = true
            </div>
            
            <div className="text-xs text-gray-500 dark:text-gray-400 mt-2">Local</div>
            <div className="px-2 py-1 text-sm bg-gray-50 dark:bg-gray-700 rounded">
              <span className="text-purple-600 dark:text-purple-400">Float</span> temp = 0.0
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
