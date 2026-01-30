import { create } from 'zustand';
import type { Tree } from '../types';

interface EditorState {
  // 当前打开的树
  currentTree: Tree | null;
  
  // 选中的节点 ID 列表
  selectedNodeIds: string[];
  
  // 操作方法
  setCurrentTree: (tree: Tree | null) => void;
  selectNodes: (nodeIds: string[]) => void;
  addSelectedNode: (nodeId: string) => void;
  clearSelection: () => void;
  
  // 节点操作
  updateNodePosition: (nodeId: string, x: number, y: number) => void;
  updateNodeNickname: (nodeId: string, nickname: string) => void;
}

export const useEditorStore = create<EditorState>((set) => ({
  currentTree: null,
  selectedNodeIds: [],
  
  setCurrentTree: (tree) => set({ currentTree: tree, selectedNodeIds: [] }),
  
  selectNodes: (nodeIds) => set({ selectedNodeIds: nodeIds }),
  
  addSelectedNode: (nodeId) => set((state) => ({
    selectedNodeIds: state.selectedNodeIds.includes(nodeId)
      ? state.selectedNodeIds
      : [...state.selectedNodeIds, nodeId]
  })),
  
  clearSelection: () => set({ selectedNodeIds: [] }),
  
  updateNodePosition: (nodeId, x, y) => set((state) => {
    if (!state.currentTree) return state;
    const node = state.currentTree.nodes.get(nodeId);
    if (!node) return state;
    
    const newNodes = new Map(state.currentTree.nodes);
    newNodes.set(nodeId, { ...node, position: { x, y } });
    
    return {
      currentTree: { ...state.currentTree, nodes: newNodes }
    };
  }),
  
  updateNodeNickname: (nodeId, nickname) => set((state) => {
    if (!state.currentTree) return state;
    const node = state.currentTree.nodes.get(nodeId);
    if (!node) return state;
    
    const newNodes = new Map(state.currentTree.nodes);
    newNodes.set(nodeId, { ...node, nickname });
    
    return {
      currentTree: { ...state.currentTree, nodes: newNodes }
    };
  }),
}));
