import { useCallback, useMemo, useEffect, useState, useRef, type MouseEvent } from 'react';
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
import { useEditorStore, getDescendantIds } from '../stores/editorStore';
import { useEditorMetaStore } from '../stores/editorMetaStore';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { useDebugStore } from '../stores/debugStore';
import { getTheme } from '../theme/theme';
import { NodeContextMenu } from './NodeContextMenu';

const theme = getTheme();
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

import { generateGUID } from '../utils/guidUtils';

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
  const removeNodes = useEditorStore((state) => state.removeNodes);
  const removeElements = useEditorStore((state) => state.removeElements);
  const addConnection = useEditorStore((state) => state.addConnection);
  const removeConnections = useEditorStore((state) => state.removeConnections);
  const removeDataConnections = useEditorStore((state) => state.removeDataConnections);
  const addDataConnection = useEditorStore((state) => state.addDataConnection);
  const duplicateSelectedNodes = useEditorStore((state) => state.duplicateSelectedNodes);
  const { getDefinition } = useNodeDefinitionStore();

  // 修正：使用 selector 订阅 currentTree，确保 store 更新时组件重渲染
  const currentTree = useEditorStore((state) => state.getCurrentTree());

  const { screenToFlowPosition, getNodes, getEdges, setCenter } = useReactFlow();
  const pendingCenterTarget = useEditorMetaStore(state => state.uiMeta.pendingCenterTarget);
  const setPendingCenterTarget = useEditorMetaStore(state => state.setPendingCenterTarget);

  useEffect(() => {
    if (pendingCenterTarget) {
      setCenter(pendingCenterTarget.x, pendingCenterTarget.y, { zoom: pendingCenterTarget.zoom || 1.2, duration: 400 });
      setPendingCenterTarget(undefined);
    }
  }, [pendingCenterTarget, setCenter, setPendingCenterTarget]);

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
  const isSelecting = useRef(false);

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
        const flowNode = treeNodeToFlowNode(node, getDefinition);
        const newNode: CustomNodeType = {
          ...flowNode,
          deletable: node.type !== 'Root',
          selected: isSelected,
          data: {
            ...flowNode.data,
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
            data: { siblingTargetIds, label: conn.label },
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


  // Keyboard Shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ignore if in input/textarea/select
      const activeElement = document.activeElement;
      if (
        activeElement instanceof HTMLInputElement ||
        activeElement instanceof HTMLTextAreaElement ||
        activeElement instanceof HTMLSelectElement
      ) {
        return;
      }

      if (e.ctrlKey && (e.key === 'd' || e.key === 'D')) {
        e.preventDefault();
        duplicateSelectedNodes();
      }

      if (e.key === 'Delete' || e.key === 'Backspace') {
        const selectedIds = useEditorStore.getState().selectedNodeIds;
        const selectedEdges = getEdges().filter(edge => edge.selected);
        if (selectedIds.length === 0 && selectedEdges.length === 0) return;

        e.preventDefault();
        e.stopPropagation();
        const tree = useEditorStore.getState().getCurrentTree();
        if (tree) {
          const allIdsToRemove = new Set<string>();
          if (e.shiftKey) {
            selectedIds.forEach(id => {
              allIdsToRemove.add(id);
              getDescendantIds(tree, id).forEach((childId: string) => allIdsToRemove.add(childId));
            });
          } else {
            selectedIds.forEach(id => allIdsToRemove.add(id));
          }

          const connsToRemove = selectedEdges.filter(edge => !edge.id.startsWith('data-')).map(edge => edge.id);
          const dataConnsToRemove = selectedEdges.filter(edge => edge.id.startsWith('data-')).map(edge => edge.id);

          removeElements(Array.from(allIdsToRemove), connsToRemove, dataConnsToRemove);
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [duplicateSelectedNodes, removeNodes]);

  const recordHistoryStart = useEditorStore((state) => state.recordHistoryStart);
  const finalizeContinuousAction = useEditorStore((state) => state.finalizeContinuousAction);

  // 自定义 onNodesChange：处理删除和选择
  const isDebugConnected = useDebugStore((s) => s.isConnected);
  const onNodesChange = useCallback((changes: import('@xyflow/react').NodeChange<Node>[]) => {
    // Filter out modification changes when in read-only debug mode
    const filteredChanges = isDebugConnected
      ? changes.filter(c => c.type === 'select' || c.type === 'dimensions') // Allow selection and dimension updates
      : changes;

    onNodesChangeBase(filteredChanges);

    let selectionChanged = false;
    const currentSelected = useEditorStore.getState().selectedNodeIds;
    const newSelected = new Set(currentSelected);

    const idsToRemove: string[] = [];
    changes.forEach((change) => {
      // 处理删除
      if (change.type === 'remove') {
        idsToRemove.push(change.id);
      }
      // 处理选择
      if (change.type === 'select') {
        if (change.selected) {
          newSelected.add(change.id);
        } else {
          newSelected.delete(change.id);
        }
        selectionChanged = true;
      }
    });

    if (idsToRemove.length > 0) {
      removeNodes(idsToRemove);
    }

    if (selectionChanged && !isSelecting.current) {
      const newNodeIds = Array.from(newSelected);
      const isSame = newNodeIds.length === currentSelected.length &&
        newNodeIds.every(id => currentSelected.includes(id));

      if (!isSame) {
        selectNodes(newNodeIds);
      }
    }
  }, [onNodesChangeBase, removeNodes, selectNodes, isDebugConnected]);

  const onSelectionStart = useCallback(() => {
    isSelecting.current = true;
  }, []);

  const onSelectionEnd = useCallback(() => {
    isSelecting.current = false;
    // 获取当前所有选中的节点 ID
    const selectedIds = nodes.filter(n => n.selected).map(n => n.id);
    const currentSelected = useEditorStore.getState().selectedNodeIds;
    const isSame = selectedIds.length === currentSelected.length &&
      selectedIds.every(id => currentSelected.includes(id));

    if (!isSame) {
      selectNodes(selectedIds);
    }
  }, [nodes, selectNodes]);

  const updateNodesPositions = useEditorStore((state) => state.updateNodesPositions);

  const dragSession = useRef<{
    isShiftDrag: boolean;
    descendantIds: string[];
    initialPositions: Map<string, { x: number; y: number }>;
    startPos: { x: number; y: number };
  } | null>(null);

  const onNodeDragStart = useCallback((event: any, draggedNode: Node) => {
    recordHistoryStart();

    if (event.shiftKey) {
      const tree = useEditorStore.getState().getCurrentTree();
      if (tree) {
        const descendants = getDescendantIds(tree, draggedNode.id);
        const initialPositions = new Map<string, { x: number; y: number }>();
        const currentNodes = getNodes();
        descendants.forEach(id => {
          const n = currentNodes.find(fn => fn.id === id);
          if (n) initialPositions.set(id, { ...n.position });
        });

        dragSession.current = {
          isShiftDrag: true,
          descendantIds: descendants,
          initialPositions,
          startPos: { ...draggedNode.position }
        };
      }
    } else {
      dragSession.current = null;
    }
  }, [recordHistoryStart, getNodes]);

  const onNodeDrag = useCallback((_event: any, node: Node) => {
    if (dragSession.current?.isShiftDrag) {
      const { descendantIds, initialPositions, startPos } = dragSession.current;
      const dx = node.position.x - startPos.x;
      const dy = node.position.y - startPos.y;

      setNodes((prevNodes) =>
        prevNodes.map((n) => {
          if (descendantIds.includes(n.id)) {
            const initPos = initialPositions.get(n.id);
            if (initPos) {
              return {
                ...n,
                position: {
                  x: initPos.x + dx,
                  y: initPos.y + dy
                }
              };
            }
          }
          return n;
        })
      );
    }
  }, [setNodes]);

  const onNodeDragStop = useCallback((_event: any, draggedNode: Node, draggedNodes: Node[]) => {
    // 松手时吸附到网格
    const gridSize = 15;
    const session = dragSession.current;

    // 使用 Map 收集所有需要更新的节点，避免重复
    const updateMap = new Map<string, { x: number, y: number }>();

    // 1. 处理 React Flow 自动拖拽的所有节点（多选拖拽、单选拖拽）
    draggedNodes.forEach(n => {
      updateMap.set(n.id, {
        x: Math.round(n.position.x / gridSize) * gridSize,
        y: Math.round(n.position.y / gridSize) * gridSize
      });
    });

    // 2. 如果是 Shift 拖拽，还需要包含手动移动的子孙节点
    // 这些节点可能没被选中，所以不在 draggedNodes 中
    if (session?.isShiftDrag) {
      const currentNodes = getNodes();

      // 确保被拖拽的主节点也被更新（通常在 draggedNodes 中，但这里通过 Map 覆盖确保万一）
      updateMap.set(draggedNode.id, {
        x: Math.round(draggedNode.position.x / gridSize) * gridSize,
        y: Math.round(draggedNode.position.y / gridSize) * gridSize
      });

      session.descendantIds.forEach(id => {
        const n = currentNodes.find(fn => fn.id === id);
        if (n) {
          updateMap.set(n.id, {
            x: Math.round(n.position.x / gridSize) * gridSize,
            y: Math.round(n.position.y / gridSize) * gridSize
          });
        }
      });
    }

    const updates = Array.from(updateMap.entries()).map(([id, pos]) => ({
      id,
      x: pos.x,
      y: pos.y
    }));

    if (updates.length > 0) {
      updateNodesPositions(updates);
    }

    finalizeContinuousAction();
    dragSession.current = null;
  }, [updateNodesPositions, finalizeContinuousAction, getNodes]);

  // 自定义 onEdgesChange：处理连接的删除
  const onEdgesChange = useCallback((changes: import('@xyflow/react').EdgeChange<Edge>[]) => {
    onEdgesChangeBase(changes);

    const connsToRemove: string[] = [];
    const dataConnsToRemove: string[] = [];

    changes.forEach((change) => {
      if (change.type === 'remove') {
        if (change.id.startsWith('data-')) {
          dataConnsToRemove.push(change.id);
        } else {
          connsToRemove.push(change.id);
        }
      }
    });

    if (connsToRemove.length > 0) {
      removeConnections(connsToRemove);
    }
    if (dataConnsToRemove.length > 0) {
      removeDataConnections(dataConnsToRemove);
    }
  }, [onEdgesChangeBase, removeDataConnections, removeConnections]);

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
            const errorMsg = `Connection failed: Type mismatch (${fromPin.valueType}${fromPin.countType === 'list' ? '[]' : ''} vs ${toPin.valueType}${toPin.countType === 'list' ? '[]' : ''})`;
            useNotificationStore.getState().notify(errorMsg, 'error');
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
            const errorMsg = `Connection failed: Cannot connect enabled node with disabled node`;
            useNotificationStore.getState().notify(errorMsg, 'error');
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
            const errorMsg = `Connection failed: Nodes must be in the same tree`;
            useNotificationStore.getState().notify(errorMsg, 'error');
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
        desc: pinDef.desc,
      };
    });

    const existingGUIDs = new Set<number>();
    if (currentTree) {
      currentTree.nodes.forEach(n => existingGUIDs.add(n.guid));
    }

    const newGuid = generateGUID(existingGUIDs);
    const timestamp = Date.now();

    const newNode: TreeNode = {
      id: `node-${timestamp}`,
      guid: newGuid,
      type: nodeClass,
      category: def.category as NodeCategory,
      position: screenToFlowPosition(contextMenu.screenPosition),
      pins,
      childrenIds: [],
      disabled: false,
    };

    addNode(newNode);
  }, [getDefinition, addNode, contextMenu.screenPosition, screenToFlowPosition, currentTree]);

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
        onNodeDrag={onNodeDrag}
        onNodeDragStop={onNodeDragStop}
        isValidConnection={isValidConnection}
        selectNodesOnDrag={false}
        onPaneClick={() => {
          setContextMenu(prev => ({ ...prev, isOpen: false }));
          onPaneClick?.();
        }}
        onNodeContextMenu={onNodeContextMenu}
        onSelectionStart={onSelectionStart}
        onSelectionEnd={onSelectionEnd}
        fitView
        snapToGrid={false}
        snapGrid={[15, 15]}
        panOnDrag={[1, 2]}
        selectionOnDrag
        selectionKeyCode={null}
        proOptions={{ hideAttribution: true }}
        style={{ backgroundColor: theme.ui.background }}
        deleteKeyCode={null}
      >
        <Background variant={BackgroundVariant.Dots} gap={20} size={1} color={theme.ui.gridDots} />
        <Controls
          style={{
            backgroundColor: theme.ui.panelBg,
            borderColor: theme.ui.border,
            fill: theme.ui.textMain
          }}
        />
        <MiniMap
          nodeStrokeWidth={3}
          zoomable
          pannable
          style={{
            backgroundColor: theme.ui.panelBg,
            borderColor: theme.ui.border,
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
