// Construye la demo web jugable a partir del contenido REAL del proyecto Unity:
//   - lecciones   unity6/Assets/_Project/Content/Lessons/*.json (orden del Chapter1ContentBuilder)
//   - glosario    unity6/Assets/_Project/Content/Glossary/glossary.json
//   - caps        valores de Cap(...) en Content/Editor/Chapter1ContentBuilder.cs
//   - plantillas  polilíneas A-Z de Content/Editor/HandstyleLetterTemplates.cs
// Salida: dist/tag-school-demo.html (autocontenido, se abre con doble clic, sin servidor).
//
// Uso:  node demo-web/build.mjs           (desde la raíz del repo o desde demo-web/)

import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const project = join(here, '..', 'unity6', 'Assets', '_Project');
const contentDir = join(project, 'Content');
const read = (p) => readFileSync(p, 'utf8');

// ---- caps + orden de lecciones (fuente: el content builder de Unity) ------------------------
const builder = read(join(contentDir, 'Editor', 'Chapter1ContentBuilder.cs'));

const caps = [...builder.matchAll(
  /Cap\("(\w+)",\s*"([^"]+)",\s*([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f,\s*(\d+)\)/g,
)].map((m) => ({
  id: m[1], name: m[2], coneAngle: +m[3], density: +m[4], falloff: +m[5],
  flowRate: +m[6], overspray: +m[7], unlockCost: +m[8],
}));
if (caps.length < 3) throw new Error('No se pudieron leer los caps del Chapter1ContentBuilder.');

const paintMatch = builder.match(/Paint\("(\w+)",\s*"([^"]+)",\s*([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f\)/);
const paint = { id: paintMatch[1], name: paintMatch[2], opacity: +paintMatch[3], glossiness: +paintMatch[4], dripThreshold: +paintMatch[5] };

const filesBlock = builder.match(/LessonFiles\s*=\s*\{([\s\S]*?)\};/)[1];
const lessonOrder = [...filesBlock.matchAll(/"(\w+)"/g)].map((m) => m[1]);
const lessons = lessonOrder.map((id) => JSON.parse(read(join(contentDir, 'Lessons', `${id}.json`))));

const glossary = JSON.parse(read(join(contentDir, 'Glossary', 'glossary.json'))).entries;

// ---- plantillas A-Z ------------------------------------------------------------------------
const tplSrc = read(join(contentDir, 'Editor', 'HandstyleLetterTemplates.cs'));
const body = tplSrc.slice(tplSrc.indexOf('Templates = new Vector2[][]'));
const letters = [...body.matchAll(/new\[\]\s*\{([^}]*)\}/g)]
  .map((m) => [...m[1].matchAll(/v\(([\d.]+)f,\s*([\d.]+)f\)/g)].map((p) => [+p[1], +p[2]]))
  .filter((pts) => pts.length >= 2)
  .slice(0, 26);
if (letters.length !== 26) throw new Error(`Se esperaban 26 plantillas, hay ${letters.length}.`);

// Bubble = handstyle suavizado con Chaikin (mismo algoritmo que BubbleLetterTemplates.cs).
function chaikin(pts, iterations) {
  let out = pts;
  for (let it = 0; it < iterations; it++) {
    const next = [out[0]];
    for (let i = 0; i < out.length - 1; i++) {
      const [ax, ay] = out[i], [bx, by] = out[i + 1];
      next.push([0.75 * ax + 0.25 * bx, 0.75 * ay + 0.25 * by]);
      next.push([0.25 * ax + 0.75 * bx, 0.25 * ay + 0.75 * by]);
    }
    next.push(out[out.length - 1]);
    out = next;
  }
  return out.map(([x, y]) => [Math.round(x * 1e4) / 1e4, Math.round(y * 1e4) / 1e4]);
}

const templates = {};
letters.forEach((pts, i) => {
  const ch = String.fromCharCode(65 + i);
  templates[`tpl_hs_${ch}`] = pts;
  templates[`tpl_bub_${ch}`] = chaikin(pts, 3);
});

// ---- integridad: toda plantilla y término referenciado existe --------------------------------
const glossaryIds = new Set(glossary.map((e) => e.id));
for (const l of lessons) {
  for (const s of l.steps) {
    if (s.type === 'trace' && !templates[s.template]) throw new Error(`${l.id}: plantilla ${s.template} no existe`);
    for (const g of s.glossaryRefs || []) if (!glossaryIds.has(g)) throw new Error(`${l.id}: término ${g} no existe`);
  }
}

const content = { caps, paint, lessons, glossary, templates, builtAt: new Date().toISOString().slice(0, 10) };

// ---- ensamblado ----------------------------------------------------------------------------
const src = (f) => read(join(here, 'src', f));
const fragment = src('shell.html')
  .replace('/*__STYLE__*/', () => src('style.css'))
  .replace(
    '/*__SCRIPT__*/',
    () => [
      `const CONTENT = ${JSON.stringify(content)};`,
      src('walls.js'),
      src('engine.js'),
      src('eval.js'),
      src('game.js'),
    ].join('\n'),
  );

const standalone = `<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
</head>
<body>
${fragment}
</body>
</html>
`;

mkdirSync(join(here, 'dist'), { recursive: true });
writeFileSync(join(here, 'dist', 'tag-school-demo.html'), standalone);
// Fragmento sin <html>/<head> para publicar como Artifact (el host añade el esqueleto).
if (process.argv.includes('--fragment')) {
  const out = process.argv[process.argv.indexOf('--fragment') + 1];
  writeFileSync(out, fragment);
}

console.log(
  `OK · ${lessons.length} lecciones · ${glossary.length} términos · ${caps.length} caps · ` +
  `${Object.keys(templates).length} plantillas → demo-web/dist/tag-school-demo.html`,
);
