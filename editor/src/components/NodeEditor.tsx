import { useCallback, useMemo, useEffect, useState, type MouseEvent } from 'react';
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  useReactFlow,
  type Node,
  type Edge,
  type OnConnect,
  BackgroundVariant,
  ReactFlowProvider,
} from '@xyflow/react';
import { useEditorStore } from '../stores/editorStore';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { NodeContextMenu } from './NodeContextMenu';
import CustomNode, { type CustomNodeType } from './CustomNode';
import TreeEdge from './TreeEdge';
import DataEdge from './DataEdge';
import type { TreeNode, NodeCategory, Pin } from '../types';

// 注册自定义节点类型
const nodeTypes = {
  custom: CustomNode,
};

// 注册自定义边类型
const edgeTypes = {
  tree: TreeEdge,
  data: DataEdge,
};

function treeNodeToFlowNode(
  node: TreeNode,
  getDefinition: (className: string) => import('../types/nodeDefinition').NodeDefinition | undefined
): CustomNodeType {
  return {
    id: node.id,
    type: 'custom',
    position: node.position,
    data: {
      label: node.nickname || node.type,
      treeNode: node,
      nodeDefinition: getDefinition(node.type),
    },
  };
}

interface NodeEditorProps {
  onPaneClick?: () => void;
}

function NodeEditorInner({ onPaneClick }: NodeEditorProps) {
  const addNode = useEditorStore((state) => state.addNode);
  const selectNodes = useEditorStore((state) => state.selectNodes);
  const updateNodePosition = useEditorStore((state) => state.updateNodePosition);
  const removeNode = useEditorStore((state) => state.removeNode);
  const addConnection = useEditorStore((state) => state.addConnection);
  const removeConnection = useEditorStore((state) => state.removeConnection);
  const removeDataConnection = useEditorStore((state) => state.removeDataConnection);
  const { getDefinition } = useNodeDefinitionStore();

  // 修正：使用 selector 订阅 currentTree，确保 store 更新时组件重渲染
  const currentTree = useEditorStore((state) => state.getCurrentTree());

  const { screenToFlowPosition } = useReactFlow();

  // 右键菜单状态
  const [contextMenu, setContextMenu] = useState<{
    isOpen: boolean;
    position: { x: number; y: number };
    screenPosition: { x: number; y: number };
    nodeId: string | null;
  }>({
    isOpen: false,
    position: { x: 0, y: 0 },
    screenPosition: { x: 0, y: 0 },
    nodeId: null,
  });

  const selectedNodeIds = useEditorStore((state) => state.selectedNodeIds);

  // 优化的节点转换和缓存逻辑，显著提升大树拖拽启动性能
  const nodeCache = useMemo(() => new Map<string, CustomNodeType>(), []);

  const { flowNodes, flowEdges } = useMemo(() => {
    if (!currentTree) {
      return { flowNodes: [], flowEdges: [] };
    }

    const edges: Edge[] = [];

    // 1. 计算被折叠隐藏的节点 ID 和 有效禁用状态
    const hiddenNodeIds = new Set<string>();
    const effectiveDisabledIds = new Set<string>();

    const processTreeStatesRecursive = (id: string, isParentFolded: boolean, isParentDisabled: boolean) => {
      const node = currentTree.nodes.get(id);
      if (!node) return;

      const isEffectivelyDisabled = isParentDisabled || node.disabled;
      if (isEffectivelyDisabled) effectiveDisabledIds.add(id);

      if (isParentFolded) {
        hiddenNodeIds.add(id);
      }

      const shouldHideChildren = isParentFolded || node.isFolded;
      // 优化：在此循环中预先构建连接器分组，避免多次 filter
      currentTree.connections
        .forEach(c => {
          if (c.parentNodeId === id) {
            processTreeStatesRecursive(c.childNodeId, !!shouldHideChildren, isEffectivelyDisabled);
          }
        });
    };

    // 找到所有顶层节点开始遍历
    const childIds = new Set(currentTree.connections.map(c => c.childNodeId));
    currentTree.nodes.forEach((_, id) => {
      if (!childIds.has(id)) {
        processTreeStatesRecursive(id, false, false);
      }
    });

    // 2. 转换节点：利用缓存避免 redundant object creation
    const newNodes: CustomNodeType[] = [];
    currentTree.nodes.forEach((node: TreeNode) => {
      if (hiddenNodeIds.has(node.id)) return;

      const isSelected = selectedNodeIds.includes(node.id);
      const isEffectivelyDisabled = effectiveDisabledIds.has(node.id);

      // 检查缓存
      const cached = nodeCache.get(node.id);
      if (cached &&
        cached.data.treeNode === node &&
        cached.selected === isSelected &&
        cached.data.isEffectivelyDisabled === isEffectivelyDisabled &&
        cached.position.x === node.position.x &&
        cached.position.y === node.position.y) {
        newNodes.push(cached);
      } else {
        const newNode: CustomNodeType = {
          ...treeNodeToFlowNode(node, getDefinition),
          deletable: node.type !== 'Root',
          selected: isSelected,
          data: {
            ...treeNodeToFlowNode(node, getDefinition).data,
            isEffectivelyDisabled,
          }
        };
        nodeCache.set(node.id, newNode);
        newNodes.push(newNode);
      }
    });

    // 3. 构建边
    const connectionsByParent = new Map<string, typeof currentTree.connections>();
    currentTree.connections.forEach((conn) => {
      const group = connectionsByParent.get(conn.parentNodeId) || [];
      group.push(conn);
      connectionsByParent.set(conn.parentNodeId, group);
    });

    connectionsByParent.forEach((conns) => {
      const siblingTargetIds = conns.map(c => c.childNodeId);
      conns.forEach((conn) => {
        if (!hiddenNodeIds.has(conn.parentNodeId) && !hiddenNodeIds.has(conn.childNodeId)) {
          edges.push({
            id: conn.id,
            source: conn.parentNodeId,
            sourceHandle: conn.parentConnector,
            target: conn.childNodeId,
            targetHandle: 'tree-target',
            type: 'tree',
            data: { siblingTargetIds },
          });
        }
      });
    });

    // 数据连接边
    currentTree.dataConnections.forEach((dataConn) => {
      if (!hiddenNodeIds.has(dataConn.fromNodeId) && !hiddenNodeIds.has(dataConn.toNodeId)) {
        edges.push({
          id: dataConn.id,
          source: dataConn.fromNodeId,
          sourceHandle: `pin-out-${dataConn.fromPinName}`,
          target: dataConn.toNodeId,
          targetHandle: `pin-in-${dataConn.toPinName}`,
          type: 'data',
          data: {
            fromPinName: dataConn.fromPinName,
            toPinName: dataConn.toPinName,
          },
        });
      }
    });

    return { flowNodes: newNodes, flowEdges: edges };
  }, [currentTree, getDefinition, selectedNodeIds, nodeCache]);

  const [nodes, setNodes, onNodesChangeBase] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChangeBase] = useEdgesState<Edge>([]);

  // 当树变化时更新节点和边
  useEffect(() => {
    setNodes(flowNodes);
    setEdges(flowEdges);
  }, [flowNodes, flowEdges, setNodes, setEdges]);

  const addDataConnection = useEditorStore((state) => state.addDataConnection);

  const recordHistoryStart = useEditorStore((state) => state.recordHistoryStart);
  const finalizeContinuousAction = useEditorStore((state) => state.finalizeContinuousAction);

  // 自定义 onNodesChange：处理删除和选择
  const onNodesChange = useCallback((changes: import('@xyflow/react').NodeChange<Node>[]) => {
    // 关键优化：位置更新(position)不再通过 onNodesChange 同步到 store
    // 而是通过 onNodeDragStop 在松手时一次性同步。
    onNodesChangeBase(changes);

    changes.forEach((change) => {
      // 处理删除
      if (change.type === 'remove') {
        removeNode(change.id);
      }
    });
  }, [onNodesChangeBase, removeNode]);

  const onNodeDragStart = useCallback(() => {
    recordHistoryStart();
  }, [recordHistoryStart]);

  const onNodeDragStop = useCallback((_event: any, node: Node) => {
    // 松手时吸附到网格
    const gridSize = 15;
    const snappedX = Math.round(node.position.x / gridSize) * gridSize;
    const snappedY = Math.round(node.position.y / gridSize) * gridSize;
    updateNodePosition(node.id, snappedX, snappedY);
    finalizeContinuousAction();
  }, [updateNodePosition, finalizeContinuousAction]);

  // 自定义 onEdgesChange：处理连接的删除
  const onEdgesChange = useCallback((changes: import('@xyflow/react').EdgeChange<Edge>[]) => {
    onEdgesChangeBase(changes);

    changes.forEach((change) => {
      if (change.type === 'remove') {
        if (change.id.startsWith('data-')) {
          removeDataConnection(change.id);
        } else {
          removeConnection(change.id);
        }
      }
    });
  }, [onEdgesChangeBase, removeDataConnection, removeConnection]);

  const onConnect: OnConnect = useCallback(
    async (params) => {
      // 判断是数据连接还是树连接
      // 数据连接的 handle id 格式: pin-in-{pinName} 或 pin-out-{pinName}
      // 树连接的 handle id 格式: connector-{index} 或 child
      const isDataConnection = params.sourceHandle?.startsWith('pin-') || params.targetHandle?.startsWith('pin-');

      if (isDataConnection && params.sourceHandle && params.targetHandle) {
        // 解析 pin 名称
        const fromPinName = params.sourceHandle.replace('pin-out-', '');
        const toPinName = params.targetHandle.replace('pin-in-', '');

        // 获取节点并检查 Pin 类型是否匹配
        const currentTree = useEditorStore.getState().getCurrentTree();
        const fromNode = currentTree?.nodes.get(params.source!);
        const toNode = currentTree?.nodes.get(params.target!);
        const fromPin = fromNode?.pins.find(p => p.name === fromPinName);
        const toPin = toNode?.pins.find(p => p.name === toPinName);

        if (fromPin && toPin) {
          // 1. 检查类型匹配
          const typeMatch = fromPin.valueType === toPin.valueType;
          const countMatch = fromPin.countType === toPin.countType;

          if (!typeMatch || !countMatch) {
            const { useNotificationStore } = await import('../stores/notificationStore');
            const { logger } = await import('../utils/logger');
            const errorMsg = `Connection failed: Type mismatch (${fromPin.valueType}${fromPin.countType === 'list' ? '[]' : ''} vs ${toPin.valueType}${toPin.countType === 'list' ? '[]' : ''})`;
            useNotificationStore.getState().notify(errorMsg, 'error');
            logger.error(errorMsg);
            return;
          }

          // 3. 检查有效禁用状态匹配
          const isEffectivelyDisabled = (nodeId: string, tree: import('../types').Tree): boolean => {
            let current = nodeId;
            while (current) {
              const n = tree.nodes.get(current);
              if (n?.disabled) return true;
              const parentConn = tree.connections.find(c => c.childNodeId === current);
              if (!parentConn) break;
              current = parentConn.parentNodeId;
            }
            return false;
          };

          if (isEffectivelyDisabled(params.source!, currentTree!) !== isEffectivelyDisabled(params.target!, currentTree!)) {
            const { useNotificationStore } = await import('../stores/notificationStore');
            const { logger } = await import('../utils/logger');
            const errorMsg = `Connection failed: Cannot connect enabled node with disabled node`;
            useNotificationStore.getState().notify(errorMsg, 'error');
            logger.error(errorMsg);
            return;
          }

          // 4. 检查是否在同一棵树下
          const getNodeRootId = (nodeId: string, tree: import('../types').Tree): string => {
            let current = nodeId;
            while (true) {
              const parentConn = tree.connections.find(c => c.childNodeId === current);
              if (!parentConn) break;
              current = parentConn.parentNodeId;
            }
            return current;
          };

          if (getNodeRootId(params.source!, currentTree!) !== getNodeRootId(params.target!, currentTree!)) {
            const { useNotificationStore } = await import('../stores/notificationStore');
            const { logger } = await import('../utils/logger');
            const errorMsg = `Connection failed: Nodes must be in the same tree`;
            useNotificationStore.getState().notify(errorMsg, 'error');
            logger.error(errorMsg);
            return;
          }
        }

        // 创建数据连接并添加到 store
        const dataConn = {
          id: `data-${Date.now()}`,
          fromNodeId: params.source!,
          fromPinName,
          toNodeId: params.target!,
          toPinName,
        };
        addDataConnection(dataConn);
      } else {
        // 树连接：添加到 store，自动触发 UID 重新计算
        const parentConnector = params.sourceHandle || 'default';
        const conn = {
          id: `conn-${params.source}-${params.target}-${parentConnector}`,
          parentNodeId: params.source!,
          parentConnector,
          childNodeId: params.target!,
        };
        addConnection(conn);
      }
    },
    [setEdges, addDataConnection, addConnection]
  );

  const isValidConnection: import('@xyflow/react').IsValidConnection = useCallback(
    (edge) => {
      const { source, target, sourceHandle, targetHandle } = edge;
      if (!source || !target || source === target) return false;

      const isDataConnection = sourceHandle?.startsWith('pin-') || targetHandle?.startsWith('pin-');
      const currentTree = useEditorStore.getState().getCurrentTree();
      if (!currentTree) return false;

      if (isDataConnection) {
        // 数据连接已经在 onConnect 中处理了大部分逻辑，这里简单放行
        return true;
      }

      // --- 树连接校验 ---

      // 1. 目标节点 (Child) 只能有一个父节点 (Tree Connection)
      const hasParent = currentTree.connections.some(c => c.childNodeId === target);
      if (hasParent) return false;

      // 2. 检查源连接器 (Parent Connector) 的 MaxChildren 限制
      if (sourceHandle) {
        const sourceNode = currentTree.nodes.get(source);
        if (sourceNode) {
          if (sourceHandle === 'condition') {
            // condition 连接器固定只能有一个子节点
            const childrenCount = currentTree.connections.filter(c => c.parentNodeId === source && c.parentConnector === 'condition').length;
            if (childrenCount >= 1) return false;
          } else {
            const def = getDefinition(sourceNode.type);
            const connectorDef = def?.childConnectors.find(c => c.name === sourceHandle);
            if (connectorDef && connectorDef.maxChildren !== undefined) {
              const childrenCount = currentTree.connections.filter(c => c.parentNodeId === source && c.parentConnector === sourceHandle).length;
              if (childrenCount >= connectorDef.maxChildren) return false;
            }
          }
        }
      }

      // 3. 防止循环引用 (Cycle Detection)
      const isDescendant = (nodeId: string, potentialAncestorId: string): boolean => {
        const connections = currentTree.connections.filter(c => c.parentNodeId === nodeId);
        for (const conn of connections) {
          if (conn.childNodeId === potentialAncestorId) return true;
          if (isDescendant(conn.childNodeId, potentialAncestorId)) return true;
        }
        return false;
      };

      if (isDescendant(target, source)) return false;

      return true;
    },
    [getDefinition]
  );

  const handleContextMenu = useCallback((e: MouseEvent) => {
    e.preventDefault();

    const bounds = e.currentTarget.getBoundingClientRect();
    const relativeX = e.clientX - bounds.left;
    const relativeY = e.clientY - bounds.top;

    setContextMenu({
      isOpen: true,
      position: { x: relativeX, y: relativeY },
      screenPosition: { x: e.clientX, y: e.clientY },
      nodeId: null, // 面板右键
    });
  }, []);

  const onNodeContextMenu = useCallback((e: React.MouseEvent, node: Node) => {
    e.preventDefault();
    e.stopPropagation();

    const bounds = document.querySelector('.react-flow__renderer')?.getBoundingClientRect();
    if (!bounds) return;

    const relativeX = e.clientX - bounds.left;
    const relativeY = e.clientY - bounds.top;

    setContextMenu({
      isOpen: true,
      position: { x: relativeX, y: relativeY },
      screenPosition: { x: e.clientX, y: e.clientY },
      nodeId: node.id, // 节点右键
    });
  }, []);

  const handleAddNode = useCallback((nodeClass: string, _position: { x: number; y: number }) => {
    const def = getDefinition(nodeClass);
    if (!def) return;

    const pins: Pin[] = def.pins.map(pinDef => {
      const bindingType = pinDef.constType === 'pointer' ? 'pointer' : 'const';
      const countType = pinDef.arrayType === 'list' ? 'list' : 'scalar';

      return {
        name: pinDef.name,
        valueType: pinDef.valueType,
        countType,
        bindingType,
        binding: bindingType === 'pointer'
          ? { type: 'pointer', variableName: '', isLocal: false }
          : { type: 'const', value: pinDef.defaultValue },
        enableType: pinDef.enableType,
        isInput: pinDef.isInput,
        enumValues: pinDef.enumValues,
        allowedValueTypes: pinDef.allowedValueTypes || [pinDef.valueType],
        vTypeGroup: pinDef.vTypeGroup,
        isCountTypeFixed: pinDef.arrayType !== 'switchable',
        isBindingTypeFixed: pinDef.constType !== 'switchable',
      };
    });

    const newNode: TreeNode = {
      id: `node-${Date.now()}`,
      guid: Date.now(),
      type: nodeClass,
      category: def.category as NodeCategory,
      position: screenToFlowPosition(contextMenu.screenPosition),
      pins,
      childrenIds: [],
      disabled: false,
    };

    addNode(newNode);
  }, [getDefinition, addNode, contextMenu.screenPosition, screenToFlowPosition]);

  if (!currentTree) {
    return (
      <div className="w-full h-full flex items-center justify-center bg-gray-900 text-gray-500">
        Select a tree file to open
      </div>
    );
  }

  return (
    <div className="w-full h-full relative" onContextMenu={handleContextMenu}>
      <ReactFlow
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onNodeDragStart={onNodeDragStart}
        onNodeDragStop={onNodeDragStop}
        isValidConnection={isValidConnection}
        selectNodesOnDrag={false}
        onPaneClick={() => {
          setContextMenu(prev => ({ ...prev, isOpen: false }));
          onPaneClick?.();
        }}
        onNodeContextMenu={onNodeContextMenu}
        onSelectionChange={({ nodes }) => {
          // 同步选中状态到 Store
          const newNodeIds = nodes.map(n => n.id);
          const currentSelected = useEditorStore.getState().selectedNodeIds;
          const isSame = newNodeIds.length === currentSelected.length &&
            newNodeIds.every(id => currentSelected.includes(id));

          if (!isSame) {
            selectNodes(newNodeIds);
          }
        }}
        fitView
        snapToGrid={false}
        snapGrid={[15, 15]}
        panOnDrag={[1, 2]}
        selectionOnDrag
        proOptions={{ hideAttribution: true }}
        className="bg-gray-900"
        deleteKeyCode={['Backspace', 'Delete']}
      >
        <Background variant={BackgroundVariant.Dots} gap={20} size={1} color="#374151" />
        <Controls
          style={{
            backgroundColor: '#1f2937',
            border: '1px solid #374151',
          }}
        />
        <MiniMap
          nodeStrokeWidth={3}
          zoomable
          pannable
          style={{
            backgroundColor: '#1f2937',
            border: '1px solid #374151',
          }}
          maskColor="rgba(0, 0, 0, 0.6)"
        />
      </ReactFlow>

      <NodeContextMenu
        isOpen={contextMenu.isOpen}
        position={contextMenu.position}
        nodeId={contextMenu.nodeId}
        onClose={() => setContextMenu(prev => ({ ...prev, isOpen: false }))}
        onAddNode={handleAddNode}
      />
    </div>
  );
}

// 包装组件，提供 ReactFlowProvider
export function NodeEditor(props: NodeEditorProps) {
  return (
    <ReactFlowProvider>
      <NodeEditorInner {...props} />
    </ReactFlowProvider>
  );
}
