import { useEditorStore } from '../stores/editorStore';
import type { Variable, ValueType } from '../types';

const TYPE_COLORS: Record<ValueType, string> = {
  int: 'text-blue-400',
  float: 'text-green-400',
  bool: 'text-yellow-400',
  string: 'text-pink-400',
  vector3: 'text-cyan-400',
  entity: 'text-orange-400',
  ulong: 'text-purple-400',
  enum: 'text-red-400',
};

function VariableItem({ variable }: { variable: Variable }) {
  const colorClass = TYPE_COLORS[variable.valueType] || 'text-gray-400';
  const typeLabel = variable.valueType.charAt(0).toUpperCase() + variable.valueType.slice(1);
  const isArray = variable.countType === 'list';
  
  return (
    <div className="px-2 py-1 text-sm bg-gray-800 rounded flex items-center gap-2">
      <span className={colorClass}>
        {typeLabel}{isArray ? '[]' : ''}
      </span>
      <span className="text-gray-300">{variable.name}</span>
      <span className="text-gray-500">=</span>
      <span className="text-gray-400 truncate">{variable.defaultValue || '(empty)'}</span>
    </div>
  );
}

export function PropertiesPanel() {
  const getCurrentTree = useEditorStore((state) => state.getCurrentTree);
  const selectedNodeIds = useEditorStore((state) => state.selectedNodeIds);
  const currentTree = getCurrentTree();
  
  const sharedVariables = currentTree?.sharedVariables || [];
  const localVariables = currentTree?.localVariables || [];
  
  return (
    <div className="w-64 h-full bg-gray-900 border-l border-gray-700 flex flex-col">
      {/* Variables 面板 */}
      <div className="flex-1 overflow-auto">
        <div className="p-3">
          <h2 className="text-xs text-gray-500 uppercase tracking-wider mb-2">
            Variables
          </h2>
          
          {/* Shared Variables */}
          <div className="mb-4">
            <div className="text-xs text-gray-500 mb-1">Shared</div>
            {sharedVariables.length === 0 ? (
              <div className="text-xs text-gray-600">No shared variables</div>
            ) : (
              <div className="space-y-1">
                {sharedVariables.map((v) => (
                  <VariableItem key={v.name} variable={v} />
                ))}
              </div>
            )}
          </div>
          
          {/* Local Variables */}
          <div>
            <div className="text-xs text-gray-500 mb-1">Local</div>
            {localVariables.length === 0 ? (
              <div className="text-xs text-gray-600">No local variables</div>
            ) : (
              <div className="space-y-1">
                {localVariables.map((v) => (
                  <VariableItem key={v.name} variable={v} />
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Properties 面板 */}
      <div className="border-t border-gray-700">
        <div className="p-3">
          <h2 className="text-xs text-gray-500 uppercase tracking-wider mb-2">
            Properties
          </h2>
          {selectedNodeIds.length === 0 ? (
            <div className="text-xs text-gray-600">Select a node</div>
          ) : (
            <div className="text-xs text-gray-400">
              {selectedNodeIds.length} node(s) selected
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
