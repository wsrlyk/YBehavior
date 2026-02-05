import { useEditorStore } from '../stores/editorStore';
import { useFSMStore } from '../stores/fsmStore';
import { useEditorMetaStore } from '../stores/editorMetaStore';

// 获取文件图标和不带后缀的名称
export function getFileDisplay(filename: string, isFsmHint?: boolean): { icon: string; name: string } {
  const isTree = filename.endsWith('.tree');
  const isFsm = isFsmHint || filename.endsWith('.fsm');

  if (isTree) {
    return { icon: '🌴', name: filename.replace(/\.tree$/, '') };
  } else if (isFsm) {
    return { icon: '🔄', name: filename.replace(/\.fsm$/, '') };
  }
  return { icon: '📄', name: filename };
}

export function Sidebar() {
  const { openedFiles, activeFilePath, setActiveFile, closeFile, isLoading } = useEditorStore();
  const { openedFSMFiles, activeFSMPath, setActiveFSM, closeFSM } = useFSMStore();
  const sidebarWidth = useEditorMetaStore(state => state.uiMeta.sidebarWidth);

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
      className="h-full bg-gray-900 border-r border-gray-700 flex flex-col flex-shrink-0"
      style={{ width: sidebarWidth }}
    >
      {/* 已打开文件列表 */}
      <div className="flex-1 overflow-auto custom-scrollbar">
        <div className="p-2 text-[10px] text-gray-500 uppercase tracking-wider font-semibold">
          Open Files
        </div>
        {isLoading ? (
          <div className="px-2 text-sm text-gray-500">Loading...</div>
        ) : (
          <div className="space-y-0.5">
            {allFiles.map((file) => {
              const { icon, name } = getFileDisplay(file.name, file.isFSM);
              const isActive = file.isFSM ? activeFSMPath === file.path : activeFilePath === file.path;
              return (
                <div
                  key={file.path}
                  onClick={() => handleFileClick(file.path, file.isFSM)}
                  className={`group relative flex items-center px-1.5 py-1 text-[11px] cursor-pointer ${isActive
                    ? 'bg-gray-700 text-white'
                    : 'text-gray-400 hover:bg-gray-800 hover:text-gray-200'
                    }`}
                  title={file.name}
                >
                  <span className="mr-1.5 flex-shrink-0">{icon}</span>
                  <span className="flex-1 truncate pr-2">
                    {file.isDirty ? '* ' : ''}{name}
                  </span>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleFileClose(file.path, file.isFSM);
                    }}
                    className="absolute right-0.5 opacity-0 group-hover:opacity-100 hover:bg-gray-600 rounded px-0.5 text-gray-400 hover:text-white transition-all"
                    title="Close"
                  >
                    ×
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
