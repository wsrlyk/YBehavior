import { useState, useEffect } from 'react';
import { useEditorStore } from '../stores/editorStore';
import type { Variable, ValueType, CountType, Pin, PinBinding } from '../types';

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

const VALUE_TYPES: ValueType[] = ['int', 'float', 'bool', 'string', 'vector3', 'entity', 'ulong'];

interface VariableItemProps {
  variable: Variable;
  onUpdate: (name: string, updates: Partial<Variable>) => void;
  onDelete: (name: string) => void;
}

function VariableItem({ variable, onUpdate, onDelete }: VariableItemProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState(variable.defaultValue);
  const colorClass = TYPE_COLORS[variable.valueType] || 'text-gray-400';
  const isArray = variable.countType === 'list';
  
  const handleValueSubmit = () => {
    onUpdate(variable.name, { defaultValue: editValue });
    setIsEditing(false);
  };
  
  // 获取类型的默认值
  const getDefaultValue = (type: ValueType, isList: boolean): string => {
    if (isList) return '';
    switch (type) {
      case 'int': return '0';
      case 'float': return '0.0';
      case 'bool': return 'F';
      case 'string': return '';
      case 'vector3': return '0,0,0';
      case 'entity': return '';
      case 'ulong': return '0';
      default: return '';
    }
  };
  
  const handleTypeChange = (newType: ValueType) => {
    onUpdate(variable.name, { valueType: newType, defaultValue: getDefaultValue(newType, isArray) });
  };
  
  const handleCountTypeToggle = () => {
    const newCountType: CountType = isArray ? 'scalar' : 'list';
    const newIsArray = newCountType === 'list';
    onUpdate(variable.name, { countType: newCountType, defaultValue: getDefaultValue(variable.valueType, newIsArray) });
  };
  
  // 循环切换类型
  const cycleType = () => {
    const currentIndex = VALUE_TYPES.indexOf(variable.valueType);
    const nextIndex = (currentIndex + 1) % VALUE_TYPES.length;
    handleTypeChange(VALUE_TYPES[nextIndex]);
  };
  
  return (
    <div className="px-2 py-1 text-sm bg-gray-800 rounded group">
      <div className="flex items-center gap-1">
        {/* 类型选择 - 点击循环切换 */}
        <span
          className={`${colorClass} text-xs cursor-pointer hover:underline`}
          onClick={cycleType}
          title="Click to change type"
        >
          {variable.valueType.charAt(0).toUpperCase() + variable.valueType.slice(1)}
        </span>
        
        {/* 数组/标量切换 */}
        <button
          className={`text-xs ${colorClass} hover:opacity-80 min-w-3`}
          onClick={handleCountTypeToggle}
          title={isArray ? 'Switch to scalar' : 'Switch to array'}
        >
          {isArray ? '[]' : '·'}
        </button>
        
        {/* 变量名 */}
        <span className="text-gray-300 flex-1 truncate">{variable.name}</span>
        
        {/* 删除按钮 */}
        <button
          className="text-gray-600 hover:text-red-400 opacity-0 group-hover:opacity-100 text-xs"
          onClick={() => onDelete(variable.name)}
          title="Delete variable"
        >
          ✕
        </button>
      </div>
      
      {/* 值编辑 */}
      <div className="flex items-center gap-1 mt-1">
        <span className="text-gray-500 text-xs">=</span>
        {isEditing ? (
          <input
            className="flex-1 bg-gray-700 text-gray-300 text-xs px-1 rounded outline-none"
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            onBlur={handleValueSubmit}
            onKeyDown={(e) => e.key === 'Enter' && handleValueSubmit()}
            autoFocus
          />
        ) : (
          <span
            className="flex-1 text-gray-400 text-xs truncate cursor-pointer hover:text-gray-300"
            onClick={() => { setEditValue(variable.defaultValue); setIsEditing(true); }}
          >
            {variable.defaultValue || '(empty)'}
          </span>
        )}
      </div>
    </div>
  );
}

function AddVariableButton({ isLocal, onAdd }: { isLocal: boolean; onAdd: (v: Variable) => void }) {
  const [isAdding, setIsAdding] = useState(false);
  const [name, setName] = useState('');
  
  const handleAdd = () => {
    if (name.trim()) {
      onAdd({
        name: name.trim(),
        valueType: 'int',
        countType: 'scalar',
        isLocal,
        defaultValue: '0',
      });
      setName('');
      setIsAdding(false);
    }
  };
  
  if (isAdding) {
    return (
      <div className="flex items-center gap-1 mt-1">
        <input
          className="flex-1 bg-gray-700 text-gray-300 text-xs px-1 rounded outline-none"
          placeholder="Variable name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
          autoFocus
        />
        <button className="text-green-400 text-xs" onClick={handleAdd}>✓</button>
        <button className="text-gray-500 text-xs" onClick={() => setIsAdding(false)}>✕</button>
      </div>
    );
  }
  
  return (
    <button
      className="text-xs text-gray-500 hover:text-gray-300 mt-1"
      onClick={() => setIsAdding(true)}
    >
      + Add
    </button>
  );
}

export function PropertiesPanel() {
  const getCurrentTree = useEditorStore((state) => state.getCurrentTree);
  const selectedNodeIds = useEditorStore((state) => state.selectedNodeIds);
  const updateVariable = useEditorStore((state) => state.updateVariable);
  const removeVariable = useEditorStore((state) => state.removeVariable);
  const addVariable = useEditorStore((state) => state.addVariable);
  const currentTree = getCurrentTree();
  
  const sharedVariables = currentTree?.sharedVariables || [];
  const localVariables = currentTree?.localVariables || [];
  
  const handleUpdateShared = (name: string, updates: Partial<Variable>) => updateVariable(false, name, updates);
  const handleDeleteShared = (name: string) => removeVariable(false, name);
  const handleAddShared = (v: Variable) => addVariable(false, v);
  
  const handleUpdateLocal = (name: string, updates: Partial<Variable>) => updateVariable(true, name, updates);
  const handleDeleteLocal = (name: string) => removeVariable(true, name);
  const handleAddLocal = (v: Variable) => addVariable(true, v);
  
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
            <div className="space-y-1">
              {sharedVariables.map((v) => (
                <VariableItem
                  key={v.name}
                  variable={v}
                  onUpdate={handleUpdateShared}
                  onDelete={handleDeleteShared}
                />
              ))}
            </div>
            <AddVariableButton isLocal={false} onAdd={handleAddShared} />
          </div>
          
          {/* Local Variables */}
          <div>
            <div className="text-xs text-gray-500 mb-1">Local</div>
            <div className="space-y-1">
              {localVariables.map((v) => (
                <VariableItem
                  key={v.name}
                  variable={v}
                  onUpdate={handleUpdateLocal}
                  onDelete={handleDeleteLocal}
                />
              ))}
            </div>
            <AddVariableButton isLocal={true} onAdd={handleAddLocal} />
          </div>
        </div>
      </div>

      {/* Properties 面板 */}
      <div className="border-t border-gray-700 flex-1 overflow-auto">
        <div className="p-3">
          <h2 className="text-xs text-gray-500 uppercase tracking-wider mb-2">
            Properties
          </h2>
          {selectedNodeIds.length === 0 ? (
            <div className="text-xs text-gray-600">Select a node</div>
          ) : selectedNodeIds.length === 1 ? (
            <NodePropertiesEditor nodeId={selectedNodeIds[0]} />
          ) : (
            <div className="text-xs text-gray-400">
              {selectedNodeIds.length} nodes selected
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// 节点属性编辑器
function NodePropertiesEditor({ nodeId }: { nodeId: string }) {
  const getCurrentTree = useEditorStore((state) => state.getCurrentTree);
  const updatePin = useEditorStore((state) => state.updatePin);
  const updateNodeProperty = useEditorStore((state) => state.updateNodeProperty);
  const currentTree = getCurrentTree();
  
  const node = currentTree?.nodes.get(nodeId);
  if (!node) return <div className="text-xs text-gray-600">Node not found</div>;
  
  const sharedVars = currentTree?.sharedVariables || [];
  const localVars = currentTree?.localVariables || [];
  const dataConnections = currentTree?.dataConnections || [];
  
  return (
    <div className="space-y-3">
      {/* 节点类型 */}
      <div className="text-sm text-gray-300 font-medium">{node.type}</div>
      
      {/* 注释 */}
      <div>
        <div className="text-xs text-gray-500 mb-1">Comment</div>
        <input
          className="w-full bg-gray-700 text-gray-300 text-xs px-2 py-1 rounded outline-none"
          value={node.comment || ''}
          onChange={(e) => updateNodeProperty(nodeId, { comment: e.target.value })}
          placeholder="Add comment..."
        />
      </div>
      
      {/* Return 属性（如果有） */}
      {node.extraAttrs?.Return !== undefined && (
        <div>
          <div className="text-xs text-gray-500 mb-1">Return</div>
          <select
            className="w-full bg-gray-700 text-gray-300 text-xs px-2 py-1 rounded outline-none"
            value={node.extraAttrs.Return}
            onChange={(e) => updateNodeProperty(nodeId, { 
              extraAttrs: { ...node.extraAttrs, Return: e.target.value } 
            })}
          >
            <option value="Success">Success</option>
            <option value="Failure">Failure</option>
            <option value="Running">Running</option>
          </select>
        </div>
      )}
      
      {/* Pin 列表 */}
      <div>
        <div className="text-xs text-gray-500 mb-1">Pins</div>
        <div className="space-y-2">
          {node.pins.map((pin) => {
            // 检查此 Pin 是否有数据连接
            const hasDataConn = dataConnections.some(
              dc => (dc.toNodeId === nodeId && dc.toPinName === pin.name) ||
                    (dc.fromNodeId === nodeId && dc.fromPinName === pin.name)
            );
            return (
              <PinEditor
                key={pin.name}
                pin={pin}
                sharedVars={sharedVars}
                localVars={localVars}
                hasDataConnection={hasDataConn}
                onUpdate={(updates) => updatePin(nodeId, pin.name, updates)}
              />
            );
          })}
        </div>
      </div>
    </div>
  );
}

// Pin 编辑器
interface PinEditorProps {
  pin: Pin;
  sharedVars: Variable[];
  localVars: Variable[];
  hasDataConnection: boolean;  // 是否有数据连接
  onUpdate: (updates: Partial<Pin>) => void;
}

function PinEditor({ pin, sharedVars, localVars, hasDataConnection, onUpdate }: PinEditorProps) {
  // 获取当前值
  const currentValue = pin.binding.type === 'const' ? pin.binding.value : '';
  const [editValue, setEditValue] = useState(currentValue);
  const [vectorIndexValue, setVectorIndexValue] = useState(
    pin.vectorIndex?.type === 'const' ? pin.vectorIndex.value : ''
  );
  
  // 当 pin 变化时更新 editValue
  useEffect(() => {
    const value = pin.binding.type === 'const' ? pin.binding.value : '';
    setEditValue(value);
    setVectorIndexValue(pin.vectorIndex?.type === 'const' ? pin.vectorIndex.value : '');
  }, [pin.name, pin.binding, pin.vectorIndex]);
  
  const colorClass = TYPE_COLORS[pin.valueType] || 'text-gray-400';
  const isConst = pin.binding.type === 'const';
  const isEnabled = pin.enableType !== 'disable';
  const canToggleEnable = pin.enableType !== 'fixed';
  
  // 检查选中的变量是否是数组（需要 Vector Index）
  const selectedVar = pin.binding.type === 'pointer' 
    ? [...sharedVars, ...localVars].find(v => v.name === (pin.binding as { variableName: string }).variableName)
    : null;
  const needsVectorIndex = selectedVar && selectedVar.countType === 'list' && pin.countType === 'scalar';
  const hasVectorIndex = pin.vectorIndex !== undefined;
  
  const handleBindingToggle = () => {
    if (isConst) {
      // 切换到引用
      const newBinding: PinBinding = {
        type: 'pointer',
        variableName: sharedVars[0]?.name || '',
        isLocal: false,
      };
      onUpdate({ binding: newBinding, vectorIndex: undefined });
    } else {
      // 切换到常量
      const newBinding: PinBinding = {
        type: 'const',
        value: '',
      };
      onUpdate({ binding: newBinding, vectorIndex: undefined });
    }
  };
  
  const handleValueChange = (value: string) => {
    setEditValue(value);
    if (isConst) {
      onUpdate({ binding: { type: 'const', value } });
    }
  };
  
  const handleVariableChange = (varName: string, isLocal: boolean) => {
    // 检查新变量是否需要 Vector Index
    const newVar = [...sharedVars, ...localVars].find(v => v.name === varName);
    const newNeedsVI = newVar && newVar.countType === 'list' && pin.countType === 'scalar';
    
    onUpdate({ 
      binding: { type: 'pointer', variableName: varName, isLocal },
      vectorIndex: newNeedsVI ? { type: 'const', value: '0' } : undefined
    });
  };
  
  const handleEnableToggle = () => {
    if (!canToggleEnable) return;
    onUpdate({ enableType: isEnabled ? 'disable' : 'enable' });
  };
  
  const handleVectorIndexChange = (value: string) => {
    setVectorIndexValue(value);
    onUpdate({ vectorIndex: { type: 'const', value } });
  };
  
  const handleVectorIndexTypeToggle = () => {
    if (pin.vectorIndex?.type === 'const') {
      // 切换到变量
      onUpdate({ vectorIndex: { type: 'pointer', variableName: sharedVars[0]?.name || '', isLocal: false } });
    } else {
      // 切换到常量
      onUpdate({ vectorIndex: { type: 'const', value: '0' } });
    }
  };
  
  const handleVectorIndexVarChange = (varName: string, isLocal: boolean) => {
    onUpdate({ vectorIndex: { type: 'pointer', variableName: varName, isLocal } });
  };
  
  // 过滤 int 类型的变量（用于 Vector Index）
  const intSharedVars = sharedVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');
  const intLocalVars = localVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');
  
  return (
    <div className={`text-xs bg-gray-800 rounded p-1.5 ${!isEnabled ? 'opacity-50' : ''}`}>
      <div className="flex items-center gap-1 mb-1">
        {/* 启用/禁用切换 */}
        {canToggleEnable && (
          <button
            className={`w-3 h-3 rounded-sm border ${isEnabled ? 'bg-green-600 border-green-500' : 'bg-gray-600 border-gray-500'}`}
            onClick={handleEnableToggle}
            title={isEnabled ? 'Disable' : 'Enable'}
          />
        )}
        
        {/* Pin 名称 */}
        <span className={`${colorClass} font-medium`}>{pin.name}</span>
        
        {/* 输入/输出标记 */}
        <span className="text-gray-600 text-[10px]">{pin.isInput ? 'in' : 'out'}</span>
        
        {/* 常量/引用切换 */}
        <button
          className={`ml-auto text-[10px] px-1 rounded ${isConst ? 'bg-gray-600 text-gray-300' : 'bg-blue-600 text-white'}`}
          onClick={handleBindingToggle}
          title={isConst ? 'Switch to variable' : 'Switch to constant'}
        >
          {isConst ? 'C' : 'V'}
        </button>
      </div>
      
      {/* 值编辑 - 有数据连接时显示空 */}
      {hasDataConnection ? (
        <div className="text-gray-500 italic px-1 py-0.5">(data connection)</div>
      ) : isConst ? (
        <input
          className="w-full bg-gray-700 text-gray-300 text-xs px-1 py-0.5 rounded outline-none"
          value={editValue}
          onChange={(e) => handleValueChange(e.target.value)}
          placeholder="Value"
        />
      ) : (
        <select
          className="w-full bg-gray-700 text-gray-300 text-xs px-1 py-0.5 rounded outline-none"
          value={`${pin.binding.type === 'pointer' && pin.binding.isLocal ? 'local:' : 'shared:'}${pin.binding.type === 'pointer' ? pin.binding.variableName : ''}`}
          onChange={(e) => {
            const [scope, name] = e.target.value.split(':');
            handleVariableChange(name, scope === 'local');
          }}
        >
          <optgroup label="Shared">
            {sharedVars.map(v => (
              <option key={`s-${v.name}`} value={`shared:${v.name}`}>{v.name}{v.countType === 'list' ? '[]' : ''}</option>
            ))}
          </optgroup>
          <optgroup label="Local">
            {localVars.map(v => (
              <option key={`l-${v.name}`} value={`local:${v.name}`}>{v.name}{v.countType === 'list' ? '[]' : ''}</option>
            ))}
          </optgroup>
        </select>
      )}
      
      {/* Vector Index 编辑（当选中数组变量且 Pin 是标量时） */}
      {(needsVectorIndex || hasVectorIndex) && !hasDataConnection && (
        <div className="mt-1 flex items-center gap-1">
          <span className="text-gray-500 text-[10px]">Index:</span>
          <button
            className={`text-[10px] px-1 rounded ${pin.vectorIndex?.type === 'const' ? 'bg-gray-600 text-gray-300' : 'bg-blue-600 text-white'}`}
            onClick={handleVectorIndexTypeToggle}
            title={pin.vectorIndex?.type === 'const' ? 'Switch to variable' : 'Switch to constant'}
          >
            {pin.vectorIndex?.type === 'const' ? 'C' : 'V'}
          </button>
          {pin.vectorIndex?.type === 'const' ? (
            <input
              className="flex-1 bg-gray-700 text-gray-300 text-xs px-1 py-0.5 rounded outline-none"
              value={vectorIndexValue}
              onChange={(e) => handleVectorIndexChange(e.target.value)}
              placeholder="0"
            />
          ) : (
            <select
              className="flex-1 bg-gray-700 text-gray-300 text-xs px-1 py-0.5 rounded outline-none"
              value={`${pin.vectorIndex?.type === 'pointer' && pin.vectorIndex.isLocal ? 'local:' : 'shared:'}${pin.vectorIndex?.type === 'pointer' ? pin.vectorIndex.variableName : ''}`}
              onChange={(e) => {
                const [scope, name] = e.target.value.split(':');
                handleVectorIndexVarChange(name, scope === 'local');
              }}
            >
              <optgroup label="Shared">
                {intSharedVars.map(v => (
                  <option key={`vi-s-${v.name}`} value={`shared:${v.name}`}>{v.name}</option>
                ))}
              </optgroup>
              <optgroup label="Local">
                {intLocalVars.map(v => (
                  <option key={`vi-l-${v.name}`} value={`local:${v.name}`}>{v.name}</option>
                ))}
              </optgroup>
            </select>
          )}
        </div>
      )}
    </div>
  );
}
