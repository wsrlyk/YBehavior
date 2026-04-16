import { memo } from 'react';
import { BaseEdge, type EdgeProps, type Edge, getBezierPath } from '@xyflow/react';
import { getTheme } from '../theme/theme';

import { useEditorStore } from '../stores/editorStore';
import { useShallow } from 'zustand/react/shallow';

const theme = getTheme();

export interface DataEdgeData extends Record<string, unknown> {
  fromNodeId: string;
  toNodeId: string;
  fromPinName: string;
  toPinName: string;
  isEffectivelyDisabled?: boolean;
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
  data,
}: EdgeProps) {
  const edgeData = data as DataEdgeData | undefined;
  const [edgePath] = getBezierPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });

  const isConnectedToSelected = useEditorStore(useShallow((s) =>
    (edgeData?.fromNodeId && s.selectedNodeIds.includes(edgeData.fromNodeId)) ||
    (edgeData?.toNodeId && s.selectedNodeIds.includes(edgeData.toNodeId))
  ));

  // Determine stroke color
  let strokeColor = theme.edge.data.default;
  if (selected) {
    strokeColor = theme.edge.data.selected;
  } else if (isConnectedToSelected) {
    strokeColor = theme.edge.data.selected + '80'; // Dim highlight
  }

  return (
    <BaseEdge
      id={id}
      path={edgePath}
      style={{
        ...style,
        stroke: strokeColor,
        strokeWidth: (selected || isConnectedToSelected) ? 3 : 2,
        strokeDasharray: '5,5',  // 虚线
        opacity: edgeData?.isEffectivelyDisabled ? 0.35 : 1,
      }}
      markerEnd={markerEnd}
    />
  );
}

export default memo(DataEdge);
