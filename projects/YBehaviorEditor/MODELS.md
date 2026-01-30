# YBehavior Editor - 完整数据模型定义

基于现有 C# 代码和 XML 文件格式分析得出。

---

## 一、基础类型

```typescript
// ==================== 值类型 ====================

/** 支持的数据类型 */
type ValueType = 
  | 'int'       // 整数
  | 'float'     // 浮点数
  | 'bool'      // 布尔
  | 'string'    // 字符串
  | 'vector3'   // 三维向量 (x, y, z)
  | 'entity'    // 游戏实体引用
  | 'ulong'     // 无符号64位整数
  | 'enum';     // 枚举

/** 数量类型 */
type CountType = 
  | 'scalar'    // 单值
  | 'list';     // 数组

/** 位置 */
interface Position {
  x: number;
  y: number;
}

/** Vector3 值 */
interface Vector3 {
  x: number;
  y: number;
  z: number;
}
```

---

## 二、Variable（树的变量）

```typescript
// ==================== Variable ====================

/**
 * 树的变量（SharedVariable 或 LocalVariable）
 * 存储在树级别，可被节点的 Pin 引用
 */
interface Variable {
  /** 变量名 */
  name: string;
  
  /** 值类型 */
  valueType: ValueType;
  
  /** 数量类型 */
  countType: CountType;
  
  /** 是否为局部变量（false = SharedVariable, true = LocalVariable） */
  isLocal: boolean;
  
  /** 默认值（字符串形式，数组用 | 分隔） */
  defaultValue: string;
}

/**
 * Variable 集合
 */
interface VariableCollection {
  sharedVariables: Variable[];
  localVariables: Variable[];
}
```

---

## 三、Pin（节点/树的端口）

```typescript
// ==================== Pin ====================

/** Pin 的绑定类型 */
type PinBindingType = 
  | 'const'     // 常量值
  | 'pointer';  // 引用 Variable

/** Pin 的启用状态 */
type EnableType = 
  | 'fixed'     // 固定启用
  | 'enable'    // 当前启用
  | 'disable';  // 当前禁用

/**
 * Pin 绑定值
 */
type PinBinding = 
  | { type: 'const'; value: string }
  | { type: 'pointer'; variableName: string; isLocal: boolean };

/**
 * 数组索引绑定（当 Pin 引用数组变量但自身是标量时）
 */
interface VectorIndex {
  binding: PinBinding;
}

/**
 * Pin - 节点或树的输入/输出端口
 */
interface Pin {
  /** Pin 名称 */
  name: string;
  
  /** 值类型 */
  valueType: ValueType;
  
  /** 数量类型 */
  countType: CountType;
  
  /** 绑定类型 */
  bindingType: PinBindingType;
  
  /** 绑定值 */
  binding: PinBinding;
  
  /** 启用状态 */
  enableType: EnableType;
  
  /** 是否为输入（false = 输出） */
  isInput: boolean;
  
  /** 可选的数组索引（当引用数组但自身是标量时） */
  vectorIndex?: VectorIndex;
  
  /** 可选的枚举值列表（当 valueType 为 enum 时） */
  enumValues?: string[];
  
  /** 允许的值类型列表（用于类型切换） */
  allowedValueTypes: ValueType[];
  
  /** 是否锁定绑定类型 */
  lockBindingType: boolean;
  
  /** 是否锁定数量类型 */
  lockCountType: boolean;
}

/**
 * 树的输入输出 Pin（用于 SubTree）
 */
interface TreePin extends Pin {
  // 继承 Pin 的所有属性
}
```

---

## 四、Node（节点）

```typescript
// ==================== Node Base ====================

/**
 * 节点基类
 */
interface Node {
  /** 全局唯一标识符 */
  id: string;
  
  /** 用户可见的唯一标识符 */
  uid: number;
  
  /** 节点类型名称 */
  type: string;
  
  /** 节点名称 */
  name: string;
  
  /** 用户自定义昵称 */
  nickname?: string;
  
  /** 注释 */
  comment?: string;
  
  /** 位置 */
  position: Position;
  
  /** 是否禁用 */
  disabled: boolean;
  
  /** 节点的所有 Pin */
  pins: Pin[];
  
  /** 调试断点 */
  debugPoint?: number;
}

// ==================== Tree Node Types ====================

/** 树节点类型 */
type TreeNodeType = 
  | 'root'
  | 'composite'
  | 'decorator'
  | 'action'
  | 'condition';

/**
 * 树节点
 */
interface TreeNode extends Node {
  /** 节点分类 */
  nodeType: TreeNodeType;
  
  /** 父节点 ID */
  parentId?: string;
  
  /** 子节点 ID 列表 */
  childrenIds: string[];
  
  /** 连接器定义 */
  connectors: Connector[];
}

/**
 * 连接器（定义节点可以连接子节点的位置）
 */
interface Connector {
  /** 连接器标识符 */
  identifier: string;
  
  /** 是否允许多个子节点 */
  allowMultiple: boolean;
  
  /** 连接的子节点 ID 列表 */
  childIds: string[];
  
  /** 连接注释（如 SwitchCase 的 case 值） */
  notes: string[];
}

// ==================== Specific Tree Nodes ====================

/** 根节点 */
interface RootNode extends TreeNode {
  type: 'Root';
  nodeType: 'root';
}

/** 序列节点 */
interface SequenceNode extends TreeNode {
  type: 'Sequence';
  nodeType: 'composite';
}

/** 选择节点 */
interface SelectorNode extends TreeNode {
  type: 'Selector';
  nodeType: 'composite';
}

/** 随机序列节点 */
interface RandomSequenceNode extends TreeNode {
  type: 'RandomSequence';
  nodeType: 'composite';
}

/** 随机选择节点 */
interface RandomSelectorNode extends TreeNode {
  type: 'RandomSelector';
  nodeType: 'composite';
}

/** 条件分支节点 */
interface IfThenElseNode extends TreeNode {
  type: 'IfThenElse';
  nodeType: 'composite';
  connectors: [
    { identifier: 'if'; allowMultiple: false },
    { identifier: 'then'; allowMultiple: false },
    { identifier: 'else'; allowMultiple: false }
  ];
}

/** Switch-Case 节点 */
interface SwitchCaseNode extends TreeNode {
  type: 'SwitchCase';
  nodeType: 'composite';
  // Pin: Switch (输入), Cases (数组)
  // 有一个 default 连接器
}

/** 事件处理节点 */
interface HandleEventNode extends TreeNode {
  type: 'HandleEvent';
  nodeType: 'composite';
  // Pin: Type (enum: Latest|Every), Events (字符串数组)
  // 输出 Pin: Int, Float, String, Ulong, Bool, Vector3, Entity, Current
}

/** 循环节点 */
interface ForNode extends TreeNode {
  type: 'For';
  nodeType: 'composite';
  connectors: [
    { identifier: 'init'; allowMultiple: false },
    { identifier: 'cond'; allowMultiple: false },
    { identifier: 'increment'; allowMultiple: false },
    { identifier: 'children'; allowMultiple: false }
  ];
}

/** ForEach 节点 */
interface ForEachNode extends TreeNode {
  type: 'ForEach';
  nodeType: 'decorator';
  // Pin: Collection (数组输入), Current (标量输出), BreakValue (bool)
}

/** Loop 节点 */
interface LoopNode extends TreeNode {
  type: 'Loop';
  nodeType: 'decorator';
  // Pin: Count (int输入), Current (int输出), BreakValue (bool)
}

/** 始终成功节点 */
interface AlwaysSuccessNode extends TreeNode {
  type: 'AlwaysSuccess';
  nodeType: 'decorator';
}

/** 始终失败节点 */
interface AlwaysFailureNode extends TreeNode {
  type: 'AlwaysFailure';
  nodeType: 'decorator';
}

/** 转换为布尔节点 */
interface ConvertToBoolNode extends TreeNode {
  type: 'ConvertToBool';
  nodeType: 'decorator';
  // Pin: Output (bool输出)
}

/** 计算器节点 */
interface CalculatorNode extends TreeNode {
  type: 'Calculator';
  nodeType: 'action';
  // Pin: Operator (enum: +|-|*|/), Output, Input1, Input2
}

/** 一元运算节点 */
interface UnaryOperationNode extends TreeNode {
  type: 'UnaryOperation';
  nodeType: 'action';
  // Pin: Operator (enum: ABS), Output, Input
}

/** 比较器节点 */
interface ComparerNode extends TreeNode {
  type: 'Comparer';
  nodeType: 'condition';
  // Pin: Operator (enum: ==|!=|>|<|>=|<=), Opl, Opr
}

/** 赋值节点 */
interface SetDataNode extends TreeNode {
  type: 'SetData';
  nodeType: 'action';
  // Pin: Target (输出), Source (输入)
}

/** 类型转换节点 */
interface ConvertNode extends TreeNode {
  type: 'Convert';
  nodeType: 'action';
  // Pin: Target (输出), Source (输入)
}

/** 随机数节点 */
interface RandomNode extends TreeNode {
  type: 'Random';
  nodeType: 'action';
  // Pin: Target (输出), Bound1, Bound2
}

/** 随机选择节点 */
interface RandomSelectNode extends TreeNode {
  type: 'RandomSelect';
  nodeType: 'action';
  // Pin: Input (数组), Output (标量)
}

/** 数组操作节点 */
interface ArrayOperationNode extends TreeNode {
  type: 'ArrayOperation';
  nodeType: 'action';
  // Pin: Operator (enum: APPEND|MERGE|...), Output, Input1, Input2
}

/** Vector3 分解节点 */
interface ProjectVector3Node extends TreeNode {
  type: 'ProjectVector3';
  nodeType: 'action';
  // Pin: Input (Vector3), X (float输出), Y (float输出), Z (float输出)
}

/** Vector3 组合节点 */
interface SetVector3Node extends TreeNode {
  type: 'SetVector3';
  nodeType: 'action';
  // Pin: X, Y, Z (float输入), Output (Vector3输出), Input (可选Vector3输入)
}

/** 分段函数节点 */
interface PiecewiseFunctionNode extends TreeNode {
  type: 'PiecewiseFunction';
  nodeType: 'action';
  // Pin: KeyPointX (数组), KeyPointY (数组), InputX, OutputY
}

/** 子树节点 */
interface SubTreeNode extends TreeNode {
  type: 'SubTree';
  nodeType: 'action';
  
  /** 引用的树名称 */
  treeName: string;
  
  /** 子树标识符 */
  identification: string;
  
  /** 子树的输入 Pin */
  inputPins: TreePin[];
  
  /** 子树的输出 Pin */
  outputPins: TreePin[];
}

/** 自定义动作节点（从外部配置加载） */
interface ActionNode extends TreeNode {
  type: string;  // 动态类名
  nodeType: 'action';
  
  /** 显示图标 */
  icon: string;
  
  /** Note 格式化字符串 */
  noteFormat: string;
}
```

---

## 五、Connection（连接）

```typescript
// ==================== Connection ====================

/**
 * 树节点之间的父子连接
 */
interface TreeConnection {
  id: string;
  
  /** 父节点 ID */
  parentNodeId: string;
  
  /** 父节点连接器标识符 */
  parentConnector: string;
  
  /** 子节点 ID */
  childNodeId: string;
  
  /** 连接注释 */
  note?: string;
}

/**
 * Pin 之间的数据连接
 */
interface DataConnection {
  id: string;
  
  /** 源节点 ID */
  fromNodeId: string;
  
  /** 源 Pin 名称 */
  fromPinName: string;
  
  /** 目标节点 ID */
  toNodeId: string;
  
  /** 目标 Pin 名称 */
  toPinName: string;
}
```

---

## 六、Tree（行为树）

```typescript
// ==================== Tree ====================

/**
 * 注释
 */
interface Comment {
  id: string;
  content: string;
  position: Position;
  size: { width: number; height: number };
}

/**
 * 行为树
 */
interface Tree {
  /** 树名称 */
  name: string;
  
  /** 文件路径 */
  path: string;
  
  /** 是否为编辑器格式（包含位置等信息） */
  isEditor: boolean;
  
  /** 所有节点 */
  nodes: Map<string, TreeNode>;
  
  /** 根节点 ID */
  rootId: string;
  
  /** 父子连接 */
  connections: TreeConnection[];
  
  /** 数据连接（Pin 之间） */
  dataConnections: DataConnection[];
  
  /** 共享变量 */
  sharedVariables: Variable[];
  
  /** 局部变量 */
  localVariables: Variable[];
  
  /** 树的输入 Pin（用于作为子树时） */
  inputPins: TreePin[];
  
  /** 树的输出 Pin（用于作为子树时） */
  outputPins: TreePin[];
  
  /** 注释 */
  comments: Comment[];
}
```

---

## 七、FSM（状态机）

```typescript
// ==================== FSM State ====================

/** FSM 状态类型 */
type FSMStateType = 
  | 'normal'    // 普通状态
  | 'meta'      // 嵌套状态机
  | 'entry'     // 入口
  | 'exit'      // 出口
  | 'any'       // 任意状态（用于创建从任意状态出发的转换）
  | 'upper';    // 上层状态（用于与父状态机交互）

/**
 * FSM 状态节点
 */
interface FSMState extends Node {
  /** 状态类型 */
  stateType: FSMStateType;
  
  /** 关联的行为树名称 */
  treeName?: string;
  
  /** 状态标识符 */
  identification?: string;
  
  /** 是否为默认状态 */
  isDefault: boolean;
}

/**
 * 嵌套状态机状态
 */
interface FSMMetaState extends FSMState {
  stateType: 'meta';
  
  /** 嵌套的状态机 */
  subMachine: FSMMachine;
}

// ==================== FSM Transition ====================

/** 转换类型 */
type TransitionType = 
  | 'normal'    // 普通转换
  | 'default'   // 默认转换（Entry -> Default State）
  | 'entry'     // 入口转换
  | 'exit';     // 出口转换

/**
 * 转换事件
 */
interface TransitionEvent {
  /** 事件名称 */
  event: string;
}

/**
 * 状态转换
 */
interface Transition {
  id: string;
  
  /** 源状态 ID（null 表示 Any） */
  fromStateId: string | null;
  
  /** 目标状态 ID */
  toStateId: string;
  
  /** 转换类型 */
  type: TransitionType;
  
  /** 触发事件列表 */
  events: TransitionEvent[];
}

/**
 * FSM 连接（状态之间的可视化连线）
 */
interface FSMConnection {
  id: string;
  
  /** 源状态 ID */
  fromStateId: string;
  
  /** 目标状态 ID */
  toStateId: string;
  
  /** 关联的转换列表 */
  transitionIds: string[];
}

// ==================== FSM Machine ====================

/**
 * 状态机
 */
interface FSMMachine {
  /** 层级（0 = 根状态机） */
  level: number;
  
  /** 所有状态 */
  states: Map<string, FSMState>;
  
  /** 入口状态 ID */
  entryStateId: string;
  
  /** 出口状态 ID */
  exitStateId: string;
  
  /** Any 状态 ID */
  anyStateId: string;
  
  /** Upper 状态 ID（非根状态机才有） */
  upperStateId?: string;
  
  /** 默认状态 ID */
  defaultStateId?: string;
  
  /** 本地转换（涉及 Entry/Exit 的转换） */
  localTransitions: Transition[];
  
  /** 连接 */
  connections: FSMConnection[];
}

/**
 * FSM 文件
 */
interface FSM {
  /** 名称 */
  name: string;
  
  /** 文件路径 */
  path: string;
  
  /** 根状态机 */
  rootMachine: FSMMachine;
  
  /** 全局转换（普通状态之间的转换） */
  globalTransitions: Transition[];
  
  /** 所有用户状态（扁平化，用于全局查找） */
  allUserStates: Map<string, FSMState>;
  
  /** 注释 */
  comments: Comment[];
}
```

---

## 八、XML 格式映射

### Pin 值格式（XML 属性值）

```
格式: [CountType][ValueType][BindingType][EnableType?] [Value] [VI [IndexBindingType] [IndexValue]]?

示例:
- "_IC 0"           -> 标量 Int 常量 0
- "_IP a"           -> 标量 Int 指针，引用共享变量 a
- "_Ip a"           -> 标量 Int 指针，引用局部变量 a  (小写 p = local)
- "IIC 1|2|3"       -> Int 数组 常量 [1, 2, 3]
- "_FP f VI c 0"    -> 标量 Float 指针，引用变量 f，数组索引为常量 0
- "_FPE f"          -> 标量 Float 指针，启用状态，引用变量 f
- "_FPD f"          -> 标量 Float 指针，禁用状态，引用变量 f

类型字符:
- I = Int, F = Float, B = Bool, V = Vector3, S = String, E = Enum, A = Entity, U = Ulong
- _ = 标量, [重复类型字符] = 数组 (如 II = Int数组)
- C = 常量, P = 指针 (大写 = shared, 小写 = local)
- E = 启用, D = 禁用
```

### DataConnection 格式（XML）

```xml
<DataConnections>
  <DataConnection FromGUID="22" FromName="Output" ToGUID="24" ToName="Input1" />
</DataConnections>
```

---

## 九、节点配置系统

### 设计思路

将**内置节点**和**外部节点**统一通过配置文件定义，同时支持内置节点的特殊逻辑通过**行为处理器**扩展。

```
┌─────────────────────────────────────────────────────────┐
│                    节点配置文件                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ builtin.xml │  │ actions.xml │  │ custom.xml  │     │
│  │  (内置节点)  │  │ (外部Action)│  │ (用户自定义) │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                  NodeDefinitionMgr                       │
│  - 加载所有配置文件                                       │
│  - 合并为统一的节点定义列表                               │
│  - 根据 className 查找节点定义                           │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                  NodeBehaviorRegistry                    │
│  - 注册内置节点的特殊行为处理器                           │
│  - 如: SubTree 的动态 Pin、SwitchCase 的子节点注释等     │
└─────────────────────────────────────────────────────────┘
```

### 节点定义（配置文件格式）

```typescript
// ==================== 节点定义 ====================

/** 节点类别 */
type NodeCategory = 
  | 'composite'   // 组合节点（多子节点）
  | 'decorator'   // 装饰节点（单子节点）
  | 'action'      // 动作节点（叶节点）
  | 'condition';  // 条件节点（叶节点）

/** 节点来源 */
type NodeSource = 
  | 'builtin'     // 内置节点
  | 'external';   // 外部配置

/**
 * Pin 定义
 */
interface PinDefinition {
  /** Pin 名称 */
  name: string;
  
  /** 允许的值类型（多个表示可切换） */
  valueTypes: ValueType[];
  
  /** 是否为数组 */
  isArray?: boolean;  // undefined = 可切换
  
  /** 是否为常量 */
  isConst?: boolean;  // undefined = 可切换
  
  /** 是否启用 */
  isEnable?: boolean; // undefined = 固定启用
  
  /** 是否为输入（false = 输出） */
  isInput: boolean;
  
  /** 默认值 */
  defaultValue?: string;
  
  /** 枚举参数（当 valueTypes 包含 enum 时） */
  enumValues?: string[];
  
  /** 值类型联动组（同组 Pin 类型同步变化） */
  vTypeGroup?: number;
  
  /** 数量类型联动组（同组 Pin 数组/标量同步变化） */
  cTypeGroup?: number;
}

/**
 * TypeMap 定义（Pin 类型联动规则）
 * 当 srcVariable 的值为 srcValue 时，desVariable 的类型变为 desType
 */
interface TypeMapDefinition {
  srcVariable: string;
  srcValue: string;
  desVariable: string;
  desType: string;  // 如 "_I" = 标量Int, "FF" = 数组Float
}

/**
 * 连接器定义
 */
interface ConnectorDefinition {
  /** 连接器标识符 */
  identifier: string;
  
  /** 是否允许多个子节点 */
  allowMultiple: boolean;
}

/**
 * 节点定义
 */
interface NodeDefinition {
  /** 节点类名（唯一标识） */
  className: string;
  
  /** 显示图标 */
  icon: string;
  
  /** 节点类别 */
  category: NodeCategory;
  
  /** 节点来源 */
  source: NodeSource;
  
  /** 层级（用于排序，数字越小越靠前） */
  hierarchy: number;
  
  /** Note 格式化字符串（{0}, {1}... 对应 Pin 值） */
  noteFormat?: string;
  
  /** Pin 定义列表 */
  pins: PinDefinition[];
  
  /** 连接器定义列表（仅 composite/decorator） */
  connectors?: ConnectorDefinition[];
  
  /** TypeMap 定义列表 */
  typeMaps?: TypeMapDefinition[];
  
  /** 特殊行为标识（用于关联行为处理器） */
  behavior?: string;
}
```

### 配置文件示例

#### builtin.xml（内置节点）

```xml
<?xml version="1.0" encoding="utf-8"?>
<Nodes>
  <!-- ========== Composite ========== -->
  <Node Class="Sequence" Icon="➜➜➜" Category="composite" Hierarchy="1">
    <Connector Id="children" Multiple="true" />
  </Node>
  
  <Node Class="Selector" Icon="？？？" Category="composite" Hierarchy="1">
    <Connector Id="children" Multiple="true" />
  </Node>
  
  <Node Class="IfThenElse" Icon="↙ ？↘" Category="composite" Hierarchy="2">
    <Connector Id="if" Multiple="false" />
    <Connector Id="then" Multiple="false" />
    <Connector Id="else" Multiple="false" />
  </Node>
  
  <Node Class="SwitchCase" Icon="↙↓↘" Category="composite" Hierarchy="2" 
        Note="{0} from {{ {1} }}" Behavior="switchCase">
    <Variable Name="Switch" ValueType="IFSU" IsConst="False" vTypeGroup="1" />
    <Variable Name="Cases" ValueType="IFSU" IsArray="True" vTypeGroup="1" />
    <Connector Id="children" Multiple="true" />
    <Connector Id="default" Multiple="false" />
  </Node>
  
  <Node Class="HandleEvent" Icon="[↙↓↘]" Category="composite" Hierarchy="2" Behavior="handleEvent">
    <Variable Name="Type" ValueType="E" IsConst="True" Value="Latest" Param="Latest|Every" />
    <Variable Name="Events" ValueType="S" IsArray="True" IsEnable="True" IsInput="True" />
    <Variable Name="Int" ValueType="I" IsConst="False" IsEnable="False" IsInput="False" />
    <Variable Name="Float" ValueType="F" IsConst="False" IsEnable="False" IsInput="False" />
    <Variable Name="String" ValueType="S" IsConst="False" IsEnable="False" IsInput="False" />
    <Variable Name="Ulong" ValueType="U" IsConst="False" IsEnable="False" IsInput="False" />
    <Variable Name="Bool" ValueType="B" IsConst="False" IsEnable="False" IsInput="False" />
    <Variable Name="Vector3" ValueType="V" IsConst="False" IsEnable="False" IsInput="False" />
    <Variable Name="Entity" ValueType="A" IsConst="False" IsEnable="False" IsInput="False" />
    <Variable Name="Current" ValueType="S" IsConst="False" IsEnable="False" IsInput="False" />
    <Connector Id="children" Multiple="true" />
  </Node>
  
  <Node Class="For" Icon="↺" Category="composite" Hierarchy="3">
    <Variable Name="BreakValue" ValueType="B" IsConst="False" IsEnable="False" Value="F" />
    <Connector Id="init" Multiple="false" />
    <Connector Id="cond" Multiple="false" />
    <Connector Id="increment" Multiple="false" />
    <Connector Id="children" Multiple="false" />
  </Node>
  
  <!-- ========== Decorator ========== -->
  <Node Class="ForEach" Icon="↺" Category="decorator" Hierarchy="1" Note="{{ {1} }} >> {0}">
    <Variable Name="Collection" ValueType="IFBSVAU" IsArray="True" vTypeGroup="1" />
    <Variable Name="Current" ValueType="IFBSVAU" IsConst="False" IsInput="False" vTypeGroup="1" />
    <Variable Name="BreakValue" ValueType="B" IsConst="False" IsEnable="False" Value="F" />
    <Connector Id="children" Multiple="false" />
  </Node>
  
  <Node Class="Loop" Icon="↺" Category="decorator" Hierarchy="1" Note="[0, {1}) >> {0}">
    <Variable Name="Count" ValueType="I" />
    <Variable Name="Current" ValueType="I" IsConst="False" IsInput="False" />
    <Variable Name="BreakValue" ValueType="B" IsConst="False" IsEnable="False" Value="F" />
    <Connector Id="children" Multiple="false" />
  </Node>
  
  <Node Class="AlwaysSuccess" Icon="T" Category="decorator" Hierarchy="2">
    <Connector Id="children" Multiple="false" />
  </Node>
  
  <Node Class="AlwaysFailure" Icon="F" Category="decorator" Hierarchy="2">
    <Connector Id="children" Multiple="false" />
  </Node>
  
  <Node Class="ConvertToBool" Icon="B" Category="decorator" Hierarchy="2">
    <Variable Name="Output" ValueType="B" IsConst="False" IsInput="False" />
    <Connector Id="children" Multiple="false" />
  </Node>
  
  <!-- ========== Action ========== -->
  <Node Class="Calculator" Icon="+-×÷" Category="action" Hierarchy="1" Note="{0} = {2} {1} {3}">
    <Variable Name="Output" ValueType="IF" IsConst="False" IsInput="False" vTypeGroup="1" cTypeGroup="1" />
    <Variable Name="Operator" ValueType="E" IsConst="True" Param="+|-|*|/" />
    <Variable Name="Opl" ValueType="IF" vTypeGroup="1" cTypeGroup="1" />
    <Variable Name="Opr" ValueType="IF" vTypeGroup="1" cTypeGroup="1" />
  </Node>
  
  <Node Class="SetData" Icon="y >> x" Category="action" Hierarchy="1" Note="{1} >> {0}">
    <Variable Name="Target" ValueType="IFBSVAU" IsConst="False" IsInput="False" vTypeGroup="1" cTypeGroup="1" />
    <Variable Name="Source" ValueType="IFBSVAU" vTypeGroup="1" cTypeGroup="1" />
  </Node>
  
  <Node Class="Comparer" Icon="x ？y" Category="condition" Hierarchy="1" Note="{1} {0} {2}">
    <Variable Name="Operator" ValueType="E" IsConst="True" Param="==|!=|>|<|>=|<=" />
    <Variable Name="Opl" ValueType="IFBSVAU" vTypeGroup="1" cTypeGroup="1" />
    <Variable Name="Opr" ValueType="IFBSVAU" vTypeGroup="1" cTypeGroup="1" />
  </Node>
  
  <Node Class="SubTree" Icon="🌲" Category="action" Hierarchy="1" Behavior="subTree">
    <Variable Name="Tree" ValueType="S" IsConst="True" />
    <Variable Name="Identification" ValueType="S" IsConst="True" IsEnable="False" />
  </Node>
  
  <!-- ... 更多内置节点 ... -->
</Nodes>
```

#### actions.xml（外部 Action，与现有格式兼容）

```xml
<?xml version="1.0" encoding="utf-8"?>
<Actions>
  <Action Class="GetNameAction" Note="{0} has {1} in {2}" Hierachy="21" Icon="🎀">
    <Variable Name="Var0" ValueType="IF" IsConst="False" vTypeGroup="1" cTypeGroup="1" />
    <Variable Name="Var1" ValueType="E" Param="Enum0|Enum1|Enum2" />
    <Variable Name="Var2" ValueType="S" IsArray="True" Value="1|2|3|4" />
    <Variable Name="Var3" ValueType="IF" IsConst="True" vTypeGroup="1" cTypeGroup="1" />
    <TypeMap SrcVariable="Var1" SrcValue="Enum0" DesVariable="Var0" DesType="_I" />
    <TypeMap SrcVariable="Var1" SrcValue="Enum1" DesVariable="Var0" DesType="_F" />
    <TypeMap SrcVariable="Var1" SrcValue="Enum2" DesVariable="Var0" DesType="FF" />
  </Action>
</Actions>
```

### 行为处理器（特殊逻辑）

```typescript
// ==================== 行为处理器 ====================

/**
 * 节点行为处理器接口
 * 用于实现内置节点的特殊逻辑
 */
interface NodeBehaviorHandler {
  /** 节点创建后的初始化 */
  onCreated?(node: TreeNode): void;
  
  /** 节点克隆后的处理 */
  onCloned?(node: TreeNode, source: TreeNode): void;
  
  /** Pin 值变化时的处理 */
  onPinValueChanged?(node: TreeNode, pin: Pin): void;
  
  /** 子节点连接变化时的处理 */
  onChildConnectionChanged?(node: TreeNode): void;
  
  /** 子节点禁用状态变化时的处理 */
  onChildDisabledChanged?(node: TreeNode, child: TreeNode): void;
  
  /** 校验节点有效性 */
  onCheckValid?(node: TreeNode): boolean;
  
  /** 自定义 Note 生成 */
  getNote?(node: TreeNode): string;
  
  /** 加载额外数据（从 XML） */
  onLoad?(node: TreeNode, xmlData: any): void;
  
  /** 保存额外数据（到 XML） */
  onSave?(node: TreeNode, xmlData: any): void;
}

/**
 * 行为处理器注册表
 */
const NodeBehaviorRegistry: Map<string, NodeBehaviorHandler> = new Map();

// ========== 内置行为处理器示例 ==========

/**
 * 注意：Variable（sharedVariables/localVariables）和 TreePin（inputPins/outputPins）
 * 直接存储在 Tree 对象中，由 Tree 的加载/保存逻辑处理，
 * 不需要通过 Root 节点的行为处理器。
 */

/** SubTree 行为：动态加载子树的 Input/Output Pin */
NodeBehaviorRegistry.set('subTree', {
  onPinValueChanged(node, pin) {
    if (pin.name === 'Tree') {
      // 根据选择的树文件，动态加载其 Input/Output Pin
      const treeName = pin.binding.type === 'const' ? pin.binding.value : '';
      loadSubTreePins(node, treeName);
    }
  },
  
  onCheckValid(node) {
    const treePin = node.pins.find(p => p.name === 'Tree');
    if (!treePin || !treePin.binding.value) {
      return false;
    }
    return true;
  }
});

/** SwitchCase 行为：根据 Cases 值更新子节点连接注释 */
NodeBehaviorRegistry.set('switchCase', {
  onPinValueChanged(node, pin) {
    if (pin.name === 'Cases') {
      refreshChildrenNotes(node, pin);
    }
  },
  
  onChildConnectionChanged(node) {
    const casesPin = node.pins.find(p => p.name === 'Cases');
    if (casesPin) {
      refreshChildrenNotes(node, casesPin);
    }
  }
});

/** HandleEvent 行为：根据 Events 值更新子节点连接注释 */
NodeBehaviorRegistry.set('handleEvent', {
  onPinValueChanged(node, pin) {
    if (pin.name === 'Events') {
      refreshChildrenNotes(node, pin);
    }
  },
  
  onChildConnectionChanged(node) {
    const eventsPin = node.pins.find(p => p.name === 'Events');
    if (eventsPin) {
      refreshChildrenNotes(node, eventsPin);
    }
  },
  
  getNote(node) {
    const eventsPin = node.pins.find(p => p.name === 'Events');
    if (eventsPin?.enableType === 'disable') {
      return 'All Events';
    }
    return eventsPin?.binding.value || '';
  }
});
```

### TypeScript 数据模型更新

```typescript
// ==================== 更新后的 Node 接口 ====================

interface TreeNode extends Node {
  /** 节点分类 */
  category: NodeCategory;
  
  /** 节点来源 */
  source: NodeSource;
  
  /** 节点定义引用 */
  definition: NodeDefinition;
  
  /** 父节点 ID */
  parentId?: string;
  
  /** 子节点 ID 列表 */
  childrenIds: string[];
  
  /** 连接器实例 */
  connectors: ConnectorInstance[];
  
  /** TypeMap 实例（运行时状态） */
  typeMap?: TypeMap;
}

interface ConnectorInstance {
  /** 连接器定义 */
  definition: ConnectorDefinition;
  
  /** 连接的子节点 ID 列表 */
  childIds: string[];
  
  /** 连接注释（如 SwitchCase 的 case 值） */
  notes: string[];
}

interface TypeMap {
  items: TypeMapDefinition[];
  
  /** 根据源 Pin 值获取目标 Pin 类型 */
  getTargetType(srcVariable: string, srcValue: string): { 
    desVariable: string; 
    countType: CountType; 
    valueType: ValueType; 
  } | null;
}
```

---

## 十、总结

| 概念 | 说明 |
|------|------|
| **Variable** | 树级别的变量（Shared/Local），存储数据 |
| **Pin** | 节点/树的端口，可绑定常量或引用 Variable |
| **TreeConnection** | 节点之间的父子关系连接 |
| **DataConnection** | Pin 之间的数据流连接 |
| **Tree** | 行为树，包含节点、连接、变量 |
| **FSMState** | 状态机的状态节点 |
| **Transition** | 状态之间的转换，包含触发事件 |
| **FSM** | 状态机，包含状态、转换、嵌套状态机 |
