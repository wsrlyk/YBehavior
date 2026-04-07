import { useState, useEffect } from 'react';
import { useDebugStore } from '../stores/debugStore';
import { useEditorStore } from '../stores/editorStore';
import { useFSMStore } from '../stores/fsmStore';
import { useTooltipStore } from '../stores/tooltipStore';
import { getFileDisplay } from '../utils/fileUtils';
import { NodeState } from '../types/debug';
import { DEBUG_COLORS, TRANSIENT_HIGHLIGHT_DURATION } from '../config/constants';
import { getTheme } from '../theme/theme';

export const RunningList = () => {
    const theme = getTheme();
    const { treeRunInfos, fsmRunInfo, isConnected } = useDebugStore();
    const { treeFiles, openTree, setActiveFile } = useEditorStore();
    const { openFSMFile, setActiveFSM } = useFSMStore();
    const [isOpen, setIsOpen] = useState(true);

    if (!isConnected || (!fsmRunInfo && treeRunInfos.size === 0)) return null;

    const treeNames = Array.from(treeRunInfos.keys()).filter(k => k && k.trim().length > 0);
    const totalCount = (fsmRunInfo ? 1 : 0) + treeNames.length;
    if (totalCount === 0) return null;

    const handleOpen = (fileName: string) => {
        const normalize = (path: string) => path.replace(/\\/g, '/');
        const fNorm = normalize(fileName);

        console.log('Trying to open (normalized):', fNorm);

        let match = treeFiles.find(p => {
            const pNorm = normalize(p);

            // 1. Exact match (normalized)
            if (pNorm === fNorm) return true;

            // 2. Path match (ends with /fileName)
            if (pNorm.endsWith('/' + fNorm) || pNorm === fNorm) return true;

            // 3. Extension match
            const withTree = fNorm + '.tree';
            if (pNorm.endsWith('/' + withTree) || pNorm === withTree) return true;

            const withFsm = fNorm + '.fsm';
            if (pNorm.endsWith('/' + withFsm) || pNorm === withFsm) return true;

            // 4. Basename loose match (ignore extensions)
            const pBasename = pNorm.split('/').pop();
            const fBasename = fNorm.split('/').pop();
            if (pBasename === fBasename) return true;

            const pNameNoExt = pBasename?.replace(/\.(tree|fsm)$/, '');
            if (pNameNoExt === fBasename) return true;

            return false;
        });

        if (match) {
            console.log('Found match:', match);
            const isFSM = match.endsWith('.fsm');
            if (isFSM) {
                openFSMFile(match);
                setActiveFile(null as any);
            } else {
                openTree(match);
                setActiveFSM(null as any);
            }
        } else {
            console.warn(`Could not find file path for ${fileName}. Available files:`, treeFiles);
        }
    };

    return (
        <div className="absolute top-2 left-2 z-50 flex flex-col items-start font-sans">
            <div
                className="border rounded shadow-lg backdrop-blur-sm overflow-hidden"
                style={{ backgroundColor: `${theme.ui.panelBg}E6`, borderColor: theme.ui.border }}
            >
                <div
                    className="px-3 py-1.5 flex items-center gap-2 cursor-pointer transition-colors select-none"
                    style={{ color: theme.ui.textMain }}
                    onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = `${theme.ui.accentSoft}CC`; }}
                    onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
                    onClick={() => setIsOpen(!isOpen)}
                >
                    <div className="w-2 h-2 rounded-full bg-pink-500 animate-pulse shadow-[0_0_8px_rgba(236,72,153,0.6)]" />
                    <span className="text-xs font-semibold" style={{ color: theme.ui.textMain }}>
                        Debug List ({totalCount})
                    </span>
                    <span className="text-[10px] ml-1" style={{ color: theme.ui.textDim }}>
                        {isOpen ? '▼' : '▶'}
                    </span>
                </div>

                {isOpen && (
                    <div className="max-h-[min(80vh,calc(100vh-120px))] overflow-y-auto min-w-[160px] max-w-[240px] border-t" style={{ borderColor: theme.ui.border }}>
                        {/* FSM entry first */}
                        {fsmRunInfo && (
                            <RunningListItem
                                key={fsmRunInfo.fsmName}
                                fileName={fsmRunInfo.fsmName}
                                isFsm={true}
                                onClick={() => handleOpen(fsmRunInfo.fsmName)}
                            />
                        )}
                        {/* Tree entries */}
                        {treeNames.map(fileName => (
                            <RunningListItem
                                key={fileName}
                                fileName={fileName}
                                isFsm={false}
                                onClick={() => handleOpen(fileName)}
                            />
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
};

function RunningListItem({ fileName, isFsm, onClick }: { fileName: string; isFsm: boolean; onClick: () => void }) {
    const theme = getTheme();
    const { isConnected, getFileRunState, treeRunInfos, fsmRunInfo, keyframe } = useDebugStore();
    const setTooltip = useTooltipStore((state) => state.setTooltip);

    // Icon determined by the caller (FSM from fsmRunInfo, Tree from treeRunInfos)
    const { icon, name: displayName } = getFileDisplay(fileName, isFsm, !isFsm);

    const [visualState, setVisualState] = useState<{ color: string } | null>(null);

    const fileRunState = isConnected ? getFileRunState(fileName) : undefined;

    let rootFinal: number | undefined;

    if (isFsm) {
        // FSM: derive rootFinal from fsmRunInfo.stateInfos
        if (isConnected && fsmRunInfo) {
            let hasBreak = false, hasRunning = false, hasFailure = false, hasSuccess = false;
            for (const state of fsmRunInfo.stateInfos.values()) {
                if (state === NodeState.Break) { hasBreak = true; break; }
                if (state === NodeState.Running) hasRunning = true;
                if (state === NodeState.Failure) hasFailure = true;
                if (state === NodeState.Success) hasSuccess = true;
            }
            if (hasBreak) rootFinal = NodeState.Break;
            else if (hasRunning) rootFinal = NodeState.Running;
            else if (hasFailure) rootFinal = NodeState.Failure;
            else if (hasSuccess) rootFinal = NodeState.Success;
        }
    } else {
        // Tree: aggregate state from all node states (same priority as FSM: Break > Running > Failure > Success)
        const treeInfo = isConnected ? treeRunInfos.get(fileName) : undefined;
        if (treeInfo) {
            let hasBreak = false, hasRunning = false, hasFailure = false, hasSuccess = false;
            for (const ns of treeInfo.nodeStates.values()) {
                if (ns.final === NodeState.Break) { hasBreak = true; break; }
                if (ns.final === NodeState.Running) hasRunning = true;
                else if (ns.final === NodeState.Failure) hasFailure = true;
                else if (ns.final === NodeState.Success) hasSuccess = true;
            }
            if (hasBreak) rootFinal = NodeState.Break;
            else if (hasRunning) rootFinal = NodeState.Running;
            else if (hasFailure) rootFinal = NodeState.Failure;
            else if (hasSuccess) rootFinal = NodeState.Success;
        }
    }

    useEffect(() => {
        if (!isConnected) {
            setVisualState(null);
            return;
        }

        let nextColor: string | null = null;
        let isTransient = false;

        if (fileRunState === NodeState.Break) {
            nextColor = DEBUG_COLORS.BREAK;
            isTransient = false;
        } else if (fileRunState === NodeState.Running) {
            nextColor = DEBUG_COLORS.RUNNING;
            isTransient = true;
        } else if (rootFinal !== undefined) {
            if (rootFinal === NodeState.Success) { nextColor = DEBUG_COLORS.SUCCESS; isTransient = true; }
            else if (rootFinal === NodeState.Failure) { nextColor = DEBUG_COLORS.FAILURE; isTransient = true; }
            else if (rootFinal === NodeState.Break) { nextColor = DEBUG_COLORS.BREAK; isTransient = false; }
            else if (rootFinal === NodeState.Running) { nextColor = DEBUG_COLORS.RUNNING; isTransient = true; }
        }

        if (nextColor) {
            setVisualState({ color: nextColor });
            if (isTransient) {
                const timer = setTimeout(() => {
                    setVisualState(null);
                }, TRANSIENT_HIGHLIGHT_DURATION);
                return () => clearTimeout(timer);
            }
        } else {
            setVisualState(null);
        }
    }, [isConnected, fileRunState, rootFinal, keyframe]);

    const activeColor = visualState?.color || theme.ui.textDim;

    return (
        <div
            className="px-3 py-1.5 text-xs cursor-pointer flex items-center gap-2 border-l-2 border-transparent transition-all"
            style={{ color: theme.ui.textMain }}
            onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = theme.ui.accentSoft;
                e.currentTarget.style.borderLeftColor = theme.ui.accent;
            }}
            onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = 'transparent';
                e.currentTarget.style.borderLeftColor = 'transparent';
            }}
            onClick={onClick}
            onMouseOver={() => setTooltip(fileName)}
            onMouseOut={() => setTooltip(null)}
        >
            {/* Status Dot */}
            <span className="w-1.5 h-1.5 rounded-full" style={{ backgroundColor: activeColor }} />

            <span className="opacity-70">{icon}</span>
            <span className="flex-1 filename-ellipsis">{displayName}</span>
        </div>
    );
}
