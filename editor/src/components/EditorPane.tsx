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

interface EditorPaneProps {
    onPaneClick?: () => void;
}

export function EditorPane({ onPaneClick }: EditorPaneProps) {
    const { openedFiles, activeFilePath } = useEditorStore();
    const { openedFSMFiles, activeFSMPath } = useFSMStore();

    const activeTreeFile = openedFiles.find(f => f.path === activeFilePath);
    const activeFSMFile = openedFSMFiles.find(f => f.path === activeFSMPath);

    // Determine which editor to show based on active file
    const isFSMActive = activeFSMPath !== null && activeFSMFile !== undefined;
    const isTreeActive = activeFilePath !== null && activeTreeFile !== undefined;

    if (isFSMActive) {
        return <FSMEditor />;
    }

    if (isTreeActive) {
        return <NodeEditor onPaneClick={onPaneClick} />;
    }

    // No file open
    return (
        <div className="h-full w-full flex items-center justify-center text-gray-500 bg-gray-900">
            <div className="text-center">
                <p className="text-lg">No file open</p>
                <p className="text-sm mt-2">Open a file from the sidebar or create a new one</p>
            </div>
        </div>
    );
}
