import type { TreeNode, Tree } from '../types';

// 定义标签更新对象
export interface ConnectionLabelUpdate {
    id: string;
    label: string;
}

// 修改接口定义以匹配新签名 (虽然 types/specialNodes.ts 定义的是 (node, tree) => boolean，但我们可以在这里使用更具体的类型，或者修改 types)
// 为了兼容性，我们这里直接导出具体函数，editorStore 中会适配

// ==================== SwitchCase Logic ====================

function validateSwitchCaseNode(node: TreeNode, tree: Tree): string[] {
    const errors: string[] = [];
    const casesPin = node.pins.find(p => p.name === 'Cases');

    if (!casesPin) return errors;

    // 1. 常量数组模式
    if (casesPin.binding.type === 'const' && casesPin.countType === 'list') {
        const childrenConns = tree.connections.filter(
            c => c.parentNodeId === node.id && c.parentConnector === 'children'
        );

        // 统计启用的子节点数量
        const enabledChildren = childrenConns.filter(conn => {
            const child = tree.nodes.get(conn.childNodeId);
            return child && !child.disabled;
        });

        // 解析 Cases 数组
        const caseValues = casesPin.binding.value.split('|').map(v => v.trim()).filter(v => v !== '');

        if (caseValues.length !== enabledChildren.length && enabledChildren.length !== 1) {
            errors.push(
                `[${node.nickname || node.type}:${node.uid}] Cases array length (${caseValues.length}) must match enabled children count (${enabledChildren.length}), or have exactly 1 child`
            );
        }
    }
    // 2. 变量数组或其他模式
    else {
        // 规则：只能有1个子节点
        const childrenConns = tree.connections.filter(
            c => c.parentNodeId === node.id && c.parentConnector === 'children'
        );
        const enabledChildren = childrenConns.filter(conn => {
            const child = tree.nodes.get(conn.childNodeId);
            return child && !child.disabled;
        });

        if (enabledChildren.length !== 1) {
            errors.push(
                `[${node.nickname || node.type}:${node.uid}] When Cases is variable or disabled, children count must be 1 (got ${enabledChildren.length})`
            );
        }
    }

    return errors;
}

function refreshSwitchCaseLabels(nodeId: string, tree: Tree): ConnectionLabelUpdate[] {
    const updates: ConnectionLabelUpdate[] = [];
    const node = tree.nodes.get(nodeId);
    if (!node || node.type !== 'SwitchCase') return updates;

    const casesPin = node.pins.find(p => p.name === 'Cases');
    if (!casesPin) return updates;

    const childrenConns = tree.connections.filter(
        c => c.parentNodeId === nodeId && c.parentConnector === 'children'
    );

    // 1. 常量数组模式
    if (casesPin.binding.type === 'const' && casesPin.countType === 'list') {
        const caseValues = casesPin.binding.value.split('|').map(v => v.trim()).filter(v => v !== '');

        const enabledChildren = childrenConns.filter(conn => {
            const child = tree.nodes.get(conn.childNodeId);
            return child && !child.disabled;
        });

        // 情况 A: 只有 1 个子节点 -> 显示所有 Case
        if (enabledChildren.length === 1) {
            const targetConn = childrenConns.find(c => c.childNodeId === enabledChildren[0].childNodeId);
            if (targetConn) {
                const newLabel = caseValues.join(' | ');
                if (targetConn.label !== newLabel) {
                    updates.push({ id: targetConn.id, label: newLabel });
                }
            }

            // 确保其他（禁用的）连接没有标签
            childrenConns.forEach(conn => {
                if (conn.childNodeId !== enabledChildren[0].childNodeId && conn.label !== '') {
                    updates.push({ id: conn.id, label: '' });
                }
            });
        }
        // 情况 B: 多个子节点 -> 一一对应
        else {
            // 按 X 坐标排序子节点连接
            const sortedConns = [...childrenConns].sort((a, b) => {
                const nodeA = tree.nodes.get(a.childNodeId);
                const nodeB = tree.nodes.get(b.childNodeId);
                return (nodeA?.position.x || 0) - (nodeB?.position.x || 0);
            });

            // 为每个启用的子节点分配标签
            let caseIndex = 0;
            sortedConns.forEach(conn => {
                const child = tree.nodes.get(conn.childNodeId);
                const isEnabled = child && !child.disabled;

                let newLabel = '';
                if (isEnabled) {
                    if (caseIndex < caseValues.length) {
                        newLabel = caseValues[caseIndex];
                        caseIndex++;
                    } else {
                        newLabel = `No.${caseIndex}`; // 超出数组范围
                        caseIndex++;
                    }
                } else {
                    newLabel = ''; // 禁用的节点不显示
                }

                if (conn.label !== newLabel) {
                    updates.push({ id: conn.id, label: newLabel });
                }
            });
        }
    }
    // 2. 变量数组或其他模式
    else {
        childrenConns.forEach((conn, i) => {
            const newLabel = childrenConns.length > 1 ? `No.${i}` : '';
            if (conn.label !== newLabel) {
                updates.push({ id: conn.id, label: newLabel });
            }
        });
    }

    return updates;
}

// ==================== HandleEvent Logic ====================

function validateHandleEventNode(node: TreeNode, tree: Tree): string[] {
    const errors: string[] = [];
    const eventsPin = node.pins.find(p => p.name === 'Events');

    if (!eventsPin) return errors;

    const childrenConns = tree.connections.filter(
        c => c.parentNodeId === node.id && c.parentConnector === 'children'
    );

    const enabledChildren = childrenConns.filter(conn => {
        const child = tree.nodes.get(conn.childNodeId);
        return child && !child.disabled;
    });

    // 1. 常量数组模式
    if (eventsPin.binding.type === 'const' && eventsPin.countType === 'list') {
        const eventNames = eventsPin.binding.value.split('|').map(v => v.trim()).filter(v => v !== '');

        // 规则：长度匹配 OR 只有1个子节点
        if (eventNames.length !== enabledChildren.length && enabledChildren.length !== 1) {
            errors.push(
                `[${node.nickname || node.type}:${node.uid}] Events array length (${eventNames.length}) must match enabled children count (${enabledChildren.length}), or have exactly 1 child`
            );
        }
    }
    // 2. 变量引用或禁用模式
    else {
        // 规则：只能有1个子节点
        if (enabledChildren.length !== 1) {
            errors.push(
                `[${node.nickname || node.type}:${node.uid}] When Events is variable or disabled, children count must be 1 (got ${enabledChildren.length})`
            );
        }
    }

    return errors;
}

function refreshHandleEventLabels(nodeId: string, tree: Tree): ConnectionLabelUpdate[] {
    const updates: ConnectionLabelUpdate[] = [];
    const node = tree.nodes.get(nodeId);
    if (!node || node.type !== 'HandleEvent') return updates;

    const eventsPin = node.pins.find(p => p.name === 'Events');
    if (!eventsPin) return updates;

    const childrenConns = tree.connections.filter(
        c => c.parentNodeId === nodeId && c.parentConnector === 'children'
    );

    const enabledChildren = childrenConns.filter(conn => {
        const child = tree.nodes.get(conn.childNodeId);
        return child && !child.disabled;
    });

    // 1. 常量数组模式
    if (eventsPin.binding.type === 'const' && eventsPin.countType === 'list') {
        const eventNames = eventsPin.binding.value.split('|').map(v => v.trim()).filter(v => v !== '');

        // 情况 A: 只有 1 个子节点 -> 显示所有事件名
        if (enabledChildren.length === 1) {
            const targetConn = childrenConns.find(c => c.childNodeId === enabledChildren[0].id);

            // 只有启用的那个子节点才显示，其他禁用的不显示
            if (targetConn) {
                const newLabel = eventNames.join(', ');
                if (targetConn.label !== newLabel) {
                    updates.push({ id: targetConn.id, label: newLabel });
                }
            }

            // 确保其他（禁用的）连接没有标签
            childrenConns.forEach(conn => {
                if (conn.childNodeId !== enabledChildren[0].id && conn.label !== '') {
                    updates.push({ id: conn.id, label: '' });
                }
            });
        }
        // 情况 B: 多个子节点 -> 一一对应
        else {
            // 按 X 坐标排序
            const sortedConns = [...childrenConns].sort((a, b) => {
                const nodeA = tree.nodes.get(a.childNodeId);
                const nodeB = tree.nodes.get(b.childNodeId);
                return (nodeA?.position.x || 0) - (nodeB?.position.x || 0);
            });

            let eventIndex = 0;
            sortedConns.forEach(conn => {
                const child = tree.nodes.get(conn.childNodeId);
                const isEnabled = child && !child.disabled;

                let newLabel = '';
                if (isEnabled) {
                    if (eventIndex < eventNames.length) {
                        newLabel = eventNames[eventIndex];
                        eventIndex++;
                    } else {
                        newLabel = `No.${eventIndex}`;
                        eventIndex++;
                    }
                } else {
                    newLabel = '';
                }

                if (conn.label !== newLabel) {
                    updates.push({ id: conn.id, label: newLabel });
                }
            });
        }
    }
    // 2. 变量引用或禁用模式
    else {
        const label = eventsPin.enableType === 'disable' ? 'All Events' : '';
        childrenConns.forEach(conn => {
            if (conn.label !== label) {
                updates.push({ id: conn.id, label });
            }
        });
    }

    return updates;
}

// ==================== SubTree Logic ====================

function validateSubTreeNode(node: TreeNode, _tree: Tree): string[] {
    const errors: string[] = [];

    // 1. 检查 Tree Pin 或 Identification Pin
    const treePin = node.pins.find(p => p.name === 'Tree');
    const identPin = node.pins.find(p => p.name === 'Identification');

    const treeValue = (treePin?.binding.type === 'const') ? treePin.binding.value : '';
    const identValue = (identPin?.binding.type === 'const') ? identPin.binding.value : '';

    if (!treeValue && !identValue) {
        errors.push(`[SubTree:${node.uid ?? '?'}] Either Tree file or Identification name must be provided`);
    }

    // 2. 检查动态 Pins 绑定
    const basePinNames = ['Tree', 'Identification'];

    node.pins.forEach(pin => {
        if (basePinNames.includes(pin.name)) return;

        if (pin.isInput) {
            // Input 可以是常量也可以是变量，不强制要求非空（由于类型多样性，这里不强制校验具体值）
        } else {
            // Output 必须是变量引用 (pointer) 且不能为空
            if (pin.binding.type === 'const') {
                errors.push(`[SubTree:${node.uid ?? '?'}] Output Pin [${pin.name}] must be bound to a variable`);
            } else if (pin.binding.type === 'pointer' && !pin.binding.variableName) {
                errors.push(`[SubTree:${node.uid ?? '?'}] Output Pin [${pin.name}] transition variable is missing`);
            }
        }
    });

    return errors;
}

// ==================== Registry ====================

// 注意：这里的类型和 types/specialNodes.ts 中的略有不同（返回值类型），store 中需要处理
export const SPECIAL_NODE_HANDLERS_IMPL = {
    SwitchCase: {
        validate: validateSwitchCaseNode,
        refreshLabels: refreshSwitchCaseLabels,
    },
    HandleEvent: {
        validate: validateHandleEventNode,
        refreshLabels: refreshHandleEventLabels,
    },
    SubTree: {
        validate: validateSubTreeNode,
    },
};
