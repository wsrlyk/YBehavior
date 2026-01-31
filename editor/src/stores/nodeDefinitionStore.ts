import { create } from 'zustand';
import type { NodeDefinition } from '../types/nodeDefinition';
import type { NodeCategory } from '../types';
import { parseActionsXml, parseBuiltinXml } from '../utils/nodeDefinitionLoader';
import { readFile } from '../utils/fileService';
import { getConfigPath } from '../utils/configPath';

interface NodeDefinitionState {
  definitions: Map<string, NodeDefinition>;
  isLoaded: boolean;
  error: string | null;
  
  loadDefinitions: () => Promise<void>;
  getDefinition: (className: string) => NodeDefinition | undefined;
  getByCategory: (category: NodeCategory) => NodeDefinition[];
  getAllDefinitions: () => NodeDefinition[];
}

export const useNodeDefinitionStore = create<NodeDefinitionState>((set, get) => ({
  definitions: new Map(),
  isLoaded: false,
  error: null,
  
  loadDefinitions: async () => {
    const definitions = new Map<string, NodeDefinition>();
    
    // 从 exe 同目录的 config 文件夹加载配置
    const builtinPath = await getConfigPath('builtin.xml');
    const actionsPath = await getConfigPath('actions.xml');
    
    // 加载内置节点
    try {
      const builtinContent = await readFile(builtinPath);
      const builtinDefs = parseBuiltinXml(builtinContent);
      for (const def of builtinDefs) {
        definitions.set(def.className, def);
      }
    } catch (e) {
      console.warn('Failed to load builtin.xml:', e);
    }
    
    // 加载外部节点
    try {
      const content = await readFile(actionsPath);
      const externalDefs = parseActionsXml(content);
      for (const def of externalDefs) {
        definitions.set(def.className, def);
      }
    } catch (e) {
      console.warn('Failed to load actions.xml:', e);
    }
    
    set({ definitions, isLoaded: true, error: null });
  },
  
  getDefinition: (className) => {
    return get().definitions.get(className);
  },
  
  getByCategory: (category) => {
    const result: NodeDefinition[] = [];
    get().definitions.forEach((def) => {
      if (def.category === category) {
        result.push(def);
      }
    });
    return result;
  },
  
  getAllDefinitions: () => {
    return Array.from(get().definitions.values());
  },
}));
