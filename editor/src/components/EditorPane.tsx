/**
 * Editor Pane Component
 * 
 * Unified editor pane that renders either NodeEditor (for .tree files)
 * or FSMEditor (for .fsm files) based on the active file type.
 */

import { useEditorStore } from '../stores/editorStore';
import { useFSMStore } from '../stores/fsmStore';
import { NodeEditor } from './NodeEditor';
import FSMEditor from './FSMEditor';
import { getTheme } from '../theme/theme';

import { RunningList } from './RunningList';

interface EditorPaneProps {
    onPaneClick?: () => void;
}

export function EditorPane({ onPaneClick }: EditorPaneProps) {
    const theme = getTheme();
    const { openedFiles, activeFilePath } = useEditorStore();
    const { openedFSMFiles, activeFSMPath } = useFSMStore();

    const activeTreeFile = openedFiles.find(f => f.path === activeFilePath);
    const activeFSMFile = openedFSMFiles.find(f => f.path === activeFSMPath);

    // Determine which editor to show based on active file
    const isFSMActive = activeFSMPath !== null && activeFSMFile !== undefined;
    const isTreeActive = activeFilePath !== null && activeTreeFile !== undefined;

    const renderContent = () => {
        if (isFSMActive) {
            return <FSMEditor key={activeFSMPath} onPaneClick={onPaneClick} />;
        }

        if (isTreeActive) {
            return <NodeEditor key={activeFilePath} onPaneClick={onPaneClick} />;
        }

        // No file open
        return (
            <div className="h-full w-full flex items-center justify-center" style={{ color: theme.ui.textDim, backgroundColor: theme.ui.background }}>
                <div className="text-center">
                    <p className="text-lg">No file open</p>
                    <p className="text-sm mt-2">Open a file from the sidebar or create a new one</p>
                </div>
            </div>
        );
    };

    return (
        <div className="relative h-full w-full">
            {renderContent()}
            <RunningList />
        </div>
    );
}
