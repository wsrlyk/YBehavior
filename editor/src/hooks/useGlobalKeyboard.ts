/**
 * Global Keyboard Hook
 * 
 * Handles:
 * - Debug shortcuts (F5 continue, F10 step over, F11 step into, F8 logpoint, F9 breakpoint)
 * - Blocking browser shortcuts (F5 refresh, Ctrl+R, etc.)
 * - Keeps F12 for dev tools
 */

import { useEffect } from 'react';
import { useDebugStore } from '../stores/debugStore';
import { useEditorStore } from '../stores/editorStore';

export function useGlobalKeyboard() {
    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            const { isConnected, isPaused, continueDebug, stepOver, stepInto, toggleBreakpoint, toggleLogpoint } = useDebugStore.getState();
            const { activeFilePath, selectedNodeIds, openedFiles } = useEditorStore.getState();

            // ==================== Debug Shortcuts ====================

            // F5 - Continue (and block browser refresh)
            if (e.key === 'F5') {
                e.preventDefault();
                e.stopPropagation();
                if (isConnected && isPaused) {
                    continueDebug();
                }
                return;
            }

            // F10 - Step Over
            if (e.key === 'F10') {
                e.preventDefault();
                e.stopPropagation();
                if (isConnected && isPaused) {
                    stepOver();
                }
                return;
            }

            // F11 - Step Into
            if (e.key === 'F11') {
                e.preventDefault();
                e.stopPropagation();
                if (isConnected && isPaused) {
                    stepInto();
                }
                return;
            }

            // F8 - Toggle Logpoint
            if (e.key === 'F8') {
                e.preventDefault();
                e.stopPropagation();
                if (activeFilePath && selectedNodeIds.length > 0) {
                    const file = openedFiles.find(f => f.path === activeFilePath);
                    if (file) {
                        const nodeId = selectedNodeIds[0];
                        const node = file.tree.nodes.get(nodeId);
                        if (node && node.uid !== undefined) {
                            toggleLogpoint(activeFilePath, node.uid);
                        }
                    }
                }
                return;
            }

            // F9 - Toggle Breakpoint
            if (e.key === 'F9') {
                e.preventDefault();
                e.stopPropagation();
                if (activeFilePath && selectedNodeIds.length > 0) {
                    const file = openedFiles.find(f => f.path === activeFilePath);
                    if (file) {
                        const nodeId = selectedNodeIds[0];
                        const node = file.tree.nodes.get(nodeId);
                        if (node && node.uid !== undefined) {
                            toggleBreakpoint(activeFilePath, node.uid);
                        }
                    }
                }
                return;
            }

            // ==================== Block Browser Shortcuts ====================

            // Ctrl+R / Ctrl+Shift+R - Block refresh
            if (e.ctrlKey && (e.key === 'r' || e.key === 'R')) {
                e.preventDefault();
                e.stopPropagation();
                return;
            }

            // Ctrl+F5 - Block hard refresh
            if (e.ctrlKey && e.key === 'F5') {
                e.preventDefault();
                e.stopPropagation();
                return;
            }

            // Ctrl+W - Block close tab (let editor handle file close)
            if (e.ctrlKey && (e.key === 'w' || e.key === 'W')) {
                e.preventDefault();
                e.stopPropagation();
                // TODO: Trigger close current file tab
                return;
            }

            // Ctrl+T - Block new tab
            if (e.ctrlKey && (e.key === 't' || e.key === 'T')) {
                e.preventDefault();
                e.stopPropagation();
                return;
            }

            // Ctrl+N - Block new window (let editor handle new file)
            if (e.ctrlKey && (e.key === 'n' || e.key === 'N')) {
                e.preventDefault();
                e.stopPropagation();
                // TODO: Trigger new file dialog
                return;
            }

            // F12 - Keep for dev tools (don't block)
        };

        // Use capture phase to intercept before anything else
        window.addEventListener('keydown', handler, { capture: true });

        return () => {
            window.removeEventListener('keydown', handler, { capture: true });
        };
    }, []);
}
