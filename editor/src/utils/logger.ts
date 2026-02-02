export type LogLevel = 'info' | 'warn' | 'error' | 'success';

export interface LogMessage {
    level: LogLevel;
    message: string;
    timestamp: number;
}

const CHANNEL_NAME = 'ybehavior-terminal-channel';

class Logger {
    private channel: BroadcastChannel;

    constructor() {
        this.channel = new BroadcastChannel(CHANNEL_NAME);
    }

    private send(level: LogLevel, message: string) {
        const log: LogMessage = {
            level,
            message,
            timestamp: Date.now(),
        };
        this.channel.postMessage(log);

        // Optional: Also log to console for debugging in main window
        console.log(`[Logger][${level}]`, message);
    }

    info(message: string) {
        this.send('info', message);
    }

    warn(message: string) {
        this.send('warn', message);
    }

    error(message: string) {
        this.send('error', message);
    }

    success(message: string) {
        this.send('success', message);
    }
}

export const logger = new Logger();
export const LOG_CHANNEL_NAME = CHANNEL_NAME;
