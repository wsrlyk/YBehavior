
// Strip .tree or .fsm extension
export function stripExtension(filename: string): string {
    return filename.replace(/\.(tree|fsm)$/i, '');
}

// Get just the filename from a full path
export function getBaseName(filepath: string): string {
    const parts = filepath.split(/[/\\]/);
    return parts[parts.length - 1] || filepath;
}


// Get file display info (icon and name without extension and only base name for UI)
// isFsmHint / isTreeHint: used when filepath has no extension (e.g. runtime debug names)
export function getFileDisplay(filepath: string, isFsmHint?: boolean, isTreeHint?: boolean): { icon: string; name: string } {
    const isTree = filepath.endsWith('.tree') || isTreeHint;
    const isFsm = isFsmHint || filepath.endsWith('.fsm');

    const baseName = getBaseName(filepath);

    if (isFsm) {
        return { icon: '🔄', name: stripExtension(baseName) };
    } else if (isTree) {
        return { icon: '🌴', name: stripExtension(baseName) };
    }
    return { icon: '📄', name: baseName };
}
