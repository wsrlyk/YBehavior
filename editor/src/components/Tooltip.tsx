import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { useTooltipStore } from '../stores/tooltipStore';
import { createPortal } from 'react-dom';
import { getTheme } from '../theme/theme';

const theme = getTheme();

export default function Tooltip() {
    const { content, position } = useTooltipStore();
    const [coords, setCoords] = useState({ x: 0, y: 0 });
    const [adjusted, setAdjusted] = useState({ x: 0, y: 0 });
    const tooltipRef = useRef<HTMLDivElement | null>(null);

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

    // Use fixed position if provided, otherwise follow mouse
    const displayX = position ? position.x : coords.x + 12;
    const displayY = position ? position.y : coords.y + 12;

    useLayoutEffect(() => {
        const el = tooltipRef.current;
        if (!el || !content) return;

        const offset = 12;
        const padding = 8;
        const rect = el.getBoundingClientRect();
        const width = rect.width;
        const height = rect.height;
        const vw = window.innerWidth;
        const vh = window.innerHeight;

        let x = displayX;
        let y = displayY;

        if (x + width + padding > vw) {
            x = Math.max(padding, vw - width - padding);
        }

        if (y + height + padding > vh) {
            y = Math.max(padding, displayY - height - offset);
        }

        if (x < padding) x = padding;
        if (y < padding) y = padding;

        setAdjusted((prev) => (prev.x === x && prev.y === y ? prev : { x, y }));
    }, [content, displayX, displayY]);

    if (!content) return null;

    return createPortal(
        <div
            ref={tooltipRef}
            className="fixed z-[9999] pointer-events-none transition-opacity duration-150"
            style={{
                left: adjusted.x > 0 ? adjusted.x : displayX,
                top: adjusted.y > 0 ? adjusted.y : displayY,
                opacity: content ? 1 : 0,
            }}
        >
            <div
                className="backdrop-blur-md px-3 py-2 rounded-lg shadow-2xl max-w-sm text-xs leading-relaxed whitespace-pre-wrap animate-in fade-in zoom-in-95 duration-200"
                style={{
                    backgroundColor: theme.ui.panelBg,
                    border: `1px solid ${theme.ui.border}`,
                    color: theme.ui.textMain,
                }}
            >
                {content}
            </div>
        </div>,
        document.body
    );
}
