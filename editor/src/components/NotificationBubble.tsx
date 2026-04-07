import { useNotificationStore } from '../stores/notificationStore';
import { getTheme } from '../theme/theme';

const theme = getTheme();

export function NotificationBubble() {
    const notifications = useNotificationStore((state) => state.notifications);

    if (notifications.length === 0) return null;

    const getBubbleStyle = (type: string) => {
        if (type === 'error') {
            return { backgroundColor: theme.ui.danger, borderColor: theme.ui.danger, color: theme.ui.tabActiveText };
        }
        if (type === 'success') {
            return { backgroundColor: theme.ui.success, borderColor: theme.ui.success, color: theme.ui.tabActiveText };
        }
        if (type === 'warning') {
            return { backgroundColor: theme.ui.warning, borderColor: theme.ui.warning, color: theme.ui.textMain };
        }
        return { backgroundColor: theme.ui.buttonBg, borderColor: theme.ui.border, color: theme.ui.tabActiveText };
    };

    return (
        <div className="fixed top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-[9999] pointer-events-none flex flex-col gap-2 pointer-events-none">
            {notifications.map((n) => (
                <div
                    key={n.id}
                    className="px-6 py-3 rounded-full shadow-2xl backdrop-blur-md border animate-in fade-in zoom-in duration-300"
                    style={getBubbleStyle(n.type)}
                >
                    <div className="text-lg font-bold text-center whitespace-nowrap">
                        {n.message}
                    </div>
                </div>
            ))}
        </div>
    );
}
