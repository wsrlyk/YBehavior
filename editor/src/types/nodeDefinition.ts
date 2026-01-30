import type { ValueType, CountType, NodeCategory } from './index';

export interface PinDefinition {
  name: string;
  valueType: ValueType;
  countType: CountType;
  isInput: boolean;
  isConst: boolean;
  isEnable: boolean;
  defaultValue: string;
  enumValues?: string[];
  vTypeGroup?: number;
  cTypeGroup?: number;
}

export interface TypeMapRule {
  srcVariable: string;
  srcValue: string;
  desVariable: string;
  desType: string;
}

export interface NodeDefinition {
  className: string;
  category: NodeCategory;
  note: string;
  hierarchy: string;
  icon?: string;
  pins: PinDefinition[];
  typeMaps: TypeMapRule[];
  source: 'builtin' | 'external';
}

export interface NodeDefinitionRegistry {
  definitions: Map<string, NodeDefinition>;
  getDefinition(className: string): NodeDefinition | undefined;
  getByCategory(category: NodeCategory): NodeDefinition[];
}
