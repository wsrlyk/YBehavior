import { memo } from 'react';
import { BaseEdge, type EdgeProps, type Edge, useStore } from '@xyflow/react';

export interface TreeEdgeData extends Record<string, unknown> {
  siblingTargetIds?: string[];  // 兄弟边的目标节点 ID 列表
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
  style,
  markerEnd,
  data,
  selected,
}: EdgeProps) {
  const edgeData = data as TreeEdgeData | undefined;
  const siblingTargetIds = edgeData?.siblingTargetIds || [];

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

  // 绘制路径：直接从连接器位置（sourceX, sourceY）出发
  // 垂直下降到水平线，然后水平移动到目标 X，再垂直下降到目标
  const path = `M ${sourceX} ${sourceY} L ${sourceX} ${horizontalY} L ${targetX} ${horizontalY} L ${targetX} ${targetY}`;

  return (
    <BaseEdge
      id={id}
      path={path}
      style={{
        ...style,
        stroke: selected ? '#fff' : '#6b7280',
        strokeWidth: selected ? 3 : 2
      }}
      markerEnd={markerEnd}
    />
  );
}

export default memo(TreeEdge);
