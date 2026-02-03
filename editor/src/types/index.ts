// ==================== 基础类型 ====================

/** 支持的数据类型 */
export type ValueType =
  | 'int'
  | 'float'
  | 'bool'
  | 'string'
  | 'vector3'
  | 'entity'
  | 'ulong'
  | 'enum';

/** 数量类型 */
export type CountType = 'scalar' | 'list';

/** 位置 */
export interface Position {
  x: number;
  y: number;
}

// ==================== Variable ====================

export interface Variable {
  name: string;
  valueType: ValueType;
  countType: CountType;
  isLocal: boolean;
  defaultValue: string;
}

// ==================== Pin ====================

export type PinBindingType = 'const' | 'pointer';
export type EnableType = 'fixed' | 'enable' | 'disable';

// pointer 类型 + 空 variableName = 数据连接状态（等待连接或已连接）
export type PinBinding =
  | { type: 'const'; value: string }
  | { type: 'pointer'; variableName: string; isLocal: boolean };

export interface Pin {
  name: string;
  valueType: ValueType;
  countType: CountType;
  bindingType: PinBindingType;
  binding: PinBinding;
  enableType: EnableType;
  isInput: boolean;
  vectorIndex?: PinBinding;
  enumValues?: string[];
  allowedValueTypes: ValueType[];
  vTypeGroup?: number;  // 类型联动组，同组 Pin 类型变化时同步
  cTypeGroup?: number;  // 数量联动组，同组 Pin 数组/标量变化时同步
  isCountTypeFixed?: boolean; // 是否固定数量类型（不可切换数组/标量）
  isBindingTypeFixed?: boolean; // 是否固定绑定类型（不可切换常量/引用）
}

// ==================== Node ====================

export type NodeCategory = 'composite' | 'decorator' | 'action' | 'condition';

/** 节点返回类型处理 */
export type ReturnType = 'Default' | 'Invert' | 'Success' | 'Failure';

export interface TreeNode {
  id: string;
  guid: number;  // 文件中保存的唯一标识，不变
  uid?: number;  // 显示用的 UID，深度遍历计算
  type: string;
  category: NodeCategory;
  nickname?: string;
  comment?: string;
  returnType?: ReturnType;
  position: Position;
  disabled: boolean;
  isFolded?: boolean;
  hasConditionConnector?: boolean;
  pins: Pin[];
  parentId?: string;
  childrenIds: string[];
  // 额外属性（如 Connection 等）
  extraAttrs?: Record<string, string>;
}

// ==================== Connection ====================

export interface TreeConnection {
  id: string;
  parentNodeId: string;
  parentConnector: string;
  childNodeId: string;
}

export interface DataConnection {
  id: string;
  fromNodeId: string;
  fromPinName: string;
  toNodeId: string;
  toPinName: string;
}

// ==================== Tree ====================

export interface TreePin extends Pin { }

export interface Comment {
  id: string;
  content: string;
  position: Position;
  size: { width: number; height: number };
}

export interface Tree {
  name: string;
  path: string;
  nodes: Map<string, TreeNode>;
  rootId: string;
  connections: TreeConnection[];
  dataConnections: DataConnection[];
  sharedVariables: Variable[];
  localVariables: Variable[];
  inputPins: TreePin[];
  outputPins: TreePin[];
  comments: Comment[];
}
