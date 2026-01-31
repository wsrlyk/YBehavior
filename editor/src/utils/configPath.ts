import { invoke } from "@tauri-apps/api/core";

let cachedExeDir: string | null = null;

/**
 * 获取 exe 所在目录
 */
export async function getExeDir(): Promise<string> {
  if (cachedExeDir) {
    return cachedExeDir;
  }
  
  try {
    cachedExeDir = await invoke<string>("get_exe_dir");
    return cachedExeDir;
  } catch (e) {
    console.warn("Failed to get exe dir:", e);
    throw e;
  }
}

/**
 * 获取配置文件路径
 * 配置文件始终在 exe 同目录的 config 文件夹
 * 开发模式下 exe 在 src-tauri/target/debug，config 在项目根目录
 * 需要向上查找到项目根目录
 */
export async function getConfigPath(filename: string): Promise<string> {
  const exeDir = await getExeDir();
  
  // 检查是否是开发模式（exe 路径包含 target/debug 或 target/release）
  if (exeDir.includes("target\\debug") || exeDir.includes("target/debug") ||
      exeDir.includes("target\\release") || exeDir.includes("target/release")) {
    // 开发/构建模式：从 src-tauri/target/xxx 向上找到项目根目录
    // exeDir: .../editor/src-tauri/target/debug
    // 需要: .../editor/config
    const projectRoot = exeDir.replace(/[/\\]src-tauri[/\\]target[/\\](debug|release).*$/, "");
    return `${projectRoot}/config/${filename}`;
  }
  
  // 打包后：配置文件在 exe 同目录的 config 文件夹
  return `${exeDir}/config/${filename}`;
}
