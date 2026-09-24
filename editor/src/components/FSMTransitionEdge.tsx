import { BaseEdge, EdgeLabelRenderer, getStraightPath, useInternalNode, type EdgeProps, type Edge } from '@xyflow/react';
import { useState } from 'react';
import { useTheme } from '../theme/theme';
import { useSelectionPreviewStore } from '../stores/selectionPreviewStore';

export type FSMTransitionEdgeData = {
    transitions?: any[];
};

export type FSMTransitionEdgeType = Edge<FSMTransitionEdgeData, 'fsmTransition'>;

export default function FSMTransitionEdge({
    source,
    target,
    style = {},
    markerEnd: _,
    data,
    label,
    selected,
}: EdgeProps<FSMTransitionEdgeType>) {
    const theme = useTheme();
    const [isHovered, setIsHovered] = useState(false);
    const sourceNode = useInternalNode(source);
    const targetNode = useInternalNode(target);

    if (!sourceNode || !targetNode) return null;
    const previewSelection = useSelectionPreviewStore((state) =>
        state.active ? state.nodeIds.has(source) || state.nodeIds.has(target) : null
    );
    const isConnectedToSelected = previewSelection ?? (!!sourceNode.selected || !!targetNode.selected);

    // Calculate true centers from node dimensions and position
    const sx_center = sourceNode.internals.positionAbsolute.x + (sourceNode.measured.width ?? 0) / 2;
    const sy_center = sourceNode.internals.positionAbsolute.y + (sourceNode.measured.height ?? 0) / 2;
    const tx_center = targetNode.internals.positionAbsolute.x + (targetNode.measured.width ?? 0) / 2;
    const ty_center = targetNode.internals.positionAbsolute.y + (targetNode.measured.height ?? 0) / 2;

    // Calculate basic geometry from centers
    const dx = tx_center - sx_center;
    const dy = ty_center - sy_center;
    const distance = Math.sqrt(dx * dx + dy * dy);

    if (distance === 0) return null;

    // Unit vector
    const nx = dx / distance;
    const ny = dy / distance;

    // Normal vector for parallel offset
    const px = -ny;
    const py = nx;

    // Parallel offset for bi-directional visibility
    const offset = 8;

    // Calculate start/end points (node centers + offset)
    const sx = sx_center + px * offset;
    const sy = sy_center + py * offset;
    const tx = tx_center + px * offset;
    const ty = ty_center + py * offset;

    const [edgePath, labelX, labelY] = getStraightPath({
        sourceX: sx,
        sourceY: sy,
        targetX: tx,
        targetY: ty,
    });

    // Arrow geometry (centered)
    const midX = (sx + tx) / 2;
    const midY = (sy + ty) / 2;
    const angle = Math.atan2(dy, dx) * (180 / Math.PI);

    const transitionCount = data?.transitions?.length || 1;
    const showCount = transitionCount > 1;
    const arrowSize = showCount ? 14 : 10;

    let color = theme.edge.fsmTransition.default;
    if (selected) {
        color = theme.edge.fsmTransition.selected;
    } else if (isHovered) {
        color = theme.edge.fsmTransition.hover;
    } else if (isConnectedToSelected) {
        color = theme.edge.fsmTransition.related;
    }

    const textX = -arrowSize * 0.9;

    return (
        <>
            <BaseEdge
                path={edgePath}
                onMouseEnter={() => setIsHovered(true)}
                onMouseLeave={() => setIsHovered(false)}
                style={{
                    ...style,
                    strokeWidth: selected ? 4.5 : isHovered ? 4 : isConnectedToSelected ? 3.25 : 2,
                    stroke: color,
                }}
            />

            {/* Centered Arrow with Optional Count */}
            <g transform={`translate(${midX},${midY}) rotate(${angle})`}>
                <polygon
                    points={`0,0 -${arrowSize * 1.6},-${arrowSize} -${arrowSize * 1.6},${arrowSize}`}
                    fill={color}
                    stroke={selected ? theme.ui.textMain : 'none'}
                    strokeWidth={1}
                />
                {showCount && (
                    <text
                        x={textX}
                        y={0}
                        transform={`rotate(${-angle}, ${textX}, 0)`}
                        fill={theme.ui.textMain}
                        fontSize="11px"
                        fontWeight="bold"
                        textAnchor="middle"
                        dominantBaseline="central"
                        style={{ pointerEvents: 'none' }}
                    >
                        {transitionCount}
                    </text>
                )}
            </g>

            {label && !showCount && (
                <EdgeLabelRenderer>
                    <div
                        style={{
                            position: 'absolute',
                            transform: `translate(-50%, -50%) translate(${labelX}px,${labelY}px)`,
                            background: theme.edge.label.bg,
                            padding: '2px 4px',
                            borderRadius: '4px',
                            fontSize: '10px',
                            color: theme.edge.label.text,
                            fontWeight: 'bold',
                            border: `1px solid ${theme.edge.label.border}`,
                            pointerEvents: 'all',
                            marginTop: '15px',
                        }}
                        className="nodrag nopan"
                    >
                        {label}
                    </div>
                </EdgeLabelRenderer>
            )}
        </>
    );
}
