import { useEffect, useState, useRef } from "react";
import { LogMessage, LOG_CHANNEL_NAME } from "../utils/logger";
import { getTheme } from "../theme/theme";

interface TerminalProps {
    isDocked: boolean;
    onToggleMode: () => void;
}

export function Terminal({ isDocked, onToggleMode }: TerminalProps) {
    const theme = getTheme();
    const logBufferRef = useRef<LogMessage[]>([]);
    const [logs, setLogs] = useState<LogMessage[]>([]);
    const scrollRef = useRef<HTMLDivElement>(null);
    const MAX_LOGS = 1000;

    useEffect(() => {
        // Initial log
        if (logs.length === 0) {
            setLogs([{ level: 'info', message: "Terminal initialized...", timestamp: Date.now() }]);
        }

        const channel = new BroadcastChannel(LOG_CHANNEL_NAME);
        
        let updateTimer: ReturnType<typeof setTimeout> | null = null;
        
        const processBuffer = () => {
            if (logBufferRef.current.length === 0) return;
            
            const newLogs = [...logBufferRef.current];
            logBufferRef.current = [];
            
            setLogs(prev => {
                const combined = [...prev, ...newLogs];
                if (combined.length > MAX_LOGS) {
                    return combined.slice(combined.length - MAX_LOGS);
                }
                return combined;
            });
            updateTimer = null;
        };

        channel.onmessage = (event) => {
            const msg = event.data as LogMessage;
            logBufferRef.current.push(msg);
            
            if (!updateTimer) {
                // Batch updates every 50ms for performance, or immediate-ish feel
                updateTimer = setTimeout(processBuffer, 50);
            }
        };

        return () => {
            channel.close();
            if (updateTimer) clearTimeout(updateTimer);
        };
    }, []);

    useEffect(() => {
        if (scrollRef.current) {
            scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
        }
    }, [logs]);

    const getColor = (level: string) => {
        switch (level) {
            case 'error': return theme.ui.terminalLogError;
            case 'warn': return theme.ui.terminalLogWarn;
            case 'success': return theme.ui.terminalLogSuccess;
            default: return theme.ui.terminalLogInfo;
        }
    };

    const getSegmentColor = (token: string | undefined, fallback: string) => {
        if (!token) return fallback;
        if (token.startsWith('#')) return token;
        switch (token) {
            case 'log-error': return theme.ui.terminalLogError;
            case 'log-warn': return theme.ui.terminalLogWarn;
            case 'log-success': return theme.debug.success.border;
            case 'log-failure': return theme.debug.failure.border;
            case 'log-running': return theme.debug.running.border;
            case 'log-break': return theme.debug.break.border;
            case 'log-accent': return theme.ui.terminalLogOrange;
            case 'log-bright': return theme.ui.terminalButtonText;
            case 'log-dim': return theme.ui.terminalLogDim;
            default:
                return fallback;
        }
    };

    return (
        <div
            className={`flex flex-col w-full font-mono text-sm overflow-hidden ${isDocked ? 'h-full border-t' : 'h-screen'}`}
            style={{ backgroundColor: theme.ui.terminalBg, color: theme.ui.terminalText, borderColor: theme.ui.terminalBorder }}
        >
            {/* Header */}
            <div className="h-8 flex items-center px-4 border-b select-none justify-between" style={{ backgroundColor: theme.ui.terminalHeaderBg, borderColor: theme.ui.terminalBorder }} data-tauri-drag-region={!isDocked}>
                <span className="font-bold text-xs uppercase tracking-wider">Terminal</span>
                <button
                    onClick={onToggleMode}
                    className="text-xs px-2 py-1 rounded transition-colors"
                    style={{ backgroundColor: theme.ui.terminalButtonBg, color: theme.ui.terminalButtonText }}
                >
                    {isDocked ? "Pop Out [↗]" : "Pop In [↙]"}
                </button>
            </div>

            {/* Log Output */}
            <div className="flex-1 overflow-y-auto p-2 space-y-1" ref={scrollRef}>
                {logs.map((log, i) => (
                    <div key={i} className="break-words flex">
                        <span className="mr-2 text-xs w-24 shrink-0 select-none font-mono" style={{ color: theme.ui.terminalTimestamp }}>
                            {(() => {
                                const d = new Date(log.timestamp);
                                return `${d.toLocaleTimeString('en-GB', { hour12: false })}.${d.getMilliseconds().toString().padStart(3, '0')}`;
                            })()}
                        </span>
                        {typeof log.message === 'string' ? (
                            <span className="whitespace-pre-wrap font-mono" style={{ color: getColor(log.level) }}>
                                {log.message}
                            </span>
                        ) : (
                            <span className="whitespace-pre-wrap font-mono">
                                {log.message.map((seg, j) => (
                                    <span key={j} style={{ color: getSegmentColor(seg.color, getColor(log.level)) }}>
                                        {seg.text}
                                    </span>
                                ))}
                            </span>
                        )}
                    </div>
                ))}
            </div>

            {/* Input Area */}
            <div className="h-10 border-t flex items-center px-2" style={{ backgroundColor: theme.ui.terminalInputBg, borderColor: theme.ui.terminalBorder }}>
                <span className="mr-2" style={{ color: theme.ui.terminalLogSuccess }}>$</span>
                <input
                    type="text"
                    className="flex-1 bg-transparent border-none outline-none"
                    style={{ color: theme.ui.terminalText }}
                    placeholder="Enter command (e.g. help, clear)..."
                    onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                            const cmd = e.currentTarget.value.trim();
                            if (!cmd) return;

                            // Echo command
                            setLogs(prev => [...prev, { level: 'info', message: `$ ${cmd}`, timestamp: Date.now() }]);

                            // Handle command
                            if (cmd === 'clear') {
                                setLogs([]);
                            } else if (cmd === 'help') {
                                const helpMsg = "Available commands: clear, echo [msg], help";
                                setLogs(prev => [...prev, { level: 'success', message: helpMsg, timestamp: Date.now() }]);
                            } else if (cmd.startsWith('echo ')) {
                                setLogs(prev => [...prev, { level: 'info', message: cmd.slice(5), timestamp: Date.now() }]);
                            } else {
                                setLogs(prev => [...prev, { level: 'warn', message: `Command not found: ${cmd}`, timestamp: Date.now() }]);
                            }

                            e.currentTarget.value = '';
                        }
                    }}
                />
            </div>
        </div>
    );
}
