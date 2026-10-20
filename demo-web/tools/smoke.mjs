// Smoke E2E de la demo web: juega la lección 1 entera como un jugador (dibuja con el ratón sobre
// la plantilla), comprueba coronas/desbloqueos/blackbook/tienda/muro libre y saca capturas.
//
// Uso:  NODE_PATH=$(npm root -g) node demo-web/tools/smoke.mjs [carpeta-de-capturas]

import { createRequire } from 'node:module';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { mkdirSync } from 'node:fs';

const here = dirname(fileURLToPath(import.meta.url));
const page_url = pathToFileURL(resolve(here, '..', 'dist', 'tag-school-demo.html')).href;
const shots = resolve(process.argv[2] || join(here, '..', 'dist', 'screens'));
mkdirSync(shots, { recursive: true });
// require() respeta NODE_PATH (playwright instalado global); import no.
const { chromium } = createRequire(import.meta.url)('playwright');

const executablePath = process.env.PW_CHROMIUM || undefined;
const browser = await chromium.launch(executablePath ? { executablePath } : {});
const errors = [];
const checks = [];
const check = (ok, what) => { checks.push([ok, what]); console.log(`${ok ? 'PASS' : 'FAIL'}  ${what}`); };

async function run(viewport, prefix) {
  const ctx = await browser.newContext({ viewport, deviceScaleFactor: 1 });
  const page = await ctx.newPage();
  page.on('pageerror', (e) => errors.push(`${prefix}: ${e.message}`));
  page.on('console', (m) => { if (m.type() === 'error' && !/fonts\.(googleapis|gstatic)/.test(m.text())) errors.push(`${prefix}: ${m.text()}`); });
  await page.goto(page_url);
  await page.waitForTimeout(300);

  // Onboarding
  check(await page.isVisible('#onboard'), `${prefix}: pide nombre de writer`);
  await page.fill('#writerInput', 'nova');
  await page.click('#onboardForm button[type=submit]');
  check((await page.textContent('#writerChip')) === 'NOVA', `${prefix}: nombre guardado en mayúsculas`);
  await page.screenshot({ path: join(shots, `${prefix}-1-mapa.png`), fullPage: true });

  // Lección 1
  await page.click('[data-lesson="0"]');
  await page.waitForTimeout(2200); // el profe pinta la demostración
  await page.screenshot({ path: join(shots, `${prefix}-2-showcase.png`) });
  await page.click('#nextStep');

  const tpl = await page.evaluate(() => CONTENT.templates.tpl_hs_A);
  let box;
  const toScreen = ([x, y]) => [box.x + x * box.width, box.y + (1 - y) * box.height];

  async function drawPolyline(pts, { jitter = 0, dwellMs = 0, offset = [0, 0], scale = 1 } = {}) {
    const dense = [];
    for (let i = 0; i < pts.length - 1; i++) {
      const [ax, ay] = pts[i], [bx, by] = pts[i + 1];
      const n = Math.max(2, Math.ceil(Math.hypot(bx - ax, by - ay) / 0.012));
      for (let k = 0; k < n; k++) dense.push([ax + ((bx - ax) * k) / n, ay + ((by - ay) * k) / n]);
    }
    dense.push(pts[pts.length - 1]);
    await page.waitForTimeout(450); // deja terminar el scroll suave
    box = await page.locator('#inputCanvas').boundingBox();
    const tr = ([x, y]) => [0.5 + (x - 0.5) * scale + offset[0] + (Math.random() - 0.5) * jitter, 0.5 + (y - 0.5) * scale + offset[1] + (Math.random() - 0.5) * jitter];
    const [sx, sy] = toScreen(tr(dense[0]));
    await page.mouse.move(sx, sy);
    await page.mouse.down();
    if (dwellMs) await page.waitForTimeout(dwellMs);
    for (const p of dense.slice(1)) {
      const [x, y] = toScreen(tr(p));
      await page.mouse.move(x, y);
      await page.waitForTimeout(16);
    }
    await page.mouse.up();
    await page.waitForTimeout(120);
  }

  // Trace: primero un intento malo (solo un trocito) → debe pedir repetir
  await drawPolyline(tpl.slice(0, 2).map((p, i) => (i ? [(tpl[0][0] + p[0]) / 2, (tpl[0][1] + p[1]) / 2] : p)));
  const failText = await page.textContent('#resultCard');
  check(/Te has dejado parte/.test(failText), `${prefix}: un trazo incompleto no puntúa (puerta de cobertura)`);

  // Intento bueno
  await drawPolyline(tpl, { jitter: 0.004 });
  const crownsTrace = await page.locator('#resultCard .crowns svg.on').count();
  check(crownsTrace >= 2, `${prefix}: trazo limpio sobre la A → ${crownsTrace} coronas`);
  await page.screenshot({ path: join(shots, `${prefix}-3-trace.png`) });
  await page.click('#acceptStep');

  // Freeform: un garabato suave que no es una A no puede puntuar alto (la forma se mide siempre)
  await drawPolyline([[0.3, 0.3], [0.7, 0.35], [0.35, 0.6], [0.65, 0.7], [0.4, 0.4]]);
  await page.click('#evalFree');
  const crownsScribble = await page.locator('#resultCard .crowns svg.on').count();
  check(crownsScribble === 1, `${prefix}: garabato que no es la letra → ${crownsScribble} corona(s)`);
  await page.click('#retryStep');

  // Freeform: la A más pequeña y desplazada, sin guía
  await drawPolyline(tpl, { jitter: 0.006, offset: [0.05, -0.05], scale: 0.7 });
  check(!(await page.isDisabled('#evalFree')), `${prefix}: freeform habilita Evaluar tras dibujar`);
  await page.click('#evalFree');
  const crownsFree = await page.locator('#resultCard .crowns svg.on').count();
  check(crownsFree >= 1, `${prefix}: freeform evaluado → ${crownsFree} coronas`);
  await page.screenshot({ path: join(shots, `${prefix}-4-freeform.png`) });
  await page.click('#acceptStep');

  // Quiz: primero mal, luego bien
  const quiz = await page.evaluate(() => CONTENT.lessons[0].steps[3]);
  const correct = quiz.glossaryRefs[quiz.correctIndex];
  const wrong = await page.locator(`.option:not([data-id="${correct}"])`).first();
  await wrong.click();
  check(await page.isVisible('#stepBody .options'), `${prefix}: respuesta incorrecta no avanza`);
  await page.screenshot({ path: join(shots, `${prefix}-5-quiz.png`) });
  await page.click(`.option[data-id="${correct}"]`);
  await page.waitForTimeout(700);

  // Review
  check(await page.isVisible('#screen-review'), `${prefix}: llega a la pantalla de review`);
  const reviewText = await page.textContent('#review');
  check(/Desbloqueado: cap Soft/.test(reviewText), `${prefix}: la lección 1 desbloquea el cap Soft`);
  check(Number(await page.textContent('#statCoins')) > 0, `${prefix}: suma monedas`);
  await page.click('#saveLessonPiece');
  check(/Guardada/.test(await page.textContent('#reviewMsg')), `${prefix}: guarda la pieza en el blackbook`);
  await page.screenshot({ path: join(shots, `${prefix}-6-review.png`), fullPage: true });

  // Mapa: lección 2 desbloqueada
  await page.click('#toMap');
  check(!(await page.isDisabled('[data-lesson="1"]')), `${prefix}: lección 2 desbloqueada tras pasar la 1`);
  check(await page.isDisabled('[data-lesson="2"]'), `${prefix}: lección 3 sigue bloqueada`);

  // Blackbook + mockup
  await page.click('#tab-book');
  check((await page.locator('.piece').count()) === 1, `${prefix}: el blackbook tiene 1 pieza`);
  await page.click('.piece');
  await page.click('#mockupPicker [data-wall="train"]');
  await page.waitForTimeout(150);
  await page.screenshot({ path: join(shots, `${prefix}-7-mockup.png`) });
  await page.click('#mockupClose');

  // Tienda
  await page.click('#tab-store');
  await page.waitForTimeout(150);
  await page.screenshot({ path: join(shots, `${prefix}-8-tienda.png`), fullPage: true });

  // Muro libre: cap fat bloqueado; aguantar quieto produce drips
  await page.click('#tab-free');
  check(await page.isDisabled('[data-cap="cap_fat"]'), `${prefix}: cap Fat bloqueado al empezar`);
  await page.click('[data-cap="cap_soft"]');
  await page.click('[data-color="#151515"]');
  await drawPolyline([[0.2, 0.75], [0.45, 0.8], [0.7, 0.72]], { dwellMs: 1600 });
  const spawned = await page.evaluate(() => window.__tagschool.engine.dripsSpawned);
  await drawPolyline(await page.evaluate(() => CONTENT.templates.tpl_bub_S), { scale: 0.8 });
  await page.waitForTimeout(800);
  check(spawned > 0, `${prefix}: aguantar el spray quieto genera drips (${spawned})`);
  await page.screenshot({ path: join(shots, `${prefix}-9-muro-libre.png`) });

  // Persistencia
  await page.reload();
  await page.waitForTimeout(300);
  check(!(await page.isVisible('#onboard')) && (await page.textContent('#writerChip')) === 'NOVA', `${prefix}: el progreso persiste al recargar`);
  await ctx.close();
}

await run({ width: 1280, height: 860 }, 'desktop');
await run({ width: 390, height: 844 }, 'movil');
await browser.close();

if (errors.length) { console.log('\nErrores de consola:'); errors.forEach((e) => console.log('  ' + e)); }
const failed = checks.filter(([ok]) => !ok).length + errors.length;
console.log(`\n${checks.length - checks.filter(([ok]) => !ok).length}/${checks.length} checks OK · ${errors.length} errores · capturas en ${shots}`);
process.exit(failed ? 1 : 0);
