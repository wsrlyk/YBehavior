import { useNotificationStore } from '../stores/notificationStore';

export function NotificationBubble() {
    const notifications = useNotificationStore((state) => state.notifications);

    if (notifications.length === 0) return null;

    return (
        <div className="fixed top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-[9999] pointer-events-none flex flex-col gap-2 pointer-events-none">
            {notifications.map((n) => (
                <div
                    key={n.id}
                    className={`px-6 py-3 rounded-full shadow-2xl backdrop-blur-md border animate-in fade-in zoom-in duration-300 ${n.type === 'error'
                            ? 'bg-red-500/90 border-red-400 text-white'
                            : n.type === 'success'
                                ? 'bg-green-500/90 border-green-400 text-white'
                                : n.type === 'warning'
                                    ? 'bg-yellow-500/90 border-yellow-400 text-black'
                                    : 'bg-blue-500/90 border-blue-400 text-white'
                        }`}
                >
                    <div className="text-lg font-bold text-center whitespace-nowrap">
                        {n.message}
                    </div>
                </div>
            ))}
        </div>
    );
}
