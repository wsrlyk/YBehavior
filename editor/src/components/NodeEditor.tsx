import { useCallback, useMemo, useEffect, useState } from 'react';
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  addEdge,
  type Node,
  type Edge,
  type OnConnect,
  BackgroundVariant,
} from '@xyflow/react';
import { useEditorStore } from '../stores/editorStore';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { NodeContextMenu } from './NodeContextMenu';
import type { TreeNode, NodeCategory } from '../types';

// 低饱和度节点颜色
const NODE_COLORS: Record<string, string> = {
  composite: '#5A8A5E',  // 低饱和度绿色
  decorator: '#5A7A9A',  // 低饱和度蓝色
  action: '#B08050',     // 低饱和度橙色
  condition: '#7A5A8A',  // 低饱和度紫色
};

function treeNodeToFlowNode(node: TreeNode): Node {
  return {
    id: node.id,
    type: 'default',
    position: node.position,
    data: { 
      label: node.nickname || node.type,
      treeNode: node,
    },
    style: {
      background: NODE_COLORS[node.category] || '#666',
      color: 'white',
      border: 'none',
      borderRadius: '4px',
      padding: '8px 12px',
      fontSize: '12px',
    },
  };
}

interface NodeEditorProps {
  onPaneClick?: () => void;
}

export function NodeEditor({ onPaneClick }: NodeEditorProps) {
  const getCurrentTree = useEditorStore((state) => state.getCurrentTree);
  const addNode = useEditorStore((state) => state.addNode);
  const { getDefinition } = useNodeDefinitionStore();
  const currentTree = getCurrentTree();
  
  // 右键菜单状态
  const [contextMenu, setContextMenu] = useState<{
    isOpen: boolean;
    position: { x: number; y: number };
    flowPosition: { x: number; y: number };
  }>({ isOpen: false, position: { x: 0, y: 0 }, flowPosition: { x: 0, y: 0 } });
  
  const { flowNodes, flowEdges } = useMemo(() => {
    if (!currentTree) {
      return { flowNodes: [], flowEdges: [] };
    }
    
    const nodes: Node[] = [];
    const edges: Edge[] = [];
    
    currentTree.nodes.forEach((node: TreeNode) => {
      nodes.push(treeNodeToFlowNode(node));
    });
    
    currentTree.connections.forEach((conn) => {
      edges.push({
        id: conn.id,
        source: conn.parentNodeId,
        target: conn.childNodeId,
        type: 'smoothstep',
      });
    });
    
    return { flowNodes: nodes, flowEdges: edges };
  }, [currentTree]);
  
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
    
    // 获取相对于容器的位置
    const bounds = e.currentTarget.getBoundingClientRect();
    const relativeX = e.clientX - bounds.left;
    const relativeY = e.clientY - bounds.top;
    
    setContextMenu({
      isOpen: true,
      position: { x: relativeX, y: relativeY },
      flowPosition: { x: relativeX, y: relativeY },
    });
  }, []);
  
  const handleAddNode = useCallback((nodeClass: string, _position: { x: number; y: number }) => {
    const def = getDefinition(nodeClass);
    if (!def) return;
    
    const newNode: TreeNode = {
      id: `node-${Date.now()}`,
      uid: Date.now(),
      type: nodeClass,
      category: def.category as NodeCategory,
      position: contextMenu.flowPosition,
      pins: [],
      childrenIds: [],
      disabled: false,
    };
    
    addNode(newNode);
  }, [getDefinition, addNode, contextMenu.flowPosition]);

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
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onPaneClick={() => {
          setContextMenu(prev => ({ ...prev, isOpen: false }));
          onPaneClick?.();
        }}
        fitView
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
