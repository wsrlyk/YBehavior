import type { Node, XYPosition } from '@xyflow/react';

export interface NodeCenter {
    id: string;
    x: number;
    y: number;
}

export function getSelectableNodeCenters(nodes: Node[]): NodeCenter[] {
    const centers: NodeCenter[] = [];
    for (const node of nodes) {
        if (node.hidden || node.selectable === false) continue;

        const width = node.measured?.width ?? node.width ?? node.initialWidth ?? 0;
        const height = node.measured?.height ?? node.height ?? node.initialHeight ?? 0;
        centers.push({
            id: node.id,
            x: node.position.x + width / 2,
            y: node.position.y + height / 2,
        });
    }
    return centers;
}

export function getNodeIdsWithCenterInRect(
    centers: NodeCenter[],
    start: XYPosition,
    end: XYPosition
): Set<string> {
    const left = Math.min(start.x, end.x);
    const right = Math.max(start.x, end.x);
    const top = Math.min(start.y, end.y);
    const bottom = Math.max(start.y, end.y);

    const selectedIds = new Set<string>();
    for (const center of centers) {
        if (center.x >= left && center.x <= right && center.y >= top && center.y <= bottom) {
            selectedIds.add(center.id);
        }
    }
    return selectedIds;
}
