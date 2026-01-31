import { useEditorStore } from '../stores/editorStore';

// 获取文件图标和不带后缀的名称
function getFileDisplay(filename: string): { icon: string; name: string } {
  if (filename.endsWith('.tree')) {
    return { icon: '🌴', name: filename.replace(/\.tree$/, '') };
  } else if (filename.endsWith('.fsm')) {
    return { icon: '🔄', name: filename.replace(/\.fsm$/, '') };
  }
  return { icon: '📄', name: filename };
}

export function Sidebar() {
  const { openedFiles, activeFilePath, setActiveFile, closeFile, isLoading } = useEditorStore();
  
  // 没有打开文件时不显示侧边栏
  if (openedFiles.length === 0 && !isLoading) {
    return null;
  }
  
  return (
    <div className="w-40 h-full bg-gray-900 border-r border-gray-700 flex flex-col">
      {/* 已打开文件列表 */}
      <div className="flex-1 overflow-auto">
        <div className="p-2 text-xs text-gray-500 uppercase tracking-wider">
          Open Files
        </div>
        {isLoading ? (
          <div className="px-2 text-sm text-gray-500">Loading...</div>
        ) : (
          <div className="space-y-0.5">
            {openedFiles.map((file) => {
              const { icon, name } = getFileDisplay(file.name);
              return (
                <div
                  key={file.path}
                  onClick={() => setActiveFile(file.path)}
                  className={`group flex items-center px-2 py-1.5 text-sm cursor-pointer ${
                    activeFilePath === file.path
                      ? 'bg-gray-700 text-white'
                      : 'text-gray-300 hover:bg-gray-800'
                  }`}
                >
                  <span className="mr-1.5">{icon}</span>
                  <span className="flex-1 truncate">
                    {file.isDirty ? '* ' : ''}{name}
                  </span>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      closeFile(file.path);
                    }}
                    className="opacity-0 group-hover:opacity-100 ml-1 px-1 text-gray-500 hover:text-white"
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
