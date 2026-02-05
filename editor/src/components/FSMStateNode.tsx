import { memo, useState } from 'react';
import { Handle, Position, type NodeProps, type Node } from '@xyflow/react';
import type { FSMState, FSMStateType } from '../types/fsm';
import { stateTypeHasParentConnector, stateTypeHasChildrenConnector, stateTypeHasTreeSelector } from '../types/fsm';
import { useFSMStore } from '../stores/fsmStore';
import { decodeXmlEntities } from '../utils/stringUtils';

// ==================== Colors ====================

const STATE_COLORS: Record<FSMStateType, { bg: string; border: string }> = {
    Normal: { bg: '#4A5568', border: '#718096' },
    Meta: { bg: '#553C9A', border: '#805AD5' },
    Entry: { bg: '#276749', border: '#48BB78' },
    Exit: { bg: '#9B2C2C', border: '#FC8181' },
    Any: { bg: '#744210', border: '#D69E2E' },
    Upper: { bg: '#2C5282', border: '#63B3ED' },
};

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
    const isGlobalConnecting = useFSMStore(state => state.isConnecting);

    const colors = STATE_COLORS[state.type];
    const icon = STATE_ICONS[state.type];
    const hasParentHandle = stateTypeHasParentConnector(state.type);
    const hasChildHandle = stateTypeHasChildrenConnector(state.type);
    const hasTree = stateTypeHasTreeSelector(state.type);

    // Visibility logic
    // Always render handles so React Flow can find them, but hide via CSS
    const isTargetVisible = hasParentHandle && isGlobalConnecting;
    const isSourceVisible = hasChildHandle && isHovered && !isGlobalConnecting;

    return (
        <div
            className={`
        relative rounded-lg shadow-lg min-w-[120px] transition-all duration-200
        ${selected ? 'ring-2 ring-blue-400 ring-offset-2 ring-offset-gray-900' : ''}
        ${isDefault ? 'ring-2 ring-yellow-400' : ''}
        ${isHovered ? 'scale-[1.02]' : ''}
      `}
            style={{
                backgroundColor: colors.bg,
                borderWidth: 2,
                borderColor: colors.border,
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
                        !rounded-full !bg-white/40 !border-4 !border-white/20 hover:!bg-white/60 !transition-all z-50 !cursor-crosshair shadow-lg
                        ${isSourceVisible ? '!opacity-100 !pointer-events-auto' : '!opacity-0 !pointer-events-none'}
                    `}
                    style={{
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        // Small visual nub, but anchor is center
                        width: '32px',
                        height: '32px',
                    }}
                />
            )}


            {/* Header */}
            <div className="flex items-center gap-2 px-3 py-2 border-b border-gray-600/50">
                <span className="text-white/70">{icon}</span>
                <span className="text-white font-medium text-sm">
                    {state.name || state.type}
                </span>
                {isDefault && (
                    <span className="ml-auto text-yellow-400 text-xs font-bold">DEFAULT</span>
                )}
            </div>

            {/* Tree reference */}
            {hasTree && state.tree && (
                <div className="px-3 py-1 text-xs text-gray-300 bg-gray-800/50">
                    🌲 {state.tree}
                </div>
            )}

            {/* Meta state indicator */}
            {state.type === 'Meta' && (
                <div className="px-3 py-1 text-xs text-purple-300 bg-purple-900/30 cursor-pointer hover:bg-purple-900/50">
                    ◇ Double-click to enter
                </div>
            )}

            {/* Comment */}
            {state.comment && (
                <div className="px-3 py-1 text-[10px] text-gray-400 italic border-t border-gray-600/30 whitespace-pre-wrap">
                    {decodeXmlEntities(state.comment)}
                </div>
            )}

        </div>
    );
}


export default memo(FSMStateNode);
