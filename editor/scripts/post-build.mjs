import { copyFileSync, mkdirSync, existsSync, readdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const projectRoot = join(__dirname, '..');
const configDir = join(projectRoot, 'config');
const releaseDir = join(projectRoot, 'src-tauri', 'target', 'release');
const releaseConfigDir = join(releaseDir, 'config');

// 确保目标目录存在
if (!existsSync(releaseConfigDir)) {
  mkdirSync(releaseConfigDir, { recursive: true });
}

// 复制所有配置文件
const configFiles = readdirSync(configDir);
for (const file of configFiles) {
  const src = join(configDir, file);
  const dest = join(releaseConfigDir, file);
  copyFileSync(src, dest);
  console.log(`Copied: ${file}`);
}

console.log('\nConfig files copied to release/config/');
