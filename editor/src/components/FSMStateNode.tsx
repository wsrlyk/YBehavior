import { memo, useState, useEffect } from 'react';
import { DEBUG_RINGS, TRANSIENT_HIGHLIGHT_DURATION } from '../config/constants';
import { NodeState } from '../types/debug';
import { Handle, Position, type NodeProps, type Node } from '@xyflow/react';
import type { FSMState, FSMStateType } from '../types/fsm';
import { stateTypeHasParentConnector, stateTypeHasChildrenConnector, stateTypeHasTreeSelector } from '../types/fsm';
import { useFSMStore } from '../stores/fsmStore';
import { useEditorStore } from '../stores/editorStore';
import { useTooltipStore } from '../stores/tooltipStore';
// NodeState imported from types/debug
import { useDebugStore } from '../stores/debugStore';
import { decodeXmlEntities } from '../utils/stringUtils';
import { stripExtension } from '../utils/fileUtils';
import { getTheme } from '../theme/theme';

const theme = getTheme();

// ==================== Colors ====================

const STATE_COLORS: Record<FSMStateType, { bg: string; border: string }> = theme.fsmState as any;

const STATE_ICONS: Record<FSMStateType, string> = {
    Normal: '●',
    Meta: '◆',
    Entry: '▶',
    Exit: '■',
    Any: '★',
    Upper: '▲',
};

// ==================== Types ====================

export type FSMStateNodeData = {
    state: FSMState;
    isDefault?: boolean;
    isCurrentMachine?: boolean;
};

export type FSMStateNodeType = Node<FSMStateNodeData, 'fsmState'>;

// ==================== Component ====================

function FSMStateNode({ data, selected }: NodeProps<FSMStateNodeType>) {
    const { state, isDefault } = data;
    const [isHovered, setIsHovered] = useState(false);
    const setTooltip = useTooltipStore((state) => state.setTooltip);
    const isGlobalConnecting = useFSMStore(state => state.isConnecting);

    // Debug info
    const { fsmRunInfo, isConnected, keyframe } = useDebugStore();
    const runState = (isConnected && state.uid !== undefined) ? fsmRunInfo?.stateInfos?.get(state.uid) : undefined;
    const isRunning = runState !== undefined;

    const [isTransientVisible, setIsTransientVisible] = useState(false);

    let ringClass = '';
    if (isRunning) {
        if (runState === NodeState.Break) ringClass = `ring-4 ${DEBUG_RINGS.BREAK} ring-offset-2 ring-offset-gray-900`;
        else if (runState === NodeState.Success) ringClass = `ring-4 ${DEBUG_RINGS.SUCCESS} ring-offset-2 ring-offset-gray-900`;
        else if (runState === NodeState.Failure) ringClass = `ring-4 ${DEBUG_RINGS.FAILURE} ring-offset-2 ring-offset-gray-900`;
        else ringClass = `ring-4 ${DEBUG_RINGS.RUNNING} ring-offset-2 ring-offset-gray-900`; // Running or Default
    }

    // Handle transient visibility (0.7s flash for non-break states)
    useEffect(() => {
        if (runState === NodeState.Break) {
            setIsTransientVisible(true);
        } else if (isRunning) {
            setIsTransientVisible(true);
            const timer = setTimeout(() => {
                setIsTransientVisible(false);
            }, TRANSIENT_HIGHLIGHT_DURATION);
            return () => clearTimeout(timer);
        } else {
            setIsTransientVisible(false);
        }
    }, [runState, isRunning, keyframe]);

    // Hide ring if transient time expired (and not Break)
    if (isRunning && !isTransientVisible && runState !== NodeState.Break) {
        ringClass = '';
    }

    const colors = STATE_COLORS[state.type];
    const styleColors = colors;

    const icon = STATE_ICONS[state.type];
    const hasParentHandle = stateTypeHasParentConnector(state.type);
    const hasChildHandle = stateTypeHasChildrenConnector(state.type);
    const hasTree = stateTypeHasTreeSelector(state.type);

    // Visibility logic
    // Always render handles so React Flow can find them, but hide via CSS
    const isTargetVisible = hasParentHandle && isGlobalConnecting;
    const isSourceVisible = hasChildHandle && isHovered && !isGlobalConnecting;
    const highlightBorder = selected && !isRunning ? `0 0 0 2px ${theme.text.variable}` : undefined;
    const defaultBorder = isDefault && !isRunning ? `0 0 0 2px ${theme.returnType.Invert}` : undefined;
    const combinedOuterGlow = [highlightBorder, defaultBorder].filter(Boolean).join(', ');

    return (
        <div
            className={`
        relative rounded-lg shadow-lg min-w-[120px] transition-all duration-200
        ${ringClass}
        ${isHovered ? 'scale-[1.02]' : ''}
      `}
            style={{
                backgroundColor: styleColors.bg,
                borderWidth: 2,
                borderColor: styleColors.border,
                boxShadow: combinedOuterGlow || undefined,
            }}
            onMouseEnter={() => setIsHovered(true)}
            onMouseLeave={() => setIsHovered(false)}
        >
            {/* Target Handle (parent) - Centered point with large hit area via padding */}
            {hasParentHandle && (
                <Handle
                    type="target"
                    position={Position.Top}
                    id="parent"
                    className="!opacity-0 !w-1 !h-1 !border-0"
                    style={{
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        // Large padding expands the hit area while keeping the anchor point at the center
                        padding: isTargetVisible ? '30px 60px' : '0',
                        pointerEvents: isTargetVisible ? 'all' : 'none',
                    }}
                />
            )}

            {/* Source Handle (child) - Centered point with hit area */}
            {hasChildHandle && (
                <Handle
                    type="source"
                    position={Position.Bottom}
                    id="child"
                    className={`
                        !rounded-full !border-4 !transition-all z-50 !cursor-crosshair shadow-lg
                        ${isSourceVisible ? '!opacity-100 !pointer-events-auto' : '!opacity-0 !pointer-events-none'}
                    `}
                    style={{
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        backgroundColor: theme.ui.border,
                        borderColor: theme.ui.panelBg,
                        // Small visual nub, but anchor is center
                        width: '32px',
                        height: '32px',
                    }}
                />
            )}


            {/* Header */}
            <div className="flex items-center gap-2 px-3 py-2 border-b" style={{ borderColor: theme.ui.border, color: theme.ui.textMain }}>
                <span style={{ color: theme.ui.textDim }}>{icon}</span>
                <div className="font-bold text-center truncate px-1" style={{ color: theme.ui.textMain }}>
                    {state.uid !== undefined && <span className="mr-1" style={{ color: theme.ui.textDim }}>[{state.uid}]</span>}
                    {state.type === 'Normal' || state.type === 'Meta' ? state.name : state.type}
                </div>
                {isDefault && (
                    <span className="ml-auto text-xs font-bold" style={{ color: theme.returnType.Invert }}>DEFAULT</span>
                )}
            </div>

            {/* Tree reference */}
            {hasTree && state.tree && (
                <div
                    className="flex items-center justify-between px-3 py-1 text-xs filename-ellipsis max-w-[200px] border-t group/tree"
                    style={{
                        color: theme.ui.textMain,
                        backgroundColor: theme.ui.background,
                        borderColor: theme.ui.border,
                    }}
                    onMouseEnter={() => setTooltip(state.tree || null)}
                    onMouseLeave={() => setTooltip(null)}
                >
                    <span className="truncate">🌲 {stripExtension(state.tree)}</span>
                    <button
                        className="opacity-0 group-hover/tree:opacity-100 transition-opacity ml-1"
                        style={{ color: theme.ui.textDim }}
                        onMouseEnter={(e) => { e.currentTarget.style.color = theme.text.variable; }}
                        onMouseLeave={(e) => { e.currentTarget.style.color = theme.ui.textDim; }}
                        onClick={(e) => {
                            e.stopPropagation();
                            useEditorStore.getState().openTree(state.tree!);
                        }}
                    >
                        ↗
                    </button>
                </div>
            )}

            {/* Meta state indicator */}
            {state.type === 'Meta' && (
                <div
                    className="px-3 py-1 text-xs cursor-pointer"
                    style={{
                        color: theme.ui.textMain,
                        backgroundColor: theme.ui.border,
                    }}
                >
                    ◇ Double-click to enter
                </div>
            )}

            {/* Comment */}
            {state.comment && (
                <div
                    className="px-3 py-1 text-[10px] italic border-t whitespace-pre-wrap"
                    style={{
                        backgroundColor: theme.comment.bg,
                        borderColor: theme.comment.border,
                        color: theme.comment.text,
                    }}
                >
                    {decodeXmlEntities(state.comment)}
                </div>
            )}

        </div>
    );
}


export default memo(FSMStateNode);
