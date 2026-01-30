# YBehavior Editor - Tauri 重构开发文档

## 项目概述

使用 Tauri + React + TypeScript 重构 YBehavior 行为树/状态机编辑器。

**目标**：替代原 WPF 编辑器，打包后约 15-25MB，用户无需安装额外依赖。

## 技术栈

- **前端**：React 19 + TypeScript + Vite
- **后端**：Tauri 2 (Rust)
- **UI**：TailwindCSS + @xyflow/react (节点编辑器)
- **状态管理**：Zustand

## 项目结构

```
editor/
├── src/
│   ├── components/
│   │   ├── NodeEditor.tsx      # React Flow 节点编辑器
│   │   ├── Sidebar.tsx         # 左侧文件列表和节点列表
│   │   └── PropertiesPanel.tsx # 右侧属性面板
│   ├── stores/
│   │   ├── editorStore.ts      # 主编辑器状态（树、选中、增删改）
│   │   └── nodeDefinitionStore.ts # 节点定义管理
│   ├── types/
│   │   ├── index.ts            # 核心类型（Tree, TreeNode, Pin, Variable 等）
│   │   └── nodeDefinition.ts   # 节点定义类型
│   ├── utils/
│   │   ├── xmlParser.ts        # XML 解析（.tree 文件）
│   │   ├── fileService.ts      # Tauri 文件操作封装
│   │   └── nodeDefinitionLoader.ts # 节点定义加载
│   ├── App.tsx                 # 主布局
│   └── App.css                 # TailwindCSS 入口
├── src-tauri/
│   └── src/lib.rs              # Rust 后端命令（read_file, write_file, list_files）
└── package.json
```

## 核心数据模型

参见 `projects/YBehaviorEditor/MODELS.md`

### 关键类型

```typescript
// 树节点
interface TreeNode {
  id: string;
  uid: number;
  type: string;           // 节点类名如 "Sequence", "Calculator"
  category: NodeCategory; // composite | decorator | action | condition
  position: Position;
  pins: Pin[];
  parentId?: string;
  childrenIds: string[];
}

// Pin（节点端口）
interface Pin {
  name: string;
  valueType: ValueType;   // int | float | bool | string | vector3 | entity | ulong | enum
  countType: CountType;   // scalar | list
  binding: PinBinding;    // { type: 'const', value } | { type: 'pointer', variableName, isLocal }
}

// 行为树
interface Tree {
  name: string;
  nodes: Map<string, TreeNode>;
  rootId: string;
  connections: TreeConnection[];
  dataConnections: DataConnection[];
  sharedVariables: Variable[];
  localVariables: Variable[];
}
```

## XML 解析

### Pin 值格式

```
"_XYZ value"
X = 值类型: I(int), F(float), B(bool), S(string), V(vector3), A(entity), U(ulong), E(enum)
Y = 数量类型: 单字符=scalar, 双字符如II/FF=list
Z = 绑定类型: C(const), P(pointer to shared), p(pointer to local)
后续: E(enable), D(disable)

示例:
"_IC 5"      = int scalar const 5
"_IP a"      = int scalar pointer to shared variable 'a'
"_Ip a"      = int scalar pointer to local variable 'a'
"IIC 1|2|3"  = int list const [1,2,3]
```

### 兼容性

- 旧配置文件可能没有 GUID，解析时自动生成临时 ID
- Root 节点分类为 decorator

## 节点定义系统

### 内置节点

定义在 `nodeDefinitionLoader.ts` 的 `getBuiltinDefinitions()`：
- Composite: Sequence, Selector, RandomSequence, RandomSelector, SwitchCase
- Decorator: Root, Loop, ForEach
- Action: Calculator, SetData, SubTree
- Condition: Comparer

### 外部节点

从 `actions.xml` 加载，格式：

```xml
<Action Class="GetTargetNameAction" Note="{0}" Hierachy="21">
  <Variable Name="Target" ValueType="A" IsArray="False" IsConst="False" IsEnable="False" Value=""/>
  <TypeMap SrcVariable="Var1" SrcValue="Enum0" DesVariable="Var0" DesType="_I" />
</Action>
```

## 状态管理 (Zustand)

### editorStore

```typescript
// 状态
workingDir: string | null;
treeFiles: string[];
currentTree: Tree | null;
selectedNodeIds: string[];
isDirty: boolean;

// 方法
setWorkingDir(dir)      // 设置工作目录并扫描文件
openTree(path)          // 打开 .tree 文件
addNode(node)           // 添加节点
removeNode(nodeId)      // 删除节点（同时删除相关连接）
updateNodePosition(nodeId, x, y)
updateNodeProperty(nodeId, updates)
addConnection(connection)
removeConnection(connectionId)
```

### nodeDefinitionStore

```typescript
definitions: Map<string, NodeDefinition>;
loadDefinitions(actionsXmlPath)  // 加载内置+外部节点定义
getDefinition(className)
getByCategory(category)
```

## 常用命令

```bash
npm run tauri dev      # 开发模式
npm run tauri build    # 打包（输出到 src-tauri/target/release/）
```

## 开发进度

### 第一阶段 ✅
- [x] XML 解析（.tree 文件加载）
- [x] 节点定义系统（builtin.xml + actions.xml）
- [x] 状态管理（节点增删改）

### 第二阶段 ✅
- [x] 多文件标签页（左侧已打开文件列表）
- [x] 弹出式文件树菜单（☰ 按钮触发，支持搜索筛选）
- [x] 右键画布弹出节点菜单（支持搜索筛选，添加节点）
- [x] 变量面板（显示 Shared/Local 变量）
- [x] 深色主题统一

### 第三阶段（待做）
- [ ] 自定义节点组件（显示 Pin）
- [ ] Pin 编辑（常量/变量切换，拖拽绑定）
- [ ] 属性面板（选中节点时编辑）

### 第四阶段（待做）
- [ ] 保存/导出 XML
- [ ] FSM 编辑器

### 第五阶段（待做）
- [ ] 调试功能（与 C++ 运行时通信）

## 注意事项

1. **Pin 交互设计**：
   - 常量：直接在节点上编辑
   - 变量引用：显示变量名，可拖拽或下拉选择
   - 提供常量/变量模式切换按钮

2. **Variable 存储位置**：
   - sharedVariables/localVariables 存储在 Tree 对象中
   - 不通过 Root 节点处理

3. **配置文件路径**：
   - 工作目录：`E:/Develop/YBehavior/bin`
   - actions.xml：`E:/Develop/YBehavior/projects/YBehaviorEditor/bin/actions.xml`

## UI 设计状态

**当前状态**：示意性布局，未经设计

- 左侧栏：256px 固定宽度，文件列表 + 节点列表
- 中间：React Flow 节点编辑器
- 右侧栏：256px 固定宽度，属性面板

**待定功能**：
- [ ] 可调整侧栏宽度
- [ ] 折叠/展开侧栏
- [ ] 标签页切换
- [ ] 响应式布局
- [ ] 视觉设计（颜色、间距、图标）

## 用户需求记录

### 2026-01-30 节点配置需求

- builtin 节点改为配置文件加载（原为硬编码）
- 与 actions.xml 格式类似
- 后续新增内置节点统一在配置文件中添加
- 配置文件：`config/builtin.xml`

### 类型联动机制

**TypeMap 已支持的**（配置即可，无需代码）：
- `vTypeGroup` - 同组 Pin 类型联动（如 Calculator 的 Input1/Input2/Output）
- `TypeMap` - 枚举值驱动类型切换

**需要特殊代码的**（UI 行为，TypeMap 无法处理）：
| 节点 | 特殊逻辑 |
|------|---------|
| SwitchCase | Cases 数组长度决定连接器数量 |
| SubTree | 需要文件选择器列出 .tree 文件 |
| HandleEvent | 事件配置决定哪些输出 Pin 启用 |

### 2026-01-30 UI 布局需求

1. **文件列表**：
   - 树状文件夹结构显示
   - 支持文本筛选快速选中打开
   - 默认不显示，通过左上角按钮展开/收起
   - 左侧栏只显示已打开的文件标签页，供快速切换

2. **节点列表**：
   - 不单独占用侧栏
   - 右键画布弹出菜单显示
   - 支持文本筛选快速选中并添加

3. **变量列表**：
   - 共享变量和局部变量列表显示在右侧栏上方
   - 类似原编辑器的布局

4. **节点属性面板**：
   - 如果所有编辑都能在节点本身上操作，则不需要专门面板
   - 否则放右侧栏下方

### 新 UI 布局方案

```
┌───────────────────────────────────────────────────────────────┐
│ [≡]                                        [Save] [Export]    │
├───────────┬───────────────────────────────────┬───────────────┤
│ 已打开文件 │                                   │ Variables     │
│ ─────────│                                   │ ─────────────│
│ Tab1.tree │                                   │ Shared:       │
│ Tab2.tree │      节点编辑器画布                 │  counter: 0   │
│ Tab3.tree │                                   │  isActive: T  │
│ *Tab4.tree│                                   │ Local:        │
│           │                                   │  temp: 0.0    │
│           │                                   ├───────────────┤
│           │                                   │ Properties    │
│           │                                   │ (可选)        │
└───────────┴───────────────────────────────────┴───────────────┘

左侧栏：已打开文件列表（竖向，可显示完整文件名，*表示未保存）

弹出菜单（覆盖式，不占用固定空间）：
- 文件树菜单：点击[≡]弹出，树状结构+搜索筛选
- 节点菜单：右键画布弹出，节点列表+搜索筛选
```
