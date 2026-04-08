import { useState, useEffect, useRef, memo, useMemo, useCallback } from 'react';
import { useEditorStore } from '../stores/editorStore';
import { useEditorMetaStore } from '../stores/editorMetaStore';
import type { Variable, ValueType, CountType, Pin } from '../types';
import { validateValue, getDefaultValue, validateVariableName } from '../utils/validation';
import { useNotificationStore } from '../stores/notificationStore';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { useTooltipStore } from '../stores/tooltipStore';
import { logger } from '../utils/logger';
import { useDebugStore } from '../stores/debugStore';
import { useShallow } from 'zustand/react/shallow';
import { getTheme } from '../theme/theme';
import { stripExtension } from '../utils/fileUtils';
import { TreeFilePicker } from './TreeFilePicker';

const theme = getTheme();

const TYPE_COLORS: Record<ValueType, string> = (() => {
  const tc = theme.text.typeColors;
  return {
    int: tc.int || theme.ui.accent,
    float: tc.float || theme.ui.success,
    bool: tc.bool || theme.ui.danger,
    string: tc.string || theme.ui.warning,
    vector3: tc.vector3 || theme.ui.warning,
    entity: tc.entity || theme.ui.accent,
    ulong: tc.ulong || theme.ui.textDim,
    enum: tc.enum || theme.ui.warning,
  };
})();

function resolveTypeColor(valueType: string): string {
  const normalized = valueType.toLowerCase();

  if (normalized.includes('enum')) return TYPE_COLORS.enum;
  if (normalized.includes('int')) return TYPE_COLORS.int;
  if (normalized.includes('float')) return TYPE_COLORS.float;
  if (normalized.includes('bool')) return TYPE_COLORS.bool;
  if (normalized.includes('string')) return TYPE_COLORS.string;
  if (normalized.includes('vector3')) return TYPE_COLORS.vector3;
  if (normalized.includes('entity')) return TYPE_COLORS.entity;
  if (normalized.includes('ulong')) return TYPE_COLORS.ulong;

  return TYPE_COLORS[normalized as ValueType] || TYPE_COLORS[valueType as ValueType] || theme.ui.textDim;
}

const VALUE_TYPES: ValueType[] = ['int', 'float', 'bool', 'string', 'vector3', 'entity', 'ulong'];

interface VariableItemProps {
  variable: Variable;
  onUpdate: (name: string, updates: Partial<Variable>) => void;
  onDelete: (name: string) => void;
  onToggleScope: (name: string) => void;
  siblingNames: string[]; // names of other variables in the same scope, for duplicate check
  debugValue?: string;
  isChanged?: boolean;
  isPaused?: boolean;
}

// 宽度自适应的自定义下拉选择组件 (用于类型选择)
function AdaptiveSelect({ value, options, onChange, renderLabel, getOptionColor, baseClassName, triggerColor, containerClassName, disabled }: {
  value: string;
  options: string[];
  onChange: (val: string) => void;
  renderLabel?: (val: string) => React.ReactNode;
  getOptionColor?: (val: string) => string; // hex color
  baseClassName?: string;
  triggerColor?: string; // hex color for the trigger text
  containerClassName?: string;
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
      <span className={`${baseClassName} opacity-100 font-semibold cursor-default px-0.5`} style={{ color: triggerColor }}>
        {displayValue}
      </span>
    );
  }

  return (
    <div className={`relative inline-block align-middle ${containerClassName || ''}`} ref={containerRef}>
      {/* Trigger */}
      <span
        className={`${baseClassName} cursor-pointer px-0.5 rounded transition-colors select-none whitespace-nowrap inline-block`}
        style={{ color: triggerColor }}
        onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
        onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
        onClick={() => setIsOpen(!isOpen)}
      >
        {displayValue}
      </span>

      {/* Custom Dropdown Menu */}
      {isOpen && (
        <div className="absolute top-full left-0 mt-1 z-50 rounded shadow-xl py-1 min-w-[80px] max-h-60 overflow-y-auto"
          style={{ backgroundColor: theme.ui.inputBg, border: `1px solid ${theme.ui.border}` }}>
          {options.map(opt => {
            const optColor = getOptionColor ? getOptionColor(opt) : theme.ui.textMain;
            const optLabel = renderLabel ? renderLabel(opt) : (opt.charAt(0).toUpperCase() + opt.slice(1));
            return (
              <div
                key={opt}
                className={`${baseClassName} px-2 py-1.5 cursor-pointer whitespace-nowrap flex items-center`}
                style={{ color: optColor, backgroundColor: opt === value ? theme.ui.accentSoft : 'transparent' }}
                onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
                onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = opt === value ? theme.ui.accentSoft : 'transparent'; }}
                onClick={() => {
                  onChange(opt);
                  setIsOpen(false);
                }}
              >
                {/* 选中标记 */}
                <span className={`w-3 mr-1 ${opt === value ? 'opacity-100' : 'opacity-0'}`} style={{ color: theme.ui.textMain }}>✓</span>
                {optLabel}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

// Action modes for VariableItem
type VarAction = 'none' | 'renaming' | 'toggling' | 'deleting';

function VariableItem({ variable, onUpdate, onDelete, onToggleScope, siblingNames, debugValue, isChanged, isPaused }: VariableItemProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState(variable.defaultValue);
  const colorClass = resolveTypeColor(variable.valueType);
  const isArray = variable.countType === 'list';
  const notify = useNotificationStore(state => state.notify);

  // Unified action state
  const [action, setAction] = useState<VarAction>('none');
  const [renameName, setRenameName] = useState('');

  const handleValueSubmit = () => {
    if (editValue !== variable.defaultValue) {
      const result = validateValue(editValue, variable.valueType, variable.countType);
      if (!result.isValid) {
        const errorMsg = `Variable [${variable.name}] invalid: ${result.error}`;
        notify(result.error || 'Invalid value', 'error');
        logger.error(errorMsg);
      } else {
        onUpdate(variable.name, { defaultValue: editValue });
      }
    }
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

  // Click-to-Copy
  const handleCopyName = () => {
    navigator.clipboard.writeText(variable.name).then(() => {
      notify(`Copied "${variable.name}"`, 'info');
    }).catch(() => {
      notify('Copy failed', 'error');
    });
  };

  // Rename handlers
  const handleStartRename = () => {
    setRenameName(variable.name);
    setAction('renaming');
  };
  const handleConfirmRename = () => {
    const trimmed = renameName.trim();
    if (!trimmed || trimmed === variable.name) {
      setAction('none');
      return;
    }
    const nameCheck = validateVariableName(trimmed);
    if (!nameCheck.isValid) {
      notify(nameCheck.error || 'Invalid variable name', 'error');
      return;
    }
    if (siblingNames.includes(trimmed)) {
      notify(`Variable "${trimmed}" already exists`, 'error');
      return;
    }
    onUpdate(variable.name, { name: trimmed });
    notify(`Renamed "${variable.name}" → "${trimmed}"`, 'info');
    setAction('none');
  };

  const focusTarget = useEditorMetaStore(state => state.uiMeta.focusTarget);
  const isFocused = focusTarget?.type === 'variable' && focusTarget.id === variable.name;

  // Render action buttons area
  const renderActionButtons = () => {
    if (action === 'renaming') return null; // name area is replaced by input
    if (action === 'toggling') {
      return (
        <div className="absolute right-1 top-1/2 -translate-y-1/2 flex items-center rounded px-1 gap-1 z-10 shadow-md border" style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}>
          <span className="text-[10px] font-bold" style={{ color: theme.ui.warning }}>Move?</span>
          <button
            onClick={() => { onToggleScope(variable.name); setAction('none'); }}
            className="px-1 py-0.5 text-[10px]"
            style={{ color: theme.ui.success }}
          >✓</button>
          <button
            onClick={() => setAction('none')}
            className="px-1 py-0.5 text-[10px]"
            style={{ color: theme.ui.textDim }}
          >✕</button>
        </div>
      );
    }
    if (action === 'deleting') {
      return (
        <div className="absolute right-1 top-1/2 -translate-y-1/2 flex items-center rounded px-1 gap-1 z-10 shadow-md border" style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}>
          <span className="text-[10px] font-bold" style={{ color: theme.ui.danger }}>Del?</span>
          <button
            onClick={() => onDelete(variable.name)}
            className="px-1 py-0.5 text-[10px]"
            style={{ color: theme.ui.success }}
          >✓</button>
          <button
            onClick={() => setAction('none')}
            className="px-1 py-0.5 text-[10px]"
            style={{ color: theme.ui.textDim }}
          >✕</button>
        </div>
      );
    }
    // Default: show action buttons on hover
    return (
      <div
        className="absolute left-full top-1/2 -translate-y-1/2 ml-1 flex items-center shadow-md border rounded px-1 py-0.5 opacity-0 group-hover/name:opacity-100 transition-opacity z-10 pointer-events-none group-hover/name:pointer-events-auto before:content-[''] before:absolute before:right-full before:top-0 before:bottom-0 before:w-2"
        style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}
      >
        <button
          className="text-[10px] px-1 transition-colors"
          style={{ color: theme.ui.textDim }}
          onClick={(e) => { e.stopPropagation(); handleStartRename(); }}
        >✎</button>
        <button
          className="text-[10px] px-1 transition-colors border-l ml-1 pl-1"
          style={{ color: theme.ui.textDim, borderColor: theme.ui.border }}
          onClick={(e) => { e.stopPropagation(); setAction('toggling'); }}
        >{variable.isLocal ? '↑' : '↓'}</button>
        <button
          className="text-[10px] px-1 transition-colors border-l ml-1 pl-1"
          style={{ color: theme.ui.textDim, borderColor: theme.ui.border }}
          onClick={(e) => { e.stopPropagation(); setAction('deleting'); }}
        >✕</button>
      </div>
    );
  };

  return (
    <div
      id={`variable-${variable.name}`}
      className={`px-2 py-0.5 text-sm rounded group transition-all duration-300 relative ${isFocused ? 'animate-pulse-subtle' : ''}`}
      style={{
        backgroundColor: theme.ui.panelBg,
        boxShadow: isFocused ? `0 0 0 2px ${theme.ui.border}` : undefined
      }}
    >
      {/* Change Highlight Overlay */}
      {isChanged && (
        <div
          key={isPaused ? 'paused' : debugValue} // Restart animation on value change
          className={`absolute inset-0 rounded pointer-events-none ${!isPaused ? 'animate-debug-flash' : ''}`}
          style={{ backgroundColor: `${theme.ui.success}33` }}
        />
      )}
      <div className="flex items-center gap-1 min-w-0">
        <AdaptiveSelect
          value={variable.valueType}
          options={VALUE_TYPES}
          onChange={(val) => handleTypeChange(val as ValueType)}
          baseClassName="text-xs"
          triggerColor={colorClass}
          getOptionColor={(opt: string) => resolveTypeColor(opt)}
        />

        <button
          className="text-xs text-center px-0.5 rounded transition-colors min-w-[14px] flex-shrink-0"
          style={{ color: colorClass, backgroundColor: 'transparent' }}
          onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
          onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
          onClick={handleCountTypeToggle}
        >
          {isArray ? '[]' : '·'}
        </button>

        {action === 'renaming' ? (
          <div className="flex items-center flex-1 min-w-0 gap-1">
            <input
              className="flex-1 min-w-0 text-xs px-1 py-0.5 rounded outline-none border"
              style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
              value={renameName}
              onChange={(e) => setRenameName(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') handleConfirmRename(); if (e.key === 'Escape') setAction('none'); }}
              autoFocus
            />
            <button onClick={handleConfirmRename} className="text-[10px] px-0.5 flex-shrink-0" style={{ color: theme.ui.success }}>✓</button>
            <button onClick={() => setAction('none')} className="text-[10px] px-0.5 flex-shrink-0" style={{ color: theme.ui.textDim }}>✕</button>
          </div>
        ) : (
          <div className="flex-1 min-w-0 flex items-center gap-1">
            <div className="relative flex items-center group/name min-w-[60px] max-w-full">
              <span
                className="truncate cursor-pointer w-full block"
                style={{ color: theme.ui.textMain }}
                onClick={handleCopyName}
              >{variable.name}{variable.isLocal ? "'" : ""}</span>
              {renderActionButtons()}
            </div>

            <span className="text-xs shrink-0" style={{ color: theme.ui.textDim }}>=</span>

            <div className="flex-1 min-w-0">
              {variable.valueType === 'bool' && variable.countType !== 'list' ? (
                <button
                  className="px-2 py-0.5 rounded text-xs font-bold transition-colors"
                  style={{
                    backgroundColor: variable.defaultValue === 'T' ? theme.ui.success : theme.ui.danger,
                    color: theme.ui.terminalButtonText,
                    border: variable.defaultValue === 'T' ? 'none' : `1px solid ${theme.ui.danger}`,
                  }}
                  onClick={() => onUpdate(variable.name, { defaultValue: variable.defaultValue === 'T' ? 'F' : 'T' })}
                >
                  {variable.defaultValue === 'T' ? 'TRUE' : 'FALSE'}
                </button>
              ) : variable.valueType === 'entity' && variable.countType !== 'list' ? (
                <span className="block w-full text-xs truncate italic cursor-not-allowed" style={{ color: theme.ui.textDim }}>
                  (Entity: Read-only)
                </span>
              ) : isEditing ? (
                <input
                  className="w-full text-xs px-1 py-0.5 rounded border outline-none"
                  style={validateValue(editValue, variable.valueType, variable.countType).isValid
                    ? { backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }
                    : { backgroundColor: theme.ui.danger, color: theme.ui.terminalButtonText, borderColor: theme.ui.danger }}
                  value={editValue}
                  onChange={(e) => setEditValue(e.target.value)}
                  onBlur={handleValueSubmit}
                  onKeyDown={(e) => e.key === 'Enter' && handleValueSubmit()}
                  autoFocus
                />
              ) : debugValue !== undefined ? (
                <span
                  className="block w-full text-xs truncate font-mono"
                  style={{ color: theme.ui.success }}
                >
                  {debugValue}
                </span>
              ) : (
                <span
                  className="block w-full text-xs truncate cursor-pointer px-1 py-0.5 rounded border"
                  style={isValid
                    ? { backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }
                    : { backgroundColor: theme.ui.danger, color: theme.ui.terminalButtonText, borderColor: theme.ui.danger }}
                  onClick={() => { setEditValue(variable.defaultValue); setIsEditing(true); }}
                >
                  {variable.defaultValue || (variable.valueType === 'string' ? '""' : '(empty)')}
                </span>
              )}
            </div>
          </div>
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
      <div className="flex items-center gap-1 rounded px-1 py-0.5 border" style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}>
        <input
          className="w-24 text-[10px] bg-transparent outline-none"
          style={{ color: !validation.isValid && name.length > 0 ? theme.ui.danger : theme.ui.textMain }}
          placeholder="Name..."
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
          onBlur={() => !name && setIsAdding(false)}
          autoFocus
        />
        <button className="text-[10px] font-bold" style={{ color: theme.ui.textMain }} onClick={handleAdd}>OK</button>
        <button className="text-[10px]" style={{ color: theme.ui.textDim }} onClick={() => setIsAdding(false)}>✕</button>
      </div>
    );
  }

  return (
    <button
      className="transition-colors flex items-center gap-0.5"
      style={{ color: theme.ui.textDim }}
      onClick={() => setIsAdding(true)}
    >
      <span className="text-xs">+</span>
      <span className="text-[10px] uppercase font-bold tracking-tighter">Add</span>
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
  const colorClass = resolveTypeColor(pin.valueType);
  const isArray = pin.countType === 'list';
  const [isDeleting, setIsDeleting] = useState(false);

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

  const focusTarget = useEditorMetaStore(state => state.uiMeta.focusTarget);
  const isFocused = focusTarget?.type === 'io' && focusTarget.id === pin.id;

  const compatibleShared = sharedVars.filter(v => v.valueType === pin.valueType && (pin.countType === 'scalar' || v.countType === 'list'));
  const compatibleLocal = localVars.filter(v => v.valueType === pin.valueType && (pin.countType === 'scalar' || v.countType === 'list'));

  return (
    <div
      id={`io-${pin.id}`}
      className={`px-2 py-0.5 text-sm rounded group transition-all duration-300 ${isFocused ? 'animate-pulse-subtle' : ''}`}
      style={{
        backgroundColor: theme.ui.panelBg,
        boxShadow: isFocused ? `0 0 0 2px ${theme.ui.border}` : undefined
      }}
    >
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
          triggerColor={colorClass}
          getOptionColor={(opt: string) => resolveTypeColor(opt)}
        />

        <button
          className="text-xs text-center px-0.5 rounded transition-colors min-w-[14px]"
          style={{ color: colorClass, backgroundColor: 'transparent' }}
          onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
          onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
          onClick={() => onUpdate(pin.id, { countType: isArray ? 'scalar' : 'list' })}
        >
          {isArray ? '[]' : '·'}
        </button>

        <span className="flex-1 truncate font-medium" style={{ color: theme.ui.textMain }}>{pin.name}</span>

        {isDeleting ? (
          <div className="flex items-center rounded px-1 gap-1" style={{ backgroundColor: theme.ui.background }}>
            <span className="text-[10px] font-bold" style={{ color: theme.ui.danger }}>Del?</span>
            <button
              onClick={() => onDelete(pin.id)}
              className="px-1 py-0.5 text-[10px]"
              style={{ color: theme.ui.success }}
            >
              ✓
            </button>
            <button
              onClick={() => setIsDeleting(false)}
              className="px-1 py-0.5 text-[10px]"
              style={{ color: theme.ui.textDim }}
            >
              ✕
            </button>
          </div>
        ) : (
          <button
            className="opacity-0 group-hover:opacity-100 text-xs transition-opacity"
            style={{ color: theme.ui.textDim }}
            onClick={() => setIsDeleting(true)}
          >
            ✕
          </button>
        )}
      </div>

      <div className="flex flex-col gap-1 mt-1 pb-0.5">
        <div className="flex items-center gap-1">
          <span className="text-[10px] w-12" style={{ color: theme.ui.textDim }}>{isInput ? 'Inner Var:' : 'Source:'}</span>

          {isInput || pin.binding.type === 'variable' ? (
            <select
              className="flex-1 text-[10px] px-1 py-0.5 rounded outline-none border"
              style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
              value={pin.binding.value ? `${pin.binding.isLocal ? 'local' : 'shared'}:${pin.binding.value}` : ''}
              onChange={(e) => {
                const [scope, name] = e.target.value.split(':');
                handleBindingChange(name, 'variable', scope === 'local');
              }}
            >
              <option value="">(None)</option>
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
          ) : (
            <input
              className="flex-1 text-[10px] px-1 py-0.5 rounded outline-none border"
              style={{
                backgroundColor: theme.ui.inputBg,
                color: theme.ui.textMain,
                borderColor: validateValue(pin.binding.value, pin.valueType, pin.countType).isValid ? theme.ui.border : theme.ui.danger
              }}
              value={pin.binding.value}
              onChange={(e) => handleBindingChange(e.target.value, 'const')}
              placeholder="Constant value"
            />
          )}

          {!isInput && (
            <button
              className="text-[10px] px-1 py-0.5 rounded leading-tight transition-colors"
              style={{
                backgroundColor: pin.binding.type === 'const' ? theme.ui.buttonBg : theme.ui.buttonHoverBg,
                color: pin.binding.type === 'const' ? theme.ui.textMain : theme.ui.tabActiveText
              }}
              onClick={() => handleBindingChange('', pin.binding.type === 'const' ? 'variable' : 'const', false)}
            >
              {pin.binding.type === 'const' ? 'C' : 'V'}
            </button>
          )}
        </div>

        {(needsVectorIndex || hasVectorIndex) && (
          <div className="flex items-center gap-1 pl-12">
            <span className="text-[10px]" style={{ color: theme.ui.textDim }}>Index:</span>
            <button
              className="text-[10px] px-1 rounded"
              style={{
                backgroundColor: pin.vectorIndex?.type === 'const' ? theme.ui.buttonBg : theme.ui.buttonHoverBg,
                color: pin.vectorIndex?.type === 'const' ? theme.ui.textMain : theme.ui.tabActiveText
              }}
              onClick={handleVectorIndexTypeToggle}
            >
              {pin.vectorIndex?.type === 'const' ? 'C' : 'V'}
            </button>
            {pin.vectorIndex?.type === 'const' ? (
              <input
                className="flex-1 text-[10px] px-1 py-0.5 rounded outline-none border"
                style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
                value={pin.vectorIndex.value}
                onChange={(e) => onUpdate(pin.id, { vectorIndex: { type: 'const', value: e.target.value } })}
              />
            ) : (
              <select
                className="flex-1 text-[10px] px-1 py-0.5 rounded outline-none border"
                style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
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
      <div className="flex items-center gap-1 mt-2 p-1.5 rounded border" style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}>
        <input
          className="flex-1 text-xs px-2 py-1 rounded outline-none border"
          style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
          placeholder="Pin name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
          autoFocus
        />
        <button className="px-1 py-1" style={{ color: theme.ui.success }} onClick={handleAdd}>✓</button>
        <button className="px-1 py-1" style={{ color: theme.ui.textDim }} onClick={() => setIsAdding(false)}>✕</button>
      </div>
    );
  }

  return (
    <button
      className="text-[11px] mt-2 flex items-center gap-1 transition-colors px-1"
      style={{ color: theme.ui.textDim }}
      onClick={() => setIsAdding(true)}
    >
      <span className="text-base leading-none underline-none">+</span>
      <span>Add {isInput ? 'Input' : 'Output'}</span>
    </button>
  );
}

export function PropertiesPanel() {
  const activeTab = useEditorMetaStore(state => state.uiMeta.activePropertiesTab);
  const setActiveTab = useEditorMetaStore(state => state.setActivePropertiesTab);
  const focusTarget = useEditorMetaStore(state => state.uiMeta.focusTarget);
  const setFocusTarget = useEditorMetaStore(state => state.setFocusTarget);

  const currentTree = useEditorStore((state) => state.getCurrentTree());
  const selectedNodeIds = useEditorStore((state) => state.selectedNodeIds);
  const updateVariable = useEditorStore((state) => state.updateVariable);
  const removeVariable = useEditorStore((state) => state.removeVariable);
  const addVariable = useEditorStore((state) => state.addVariable);

  const addTreeInterfacePin = useEditorStore((state) => state.addTreeInterfacePin);
  const updateTreeInterfacePin = useEditorStore((state) => state.updateTreeInterfacePin);
  const removeTreeInterfacePin = useEditorStore((state) => state.removeTreeInterfacePin);
  const toggleVariableScope = useEditorStore((state) => state.toggleVariableScope);

  const sharedVariables = currentTree?.sharedVariables || [];
  const localVariables = currentTree?.localVariables || [];
  const inputs = currentTree?.inputs || [];
  const outputs = currentTree?.outputs || [];

  // Debug state
  const activeFilePath = useEditorStore(s => s.activeFilePath);
  const fileName = useMemo(() => activeFilePath?.split(/[\\/]/).pop()?.replace(/\.(tree|fsm)$/, '') || '', [activeFilePath]);

  const { isConnected, treeRunInfos, fsmRunInfo, currentKeyframe, isPaused } = useDebugStore(
    useShallow(s => ({
      isConnected: s.isConnected,
      treeRunInfos: s.treeRunInfos,
      fsmRunInfo: s.fsmRunInfo,
      currentKeyframe: s.keyframe,
      isPaused: s.isPaused
    }))
  );

  const debugValues = useMemo(() => {
    if (!isConnected || !fileName) return null;
    const treeInfo = treeRunInfos.get(fileName);
    if (treeInfo) {
      return {
        shared: treeInfo.sharedVariables,
        local: treeInfo.localVariables,
        sharedTs: treeInfo.sharedVariableTimestamps,
        localTs: treeInfo.localVariableTimestamps
      };
    }
    // FSM support placeholder
    return null;
  }, [isConnected, fileName, treeRunInfos, fsmRunInfo]);

  const notify = useNotificationStore(state => state.notify);

  const handleUpdateShared = (name: string, updates: Partial<Variable>) => updateVariable(false, name, updates);
  const handleDeleteShared = (name: string) => removeVariable(false, name);

  const handleUpdateLocal = (name: string, updates: Partial<Variable>) => updateVariable(true, name, updates);

  const handleDeleteLocal = (name: string) => removeVariable(true, name);

  const handleToggleScope = (name: string, isLocal: boolean) => {
    const error = toggleVariableScope(name, isLocal);
    if (error) {
      notify(error, 'error');
    } else {
      notify(`Moved "${name}" to ${isLocal ? 'Shared' : 'Local'}`, 'info');
    }
  };

  const handleAddShared = (v: Variable) => addVariable(false, v);
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

  const handleDeleteInterfacePin = (isInput: boolean, id: string) => removeTreeInterfacePin(isInput, id);

  // Ensure active properties tab is valid (not 'properties' anymore in top section)
  useEffect(() => {
    if (activeTab === 'properties') {
      setActiveTab('variables');
    }
  }, [activeTab]);

  // 处理聚焦跳转
  useEffect(() => {
    if (focusTarget) {
      if (focusTarget.type === 'variable') setActiveTab('variables');
      if (focusTarget.type === 'io') setActiveTab('io');

      const elementId = focusTarget.type === 'variable' ? `variable-${focusTarget.id}` :
        focusTarget.type === 'io' ? `io-${focusTarget.id}` :
          null;

      if (elementId) {
        setTimeout(() => {
          const el = document.getElementById(elementId);
          if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            setTimeout(() => setFocusTarget(undefined), 2000);
          }
        }, 100);
      }
    }
  }, [focusTarget, setFocusTarget]);

  return (
    <div className="h-full border-l flex flex-col" style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}>
      {/* Top Section: Data (Vars/IO) */}
      <div className="flex-1 flex flex-col min-h-0 border-b" style={{ borderColor: theme.ui.border }}>
        <div className="flex shrink-0" style={{ backgroundColor: theme.ui.tabBarBg }}>
          <button
            className="flex-1 py-1.5 text-[9px] uppercase tracking-wider font-bold transition-all"
            style={{
              color: activeTab === 'variables' ? theme.ui.tabActiveText : theme.ui.tabInactiveText,
              backgroundColor: activeTab === 'variables' ? theme.ui.panelBg : theme.ui.tabInactiveBg,
              border: 'none'
            }}
            onClick={() => setActiveTab('variables')}
          >
            Variables
          </button>
          <button
            className="flex-1 py-1.5 text-[9px] uppercase tracking-wider font-bold transition-all"
            style={{
              color: activeTab === 'io' ? theme.ui.tabActiveText : theme.ui.tabInactiveText,
              backgroundColor: activeTab === 'io' ? theme.ui.panelBg : theme.ui.tabInactiveBg,
              border: 'none'
            }}
            onClick={() => setActiveTab('io')}
          >
            Interface I/O
          </button>
        </div>

        <div className="flex-1 overflow-y-auto overflow-x-hidden p-2 scrollbar-thin">
          {activeTab === 'variables' ? (
            <>
              {/* Shared Variables */}
              <div className="mb-4">
                <div className="flex items-center justify-between mb-1 px-1">
                  <div className="text-[10px] font-bold uppercase tracking-tight" style={{ color: theme.ui.textDim }}>Shared</div>
                  <AddVariableButton isLocal={false} onAdd={handleAddShared} />
                </div>
                <div className="flex flex-col">
                  {sharedVariables.map((v, index) => (
                    <div key={v.name}>
                      {index > 0 && <div className="h-px" style={{ backgroundColor: theme.ui.border, opacity: 0.45 }} />}
                      <VariableItem
                        variable={v}
                        onUpdate={handleUpdateShared}
                        onDelete={handleDeleteShared}
                        onToggleScope={(name) => handleToggleScope(name, false)}
                        siblingNames={sharedVariables.filter(sv => sv.name !== v.name).map(sv => sv.name)}
                        debugValue={debugValues?.shared?.get(v.name)}
                        isChanged={debugValues?.sharedTs?.get(v.name) === currentKeyframe}
                        isPaused={isPaused}
                      />
                    </div>
                  ))}
                </div>
              </div>

              {/* Local Variables */}
              <div>
                <div className="flex items-center justify-between mb-1 px-1">
                  <div className="text-[10px] font-bold uppercase tracking-tight" style={{ color: theme.ui.textDim }}>Local</div>
                  <AddVariableButton isLocal={true} onAdd={handleAddLocal} />
                </div>
                <div className="flex flex-col">
                  {localVariables.map((v, index) => (
                    <div key={v.name}>
                      {index > 0 && <div className="h-px" style={{ backgroundColor: theme.ui.border, opacity: 0.45 }} />}
                      <VariableItem
                        variable={v}
                        onUpdate={handleUpdateLocal}
                        onDelete={handleDeleteLocal}
                        onToggleScope={(name) => handleToggleScope(name, true)}
                        siblingNames={localVariables.filter(lv => lv.name !== v.name).map(lv => lv.name)}
                        debugValue={debugValues?.local?.get(v.name)}
                        isChanged={debugValues?.localTs?.get(v.name) === currentKeyframe}
                        isPaused={isPaused}
                      />
                    </div>
                  ))}
                </div>
              </div>
            </>
          ) : (
            <>
              {/* Inputs */}
              <div className="mb-4">
                <div className="flex items-center justify-between mb-1 px-1">
                  <div className="text-[10px] font-bold uppercase tracking-tight" style={{ color: theme.ui.textDim }}>Inputs</div>
                  <AddInterfacePinButton isInput={true} onAdd={(name) => handleAddInterfacePin(true, name)} />
                </div>
                <div className="flex flex-col">
                  {inputs.map((pin, index) => (
                    <div key={pin.id}>
                      {index > 0 && <div className="h-px" style={{ backgroundColor: theme.ui.border, opacity: 0.45 }} />}
                      <InterfacePinItem
                        pin={pin}
                        isInput={true}
                        onUpdate={(id, updates) => updateTreeInterfacePin(true, id, updates)}
                        onDelete={(id) => handleDeleteInterfacePin(true, id)}
                        sharedVars={sharedVariables}
                        localVars={localVariables}
                      />
                    </div>
                  ))}
                </div>
              </div>

              {/* Outputs */}
              <div>
                <div className="flex items-center justify-between mb-1 px-1">
                  <div className="text-[10px] font-bold uppercase tracking-tight" style={{ color: theme.ui.textDim }}>Outputs</div>
                  <AddInterfacePinButton isInput={false} onAdd={(name) => handleAddInterfacePin(false, name)} />
                </div>
                <div className="flex flex-col">
                  {outputs.map((pin, index) => (
                    <div key={pin.id}>
                      {index > 0 && <div className="h-px" style={{ backgroundColor: theme.ui.border, opacity: 0.45 }} />}
                      <InterfacePinItem
                        pin={pin}
                        isInput={false}
                        onUpdate={(id, updates) => updateTreeInterfacePin(false, id, updates)}
                        onDelete={(id) => handleDeleteInterfacePin(false, id)}
                        sharedVars={sharedVariables}
                        localVars={localVariables}
                      />
                    </div>
                  ))}
                </div>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Bottom Section: Node Properties */}
      <div className="flex-1 flex flex-col min-h-0">
        <div className="px-3 py-1.5 text-[9px] uppercase tracking-widest font-black border-b shrink-0" style={{ backgroundColor: theme.ui.panelBg, color: theme.ui.textDim, borderColor: theme.ui.border }}>
          Node Properties
        </div>
        <div className="flex-1 overflow-y-auto overflow-x-hidden p-2 scrollbar-thin">
          {selectedNodeIds.length === 0 ? (
            <div className="h-full flex flex-col items-center justify-center text-[10px] text-center px-4 space-y-2" style={{ color: theme.ui.textDim }}>
              <span className="text-2xl opacity-20">🖱️</span>
              <span>Select a node to edit properties</span>
            </div>
          ) : selectedNodeIds.length === 1 ? (
            <NodePropertiesEditor nodeId={selectedNodeIds[0]} />
          ) : (
            <div className="h-full flex flex-col items-center justify-center text-[10px] text-center px-4 space-y-2" style={{ color: theme.ui.textDim }}>
              <span className="text-2xl opacity-20">🔲</span>
              <span>{selectedNodeIds.length} nodes selected</span>
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
  const { getDefinition } = useNodeDefinitionStore();
  const setTooltip = useTooltipStore(state => state.setTooltip);
  const node = useMemo(() => currentTree?.nodes.get(nodeId), [currentTree, nodeId]);
  const definition = useMemo(() => node ? getDefinition(node.type) : undefined, [node, getDefinition]);

  const handleReloadSubTree = () => {
    reloadSubTreePins(nodeId);
  };

  const sharedVars = currentTree?.sharedVariables || [];
  const localVars = currentTree?.localVariables || [];
  const dataConnections = currentTree?.dataConnections || [];

  const safePins = node?.pins || [];

  // 分组 Pin：输入和输出
  const inputPins = useMemo(() => safePins.filter(pin => pin.isInput), [safePins]);
  const outputPins = useMemo(() => safePins.filter(pin => !pin.isInput), [safePins]);

  // 处理 Pin 更新 (内含类型/数量联动及 TypeMap)
  const handlePinUpdate = useCallback((pinName: string, updates: Partial<Pin>) => {
    updatePin(nodeId, pinName, updates);
  }, [updatePin, nodeId]);

  if (!node) return <div className="text-xs" style={{ color: theme.ui.textDim }}>Node not found</div>;

  const renderPinList = (pins: Pin[]) => {
    if (pins.length === 0) return null;

    return (
      <div className="mb-1">
        <div className="flex flex-col">
          {pins.map((pin, index) => {
            const dataConn = dataConnections.find(
              dc => (dc.toNodeId === nodeId && dc.toPinName === pin.name) ||
                (dc.fromNodeId === nodeId && dc.fromPinName === pin.name)
            );
            return (
              <div key={pin.name}>
                {index > 0 && <div className="h-px" style={{ backgroundColor: theme.ui.border, opacity: 0.45 }} />}
                <PinEditor
                  pin={pin}
                  nodeId={nodeId}
                  nodeUid={node.uid}
                  nodeType={node.type}
                  sharedVars={sharedVars}
                  localVars={localVars}
                  dataConnection={dataConn}
                  onUpdate={(updates) => handlePinUpdate(pin.name, updates)}
                />
              </div>
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
        <div
          className="text-sm font-medium"
          style={{ color: theme.ui.textMain }}
          onMouseEnter={() => definition?.desc && setTooltip(definition.desc)}
          onMouseLeave={() => setTooltip(null)}
        >
          {node.type}
        </div>
        {node.type === 'SubTree' && (
          <button
            className="text-[10px] px-2 py-0.5 rounded transition-colors"
            style={{ backgroundColor: theme.ui.buttonBg, color: theme.ui.tabActiveText }}
            onClick={handleReloadSubTree}
          >
            Reload Pins
          </button>
        )}
      </div>

      {/* Return 属性 */}
      <div className="flex items-center gap-1">
        <div className="text-[10px] w-12 shrink-0" style={{ color: theme.ui.textDim }}>Return</div>
        <div className="flex-1 h-5 rounded px-1 border flex items-center" style={{ backgroundColor: theme.ui.inputBg, borderColor: theme.ui.border }}>
          <AdaptiveSelect
            value={node.returnType || 'Default'}
            options={['Default', 'Invert', 'Success', 'Failure']}
            onChange={(val) => updateNodeProperty(nodeId, { returnType: val as any })}
            baseClassName="text-xs w-full block leading-4"
            containerClassName="w-full block"
            triggerColor={theme.ui.textMain}
            renderLabel={(val) => val === 'Default' ? '(Default)' : val}
          />
        </div>
      </div>

      {/* Nickname */}
      <div className="flex items-center gap-1">
        <input
          className="flex-1 h-5 leading-5 text-xs px-1 rounded outline-none border"
          style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
          value={node.nickname || ''}
          onChange={(e) => updateNodeProperty(nodeId, { nickname: e.target.value })}
          placeholder="Nickname..."
        />
      </div>

      {/* 注释 */}
      <div>
        <textarea
          className="w-full text-xs px-1 py-0.5 rounded outline-none resize-none overflow-hidden"
          style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain }}
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
        {inputPins.length > 0 && outputPins.length > 0 && (
          <div className="h-px mb-1" style={{ backgroundColor: theme.ui.border, opacity: 0.45 }} />
        )}
        {renderPinList(outputPins)}
      </div>
    </div>
  );
}

// ==================== SubTree Specialized UI ====================

// TreeFilePicker extracted to separate file

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
  const setTooltip = useTooltipStore(state => state.setTooltip);
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

  const colorClass = resolveTypeColor(pin.valueType);
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
    let valueToSubmit = manualValue !== undefined ? manualValue : editValue;
    if (bindingType === 'const') {
      if (nodeType === 'SubTree' && pin.name === 'Tree') {
        valueToSubmit = stripExtension(valueToSubmit).replace(/\\/g, '/');
      }
      const result = validateValue(valueToSubmit, pin.valueType, pin.countType);
      if (!result.isValid) {
        const nodeLabel = nodeType ? `${nodeType}:${nodeUid ?? '?'}` : '';
        const prefix = nodeLabel ? `Node [${nodeLabel}] ` : '';
        const errorMsg = `${prefix}Pin [${pin.name}] invalid: ${result.error}`;
        notify(result.error || 'Invalid value', 'error');
        logger.error(errorMsg);
      } else {
        // Sync the value if it was modified (stripped extension)
        if (valueToSubmit !== (manualValue !== undefined ? manualValue : editValue)) {
          onUpdate({ binding: { type: 'const', value: valueToSubmit } });
        }

        if (nodeType === 'SubTree' && pin.name === 'Tree') {
          // 如果是 SubTree 的 Tree 路径更新，自动触发 Reload
          reloadSubTreePins(nodeId);
        }
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
    ? { bg: theme.ui.buttonBg, color: theme.ui.textMain, text: 'C', title: 'Constant → Click to switch to Variable' }
    : { bg: theme.ui.buttonHoverBg, color: theme.ui.tabActiveText, text: 'V', title: 'Variable → Click to switch to Constant' };

  return (
    <div
      className={`text-xs rounded p-1 ${!isEnabled ? 'opacity-90' : ''}`}
      style={{ backgroundColor: theme.ui.panelBg }}
      onMouseEnter={() => pin.desc && setTooltip(pin.desc)}
      onMouseLeave={() => setTooltip(null)}
    >
      <div className="flex items-center gap-1 min-w-0">
        <div className="flex items-center gap-0.5 shrink-0">
          {canToggleEnable && (
            <button
              className="w-3 h-3 rounded-sm border"
              style={{
                backgroundColor: isEnabled ? theme.ui.success : theme.ui.buttonBg,
                borderColor: isEnabled ? theme.ui.success : theme.ui.border
              }}
              onClick={handleEnableToggle}
            />
          )}

          <span
            className="font-medium truncate min-w-[40px]"
            style={{ color: theme.ui.textMain }}
            onMouseEnter={() => setTooltip(pin.desc || null)}
            onMouseLeave={() => setTooltip(null)}
          >
            {pin.name}
          </span>
          <span className="text-[10px] font-semibold" style={{ color: theme.ui.textDim }}>{pin.isInput ? 'in' : 'out'}</span>
        </div>

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
          baseClassName="text-[11px] font-semibold"
          triggerColor={colorClass}
          getOptionColor={(opt: string) => resolveTypeColor(opt)}
          disabled={pin.allowedValueTypes.length <= 1}
        />

        {!pin.isCountTypeFixed && (
          <button
            className="text-[10px] text-center px-0.5 rounded transition-colors min-w-[12px] font-bold"
            style={{ color: colorClass, backgroundColor: 'transparent' }}
            onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
            onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
            onClick={toggleCountType}
          >
            {pin.countType === 'list' ? '[]' : '·'}
          </button>
        )}

        {pin.isCountTypeFixed && (
          <span className="text-[10px] opacity-80 min-w-3 text-center cursor-default font-bold" style={{ color: colorClass }}>
            {pin.countType === 'list' ? '[]' : '·'}
          </span>
        )}

        {/* Binding Toggle Button */}
        {pin.isBindingTypeFixed ? (
          <span
            className="text-[10px] px-1 rounded opacity-50 cursor-default shrink-0"
            style={{ backgroundColor: bindingBtnInfo.bg, color: bindingBtnInfo.color }}
          >
            {bindingBtnInfo.text}
          </span>
        ) : (
          <button
            className="text-[10px] px-1 rounded shrink-0"
            style={{ backgroundColor: bindingBtnInfo.bg, color: bindingBtnInfo.color }}
            onClick={handleBindingToggle}
          >
            {bindingBtnInfo.text}
          </button>
        )}

        {/* Value / Binding Input Area */}
        <div className="flex-1 min-w-0">
          {bindingType === 'pointer' ? (
            <div className="relative">
              <select
                className="w-full text-xs px-1 py-0.5 rounded outline-none appearance-none truncate pr-3 border"
                style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
                value={(() => {
                  if (isDataConnection) return 'data';
                  if (pin.binding.type === 'pointer') {
                    return `${pin.binding.isLocal ? 'local' : 'shared'}:${pin.binding.variableName}`;
                  }
                  return '';
                })()}
                onChange={(e) => {
                  if (e.target.value === 'data') {
                    handleVariableChange('', false);
                  } else {
                    const [scope, name] = e.target.value.split(':');
                    handleVariableChange(name, scope === 'local');
                  }
                }}
              >
                <option value="data">(Data)</option>
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
              {isDataConnection && hasDataConnection && (
                <div className="absolute right-0 top-1/2 -translate-y-1/2 w-2 h-2 rounded-full pointer-events-none" style={{ backgroundColor: theme.debug.running.border }} />
              )}
            </div>
          ) : pin.valueType === 'bool' && pin.countType !== 'list' ? (
            <button
              className="w-full text-xs px-1 py-0.5 rounded font-bold transition-colors"
              style={{
                backgroundColor: pin.binding.type === 'const' && pin.binding.value === 'T' ? theme.ui.success : theme.ui.danger,
                color: theme.ui.terminalButtonText,
                border: pin.binding.type === 'const' && pin.binding.value === 'T' ? 'none' : `1px solid ${theme.ui.danger}`,
              }}
              onClick={() => {
                const currentVal = pin.binding.type === 'const' ? pin.binding.value : 'F';
                const newValue = currentVal === 'T' ? 'F' : 'T';
                onUpdate({ binding: { type: 'const', value: newValue } });
                setEditValue(newValue);
              }}
            >
              {pin.binding.type === 'const' && pin.binding.value === 'T' ? 'TRUE' : 'FALSE'}
            </button>
          ) : pin.valueType === 'entity' && pin.countType !== 'list' ? (
            <span className="block w-full text-xs italic px-1 py-0.5 cursor-not-allowed border border-transparent" style={{ color: theme.ui.textDim }}>
              (Entity: Read-only)
            </span>
          ) : pin.valueType === 'enum' && pin.enumValues && pin.enumValues.length > 0 ? (
            <div className="w-full rounded px-1 py-0.5 border" style={{ backgroundColor: theme.ui.inputBg, borderColor: theme.ui.border }}>
              <AdaptiveSelect
                value={(pin.binding.type === 'const' ? pin.binding.value : editValue) || pin.enumValues[0]}
                options={pin.enumValues || []}
                onChange={(val) => { handleValueChange(val); handleValueSubmit(val); }}
                baseClassName="text-xs w-full"
                containerClassName="w-full block"
                triggerColor={theme.ui.textMain}
                getOptionColor={() => theme.ui.textMain}
                renderLabel={(val) => val}
              />
            </div>
          ) : (
            // Constant Mode
            isEditing ? (
              <div className="w-full">
                {nodeType === 'SubTree' && pin.name === 'Tree' ? (
                  <TreeFilePicker
                    value={editValue}
                    onChange={(val) => {
                      handleValueChange(val);
                      handleValueSubmit(val);
                    }}
                    options={treeFiles}
                    defaultOpen
                  />
                ) : (
                  <input
                    className="w-full h-5 leading-5 text-xs px-1 rounded border outline-none"
                    style={validateValue(editValue, pin.valueType, pin.countType).isValid
                      ? { backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }
                      : { backgroundColor: theme.ui.danger, color: theme.ui.terminalButtonText, borderColor: theme.ui.danger }}
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
                className="block w-full h-5 leading-5 text-xs truncate cursor-pointer transition-opacity px-1 rounded border"
                style={currentPinValidation.isValid
                  ? { backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }
                  : { backgroundColor: theme.ui.danger, color: theme.ui.terminalButtonText, borderColor: theme.ui.danger }}
                onMouseEnter={() => {
                  if (pin.binding.type === 'const') {
                    setTooltip(pin.binding.value || '-');
                  }
                }}
                onMouseLeave={() => setTooltip(null)}
                onClick={() => { setEditValue(pin.binding.type === 'const' ? pin.binding.value : ''); setIsEditing(true); }}
              >
                <div className="flex items-center gap-1 group/value">
                  <span className="truncate">
                    {(() => {
                      const b = pin.binding as { type: 'const'; value: string };
                      return b.value || (pin.valueType === 'string' ? '""' : '0');
                    })()}
                  </span>
                  {nodeType === 'SubTree' && pin.name === 'Tree' && pin.binding.type === 'const' && pin.binding.value && (
                    <button
                      className="opacity-0 group-hover/value:opacity-100 transition-opacity"
                      style={{ color: theme.ui.textDim }}
                      onClick={(e) => {
                        e.stopPropagation();
                        const b = pin.binding as { type: 'const'; value: string };
                        useEditorStore.getState().openTree(b.value);
                      }}
                    >
                      ↗
                    </button>
                  )}
                </div>
              </span>
            )

          )}
        </div >
      </div >

      {(needsVectorIndex || hasVectorIndex) && bindingType === 'pointer' && (
        <div className="mt-1 flex items-center gap-1 pl-4">
          <span className="text-[10px]" style={{ color: theme.ui.textDim }}>Index:</span>
          <button
            className="text-[10px] px-1 rounded"
            style={{
              backgroundColor: pin.vectorIndex?.type === 'const' ? theme.ui.buttonBg : theme.ui.buttonHoverBg,
              color: pin.vectorIndex?.type === 'const' ? theme.ui.textMain : theme.ui.tabActiveText
            }}
            onClick={handleVectorIndexTypeToggle}
          >
            {pin.vectorIndex?.type === 'const' ? 'C' : 'V'}
          </button>
          {pin.vectorIndex?.type === 'const' ? (
            <input
              className="flex-1 text-xs px-1 py-0.5 rounded outline-none border"
              style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
              value={vectorIndexValue}
              onChange={(e) => handleVectorIndexChange(e.target.value)}
              placeholder="0"
            />
          ) : (
            <select
              className="flex-1 text-xs px-1 py-0.5 rounded outline-none border"
              style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
              value={(() => {
                if (pin.vectorIndex?.type === 'pointer') {
                  return `${pin.vectorIndex.isLocal ? 'local' : 'shared'}:${pin.vectorIndex.variableName}`;
                }
                return '';
              })()}
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
    </div >
  );
});
