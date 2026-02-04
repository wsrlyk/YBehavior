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
 * Recalculate UIDs for all nodes (depth-first traversal)
 * NOTE: This function MUTATES tree.nodes directly.
 * Caller must ensure tree.nodes is a fresh clone.
 */
export function recalculateUIDs(tree: Tree): void {
    const nodeDefStore = useNodeDefinitionStore.getState();
    let uid = 1;
    const visited = new Set<string>();

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
        const childConns = connectionsByParent.get(nodeId) || [];

        // 1. Process condition connector first
        const conditionConn = childConns.find(c => c.parentConnector === 'condition');
        if (conditionConn) {
            dfs(conditionConn.childNodeId, effectivelyDisabled);
        }

        // 2. Assign UID to current node
        node.uid = effectivelyDisabled ? undefined : uid++;

        // 3. Process other children by connector order
        const nodeDef = nodeDefStore.getDefinition(node.type);
        const connectors = nodeDef?.childConnectors || [];

        for (const connectorDef of connectors) {
            const connsForThisType = childConns.filter(c => c.parentConnector === connectorDef.name);
            const sortedConns = [...connsForThisType].sort((a, b) => {
                const nodeA = tree.nodes.get(a.childNodeId);
                const nodeB = tree.nodes.get(b.childNodeId);
                return (nodeA?.position.x || 0) - (nodeB?.position.x || 0);
            });

            for (const conn of sortedConns) {
                dfs(conn.childNodeId, effectivelyDisabled);
            }
        }

        // Handle connectors not in definition
        const knownConnectorNames = new Set(connectors.map(c => c.name));
        knownConnectorNames.add('condition');

        const extraConns = childConns.filter(c => !knownConnectorNames.has(c.parentConnector));
        const sortedExtraConns = [...extraConns].sort((a, b) => {
            const nodeA = tree.nodes.get(a.childNodeId);
            const nodeB = tree.nodes.get(b.childNodeId);
            return (nodeA?.position.x || 0) - (nodeB?.position.x || 0);
        });

        for (const conn of sortedExtraConns) {
            dfs(conn.childNodeId, effectivelyDisabled);
        }
    }

    // Reset all UIDs
    tree.nodes.forEach(node => { node.uid = undefined; });

    // Process main tree from root
    if (tree.rootId) dfs(tree.rootId, false);

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
    const newTree = updater(oldTree);

    // No changes
    if (newTree === oldTree) return null;

    // Recalculate UIDs
    if (!options.skipUID) {
        recalculateUIDs(newTree);
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
