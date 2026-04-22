import { XMLParser } from 'fast-xml-parser';
import type {
  Tree, TreeNode, Variable, Pin, TreeConnection, DataConnection,
  ValueType, CountType, PinBinding, NodeCategory, TreeInterfacePin
} from '../types';
import { decodeXmlEntities } from './stringUtils';
import type { NodeDefinition, PinDefinition } from '../types/nodeDefinition';

// Pin 值格式: "_XY value" 或 "XYZ value"
// X = 值类型: I(int), F(float), B(bool), S(string), V(vector3), A(entity), U(ulong), E(enum)
// Y = 数量类型: _(scalar), _(list用大写重复如II, FF)
// Z = 绑定类型: C(const), P(pointer to shared), p(pointer to local)
// 后续可能有: E(enable), D(disable), c/C(const vector index), p/P(pointer vector index)

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

function parseValueType(char: string): ValueType {
  return VALUE_TYPE_MAP[char.toUpperCase()] || 'int';
}

interface ParsedPinValue {
  valueType: ValueType;
  countType: CountType;
  isPointer: boolean;
  isLocal: boolean;
  value: string;
  variableName: string;
  isEnabled: boolean;
  vectorIndex?: PinBinding;
}

function parsePinValue(raw: string): ParsedPinValue {
  // 格式: "_XY value" 或 "XYZ value"
  // 例如: "_IC 5" = int scalar const 5
  //       "_IP a" = int scalar pointer to shared variable 'a'
  //       "_Ip a" = int scalar pointer to local variable 'a'
  //       "IIC 1|2|3" = int list const [1,2,3]
  //       "_FPE " = float scalar pointer enabled (no value)

  const result: ParsedPinValue = {
    valueType: 'int',
    countType: 'scalar',
    isPointer: false,
    isLocal: false,
    value: '',
    variableName: '',
    isEnabled: true,
  };

  if (!raw || raw.length < 3) return result;

  const code = raw.substring(0, 4).replace(/ .*/, ''); // 取前面的类型码
  const valueStart = raw.indexOf(' ');
  const value = valueStart >= 0 ? raw.substring(valueStart + 1) : '';

  // 解析类型码
  let idx = 0;
  if (code[idx] === '_') idx++;

  // 值类型
  if (idx < code.length) {
    result.valueType = parseValueType(code[idx]);
    idx++;
  }

  // 数量类型 (检查是否是 list)
  if (idx < code.length && code[idx].toUpperCase() === code[idx - 1].toUpperCase()) {
    result.countType = 'list';
    idx++;
  }

  // 绑定类型
  if (idx < code.length) {
    const bindChar = code[idx];
    if (bindChar === 'C' || bindChar === 'c') {
      result.isPointer = false;
      // 值可能包含 Vector Index 信息，格式: "value VI c index"
      // 只取第一部分作为值
      const parts = value.split(' VI ');
      result.value = parts[0];
      if (parts.length > 1) {
        // 解析 Vector Index: "c 0" 或 "p varName"
        const viParts = parts[1].split(' ');
        if (viParts[0] === 'c') {
          result.vectorIndex = { type: 'const', value: viParts[1] || '0' };
        } else if (viParts[0] === 'p' || viParts[0] === 'P') {
          result.vectorIndex = { type: 'pointer', variableName: viParts[1] || '', isLocal: viParts[0] === 'p' };
        }
      }
    } else if (bindChar === 'P') {
      result.isPointer = true;
      result.isLocal = false;
      // 变量名可能包含 Vector Index 信息
      const parts = value.split(' VI ');
      result.variableName = parts[0];
      if (parts.length > 1) {
        const viParts = parts[1].split(' ');
        if (viParts[0] === 'c') {
          result.vectorIndex = { type: 'const', value: viParts[1] || '0' };
        } else if (viParts[0] === 'p' || viParts[0] === 'P') {
          result.vectorIndex = { type: 'pointer', variableName: viParts[1] || '', isLocal: viParts[0] === 'p' };
        }
      }
    } else if (bindChar === 'p') {
      result.isPointer = true;
      result.isLocal = true;
      // 变量名可能包含 Vector Index 信息
      const parts = value.split(' VI ');
      result.variableName = parts[0];
      if (parts.length > 1) {
        const viParts = parts[1].split(' ');
        if (viParts[0] === 'c') {
          result.vectorIndex = { type: 'const', value: viParts[1] || '0' };
        } else if (viParts[0] === 'p' || viParts[0] === 'P') {
          result.vectorIndex = { type: 'pointer', variableName: viParts[1] || '', isLocal: viParts[0] === 'p' };
        }
      }
    }
    idx++;
  }

  // Enable/Disable 标记
  if (idx < code.length) {
    const enableChar = code[idx];
    if (enableChar === 'E') {
      result.isEnabled = true;
    } else if (enableChar === 'D') {
      result.isEnabled = false;
    }
  }

  return result;
}

export function parseVariable(name: string, raw: string, isLocal: boolean): Variable {
  const parsed = parsePinValue(raw);
  return {
    name,
    valueType: parsed.valueType,
    countType: parsed.countType,
    isLocal,
    defaultValue: parsed.value,
  };
}

function createPin(name: string, raw: string, pinDef?: PinDefinition): Pin {
  const parsed = parsePinValue(raw);

  const binding: PinBinding = parsed.isPointer
    ? { type: 'pointer', variableName: parsed.variableName, isLocal: parsed.isLocal }
    : { type: 'const', value: parsed.value };

  // 使用节点定义中的参数，如果没有则使用解析出的值
  const isInput = pinDef?.isInput ?? true;
  const enumValues = pinDef?.enumValues;

  // enableType: 
  // - 如果有定义且为 fixed，则固定启用
  // - 如果有定义且为 enable/disable，使用 XML 中的值（parsed.isEnabled）决定当前状态
  // - 如果没有定义，根据 XML 中的值决定
  let enableType: 'fixed' | 'enable' | 'disable';
  if (pinDef) {
    if (pinDef.enableType === 'fixed') {
      enableType = 'fixed';
    } else {
      // 可切换的 Pin，根据 XML 中的值决定当前状态
      enableType = parsed.isEnabled ? 'enable' : 'disable';
    }
  } else {
    enableType = parsed.isEnabled ? 'enable' : 'disable';
  }

  return {
    name,
    valueType: parsed.valueType,
    countType: parsed.countType,
    bindingType: parsed.isPointer ? 'pointer' : 'const',
    binding,
    enableType,
    isInput,
    vectorIndex: parsed.vectorIndex,
    enumValues,
    allowedValueTypes: pinDef?.allowedValueTypes || [parsed.valueType],  // 从定义中读取，默认只有当前类型
    vTypeGroup: pinDef?.vTypeGroup,  // 类型联动组
    isCountTypeFixed: pinDef ? pinDef.arrayType !== 'switchable' : false,
    isBindingTypeFixed: pinDef ? pinDef.constType !== 'switchable' : false,
    desc: pinDef?.desc,
  };
}

// 从定义创建默认 Pin（用于树文件中缺少的 Pin 或新建节点）
function createPinFromDefinition(pinDef: PinDefinition): Pin {
  // 根据 constType 决定默认绑定类型
  // 引用类型默认使用空变量名（表示数据连接状态，等待连接）
  const bindingType = pinDef.constType === 'pointer' ? 'pointer' : 'const' as const;
  const binding: PinBinding = bindingType === 'pointer'
    ? { type: 'pointer', variableName: '', isLocal: false }  // 空变量名 = 数据连接状态
    : { type: 'const', value: pinDef.defaultValue };

  // 根据 arrayType 决定 countType
  const countType = pinDef.arrayType === 'list' ? 'list' : 'scalar';

  return {
    name: pinDef.name,
    valueType: pinDef.valueType,
    countType,
    bindingType,
    binding,
    enableType: pinDef.enableType,
    isInput: pinDef.isInput,
    enumValues: pinDef.enumValues,
    allowedValueTypes: pinDef.allowedValueTypes || [pinDef.valueType],  // 从定义中读取，默认只有当前类型
    vTypeGroup: pinDef.vTypeGroup,  // 类型联动组
    isCountTypeFixed: pinDef.arrayType !== 'switchable',
    isBindingTypeFixed: pinDef.constType !== 'switchable',
    desc: pinDef.desc,
  };
}

function getNodeCategory(nodeClass: string): NodeCategory {
  if (nodeClass === 'Root') return 'decorator'; // Root 作为特殊的装饰器
  const composites = ['Sequence', 'Selector', 'RandomSequence', 'RandomSelector', 'SwitchCase', 'IfThenElse'];
  const decorators = ['Loop', 'ForEach', 'For', 'Not', 'AlwaysSuccess', 'AlwaysFailed'];
  const conditions = ['Comparer'];

  if (composites.includes(nodeClass)) return 'composite';
  if (decorators.includes(nodeClass)) return 'decorator';
  if (conditions.includes(nodeClass)) return 'condition';
  return 'action';
}

interface XmlNode {
  '@_Class': string;
  '@_GUID': string;
  '@_Pos': string;
  '@_Connection'?: string;
  '@_Return'?: string;
  Shared?: Record<string, string>;
  Local?: Record<string, string>;
  Node?: XmlNode | XmlNode[];
  [key: string]: unknown;
}

let nodeIdCounter = 0;

function parseXmlNode(
  xmlNode: XmlNode,
  nodes: Map<string, TreeNode>,
  connections: TreeConnection[],
  parentId?: string,
  getNodeDefinition?: (className: string) => NodeDefinition | undefined
): string {
  const nodeClass = xmlNode['@_Class'];
  const guid = xmlNode['@_GUID'] || String(++nodeIdCounter);
  const pos = xmlNode['@_Pos']?.split(',').map(Number) || [0, 0];

  const id = `node-${guid}`;

  // 获取节点定义
  const nodeDef = getNodeDefinition?.(nodeClass);

  // 1.1 处理节点属性中的 Pin（用于基础 Pin，来自节点定义）
  const skipAttrs = [
    '@_Class', '@_GUID', '@_Pos', '@_Connection', '@_Return',
    '@_NickName', '@_Comment', '@_Disabled',
    'Shared', 'Local', 'Node', 'Input', 'Output'
  ];
  const xmlAttrValues = new Map<string, string>();  // 节点属性 → 基础 Pin
  const xmlInputValues = new Map<string, string>(); // <Input> 标签 → 动态 Input Pin
  const xmlOutputValues = new Map<string, string>(); // <Output> 标签 → 动态 Output Pin

  // 节点属性存入 xmlAttrValues
  for (const [key, value] of Object.entries(xmlNode)) {
    if (!skipAttrs.includes(key) && typeof value === 'string') {
      const pinName = key.startsWith('@_') ? key.substring(2) : key;
      xmlAttrValues.set(pinName, value);
    }
  }

  // 1.2 处理 <Input> 和 <Output> 子标签中的 Pin
  if (nodeClass !== 'Root') {
    const processLegacyPins = (tags: unknown, map: Map<string, string>) => {
      if (!tags) return;
      const tagList = Array.isArray(tags) ? tags : [tags];
      tagList.forEach(tag => {
        if (typeof tag === 'object' && tag !== null) {
          for (const [key, value] of Object.entries(tag)) {
            if (typeof value === 'string') {
              const pinName = key.startsWith('@_') ? key.substring(2) : key;
              map.set(pinName, value);
            }
          }
        }
      });
    };

    processLegacyPins(xmlNode.Input, xmlInputValues);
    processLegacyPins(xmlNode.Output, xmlOutputValues);
  }

  // 基于定义文件合并 Pin 数据
  const pins: Pin[] = [];

  if (nodeDef) {
    // 1. 基础 Pin（来自节点定义）→ 从节点属性中查找
    for (const pinDef of nodeDef.pins) {
      const xmlValue = xmlAttrValues.get(pinDef.name);
      if (xmlValue !== undefined) {
        pins.push(createPin(pinDef.name, xmlValue, pinDef));
      } else {
        pins.push(createPinFromDefinition(pinDef));
      }
    }

    // 2. 处理动态 Pin (不在定义中，但在 XML 中存在的)
    // 这种情况通常是 SubTree 节点
    const dynamicInputNames = Array.from(xmlInputValues.keys()).filter(name => !nodeDef.pins?.some(p => p.name === name));
    const dynamicOutputNames = Array.from(xmlOutputValues.keys()).filter(name => !nodeDef.pins?.some(p => p.name === name));
    const allDynamicNames = new Set([...dynamicInputNames, ...dynamicOutputNames]);

    allDynamicNames.forEach(pinName => {
      const inVal = xmlInputValues.get(pinName);
      const outVal = xmlOutputValues.get(pinName);

      if (inVal !== undefined) {
        pins.push(createPin(pinName, inVal, {
          name: pinName,
          isInput: true,
          valueType: 'int', // 基础动态 Pin，默认设为 int
          arrayType: 'scalar',
          constType: 'switchable',
          enableType: 'fixed',
          defaultValue: '',
        } as any));
      }
      if (outVal !== undefined) {
        pins.push(createPin(pinName, outVal, {
          name: pinName,
          isInput: false,
          valueType: 'int',
          arrayType: 'scalar',
          constType: 'switchable',
          enableType: 'fixed',
          defaultValue: '',
        } as any));
      }
    });
  } else {
    // 没有定义：直接使用树文件中的所有 Pin
    xmlInputValues.forEach((val, name) => {
      pins.push(createPin(name, val, { name, isInput: true, enableType: 'fixed' } as any));
    });
    xmlOutputValues.forEach((val, name) => {
      // 如果同名且值相同，可能已经在 Input 里了？但为了安全，还是分开存
      pins.push(createPin(name, val, { name, isInput: false, enableType: 'fixed' } as any));
    });
  }

  // 收集额外属性
  const extraAttrs: Record<string, string> = {};
  if (xmlNode['@_Connection']) extraAttrs['Connection'] = xmlNode['@_Connection'] as string;

  const node: TreeNode = {
    id,
    guid: parseInt(guid),
    type: nodeClass,
    category: getNodeCategory(nodeClass),
    position: { x: pos[0], y: pos[1] },
    nickname: xmlNode['@_NickName'] as string,
    comment: decodeXmlEntities(xmlNode['@_Comment'] as string),
    disabled: xmlNode['@_Disabled'] === 'true',
    returnType: (xmlNode['@_Return'] as any) || 'Default',
    pins,
    parentId,
    childrenIds: [],
    extraAttrs: Object.keys(extraAttrs).length > 0 ? extraAttrs : undefined,
  };

  nodes.set(id, node);

  // 处理子节点
  const childNodes = xmlNode.Node;
  if (childNodes) {
    const children = Array.isArray(childNodes) ? childNodes : [childNodes];
    for (const child of children) {
      const childId = parseXmlNode(child, nodes, connections, id, getNodeDefinition);
      node.childrenIds.push(childId);

      // 决定连接器名称：优先使用 XML 中的，否则使用父节点的第一个连接器名称，最后兜底用 'default'
      const parentNode = nodes.get(id);
      const parentDef = parentNode ? getNodeDefinition?.(parentNode.type) : undefined;
      const defaultConnectorName = parentDef?.childConnectors?.[0]?.name || 'default';
      const connName = child['@_Connection'] || defaultConnectorName;

      if (connName === 'condition') {
        node.hasConditionConnector = true;
      }

      connections.push({
        id: `conn-${id}-${childId}`,
        parentNodeId: id,
        parentConnector: connName,
        childNodeId: childId,
      });
    }
  }

  return id;
}

export function parseTreeXml(
  xmlContent: string,
  fileName: string,
  getNodeDefinition?: (className: string) => NodeDefinition | undefined
): Tree {
  // 重置计数器
  nodeIdCounter = 0;

  const parser = new XMLParser({
    ignoreAttributes: false,
    attributeNamePrefix: '@_',
    allowBooleanAttributes: true,
  });

  const parsed = parser.parse(xmlContent);
  const treeName = Object.keys(parsed).find(k => k !== '?xml') || 'Unknown';
  const treeData = parsed[treeName];
  console.log(`Parsing tree: ${treeName}, nodes count: ${JSON.stringify(treeData.Node ? (Array.isArray(treeData.Node) ? treeData.Node.length : 1) : 0)}`);

  const nodes = new Map<string, TreeNode>();
  const connections: TreeConnection[] = [];
  const sharedVariables: Variable[] = [];
  const localVariables: Variable[] = [];
  const dataConnections: DataConnection[] = [];

  const inputs: TreeInterfacePin[] = [];
  const outputs: TreeInterfacePin[] = [];

  const rootNodes = treeData.Node;
  console.log(`rootNodes found: ${!!rootNodes}`);
  let rootId = '';

  if (rootNodes) {
    const nodeList = Array.isArray(rootNodes) ? rootNodes : [rootNodes];
    console.log(`Node list length: ${nodeList.length}`);

    for (const xmlNode of nodeList) {
      const nodeId = parseXmlNode(xmlNode, nodes, connections, undefined, getNodeDefinition);
      console.log(`Parsed node: ${xmlNode['@_Class']}, ID: ${nodeId}`);

      // 第一个 Root 节点作为根
      if (xmlNode['@_Class'] === 'Root' && !rootId) {
        rootId = nodeId;
        console.log(`Root identified: ${rootId}`);

        // 解析 Shared 和 Local 变量
        if (xmlNode.Shared) {
          for (const [name, value] of Object.entries(xmlNode.Shared)) {
            if (name.startsWith('@_') && typeof value === 'string') {
              sharedVariables.push(parseVariable(name.substring(2), value, false));
            }
          }
        }
        if (xmlNode.Local) {
          for (const [name, value] of Object.entries(xmlNode.Local)) {
            if (name.startsWith('@_') && typeof value === 'string') {
              localVariables.push(parseVariable(name.substring(2), value, true));
            }
          }
        }

        // 解析 Root 节点的 Input 和 Output 作为 Tree Interface
        const parseRootInterfacePin = (tags: any, isInput: boolean) => {
          if (!tags) return;
          const tagList = Array.isArray(tags) ? tags : [tags];
          tagList.forEach(tag => {
            for (const [key, value] of Object.entries(tag)) {
              if (typeof value === 'string') {
                const name = key.startsWith('@_') ? key.substring(2) : key;
                const parsed = parsePinValue(value);
                const pin: TreeInterfacePin = {
                  id: `${isInput ? 'input' : 'output'}-${name}`,
                  name,
                  valueType: parsed.valueType,
                  countType: parsed.countType,
                  binding: {
                    type: parsed.isPointer ? 'variable' : 'const',
                    value: parsed.isPointer ? parsed.variableName : parsed.value,
                    isLocal: parsed.isPointer ? parsed.isLocal : undefined,
                  },
                  vectorIndex: parsed.vectorIndex,
                };
                if (isInput) inputs.push(pin);
                else outputs.push(pin);
              }
            }
          });
        };

        parseRootInterfacePin(xmlNode.Input, true);
        parseRootInterfacePin(xmlNode.Output, false);
      }
    }
  }

  // 解析数据连接
  if (treeData.DataConnections?.DataConnection) {
    const dataConns = treeData.DataConnections.DataConnection;
    const connList = Array.isArray(dataConns) ? dataConns : [dataConns];

    for (const conn of connList) {
      dataConnections.push({
        id: `data-${conn['@_FromGUID']}-${conn['@_ToGUID']}-${conn['@_FromName']}-${conn['@_ToName']}`,
        fromNodeId: `node-${conn['@_FromGUID']}`,
        fromPinName: conn['@_FromName'],
        toNodeId: `node-${conn['@_ToGUID']}`,
        toPinName: conn['@_ToName'],
      });
    }
  }


  // 读取后先规范内存中的子节点/连接顺序，再计算 UID，确保结果稳定
  normalizeTreeTraversalOrder(nodes, rootId, connections, getNodeDefinition);
  calculateUIDs(nodes, rootId, connections, getNodeDefinition);

  return {
    name: treeName,
    path: fileName,
    nodes,
    rootId,
    connections,
    dataConnections,
    sharedVariables,
    localVariables,
    inputs,
    outputs,
    inputPins: [],
    outputPins: [],
  };
}


/**
 * 计算节点的 UID（深度优先遍历）
 * Root 节点从 1 开始，森林中其他树从 1001、2001 等开始
 */
function getConnectorOrder(
  nodeType: string,
  getNodeDefinition?: (className: string) => NodeDefinition | undefined
): string[] {
  const nodeDef = getNodeDefinition?.(nodeType);
  const order = ['condition'];

  if (nodeDef?.childConnectors) {
    for (const c of nodeDef.childConnectors) {
      if (c.name !== 'condition' && !order.includes(c.name)) {
        order.push(c.name);
      }
    }
  }

  if (!order.includes('children')) order.push('children');
  if (!order.includes('default')) order.push('default');
  return order;
}

function sortChildConnections(
  nodeId: string,
  nodeType: string,
  nodes: Map<string, TreeNode>,
  connectionsByParent: Map<string, TreeConnection[]>,
  getNodeDefinition?: (className: string) => NodeDefinition | undefined
): TreeConnection[] {
  const childConns = connectionsByParent.get(nodeId) || [];
  const order = getConnectorOrder(nodeType, getNodeDefinition);

  return [...childConns].sort((a, b) => {
    const connA = a.parentConnector || 'children';
    const connB = b.parentConnector || 'children';

    if (connA !== connB) {
      let idxA = order.indexOf(connA);
      let idxB = order.indexOf(connB);
      if (idxA === -1) idxA = 999;
      if (idxB === -1) idxB = 999;
      if (idxA !== idxB) return idxA - idxB;
    }

    const nodeA = nodes.get(a.childNodeId);
    const nodeB = nodes.get(b.childNodeId);
    return (nodeA?.position.x ?? 0) - (nodeB?.position.x ?? 0);
  });
}

function normalizeTreeTraversalOrder(
  nodes: Map<string, TreeNode>,
  rootId: string,
  connections: TreeConnection[],
  getNodeDefinition?: (className: string) => NodeDefinition | undefined
): void {
  const connectionsByParent = new Map<string, TreeConnection[]>();
  for (const conn of connections) {
    const list = connectionsByParent.get(conn.parentNodeId) || [];
    list.push(conn);
    connectionsByParent.set(conn.parentNodeId, list);
  }

  const orderedConnections: TreeConnection[] = [];
  const addedConnectionIds = new Set<string>();
  const visited = new Set<string>();

  nodes.forEach((node) => {
    node.childrenIds = [];
  });

  function traverse(nodeId: string) {
    if (visited.has(nodeId)) return;
    visited.add(nodeId);

    const node = nodes.get(nodeId);
    if (!node) return;

    const sortedConns = sortChildConnections(nodeId, node.type, nodes, connectionsByParent, getNodeDefinition);
    node.childrenIds = sortedConns.map(conn => conn.childNodeId);

    for (const conn of sortedConns) {
      if (!addedConnectionIds.has(conn.id)) {
        orderedConnections.push(conn);
        addedConnectionIds.add(conn.id);
      }
      traverse(conn.childNodeId);
    }
  }

  if (rootId) {
    traverse(rootId);
  }

  const childIds = new Set(connections.map(c => c.childNodeId));
  for (const nodeId of nodes.keys()) {
    if (!childIds.has(nodeId) && !visited.has(nodeId)) {
      traverse(nodeId);
    }
  }

  for (const conn of connections) {
    if (!addedConnectionIds.has(conn.id)) {
      orderedConnections.push(conn);
      addedConnectionIds.add(conn.id);
    }
  }

  connections.splice(0, connections.length, ...orderedConnections);
}

function calculateUIDs(
  nodes: Map<string, TreeNode>,
  rootId: string,
  connections: TreeConnection[],
  getNodeDefinition?: (className: string) => NodeDefinition | undefined
): void {
  let uid = 1;
  const visited = new Set<string>();

  // Build parent->children index for performance
  const connectionsByParent = new Map<string, TreeConnection[]>();
  for (const conn of connections) {
    const list = connectionsByParent.get(conn.parentNodeId) || [];
    list.push(conn);
    connectionsByParent.set(conn.parentNodeId, list);
  }

  // 深度优先先序遍历（父节点优先）
  function dfs(nodeId: string, isAncestorDisabled: boolean) {
    if (visited.has(nodeId)) return;
    visited.add(nodeId);

    const node = nodes.get(nodeId);
    if (!node) return;

    const effectivelyDisabled = isAncestorDisabled || node.disabled;
    node.uid = effectivelyDisabled ? undefined : uid++;

    // 子节点：先按 connection 顺序，再在同 connection 内按 x 坐标
    const sortedConns = sortChildConnections(nodeId, node.type, nodes, connectionsByParent, getNodeDefinition);
    for (const conn of sortedConns) {
      dfs(conn.childNodeId, effectivelyDisabled);
    }
  }

  // 从主根节点开始
  if (rootId) {
    dfs(rootId, false);
  }

  // 处理森林中的其他树（从 1001、2001 等开始）
  let forestIndex = 1;
  const childIds = new Set(connections.map(c => c.childNodeId));
  
  for (const nodeId of nodes.keys()) {
    if (!childIds.has(nodeId) && !visited.has(nodeId)) {
      // 这是一个未被遍历到的根节点（森林中的其他树）
      uid = forestIndex * 1000 + 1;
      dfs(nodeId, false);
      forestIndex++;
    }
  }
}
