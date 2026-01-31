import { invoke } from '@tauri-apps/api/core';
import { parseTreeXml } from './xmlParser';
import type { Tree } from '../types';
import type { NodeDefinition } from '../types/nodeDefinition';

export async function readFile(path: string): Promise<string> {
  return invoke<string>('read_file', { path });
}

export async function writeFile(path: string, content: string): Promise<void> {
  return invoke('write_file', { path, content });
}

export async function listFiles(dir: string, extensions: string[]): Promise<string[]> {
  return invoke<string[]>('list_files', { dir, extensions });
}

export async function loadTree(
  path: string,
  getNodeDefinition?: (className: string) => NodeDefinition | undefined
): Promise<Tree> {
  const content = await readFile(path);
  const fileName = path.split(/[/\\]/).pop() || 'Unknown';
  return parseTreeXml(content, fileName, getNodeDefinition);
}

export async function listTreeFiles(dir: string): Promise<string[]> {
  return listFiles(dir, ['tree']);
}

export async function listFsmFiles(dir: string): Promise<string[]> {
  return listFiles(dir, ['fsm']);
}

export async function saveFile(path: string, content: string): Promise<void> {
  return writeFile(path, content);
}
