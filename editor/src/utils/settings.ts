import { getConfigPath, getExeDir } from './configPath';
import { readFile } from './fileService';

export interface Settings {
  editorTreeDir: string;
  runtimeTreeDir: string;
}

let cachedSettings: Settings | null = null;
let loadingPromise: Promise<Settings> | null = null;

/**
 * 获取基准目录（用于解析 settings.json 中的相对路径）
 * 路径相对于 exe 所在目录
 */
async function getBaseDir(): Promise<string> {
  return await getExeDir();
}

/**
 * 加载设置文件
 * 路径相对于基准目录解析
 */
export async function loadSettings(): Promise<Settings> {
  if (cachedSettings) {
    return cachedSettings;
  }
  if (loadingPromise) {
    return loadingPromise;
  }

  loadingPromise = (async () => {
    const settingsPath = await getConfigPath('settings.json');
    const baseDir = await getBaseDir();

    try {
      const content = await readFile(settingsPath);
      const raw = JSON.parse(content);

      // 将相对路径转换为绝对路径（相对于基准目录）
      cachedSettings = {
        editorTreeDir: resolvePath(baseDir, raw.editorTreeDir || ''),
        runtimeTreeDir: resolvePath(baseDir, raw.runtimeTreeDir || ''),
      };

      console.log('Settings loaded:', cachedSettings);
      return cachedSettings;
    } catch (e) {
      console.error('Failed to load settings:', e);
      // 返回默认值
      const exeDir = await getExeDir();
      cachedSettings = {
        editorTreeDir: exeDir,
        runtimeTreeDir: exeDir,
      };
      return cachedSettings;
    } finally {
      loadingPromise = null;
    }
  })();

  return loadingPromise;
}

/**
 * 解析相对路径为绝对路径
 */
function resolvePath(base: string, relative: string): string {
  if (!relative) return base;

  // 如果是绝对路径，直接返回
  if (relative.match(/^[A-Za-z]:[/\\]/) || relative.startsWith('/')) {
    return relative;
  }

  // 处理相对路径
  const baseParts = base.replace(/\\/g, '/').split('/').filter(Boolean);
  const relativeParts = relative.replace(/\\/g, '/').split('/').filter(Boolean);

  for (const part of relativeParts) {
    if (part === '..') {
      baseParts.pop();
    } else if (part !== '.') {
      baseParts.push(part);
    }
  }

  // Windows 路径处理
  if (base.match(/^[A-Za-z]:/)) {
    return baseParts.join('/');
  }
  return '/' + baseParts.join('/');
}

/**
 * 清除缓存的设置（用于重新加载）
 */
export function clearSettingsCache(): void {
  cachedSettings = null;
}
