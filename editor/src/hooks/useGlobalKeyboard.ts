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

            // Helper for path comparison
            const normalizePath = (p: string) => p.replace(/\\/g, '/').toLowerCase();

            // F8 - Toggle Logpoint
            if (e.key === 'F8') {
                console.log('F8 Pressed. ActiveFile:', activeFilePath, 'SelectedNodes:', selectedNodeIds);
                e.preventDefault();
                e.stopPropagation();
                if (activeFilePath && selectedNodeIds.length > 0) {
                    const normActive = normalizePath(activeFilePath);
                    const file = openedFiles.find(f => normalizePath(f.path) === normActive);

                    if (file) {
                        const nodeId = selectedNodeIds[0];
                        const node = file.tree.nodes.get(nodeId);
                        if (node && node.uid !== undefined) {
                            console.log('Toggling Logpoint for:', nodeId, node.uid);
                            toggleLogpoint(activeFilePath, node.uid);
                        } else {
                            console.warn('Node not found or no UID:', nodeId);
                        }
                    } else {
                        console.warn('File not found in openedFiles. Path:', activeFilePath);
                    }
                } else {
                    console.warn('No Active File or No Selection');
                }
                return;
            }

            // F9 - Toggle Breakpoint
            if (e.key === 'F9') {
                console.log('F9 Pressed. ActiveFile:', activeFilePath, 'SelectedNodes:', selectedNodeIds);
                e.preventDefault();
                e.stopPropagation();
                if (activeFilePath && selectedNodeIds.length > 0) {
                    const normActive = normalizePath(activeFilePath);
                    const file = openedFiles.find(f => normalizePath(f.path) === normActive);

                    if (file) {
                        const nodeId = selectedNodeIds[0];
                        const node = file.tree.nodes.get(nodeId);
                        if (node && node.uid !== undefined) {
                            console.log('Toggling Breakpoint for:', nodeId, node.uid);
                            toggleBreakpoint(activeFilePath, node.uid);
                        } else {
                            console.warn('Node not found or no UID:', nodeId);
                        }
                    } else {
                        console.warn('File not found in openedFiles. Path:', activeFilePath);
                    }
                } else {
                    console.warn('No Active File or No Selection');
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
