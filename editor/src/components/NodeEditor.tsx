import { useCallback, useMemo, useEffect, useState } from 'react';
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  addEdge,
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
  const getCurrentTree = useEditorStore((state) => state.getCurrentTree);
  const addNode = useEditorStore((state) => state.addNode);
  const selectNodes = useEditorStore((state) => state.selectNodes);
  const { getDefinition } = useNodeDefinitionStore();
  const currentTree = getCurrentTree();
  const { screenToFlowPosition } = useReactFlow();
  
  // 右键菜单状态
  const [contextMenu, setContextMenu] = useState<{
    isOpen: boolean;
    position: { x: number; y: number };
    screenPosition: { x: number; y: number };
  }>({
    isOpen: false,
    position: { x: 0, y: 0 },
    screenPosition: { x: 0, y: 0 },
  });
  
  const { flowNodes, flowEdges } = useMemo(() => {
    if (!currentTree) {
      return { flowNodes: [], flowEdges: [] };
    }
    
    const nodes: CustomNodeType[] = [];
    const edges: Edge[] = [];
    
    currentTree.nodes.forEach((node: TreeNode) => {
      nodes.push(treeNodeToFlowNode(node, getDefinition));
    });
    
    // 按父节点分组连接，收集兄弟节点 ID
    const connectionsByParent = new Map<string, typeof currentTree.connections>();
    currentTree.connections.forEach((conn) => {
      const group = connectionsByParent.get(conn.parentNodeId) || [];
      group.push(conn);
      connectionsByParent.set(conn.parentNodeId, group);
    });
    
    // 为每条边传递兄弟节点 ID 列表，用于计算共享的水平线高度
    connectionsByParent.forEach((conns) => {
      const siblingTargetIds = conns.map(c => c.childNodeId);
      
      conns.forEach((conn) => {
        // 多连接器时使用连接器名称，否则使用 tree-source
        const sourceHandle = conn.parentConnector && conn.parentConnector !== 'default' 
          ? conn.parentConnector 
          : 'tree-source';
        edges.push({
          id: conn.id,
          source: conn.parentNodeId,
          sourceHandle,
          target: conn.childNodeId,
          targetHandle: 'tree-target',
          type: 'tree',
          data: { siblingTargetIds },
        });
      });
    });
    
    // 添加数据连接的边
    currentTree.dataConnections.forEach((dataConn) => {
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
    });
    
    return { flowNodes: nodes, flowEdges: edges };
  }, [currentTree, getDefinition]);
  
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);

  // 当树变化时更新节点和边
  useEffect(() => {
    setNodes(flowNodes);
    setEdges(flowEdges);
  }, [flowNodes, flowEdges, setNodes, setEdges]);

  const onConnect: OnConnect = useCallback(
    (params) => setEdges((eds) => addEdge(params, eds)),
    [setEdges]
  );
  
  const handleContextMenu = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    
    // 获取相对于容器的位置（用于菜单显示）
    const bounds = e.currentTarget.getBoundingClientRect();
    const relativeX = e.clientX - bounds.left;
    const relativeY = e.clientY - bounds.top;
    
    setContextMenu({
      isOpen: true,
      position: { x: relativeX, y: relativeY },
      screenPosition: { x: e.clientX, y: e.clientY },
    });
  }, []);
  
  const handleAddNode = useCallback((nodeClass: string, _position: { x: number; y: number }) => {
    const def = getDefinition(nodeClass);
    if (!def) return;
    
    // 从节点定义创建默认 Pin
    const pins: Pin[] = def.pins.map(pinDef => {
      // 根据 constType 决定默认绑定类型
      const bindingType = pinDef.constType === 'pointer' ? 'pointer' : 'const';
      const binding: Pin['binding'] = bindingType === 'pointer'
        ? { type: 'pointer', variableName: '', isLocal: false }
        : { type: 'const', value: pinDef.defaultValue };
      
      // 根据 arrayType 决定 countType
      const countType = pinDef.arrayType === 'list' ? 'list' : 'scalar';
      
      return {
        name: pinDef.name,
        valueType: pinDef.valueType,
        countType,
        bindingType,
        binding,
        enableType: pinDef.enableType,
        isInput: pinDef.isInput,
        enumValues: pinDef.enumValues,
        allowedValueTypes: [pinDef.valueType],
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
        onPaneClick={() => {
          setContextMenu(prev => ({ ...prev, isOpen: false }));
          onPaneClick?.();
        }}
        onNodeClick={(_, node) => {
          selectNodes([node.id]);
        }}
        fitView
        panOnDrag={[1, 2]}
        selectionOnDrag
        proOptions={{ hideAttribution: true }}
        className="bg-gray-900"
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
