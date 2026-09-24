import { memo, useState } from 'react';
import { BaseEdge, type EdgeProps, type Edge, useStore, EdgeLabelRenderer } from '@xyflow/react';
import { useEditorStore } from '../stores/editorStore';
import { useDebugStore } from '../stores/debugStore';
import { useShallow } from 'zustand/react/shallow';
import { NodeState } from '../types/debug';
import { useTheme } from '../theme/theme';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import type { TreeNode } from '../types';
import { useSelectionPreviewStore } from '../stores/selectionPreviewStore';

export interface TreeEdgeData extends Record<string, unknown> {
  siblingTargetIds?: string[];  // 兄弟边的目标节点 ID 列表
  label?: string;               // 连线标签
  isEffectivelyDisabled?: boolean;
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
  sourceHandleId,
  style,
  markerEnd,
  data,
  selected,
}: EdgeProps) {
  const theme = useTheme();
  const [isHovered, setIsHovered] = useState(false);
  const edgeData = data as TreeEdgeData | undefined;
  const siblingTargetIds = edgeData?.siblingTargetIds || [];
  const label = edgeData?.label;

  // Import definition store
  const { getDefinition } = useNodeDefinitionStore();

  // Calculate the shared bus once per connector group. Individual edges only
  // draw their own vertical branch, avoiding stacked strokes on common paths.
  const busGeometry = useStore((state) => {
    // 1. 获取父节点的位置和尺寸
    const sourceNode = source ? state.nodeLookup.get(source) : null;
    if (!sourceNode) {
      return sourceY + 30;
    }

    // Determine handle index for vertical offset
    // Determine handle index for vertical offset
    let handleIndex = 0;
    const treeNode = sourceNode.data.treeNode as TreeNode | undefined;
    const nodeDef = getDefinition(treeNode?.type || '');

    if (sourceHandleId === 'condition') {
      handleIndex = -1;
    } else if (nodeDef && nodeDef.childConnectors && nodeDef.childConnectors.length > 0) {
      if (sourceHandleId) {
        const index = nodeDef.childConnectors.findIndex(c => c.name === sourceHandleId);
        if (index !== -1) {
          handleIndex = index;
        }
      }
    }

    // Base vertical offset per handle index (e.g., 20px per index)
    const baseOffset = (handleIndex * 20);

    const height = sourceNode.measured?.height ?? 60;
    const parentBottomY = sourceNode.position.y + height;

    // 2. 遍历所有兄弟节点，找到最小的 Y（最高的节点）
    let minChildY = Infinity;
    for (const targetId of siblingTargetIds) {
      const node = state.nodeLookup.get(targetId);
      if (node) {
        minChildY = Math.min(minChildY, node.internals.positionAbsolute.y);
      }
    }
    if (minChildY === Infinity) {
      minChildY = parentBottomY + 100;
    }

    // 3. 计算水平线高度
    // Add baseOffset to the calculation to separate lines
    // If it's a condition handle (at the top), the horizontal line should be near the sourceY
    if (sourceHandleId === 'condition') {
      return sourceY + 15;
    }

    return Math.max(
      parentBottomY + 10 + baseOffset,
      ((parentBottomY + minChildY) / 2) + baseOffset
    );
  });
  const horizontalY = busGeometry;

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

  // Read endpoint selection from React Flow so marquee feedback updates immediately.
  const isConnectedToFlowSelection = useStore((state) =>
    !!state.nodeLookup.get(source)?.selected || !!state.nodeLookup.get(target)?.selected
  );
  const previewSelection = useSelectionPreviewStore((state) =>
    state.active ? state.nodeIds.has(source) || state.nodeIds.has(target) : null
  );
  const isConnectedToSelected = previewSelection ?? isConnectedToFlowSelection;

  const getEdgeColor = (state: NodeState) => {
    if (edgeData?.isEffectivelyDisabled) {
      return theme.edge.tree.default;
    }
    switch (state) {
      case NodeState.Success: return theme.debug.success.edge;
      case NodeState.Failure: return theme.debug.failure.edge;
      case NodeState.Running: return theme.debug.running.edge;
      case NodeState.Break: return theme.debug.break.edge;
      default:
        if (selected) return theme.edge.tree.selected;
        if (isConnectedToSelected) return theme.edge.tree.selected;
        return theme.edge.tree.default;
    }
  };

  const edgeColor = (debugState !== NodeState.Invalid) ? getEdgeColor(debugState) : getEdgeColor(NodeState.Invalid);
  const isHighlighted = !edgeData?.isEffectivelyDisabled && (selected || isHovered || isConnectedToSelected || debugState !== NodeState.Invalid);
  const highlightColor = debugState !== NodeState.Invalid
    ? edgeColor
    : selected
      ? theme.edge.tree.selected
      : isHovered
        ? theme.edge.tree.hover
        : theme.edge.tree.related;
  const highlightOpacity = debugState !== NodeState.Invalid && isPaused ? 0.75 : 1;

  const branchPath = `M ${targetX} ${horizontalY} L ${targetX} ${targetY}`;
  const highlightedRoute = `M ${sourceX} ${sourceY} L ${sourceX} ${horizontalY} L ${targetX} ${horizontalY} L ${targetX} ${targetY}`;

  return (
    <>
      {/* Keep the complete route interactive while the shared bus is rendered once in TreeBusLayer. */}
      <path
        d={highlightedRoute}
        fill="none"
        stroke="transparent"
        strokeWidth={18}
        pointerEvents="stroke"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      />
      <BaseEdge
        id={`${id}-base`}
        path={branchPath}
        interactionWidth={0}
        style={{
          ...style,
          stroke: theme.edge.tree.default,
          strokeWidth: 2,
          opacity: edgeData?.isEffectivelyDisabled ? 0.35 : 1,
          pointerEvents: 'none',
        }}
        markerEnd={markerEnd}
      />
      {isHighlighted && (
        <BaseEdge
          id={id}
          path={highlightedRoute}
          interactionWidth={0}
          style={{
            ...style,
            stroke: highlightColor,
            strokeWidth: selected ? 4.5 : isHovered ? 4 : 3.25,
            strokeOpacity: highlightOpacity,
            transition: 'stroke 0.15s, stroke-width 0.15s',
            pointerEvents: 'none',
          }}
          markerEnd={markerEnd}
        />
      )}
      {label && (
        <EdgeLabelRenderer>
          <div
            style={{
              position: 'absolute',
              transform: `translate(-50%, -50%) translate(${targetX}px,${(horizontalY + targetY) / 2}px)`,
              background: theme.edge.label.bg,
              padding: '2px 6px',
              borderRadius: '4px',
              fontSize: '10px',
              color: theme.edge.label.text,
              pointerEvents: 'none',
              zIndex: 10,
              whiteSpace: 'nowrap',
              maxWidth: '120px',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              border: `1px solid ${isHighlighted
                ? highlightColor
                : theme.edge.label.border}`,
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
