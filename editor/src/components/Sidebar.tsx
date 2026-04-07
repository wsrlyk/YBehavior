import { useState, useEffect } from 'react';
import { useEditorStore } from '../stores/editorStore';
import { useFSMStore } from '../stores/fsmStore';
import { useEditorMetaStore } from '../stores/editorMetaStore';
import { useDebugStore } from '../stores/debugStore';
import { useTooltipStore } from '../stores/tooltipStore';
import { NodeState } from '../types/debug';
import { getFileDisplay } from '../utils/fileUtils';
import { DEBUG_COLORS, TRANSIENT_HIGHLIGHT_DURATION } from '../config/constants';

import { getTheme } from '../theme/theme';

const theme = getTheme();

export function Sidebar() {
  const { openedFiles, activeFilePath, setActiveFile, closeFile, isLoading } = useEditorStore();
  const { openedFSMFiles, activeFSMPath, setActiveFSM, closeFSM } = useFSMStore();
  const sidebarWidth = useEditorMetaStore(state => state.uiMeta.sidebarWidth);
  // const { isConnected, isFileRunning, treeRunInfos } = useDebugStore();

  // 没有打开文件时不显示侧边栏
  if (openedFiles.length === 0 && openedFSMFiles.length === 0 && !isLoading) {
    return null;
  }

  const handleFileClick = (path: string, isFSM: boolean) => {
    if (isFSM) {
      setActiveFSM(path);
      // Deactivate tree
      useEditorStore.getState().setActiveFile(null as any);
    } else {
      setActiveFile(path);
      // Deactivate fsm
      useFSMStore.getState().setActiveFSM(null as any);
    }
  };

  const handleFileClose = (path: string, isFSM: boolean) => {
    if (isFSM) {
      closeFSM(path);
    } else {
      closeFile(path);
    }
  };

  // 合并文件列表以便统一排序或显示
  const allFiles = [
    ...openedFiles.map(f => ({ ...f, isFSM: false })),
    ...openedFSMFiles.map(f => ({ ...f, isFSM: true }))
  ];

  return (
    <div
      className="h-full flex flex-col flex-shrink-0"
      style={{
        width: sidebarWidth,
        backgroundColor: theme.ui.panelBg,
        borderRight: `1px solid ${theme.ui.border}`
      }}
    >
      {/* 已打开文件列表 */}
      <div className="flex-1 overflow-auto custom-scrollbar">
        <div className="p-2 text-[10px] uppercase tracking-wider font-semibold" style={{ color: theme.ui.textMain }}>
          Open Files
        </div>
        {isLoading ? (
          <div className="px-2 text-sm" style={{ color: theme.ui.textMain }}>Loading...</div>
        ) : (
          <div className="space-y-0.5">
            {allFiles.map((file) => (
              <SidebarItem
                key={file.path}
                file={file}
                isActive={file.isFSM ? activeFSMPath === file.path : activeFilePath === file.path}
                onClick={() => handleFileClick(file.path, file.isFSM)}
                onClose={() => handleFileClose(file.path, file.isFSM)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function SidebarItem({ file, isActive, onClick, onClose }: any) {
  const { isConnected, getFileRunState, treeRunInfos, keyframe } = useDebugStore();
  const setTooltip = useTooltipStore((state) => state.setTooltip);
  const { icon, name } = getFileDisplay(file.name, file.isFSM);

  // Visual state for transient indicators
  const [visualState, setVisualState] = useState<{ color: string } | null>(null);

  // Compute current data state
  const fileRunState = isConnected ? getFileRunState(name) : undefined;
  const fsmInfo = isConnected && file.isFSM ? useDebugStore.getState().fsmRunInfo : undefined;

  // Resolve root/final state
  let rootFinal: number | undefined;
  if (file.isFSM) {
    if (fsmInfo) {
      const normalize = (s: string) => s.replace(/\\/g, '/').replace(/\.(fsm|tree)$/, '');
      const fName = normalize(file.name);
      const fsmName = normalize(fsmInfo.fsmName);

      // Match if identical or one ends with the other
      const isMatch = fName === fsmName || fName.endsWith('/' + fsmName) || fsmName.endsWith('/' + fName);

      if (isMatch) {
        let hasBreak = false;
        let hasRunning = false;
        let hasFailure = false;
        let hasSuccess = false;

        for (const state of fsmInfo.stateInfos.values()) {
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
    }
  } else {
    // Tree: fuzzy-match treeRunInfos (key may be full relative path, name is basename)
    // then aggregate all node states (same priority as FSM/RunningList)
    const normName = name.replace(/\\/g, '/');
    for (const [key, info] of treeRunInfos) {
      const normKey = key.replace(/\\/g, '/');
      if (normKey === normName || normKey.endsWith('/' + normName) || normName.endsWith('/' + normKey)) {
        let hasBreak = false, hasRunning = false, hasFailure = false, hasSuccess = false;
        for (const ns of info.nodeStates.values()) {
          if (ns.final === NodeState.Break) { hasBreak = true; break; }
          if (ns.final === NodeState.Running) hasRunning = true;
          else if (ns.final === NodeState.Failure) hasFailure = true;
          else if (ns.final === NodeState.Success) hasSuccess = true;
        }
        if (hasBreak) rootFinal = NodeState.Break;
        else if (hasRunning) rootFinal = NodeState.Running;
        else if (hasFailure) rootFinal = NodeState.Failure;
        else if (hasSuccess) rootFinal = NodeState.Success;
        break;
      }
    }
  }

  useEffect(() => {
    if (!isConnected) {
      setVisualState(null);
      return;
    }

    let nextColor: string | null = null;
    let isTransient = false;

    // Priority 1: Break (Persistent) from fileRunState (if matched)
    if (fileRunState === NodeState.Break) {
      nextColor = DEBUG_COLORS.BREAK;
      isTransient = false;
    }
    // Priority 2: Running (Transient) from fileRunState
    else if (fileRunState === NodeState.Running) {
      nextColor = DEBUG_COLORS.RUNNING;
      isTransient = true;
    }
    // Priority 3: Derived Result (Success/Failure/Break/Running from scan)
    else if (rootFinal !== undefined) {
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
  }, [isConnected, fileRunState, rootFinal, keyframe, file.isFSM]);

  return (
    <div
      onClick={onClick}
      className="group relative flex items-center px-1.5 py-1 text-[11px] cursor-pointer"
      style={{
        backgroundColor: isActive ? theme.ui.border : 'transparent',
        color: isActive ? theme.ui.textMain : theme.ui.textMain,
      }}
      onMouseEnter={(e) => {
        if (isActive) return;
        e.currentTarget.style.backgroundColor = theme.ui.panelBg;
        e.currentTarget.style.color = theme.ui.textMain;
        setTooltip(file.name);
      }}
      onMouseLeave={(e) => {
        if (isActive) return;
        e.currentTarget.style.backgroundColor = 'transparent';
        e.currentTarget.style.color = theme.ui.textMain;
        setTooltip(null);
      }}
    >
      {/* Status indicator */}
      {visualState && (
        <span className={`w-2 h-2 rounded-full mr-1 ${visualState.color}`} />
      )}

      <span className="mr-1.5 flex-shrink-0">{icon}</span>
      <span className="flex-1 filename-ellipsis pr-2" style={{ color: isActive ? theme.ui.terminalButtonText : theme.ui.textMain }}>
        {file.isDirty ? '* ' : ''}{name}
      </span>
      <button
        onClick={(e) => {
          e.stopPropagation();
          onClose();
        }}
        className="absolute right-0.5 opacity-0 group-hover:opacity-100 rounded px-0.5 transition-all"
        style={{ color: theme.ui.textDim }}
        onMouseEnter={(e) => {
          e.currentTarget.style.backgroundColor = theme.ui.border;
          e.currentTarget.style.color = theme.ui.textMain;
        }}
        onMouseLeave={(e) => {
          e.currentTarget.style.backgroundColor = 'transparent';
          e.currentTarget.style.color = theme.ui.textDim;
        }}
      >
        ×
      </button>
    </div>
  );
}
