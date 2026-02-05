import { create } from 'zustand';

interface TooltipState {
    content: string | null;
    position: { x: number; y: number } | null;
    setTooltip: (content: string | null, position?: { x: number; y: number } | null) => void;
}

export const useTooltipStore = create<TooltipState>((set) => ({
    content: null,
    position: null,
    setTooltip: (content, position = null) => set({ content, position: position || null }),
}));
