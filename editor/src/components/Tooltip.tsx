import { useEffect, useState } from 'react';
import { useTooltipStore } from '../stores/tooltipStore';
import { createPortal } from 'react-dom';

export default function Tooltip() {
    const { content, position } = useTooltipStore();
    const [coords, setCoords] = useState({ x: 0, y: 0 });

    useEffect(() => {
        const handleMouseMove = (e: MouseEvent) => {
            setCoords({ x: e.clientX, y: e.clientY });
        };

        if (content) {
            window.addEventListener('mousemove', handleMouseMove);
        }

        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
        };
    }, [content]);

    if (!content) return null;

    // Use fixed position if provided, otherwise follow mouse
    const displayX = position ? position.x : coords.x + 12;
    const displayY = position ? position.y : coords.y + 12;

    return createPortal(
        <div
            className="fixed z-[9999] pointer-events-none transition-opacity duration-150"
            style={{
                left: displayX,
                top: displayY,
                opacity: content ? 1 : 0,
            }}
        >
            <div className="bg-gray-900/90 backdrop-blur-md border border-gray-700/50 text-gray-100 px-3 py-2 rounded-lg shadow-2xl max-w-sm text-xs leading-relaxed whitespace-pre-wrap animate-in fade-in zoom-in-95 duration-200">
                {content}
            </div>
        </div>,
        document.body
    );
}
