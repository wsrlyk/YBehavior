# YBehavior Editor 重构方案

## 技术栈：Electron + React

---

## 一、技术选型

| 类别 | 技术 | 说明 |
|------|------|------|
| **框架** | Electron | 桌面应用容器 |
| **前端** | React 18 + TypeScript | 类型安全、生态丰富 |
| **节点编辑器** | react-flow | 专业节点编辑器，内置虚拟化 |
| **状态管理** | Zustand | 轻量、简单、支持持久化 |
| **样式** | TailwindCSS | 原子化 CSS，快速开发 |
| **UI 组件** | shadcn/ui | 现代美观、可定制 |
| **图标** | Lucide React | 统一图标库 |
| **网络通信** | WebSocket | 与 C++ 后端实时通信 |
| **构建工具** | Vite | 快速开发、热更新 |
| **打包** | electron-builder | 跨平台打包 |

---

## 二、项目结构

```
ybehavior-editor/
├── electron/                    # Electron 主进程
│   ├── main.ts                  # 主进程入口
│   ├── preload.ts               # 预加载脚本
│   └── ipc/                     # IPC 通信
│       └── fileHandlers.ts      # 文件操作
│
├── src/                         # React 渲染进程
│   ├── main.tsx                 # 渲染进程入口
│   ├── App.tsx                  # 根组件
│   │
│   ├── components/              # UI 组件
│   │   ├── layout/              # 布局组件
│   │   │   ├── MainLayout.tsx
│   │   │   ├── Sidebar.tsx
│   │   │   ├── TabBar.tsx
│   │   │   └── StatusBar.tsx
│   │   │
│   │   ├── editor/              # 编辑器组件
│   │   │   ├── TreeEditor.tsx       # 行为树编辑器
│   │   │   ├── FSMEditor.tsx        # 状态机编辑器
│   │   │   ├── NodePanel.tsx        # 节点面板
│   │   │   └── PropertyPanel.tsx    # 属性面板
│   │   │
│   │   ├── nodes/               # 节点组件
│   │   │   ├── BaseNode.tsx
│   │   │   ├── ActionNode.tsx
│   │   │   ├── ConditionNode.tsx
│   │   │   ├── CompositeNode.tsx
│   │   │   ├── DecoratorNode.tsx
│   │   │   └── FSMStateNode.tsx
│   │   │
│   │   ├── edges/               # 连线组件
│   │   │   ├── TreeEdge.tsx
│   │   │   └── FSMEdge.tsx
│   │   │
│   │   └── ui/                  # 通用 UI 组件
│   │       ├── Button.tsx
│   │       ├── Input.tsx
│   │       ├── Select.tsx
│   │       └── Modal.tsx
│   │
│   ├── stores/                  # 状态管理
│   │   ├── editorStore.ts       # 编辑器状态
│   │   ├── treeStore.ts         # 行为树数据
│   │   ├── fsmStore.ts          # 状态机数据
│   │   └── debugStore.ts        # 调试状态
│   │
│   ├── services/                # 服务层
│   │   ├── fileService.ts       # 文件操作
│   │   ├── networkService.ts    # 网络通信
│   │   └── debugService.ts      # 调试服务
│   │
│   ├── models/                  # 数据模型
│   │   ├── Node.ts
│   │   ├── Connection.ts
│   │   ├── Tree.ts
│   │   ├── FSM.ts
│   │   └── Variable.ts
│   │
│   ├── hooks/                   # 自定义 Hooks
│   │   ├── useTree.ts
│   │   ├── useFSM.ts
│   │   ├── useUndo.ts
│   │   └── useDebug.ts
│   │
│   ├── utils/                   # 工具函数
│   │   ├── xmlParser.ts         # XML 解析（兼容现有格式）
│   │   ├── xmlSerializer.ts     # XML 序列化
│   │   └── treeLayout.ts        # 树布局算法
│   │
│   └── styles/                  # 样式
│       └── globals.css
│
├── package.json
├── tsconfig.json
├── vite.config.ts
├── electron-builder.json
└── tailwind.config.js
```

---

## 三、核心模块设计

### 3.1 术语定义

| 术语 | 说明 |
|------|------|
| **Node** | 节点（保留） |
| **Tree** | 行为树（保留） |
| **FSM** | 状态机（保留） |
| **Variable** | 树的变量（SharedVariables / LocalVariables） |
| **Pin** | 节点/树的输入输出端口，可传入常量或引用 Variable |
| **Connection** | 节点之间的连线 |
| **PinBinding** | Pin 的绑定值（常量或 Variable 引用） |

### 3.2 数据模型

```typescript
// models/Pin.ts
type PinDirection = 'input' | 'output' | 'inout';
type PinValueType = 'int' | 'float' | 'bool' | 'string' | 'vector3' | 'entity';

interface Pin {
  id: string;
  name: string;
  direction: PinDirection;
  valueType: PinValueType;
  binding: PinBinding;
}

// Pin 绑定：常量值 或 引用树的 Variable
type PinBinding = 
  | { type: 'constant'; value: any }
  | { type: 'variable'; variableName: string };

// models/Variable.ts
interface Variable {
  name: string;
  valueType: PinValueType;
  isShared: boolean;        // SharedVariable vs LocalVariable
  defaultValue?: any;
}

// models/Node.ts
interface Node {
  id: string;               // GUID
  uid: number;              // UID
  type: string;             // 节点类型
  name: string;             // 节点名称
  nickname?: string;        // 昵称
  comment?: string;         // 注释
  position: { x: number; y: number };
  disabled?: boolean;
  pins: Pin[];              // 节点的所有 Pin
}

interface TreeNode extends Node {
  parentId?: string;
  childrenIds: string[];
}

interface FSMState extends Node {
  isEntry?: boolean;
  isExit?: boolean;
}

// models/Connection.ts
interface Connection {
  id: string;
  sourceNodeId: string;
  sourceHandle: string;     // 子节点连接点
  targetNodeId: string;
  targetHandle: string;     // 父节点连接点
}

// models/Tree.ts
interface Tree {
  name: string;
  path: string;
  nodes: Map<string, TreeNode>;
  connections: Connection[];
  sharedVariables: Variable[];   // 共享变量
  localVariables: Variable[];    // 局部变量
  inputPins: Pin[];              // 树的输入 Pin
  outputPins: Pin[];             // 树的输出 Pin
  comments: Comment[];
}

// models/FSM.ts
interface FSM {
  name: string;
  path: string;
  states: Map<string, FSMState>;
  transitions: Transition[];
  sharedVariables: Variable[];
  localVariables: Variable[];
  comments: Comment[];
}

interface Transition {
  id: string;
  fromStateId: string;
  toStateId: string;
  conditions: TransitionCondition[];
}
```

### 3.3 状态管理 (Zustand)

```typescript
// stores/treeStore.ts
interface TreeStore {
  // 数据
  trees: Map<string, Tree>;
  activeTreeId: string | null;
  
  // 文件操作
  openTree: (path: string) => Promise<void>;
  saveTree: (id: string) => Promise<void>;
  closeTree: (id: string) => void;
  
  // 节点操作
  addNode: (treeId: string, node: TreeNode) => void;
  removeNode: (treeId: string, nodeId: string) => void;
  updateNode: (treeId: string, nodeId: string, updates: Partial<TreeNode>) => void;
  moveNode: (treeId: string, nodeId: string, position: Position) => void;
  
  // 连接操作
  connect: (treeId: string, connection: Connection) => void;
  disconnect: (treeId: string, connectionId: string) => void;
  
  // Variable 操作
  addVariable: (treeId: string, variable: Variable) => void;
  removeVariable: (treeId: string, variableName: string) => void;
  updateVariable: (treeId: string, variableName: string, updates: Partial<Variable>) => void;
  
  // Pin 绑定操作
  bindPin: (treeId: string, nodeId: string, pinId: string, binding: PinBinding) => void;
  
  // 撤销/重做
  undo: () => void;
  redo: () => void;
  canUndo: boolean;
  canRedo: boolean;
}
```

### 3.4 网络通信

```typescript
// services/networkService.ts
class NetworkService {
  private ws: WebSocket | null = null;
  private handlers: Map<string, Function> = new Map();
  
  connect(host: string, port: number): Promise<void>;
  disconnect(): void;
  
  // 调试相关
  startDebug(treeName: string): void;
  stopDebug(): void;
  stepOver(): void;
  continue(): void;
  
  // 事件监听
  onNodeStateChange(callback: (nodeId: string, state: NodeState) => void): void;
  onBreakpoint(callback: (nodeId: string) => void): void;
  onLog(callback: (message: string) => void): void;
}
```

### 3.5 节点编辑器 (react-flow)

```tsx
// components/editor/TreeEditor.tsx
import ReactFlow, {
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
} from 'reactflow';

const nodeTypes = {
  action: ActionNode,
  condition: ConditionNode,
  composite: CompositeNode,
  decorator: DecoratorNode,
};

export function TreeEditor({ treeId }: { treeId: string }) {
  const tree = useTreeStore(state => state.trees.get(treeId));
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  
  return (
    <ReactFlow
      nodes={nodes}
      edges={edges}
      nodeTypes={nodeTypes}
      onNodesChange={onNodesChange}
      onEdgesChange={onEdgesChange}
      onConnect={onConnect}
      fitView
    >
      <Background />
      <Controls />
      <MiniMap />
    </ReactFlow>
  );
}
```

---

## 四、迁移策略

### 阶段一：基础框架 (1-2 周)
- [ ] 搭建 Electron + React + Vite 项目
- [ ] 配置 TailwindCSS + shadcn/ui
- [ ] 实现基础布局（侧边栏、标签栏、状态栏）
- [ ] 集成 react-flow

### 阶段二：文件兼容 (1 周)
- [ ] 实现 XML 解析器（兼容 .tree/.fsm 格式）
- [ ] 实现 XML 序列化器
- [ ] 文件打开/保存功能

### 阶段三：行为树编辑器 (2 周)
- [ ] 实现各类节点组件
- [ ] 实现连线逻辑
- [ ] 实现拖拽添加节点
- [ ] 实现属性面板
- [ ] 实现变量编辑

### 阶段四：状态机编辑器 (1 周)
- [ ] 实现 FSM 状态节点
- [ ] 实现状态转换连线
- [ ] 实现嵌套状态机

### 阶段五：调试功能 (1 周)
- [ ] 实现 WebSocket 通信
- [ ] 实现断点设置
- [ ] 实现运行时状态显示
- [ ] 实现日志面板

### 阶段六：完善功能 (1 周)
- [ ] 撤销/重做
- [ ] 复制/粘贴
- [ ] 搜索功能
- [ ] 快捷键
- [ ] 主题切换

---

## 五、关键优势

| 对比项 | 现有 WPF | 新 Electron + React |
|--------|----------|---------------------|
| **虚拟化** | 需手动实现，复杂 | react-flow 内置，开箱即用 |
| **开发效率** | 编译慢，调试繁琐 | 热更新，秒级刷新 |
| **UI 美观** | 需大量自定义 | TailwindCSS + shadcn 现代美观 |
| **跨平台** | 仅 Windows | Windows/Mac/Linux |
| **维护性** | MVVM 混乱 | 清晰的组件化架构 |
| **生态** | 有限 | npm 海量组件 |
| **人才** | WPF 开发者稀缺 | 前端开发者众多 |

---

## 六、风险与应对

| 风险 | 应对措施 |
|------|----------|
| XML 格式兼容 | 编写完整的解析/序列化测试用例 |
| 性能问题 | react-flow 已验证支持上千节点 |
| Electron 体积大 | 使用 electron-builder 优化，约 80MB |
| 学习成本 | React 生态成熟，文档丰富 |

---

## 七、预计工期

**总计：7-9 周**

可并行开发，如果有 2 人，可缩短至 4-5 周。
