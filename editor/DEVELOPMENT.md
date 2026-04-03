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

### 第三阶段（进行中）
- [x] 自定义节点组件（显示 Pin，类别颜色，类型颜色）
- [x] 输出 Pin 放右侧，禁用 Pin 不显示
- [x] 节点定义 Connector 配置（hasParent, childConnectors）
- [x] 树文件解析基于定义文件合并数据（丢弃多余、补充默认值）
- [ ] Pin 编辑（常量/变量切换，拖拽绑定）
- [ ] 属性面板（选中节点时编辑）
- [ ] 连线水平部分对齐算法
- [ ] 一键整理节点布局按钮

### 第四阶段 ✅
- [x] 保存/导出 XML - 编辑器版
- [x] 保存/导出 XML - 运行时版（变量引用计算、森林过滤、UID管理）

### 第四阶段补充 ✅
- [x] Variable 编辑功能（修改值、切换数组/标量、切换类型、添加、删除）
- [x] Pin 编辑功能（切换常量/引用、修改值、禁用/启用）
- [x] 节点编辑功能（Return、注释）
- [x] 数据连接的连线（蓝色虚线贝塞尔曲线）
- [x] 新建画布 + 自动创建Root（Root不可删除，可选中移动）
- [x] 鼠标样式优化（左键选择/框选，中键拖动画布）
- [x] UID 实时计算（节点增删改、禁用、调整顺序时自动刷新）
- [x] 连线悬停、多连接器连线位置修复
- [x] Vector Index 显示（节点上显示 `ff[0]`）和编辑（面板上有 Index 编辑区域）
- [x] 数据连接的 Pin 在面板上显示 `(data connection)`

### 数据连接状态管理 ✅
**设计决策**：使用 `pointer` 类型 + 空 `variableName` 表示数据连接状态（等待连接或已连接）

- [x] 新建节点的 Pin 默认是引用类型时，默认处于"数据连接（未连接）"状态（空变量名）
- [x] Pin 绑定类型循环切换：C(常量) → V(变量引用) → D(数据连接) → C
- [x] 节点上显示：变量引用显示变量名，数据连接显示空心圆 ○
- [x] 面板上显示：数据连接状态显示 "● connected" 或 "○ waiting for connection"
- [x] 数据连接同步到 store（addDataConnection/removeDataConnection）
- [x] 节点位置拖拽后同步到 store（避免切换 Pin 时位置回滚）
- [x] 数据连接删除同步到 store（onEdgesChange 处理）
- [x] Pin 从数据连接状态切换时自动断开连接

### Pin 类型切换 ✅
- [x] 支持切换数据类型（valueType）- 下拉选择，仅当 allowedValueTypes.length > 1 时显示
- [x] 支持切换数组/标量类型（countType）- 按钮切换 `[]`/`·`

### 第五阶段（待做）
- [ ] FSM 编辑器
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

### Connector 配置

节点定义支持配置连接器（`config/builtin.xml`）：

```xml
<!-- Root 没有父连接器，有一个子连接器（最多1个子节点） -->
<Node Class="Root" Category="decorator" HasParent="False">
  <Connector Name="children" MaxChildren="1" />
</Node>

<!-- Composite 节点：可以有多个子节点 -->
<Node Class="Sequence" Category="composite">
  <Connector Name="children" />
</Node>

<!-- IfThenElse 有三个子连接器，各最多1个子节点 -->
<Node Class="IfThenElse" Category="composite">
  <Connector Name="if" Label="If" MaxChildren="1" />
  <Connector Name="then" Label="Then" MaxChildren="1" />
  <Connector Name="else" Label="Else" MaxChildren="1" />
</Node>

<!-- For 有四个子连接器 -->
<Node Class="For" Category="decorator">
  <Connector Name="init" Label="Init" MaxChildren="1" />
  <Connector Name="cond" Label="Cond" MaxChildren="1" />
  <Connector Name="increment" Label="Incr" MaxChildren="1" />
  <Connector Name="children" MaxChildren="1" />
</Node>

<!-- SwitchCase 有 children 和 default 两个连接器 -->
<Node Class="SwitchCase" Category="composite">
  <Connector Name="children" />
  <Connector Name="default" Label="Default" MaxChildren="1" />
</Node>

<!-- Action/Condition 没有子连接器（不配置 Connector 元素） -->
<Node Class="Calculator" Category="action">...</Node>
```

**连接器规则**：
- `hasParent`: 默认 true，Root 设为 false
- `childConnectors`: 空数组表示没有子连接器（Action/Condition）
- `MaxChildren`: 不设置表示无限制，设为 1 表示最多一个子节点

### Pin 属性配置规则

| 属性 | 配置值 | 含义 |
|------|--------|------|
| **IsConst** | `True` | 固定常量 |
| | `False` | 固定引用（指针） |
| | 没有配置 | 默认常量，可切换 |
| **IsArray** | `True` | 固定数组 |
| | `False` | 固定标量 |
| | 没有配置 | 默认标量，可切换 |
| **IsEnable** | `True` | 默认启用，可切换 |
| | `False` | 默认禁用，可切换 |
| | 没有配置 | 固定启用，不可切换 |
| **IsInput** | `True` | 输入 Pin |
| | `False` | 输出 Pin |
| | 没有配置 | 默认输入 |

### 树文件解析规则

树文件中的节点数据会与节点定义合并：
1. **多余的 Pin**：丢弃（可能是旧配置）
2. **缺少的 Pin**：使用定义中的默认值（可能是新增参数）
3. **都有的 Pin**：使用树文件中的值

### 类型联动机制

**TypeMap 已支持的**（配置即可，无需代码）：
- `vTypeGroup` - 同组 Pin 类型联动（如 Calculator 的 Input1/Input2/Output）
- `TypeMap` - 枚举值驱动类型切换

**需要特殊代码的**（UI 行为，TypeMap 无法处理）：
| 节点 | 特殊逻辑 |
|------|---------|
| SwitchCase | Cases 常量数组长度匹配children连接器下的启用的子节点数量（或只有1个子节点），每个子节点的连线上显示对应数组元素的值，变量数组模式下子节点数量必须为1 |
| SubTree | 需要文件选择器列出 .tree 文件，有额外的子树pin的配置面板 |
| HandleEvent | Cases 常量数组长度匹配children连接器下的启用的子节点数量，每个子节点的连线上显示对应数组元素的值，允许：一对多，多对一，一一对应三种情况。变量数组无数量限制 |

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

## XML 保存/导出规范

### 文件格式

- **编码**：UTF-8 with BOM
- **换行符**：CRLF (`\r\n`)
- **自闭合标签**：`/>` 前有空格

### Pin 值序列化格式

```
单体(scalar): _[类型][绑定][启用] [值]    例如: _IP a, _IC 1, _FpE 
数组(list):   [类型][类型][绑定][启用] [值]  例如: IIC 1|2|3, FFp fff
```

- **类型字符**：I(int), F(float), B(bool), S(string), V(vector3), A(entity), U(ulong), E(enum)
- **绑定字符**：C/c(const), P(shared pointer), p(local pointer)
- **启用字符**：E(enable), D(disable)，固定启用时不加

### 编辑器版 XML

保存到 `editorTreeDir`（如 `bin/`），包含：

- `IsEditor=""` 属性标记
- 节点 `GUID` 属性
- 节点 `Pos` 位置
- 节点 `Connection`、`Return` 等额外属性
- 禁用节点（`Disabled="true"`）
- 禁用 Pin（`D` 标记）
- 所有 Shared/Local 变量
- 所有 DataConnections（使用 GUID）
- 森林中的所有树

### 运行时版 XML

保存到 `runtimeTreeDir`（如 `export/`），**不包含**：

- `IsEditor` 属性
- `GUID`、`Pos` 等编辑器属性
- 禁用节点（跳过）
- 禁用 Pin（跳过）
- 未引用的变量（需计算引用）
- 森林中非主树的 DataConnections

### ID 管理规则

#### GUID（编辑器用）

- 创建节点时生成，取当前最大 GUID + 1
- **永不改变**，除非复制节点（需重新分配）
- 编辑器版 XML 使用 GUID
- DataConnections 中使用 `FromGUID`/`ToGUID`

#### UID（运行时用）

- 每次保存时重新计算
- 从 Root 节点开始，深度优先遍历，从 1 开始递增
- 森林中其他树：
  - 第一棵额外树从 1001 开始
  - 第二棵额外树从 2001 开始
  - 以此类推（每棵树最多 1000 节点）
- 运行时版 XML 使用 UID
- DataConnections 中使用 `FromUID`/`ToUID`

### 变量引用计算（运行时版）

遍历主树（Root 及其子树）中所有节点的 Pin：
- 如果 Pin 绑定类型是 pointer（`P` 或 `p`）
- 记录引用的变量名
- 只导出被引用的 Shared/Local 变量

### DataConnections 过滤（运行时版）

- 只导出主树（Root 树）内部的数据连接
- 森林中其他树的数据连接不导出

### 节点属性顺序

```xml
<Node Class="..." GUID="..." Connection="..." Return="..." Pos="..." ...Pin属性...>
```

1. `Class` - 节点类名
2. `GUID` - 编辑器版
3. `Connection` - 如果有
4. `Return` - 如果有
5. `Pos` - 位置
6. Pin 属性按定义顺序

### 2026-04-01 序号计算规则统一

- 统一 `UID` 计算与读取后内存顺序：父子关系按先序遍历（父节点优先）。
- 同一父节点下：不同 connection 按定义顺序（`condition` 永远最前）；同一 connection 内按子节点 `x` 坐标从左到右排序。
- 保存 XML 时沿用同一 connection 排序规则；读取后会先规范 `connections` 与 `childrenIds`，保证内存与保存/UID 结果一致。
- 修改文件：`src/utils/xmlParser.ts`、`src/stores/editorStoreCore.ts`。

### 2026-04-01 WebView2 性能调优（CPU/拖拽/调试）

- `CustomNode` 去除对全局 `keyframe` 的订阅，避免每个调试帧触发全节点重渲染；保留按节点状态变化的高亮刷新。
- `NodeEditor` 的 Shift 拖拽路径改为 `Set/Map` 查找，避免 `includes/find` 的 O(n*m) 扫描导致拖拽卡顿。
- `NodeEditor` 开启 `onlyRenderVisibleElements`，降低大树静止与拖动画布时的渲染负载。
- `debugStore` 的 `displayKeyframe` 改为每帧只解析一次 shared 变量，并移除暂停状态下的高频日志输出。
- 修改文件：`src/components/CustomNode.tsx`、`src/components/NodeEditor.tsx`、`src/stores/debugStore.ts`。

### 2026-04-01 Tooltip 截断修复

- `Tooltip` 增加尺寸测量与视口边界裁剪：靠近窗口右/下边缘时自动翻转或回退到可见区域。
- 保持 `createPortal(document.body)` + `position: fixed`，避免被编辑区容器裁剪。
- 修改文件：`src/components/Tooltip.tsx`。

### 2026-04-01 画布缩放与小地图尺寸调整

- `NodeEditor` 调整缩放范围：支持更远距离缩小查看（`minZoom=0.08`，`maxZoom=2.5`）。
- `NodeEditor` 缩小小地图显示面积（`MiniMap` 设为 `140x90`）。
- 修改文件：`src/components/NodeEditor.tsx`。

### 2026-04-01 画布控件与视口记忆

- 移除树编辑器画布左下角 ReactFlow 默认控制按钮（`Controls`）。
- 切换树文件时恢复该文件上次视口（位置+缩放）；若首次无视口则 `fitView` 后写入并记忆。
- 修改文件：`src/components/NodeEditor.tsx`。

### 2026-04-01 树视口持久化（跨重启）

- `setViewport` 在更新内存态 `openedFiles` 的同时，将视口落盘到 `editor_meta.local.json`（`treeMetas[filePath].viewport`）。
- `openTree` 打开树时读取并恢复该文件保存过的 `viewport`，实现“关闭编辑器/关闭文件后，下次打开仍保持位置和缩放”。
- 修改文件：`src/stores/editorStore.ts`。

### 2026-04-01 FSM 画布控件调整

- 移除 FSM 编辑器画布左下角 ReactFlow 默认控制按钮（`Controls`）。
- 按需求不新增 FSM 的 viewport 专门持久化逻辑。
- 修改文件：`src/components/FSMEditor.tsx`。

### 2026-04-01 Save As 重名打开文件冲突修复

- 修复场景：A 树已打开时，将另一棵树 `Save As` 为同名路径会导致 `openedFiles` 出现路径冲突，进而出现空白标签与无法关闭。
- 在 `saveFileAs` 中增加已打开路径冲突检测：若目标路径已被其它已打开标签占用，则阻止保存并提示先关闭对应标签。
- 同时统一 `Save As` 路径为标准化路径（斜杠归一），避免路径比较不一致。
- 修改文件：`src/stores/editorStore.ts`。

### 2026-04-01 节点 Note 显示与占位符替换

- 在节点标题下方增加 `Note` 显示区域。
- 支持 `Note` 中的 `{index}` 占位符按 Pin 序号替换为对应 Pin 当前值（常量/变量名/向量下标），禁用 Pin 替换为空。
- 修改文件：`src/components/CustomNode.tsx`。
