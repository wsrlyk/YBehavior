import type { ValueType, CountType, NodeCategory } from './index';

export interface PinDefinition {
  name: string;
  valueType: ValueType;
  countType: CountType;

  // isInput: true = 输入, false = 输出（没有配置默认 true）
  isInput: boolean;

  // constType: 'const' = 固定常量, 'pointer' = 固定引用, 'switchable' = 默认常量可切换
  constType: 'const' | 'pointer' | 'switchable';

  // arrayType: 'scalar' = 固定标量, 'list' = 固定数组, 'switchable' = 默认标量可切换
  arrayType: 'scalar' | 'list' | 'switchable';

  // enableType: 'fixed' = 固定启用不可切换, 'enable' = 默认启用可切换, 'disable' = 默认禁用可切换
  enableType: 'fixed' | 'enable' | 'disable';

  defaultValue: string;
  enumValues?: string[];
  vTypeGroup?: number;
  cTypeGroup?: number;
  allowedValueTypes?: import('./index').ValueType[];  // 允许的数据类型，默认只有 valueType
  desc?: string;
}

export interface TypeMapRule {
  srcPin: string;
  srcValue: string;
  desPin: string;
  desType: string;
}

export interface ConnectorDefinition {
  name: string;           // 连接器名称，如 "default", "if", "then", "else"
  label?: string;         // 显示标签
  maxChildren?: number;   // 最大子节点数，undefined 表示无限制
}

export interface NodeDefinition {
  className: string;
  category: NodeCategory;
  note: string;
  hierarchy: string;
  icon?: string;
  pins: PinDefinition[];
  typeMaps: TypeMapRule[];
  desc?: string;
  source: 'builtin' | 'external';

  // Connector 配置
  hasParent: boolean;                    // 是否有父节点连接器（Root 为 false）
  childConnectors: ConnectorDefinition[]; // 子连接器列表（空数组表示没有子连接器）
}

export interface NodeDefinitionRegistry {
  definitions: Map<string, NodeDefinition>;
  getDefinition(className: string): NodeDefinition | undefined;
  getByCategory(category: NodeCategory): NodeDefinition[];
}
