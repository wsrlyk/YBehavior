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
}: EdgeProps) {
  const edgeData = data as TreeEdgeData | undefined;
  const siblingTargetIds = edgeData?.siblingTargetIds || [];
  
  // 从 store 获取所有共享值，确保所有兄弟边计算出相同的结果
  const { horizontalY, verticalX, startY } = useStore((state) => {
    // 1. 获取父节点的位置和尺寸
    const sourceNode = source ? state.nodeLookup.get(source) : null;
    if (!sourceNode) {
      return { horizontalY: sourceY + 30, verticalX: sourceX, startY: sourceY };
    }
    
    const width = sourceNode.measured?.width ?? 128;
    const height = sourceNode.measured?.height ?? 60;
    const vx = sourceNode.position.x + width / 2;
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
    const hY = Math.max(
      parentBottomY + 10,
      (parentBottomY + minChildY) / 2
    );
    
    return { horizontalY: hY, verticalX: vx, startY: parentBottomY };
  });
  
  // 绘制路径：从源点出发，到共享的垂直线和水平线位置
  const path = `M ${sourceX} ${sourceY} L ${verticalX} ${startY} L ${verticalX} ${horizontalY} L ${targetX} ${horizontalY} L ${targetX} ${targetY}`;
  
  return (
    <BaseEdge
      id={id}
      path={path}
      style={{ ...style, stroke: '#6b7280', strokeWidth: 2 }}
      markerEnd={markerEnd}
    />
  );
}

export default memo(TreeEdge);
