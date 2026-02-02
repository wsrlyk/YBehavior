import { useState, useEffect, useRef } from 'react';
import { useEditorStore } from '../stores/editorStore';
import type { Variable, ValueType, CountType, Pin } from '../types';

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

// 宽度自适应的自定义下拉选择组件 (用于类型选择)
function AdaptiveSelect({ value, options, onChange, renderLabel, getOptionClass, baseClassName, triggerClassName, containerClassName, disabled }: {
  value: string;
  options: string[];
  onChange: (val: string) => void;
  renderLabel?: (val: string) => React.ReactNode;
  getOptionClass?: (val: string) => string;
  baseClassName?: string; // 基础样式（如字体大小），应用到 Trigger 和 Option
  triggerClassName?: string; // Trigger 专用样式（如当前值的颜色）
  containerClassName?: string; // 容器样式
  disabled?: boolean;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // 点击外部关闭
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
    }
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [isOpen]);

  const displayValue = renderLabel ? renderLabel(value) : (value.charAt(0).toUpperCase() + value.slice(1));

  if (disabled) {
    return (
      <span className={`${baseClassName} ${triggerClassName} opacity-70 cursor-default px-0.5`}>
        {displayValue}
      </span>
    );
  }

  return (
    <div className={`relative inline-block align-middle ${containerClassName || ''}`} ref={containerRef}>
      {/* Trigger */}
      <span
        className={`${baseClassName} ${triggerClassName} cursor-pointer hover:bg-gray-700 px-1 rounded transition-colors select-none whitespace-nowrap inline-block`}
        onClick={() => setIsOpen(!isOpen)}
        title="Click to change"
      >
        {displayValue}
      </span>

      {/* Custom Dropdown Menu */}
      {isOpen && (
        <div className="absolute top-full left-0 mt-1 z-50 bg-gray-800 border border-gray-600 rounded shadow-xl py-1 min-w-[80px] max-h-60 overflow-y-auto">
          {options.map(opt => {
            const optClass = getOptionClass ? getOptionClass(opt) : 'text-gray-300';
            const optLabel = renderLabel ? renderLabel(opt) : (opt.charAt(0).toUpperCase() + opt.slice(1));
            return (
              <div
                key={opt}
                className={`${optClass} ${baseClassName} px-2 py-1.5 hover:bg-gray-700 cursor-pointer whitespace-nowrap flex items-center`}
                onClick={() => {
                  onChange(opt);
                  setIsOpen(false);
                }}
              >
                {/* 选中标记 */}
                <span className={`w-3 mr-1 ${opt === value ? 'opacity-100' : 'opacity-0'}`}>✓</span>
                {optLabel}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
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
      case 'enum': return ''; // Enums might need a default from their enumValues
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



  return (
    <div className="px-2 py-1 text-sm bg-gray-800 rounded group">
      <div className="flex items-center gap-1">
        {/* 类型选择 - 下拉选取 (自适应宽度) */}
        <AdaptiveSelect
          value={variable.valueType}
          options={VALUE_TYPES}
          onChange={(val) => handleTypeChange(val as ValueType)}
          baseClassName="text-xs"
          triggerClassName={colorClass}
          getOptionClass={(opt) => TYPE_COLORS[opt as ValueType] || 'text-gray-300'}
        />

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
  const updatePinsByTypeGroup = useEditorStore((state) => state.updatePinsByTypeGroup);
  const currentTree = getCurrentTree();

  const node = currentTree?.nodes.get(nodeId);
  if (!node) return <div className="text-xs text-gray-600">Node not found</div>;

  const sharedVars = currentTree?.sharedVariables || [];
  const localVars = currentTree?.localVariables || [];
  const dataConnections = currentTree?.dataConnections || [];

  // 分组 Pin：输入和输出
  const inputPins = node.pins.filter(pin => pin.isInput);
  const outputPins = node.pins.filter(pin => !pin.isInput);

  // 处理类型变化（带类型联动）
  const handlePinUpdate = (pinName: string, updates: Partial<Pin>) => {
    const pin = node.pins.find(p => p.name === pinName);

    // 如果更新了 valueType 且有 vTypeGroup，触发类型联动
    if (updates.valueType && pin?.vTypeGroup !== undefined) {
      updatePinsByTypeGroup(nodeId, pin.vTypeGroup, updates.valueType);
    } else {
      updatePin(nodeId, pinName, updates);
    }
  };

  const renderPinList = (pins: Pin[], label: string) => {
    if (pins.length === 0) return null;

    return (
      <div className="mb-2">
        <div className="text-xs text-gray-500 mb-1">{label}</div>
        <div className="space-y-2">
          {pins.map((pin) => {
            const dataConn = dataConnections.find(
              dc => (dc.toNodeId === nodeId && dc.toPinName === pin.name) ||
                (dc.fromNodeId === nodeId && dc.fromPinName === pin.name)
            );
            return (
              <PinEditor
                key={pin.name}
                pin={pin}
                sharedVars={sharedVars}
                localVars={localVars}
                dataConnection={dataConn}
                onUpdate={(updates) => handlePinUpdate(pin.name, updates)}
              />
            );
          })}
        </div>
      </div>
    );
  };

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

      {/* Pin 列表（分组显示） */}
      <div>
        <div className="text-xs text-gray-500 mb-1">Pins</div>
        {renderPinList(inputPins, 'Inputs')}
        {renderPinList(outputPins, 'Outputs')}
      </div>
    </div>
  );
}

// Pin 编辑器
interface PinEditorProps {
  pin: Pin;
  sharedVars: Variable[];
  localVars: Variable[];
  dataConnection?: import('../types').DataConnection;  // 当前 Pin 的数据连接
  onUpdate: (updates: Partial<Pin>) => void;
}

function PinEditor({ pin, sharedVars, localVars, dataConnection, onUpdate }: PinEditorProps) {
  const removeDataConnection = useEditorStore((state) => state.removeDataConnection);
  const hasDataConnection = !!dataConnection;

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
  const bindingType = pin.binding.type;  // 'const' | 'pointer'
  const isEnabled = pin.enableType !== 'disable';
  const canToggleEnable = pin.enableType !== 'fixed';

  // 判断是否为数据连接状态（pointer + 空变量名）
  const isDataConnection = bindingType === 'pointer' &&
    (pin.binding.type === 'pointer' && !pin.binding.variableName);

  // 检查选中的变量是否是数组（需要 Vector Index）
  // 逻辑：bindingType=pointer => 查找变量 => 变量是 array & pin 是 scalar => 需要 index
  const selectedVarName = pin.binding.type === 'pointer' ? pin.binding.variableName : '';
  const selectedVar = selectedVarName
    ? [...sharedVars, ...localVars].find(v => v.name === selectedVarName)
    : null;

  const needsVectorIndex = selectedVar && selectedVar.countType === 'list' && pin.countType === 'scalar';
  const hasVectorIndex = pin.vectorIndex !== undefined;

  // 切换绑定类型：C(常量) <-> V(变量引用)
  const handleBindingToggle = () => {
    // 如果当前有数据连接且要切换到常量，先断开连接
    if (bindingType === 'pointer' && hasDataConnection && isDataConnection && dataConnection) {
      removeDataConnection(dataConnection.id);
    }

    if (bindingType === 'const') {
      // 常量 -> 变量引用（默认选第一个兼容变量，如果没有则选空-数据连接）
      const compatibleVars = getCompatibleVariables();
      const firstVar = compatibleVars[0];

      if (firstVar) {
        onUpdate({
          binding: { type: 'pointer', variableName: firstVar.name, isLocal: !sharedVars.includes(firstVar) },
          bindingType: 'pointer',
          vectorIndex: undefined
        });
      } else {
        // 没有兼容变量，默认为数据连接
        onUpdate({
          binding: { type: 'pointer', variableName: '', isLocal: false },
          bindingType: 'pointer',
          vectorIndex: undefined
        });
      }
    } else {
      // 变量 -> 常量
      onUpdate({ binding: { type: 'const', value: '' }, bindingType: 'const', vectorIndex: undefined });
    }
  };

  const handleValueChange = (value: string) => {
    setEditValue(value);
    if (bindingType === 'const') {
      onUpdate({ binding: { type: 'const', value } });
    }
  };

  const handleVariableChange = (varName: string, isLocal: boolean) => {
    if (!varName) {
      // 选择了 "(Data Connection)"
      onUpdate({
        binding: { type: 'pointer', variableName: '', isLocal: false },
        vectorIndex: undefined
      });
      return;
    }

    // 如果当前有数据连接，切换到变量时需要断开连接
    if (dataConnection) {
      removeDataConnection(dataConnection.id);
    }

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



  // 数组/标量切换
  const toggleCountType = () => {
    onUpdate({ countType: pin.countType === 'list' ? 'scalar' : 'list' });
  };

  // Vector Index 相关
  const handleVectorIndexChange = (value: string) => {
    setVectorIndexValue(value);
    onUpdate({ vectorIndex: { type: 'const', value } });
  };

  const handleVectorIndexTypeToggle = () => {
    if (pin.vectorIndex?.type === 'const') {
      // 切换到变量
      const firstIntVar = intSharedVars[0] || intLocalVars[0];
      if (firstIntVar) {
        onUpdate({ vectorIndex: { type: 'pointer', variableName: firstIntVar.name, isLocal: !intSharedVars.includes(firstIntVar) } });
      }
    } else {
      // 切换到常量
      onUpdate({ vectorIndex: { type: 'const', value: '0' } });
    }
  };

  const handleVectorIndexVarChange = (varName: string, isLocal: boolean) => {
    onUpdate({ vectorIndex: { type: 'pointer', variableName: varName, isLocal } });
  };

  // 过滤兼容的变量
  const getCompatibleVariables = () => {
    // 兼容规则：
    // 1. 值类型匹配 (pin.valueType 或在 allowedValueTypes 中) -- 但这里通常只看当前 valueType 即可，因为切换变量不改变 pin 类型
    // 2. 数量类型匹配：
    //    - Pin 是 list: 只能选 list 变量
    //    - Pin 是 scalar: 可以选 scalar 变量，也可以选 list 变量（需 vector index）

    return [...sharedVars, ...localVars].filter(v => {
      // 类型检查
      if (v.valueType !== pin.valueType) return false;

      // 数量检查
      if (pin.countType === 'list') {
        return v.countType === 'list';
      } else {
        // Pin 是 scalar，scalar/list 变量都支持
        return true;
      }
    });
  };

  const compatibleVars = getCompatibleVariables();
  const compatibleShared = compatibleVars.filter(v => sharedVars.includes(v));
  const compatibleLocal = compatibleVars.filter(v => localVars.includes(v));

  // 过滤 int 类型的变量（用于 Vector Index）
  const intSharedVars = sharedVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');
  const intLocalVars = localVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');

  // 绑定按钮
  const bindingBtnInfo = bindingType === 'const'
    ? { style: 'bg-gray-600 text-gray-300', text: 'C', title: 'Constant → Click to switch to Variable' }
    : { style: 'bg-blue-600 text-white', text: 'V', title: 'Variable → Click to switch to Constant' };


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

        {/* 数据类型切换 (与 Variables 面板一致交互) - 下拉选取 (自适应宽度) */}
        <AdaptiveSelect
          value={pin.valueType}
          options={pin.allowedValueTypes}
          onChange={(val) => onUpdate({ valueType: val as ValueType })}
          baseClassName="text-[10px]"
          triggerClassName={colorClass}
          getOptionClass={(opt) => TYPE_COLORS[opt as ValueType] || 'text-gray-300'}
          disabled={pin.allowedValueTypes.length <= 1}
        />

        {/* 数组/标量切换 (与 Variables 面板一致交互) */}
        {!pin.isCountTypeFixed && (
          <button
            className={`text-[10px] ${colorClass} hover:opacity-80 min-w-3 text-center`}
            onClick={toggleCountType}
            title={pin.countType === 'list' ? 'Switch to scalar' : 'Switch to array'}
          >
            {pin.countType === 'list' ? '[]' : '·'}
          </button>
        )}

        {/* 固定数组/标量显示 */}
        {pin.isCountTypeFixed && (
          <span className={`text-[10px] ${colorClass} opacity-50 min-w-3 text-center cursor-default`} title="Fixed count type">
            {pin.countType === 'list' ? '[]' : '·'}
          </span>
        )}

        {/* 绑定类型切换：C(常量) / V(变量) */}
        {pin.isBindingTypeFixed ? (
          <span
            className={`ml-auto text-[10px] px-1 rounded ${bindingBtnInfo.style} opacity-50 cursor-default`}
            title="Fixed binding type"
          >
            {bindingBtnInfo.text}
          </span>
        ) : (
          <button
            className={`ml-auto text-[10px] px-1 rounded ${bindingBtnInfo.style}`}
            onClick={handleBindingToggle}
            title={bindingBtnInfo.title}
          >
            {bindingBtnInfo.text}
          </button>
        )}
      </div>

      {/* 值编辑区域 */}
      {bindingType === 'const' && (
        <>
          {pin.valueType === 'enum' && pin.enumValues && pin.enumValues.length > 0 ? (
            <div className="w-full bg-gray-700 rounded px-1 py-0.5">
              <AdaptiveSelect
                value={editValue || pin.enumValues[0]}
                options={pin.enumValues}
                onChange={(val) => handleValueChange(val)}
                baseClassName="text-xs text-gray-300"
                triggerClassName="w-full"
                containerClassName="w-full block"
                getOptionClass={() => 'text-gray-300'}
                renderLabel={(val) => val}
              />
            </div>
          ) : (
            <input
              className="w-full bg-gray-700 text-gray-300 text-xs px-1 py-0.5 rounded outline-none"
              value={editValue}
              onChange={(e) => handleValueChange(e.target.value)}
              placeholder="Value"
            />
          )}
        </>
      )}
      {bindingType === 'pointer' && (
        <div className="space-y-1">
          <select
            className="w-full bg-gray-700 text-gray-300 text-xs px-1 py-0.5 rounded outline-none"
            value={`${isDataConnection ? 'data' : (pin.binding.type === 'pointer' && pin.binding.isLocal ? 'local:' : 'shared:')}${pin.binding.type === 'pointer' ? pin.binding.variableName : ''}`}
            onChange={(e) => {
              if (e.target.value === 'data') {
                handleVariableChange('', false);
              } else {
                const [scope, name] = e.target.value.split(':');
                handleVariableChange(name, scope === 'local');
              }
            }}
          >
            {/* 顶层空值：数据连接 */}
            <option value="data">(Data Connection)</option>

            {compatibleShared.length > 0 && (
              <optgroup label="Shared">
                {compatibleShared.map(v => (
                  <option key={`s-${v.name}`} value={`shared:${v.name}`}>{v.name}{v.countType === 'list' ? '[]' : ''}</option>
                ))}
              </optgroup>
            )}

            {compatibleLocal.length > 0 && (
              <optgroup label="Local">
                {compatibleLocal.map(v => (
                  <option key={`l-${v.name}`} value={`local:${v.name}`}>{v.name}{v.countType === 'list' ? '[]' : ''}</option>
                ))}
              </optgroup>
            )}
          </select>

          {/* 数据连接状态提示 */}
          {isDataConnection && (
            <div className={`px-1 py-0.5 text-[10px] ${hasDataConnection ? 'text-purple-400' : 'text-gray-500 italic'}`}>
              {hasDataConnection ? '● Connected' : '○ Waiting for connection...'}
            </div>
          )}
        </div>
      )}


      {/* Vector Index 编辑（当选中数组变量且 Pin 是标量时） */}
      {(needsVectorIndex || hasVectorIndex) && bindingType === 'pointer' && (
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
