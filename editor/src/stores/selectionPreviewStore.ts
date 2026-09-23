import { create } from 'zustand';

interface SelectionPreviewState {
    active: boolean;
    nodeIds: Set<string>;
    begin: () => void;
    update: (nodeIds: Set<string>) => void;
    end: () => void;
}

export const useSelectionPreviewStore = create<SelectionPreviewState>((set) => ({
    active: false,
    nodeIds: new Set(),
    begin: () => set({ active: true, nodeIds: new Set() }),
    update: (nodeIds) => set({ nodeIds }),
    end: () => set({ active: false, nodeIds: new Set() }),
}));
