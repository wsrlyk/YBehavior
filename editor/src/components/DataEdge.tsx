import { memo } from 'react';
import { BaseEdge, type EdgeProps, type Edge, getBezierPath } from '@xyflow/react';

export interface DataEdgeData extends Record<string, unknown> {
  fromPinName: string;
  toPinName: string;
}

export type DataEdgeType = Edge<DataEdgeData, 'data'>;

/**
 * 数据连接的连线
 * 使用贝塞尔曲线，颜色为蓝色以区分树连线
 */
function DataEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  style,
  markerEnd,
  selected,
}: EdgeProps) {
  const [edgePath] = getBezierPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });

  return (
    <BaseEdge
      id={id}
      path={edgePath}
      style={{
        ...style,
        stroke: selected ? '#fff' : '#60a5fa',  // 选中变白，否则蓝色
        strokeWidth: selected ? 3 : 2,
        strokeDasharray: '5,5',  // 虚线
      }}
      markerEnd={markerEnd}
    />
  );
}

export default memo(DataEdge);
