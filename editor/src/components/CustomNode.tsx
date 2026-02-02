import { memo } from 'react';
import { Handle, Position, type NodeProps, type Node } from '@xyflow/react';
import type { TreeNode, Pin } from '../types';
import type { NodeDefinition, ConnectorDefinition } from '../types/nodeDefinition';

// 低饱和度节点颜色
const NODE_COLORS: Record<string, string> = {
  composite: '#5A8A5E',
  decorator: '#5A7A9A',
  action: '#B08050',
  condition: '#7A5A8A',
};

// Pin 类型颜色
const PIN_COLORS: Record<string, string> = {
  int: '#6B9BD2',
  float: '#7BC96F',
  bool: '#CC6666',
  string: '#D4A5D9',
  vector3: '#D9C76B',
  entity: '#6BD9D9',
  ulong: '#9999CC',
  enum: '#CC9966',
};

type CustomNodeData = {
  label: string;
  treeNode: TreeNode;
  nodeDefinition?: NodeDefinition;
};

export type CustomNodeType = Node<CustomNodeData, 'custom'>;

function PinRow({ pin, isInput }: { pin: Pin; isInput: boolean }) {
  const pinColor = PIN_COLORS[pin.valueType] || '#888';

  // 构建 Vector Index 显示
  const getVectorIndexDisplay = () => {
    if (!pin.vectorIndex) return '';
    if (pin.vectorIndex.type === 'const') {
      return `[${pin.vectorIndex.value}]`;
    } else if (pin.vectorIndex.type === 'pointer') {
      return `[${pin.vectorIndex.variableName}]`;
    }
    return '';
  };

  return (
    <div className={`relative flex items-center gap-1 text-xs py-0.5 ${isInput ? '' : 'flex-row-reverse'}`}>
      {/* Pin 的 Handle，用于数据连接 */}
      <Handle
        type={isInput ? 'target' : 'source'}
        position={isInput ? Position.Left : Position.Right}
        id={`pin-${isInput ? 'in' : 'out'}-${pin.name}`}
        className="!w-2 !h-2 !rounded-full !border-0 hover:!scale-125 !cursor-crosshair !transition-all"
        style={{
          backgroundColor: pinColor,
          [isInput ? 'left' : 'right']: '-4px',
        }}
      />
      <span className="text-gray-300 truncate max-w-20">{pin.name}</span>
      {pin.binding.type === 'const' && (
        <span className="text-gray-500 text-[10px] truncate max-w-12">
          {pin.binding.value || '-'}
        </span>
      )}
      {pin.binding.type === 'pointer' && pin.binding.variableName && (
        <span className="text-blue-400 text-[10px] truncate max-w-12">
          {pin.binding.variableName}{getVectorIndexDisplay()}
        </span>
      )}
      {pin.binding.type === 'pointer' && !pin.binding.variableName && (
        <span className="text-gray-500 text-[10px] italic">○</span>
      )}
    </div>
  );
}

function CustomNode({ data, selected }: NodeProps<CustomNodeType>) {
  const { label, treeNode, nodeDefinition } = data;
  const bgColor = NODE_COLORS[treeNode.category] || '#666';

  // 过滤掉禁用的 Pin
  const inputPins = treeNode.pins.filter((p: Pin) => p.isInput && p.enableType !== 'disable');
  const outputPins = treeNode.pins.filter((p: Pin) => !p.isInput && p.enableType !== 'disable');

  // 根据节点定义决定是否显示连接器
  const hasParent = nodeDefinition?.hasParent ?? true;
  const childConnectors = nodeDefinition?.childConnectors ?? [];
  const hasChildren = childConnectors.length > 0;

  // 计算子连接器的位置（多个连接器时均匀分布）
  const connectorCount = childConnectors.length;

  return (
    <div
      className="rounded shadow-lg min-w-32 cursor-move hover:brightness-110 transition-all"
      style={{
        backgroundColor: '#1f2937',
        border: `2px solid ${selected ? '#fff' : bgColor}`,
        boxShadow: selected ? '0 0 0 2px rgba(255,255,255,0.3)' : undefined,
      }}
    >
      {/* 标题栏 */}
      <div
        className="px-2 py-1 text-xs font-medium text-white rounded-t flex items-center gap-1"
        style={{ backgroundColor: bgColor }}
      >
        <span className="text-white text-[10px]">{treeNode.uid ?? '?'}</span>
        <span>{label}</span>
        {treeNode.disabled && <span className="ml-1 text-gray-300">(disabled)</span>}
      </div>

      {/* Pin 区域 */}
      {(inputPins.length > 0 || outputPins.length > 0) && (
        <div className="flex justify-between px-2 py-1 gap-4">
          {/* 输入 Pin - 左侧 */}
          <div className="flex flex-col">
            {inputPins.map((pin: Pin, i: number) => (
              <PinRow key={`in-${i}`} pin={pin} isInput={true} />
            ))}
          </div>

          {/* 输出 Pin - 右侧 */}
          <div className="flex flex-col items-end">
            {outputPins.map((pin: Pin, i: number) => (
              <PinRow key={`out-${i}`} pin={pin} isInput={false} />
            ))}
          </div>
        </div>
      )}

      {/* 子连接器标签（多个连接器时显示） */}
      {connectorCount > 1 && (
        <div className="flex justify-around px-1 pb-1 text-[9px] text-gray-400">
          {childConnectors.map((conn: ConnectorDefinition, i: number) => (
            <span key={i}>{conn.label || conn.name}</span>
          ))}
        </div>
      )}

      {/* 连接点 - 父节点连接（顶部） */}
      {hasParent && (
        <Handle
          type="target"
          position={Position.Top}
          id="tree-target"
          className="!bg-gray-400 !w-3 !h-2 !rounded-sm !border-0 hover:!bg-white hover:!scale-125 !cursor-crosshair !transition-all"
        />
      )}

      {/* 连接点 - 子节点连接（底部） */}
      {hasChildren && connectorCount === 1 && (
        <Handle
          type="source"
          position={Position.Bottom}
          id={childConnectors[0].name}
          className="!bg-gray-400 !w-3 !h-2 !rounded-sm !border-0 hover:!bg-white hover:!scale-125 !cursor-crosshair !transition-all"
        />
      )}

      {/* 多个子连接器时，均匀分布 */}
      {hasChildren && connectorCount > 1 && childConnectors.map((conn: ConnectorDefinition, i: number) => (
        <Handle
          key={conn.name}
          type="source"
          position={Position.Bottom}
          id={conn.name}
          className="!bg-gray-400 !w-3 !h-2 !rounded-sm !border-0 hover:!bg-white hover:!scale-125 !cursor-crosshair !transition-all"
          style={{
            left: `${((i + 1) / (connectorCount + 1)) * 100}%`,
          }}
        />
      ))}
    </div>
  );
}

export default memo(CustomNode);
