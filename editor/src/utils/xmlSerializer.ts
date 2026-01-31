import type { Tree, TreeNode, Pin, TreeConnection, ValueType, Variable, DataConnection } from '../types';

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
    // 指针: P = 全局, p = 本地
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
  } else {
    value = pin.binding.variableName;
  }
  
  return code + ' ' + value;
}

/**
 * 序列化变量为 XML 属性格式
 * 格式规则同 Pin: 单体 _XC, 数组 XXc
 */
function serializeVariable(v: Variable): string {
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
  localVars?: Variable[]
): Element {
  const el = doc.createElement('Node');
  el.setAttribute('Class', node.type);
  
  // GUID (编辑器版需要)
  el.setAttribute('GUID', String(node.uid));
  
  // Connection 属性（在 GUID 之后，Pos 之前）
  if (node.extraAttrs?.Connection) {
    el.setAttribute('Connection', node.extraAttrs.Connection);
  }
  
  // Return 属性（在 Pos 之前）
  if (node.extraAttrs?.Return) {
    el.setAttribute('Return', node.extraAttrs.Return);
  }
  
  // 位置
  el.setAttribute('Pos', `${Math.round(node.position.x)},${Math.round(node.position.y)}`);
  
  if (node.nickname) el.setAttribute('NickName', node.nickname);
  if (node.comment) el.setAttribute('Comment', node.comment);
  if (node.disabled) el.setAttribute('Disabled', 'true');
  
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
  for (const pin of node.pins) {
    const value = serializePinValue(pin, true);
    if (value) el.setAttribute(pin.name, value);
  }
  
  // 递归序列化子节点
  const childConnections = connections.filter(c => c.parentNodeId === node.id);
  for (const conn of childConnections) {
    const childNode = nodeMap.get(conn.childNodeId);
    if (childNode) {
      el.appendChild(serializeNodeForEditor(childNode, doc, nodeMap, connections));
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
      if (fromNode) connEl.setAttribute('FromGUID', String(fromNode.uid));
      connEl.setAttribute('FromName', conn.fromPinName);
      if (toNode) connEl.setAttribute('ToGUID', String(toNode.uid));
      connEl.setAttribute('ToName', conn.toPinName);
    } else {
      // 运行时版使用 UID
      if (fromNode) connEl.setAttribute('FromUID', String(fromNode.uid));
      connEl.setAttribute('FromName', conn.fromPinName);
      if (toNode) connEl.setAttribute('ToUID', String(toNode.uid));
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
  const mainRoot = tree.nodes.get(tree.rootId);
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
  const tab = '  '; // 2 空格缩进
  
  // 先处理自闭合标签，确保 /> 前有空格
  xml = xml.replace(/([^\s])\/>/g, '$1 />');
  
  xml.split(/>\s*</).forEach((node) => {
    if (node.match(/^\/\w/)) {
      // 结束标签，减少缩进
      indent = indent.substring(tab.length);
    }
    formatted += indent + '<' + node + '>\n';
    if (node.match(/^<?\w[^>]*[^/]$/) && !node.startsWith('?')) {
      // 开始标签（非自闭合），增加缩进
      indent += tab;
    }
  });
  
  // 清理首尾多余的 < 和 >
  return formatted.substring(1, formatted.length - 2);
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
      isMainRoot ? tree.localVariables : undefined
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
  
  function traverse(nodeId: string) {
    const node = nodeMap.get(nodeId);
    if (!node || node.disabled) return;
    
    uidMap.set(nodeId, uid++);
    
    // 深度优先遍历子节点
    const childConns = connections.filter(c => c.parentNodeId === nodeId);
    for (const conn of childConns) {
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
  
  // 额外属性（Connection, Return 等）
  if (node.extraAttrs) {
    for (const [key, value] of Object.entries(node.extraAttrs)) {
      el.setAttribute(key, value);
    }
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
  for (const pin of node.pins) {
    const value = serializePinValue(pin, false);
    if (value) el.setAttribute(pin.name, value);
  }
  
  // 递归序列化子节点
  const childConnections = connections.filter(c => c.parentNodeId === node.id);
  for (const conn of childConnections) {
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
  
  // 1. 收集主树中所有节点 ID
  const mainTreeNodeIds = collectMainTreeNodeIds(tree.rootId, tree.nodes, tree.connections);
  
  // 2. 计算被引用的变量
  const { sharedRefs, localRefs } = collectReferencedVariables(mainTreeNodeIds, tree.nodes);
  const referencedSharedVars = tree.sharedVariables.filter(v => sharedRefs.has(v.name));
  const referencedLocalVars = tree.localVariables.filter(v => localRefs.has(v.name));
  
  // 3. 计算运行时 UID
  const uidMap = calculateRuntimeUIDs(tree.rootId, tree.nodes, tree.connections);
  
  // 4. 序列化主根节点
  const rootNode = tree.nodes.get(tree.rootId);
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
