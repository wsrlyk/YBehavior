import { useCallback } from 'react';
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

const initialNodes: Node[] = [
  {
    id: 'root',
    type: 'default',
    position: { x: 400, y: 50 },
    data: { label: 'Root' },
  },
  {
    id: 'sequence-1',
    type: 'default',
    position: { x: 400, y: 150 },
    data: { label: 'Sequence ➜➜➜' },
  },
  {
    id: 'action-1',
    type: 'default',
    position: { x: 250, y: 280 },
    data: { label: 'Action 1' },
  },
  {
    id: 'action-2',
    type: 'default',
    position: { x: 550, y: 280 },
    data: { label: 'Action 2' },
  },
];

const initialEdges: Edge[] = [
  { id: 'e-root-seq', source: 'root', target: 'sequence-1' },
  { id: 'e-seq-a1', source: 'sequence-1', target: 'action-1' },
  { id: 'e-seq-a2', source: 'sequence-1', target: 'action-2' },
];

export function NodeEditor() {
  const [nodes, , onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

  const onConnect: OnConnect = useCallback(
    (params) => setEdges((eds) => addEdge(params, eds)),
    [setEdges]
  );

  return (
    <div className="w-full h-full">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        fitView
        className="bg-gray-100 dark:bg-gray-900"
      >
        <Background variant={BackgroundVariant.Dots} gap={20} size={1} />
        <Controls />
        <MiniMap 
          nodeStrokeWidth={3}
          zoomable
          pannable
        />
      </ReactFlow>
    </div>
  );
}
