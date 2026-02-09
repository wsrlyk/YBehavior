export type LogLevel = 'info' | 'warn' | 'error' | 'success';

export interface LogSegment {
    text: string;
    color?: string; // Hex code or Tailwind class
    newline?: boolean;
}

export interface LogMessage {
    level: LogLevel;
    message: string | LogSegment[];
    timestamp: number;
}

const CHANNEL_NAME = 'ybehavior-terminal-channel';

class Logger {
    private channel: BroadcastChannel;

    constructor() {
        this.channel = new BroadcastChannel(CHANNEL_NAME);
    }

    private send(level: LogLevel, message: string | LogSegment[]) {
        const log: LogMessage = {
            level,
            message,
            timestamp: Date.now(),
        };
        this.channel.postMessage(log);

        // Optional: Also log to console for debugging in main window (Commented out for production)
        // if (typeof message === 'string') {
        //     console.log(`[Logger][${level}]`, message);
        // } else {
        //     console.log(`[Logger][${level}]`, message.map(s => s.text).join(''));
        // }
    }

    info(message: string | LogSegment[]) {
        this.send('info', message);
    }

    // ... rest same ...
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
