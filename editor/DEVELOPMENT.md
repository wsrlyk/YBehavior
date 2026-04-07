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

### 2026-04-03 编辑器视觉改造（经典灰色工业风）

- 目标：将新版 Tauri 编辑器视觉风格向旧版 WPF 编辑器靠拢，采用“深灰画布 + 浅灰面板 + 黑色文字 + 低饱和强调色”的经典工具风格。
- 主题色板重做：统一调整节点类别色、Pin 类型色、边颜色、FSM 状态色、调试高亮色、注释色与 UI chrome 色。
- 全局样式调整：主字体改为 `Tahoma/Verdana/Segoe UI`，ReactFlow Controls 与 MiniMap 改为浅灰边框和浅底按钮风格。
- 主窗口样式调整：工具栏、按钮、下拉菜单、分割条、底部 terminal 容器统一灰色体系；分割条 hover 强调色调整为青蓝色。
- 验证：执行 `npm run build` 通过（`tsc && vite build`）。
- 修改文件：`src/theme/theme.ts`、`src/App.css`、`src/windows/MainWindow.tsx`。

### 2026-04-03 节点标题可读性与节点颜色主题化（续）

- 行为树节点：标题栏文字改为深色，解决浅色标题栏下白字不清晰问题。
- `CustomNode`：清理硬编码颜色（白色标题字、白色选中框、紫色 IF 连接器、灰色 handle、断点点颜色等），统一改为主题引用。
- `NodeContextMenu`：节点动作/调试区域的硬编码颜色改为主题引用，状态胶囊与选中项使用主题色。
- `FSMStateNode`：标题、树引用条、Meta 提示条、连接点等硬编码颜色改为主题引用，保持与全局灰色风格一致。
- 验证：执行 `npm run build` 通过（`tsc && vite build`）。
- 修改文件：`src/components/CustomNode.tsx`、`src/components/NodeContextMenu.tsx`、`src/components/FSMStateNode.tsx`。

### 2026-04-03 全量主题化改造（主题商店准备）

- 目标：将编辑器 UI 颜色能力从“组件内写死”升级为“主题变量驱动”，为后续主题商店/运行时切换打基础。
- 主题系统扩展：`GraphTheme.ui` 增补交互色与状态色（`accent/accentSoft/buttonBg/buttonHoverBg/splitterHover/terminalBg/success/danger/warning`）。
- 运行时主题注入：新增 `applyThemeCssVariables()`，统一把主题值写入 CSS Variables（`--tb-ui-*` + `--tb-color-*`）。
- 应用启动接入：`App` 启动时应用主题变量，保证 Tailwind 颜色 utility（如 `text-gray-*`、`bg-blue-*`、`ring-red-*`）也受主题控制。
- Tailwind token 映射：`App.css` 的 `@theme` 改为引用 `--tb-color-*`，并补齐项目中使用到的常见色阶（gray/blue/green/red/yellow/purple/orange/pink/cyan/black/white）。
- 组件去硬编码：清理 `MainWindow`、`Sidebar`、`TreeEdge`、`PropertiesPanel`、`FSMEditor`、`FSMTransitionEdge`、`DebugToolbar` 中十六进制/固定色值，改为主题 token 或主题映射 utility class。
- 校验：全局检索 `src` 中 `tsx/ts/css` 的十六进制颜色（排除主题定义文件）已清零；执行 `npm run build` 通过。
- 修改文件：`src/theme/theme.ts`、`src/App.tsx`、`src/App.css`、`src/windows/MainWindow.tsx`、`src/components/Sidebar.tsx`、`src/components/TreeEdge.tsx`、`src/components/PropertiesPanel.tsx`、`src/components/FSMEditor.tsx`、`src/components/FSMTransitionEdge.tsx`、`src/components/DebugToolbar.tsx`。

### 2026-04-03 属性面板残余深色修正

- 问题：`PropertiesPanel` 仍有大量 `bg-gray-900/800/700` 等 utility class，导致视觉仍偏深色。
- 处理：将运行时 `--tb-color-gray-*` token 改为浅灰映射（基于 `theme.ui`），让遗留 gray utility class 自动跟随浅色主题。
- 修复：移除主题变量注入中的重复 key（`--tb-color-gray-750`）并通过构建校验。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/theme/theme.ts`。

### 2026-04-07 右侧面板亮度与 SubTree 选择交互修复

- 背景：用户反馈右侧面板需比画布更亮；`Return` 值框文字超出底框；`SubTree` 的 `Tree` 选择器需要点两次才能开始选。
- 处理：
  - `PropertiesPanel` 根容器背景从 `theme.ui.background` 调整为 `theme.ui.panelBg`，确保右侧面板整体亮于画布。
  - `NodePropertiesEditor` 的 `Return` 区域改为与 `Nickname` 同类输入高度策略（移除固定 `h-5` 盒约束，统一 `px/py` 与触发文本行高）。
  - `TreeFilePicker` 增加 `defaultOpen` 参数；在 `SubTree` 的 `Tree` 编辑场景传入 `defaultOpen`，进入编辑即展开下拉，实现单击开始选择。
- 修改文件：`src/components/PropertiesPanel.tsx`、`src/components/TreeFilePicker.tsx`。

### 2026-04-07 变量/引脚行密度与标签页选中态一致性调整

- 背景：用户反馈变量与引脚项边框不明显、行间观感偏松；并要求顶部 `VARIABLES/INTERFACE I/O` 选中态与下方面板背景保持一致。
- 处理：
  - `VariableItem`、`InterfacePinItem`、`PinEditor` 恢复常显边框（`borderColor: theme.ui.border`），并将项内垂直内边距从 `py-1` 收紧为 `py-0.5`，让 1px 项间距可见。
  - 顶部标签页选中态背景改为 `theme.ui.panelBg`（与内容面板同底色），未选中态继续使用 `theme.ui.tabInactiveBg`。
- 修改文件：`src/components/PropertiesPanel.tsx`。

### 2026-04-07 节点标题颜色改为直接使用提亮后的主题色

- 背景：用户要求移除 `CustomNode` 内部的 `lightenHexColor`，改为直接在 `theme.node` 配置中提亮节点颜色。
- 处理：
  - 删除 `CustomNode.tsx` 中 `lightenHexColor` 函数与 `titleBgColor` 派生逻辑。
  - 节点标题背景改为直接使用 `theme.node` 对应分类色。
  - 在 `theme.ts` 中直接提高 `node.composite/decorator/action/condition/default` 亮度。
- 修改文件：`src/components/CustomNode.tsx`、`src/theme/theme.ts`。

### 2026-04-07 分隔线补全与弱化

- 背景：用户反馈 Node Properties 中 `Input2` 与 `Output` 之间缺少分隔线，且分隔线希望更淡。
- 处理：
  - 在输入/输出两个 Pin 分组之间增加单独的 1px 分隔线。
  - 变量列表、I/O 列表、Pin 列表及分组间分隔线统一降低不透明度（`opacity: 0.45`）。
- 修改文件：`src/components/PropertiesPanel.tsx`。

### 2026-04-07 变量/引脚分隔样式微调（去常显边框）

- 背景：用户反馈常显边框让列表观感过于拥挤。
- 处理：
  - 去除 `VariableItem`、`InterfacePinItem`、`PinEditor` 的常显边框。
  - 在变量、I/O、节点 Pin 列表中改为“仅两项之间渲染 1px 分隔线”（`theme.ui.border`），保留层次同时减轻拥挤感。
- 修改文件：`src/components/PropertiesPanel.tsx`。

### 2026-04-07 亮色主题可读性增强（类型色/白底/输入框）

- 背景：用户反馈亮色主题下类型颜色不明显，`TRUE/FALSE` 无颜色，输入框底色偏灰。
- 处理：
  - 类型色增强：恢复并提升 `theme.pin` 与 `theme.text.typeColors` 的饱和度（避开青绿色），增强类型区分。
  - 类型白底：`PropertiesPanel` 的 `AdaptiveSelect`（含禁用态）增加白底+边框，让彩色类型字在浅灰面板上更清楚。
  - bool 状态色：变量区与引脚区 `TRUE/FALSE` 按钮恢复为绿/红状态色。
  - 输入框提白：`theme.ui.inputBg` 提升到更白，并在 `App.css` 对 `input/select/textarea` 统一应用 `--tb-ui-input` 背景。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/theme/theme.ts`、`src/components/PropertiesPanel.tsx`、`src/App.css`。

### 2026-04-07 终端与通知颜色硬编码清理（主题化）

- 背景：用户指出终端改色过程中出现硬编码颜色，要求统一排查并改为主题驱动。
- 处理：
  - 全局扫描：检索 `src`（排除 `theme.ts`）中的十六进制色值，定位残留集中在 `Terminal.tsx`、`Sidebar.tsx`、`NotificationBubble.tsx`。
  - 主题扩展：在 `theme.ui` 增加终端专用 token（`terminalText/header/input/border/button/timestamp/log-*`），统一由主题配置提供。
  - 组件替换：
    - `Terminal.tsx` 全部硬编码十六进制改为 `theme.ui.terminal*`；保留日志分段颜色映射但映射目标改为主题 token。
    - `Sidebar.tsx` 选中文件名白色由硬编码改为主题 token（`theme.ui.terminalButtonText`）。
    - `NotificationBubble.tsx` warning 文字色由硬编码改为 `theme.ui.textMain`。
- 结果：`src` 非主题文件中的十六进制硬编码颜色扫描结果为 0。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/theme/theme.ts`、`src/components/Terminal.tsx`、`src/components/Sidebar.tsx`、`src/components/NotificationBubble.tsx`。

### 2026-04-07 主题语义化继续推进（PropertiesPanel 第一轮）

- 背景：用户要求继续推进“颜色可主题化”，减少组件内灰阶硬编码语义。
- 处理：
  - `PropertiesPanel`（变量区域）将一批 `bg-gray/text-gray/border-gray` 视觉色替换为 `theme.ui.panelBg/inputBg/border/textMain/textDim`。
  - `AdaptiveSelect` 下拉选中/hover 背景从硬编码十六进制改为主题 token（`theme.ui.accentSoft`）。
  - 保持布局与交互不变，仅替换颜色来源为主题语义 token。
- 结果：再次扫描 `src`（排除 `theme.ts`）中的十六进制硬编码颜色为 0。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/components/PropertiesPanel.tsx`、`src/DEVELOPMENT.md`。

### 2026-04-07 主题语义化继续推进（PropertiesPanel 第二轮）

- 背景：继续减少组件内灰阶语义类，确保主题切换只依赖语义 token。
- 处理：
  - `PropertiesPanel.tsx` 中所有 `gray-*` 颜色类替换为 `theme.ui.*`（包含容器、分组标题、Node Properties 区、Node 编辑输入、Pin 编辑输入/按钮、向量索引、下拉触发 hover 等）。
  - 保留结构与交互逻辑，仅调整颜色来源，避免布局与功能回归。
- 结果：`PropertiesPanel.tsx` 内 `gray-` 颜色类扫描结果为 0。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/components/PropertiesPanel.tsx`、`src/DEVELOPMENT.md`。

### 2026-04-07 主题语义化收尾（剩余组件全量清理）

- 背景：继续处理“剩下所有颜色灰阶写死”的收口工作。
- 处理：
  - 组件层面继续清理 `gray-*` 颜色类并改为 `theme.ui.*`：
    - `GlobalSearch.tsx`
    - `FSMPropertiesPanel.tsx`
    - `DebugToolbar.tsx`
    - `TreeFilePicker.tsx`
    - `FileTreePopup.tsx`
    - `RunningList.tsx`
    - `EditorPane.tsx`
    - `NodeEditor.tsx`
    - `FSMEditor.tsx`
    - `Terminal.tsx`
    - `NotificationBubble.tsx`
    - `MainWindow.tsx`
  - `FSMStateNode.tsx` 去除 `ring-offset-gray-*` 依赖，改为基于主题色的内联 ring/shadow。
  - `config/constants.ts` 调试色常量从 Tailwind 颜色类改为显式颜色值，避免样式类耦合。
- 结果：`src` 内 `gray-*` 仅剩主题映射与日志 token 兼容字符串（`theme.ts`/`debugStore.ts`/`Terminal.tsx`），UI 组件样式层已完成主题 token 化。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/components/*` 多文件、`src/config/constants.ts`、`src/windows/MainWindow.tsx`、`src/DEVELOPMENT.md`。

### 2026-04-07 面板密度与 ReturnType 输入高度微调

- 背景：用户反馈“引脚/变量底框间距过大”，以及 ReturnType 值框高度明显高于其它输入。
- 处理（仅样式，不改交互逻辑）：
  - `PropertiesPanel.tsx` 中变量、接口 I/O、节点 Pin 列表容器由 `space-y-1` 调整为 `gap-px`，使项间距统一为 1px。
  - `NodePropertiesEditor` 的 ReturnType 区域容器统一为 `h-5` + 边框，并让下拉触发文本使用 `leading-5` 对齐，和旁边输入高度一致。
- 修改文件：`src/components/PropertiesPanel.tsx`、`src/DEVELOPMENT.md`。

### 2026-04-07 节点标题底色提亮（Sequence/Selector 可读性）

- 背景：用户反馈 Sequence、Selector 等节点标题条底色过灰，标题不够清晰。
- 处理：
  - 在 `CustomNode.tsx` 增加 `lightenHexColor`，仅对节点标题条背景做提亮混合（约 35% toward white）。
  - 节点主体面板背景、边框、交互与调试态保持不变，只提升标题条可读性。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/components/CustomNode.tsx`、`src/DEVELOPMENT.md`。

### 2026-04-03 中性灰回调 + bool 对比度修复

- 背景：用户反馈“太暖了，要严格中性灰”，并指出 `bool` 文本不清晰。
- 处理：
  - 严格中性灰：`theme.ts` 中最近一轮偏暖改动回调为纯中性灰（`pin`、`text.typeColors`、`gray-50/100/400/700/750/800/900`）。
  - bool 可读性：`PropertiesPanel` 中变量区与引脚区的 `TRUE/FALSE` 按钮从红绿样式改为灰阶高对比（深灰底白字 / 浅灰底深字+边框）。
  - bool 类型字重色：`typeColors.bool` 统一为更深中性灰（`#1F1F1F`），避免浅底下发灰发虚。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/theme/theme.ts`、`src/components/PropertiesPanel.tsx`。

### 2026-04-03 偏青观感二次修正（偏暖中性灰）

- 背景：用户反馈“还是青青的感觉”，截图中主要体现在大面积灰底与节点/类型辅助色的整体观感偏冷。
- 处理：
  - 主题灰阶映射：`applyThemeCssVariables` 中 `--tb-color-gray-50/100/400/700/750/800/900` 调整为偏暖中性灰，避免蓝灰底色。
  - 类型与引脚色：`pin` 与 `text.typeColors` 中 `int/entity/ulong` 等偏蓝青项改为棕灰/橄榄灰/暖紫灰系，保留区分但去青色。
  - FSM 色块去青：`fsmState.Entry` 与 `fsmState.Upper` 从绿青/蓝灰调整为暖灰土色。
  - 节点主色微调：`node.composite` 从偏绿灰改为暖灰。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/theme/theme.ts`。

### 2026-04-03 青绿色残留专项排查（截图反馈）

- 背景：用户反馈界面仍有青绿色残留（重点在工具栏/文件树弹窗/输入框焦点与选中态）。
- 处理：
  - 交互态去蓝化：`TreeFilePicker`、`FileTreePopup`、`DebugToolbar`、`GlobalSearch`、`FSMPropertiesPanel`、`PropertiesPanel` 的 `blue-*` 焦点/hover/选中样式统一改为灰色系。
  - 视觉杂色清理：`MainWindow` 更新水印、`NotificationBubble` info 态、`DEBUG_COLORS/DEBUG_RINGS` 中 failure 态、`debugStore` 的 failure 文本色去除蓝青色。
  - 主题中和：进一步把 `theme.ui`（`panelBg/inputBg/button/accent/splitter`）调整为更中性的工业灰；`edge.data` 与 `fsmTransition.selected` 从偏蓝改为中灰。
  - 回退链路修正：`App.css` 中 `--tb-ui-accent`/`accent-soft`/`splitter-hover` 的 root fallback 从 `--color-blue-*` 改为 `--color-gray-*`，避免变量注入失败时回落到蓝色。
- 结果：全局 `blue/cyan/teal` 检索仅剩 `App.css/theme.ts` 的 token 名称映射（非实际蓝青色值）；组件层已无蓝青交互类。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/components/TreeFilePicker.tsx`、`src/components/FileTreePopup.tsx`、`src/components/DebugToolbar.tsx`、`src/components/GlobalSearch.tsx`、`src/components/FSMPropertiesPanel.tsx`、`src/components/PropertiesPanel.tsx`、`src/components/NotificationBubble.tsx`、`src/windows/MainWindow.tsx`、`src/config/constants.ts`、`src/stores/debugStore.ts`、`src/App.css`、`src/theme/theme.ts`。

### 2026-04-03 悬停提示统一修复 + 去青绿色回调

- 背景：用户要求移除青绿色，仅保留中性高对比；并修正引脚悬停语义（引脚名显示注释、引脚值显示值），同时统一清理黑底白字原生 Tooltip。
- 处理：
  - 主题回调：`theme.ui.accent/accentSoft/splitterHover` 从青蓝改为中性工业灰蓝，保留层次但去掉青绿色观感。
  - Pin 语义修复：`PropertiesPanel` 与 `CustomNode` 的引脚名悬停统一显示 `pin.desc`；值区域悬停显示完整值（长值可查看）。
  - Tooltip 统一：全局清理 `src` 内所有 `title=`（扫描结果由 24 处降到 0），改为 `useTooltipStore` 或直接移除。
- 覆盖文件：`MainWindow`、`DebugToolbar`、`TreeFilePicker`、`RunningList`、`FileTreePopup`、`FSMStateNode`、`FSMPropertiesPanel`、`GlobalSearch`、`PropertiesPanel`、`CustomNode`。
- 验证：执行 `npm run build` 通过。

### 2026-04-03 可读性热修（文字对比度）

- 问题：浅灰背景下，遗留 `text-gray-300/400/500/600` 在多个面板中对比度不足，文字不清晰。
- 处理：在 `App.css` 增加对这些 utility class 的统一对比度覆盖，全部映射到 `--tb-ui-text-main` / `--tb-ui-text-dim`。
- 结果：不改结构、不改功能，仅提升文字可读性，尤其是右侧属性区和文件树弹层。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/App.css`。

### 2026-04-03 可读性二次修复（截图反馈）

- 问题：`Open Files`、`VARIABLES/INTERFACE`、`State Properties` 与节点 Tooltip 仍存在低对比文字。
- 处理：
  - `Sidebar` 标题/列表文字统一提升为 `theme.ui.textMain`，并将背景恢复为 `theme.ui.panelBg` 提高前景对比。
  - `Tooltip` 改为显式主题样式（`panelBg/border/textMain`），不再依赖灰阶 utility 组合。
  - `App.css` 扩展全局对比度覆盖：`text-gray-100~600` 统一映射到高对比文本色，并提升 placeholder 可见性。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/components/Sidebar.tsx`、`src/components/Tooltip.tsx`、`src/App.css`。

### 2026-04-03 可读性三次修复（原生 Tooltip + 引脚参数）

- 问题：文件项与引脚名悬停仍触发浏览器原生 `title` 提示（黑底白字）；`Node Properties` 中引脚 `int`/`[]` 参数对比度不足。
- 处理：
  - `SidebarItem`：移除 `title`，改用 `useTooltipStore` 驱动自定义 Tooltip，避免原生黑底白字。
  - `PinEditor`：移除引脚名 `title`，改用主题 Tooltip；提高方向标识与数组标记可读性（`in/out`、`[]` 字重/透明度）。
  - 主题增强：`theme.text.typeColors.int` 从 `#3F6F8F` 调整为 `#1F5C7A`，提升 `Int` 标签和相关标记对比度。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/components/Sidebar.tsx`、`src/components/PropertiesPanel.tsx`、`src/theme/theme.ts`。

### 2026-04-03 类型颜色统一高对比调整

- 背景：用户反馈 `int` 只是示例，`enum` 等其它类型在浅色面板中同样不清晰。
- 处理：统一调整 `theme.text.typeColors` 全类型文本色（`int/float/bool/string/vector3/entity/ulong/enum`），整体加深并保持类型区分。
- 结果：`PropertiesPanel`/`PinEditor` 中类型标签与 `[]` 计数标记可读性同步提升，不再只优化单一类型。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/theme/theme.ts`。

### 2026-04-03 旧编辑器风格回调（整体去灰）

- 背景：用户反馈“整体太灰看不清”，要求参考旧编辑器配色层次。
- 处理：
  - 统一调整 `DefaultTheme.ui`：加深主背景、提亮面板与输入框、加深主文字/次文字，提升前后景分离度。
  - 强化工业风蓝青点缀：调整 `accent/accentSoft/button` 等交互色，减少纯灰雾感。
  - 同步重设 `--tb-color-gray-*` 映射，让遗留 `bg-gray-*`/`border-gray-*` 在不重写组件的前提下自动获得更清晰层次。
- 验证：执行 `npm run build` 通过。
- 修改文件：`src/theme/theme.ts`。
