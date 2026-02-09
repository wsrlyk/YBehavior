import { create } from 'zustand';
import { logger } from '../utils/logger';

export type NotificationType = 'info' | 'error' | 'success' | 'warning';

export interface Notification {
    id: string;
    message: string;
    type: NotificationType;
}

interface NotificationState {
    notifications: Notification[];
    notify: (message: string, type?: NotificationType) => void;
    remove: (id: string) => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
    notifications: [],
    notify: (message, type = 'info') => {
        // Log to logger automatically
        if (type === 'error') {
            logger.error(message);
        } else if (type === 'warning') {
            logger.warn(message);
        } else if (type === 'success') {
            logger.success(message);
        } else {
            logger.info(message);
        }

        const id = Date.now().toString();
        set((state) => ({
            notifications: [...state.notifications, { id, message, type }],
        }));

        // Auto remove after 2 seconds
        setTimeout(() => {
            set((state) => ({
                notifications: state.notifications.filter((n) => n.id !== id),
            }));
        }, 2000);
    },
    remove: (id) =>
        set((state) => ({
            notifications: state.notifications.filter((n) => n.id !== id),
        })),
}));
