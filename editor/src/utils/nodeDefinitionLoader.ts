import { XMLParser } from 'fast-xml-parser';
import type { NodeDefinition, PinDefinition, TypeMapRule, ConnectorDefinition } from '../types/nodeDefinition';
import type { ValueType, CountType, NodeCategory } from '../types';

const VALUE_TYPE_MAP: Record<string, ValueType> = {
  'I': 'int',
  'F': 'float',
  'B': 'bool',
  'S': 'string',
  'V': 'vector3',
  'A': 'entity',
  'U': 'ulong',
  'E': 'enum',
};

function parseValueType(typeStr: string): ValueType {
  // 可能是单字符如 "I" 或组合如 "IF"
  const char = typeStr.charAt(0).toUpperCase();
  return VALUE_TYPE_MAP[char] || 'int';
}

function parseCountType(typeStr: string, isArray?: string): CountType {
  if (isArray === 'True') return 'list';
  // 检查类型字符串是否表示数组（如 "II", "FF"）
  if (typeStr.length >= 2 && typeStr[0].toUpperCase() === typeStr[1].toUpperCase()) {
    return 'list';
  }
  return 'scalar';
}

interface XmlVariable {
  '@_Name': string;
  '@_ValueType': string;
  '@_IsArray'?: string;
  '@_IsConst'?: string;
  '@_IsInput'?: string;
  '@_IsEnable'?: string;
  '@_Value'?: string;
  '@_Param'?: string;
  '@_vTypeGroup'?: string;
  '@_cTypeGroup'?: string;
}

interface XmlTypeMap {
  '@_SrcVariable': string;
  '@_SrcValue': string;
  '@_DesVariable': string;
  '@_DesType': string;
}

interface XmlAction {
  '@_Class': string;
  '@_Note'?: string;
  '@_Hierachy'?: string;
  '@_Icon'?: string;
  Variable?: XmlVariable | XmlVariable[];
  TypeMap?: XmlTypeMap | XmlTypeMap[];
}

function parseVariable(v: XmlVariable): PinDefinition {
  const valueTypeStr = v['@_ValueType'] || 'I';
  
  // enableType: 没有配置 = fixed, True = enable, False = disable
  let enableType: 'fixed' | 'enable' | 'disable' = 'fixed';
  if (v['@_IsEnable'] !== undefined) {
    enableType = v['@_IsEnable'] === 'True' ? 'enable' : 'disable';
  }
  
  // constType: 没有配置 = switchable, True = const, False = pointer
  let constType: 'const' | 'pointer' | 'switchable' = 'switchable';
  if (v['@_IsConst'] !== undefined) {
    constType = v['@_IsConst'] === 'True' ? 'const' : 'pointer';
  }
  
  // arrayType: 没有配置 = switchable, True = list, False = scalar
  let arrayType: 'scalar' | 'list' | 'switchable' = 'switchable';
  if (v['@_IsArray'] !== undefined) {
    arrayType = v['@_IsArray'] === 'True' ? 'list' : 'scalar';
  }
  
  return {
    name: v['@_Name'],
    valueType: parseValueType(valueTypeStr),
    countType: parseCountType(valueTypeStr, v['@_IsArray']),
    isInput: v['@_IsInput'] !== 'False',
    constType,
    arrayType,
    enableType,
    defaultValue: v['@_Value'] || '',
    enumValues: v['@_Param']?.split('|'),
    vTypeGroup: v['@_vTypeGroup'] ? parseInt(v['@_vTypeGroup']) : undefined,
    cTypeGroup: v['@_cTypeGroup'] ? parseInt(v['@_cTypeGroup']) : undefined,
  };
}

function parseTypeMap(tm: XmlTypeMap): TypeMapRule {
  return {
    srcVariable: tm['@_SrcVariable'],
    srcValue: tm['@_SrcValue'],
    desVariable: tm['@_DesVariable'],
    desType: tm['@_DesType'],
  };
}

function parseAction(action: XmlAction): NodeDefinition {
  const pins: PinDefinition[] = [];
  const typeMaps: TypeMapRule[] = [];
  
  if (action.Variable) {
    const vars = Array.isArray(action.Variable) ? action.Variable : [action.Variable];
    for (const v of vars) {
      pins.push(parseVariable(v));
    }
  }
  
  if (action.TypeMap) {
    const maps = Array.isArray(action.TypeMap) ? action.TypeMap : [action.TypeMap];
    for (const tm of maps) {
      typeMaps.push(parseTypeMap(tm));
    }
  }
  
  return {
    className: action['@_Class'],
    category: 'action',
    note: action['@_Note'] || '',
    hierarchy: action['@_Hierachy'] || '',
    icon: action['@_Icon'],
    pins,
    typeMaps,
    source: 'external',
    hasParent: true,        // Action 节点有父连接器
    childConnectors: [],    // Action 节点没有子连接器
  };
}

export function parseActionsXml(xmlContent: string): NodeDefinition[] {
  const parser = new XMLParser({
    ignoreAttributes: false,
    attributeNamePrefix: '@_',
  });
  
  const parsed = parser.parse(xmlContent);
  const actions = parsed.Actions?.Action;
  
  if (!actions) return [];
  
  const actionList = Array.isArray(actions) ? actions : [actions];
  return actionList.map(parseAction);
}

interface XmlConnector {
  '@_Name': string;
  '@_Label'?: string;
  '@_MaxChildren'?: string;
}

interface XmlBuiltinNode {
  '@_Class': string;
  '@_Category': string;
  '@_Note'?: string;
  '@_Hierachy'?: string;
  '@_Icon'?: string;
  '@_HasParent'?: string;
  Variable?: XmlVariable | XmlVariable[];
  TypeMap?: XmlTypeMap | XmlTypeMap[];
  Connector?: XmlConnector | XmlConnector[];
}

function parseBuiltinNode(node: XmlBuiltinNode): NodeDefinition {
  const pins: PinDefinition[] = [];
  const typeMaps: TypeMapRule[] = [];
  
  if (node.Variable) {
    const vars = Array.isArray(node.Variable) ? node.Variable : [node.Variable];
    for (const v of vars) {
      pins.push(parseVariable(v));
    }
  }
  
  if (node.TypeMap) {
    const maps = Array.isArray(node.TypeMap) ? node.TypeMap : [node.TypeMap];
    for (const tm of maps) {
      typeMaps.push(parseTypeMap(tm));
    }
  }
  
  const category = node['@_Category'] as NodeCategory;
  const className = node['@_Class'];
  
  // hasParent: Root 没有父连接器
  let hasParent: boolean;
  if (node['@_HasParent'] !== undefined) {
    hasParent = node['@_HasParent'] !== 'False';
  } else {
    hasParent = className !== 'Root';
  }
  
  // childConnectors: 解析 Connector 子元素，或根据 category 生成默认值
  let childConnectors: ConnectorDefinition[] = [];
  
  if (node.Connector) {
    // 从配置文件读取连接器定义
    const connectors = Array.isArray(node.Connector) ? node.Connector : [node.Connector];
    for (const c of connectors) {
      childConnectors.push({
        name: c['@_Name'],
        label: c['@_Label'],
        maxChildren: c['@_MaxChildren'] ? parseInt(c['@_MaxChildren']) : undefined,
      });
    }
  } else if (category !== 'action' && category !== 'condition') {
    // 默认：非 action/condition 节点有一个 default 连接器
    childConnectors = [{ name: 'default' }];
  }
  // action/condition 节点默认没有子连接器（childConnectors 保持空数组）
  
  return {
    className,
    category: category || 'action',
    note: node['@_Note'] || '',
    hierarchy: node['@_Hierachy'] || '',
    icon: node['@_Icon'],
    pins,
    typeMaps,
    source: 'builtin',
    hasParent,
    childConnectors,
  };
}

export function parseBuiltinXml(xmlContent: string): NodeDefinition[] {
  const parser = new XMLParser({
    ignoreAttributes: false,
    attributeNamePrefix: '@_',
  });
  
  const parsed = parser.parse(xmlContent);
  const nodes = parsed.Nodes?.Node;
  
  if (!nodes) return [];
  
  const nodeList = Array.isArray(nodes) ? nodes : [nodes];
  return nodeList.map(parseBuiltinNode);
}
