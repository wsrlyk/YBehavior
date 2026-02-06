
// Get file display info (icon and name without extension)
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
