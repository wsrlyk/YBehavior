import type { Tree, TreeNode, Pin, TreeConnection, ValueType, Variable, DataConnection, TreeInterfacePin } from '../types';
import { stripExtension } from './fileUtils';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';

/** ValueType 到 XML 字符的映射 */
const VALUE_TYPE_TO_CHAR: Record<ValueType, string> = {
  'int': 'I',
  'float': 'F',
  'bool': 'B',
  'string': 'S',
  'vector3': 'V',
  'entity': 'A',
  'ulong': 'U',
  'enum': 'E',
};


/**
 * 将 Pin 值序列化为 XML 属性格式
 * 格式规则:
 * - 单体(scalar): _[valueType][bindingType][enableFlag] [value]  例如: _IP a, _IC 1
 * - 数组(list):   [valueType][valueType][bindingType][enableFlag] [value]  例如: IIC 1|2|3, FFp fff
 */
function serializePinValue(pin: Pin, forEditor: boolean): string {
  let code = '';

  // 值类型
  const typeChar = VALUE_TYPE_TO_CHAR[pin.valueType] || 'I';

  if (pin.countType === 'list') {
    // 数组: 无下划线，重复类型字符
    code = typeChar + typeChar;
  } else {
    // 单体: 有下划线
    code = '_' + typeChar;
  }

  // 绑定类型
  if (pin.binding.type === 'const') {
    code += 'C'; // 或小写 c
  } else {
    // 指针: P = 全局, p = 本地（空变量名表示数据连接状态）
    code += pin.binding.isLocal ? 'p' : 'P';
  }

  // Enable/Disable 标记 (仅当可切换时)
  if (pin.enableType === 'enable') {
    code += 'E';
  } else if (pin.enableType === 'disable') {
    if (forEditor) {
      code += 'D';
    } else {
      return ''; // 运行时版不导出禁用的 Pin
    }
  }
  // fixed 状态不添加标记

  // 值
  let value = '';
  if (pin.binding.type === 'const') {
    value = pin.binding.value;
    // 如果是引用了其他树或FSM的文件路径，去除后缀名
    if ((pin.name === 'Tree' || pin.name === 'Reference' || pin.name === 'treeNode.Reference') && (value.endsWith('.tree') || value.endsWith('.fsm'))) {
      value = stripExtension(value);
    }
  } else {
    value = pin.binding.variableName; // 空变量名表示数据连接状态

    // 如果有 Vector Index，追加序列化
    if (pin.vectorIndex) {
      const vi = pin.vectorIndex;
      const viType = vi.type === 'const' ? 'c' : (vi.isLocal ? 'p' : 'P');
      const viVal = vi.type === 'const' ? vi.value : vi.variableName;
      value += ` VI ${viType} ${viVal}`;
    }
  }

  return code + ' ' + value;
}

/**
 * 序列化变量为 XML 属性格式
 * 格式规则同 Pin: 单体 _XC, 数组 XXc
 */
export function serializeVariable(v: Variable): string {
  let code = '';
  const typeChar = VALUE_TYPE_TO_CHAR[v.valueType] || 'I';

  if (v.countType === 'list') {
    // 数组: 无下划线，重复类型字符
    code = typeChar + typeChar;
  } else {
    // 单体: 有下划线
    code = '_' + typeChar;
  }

  code += v.isLocal ? 'c' : 'C'; // 变量默认是常量
  return code + ' ' + v.defaultValue;
}

/**
 * 序列化节点为 XML 元素（编辑器版）
 */
function serializeNodeForEditor(
  node: TreeNode,
  doc: Document,
  nodeMap: Map<string, TreeNode>,
  connections: TreeConnection[],
  sharedVars?: Variable[],
  localVars?: Variable[],
  parentConnector?: string,
  treeInterface?: { inputs: TreeInterfacePin[], outputs: TreeInterfacePin[] }
): Element {
  const el = doc.createElement('Node');
  el.setAttribute('Class', node.type);

  // GUID (编辑器版需要)
  el.setAttribute('GUID', String(node.guid));

  // 连接属性 (如果有，则放在 Pos 之前)
  if (parentConnector) {
    el.setAttribute('Connection', parentConnector);
  }

  if (node.returnType && node.returnType !== 'Default') {
    el.setAttribute('Return', node.returnType);
  } else if (node.extraAttrs?.Return) {
    el.setAttribute('Return', node.extraAttrs.Return);
  }

  // 位置
  el.setAttribute('Pos', `${Math.round(node.position.x)},${Math.round(node.position.y)}`);

  if (node.nickname) el.setAttribute('NickName', node.nickname);
  if (node.comment) el.setAttribute('Comment', node.comment);
  if (node.disabled) el.setAttribute('Disabled', 'true');
  if (node.returnType && node.returnType !== 'Default') {
    el.setAttribute('Return', node.returnType);
  }

  // 如果是 Root 节点，添加 Shared 和 Local 变量
  if (node.type === 'Root') {
    if (sharedVars && sharedVars.length > 0) {
      const sharedEl = doc.createElement('Shared');
      for (const v of sharedVars) {
        sharedEl.setAttribute(v.name, serializeVariable(v));
      }
      el.appendChild(sharedEl);
    }
    if (localVars && localVars.length > 0) {
      const localEl = doc.createElement('Local');
      for (const v of localVars) {
        localEl.setAttribute(v.name, serializeVariable(v));
      }
      el.appendChild(localEl);
    }
  }

  // Pin 值
  const inputEl = doc.createElement('Input');
  const outputEl = doc.createElement('Output');
  let hasInput = false;
  let hasOutput = false;

  const nodeDef = useNodeDefinitionStore.getState().getDefinition(node.type);

  for (const pin of node.pins) {
    const value = serializePinValue(pin, true);
    if (!value) continue;

    // 规则：如果在 NodeDefinition 中定义的 Pin，作为属性；否则作为子标签
    const isBasePin = nodeDef?.pins?.some(p => p.name === pin.name) ?? false;

    if (isBasePin) {
      el.setAttribute(pin.name, value);
    } else {
      if (pin.isInput) {
        inputEl.setAttribute(pin.name, value);
        hasInput = true;
      } else {
        outputEl.setAttribute(pin.name, value);
        hasOutput = true;
      }
    }
  }

  // 特殊处理 Root 节点的 Interface Pins (从 treeInterface 传入)
  if (node.type === 'Root' && treeInterface) {
    treeInterface.inputs.forEach(pin => {
      // 构造一个临时 Pin 对象进行序列化
      const tempPin: Pin = {
        name: pin.name,
        valueType: pin.valueType,
        countType: pin.countType,
        bindingType: pin.binding.type === 'variable' ? 'pointer' : 'const',
        binding: pin.binding.type === 'variable'
          ? { type: 'pointer', variableName: pin.binding.value, isLocal: pin.binding.isLocal || false }
          : { type: 'const', value: pin.binding.value },
        enableType: 'fixed', // Root Interface Pin 不支持禁用启用
        isInput: true,
        allowedValueTypes: [pin.valueType],
        isCountTypeFixed: true,
        isBindingTypeFixed: false,
        vectorIndex: pin.vectorIndex,
      };
      inputEl.setAttribute(pin.name, serializePinValue(tempPin, true));
      hasInput = true;
    });
    treeInterface.outputs.forEach(pin => {
      // 临时构造一个 Pin 对象用于序列化
      const tempPin: Pin = {
        name: pin.name,
        valueType: pin.valueType,
        countType: pin.countType,
        bindingType: pin.binding.type === 'variable' ? 'pointer' : 'const',
        binding: pin.binding.type === 'variable'
          ? { type: 'pointer', variableName: pin.binding.value, isLocal: pin.binding.isLocal || false }
          : { type: 'const', value: pin.binding.value },
        enableType: 'fixed', // Root Interface Pin 不支持禁用启用
        isInput: false,
        allowedValueTypes: [pin.valueType],
        isCountTypeFixed: true,
        isBindingTypeFixed: false,
        vectorIndex: pin.vectorIndex,
      };
      outputEl.setAttribute(pin.name, serializePinValue(tempPin, true));
      hasOutput = true;
    });
  }

  if (hasInput) el.appendChild(inputEl);
  if (hasOutput) el.appendChild(outputEl);

  // 递归序列化子节点（按 X 坐标从左到右排序）
  const childConnections = connections.filter(c => c.parentNodeId === node.id);
  const sortedChildConns = [...childConnections].sort((a, b) => {
    const nodeA = nodeMap.get(a.childNodeId);
    const nodeB = nodeMap.get(b.childNodeId);
    return (nodeA?.position.x ?? 0) - (nodeB?.position.x ?? 0);
  });

  for (const conn of sortedChildConns) {
    const childNode = nodeMap.get(conn.childNodeId);
    if (childNode) {
      // 传递 connection 名称 (children 是默认，不需要显式保存)
      const connName = conn.parentConnector === 'children' ? undefined : conn.parentConnector;

      const childEl = serializeNodeForEditor(childNode, doc, nodeMap, connections, undefined, undefined, connName, treeInterface);
      el.appendChild(childEl);
    }
  }

  return el;
}

/**
 * 序列化 DataConnections
 * 属性顺序: FromGUID, FromName, ToGUID, ToName
 */
function serializeDataConnections(
  dataConnections: DataConnection[],
  nodeMap: Map<string, TreeNode>,
  doc: Document,
  forEditor: boolean
): Element | null {
  if (dataConnections.length === 0) return null;

  const el = doc.createElement('DataConnections');
  for (const conn of dataConnections) {
    const connEl = doc.createElement('DataConnection');
    const fromNode = nodeMap.get(conn.fromNodeId);
    const toNode = nodeMap.get(conn.toNodeId);

    if (forEditor) {
      // 编辑器版使用 GUID，顺序: FromGUID, FromName, ToGUID, ToName
      if (fromNode) connEl.setAttribute('FromGUID', String(fromNode.guid));
      connEl.setAttribute('FromName', conn.fromPinName);
      if (toNode) connEl.setAttribute('ToGUID', String(toNode.guid));
      connEl.setAttribute('ToName', conn.toPinName);
    } else {
      // 运行时版使用 UID
      if (fromNode) connEl.setAttribute('FromUID', String(fromNode.uid ?? 0));
      connEl.setAttribute('FromName', conn.fromPinName);
      if (toNode) connEl.setAttribute('ToUID', String(toNode.uid ?? 0));
      connEl.setAttribute('ToName', conn.toPinName);
    }
    el.appendChild(connEl);
  }
  return el;
}

/**
 * 找到所有根节点（森林中的树）
 */
function findAllRootNodes(tree: Tree): TreeNode[] {
  const roots: TreeNode[] = [];

  // 首先添加主根节点
  const mainRoot = tree.rootId ? tree.nodes.get(tree.rootId) : undefined;
  if (mainRoot) roots.push(mainRoot);

  // 查找其他没有父节点的节点（森林中的其他树）
  const childNodeIds = new Set(tree.connections.map(c => c.childNodeId));
  tree.nodes.forEach((node, id) => {
    if (id !== tree.rootId && !childNodeIds.has(id)) {
      roots.push(node);
    }
  });

  return roots;
}

/**
 * 格式化 XML 字符串，添加缩进和换行
 */
function formatXml(xml: string): string {
  let formatted = '';
  let indent = '';
  const tab = '  ';

  // 处理自闭合标签的空格
  xml = xml.replace(/([^\s])\/>/g, '$1 />');

  // 使用正则表达式匹配标签
  const parts = xml.split(/(?=<)|(?<=>)/).filter(p => p.trim());

  parts.forEach((part) => {
    if (part.startsWith('</')) {
      // 结束标签
      indent = indent.substring(tab.length);
      formatted += indent + part + '\n';
    } else if (part.startsWith('<') && !part.endsWith('/>') && !part.startsWith('<?') && !part.startsWith('<!')) {
      // 开始标签（且非自闭合）
      formatted += indent + part + '\n';
      indent += tab;
    } else {
      // 自闭合标签、内容或声明
      formatted += indent + part + '\n';
    }
  });

  // 将十进制实体转换为十六进制格式，保持与原文件一致
  formatted = formatted.replace(/&#(\d+);/g, (_, dec) => {
    return '&#x' + parseInt(dec).toString(16).toUpperCase() + ';';
  });

  return formatted.trim();
}

/**
 * 将树文件序列化为编辑器版 XML
 */
export function serializeTreeForEditor(tree: Tree): string {
  const doc = document.implementation.createDocument(null, tree.name, null);
  const root = doc.documentElement;
  root.setAttribute('IsEditor', '');

  // 序列化所有根节点（支持森林）
  const rootNodes = findAllRootNodes(tree);
  for (const rootNode of rootNodes) {
    const isMainRoot = rootNode.id === tree.rootId;
    root.appendChild(serializeNodeForEditor(
      rootNode, doc, tree.nodes, tree.connections,
      isMainRoot ? tree.sharedVariables : undefined,
      isMainRoot ? tree.localVariables : undefined,
      undefined,
      isMainRoot ? { inputs: tree.inputs, outputs: tree.outputs } : undefined
    ));
  }

  // 序列化 DataConnections
  const dataConnsEl = serializeDataConnections(tree.dataConnections, tree.nodes, doc, true);
  if (dataConnsEl) root.appendChild(dataConnsEl);

  const serializer = new XMLSerializer();
  const xmlString = serializer.serializeToString(doc);
  // UTF-8 BOM + CRLF 换行符
  const BOM = '\uFEFF';
  return BOM + ('<?xml version="1.0" encoding="utf-8"?>\n' + formatXml(xmlString)).replace(/\n/g, '\r\n');
}

/**
 * 收集主树中所有节点的 ID（深度优先遍历）
 */
function collectMainTreeNodeIds(
  rootId: string,
  nodeMap: Map<string, TreeNode>,
  connections: TreeConnection[]
): Set<string> {
  const nodeIds = new Set<string>();

  function traverse(nodeId: string) {
    const node = nodeMap.get(nodeId);
    if (!node || node.disabled) return;

    nodeIds.add(nodeId);

    // 遍历子节点
    const childConns = connections.filter(c => c.parentNodeId === nodeId);
    for (const conn of childConns) {
      traverse(conn.childNodeId);
    }
  }

  traverse(rootId);
  return nodeIds;
}

/**
 * 计算主树中被引用的变量
 */
function collectReferencedVariables(
  mainTreeNodeIds: Set<string>,
  nodeMap: Map<string, TreeNode>
): { sharedRefs: Set<string>; localRefs: Set<string> } {
  const sharedRefs = new Set<string>();
  const localRefs = new Set<string>();

  for (const nodeId of mainTreeNodeIds) {
    const node = nodeMap.get(nodeId);
    if (!node) continue;

    for (const pin of node.pins) {
      if (pin.binding.type === 'pointer') {
        if (pin.binding.isLocal) {
          localRefs.add(pin.binding.variableName);
        } else {
          sharedRefs.add(pin.binding.variableName);
        }
      }

      // 检查 Vector Index 引用
      if (pin.vectorIndex && pin.vectorIndex.type === 'pointer') {
        if (pin.vectorIndex.isLocal) {
          localRefs.add(pin.vectorIndex.variableName);
        } else {
          sharedRefs.add(pin.vectorIndex.variableName);
        }
      }
    }
  }

  return { sharedRefs, localRefs };
}

/**
 * 计算运行时 UID（深度优先遍历）
 * 返回 nodeId -> UID 的映射
 */
function calculateRuntimeUIDs(
  rootId: string,
  nodeMap: Map<string, TreeNode>,
  connections: TreeConnection[]
): Map<string, number> {
  const uidMap = new Map<string, number>();
  let uid = 1;

  // Build parent->children index for performance
  const connectionsByParent = new Map<string, TreeConnection[]>();
  for (const conn of connections) {
    const list = connectionsByParent.get(conn.parentNodeId) || [];
    list.push(conn);
    connectionsByParent.set(conn.parentNodeId, list);
  }

  // 深度优先遍历，子节点按 X 坐标从左到右排序（与编辑器显示 UID 一致）
  function traverse(nodeId: string) {
    const node = nodeMap.get(nodeId);
    if (!node || node.disabled) return;

    uidMap.set(nodeId, uid++);

    const childConns = connectionsByParent.get(nodeId) || [];
    const sortedConns = [...childConns].sort((a, b) => {
      const nodeA = nodeMap.get(a.childNodeId);
      const nodeB = nodeMap.get(b.childNodeId);
      return (nodeA?.position.x ?? 0) - (nodeB?.position.x ?? 0);
    });
    for (const conn of sortedConns) {
      traverse(conn.childNodeId);
    }
  }

  traverse(rootId);
  return uidMap;
}

/**
 * 序列化节点为运行时 XML（使用 UID）
 */
function serializeNodeForRuntimeWithUID(
  node: TreeNode,
  doc: Document,
  nodeMap: Map<string, TreeNode>,
  connections: TreeConnection[],
  uidMap: Map<string, number>,
  sharedVars?: Variable[],
  localVars?: Variable[]
): Element | null {
  if (node.disabled) return null;

  const el = doc.createElement('Node');
  el.setAttribute('Class', node.type);

  // 额外属性 (Connection 等)
  if (node.extraAttrs) {
    for (const [key, value] of Object.entries(node.extraAttrs)) {
      if (key !== 'Return') {
        el.setAttribute(key, value);
      }
    }
  }

  // Return 属性 (Success/Failure/Invert)
  if (node.returnType && node.returnType !== 'Default') {
    el.setAttribute('Return', node.returnType);
  } else if (node.extraAttrs?.Return) {
    el.setAttribute('Return', node.extraAttrs.Return);
  }

  // 如果是 Root 节点，添加被引用的 Shared 和 Local 变量（只有非空时才添加）
  if (node.type === 'Root') {
    if (sharedVars && sharedVars.length > 0) {
      const sharedEl = doc.createElement('Shared');
      for (const v of sharedVars) {
        sharedEl.setAttribute(v.name, serializeVariable(v));
      }
      el.appendChild(sharedEl);
    }
    // 只有有被引用的 Local 变量时才添加 Local 元素
    if (localVars && localVars.length > 0) {
      const localEl = doc.createElement('Local');
      for (const v of localVars) {
        localEl.setAttribute(v.name, serializeVariable(v));
      }
      el.appendChild(localEl);
    }
  }

  // Pin 值（跳过禁用的）
  const inputEl = doc.createElement('Input');
  const outputEl = doc.createElement('Output');
  let hasInput = false;
  let hasOutput = false;

  const nodeDef = useNodeDefinitionStore.getState().getDefinition(node.type);

  for (const pin of node.pins) {
    const value = serializePinValue(pin, false);
    if (!value) continue;

    const isBasePin = nodeDef?.pins?.some(p => p.name === pin.name) ?? false;

    if (isBasePin) {
      el.setAttribute(pin.name, value);
    } else {
      if (pin.isInput) {
        inputEl.setAttribute(pin.name, value);
        hasInput = true;
      } else {
        outputEl.setAttribute(pin.name, value);
        hasOutput = true;
      }
    }
  }

  if (hasInput) el.appendChild(inputEl);
  if (hasOutput) el.appendChild(outputEl);

  // 递归序列化子节点（按 X 坐标从左到右排序）
  const childConnections = connections.filter(c => c.parentNodeId === node.id);
  const sortedChildConns = [...childConnections].sort((a, b) => {
    const nodeA = nodeMap.get(a.childNodeId);
    const nodeB = nodeMap.get(b.childNodeId);
    return (nodeA?.position.x ?? 0) - (nodeB?.position.x ?? 0);
  });
  for (const conn of sortedChildConns) {
    const childNode = nodeMap.get(conn.childNodeId);
    if (childNode) {
      const childEl = serializeNodeForRuntimeWithUID(childNode, doc, nodeMap, connections, uidMap);
      if (childEl) el.appendChild(childEl);
    }
  }

  return el;
}

/**
 * 序列化运行时版 DataConnections（使用 UID，只包含主树内的连接）
 */
function serializeDataConnectionsForRuntime(
  dataConnections: DataConnection[],
  mainTreeNodeIds: Set<string>,
  uidMap: Map<string, number>,
  doc: Document
): Element | null {
  // 过滤：只保留主树内部的连接
  const filteredConns = dataConnections.filter(conn =>
    mainTreeNodeIds.has(conn.fromNodeId) && mainTreeNodeIds.has(conn.toNodeId)
  );

  if (filteredConns.length === 0) return null;

  const el = doc.createElement('DataConnections');
  for (const conn of filteredConns) {
    const connEl = doc.createElement('DataConnection');
    const fromUID = uidMap.get(conn.fromNodeId);
    const toUID = uidMap.get(conn.toNodeId);

    if (fromUID !== undefined) connEl.setAttribute('FromUID', String(fromUID));
    connEl.setAttribute('FromName', conn.fromPinName);
    if (toUID !== undefined) connEl.setAttribute('ToUID', String(toUID));
    connEl.setAttribute('ToName', conn.toPinName);
    el.appendChild(connEl);
  }
  return el;
}

/**
 * 将树文件序列化为运行时版 XML
 */
export function serializeTreeForRuntime(tree: Tree): string {
  const doc = document.implementation.createDocument(null, tree.name, null);
  const root = doc.documentElement;

  // 1. 识别并标记主树节点
  const rootId = tree.rootId || '';
  const mainTreeNodeIds = collectMainTreeNodeIds(rootId, tree.nodes, tree.connections);

  // 2. 计算被引用的变量
  const { sharedRefs, localRefs } = collectReferencedVariables(mainTreeNodeIds, tree.nodes);
  const referencedSharedVars = tree.sharedVariables.filter(v => sharedRefs.has(v.name));
  const referencedLocalVars = tree.localVariables.filter(v => localRefs.has(v.name));

  // 3. 计算 UID (只针对主树)
  const uidMap = calculateRuntimeUIDs(rootId, tree.nodes, tree.connections);

  // 4. 序列化主树根节点
  const rootNode = tree.nodes.get(rootId);
  if (rootNode) {
    const nodeEl = serializeNodeForRuntimeWithUID(
      rootNode, doc, tree.nodes, tree.connections, uidMap,
      referencedSharedVars, referencedLocalVars
    );
    if (nodeEl) root.appendChild(nodeEl);
  }

  // 5. 序列化 DataConnections（只包含主树内的连接）
  const dataConnsEl = serializeDataConnectionsForRuntime(
    tree.dataConnections, mainTreeNodeIds, uidMap, doc
  );
  if (dataConnsEl) root.appendChild(dataConnsEl);

  const serializer = new XMLSerializer();
  const xmlString = serializer.serializeToString(doc);
  // UTF-8 BOM + CRLF 换行符
  const BOM = '\uFEFF';
  return BOM + ('<?xml version="1.0" encoding="utf-8"?>\n' + formatXml(xmlString)).replace(/\n/g, '\r\n');
}
