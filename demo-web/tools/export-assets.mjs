// Exporta a PNG los assets procedurales de la demo para el proyecto Unity, usando el MISMO
// código que la demo (src/walls.js) dentro de Chromium headless:
//   - paredes 512² → unity6/Assets/_Project/Art/Textures/Walls/wall_<tipo>.png
//
// Uso:  NODE_PATH=$(npm root -g) node demo-web/tools/export-assets.mjs

import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const { chromium } = createRequire(import.meta.url)('playwright');
const here = dirname(fileURLToPath(import.meta.url));
const wallsJs = readFileSync(resolve(here, '..', 'src', 'walls.js'), 'utf8');
const outDir = resolve(here, '..', '..', 'unity6', 'Assets', '_Project', 'Art', 'Textures', 'Walls');
const SIZE = 512; // igual que los placeholders originales; suficiente para móvil

const executablePath = process.env.PW_CHROMIUM || undefined;
const browser = await chromium.launch(executablePath ? { executablePath } : {});
const page = await browser.newPage();
await page.setContent('<canvas id="c"></canvas>');
await page.addScriptTag({ content: `${wallsJs}\nwindow.Walls = Walls;` });

const kinds = await page.evaluate(() => Object.keys(window.Walls.KINDS));
for (const kind of kinds) {
  const dataUrl = await page.evaluate(({ kind, size }) => {
    const c = document.getElementById('c');
    c.width = c.height = size;
    window.Walls.paint(c.getContext('2d'), kind, size);
    return c.toDataURL('image/png');
  }, { kind, size: SIZE });
  const file = join(outDir, `wall_${kind}.png`);
  writeFileSync(file, Buffer.from(dataUrl.split(',')[1], 'base64'));
  console.log(`→ ${file}`);
}
await browser.close();
