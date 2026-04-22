/**
 * Editor Store Core - Shared Types and Helper Functions
 * 
 * This module contains the shared state, types, and helper functions
 * that are used by both Tree and FSM editors.
 */

import type { Tree, TreeConnection } from '../types';
import { serializeTreeForEditor } from '../utils/xmlSerializer';
import { useNodeDefinitionStore } from './nodeDefinitionStore';

// ==================== Shared Types ====================

export interface Viewport {
    x: number;
    y: number;
    zoom: number;
}

export interface HistoryState {
    past: Tree[];
    future: Tree[];
}

export interface OpenedFile {
    path: string;
    name: string;
    tree: Tree;
    lastSavedTreeSnapshot: string;
    isDirty: boolean;
    isNew?: boolean;
    history: HistoryState;
    viewport?: Viewport;
}

// ==================== Shared Helper Functions ====================

/**
 * Get all descendant IDs of a node (used for recursive selection/deletion)
 */
export function getDescendantIds(tree: Tree, nodeId: string): string[] {
    const descendantIds: string[] = [];
    const stack = [nodeId];
    const visited = new Set<string>();

    while (stack.length > 0) {
        const currentId = stack.pop()!;
        if (visited.has(currentId)) continue;
        visited.add(currentId);

        const children = tree.connections
            .filter(c => c.parentNodeId === currentId)
            .map(c => c.childNodeId);

        for (const childId of children) {
            if (!visited.has(childId)) {
                descendantIds.push(childId);
                stack.push(childId);
            }
        }
    }
    return descendantIds;
}

/**
 * Recalculate UIDs for all nodes (depth-first pre-order traversal)
 * IMPORTANT: This function clones all affected nodes to ensure Zustand detects the state change.
 * Returns a new nodes Map with updated UIDs.
 */
export function recalculateUIDs(tree: Tree): void {
    const nodeDefStore = useNodeDefinitionStore.getState();
    let uid = 1;
    const visited = new Set<string>();

    const getConnectorOrder = (nodeType: string): string[] => {
        const nodeDef = nodeDefStore.getDefinition(nodeType);
        const order = ['condition'];

        if (nodeDef?.childConnectors) {
            for (const c of nodeDef.childConnectors) {
                if (c.name !== 'condition' && !order.includes(c.name)) {
                    order.push(c.name);
                }
            }
        }

        if (!order.includes('children')) order.push('children');
        if (!order.includes('default')) order.push('default');
        return order;
    };

    const sortChildConns = (nodeId: string, nodeType: string): TreeConnection[] => {
        const childConns = connectionsByParent.get(nodeId) || [];
        const order = getConnectorOrder(nodeType);

        return [...childConns].sort((a, b) => {
            const connA = a.parentConnector || 'children';
            const connB = b.parentConnector || 'children';

            if (connA !== connB) {
                let idxA = order.indexOf(connA);
                let idxB = order.indexOf(connB);
                if (idxA === -1) idxA = 999;
                if (idxB === -1) idxB = 999;
                if (idxA !== idxB) return idxA - idxB;
            }

            const nodeA = tree.nodes.get(a.childNodeId);
            const nodeB = tree.nodes.get(b.childNodeId);
            return (nodeA?.position.x || 0) - (nodeB?.position.x || 0);
        });
    };

    // Build parent->children index
    const connectionsByParent = new Map<string, TreeConnection[]>();
    for (const conn of tree.connections) {
        const list = connectionsByParent.get(conn.parentNodeId) || [];
        list.push(conn);
        connectionsByParent.set(conn.parentNodeId, list);
    }

    function dfs(nodeId: string, isAncestorDisabled: boolean) {
        if (visited.has(nodeId)) return;
        visited.add(nodeId);

        const node = tree.nodes.get(nodeId);
        if (!node) return;

        const effectivelyDisabled = isAncestorDisabled || node.disabled;

        // 1. Assign UID to current node (pre-order)
        node.uid = effectivelyDisabled ? undefined : uid++;

        // 2. Process children by connector order; within same connector by x
        const sortedConns = sortChildConns(nodeId, node.type);
        for (const conn of sortedConns) {
            dfs(conn.childNodeId, effectivelyDisabled);
        }
    }

    // Reset all UIDs
    tree.nodes.forEach(node => { node.uid = undefined; });

    // Process main tree from root
    if (tree.rootId) {
        dfs(tree.rootId, false);
    }

    // Process forest (orphan trees)
    let forestIndex = 1;
    const childIds = new Set(tree.connections.map(c => c.childNodeId));

    for (const nodeId of tree.nodes.keys()) {
        if (!childIds.has(nodeId) && !visited.has(nodeId)) {
            uid = forestIndex * 1000 + 1;
            dfs(nodeId, false);
            forestIndex++;
        }
    }
}

/**
 * Update the tree of an opened file (auto-recalculates UID and handles history)
 * Returns null if no changes were made.
 */
export function updateOpenedFileTree(
    openedFiles: OpenedFile[],
    activeFilePath: string | null,
    updater: (tree: Tree) => Tree,
    options: { skipHistory?: boolean; isUIChange?: boolean; skipUID?: boolean } = {}
): { openedFiles: OpenedFile[] } | null {
    if (!activeFilePath) return null;

    const fileIndex = openedFiles.findIndex(f => f.path === activeFilePath);
    if (fileIndex === -1) return null;

    const file = openedFiles[fileIndex];
    const oldTree = file.tree;
    let newTree = updater(oldTree);

    // No changes
    if (newTree === oldTree) return null;

    // Recalculate UIDs and create a new nodes Map to trigger React re-render
    if (!options.skipUID) {
        recalculateUIDs(newTree);
        // Create a new Map to ensure Zustand detects the change
        const newNodes = new Map<string, any>();
        newTree.nodes.forEach((node, id) => {
            newNodes.set(id, { ...node });
        });
        newTree = { ...newTree, nodes: newNodes };
    }

    // Calculate dirty state
    let isDirty = file.isDirty;
    if (options.isUIChange) {
        isDirty = file.isDirty;
    } else if (!options.skipHistory) {
        const currentSnapshot = serializeTreeForEditor(newTree);
        isDirty = currentSnapshot !== file.lastSavedTreeSnapshot;
    } else {
        isDirty = true;
    }

    const newFile: OpenedFile = {
        ...file,
        tree: newTree,
        isDirty
    };

    // Handle history
    if (!options.skipHistory) {
        newFile.history = {
            past: [oldTree, ...file.history.past].slice(0, 50),
            future: []
        };
    }

    const newOpenedFiles = [...openedFiles];
    newOpenedFiles[fileIndex] = newFile;

    return { openedFiles: newOpenedFiles };
}
