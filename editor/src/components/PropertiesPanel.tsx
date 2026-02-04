import { useState, useEffect, useRef, memo, useMemo, useCallback } from 'react';
import { useEditorStore } from '../stores/editorStore';
import type { Variable, ValueType, CountType, Pin } from '../types';
import { validateValue, getDefaultValue, validateVariableName } from '../utils/validation';
import { useNotificationStore } from '../stores/notificationStore';
import { logger } from '../utils/logger';

const TYPE_COLORS: Record<ValueType, string> = {
  int: 'text-blue-400',
  float: 'text-green-400',
  bool: 'text-red-400',
  string: 'text-pink-400',
  vector3: 'text-yellow-400',
  entity: 'text-cyan-400',
  ulong: 'text-purple-400',
  enum: 'text-orange-400',
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
        className={`${baseClassName} ${triggerClassName} cursor-pointer hover:bg-gray-700 px-0.5 rounded transition-colors select-none whitespace-nowrap inline-block`}
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
  const notify = useNotificationStore(state => state.notify);

  const handleValueSubmit = () => {
    const result = validateValue(editValue, variable.valueType, variable.countType);
    if (!result.isValid) {
      const errorMsg = `Variable [${variable.name}] invalid: ${result.error}`;
      notify(result.error || 'Invalid value', 'error');
      logger.error(errorMsg);
    }
    onUpdate(variable.name, { defaultValue: editValue });
    setIsEditing(false);
  };

  const validation = validateValue(variable.defaultValue, variable.valueType, variable.countType);
  const isValid = validation.isValid;

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
      <div className="flex items-center gap-0.5">
        <AdaptiveSelect
          value={variable.valueType}
          options={VALUE_TYPES}
          onChange={(val) => handleTypeChange(val as ValueType)}
          baseClassName="text-xs"
          triggerClassName={colorClass}
          getOptionClass={(opt) => TYPE_COLORS[opt as ValueType] || 'text-gray-300'}
        />

        <button
          className={`text-xs hover:bg-gray-700 ${colorClass} text-center px-0.5 rounded transition-colors min-w-[14px]`}
          onClick={handleCountTypeToggle}
          title={isArray ? 'Switch to scalar' : 'Switch to array'}
        >
          {isArray ? '[]' : '·'}
        </button>

        <span className="text-gray-300 flex-1 truncate">{variable.name}{variable.isLocal ? "'" : ""}</span>

        <button
          className="text-gray-600 hover:text-red-400 opacity-0 group-hover:opacity-100 text-xs"
          onClick={() => onDelete(variable.name)}
          title="Delete variable"
        >
          ✕
        </button>
      </div>

      <div className="flex items-center gap-1 mt-1">
        <span className="text-gray-500 text-xs">=</span>
        {isEditing ? (
          <input
            className={`flex-1 text-xs px-1 rounded outline-none ${!validateValue(editValue, variable.valueType, variable.countType).isValid
              ? 'bg-red-900 text-white'
              : 'bg-gray-700 text-gray-300'
              }`}
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            onBlur={handleValueSubmit}
            onKeyDown={(e) => e.key === 'Enter' && handleValueSubmit()}
            autoFocus
          />
        ) : (
          <span
            className={`flex-1 text-xs truncate cursor-pointer hover:text-gray-300 ${!isValid ? 'bg-red-900 text-white px-1 rounded' : 'text-gray-400'
              }`}
            onClick={() => { setEditValue(variable.defaultValue); setIsEditing(true); }}
          >
            {variable.defaultValue || (variable.valueType === 'string' ? '""' : '(empty)')}
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
    const validation = validateVariableName(name);
    return (
      <div className="flex items-center gap-1 mt-1">
        <input
          className={`flex-1 text-gray-300 text-xs px-1 rounded outline-none ${!validation.isValid && name.length > 0 ? 'bg-red-900 border-red-500' : 'bg-gray-700'
            }`}
          placeholder="Variable name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
          autoFocus
        />
        <button className="text-green-400 text-xs" onClick={handleAdd} title={validation.error}>✓</button>
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

// ==================== Tree Interface (I/O) ====================

interface InterfacePinItemProps {
  pin: import('../types').TreeInterfacePin;
  isInput: boolean;
  onUpdate: (id: string, updates: Partial<import('../types').TreeInterfacePin>) => void;
  onDelete: (id: string) => void;
  sharedVars: Variable[];
  localVars: Variable[];
}

function InterfacePinItem({ pin, isInput, onUpdate, onDelete, sharedVars, localVars }: InterfacePinItemProps) {
  const colorClass = TYPE_COLORS[pin.valueType] || 'text-gray-400';
  const isArray = pin.countType === 'list';

  const handleBindingChange = (val: string, type: 'variable' | 'const', isLocal?: boolean) => {
    let vectorIndex = pin.vectorIndex;
    if (type === 'variable') {
      const v = (isLocal ? localVars : sharedVars).find(v => v.name === val);
      const needsVI = v?.countType === 'list' && pin.countType === 'scalar';
      if (needsVI && !vectorIndex) {
        vectorIndex = { type: 'const', value: '0' };
      } else if (!needsVI) {
        vectorIndex = undefined;
      }
    } else {
      vectorIndex = undefined;
    }

    onUpdate(pin.id, {
      binding: { type, value: val, isLocal },
      vectorIndex
    });
  };

  const handleVectorIndexTypeToggle = () => {
    if (pin.vectorIndex?.type === 'const') {
      const intSharedVars = sharedVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');
      const intLocalVars = localVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');
      const firstIntVar = intSharedVars[0] || intLocalVars[0];
      if (firstIntVar) {
        onUpdate(pin.id, {
          vectorIndex: {
            type: 'pointer',
            variableName: firstIntVar.name,
            isLocal: !sharedVars.includes(firstIntVar)
          }
        });
      }
    } else {
      onUpdate(pin.id, { vectorIndex: { type: 'const', value: '0' } });
    }
  };

  const selectedVar = [...sharedVars, ...localVars].find(v => v.name === pin.binding.value);
  const needsVectorIndex = selectedVar?.countType === 'list' && pin.countType === 'scalar';
  const hasVectorIndex = !!pin.vectorIndex;

  const intSharedVars = sharedVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');
  const intLocalVars = localVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');

  return (
    <div className="px-2 py-1 text-sm bg-gray-800 rounded group border border-transparent hover:border-gray-600 transition-colors">
      <div className="flex items-center gap-0.5">
        <AdaptiveSelect
          value={pin.valueType}
          options={VALUE_TYPES}
          onChange={(val) => onUpdate(pin.id, {
            valueType: val as ValueType,
            binding: {
              type: pin.binding.type,
              value: pin.binding.type === 'variable' ? '' : getDefaultValue(val as ValueType, isArray),
              isLocal: pin.binding.isLocal
            }
          })}
          baseClassName="text-xs"
          triggerClassName={colorClass}
          getOptionClass={(opt) => TYPE_COLORS[opt as ValueType] || 'text-gray-300'}
        />

        <button
          className={`text-xs hover:bg-gray-700 ${colorClass} text-center px-0.5 rounded transition-colors min-w-[14px]`}
          onClick={() => onUpdate(pin.id, { countType: isArray ? 'scalar' : 'list' })}
          title={isArray ? 'Switch to scalar' : 'Switch to array'}
        >
          {isArray ? '[]' : '·'}
        </button>

        <span className="text-white flex-1 truncate font-medium">{pin.name}</span>

        <button
          className="text-gray-600 hover:text-red-400 opacity-0 group-hover:opacity-100 text-xs transition-opacity"
          onClick={() => onDelete(pin.id)}
          title="Delete pin"
        >
          ✕
        </button>
      </div>

      <div className="flex flex-col gap-1 mt-1 pb-0.5">
        <div className="flex items-center gap-1">
          <span className="text-gray-500 text-[10px] w-12">{isInput ? 'Inner Var:' : 'Source:'}</span>

          {isInput || pin.binding.type === 'variable' ? (
            <select
              className="flex-1 bg-gray-700 text-gray-200 text-[10px] px-1 py-0.5 rounded outline-none border border-gray-600 focus:border-blue-500"
              value={pin.binding.value ? `${pin.binding.isLocal ? 'local' : 'shared'}:${pin.binding.value}` : ''}
              onChange={(e) => {
                const [scope, name] = e.target.value.split(':');
                handleBindingChange(name, 'variable', scope === 'local');
              }}
            >
              <option value="">(None)</option>
              {sharedVars.length > 0 && (
                <optgroup label="Shared">
                  {sharedVars.map(v => (
                    <option key={`s-${v.name}`} value={`shared:${v.name}`}>{v.name}{v.countType === 'list' ? '[]' : ''}</option>
                  ))}
                </optgroup>
              )}
              {localVars.length > 0 && (
                <optgroup label="Local">
                  {localVars.map(v => (
                    <option key={`l-${v.name}`} value={`local:${v.name}`}>{v.name}'{v.countType === 'list' ? '[]' : ''}</option>
                  ))}
                </optgroup>
              )}
            </select>
          ) : (
            <input
              className={`flex-1 bg-gray-700 text-gray-200 text-[10px] px-1 py-0.5 rounded outline-none border ${!validateValue(pin.binding.value, pin.valueType, pin.countType).isValid ? 'border-red-500' : 'border-gray-600 focus:border-blue-500'
                }`}
              value={pin.binding.value}
              onChange={(e) => handleBindingChange(e.target.value, 'const')}
              placeholder="Constant value"
            />
          )}

          {!isInput && (
            <button
              className={`text-[10px] px-1 py-0.5 rounded leading-tight transition-colors ${pin.binding.type === 'const' ? 'bg-gray-600 text-gray-300' : 'bg-blue-600 text-white'}`}
              onClick={() => handleBindingChange('', pin.binding.type === 'const' ? 'variable' : 'const', false)}
              title={pin.binding.type === 'const' ? 'Switch to Variable binding' : 'Switch to Constant binding'}
            >
              {pin.binding.type === 'const' ? 'C' : 'V'}
            </button>
          )}
        </div>

        {(needsVectorIndex || hasVectorIndex) && (
          <div className="flex items-center gap-1 pl-12">
            <span className="text-gray-500 text-[10px]">Index:</span>
            <button
              className={`text-[10px] px-1 rounded ${pin.vectorIndex?.type === 'const' ? 'bg-gray-600 text-gray-300' : 'bg-blue-600 text-white'}`}
              onClick={handleVectorIndexTypeToggle}
            >
              {pin.vectorIndex?.type === 'const' ? 'C' : 'V'}
            </button>
            {pin.vectorIndex?.type === 'const' ? (
              <input
                className={`flex-1 bg-gray-700 text-gray-200 text-[10px] px-1 py-0.5 rounded outline-none border border-gray-600`}
                value={pin.vectorIndex.value}
                onChange={(e) => onUpdate(pin.id, { vectorIndex: { type: 'const', value: e.target.value } })}
              />
            ) : (
              <select
                className={`flex-1 bg-gray-700 text-gray-200 text-[10px] px-1 py-0.5 rounded outline-none border border-gray-600`}
                value={pin.vectorIndex ? `${pin.vectorIndex.isLocal ? 'local' : 'shared'}:${pin.vectorIndex.variableName}` : ''}
                onChange={(e) => {
                  const [scope, name] = e.target.value.split(':');
                  onUpdate(pin.id, { vectorIndex: { type: 'pointer', variableName: name, isLocal: scope === 'local' } });
                }}
              >
                {intSharedVars.map(v => <option key={`s-${v.name}`} value={`shared:${v.name}`}>{v.name}</option>)}
                {intLocalVars.map(v => <option key={`l-${v.name}`} value={`local:${v.name}`}>{v.name}</option>)}
              </select>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function AddInterfacePinButton({ isInput, onAdd }: { isInput: boolean; onAdd: (name: string) => void }) {
  const [isAdding, setIsAdding] = useState(false);
  const [name, setName] = useState('');

  const handleAdd = () => {
    if (name.trim()) {
      onAdd(name.trim());
      setName('');
      setIsAdding(false);
    }
  };

  if (isAdding) {
    return (
      <div className="flex items-center gap-1 mt-2 bg-gray-800 p-1.5 rounded border border-gray-700">
        <input
          className="flex-1 bg-gray-900 text-gray-300 text-xs px-2 py-1 rounded outline-none border border-gray-600 focus:border-blue-500"
          placeholder="Pin name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
          autoFocus
        />
        <button className="text-green-400 hover:text-green-300 px-1 py-1" onClick={handleAdd} title="Confirm">✓</button>
        <button className="text-gray-500 hover:text-gray-400 px-1 py-1" onClick={() => setIsAdding(false)} title="Cancel">✕</button>
      </div>
    );
  }

  return (
    <button
      className="text-[11px] text-gray-500 hover:text-blue-400 mt-2 flex items-center gap-1 transition-colors px-1"
      onClick={() => setIsAdding(true)}
    >
      <span className="text-base leading-none underline-none">+</span>
      <span>Add {isInput ? 'Input' : 'Output'}</span>
    </button>
  );
}

export function PropertiesPanel() {
  const [activeTab, setActiveTab] = useState<'variables' | 'io'>('variables');

  // 优化：直接订阅 currentTree
  const currentTree = useEditorStore((state) => state.getCurrentTree());
  const selectedNodeIds = useEditorStore((state) => state.selectedNodeIds);
  const updateVariable = useEditorStore((state) => state.updateVariable);
  const removeVariable = useEditorStore((state) => state.removeVariable);
  const addVariable = useEditorStore((state) => state.addVariable);

  const addTreeInterfacePin = useEditorStore((state) => state.addTreeInterfacePin);
  const updateTreeInterfacePin = useEditorStore((state) => state.updateTreeInterfacePin);
  const removeTreeInterfacePin = useEditorStore((state) => state.removeTreeInterfacePin);

  const sharedVariables = currentTree?.sharedVariables || [];
  const localVariables = currentTree?.localVariables || [];
  const inputs = currentTree?.inputs || [];
  const outputs = currentTree?.outputs || [];

  const handleUpdateShared = (name: string, updates: Partial<Variable>) => updateVariable(false, name, updates);
  const handleDeleteShared = (name: string) => removeVariable(false, name);
  const handleAddShared = (v: Variable) => addVariable(false, v);

  const handleUpdateLocal = (name: string, updates: Partial<Variable>) => updateVariable(true, name, updates);
  const handleDeleteLocal = (name: string) => removeVariable(true, name);
  const handleAddLocal = (v: Variable) => addVariable(true, v);

  const handleAddInterfacePin = (isInput: boolean, name: string) => {
    addTreeInterfacePin(isInput, {
      id: `${isInput ? 'in' : 'out'}-${Date.now()}`,
      name,
      valueType: 'int',
      countType: 'scalar',
      binding: { type: isInput ? 'variable' : 'const', value: isInput ? '' : '0' }
    });
  };

  return (
    <div className="w-64 h-full bg-gray-900 border-l border-gray-700 flex flex-col">
      {/* Tabs */}
      <div className="flex border-b border-gray-800">
        <button
          className={`flex-1 py-2 text-[10px] uppercase tracking-wider font-medium transition-colors ${activeTab === 'variables' ? 'text-blue-400 bg-gray-800' : 'text-gray-500 hover:text-gray-300'}`}
          onClick={() => setActiveTab('variables')}
        >
          Variables
        </button>
        <button
          className={`flex-1 py-2 text-[10px] uppercase tracking-wider font-medium transition-colors ${activeTab === 'io' ? 'text-blue-400 bg-gray-800' : 'text-gray-500 hover:text-gray-300'}`}
          onClick={() => setActiveTab('io')}
        >
          Tree I/O
        </button>
      </div>

      {/* Variables 面板 */}
      <div className="flex-1 overflow-auto">
        <div className="p-3">
          {activeTab === 'variables' ? (
            <>
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
            </>
          ) : (
            <>
              {/* Inputs */}
              <div className="mb-4">
                <div className="text-xs text-gray-500 mb-1">Inputs</div>
                <div className="space-y-1">
                  {inputs.map((pin) => (
                    <InterfacePinItem
                      key={pin.id}
                      pin={pin}
                      isInput={true}
                      onUpdate={(id, updates) => updateTreeInterfacePin(true, id, updates)}
                      onDelete={(id) => removeTreeInterfacePin(true, id)}
                      sharedVars={sharedVariables}
                      localVars={localVariables}
                    />
                  ))}
                </div>
                <AddInterfacePinButton isInput={true} onAdd={(name) => handleAddInterfacePin(true, name)} />
              </div>

              {/* Outputs */}
              <div>
                <div className="text-xs text-gray-500 mb-1">Outputs</div>
                <div className="space-y-1">
                  {outputs.map((pin) => (
                    <InterfacePinItem
                      key={pin.id}
                      pin={pin}
                      isInput={false}
                      onUpdate={(id, updates) => updateTreeInterfacePin(false, id, updates)}
                      onDelete={(id) => removeTreeInterfacePin(false, id)}
                      sharedVars={sharedVariables}
                      localVars={localVariables}
                    />
                  ))}
                </div>
                <AddInterfacePinButton isInput={false} onAdd={(name) => handleAddInterfacePin(false, name)} />
              </div>
            </>
          )}
        </div>
      </div>

      {/* Properties 面板 */}
      <div className="border-t border-gray-700 flex-1 overflow-auto">
        <div className="p-3">
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
  // 优化：直接订阅
  const currentTree = useEditorStore((state) => state.getCurrentTree());
  const updatePin = useEditorStore((state) => state.updatePin);
  const updateNodeProperty = useEditorStore((state) => state.updateNodeProperty);
  const reloadSubTreePins = useEditorStore((state) => state.reloadSubTreePins);
  const node = useMemo(() => currentTree?.nodes.get(nodeId), [currentTree, nodeId]);
  if (!node) return <div className="text-xs text-gray-600">Node not found</div>;

  const handleReloadSubTree = () => {
    reloadSubTreePins(nodeId);
  };


  const sharedVars = currentTree?.sharedVariables || [];
  const localVars = currentTree?.localVariables || [];
  const dataConnections = currentTree?.dataConnections || [];

  // 分组 Pin：输入和输出
  const inputPins = useMemo(() => node.pins.filter(pin => pin.isInput), [node.pins]);
  const outputPins = useMemo(() => node.pins.filter(pin => !pin.isInput), [node.pins]);

  // 处理 Pin 更新 (内含类型/数量联动及 TypeMap)
  const handlePinUpdate = useCallback((pinName: string, updates: Partial<Pin>) => {
    updatePin(nodeId, pinName, updates);
  }, [updatePin, nodeId]);

  const renderPinList = (pins: Pin[]) => {
    if (pins.length === 0) return null;

    return (
      <div className="mb-1">
        <div className="space-y-1">
          {pins.map((pin) => {
            const dataConn = dataConnections.find(
              dc => (dc.toNodeId === nodeId && dc.toPinName === pin.name) ||
                (dc.fromNodeId === nodeId && dc.fromPinName === pin.name)
            );
            return (
              <PinEditor
                key={pin.name}
                pin={pin}
                nodeId={nodeId}
                nodeUid={node.uid}
                nodeType={node.type}
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
    <div className="space-y-2">
      {/* 节点类型 */}
      <div className="flex items-center justify-between">
        <div className="text-sm text-gray-300 font-medium">{node.type}</div>
        {node.type === 'SubTree' && (
          <button
            className="text-[10px] bg-blue-600 hover:bg-blue-500 text-white px-2 py-0.5 rounded transition-colors"
            onClick={handleReloadSubTree}
            title="Reload Input/Output pins from subtree file"
          >
            Reload Pins
          </button>
        )}
      </div>

      {/* Return 属性 */}
      <div className="flex items-center gap-1">
        <div className="text-[10px] text-gray-500 w-12 shrink-0">Return</div>
        <div className="flex-1 bg-gray-700 rounded px-1 py-0.5">
          <AdaptiveSelect
            value={node.returnType || 'Default'}
            options={['Default', 'Invert', 'Success', 'Failure']}
            onChange={(val) => updateNodeProperty(nodeId, { returnType: val as any })}
            baseClassName="text-xs text-gray-300"
            triggerClassName="w-full"
            containerClassName="w-full block"
            renderLabel={(val) => val === 'Default' ? '(Default)' : val}
          />
        </div>
      </div>

      {/* Nickname */}
      <div className="flex items-center gap-1">
        <input
          className="flex-1 bg-gray-700 text-gray-300 text-xs px-1 py-0.5 rounded outline-none border border-transparent focus:border-blue-500"
          value={node.nickname || ''}
          onChange={(e) => updateNodeProperty(nodeId, { nickname: e.target.value })}
          placeholder="Nickname..."
        />
      </div>

      {/* 注释 */}
      <div>
        <textarea
          className="w-full bg-gray-700 text-gray-300 text-xs px-1 py-0.5 rounded outline-none resize-none overflow-hidden"
          value={node.comment || ''}
          onChange={(e) => {
            updateNodeProperty(nodeId, { comment: e.target.value });
            // 自动调整高度
            e.target.style.height = 'auto';
            e.target.style.height = e.target.scrollHeight + 'px';
          }}
          onFocus={(e) => {
            // 获焦时也触发一次高度调整，确保初始显示正确
            e.target.style.height = 'auto';
            e.target.style.height = e.target.scrollHeight + 'px';
          }}
          placeholder="Comment..."
          spellCheck={false}
          rows={1}
        />
      </div>

      {/* Pin 列表（分组显示） */}
      <div className="pt-1">
        {renderPinList(inputPins)}
        {renderPinList(outputPins)}
      </div>
    </div>
  );
}

// ==================== SubTree Specialized UI ====================

interface TreeFilePickerProps {
  value: string;
  onChange: (value: string) => void;
  options: string[];
}

function TreeFilePicker({ value, onChange, options }: TreeFilePickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  const filteredOptions = options.filter(opt =>
    opt.toLowerCase().includes(search.toLowerCase())
  );

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div className="relative w-full" ref={containerRef}>
      <div
        className="w-full bg-gray-700 text-gray-300 text-xs px-2 py-1 rounded cursor-pointer border border-gray-600 hover:border-blue-500 flex justify-between items-center group"
        onClick={() => {
          setIsOpen(!isOpen);
          if (!isOpen) setSearch('');
        }}
      >
        <span className="truncate flex-1">{value ? value.replace(/\.tree$/, '') : 'Select a tree...'}</span>
        <div className="flex items-center">
          {value && (
            <button
              className="text-[10px] text-gray-500 hover:text-red-400 mr-1 opacity-0 group-hover:opacity-100 transition-opacity"
              onClick={(e) => {
                e.stopPropagation();
                onChange('');
                setIsOpen(false);
              }}
              title="Clear selection"
            >
              ✕
            </button>
          )}
          <span className="text-[10px] text-gray-500">▼</span>
        </div>
      </div>

      {isOpen && (
        <div className="absolute z-50 mt-1 w-full bg-gray-800 border border-gray-700 rounded shadow-xl max-h-48 overflow-hidden flex flex-col">
          <div className="p-1 border-b border-gray-700">
            <input
              className="w-full bg-gray-900 text-gray-300 text-[10px] px-2 py-1 rounded outline-none border border-gray-600 focus:border-blue-500"
              placeholder="Search trees..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              autoFocus
            />
          </div>
          <div className="overflow-y-auto flex-1 custom-scrollbar">
            <div
              className={`px-2 py-1.5 text-[10px] cursor-pointer hover:bg-blue-600 hover:text-white transition-colors truncate ${!value ? 'bg-blue-900 text-blue-200' : 'text-gray-500 italic'}`}
              onClick={() => {
                onChange('');
                setIsOpen(false);
              }}
            >
              (None)
            </div>
            {filteredOptions.length > 0 ? (
              filteredOptions.map(opt => (
                <div
                  key={opt}
                  className={`px-2 py-1.5 text-[10px] cursor-pointer hover:bg-blue-600 hover:text-white transition-colors truncate ${opt === value ? 'bg-blue-900 text-blue-200' : 'text-gray-300'}`}
                  onClick={() => {
                    onChange(opt);
                    setIsOpen(false);
                  }}
                  title={opt}
                >
                  {opt.replace(/\.tree$/, '')}
                </div>
              ))
            ) : (
              <div className="px-2 py-2 text-[10px] text-gray-500 italic">No trees found</div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// Pin 编辑器
interface PinEditorProps {
  pin: Pin;
  nodeId: string;
  nodeUid?: number;
  nodeType?: string;
  sharedVars: Variable[];
  localVars: Variable[];
  dataConnection?: import('../types').DataConnection;  // 当前 Pin 的数据连接
  onUpdate: (updates: Partial<Pin>) => void;
}

const PinEditor = memo(function PinEditor({ pin, nodeId, nodeUid, nodeType, sharedVars, localVars, dataConnection, onUpdate }: PinEditorProps) {
  const treeFiles = useEditorStore(state => state.treeFiles);
  const reloadSubTreePins = useEditorStore(state => state.reloadSubTreePins);
  const removeDataConnection = useEditorStore((state) => state.removeDataConnection);
  const notify = useNotificationStore(state => state.notify);
  const [isEditing, setIsEditing] = useState(false);
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
    if (!isEditing) {
      setEditValue(value);
    }
    setVectorIndexValue(pin.vectorIndex?.type === 'const' ? pin.vectorIndex.value : '');
  }, [pin.name, pin.binding, pin.vectorIndex, isEditing]);

  const colorClass = TYPE_COLORS[pin.valueType] || 'text-gray-400';
  const bindingType = pin.binding.type;  // 'const' | 'pointer'
  const isEnabled = pin.enableType !== 'disable';
  const canToggleEnable = pin.enableType !== 'fixed';

  // 判断是否为数据连接状态（pointer + 空变量名）
  const isDataConnection = bindingType === 'pointer' &&
    (pin.binding.type === 'pointer' && !pin.binding.variableName);

  const selectedVarName = pin.binding.type === 'pointer' ? pin.binding.variableName : '';
  const selectedVar = selectedVarName
    ? [...sharedVars, ...localVars].find(v => v.name === selectedVarName)
    : null;

  const needsVectorIndex = selectedVar && selectedVar.countType === 'list' && pin.countType === 'scalar';
  const hasVectorIndex = pin.vectorIndex !== undefined;

  const handleBindingToggle = () => {
    if (bindingType === 'pointer' && hasDataConnection && isDataConnection && dataConnection) {
      removeDataConnection(dataConnection.id);
    }

    if (bindingType === 'const') {
      const compatibleVars = getCompatibleVariables();
      const firstVar = compatibleVars[0];

      if (firstVar) {
        onUpdate({
          binding: { type: 'pointer', variableName: firstVar.name, isLocal: !sharedVars.includes(firstVar) },
          bindingType: 'pointer',
          vectorIndex: undefined
        });
      } else {
        onUpdate({
          binding: { type: 'pointer', variableName: '', isLocal: false },
          bindingType: 'pointer',
          vectorIndex: undefined
        });
      }
    } else {
      onUpdate({ binding: { type: 'const', value: getDefaultValue(pin.valueType, pin.countType === 'list') }, bindingType: 'const', vectorIndex: undefined });
    }
  };

  const handleValueChange = (value: string) => {
    setEditValue(value);
    if (bindingType === 'const') {
      onUpdate({ binding: { type: 'const', value } });
    }
  };

  const handleValueSubmit = (manualValue?: string) => {
    const valueToSubmit = manualValue !== undefined ? manualValue : editValue;
    if (bindingType === 'const') {
      const result = validateValue(valueToSubmit, pin.valueType, pin.countType);
      if (!result.isValid) {
        const nodeLabel = nodeType ? `${nodeType}:${nodeUid ?? '?'}` : '';
        const prefix = nodeLabel ? `Node [${nodeLabel}] ` : '';
        const errorMsg = `${prefix}Pin [${pin.name}] invalid: ${result.error}`;
        notify(result.error || 'Invalid value', 'error');
        logger.error(errorMsg);
      } else if (nodeType === 'SubTree' && pin.name === 'Tree') {
        // 如果是 SubTree 的 Tree 路径更新，自动触发 Reload
        reloadSubTreePins(nodeId);
      }
    }
    setIsEditing(false);
  };

  const currentPinValidation = pin.binding.type === 'const'
    ? validateValue(pin.binding.value, pin.valueType, pin.countType)
    : { isValid: true };

  const handleVariableChange = (varName: string, isLocal: boolean) => {
    if (!varName) {
      onUpdate({
        binding: { type: 'pointer', variableName: '', isLocal: false },
        vectorIndex: undefined
      });
      return;
    }

    if (dataConnection) {
      removeDataConnection(dataConnection.id);
    }

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

  const toggleCountType = () => {
    onUpdate({ countType: pin.countType === 'list' ? 'scalar' : 'list' });
  };

  const handleVectorIndexChange = (value: string) => {
    setVectorIndexValue(value);
    onUpdate({ vectorIndex: { type: 'const', value } });
  };

  const handleVectorIndexTypeToggle = () => {
    if (pin.vectorIndex?.type === 'const') {
      const firstIntVar = intSharedVars[0] || intLocalVars[0];
      if (firstIntVar) {
        onUpdate({ vectorIndex: { type: 'pointer', variableName: firstIntVar.name, isLocal: !intSharedVars.includes(firstIntVar) } });
      }
    } else {
      onUpdate({ vectorIndex: { type: 'const', value: '0' } });
    }
  };

  const handleVectorIndexVarChange = (varName: string, isLocal: boolean) => {
    onUpdate({ vectorIndex: { type: 'pointer', variableName: varName, isLocal } });
  };

  const getCompatibleVariables = () => {
    return [...sharedVars, ...localVars].filter(v => {
      if (v.valueType !== pin.valueType) return false;
      if (pin.countType === 'list') {
        return v.countType === 'list';
      } else {
        return true;
      }
    });
  };

  const compatibleVars = getCompatibleVariables();
  const compatibleShared = compatibleVars.filter(v => sharedVars.includes(v));
  const compatibleLocal = compatibleVars.filter(v => localVars.includes(v));
  const intSharedVars = sharedVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');
  const intLocalVars = localVars.filter(v => v.valueType === 'int' && v.countType === 'scalar');

  const bindingBtnInfo = bindingType === 'const'
    ? { style: 'bg-gray-600 text-gray-300', text: 'C', title: 'Constant → Click to switch to Variable' }
    : { style: 'bg-blue-600 text-white', text: 'V', title: 'Variable → Click to switch to Constant' };

  return (
    <div className={`text-xs bg-gray-800 rounded p-1.5 ${!isEnabled ? 'opacity-50' : ''}`}>
      <div className="flex items-center gap-0.5 mb-1">
        {canToggleEnable && (
          <button
            className={`w-3 h-3 rounded-sm border ${isEnabled ? 'bg-green-600 border-green-500' : 'bg-gray-600 border-gray-500'}`}
            onClick={handleEnableToggle}
            title={isEnabled ? 'Disable' : 'Enable'}
          />
        )}

        <span className="text-white font-medium">{pin.name}</span>
        <span className="text-gray-600 text-[10px]">{pin.isInput ? 'in' : 'out'}</span>

        <AdaptiveSelect
          value={pin.valueType}
          options={pin.allowedValueTypes}
          onChange={(val) => {
            const nextType = val as ValueType;
            onUpdate({
              valueType: nextType,
              binding: pin.binding.type === 'const'
                ? { type: 'const', value: getDefaultValue(nextType, pin.countType === 'list') }
                : pin.binding
            });
          }}
          baseClassName="text-[10px]"
          triggerClassName={colorClass}
          getOptionClass={(opt) => TYPE_COLORS[opt as ValueType] || 'text-gray-300'}
          disabled={pin.allowedValueTypes.length <= 1}
        />

        {!pin.isCountTypeFixed && (
          <button
            className={`text-[10px] hover:bg-gray-700 ${colorClass} text-center px-0.5 rounded transition-colors min-w-[12px]`}
            onClick={toggleCountType}
            title={pin.countType === 'list' ? 'Switch to scalar' : 'Switch to array'}
          >
            {pin.countType === 'list' ? '[]' : '·'}
          </button>
        )}

        {pin.isCountTypeFixed && (
          <span className={`text-[10px] ${colorClass} opacity-50 min-w-3 text-center cursor-default`} title="Fixed count type">
            {pin.countType === 'list' ? '[]' : '·'}
          </span>
        )}

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

      <div className="flex items-center gap-1">
        {isEditing ? (
          <div className="flex-1">
            {nodeType === 'SubTree' && pin.name === 'Tree' ? (
              <TreeFilePicker
                value={editValue}
                onChange={(val) => {
                  handleValueChange(val);
                  handleValueSubmit(val);
                }}
                options={treeFiles}
              />
            ) : pin.valueType === 'enum' && pin.enumValues && pin.enumValues.length > 0 ? (
              <div className="w-full bg-gray-700 rounded px-1 py-0.5">
                <AdaptiveSelect
                  value={editValue || pin.enumValues[0]}
                  options={pin.enumValues}
                  onChange={(val) => { handleValueChange(val); handleValueSubmit(); }}
                  baseClassName="text-xs text-gray-300"
                  triggerClassName="w-full"
                  containerClassName="w-full block"
                  getOptionClass={() => 'text-gray-300'}
                  renderLabel={(val) => val}
                />
              </div>
            ) : (
              <input
                className={`w-full text-xs px-1 py-0.5 rounded outline-none ${!validateValue(editValue, pin.valueType, pin.countType).isValid
                  ? 'bg-red-900 text-white'
                  : 'bg-gray-700 text-gray-300'
                  }`}
                value={editValue}
                onChange={(e) => handleValueChange(e.target.value)}
                onBlur={() => handleValueSubmit()}
                onKeyDown={(e) => e.key === 'Enter' && handleValueSubmit()}
                autoFocus
                placeholder="Value"
              />
            )}
          </div>
        ) : (
          <span
            className={`flex-1 text-xs truncate cursor-pointer hover:text-gray-300 transition-opacity ${!currentPinValidation.isValid ? 'bg-red-900 text-white px-1 rounded' : 'text-gray-400'
              }`}
            onClick={() => { setEditValue(pin.binding.type === 'const' ? pin.binding.value : ''); setIsEditing(true); }}
          >
            {pin.binding.type === 'const' && (
              <div className="flex items-center gap-1 group/value">
                <span className="truncate">
                  {(() => {
                    const b = pin.binding as { type: 'const'; value: string };
                    return nodeType === 'SubTree' && pin.name === 'Tree'
                      ? (b.value ? b.value.replace(/\.tree$/, '') : '(none)')
                      : (b.value || (pin.valueType === 'string' ? '""' : '0'));
                  })()}
                </span>
                {nodeType === 'SubTree' && pin.name === 'Tree' && pin.binding.value && (
                  <button
                    className="opacity-0 group-hover/value:opacity-100 text-gray-500 hover:text-blue-400 transition-opacity"
                    onClick={(e) => {
                      e.stopPropagation();
                      const b = pin.binding as { type: 'const'; value: string };
                      const path = b.value.endsWith('.tree') ? b.value : `${b.value}.tree`;
                      useEditorStore.getState().openTree(path);
                    }}
                    title="Open this tree"
                  >
                    ↗
                  </button>
                )}
              </div>
            )}
          </span>
        )}
      </div>

      {bindingType === 'pointer' && (
        <div className="space-y-1 mt-1">
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
                  <option key={`l-${v.name}`} value={`local:${v.name}`}>{v.name}'{v.countType === 'list' ? '[]' : ''}</option>
                ))}
              </optgroup>
            )}
          </select>

          {isDataConnection && (
            <div className={`px-1 py-0.5 text-[10px] ${hasDataConnection ? 'text-purple-400' : 'text-gray-500 italic'}`}>
              {hasDataConnection ? '● Connected' : '○ Waiting for connection...'}
            </div>
          )}
        </div>
      )}

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
});
