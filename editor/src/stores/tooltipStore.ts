import { create } from 'zustand';

interface TooltipState {
    content: string | null;
    position: { x: number; y: number } | null;
    isDisabled: boolean;
    setTooltip: (content: string | null, position?: { x: number; y: number } | null) => void;
    setDisabled: (disabled: boolean) => void;
}

export const useTooltipStore = create<TooltipState>((set, get) => ({
    content: null,
    position: null,
    isDisabled: false,
    setTooltip: (content, position = null) => {
        if (get().isDisabled) return;
        set({ content, position: position || null });
    },
    setDisabled: (disabled: boolean) => {
        const updates: Partial<TooltipState> = { isDisabled: disabled };
        if (disabled) updates.content = null;
        set(updates);
    },
}));
