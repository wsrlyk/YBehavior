import { memo, useState } from 'react';
import { BaseEdge, EdgeLabelRenderer, type EdgeProps, type Edge, getBezierPath, useStore } from '@xyflow/react';
import { useTheme } from '../theme/theme';
import { useSelectionPreviewStore } from '../stores/selectionPreviewStore';

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
  source,
  target,
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
  const theme = useTheme();
  const [isHovered, setIsHovered] = useState(false);
  const edgeData = data as DataEdgeData | undefined;
  const [edgePath] = getBezierPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });

  const isConnectedToFlowSelection = useStore((state) =>
    !!state.nodeLookup.get(source)?.selected || !!state.nodeLookup.get(target)?.selected
  );
  const previewSelection = useSelectionPreviewStore((state) =>
    state.active ? state.nodeIds.has(source) || state.nodeIds.has(target) : null
  );
  const isConnectedToSelected = previewSelection ?? isConnectedToFlowSelection;
  const showEndpointGuide = (selected || isHovered) && !edgeData?.isEffectivelyDisabled;

  // Determine stroke color
  let strokeColor = theme.edge.data.default;
  if (selected) {
    strokeColor = theme.edge.data.selected;
  } else if (isConnectedToSelected) {
    strokeColor = theme.edge.data.related;
  }

  const guideColor = selected ? theme.edge.data.selected : theme.edge.data.hover;

  return (
    <>
      <BaseEdge
        id={id}
        path={edgePath}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        style={{
          ...style,
          stroke: isHovered && !selected ? theme.edge.data.hover : strokeColor,
          strokeWidth: selected ? 4.5 : isHovered ? 4 : isConnectedToSelected ? 3.25 : 2,
          strokeDasharray: '5,5',
          opacity: edgeData?.isEffectivelyDisabled ? 0.35 : 1,
        }}
        markerEnd={markerEnd}
      />
      {showEndpointGuide && (
        <EdgeLabelRenderer>
          {[{ x: sourceX, y: sourceY }, { x: targetX, y: targetY }].map((endpoint, index) => (
            <div
              key={index}
              className="absolute pointer-events-none z-[120]"
              style={{ transform: `translate(${endpoint.x}px, ${endpoint.y}px)` }}
            >
              <div
                className="absolute left-0 top-0 w-3 h-3 rounded-full border-2 -translate-x-1/2 -translate-y-1/2"
                style={{ backgroundColor: theme.ui.inputBg, borderColor: guideColor, boxShadow: `0 0 0 2px ${theme.ui.panelBg}` }}
              />
            </div>
          ))}
        </EdgeLabelRenderer>
      )}
    </>
  );
}

export default memo(DataEdge);
