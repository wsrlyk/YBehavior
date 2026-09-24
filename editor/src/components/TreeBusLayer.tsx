import { ViewportPortal, useStore } from '@xyflow/react';
import { useEditorStore } from '../stores/editorStore';
import { useNodeDefinitionStore } from '../stores/nodeDefinitionStore';
import { useTheme } from '../theme/theme';

interface BusPath {
  id: string;
  d: string;
  disabled: boolean;
}

export function TreeBusLayer() {
  const theme = useTheme();
  const tree = useEditorStore((state) => state.getCurrentTree());
  const getDefinition = useNodeDefinitionStore((state) => state.getDefinition);

  const paths = useStore((state) => {
    if (!tree) return [];

    const groups = new Map<string, typeof tree.connections>();
    for (const connection of tree.connections) {
      const parent = tree.nodes.get(connection.parentNodeId);
      const child = tree.nodes.get(connection.childNodeId);
      if (!parent || !child || parent.isFolded) continue;
      const connector = connection.parentConnector || 'default';
      const key = `${connection.parentNodeId}:${connector}`;
      const group = groups.get(key) || [];
      group.push(connection);
      groups.set(key, group);
    }

    const [viewportX, viewportY, zoom] = state.transform;
    const left = (-viewportX - 100) / zoom;
    const top = (-viewportY - 100) / zoom;
    const right = (state.width - viewportX + 100) / zoom;
    const bottom = (state.height - viewportY + 100) / zoom;
    const result: BusPath[] = [];

    for (const [key, connections] of groups) {
      const first = connections[0];
      const sourceNode = state.nodeLookup.get(first.parentNodeId);
      if (!sourceNode) continue;

      const parent = tree.nodes.get(first.parentNodeId);
      const definition = parent ? getDefinition(parent.type) : undefined;
      const connector = first.parentConnector || 'default';
      const connectors = definition?.childConnectors || [];
      const connectorIndex = Math.max(0, connectors.findIndex((item) => item.name === connector));
      const sourceWidth = sourceNode.measured.width ?? 0;
      const sourceHeight = sourceNode.measured.height ?? 60;
      const sourceX = connectors.length > 1
        ? sourceNode.internals.positionAbsolute.x + sourceWidth * ((connectorIndex + 1) / (connectors.length + 1))
        : sourceNode.internals.positionAbsolute.x + sourceWidth / 2;
      const sourceY = connector === 'condition'
        ? sourceNode.internals.positionAbsolute.y + 20
        : sourceNode.internals.positionAbsolute.y + sourceHeight;

      let minChildY = Infinity;
      let minX = sourceX;
      let maxX = sourceX;
      let disabled = !!parent?.disabled;
      for (const connection of connections) {
        const childNode = state.nodeLookup.get(connection.childNodeId);
        if (!childNode) continue;
        const childX = childNode.internals.positionAbsolute.x + (childNode.measured.width ?? 0) / 2;
        minX = Math.min(minX, childX);
        maxX = Math.max(maxX, childX);
        minChildY = Math.min(minChildY, childNode.internals.positionAbsolute.y);
        disabled ||= !!tree.nodes.get(connection.childNodeId)?.disabled;
      }
      if (minChildY === Infinity) continue;

      const offset = connector === 'condition' ? 0 : connectorIndex * 20;
      const horizontalY = connector === 'condition'
        ? sourceY + 15
        : Math.max(sourceY + 10 + offset, (sourceY + minChildY) / 2 + offset);

      const intersectsViewport = maxX >= left && minX <= right &&
        Math.max(sourceY, horizontalY) >= top && Math.min(sourceY, horizontalY) <= bottom;
      if (!intersectsViewport) continue;

      result.push({
        id: key,
        d: `M ${sourceX} ${sourceY} L ${sourceX} ${horizontalY} M ${minX} ${horizontalY} L ${maxX} ${horizontalY}`,
        disabled,
      });
    }

    return result;
  });

  return (
    <ViewportPortal>
      <svg className="absolute inset-0 overflow-visible pointer-events-none" style={{ zIndex: -1 }}>
        {paths.map((path) => (
          <path
            key={path.id}
            d={path.d}
            fill="none"
            stroke={theme.edge.tree.default}
            strokeWidth={2}
            opacity={path.disabled ? 0.35 : 1}
          />
        ))}
      </svg>
    </ViewportPortal>
  );
}
