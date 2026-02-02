import { useEffect } from "react";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { Terminal } from "../components/Terminal";

export function TerminalWindow() {

    // Switch to docked mode: Close this window (hide it) and signal main window
    const handlePopIn = async () => {
        const win = getCurrentWindow();
        await win.hide();

        const channel = new BroadcastChannel('terminal-control');
        channel.postMessage({ type: 'dock' });
        channel.close();
    };

    // Listen for close request (X button) -> Switch to docked
    useEffect(() => {
        const win = getCurrentWindow();
        const unlistenPromise = win.onCloseRequested((event) => {
            event.preventDefault(); // Prevent window from closing
            handlePopIn();
        });

        return () => {
            unlistenPromise.then(unlisten => unlisten());
        };
    }, []);

    return (
        <Terminal
            isDocked={false}
            onToggleMode={handlePopIn}
        />
    );
}
