import { useCallback, useEffect, useRef, type Dispatch, type MouseEvent, type SetStateAction } from 'react';
import type { Node, XYPosition } from '@xyflow/react';
import { getNodeIdsWithCenterInRect, getSelectableNodeCenters, type NodeCenter } from '../utils/selection';
import { useSelectionPreviewStore } from '../stores/selectionPreviewStore';

interface SelectionSession {
    start: XYPosition;
    centers: NodeCenter[];
    latestPointer: XYPosition;
}

interface CenterNodeSelectionOptions<NodeType extends Node> {
    getNodes: () => Node[];
    screenToFlowPosition: (position: XYPosition) => XYPosition;
    setNodes: Dispatch<SetStateAction<NodeType[]>>;
    onSelectionComplete?: (selectedIds: string[]) => void;
}

export function useCenterNodeSelection<NodeType extends Node>({
    getNodes,
    screenToFlowPosition,
    setNodes,
    onSelectionComplete,
}: CenterNodeSelectionOptions<NodeType>) {
    const isSelectingRef = useRef(false);
    const sessionRef = useRef<SelectionSession | null>(null);
    const animationFrameRef = useRef<number | null>(null);
    const releaseFrameRef = useRef<number | null>(null);

    const applySelection = useCallback((pointer: XYPosition): Set<string> | null => {
        const session = sessionRef.current;
        if (!session) return null;

        const end = screenToFlowPosition(pointer);
        const selectedIds = getNodeIdsWithCenterInRect(session.centers, session.start, end);
        useSelectionPreviewStore.getState().update(selectedIds);
        setNodes(currentNodes => {
            let changed = false;
            const nextNodes = currentNodes.map(node => {
                const selected = selectedIds.has(node.id);
                if (!!node.selected === selected) return node;
                changed = true;
                return { ...node, selected };
            });
            return changed ? nextNodes : currentNodes;
        });
        return selectedIds;
    }, [screenToFlowPosition, setNodes]);

    useEffect(() => {
        const handleMouseMove = (event: globalThis.MouseEvent) => {
            const session = sessionRef.current;
            if (!session) return;

            session.latestPointer = { x: event.clientX, y: event.clientY };
            if (animationFrameRef.current !== null) return;

            animationFrameRef.current = requestAnimationFrame(() => {
                animationFrameRef.current = null;
                const currentSession = sessionRef.current;
                if (currentSession) applySelection(currentSession.latestPointer);
            });
        };

        window.addEventListener('mousemove', handleMouseMove);
        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            if (animationFrameRef.current !== null) cancelAnimationFrame(animationFrameRef.current);
            if (releaseFrameRef.current !== null) cancelAnimationFrame(releaseFrameRef.current);
            if (sessionRef.current) useSelectionPreviewStore.getState().end();
        };
    }, [applySelection]);

    const onSelectionStart = useCallback((event: MouseEvent) => {
        const pointer = { x: event.clientX, y: event.clientY };
        if (releaseFrameRef.current !== null) {
            cancelAnimationFrame(releaseFrameRef.current);
            releaseFrameRef.current = null;
        }
        isSelectingRef.current = true;
        useSelectionPreviewStore.getState().begin();
        sessionRef.current = {
            start: screenToFlowPosition(pointer),
            centers: getSelectableNodeCenters(getNodes()),
            latestPointer: pointer,
        };
        applySelection(pointer);
    }, [applySelection, getNodes, screenToFlowPosition]);

    const onSelectionEnd = useCallback((event: MouseEvent) => {
        if (animationFrameRef.current !== null) {
            cancelAnimationFrame(animationFrameRef.current);
            animationFrameRef.current = null;
        }
        const selectedIds = applySelection({ x: event.clientX, y: event.clientY });
        sessionRef.current = null;
        if (selectedIds) onSelectionComplete?.([...selectedIds]);
        useSelectionPreviewStore.getState().end();

        // React Flow emits its native selection changes after onSelectionEnd.
        // Keep filtering them through this frame so they cannot overwrite the
        // center-point result that was just committed to the editor store.
        releaseFrameRef.current = requestAnimationFrame(() => {
            releaseFrameRef.current = null;
            isSelectingRef.current = false;
        });
    }, [applySelection, onSelectionComplete]);

    return { isSelectingRef, onSelectionStart, onSelectionEnd };
}
