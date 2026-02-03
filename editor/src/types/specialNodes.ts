import type { TreeNode, Tree } from './index';

// 注意：这里为了避免循环依赖，我们只定义接口
// 具体的 store 类型可以作为 any 或者更松散的类型传入，或者使用泛型

export interface SpecialNodeHandler {
    /**
     * 验证节点配置是否合法
     * 返回错误信息数组，空数组表示合法
     */
    validate?: (node: TreeNode, tree: Tree) => string[];

    /**
     * 刷新节点相关的连线标签
     * 这个函数会直接修改 tree.connections 中的 label 属性
     * 调用者负责触发 store 更新
     */
    refreshLabels?: (nodeId: string, tree: Tree) => boolean;
}

// 注册表将由具体的逻辑实现文件填充
export const SPECIAL_NODE_HANDLERS: Record<string, SpecialNodeHandler> = {};
