import sharp from 'sharp';
import { writeFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import icoEndec from 'ico-endec';
const { encode } = icoEndec;

const __dirname = dirname(fileURLToPath(import.meta.url));
const iconsDir = join(__dirname, '..', 'src-tauri', 'icons');

// SVG 图标内容 - Adobe 风格：深紫色背景 + 浅紫色边框 + Ai 字样
const svgContent = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <!-- 外边框 - 浅紫色 -->
  <rect x="8" y="8" width="496" height="496" rx="48" fill="#C4B5FD"/>
  <!-- 内部背景 - 深紫色 -->
  <rect x="24" y="24" width="464" height="464" rx="36" fill="#4C1D95"/>
  <!-- AI 文字 -->
  <text x="256" y="400" font-family="Adobe Clean, Myriad Pro, Arial, sans-serif" font-size="320" font-weight="600" fill="#C4B5FD" text-anchor="middle">AI</text>
</svg>`;

const sizes = [
  { name: '32x32.png', size: 32 },
  { name: '128x128.png', size: 128 },
  { name: '128x128@2x.png', size: 256 },
  { name: 'icon.png', size: 512 },
  { name: 'Square30x30Logo.png', size: 30 },
  { name: 'Square44x44Logo.png', size: 44 },
  { name: 'Square71x71Logo.png', size: 71 },
  { name: 'Square89x89Logo.png', size: 89 },
  { name: 'Square107x107Logo.png', size: 107 },
  { name: 'Square142x142Logo.png', size: 142 },
  { name: 'Square150x150Logo.png', size: 150 },
  { name: 'Square284x284Logo.png', size: 284 },
  { name: 'Square310x310Logo.png', size: 310 },
  { name: 'StoreLogo.png', size: 50 },
];

async function generateIcons() {
  const svgBuffer = Buffer.from(svgContent);
  
  for (const { name, size } of sizes) {
    const outputPath = join(iconsDir, name);
    await sharp(svgBuffer)
      .resize(size, size)
      .png()
      .toFile(outputPath);
    console.log(`Generated: ${name}`);
  }
  
  // 生成 ICO 文件 - 只用一个 128x128 高质量图像，让 Windows 自动缩放
  const icoImage = await sharp(svgBuffer)
    .resize(128, 128)
    .png()
    .toBuffer();
  
  const icoBuffer = encode([icoImage]);
  writeFileSync(join(iconsDir, 'icon.ico'), icoBuffer);
  console.log('Generated: icon.ico');
  
  console.log('\nAll icons generated successfully!');
}

generateIcons().catch(console.error);
