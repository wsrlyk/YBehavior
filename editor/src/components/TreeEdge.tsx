import { memo } from 'react';
import { BaseEdge, type EdgeProps, type Edge, useStore, EdgeLabelRenderer } from '@xyflow/react';
import { useEditorStore } from '../stores/editorStore';
import { useDebugStore } from '../stores/debugStore';
import { useShallow } from 'zustand/react/shallow';
import { NodeState } from '../types/debug';

export interface TreeEdgeData extends Record<string, unknown> {
  siblingTargetIds?: string[];  // 兄弟边的目标节点 ID 列表
  label?: string;               // 连线标签
}

export type TreeEdgeType = Edge<TreeEdgeData, 'tree'>;

/**
 * 自定义树形连线
 * 算法：同一父节点的所有子边使用统一的水平线高度
 * 实时从 ReactFlow store 获取节点位置，支持拖动时更新
 */
function TreeEdge({
  id,
  source,
  sourceX,
  sourceY,
  targetX,
  targetY,
  target,
  style,
  markerEnd,
  data,
  selected,
}: EdgeProps) {
  const edgeData = data as TreeEdgeData | undefined;
  const siblingTargetIds = edgeData?.siblingTargetIds || [];
  const label = edgeData?.label;

  // 从 store 获取水平线高度
  const horizontalY = useStore((state) => {
    // 1. 获取父节点的位置和尺寸
    const sourceNode = source ? state.nodeLookup.get(source) : null;
    if (!sourceNode) {
      return sourceY + 30;
    }

    const height = sourceNode.measured?.height ?? 60;
    const parentBottomY = sourceNode.position.y + height;

    // 2. 遍历所有兄弟节点，找到最小的 Y（最高的节点）
    let minChildY = Infinity;
    for (const targetId of siblingTargetIds) {
      const node = state.nodeLookup.get(targetId);
      if (node) {
        minChildY = Math.min(minChildY, node.position.y);
      }
    }
    if (minChildY === Infinity) {
      minChildY = parentBottomY + 100;
    }

    // 3. 计算水平线高度
    return Math.max(
      parentBottomY + 10,
      (parentBottomY + minChildY) / 2
    );
  });

  // Debug state integration
  const activeFilePath = useEditorStore((s) => s.activeFilePath);
  const fileName = activeFilePath?.split(/[\\/]/).pop()?.replace(/\.tree$/, '') || '';

  // Use target node's ID (Child) to get its debug state, representing the flow into/result of that child
  const { debugState, isPaused } = useDebugStore(
    useShallow(s => {
      if (!s.isConnected || !target) return { debugState: NodeState.Invalid, isPaused: false };

      const currentTree = useEditorStore.getState().getCurrentTree();
      const node = currentTree?.nodes.get(target);

      if (!node || node.uid === undefined) return { debugState: NodeState.Invalid, isPaused: s.isPaused };

      const state = s.getNodeRunState(fileName, node.uid);

      return {
        debugState: state ? state.final : NodeState.Invalid,
        isPaused: s.isPaused,
      };
    })
  );

  const getEdgeColor = (state: NodeState) => {
    switch (state) {
      case NodeState.Success: return '#22c55e'; // Green
      case NodeState.Failure: return '#3b82f6'; // Blue
      case NodeState.Running: return '#ec4899'; // Pink
      // Break usually doesn't apply to edge flux, but if needed:
      case NodeState.Break: return '#ef4444';
      default: return selected ? '#fff' : '#6b7280';
    }
  };

  const edgeColor = (debugState !== NodeState.Invalid) ? getEdgeColor(debugState) : (selected ? '#fff' : '#6b7280');
  const edgeWidth = (selected || debugState !== NodeState.Invalid) ? 3 : 2;

  // 绘制路径：直接从连接器位置（sourceX, sourceY）出发
  // 垂直下降到水平线，然后水平移动到目标 X，再垂直下降到目标
  const path = `M ${sourceX} ${sourceY} L ${sourceX} ${horizontalY} L ${targetX} ${horizontalY} L ${targetX} ${targetY}`;

  return (
    <>
      <BaseEdge
        id={id}
        path={path}
        style={{
          ...style,
          stroke: edgeColor,
          strokeWidth: edgeWidth,
          transition: 'stroke 0.2s, stroke-width 0.2s',
          opacity: (debugState !== NodeState.Invalid && isPaused) ? 0.6 : 1
        }}
        markerEnd={markerEnd}
      />
      {label && (
        <EdgeLabelRenderer>
          <div
            style={{
              position: 'absolute',
              transform: `translate(-50%, -50%) translate(${targetX}px,${(horizontalY + targetY) / 2}px)`,
              background: '#374151',
              padding: '2px 6px',
              borderRadius: '4px',
              fontSize: '10px',
              color: '#e5e7eb',
              pointerEvents: 'none',
              zIndex: 10,
              whiteSpace: 'nowrap',
              maxWidth: '120px',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              border: `1px solid ${edgeColor !== '#6b7280' && edgeColor !== '#fff' ? edgeColor : '#4b5563'}`,
            }}
          >
            {label}
          </div>
        </EdgeLabelRenderer>
      )}
    </>
  );
}

export default memo(TreeEdge);
