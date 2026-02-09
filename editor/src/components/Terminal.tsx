import { useEffect, useState, useRef } from "react";
import { LogMessage, LOG_CHANNEL_NAME } from "../utils/logger";

interface TerminalProps {
    isDocked: boolean;
    onToggleMode: () => void;
}

export function Terminal({ isDocked, onToggleMode }: TerminalProps) {
    const [logs, setLogs] = useState<LogMessage[]>([]);
    const scrollRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        // Initial log
        if (logs.length === 0) {
            setLogs([{ level: 'info', message: "Terminal initialized...", timestamp: Date.now() }]);
        }

        const channel = new BroadcastChannel(LOG_CHANNEL_NAME);
        channel.onmessage = (event) => {
            const msg = event.data as LogMessage;
            setLogs(prev => [...prev, msg]);
        };

        return () => {
            channel.close();
        };
    }, []);

    useEffect(() => {
        if (scrollRef.current) {
            scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
        }
    }, [logs]);

    const getColor = (level: string) => {
        switch (level) {
            case 'error': return 'text-red-500';
            case 'warn': return 'text-yellow-500';
            case 'success': return 'text-green-500';
            default: return 'text-gray-300';
        }
    };

    return (
        <div className={`flex flex-col w-full bg-black text-gray-300 font-mono text-sm overflow-hidden ${isDocked ? 'h-full border-t border-gray-700' : 'h-screen'}`}>
            {/* Header */}
            <div className="h-8 bg-gray-800 flex items-center px-4 border-b border-gray-700 select-none justify-between" data-tauri-drag-region={!isDocked}>
                <span className="font-bold text-xs uppercase tracking-wider">Terminal</span>
                <button
                    onClick={onToggleMode}
                    className="text-xs px-2 py-1 bg-gray-700 hover:bg-gray-600 rounded text-gray-300 transition-colors"
                >
                    {isDocked ? "Pop Out [↗]" : "Pop In [↙]"}
                </button>
            </div>

            {/* Log Output */}
            <div className="flex-1 overflow-y-auto p-2 space-y-1" ref={scrollRef}>
                {logs.map((log, i) => (
                    <div key={i} className="break-words flex">
                        <span className="text-gray-500 mr-2 text-xs w-24 shrink-0 select-none font-mono">
                            {(() => {
                                const d = new Date(log.timestamp);
                                return `${d.toLocaleTimeString('en-GB', { hour12: false })}.${d.getMilliseconds().toString().padStart(3, '0')}`;
                            })()}
                        </span>
                        {typeof log.message === 'string' ? (
                            <span className={`${getColor(log.level)} whitespace-pre-wrap font-mono`}>
                                {log.message}
                            </span>
                        ) : (
                            <span className="whitespace-pre-wrap font-mono">
                                {log.message.map((seg, j) => (
                                    <span key={j} className={seg.color || getColor(log.level)}>
                                        {seg.text}
                                    </span>
                                ))}
                            </span>
                        )}
                    </div>
                ))}
            </div>

            {/* Input Area */}
            <div className="h-10 border-t border-gray-700 flex items-center px-2 bg-gray-900">
                <span className="text-green-500 mr-2">$</span>
                <input
                    type="text"
                    className="flex-1 bg-transparent border-none outline-none text-white placeholder-gray-600"
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
