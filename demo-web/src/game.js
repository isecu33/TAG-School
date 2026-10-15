// ---------------------------------------------------------------------------------------------
// TAG School — demo jugable. Flujo de la app igual que en Unity (AppStateMachine):
// Menú (mapa) → Lección (showcase → trace → freeform → quiz) → Review, más Muro libre, Blackbook,
// Glosario y Tienda. Progresión = ProgressionService/UnlockService/StoreService de MetaGame.
// ---------------------------------------------------------------------------------------------

(() => {
  const $ = (id) => document.getElementById(id);
  const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const clamp = (v, a, b) => (v < a ? a : v > b ? b : v);

  const COINS_PER_CROWN = 10;      // ProgressionService.CoinsPerCrown
  const BOOK_LIMIT = 16;
  const SAVE_KEY = 'tagschool.demo.v1';
  const CHAPTERS = {
    1: { name: 'Tag', sub: 'La firma de un trazo: mano firme, presión, flow y velocidad.' },
    2: { name: 'Throw-up', sub: 'Letras de burbuja, rápidas: relleno y contorno.' },
    3: { name: 'Color', sub: 'Capas de color, contornos limpios y brillos.' },
  };
  const COLORS = ['#ff5ea8', '#151515', '#f4f1e8', '#59d3f0', '#f4c84a', '#e8452c', '#58c96b', '#7a5cff'];
  const STEP_LABEL = { showcase: 'Mira', trace: 'Traza', freeform: 'Libre', quiz: 'Quiz' };
  const TIPS = {
    precision: 'Te sales de la guía. Mira un poco por delante del bote, no a la boquilla.',
    smoothness: 'El trazo tiembla. Mueve el brazo entero, no solo la muñeca.',
    speed_consistency: 'Cambias de velocidad. Decide el recorrido antes de apretar y mantén el ritmo.',
    line_weight: 'La presión varía. Mantén el bote a la misma distancia de la pared.',
  };

  const caps = CONTENT.caps;
  const capById = Object.fromEntries(caps.map((c) => [c.id, c]));
  const glossById = Object.fromEntries(CONTENT.glossary.map((g) => [g.id, g]));
  const lessons = CONTENT.lessons;

  // ------------------------------------------------------------------ guardado (IProgressRepo)

  function fresh() {
    return { writer: '', crowns: {}, coins: 0, streak: 0, unlocked: ['cap_skinny'], book: [], sound: true, openAll: false };
  }
  let P = fresh();
  try {
    const raw = localStorage.getItem(SAVE_KEY);
    if (raw) P = Object.assign(fresh(), JSON.parse(raw));
  } catch (_) { /* sin almacenamiento: la demo funciona igual, sin persistir */ }

  function save() {
    try {
      localStorage.setItem(SAVE_KEY, JSON.stringify(P));
      return true;
    } catch (_) {
      return false;
    }
  }

  const totalCrowns = () => Object.values(P.crowns).reduce((s, c) => s + c, 0);
  const owns = (id) => P.unlocked.includes(id);
  const lessonUnlocked = (i) => P.openAll || i === 0 || (P.crowns[lessons[i - 1].id] || 0) > 0;

  // ------------------------------------------------------------------ audio + hápticos

  let audio = null;
  function ensureAudio() {
    if (audio || !P.sound) return;
    try {
      const ctx = new (window.AudioContext || window.webkitAudioContext)();
      const len = ctx.sampleRate * 2;
      const buf = ctx.createBuffer(1, len, ctx.sampleRate);
      const d = buf.getChannelData(0);
      for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
      const src = ctx.createBufferSource();
      src.buffer = buf; src.loop = true;
      const bp = ctx.createBiquadFilter();
      bp.type = 'bandpass'; bp.frequency.value = 4200; bp.Q.value = 0.6;
      const gain = ctx.createGain();
      gain.gain.value = 0;
      src.connect(bp).connect(gain).connect(ctx.destination);
      src.start();
      audio = { ctx, gain };
    } catch (_) { audio = null; }
  }
  function spray(on) {
    if (!audio || !P.sound) return;
    const t = audio.ctx.currentTime;
    audio.gain.gain.cancelScheduledValues(t);
    audio.gain.gain.setTargetAtTime(on ? 0.16 : 0, t, on ? 0.02 : 0.05);
  }
  function blip(freqs, dur = 0.12) {
    if (!audio || !P.sound) return;
    const { ctx } = audio;
    freqs.forEach((f, i) => {
      const o = ctx.createOscillator(), g = ctx.createGain();
      o.type = 'triangle'; o.frequency.value = f;
      const t = ctx.currentTime + i * dur;
      g.gain.setValueAtTime(0.0001, t);
      g.gain.exponentialRampToValueAtTime(0.15, t + 0.01);
      g.gain.exponentialRampToValueAtTime(0.0001, t + dur * 1.6);
      o.connect(g).connect(ctx.destination);
      o.start(t); o.stop(t + dur * 1.8);
    });
  }
  const haptic = (p) => { try { navigator.vibrate && navigator.vibrate(p); } catch (_) {} };

  // ------------------------------------------------------------------ pared + motor

  const bg = $('bgCanvas'), paint = $('paintCanvas'), guide = $('guideCanvas'), input = $('inputCanvas');
  const engine = new SprayEngine.Engine(paint);
  const gctx = guide.getContext('2d');
  let wallKind = 'concrete';
  let selectedCap = 'cap_skinny';
  let selectedColor = COLORS[0];
  let inputEnabled = false;

  function paintWall() { Walls.paint(bg.getContext('2d'), wallKind, bg.width); }

  const strokeConfig = () => ({ cap: capById[selectedCap], paint: CONTENT.paint, color: selectedColor });

  // Plantilla → píxeles de la pared (y hacia arriba en datos, hacia abajo en canvas).
  const toPx = ([x, y], S = 1024) => [x * S, (1 - y) * S];

  function drawGuide(tpl, tolerance, opts = {}) {
    gctx.clearRect(0, 0, guide.width, guide.height);
    if (!tpl) return;
    const S = guide.width;
    const path = () => {
      gctx.beginPath();
      tpl.forEach((p, i) => { const [x, y] = toPx(p, S); i ? gctx.lineTo(x, y) : gctx.moveTo(x, y); });
    };
    gctx.lineCap = gctx.lineJoin = 'round';
    // Banda de tolerancia: lo que cuenta como "encima de la guía".
    if (tolerance) {
      path();
      gctx.strokeStyle = 'rgba(89, 211, 240, 0.09)';
      gctx.lineWidth = tolerance * S * 2 * 0.6;
      gctx.stroke();
    }
    path();
    gctx.setLineDash([18, 14]);
    gctx.strokeStyle = 'rgba(89, 211, 240, 0.75)';
    gctx.lineWidth = 4;
    gctx.stroke();
    gctx.setLineDash([]);
    // Flechas de dirección.
    const pts = Evaluation.resample(tpl, 0.01);
    for (let i = 14; i < pts.length - 4; i += 22) {
      const [x0, y0] = toPx(pts[i - 2], S), [x1, y1] = toPx(pts[i + 2], S);
      const a = Math.atan2(y1 - y0, x1 - x0);
      gctx.save();
      gctx.translate(x1, y1); gctx.rotate(a);
      gctx.fillStyle = 'rgba(89, 211, 240, 0.95)';
      gctx.beginPath(); gctx.moveTo(10, 0); gctx.lineTo(-8, -8); gctx.lineTo(-8, 8); gctx.closePath(); gctx.fill();
      gctx.restore();
    }
    // Inicio.
    const [sx, sy] = toPx(tpl[0], S);
    gctx.fillStyle = '#8ee08a';
    gctx.beginPath(); gctx.arc(sx, sy, 16, 0, Math.PI * 2); gctx.fill();
    gctx.fillStyle = '#10200f';
    gctx.font = '600 18px "IBM Plex Mono", monospace';
    gctx.textAlign = 'center'; gctx.textBaseline = 'middle';
    gctx.fillText('1', sx, sy + 1);
    if (opts.cursor) {
      const [cx, cy] = toPx(opts.cursor, S);
      gctx.strokeStyle = '#efe8d8'; gctx.lineWidth = 3;
      gctx.beginPath(); gctx.arc(cx, cy, 22, 0, Math.PI * 2); gctx.stroke();
    }
  }
  const clearGuide = () => gctx.clearRect(0, 0, guide.width, guide.height);

  // ------------------------------------------------------------------ input (≈ StrokeInputController)

  let down = false, pending = null, pendingPressure = 1, sample = null, t0 = 0;

  function toNorm(e) {
    const r = input.getBoundingClientRect();
    return [clamp((e.clientX - r.left) / r.width, 0, 1), clamp(1 - (e.clientY - r.top) / r.height, 0, 1)];
  }

  input.addEventListener('pointerdown', (e) => {
    if (!inputEnabled || e.button > 0) return;
    e.preventDefault();
    ensureAudio();
    try { input.setPointerCapture(e.pointerId); } catch (_) {}
    if (onStrokeStart) onStrokeStart();
    pendingPressure = e.pointerType === 'pen' && e.pressure > 0 ? e.pressure : 1;
    engine.begin(strokeConfig(), pendingPressure);
    sample = { positions: [], times: [], pressures: [] };
    t0 = performance.now();
    pending = toNorm(e);
    down = true;
    spray(true);
  });
  input.addEventListener('pointermove', (e) => {
    if (!down) return;
    pending = toNorm(e);
    if (e.pointerType === 'pen' && e.pressure > 0) pendingPressure = e.pressure;
  });
  const finishStroke = () => {
    if (!down) return;
    flushSample(performance.now());
    down = false;
    engine.end();
    spray(false);
    if (onStrokeEnd) onStrokeEnd(sample);
  };
  input.addEventListener('pointerup', finishStroke);
  input.addEventListener('pointercancel', finishStroke);
  input.addEventListener('lostpointercapture', finishStroke);

  // Una muestra por frame, como UpdateStroke en Unity.
  function flushSample(now) {
    if (!down || !pending) return;
    engine.update(pending[0], pending[1], pendingPressure);
    sample.positions.push(pending);
    sample.times.push((now - t0) / 1000);
    sample.pressures.push(pendingPressure);
    pending = null;
  }

  let onStrokeStart = null, onStrokeEnd = null;

  // ------------------------------------------------------------------ bucle de frame + HUD

  let ghost = null;          // animación del showcase
  let fpsAvg = 60, last = performance.now();
  function frame(now) {
    flushSample(now);
    if (ghost) stepGhost(now);
    const stamps = engine.tick();
    const dt = now - last; last = now;
    if (dt > 0 && dt < 500) fpsAvg += (1000 / dt - fpsAvg) * 0.05;
    $('hud').textContent = `${Math.round(fpsAvg)} fps · drips ${engine.activeDrips} · stamps ${stamps}`;
    requestAnimationFrame(frame);
  }

  // ------------------------------------------------------------------ herramientas

  function renderTools() {
    $('capRow').innerHTML = caps.map((c) => {
      const has = owns(c.id);
      return `<button type="button" class="cap-btn" data-cap="${c.id}" aria-pressed="${c.id === selectedCap}" ${has ? '' : 'disabled title="Bloqueado: gánalo en una lección o cómpralo en la tienda"'}>
        <span>${esc(c.name)}</span><small>${c.coneAngle}° ${has ? '' : '· bloqueado'}</small></button>`;
    }).join('');
    $('colorRow').innerHTML = COLORS.map((c) =>
      `<button type="button" class="swatch" data-color="${c}" style="background:${c}" aria-label="Color ${c}" aria-pressed="${c === selectedColor}"></button>`).join('');
    $('wallPicker').innerHTML = Object.entries(Walls.KINDS).map(([k, w]) =>
      `<button type="button" data-wall="${k}" aria-pressed="${k === wallKind}">${w.name}</button>`).join('');
    $('undoBtn').disabled = !engine.canUndo;
    $('redoBtn').disabled = !engine.canRedo;
  }
  engine.onChange = () => { $('undoBtn').disabled = !engine.canUndo; $('redoBtn').disabled = !engine.canRedo; };

  $('tools').addEventListener('click', (e) => {
    const b = e.target.closest('button');
    if (!b) return;
    if (b.dataset.cap) { selectedCap = b.dataset.cap; blip([660], 0.05); renderTools(); }
    else if (b.dataset.color) { selectedColor = b.dataset.color; renderTools(); }
    else if (b.dataset.wall) { wallKind = b.dataset.wall; paintWall(); renderTools(); }
  });
  $('distance').addEventListener('input', (e) => {
    engine.distance = e.target.value / 100;
    $('distanceVal').textContent = e.target.value;
  });
  $('undoBtn').addEventListener('click', () => { engine.undo(); if (onUndo) onUndo(); });
  $('redoBtn').addEventListener('click', () => engine.redo());
  $('clearBtn').addEventListener('click', () => { engine.clear(); if (onUndo) onUndo(true); });
  let onUndo = null;

  // ------------------------------------------------------------------ navegación

  const SCREENS = ['map', 'stage', 'review', 'book', 'glossary', 'store'];
  let freeSnapshot = null, inFree = false;
  function show(screen, tab) {
    if (inFree) { freeSnapshot = engine.snapshot(); inFree = false; }
    SCREENS.forEach((s) => { $('screen-' + s).hidden = s !== screen; });
    document.querySelectorAll('.tabs button').forEach((b) => b.setAttribute('aria-selected', String(b.dataset.screen === tab)));
    stopGhost();
    if (screen !== 'stage') { inputEnabled = false; finishStroke(); }
    window.scrollTo({ top: 0 });
  }

  document.querySelector('.tabs').addEventListener('click', (e) => {
    const b = e.target.closest('button[data-screen]');
    if (!b) return;
    ensureAudio();
    blip([520], 0.04);
    const s = b.dataset.screen;
    if (s === 'map') openMap();
    else if (s === 'free') openFree();
    else if (s === 'book') openBook();
    else if (s === 'glossary') openGlossary();
    else if (s === 'store') openStore();
  });

  function renderStats() {
    $('statCrowns').textContent = totalCrowns();
    $('statCoins').textContent = P.coins;
    $('writerChip').textContent = P.writer || 'writer';
    $('soundBtn').setAttribute('aria-pressed', String(P.sound));
  }

  const crownSvg = (on) => `<svg class="${on ? 'on' : ''}" viewBox="0 0 24 24" aria-hidden="true"><path d="M3 18h18l-1.5-10-5 4.5L12 5l-2.5 7.5-5-4.5z"/></svg>`;
  const crownRow = (n, big = false) =>
    `<span class="crowns${big ? ' big' : ''}" aria-label="${n} de 3 coronas">${[1, 2, 3].map((i) => crownSvg(i <= n)).join('')}</span>`;

  // ------------------------------------------------------------------ mapa

  function lessonTemplate(l) {
    const tr = l.steps.find((s) => s.type === 'trace');
    return tr ? { id: tr.template, pts: CONTENT.templates[tr.template], tol: tr.tolerance } : null;
  }

  function openMap() {
    show('map', 'map');
    const next = lessons.findIndex((l) => !P.crowns[l.id]);
    $('mapMentor').innerHTML = totalCrowns() === 0
      ? `Hola <b>${esc(P.writer || 'writer')}</b>. Empieza por el capítulo 1: tu primer tag. Una letra, un trazo.`
      : next >= 0
        ? `Vas bien, <b>${esc(P.writer)}</b>. Lo siguiente: <b>${esc(lessons[next].title)}</b>.`
        : `Has pasado las ${lessons.length} lecciones. Repite las que no tengan tres coronas o pinta en el muro libre.`;
    $('reviewMode').checked = P.openAll;

    const byChapter = {};
    lessons.forEach((l, i) => (byChapter[l.chapter] = byChapter[l.chapter] || []).push([l, i]));
    $('chapters').innerHTML = Object.entries(byChapter).map(([ch, list]) => `
      <section class="chapter">
        <h3>Capítulo ${ch} · ${esc(CHAPTERS[ch]?.name || '')}</h3>
        <p class="chapter-sub">${esc(CHAPTERS[ch]?.sub || '')}</p>
        <div class="lesson-grid">
          ${list.map(([l, i], k) => {
            const open = lessonUnlocked(i);
            const c = P.crowns[l.id] || 0;
            return `<button type="button" class="lesson-card${open ? '' : ' locked'}" data-lesson="${i}" ${open ? '' : 'disabled'}>
              <span class="letter"><canvas width="92" height="92" data-tpl="${esc(lessonTemplate(l)?.id || '')}"></canvas></span>
              <span class="meta">Lección ${k + 1} · ${open ? (c ? 'superada' : 'nueva') : 'bloqueada'}</span>
              <span class="t">${esc(l.title)}</span>
              ${crownRow(c)}
            </button>`;
          }).join('')}
        </div>
      </section>`).join('');
    document.querySelectorAll('canvas[data-tpl]').forEach((cv) => {
      const pts = CONTENT.templates[cv.dataset.tpl];
      if (!pts) return;
      const c = cv.getContext('2d');
      c.lineCap = c.lineJoin = 'round';
      c.strokeStyle = '#ff5ea8';
      c.lineWidth = 7;
      c.beginPath();
      pts.forEach((p, i) => { const [x, y] = toPx(p, 92); i ? c.lineTo(x, y) : c.moveTo(x, y); });
      c.stroke();
    });
  }

  $('chapters').addEventListener('click', (e) => {
    const b = e.target.closest('[data-lesson]');
    if (b && !b.disabled) startLesson(+b.dataset.lesson);
  });
  $('reviewMode').addEventListener('change', (e) => { P.openAll = e.target.checked; save(); openMap(); });

  // ------------------------------------------------------------------ lección (LessonRunner)

  let L = null;

  function startLesson(index) {
    const lesson = lessons[index];
    const tpl = lessonTemplate(lesson);
    L = { index, lesson, step: 0, drawCrowns: [], tpl, freeStrokes: [], result: null, piece: null, quizWrong: 0 };
    wallKind = lesson.chapter === 2 ? 'shutter' : lesson.chapter === 3 ? 'brick' : 'concrete';
    // Cap recomendado: el más gordo que tengas para throw-up/color, skinny para tags.
    selectedCap = lesson.chapter === 1 ? 'cap_skinny' : (['cap_fat', 'cap_soft', 'cap_skinny'].find(owns) || 'cap_skinny');
    paintWall();
    engine.clear(false);
    show('stage', 'map');
    $('lessonPanel').hidden = false;
    $('freePanel').hidden = true;
    $('lessonCrumb').textContent = `Capítulo ${lesson.chapter} · ${CHAPTERS[lesson.chapter]?.name || ''}`;
    $('lessonTitle').textContent = lesson.title;
    renderTools();
    renderStep();
  }

  function mentor(html) { $('mentorText').innerHTML = html; }

  function renderPills() {
    $('stepPills').innerHTML = L.lesson.steps.map((s, i) =>
      `<li class="${i < L.step ? 'done' : i === L.step ? 'now' : ''}">${STEP_LABEL[s.type] || s.type}</li>`).join('');
  }

  // En móvil el panel queda debajo de la pared: al cambiar de paso, vuelve a enseñar la pared.
  function revealWall() {
    const r = $('wall').getBoundingClientRect();
    if (r.top < 0 || r.top > window.innerHeight * 0.5) $('wall').scrollIntoView({ block: 'start', behavior: 'smooth' });
  }

  function renderStep() {
    const s = L.lesson.steps[L.step];
    renderPills();
    if (L.step > 0 && s.type !== 'quiz') revealWall();
    $('resultCard').hidden = true;
    L.result = null;
    onStrokeStart = null; onStrokeEnd = null; onUndo = null;
    stopGhost();

    if (s.type === 'showcase') {
      inputEnabled = false;
      mentor(esc(s.caption || 'Mira cómo se hace y luego te toca.'));
      $('stepBody').innerHTML = `
        <p class="muted small">Demostración (sustituye al vídeo <span class="mono">${esc(s.media)}</span>, que aún no existe).</p>
        <div class="row">
          <button type="button" class="btn ghost" id="replayGhost">Ver otra vez</button>
          <button type="button" class="btn primary" id="nextStep">Entendido, me toca</button>
        </div>`;
      $('replayGhost').onclick = () => playGhost();
      $('nextStep').onclick = () => advance();
      playGhost();
    } else if (s.type === 'trace') {
      engine.clear(false);
      inputEnabled = true;
      drawGuide(L.tpl.pts, s.tolerance);
      mentor(`Sigue la guía cian <b>de una sola pasada</b>, empezando en el punto verde. La banda clara es tu margen de error.`);
      $('stepBody').innerHTML = `
        <p class="prompt">Traza la letra ${esc(s.template.split('_').pop())}</p>
        <p class="muted small">Puntúa: ${s.metrics.map((m) => Evaluation.NAMES[m]).join(', ')}. Levanta el dedo para evaluar.</p>`;
      onStrokeStart = () => { if (L.result) { engine.clear(false); $('resultCard').hidden = true; L.result = null; } };
      onStrokeEnd = (smp) => evaluateTrace(s, smp);
    } else if (s.type === 'freeform') {
      engine.clear(false);
      clearGuide();
      inputEnabled = true;
      L.freeStrokes = [];
      const signs = /nombre|tag/i.test(s.prompt);
      mentor(signs
        ? `Sin guía. Firma <b>${esc(P.writer || 'tu nombre')}</b> como te salga: de un trazo si puedes. Pulsa <b>Evaluar</b> cuando acabes.`
        : `Sin guía. Hazlo de memoria, del tamaño que quieras. Pulsa <b>Evaluar</b> cuando acabes.`);
      $('stepBody').innerHTML = `
        <p class="prompt">${esc(s.prompt)}</p>
        <p class="muted small">Puntúa: ${s.evaluate.map((m) => Evaluation.NAMES[m]).join(', ')}.</p>
        <div class="row"><button type="button" class="btn primary" id="evalFree" disabled>Evaluar</button></div>`;
      $('evalFree').onclick = () => evaluateFree(s);
      onStrokeStart = () => { if (L.result) { $('resultCard').hidden = true; L.result = null; } };
      onStrokeEnd = (smp) => {
        if (smp.positions.length >= 2) L.freeStrokes.push(smp);
        $('evalFree').disabled = Evaluation.length(L.freeStrokes.flatMap((k) => k.positions)) < 0.15;
      };
      onUndo = (all) => {
        if (all) L.freeStrokes = []; else L.freeStrokes.pop();
        $('evalFree').disabled = Evaluation.length(L.freeStrokes.flatMap((k) => k.positions)) < 0.15;
      };
    } else if (s.type === 'quiz') {
      inputEnabled = false;
      clearGuide();
      renderQuiz(s);
    }
  }

  function advance() {
    L.step++;
    if (L.step >= L.lesson.steps.length) finishLesson();
    else renderStep();
  }

  // ---- showcase: el profe pinta la plantilla con el motor real

  function stopGhost() {
    if (ghost && ghost.started) { engine.end(); spray(false); }
    ghost = null;
  }

  function playGhost() {
    stopGhost();
    engine.clear(false);
    const pts = Evaluation.resample(L.tpl.pts, 0.004);
    const cfg = { cap: capById[selectedCap] || caps[0], paint: CONTENT.paint, color: selectedColor };
    ghost = { pts, i: 0, cfg, started: false, pause: 0 };
  }

  function stepGhost() {
    const g = ghost;
    if (!g.started) { engine.begin(g.cfg, 1); g.started = true; spray(true); }
    const perFrame = 3;
    for (let k = 0; k < perFrame && g.i < g.pts.length; k++, g.i++) engine.update(g.pts[g.i][0], g.pts[g.i][1], 1);
    drawGuide(L.tpl.pts, 0, { cursor: g.pts[Math.min(g.i, g.pts.length - 1)] });
    if (g.i >= g.pts.length) {
      engine.end();
      spray(false);
      drawGuide(L.tpl.pts, 0);
      ghost = null;
    }
  }

  // ---- evaluación (DrawingStepController)

  function bars(scores, extra = []) {
    const rows = Object.entries(scores).map(([k, v]) => [Evaluation.NAMES[k] || k, v]).concat(extra);
    return `<div class="bars">${rows.map(([name, v]) => `
      <div class="bar"><span>${esc(name)}</span><span class="track"><span class="fill${v < 0.5 ? ' low' : ''}" style="width:${Math.round(v * 100)}%"></span></span><span class="v">${Math.round(v * 100)}%</span></div>`).join('')}</div>`;
  }

  function weakest(scores) {
    let k = null, v = 2;
    for (const [m, s] of Object.entries(scores)) if (s < v) { v = s; k = m; }
    return k;
  }

  function showResult(r, extraBars, retryLabel, onRetry) {
    const card = $('resultCard');
    const tip = r.crowns === 3 ? 'Limpio. Así se pinta.' : TIPS[weakest(r.scores)] || 'Otra vez, con calma.';
    card.innerHTML = `
      <div class="row" style="justify-content:space-between"><h4>Resultado</h4>${crownRow(r.crowns)}</div>
      ${bars(r.scores, extraBars)}
      <div class="row">
        <button type="button" class="btn ghost" id="retryStep">${retryLabel}</button>
        <button type="button" class="btn primary" id="acceptStep">Siguiente</button>
      </div>`;
    card.hidden = false;
    mentor(esc(tip));
    $('retryStep').onclick = onRetry;
    $('acceptStep').onclick = () => {
      L.drawCrowns.push(clamp(r.crowns, 1, 3));
      if (L.lesson.steps[L.step].type === 'freeform') capturePiece();
      advance();
    };
    blip(r.crowns === 3 ? [660, 880, 1100] : r.crowns === 2 ? [660, 880] : [520]);
    haptic(r.crowns === 3 ? [20, 40, 20] : 15);
  }

  function evaluateTrace(step, smp) {
    if (smp.positions.length < 3) return;
    const cov = Evaluation.coverage(smp.positions, L.tpl.pts, step.tolerance * 0.6);
    if (cov < Evaluation.MIN_COVERAGE) {
      L.result = { fail: true };
      const card = $('resultCard');
      card.innerHTML = `
        <h4>Te has dejado parte de la letra</h4>
        ${bars({}, [['Cobertura', cov]])}
        <p class="muted small">Necesitas recorrer al menos el ${Math.round(Evaluation.MIN_COVERAGE * 100)}% de la guía. Vuelve a pintar encima: se borra solo.</p>`;
      card.hidden = false;
        mentor('Recorre la letra entera, del punto verde hasta el final, sin levantar.');
      blip([300], 0.1);
      return;
    }
    const r = Evaluation.evaluate(smp, step.metrics, L.tpl.pts, step.tolerance);
    L.result = r;
    showResult(r, [['Cobertura', cov]], 'Repetir', () => { engine.clear(false); $('resultCard').hidden = true; L.result = null; revealWall(); });
  }

  function evaluateFree(step) {
    let strokes = L.freeStrokes;
    if (step.evaluate.includes('precision') && L.tpl) {
      // Sin guía: se compara la forma (dibujo encajado en la caja de la plantilla).
      const all = strokes.flatMap((k) => k.positions);
      const a = Evaluation.bbox(all), b = Evaluation.bbox(L.tpl.pts);
      const map = ([x, y]) => [b.x0 + ((x - a.x0) / a.w) * b.w, b.y0 + ((y - a.y0) / a.h) * b.h];
      strokes = strokes.map((k) => ({ ...k, positions: k.positions.map(map) }));
    }
    const r = Evaluation.evaluateStrokes(strokes, step.evaluate, L.tpl?.pts, L.tpl ? 0.2 : 0.1);
    if (!r.crowns) return;
    L.result = r;
    showResult(r, [], 'Borrar y repetir', () => {
      engine.clear(false); L.freeStrokes = []; $('resultCard').hidden = true; L.result = null; $('evalFree').disabled = true;
      revealWall();
    });
  }

  // ---- quiz

  function renderQuiz(step) {
    const refs = step.glossaryRefs;
    const correct = refs[step.correctIndex];
    const used = new Set([...refs, ...L.lesson.glossaryRefs]);
    const pool = CONTENT.glossary.filter((g) => !used.has(g.id));
    const distractor = pool[(L.index * 3) % pool.length];
    const options = refs.map((id) => glossById[id]).concat(distractor ? [distractor] : []);
    mentor('Última. Un writer sabe de qué habla.');
    $('stepBody').innerHTML = `
      <p class="muted small">¿Qué término es este?</p>
      <p class="quiz-q">“${esc(glossById[correct].definition)}”</p>
      <div class="options">${options.map((g) => `<button type="button" class="option" data-id="${g.id}">${esc(g.term)}</button>`).join('')}</div>`;
    $('stepBody').querySelector('.options').onclick = (e) => {
      const b = e.target.closest('.option');
      if (!b) return;
      if (b.dataset.id === correct) {
        b.classList.add('right');
        blip([660, 990]);
        setTimeout(advance, 450);
      } else {
        b.classList.remove('wrong'); void b.offsetWidth; b.classList.add('wrong');
        L.quizWrong++;
        mentor(`No. “${esc(glossById[b.dataset.id].term)}” es otra cosa: ${esc(glossById[b.dataset.id].definition.toLowerCase())}`);
        blip([260], 0.12);
        haptic(30);
      }
    };
  }

  // ---- fin de lección (LessonPassed → ProgressionService + UnlockService)

  function flatten(size, withWall) {
    const c = document.createElement('canvas');
    c.width = c.height = size;
    const x = c.getContext('2d');
    if (withWall) x.drawImage(bg, 0, 0, size, size);
    x.drawImage(paint, 0, 0, size, size);
    return c;
  }

  function capturePiece() {
    L.piece = { img: flatten(384, true).toDataURL('image/jpeg', 0.82), ink: flatten(384, false).toDataURL('image/png') };
  }

  function finishLesson() {
    const crowns = L.drawCrowns.length
      ? clamp(Math.round(L.drawCrowns.reduce((s, c) => s + c, 0) / L.drawCrowns.length), 1, 3)
      : 3;
    const id = L.lesson.id;
    const prev = P.crowns[id] || 0;
    const improved = crowns > prev;
    let coins = 0;
    if (improved) { P.crowns[id] = crowns; coins = crowns * COINS_PER_CROWN; P.coins += coins; }
    P.streak += 1;
    const unlocked = [];
    for (const u of L.lesson.unlocks) if (!owns(u)) { P.unlocked.push(u); unlocked.push(u); }
    save();
    renderStats();
    blip([523, 659, 784, 1046], 0.11);
    haptic([30, 60, 30, 60, 80]);
    openReview(crowns, coins, unlocked, prev);
  }

  function openReview(crowns, coins, unlocked, prev) {
    show('review', 'map');
    const heads = { 3: 'Pieza de king', 2: 'Buen trazo', 1: 'Aprobado. Sigue dándole' };
    const next = L.index + 1 < lessons.length ? L.index + 1 : -1;
    const piece = L.piece;
    $('review').innerHTML = `
      <p class="crumb">${esc(L.lesson.title)}</p>
      <h2>${heads[crowns]}</h2>
      ${crownRow(crowns, true)}
      <p>${coins ? `+${coins} monedas` : prev ? `Tu récord sigue en ${prev} corona${prev > 1 ? 's' : ''}: sin monedas esta vez.` : ''}</p>
      ${unlocked.map((u) => `<span class="unlock">Desbloqueado: cap ${esc(capById[u]?.name || u)}</span>`).join('')}
      ${piece ? `<img class="thumb" src="${piece.img}" alt="Tu pieza de esta lección">` : ''}
      <div class="row" style="justify-content:center">
        ${piece ? `<button type="button" class="btn ghost" id="saveLessonPiece">Guardar en el blackbook</button>` : ''}
        <button type="button" class="btn ghost" id="again">Repetir lección</button>
        ${next >= 0 ? `<button type="button" class="btn primary" id="nextLesson">Siguiente lección</button>` : ''}
        <button type="button" class="btn link" id="toMap">Volver al mapa</button>
      </div>
      <p class="muted small" id="reviewMsg" role="status"></p>`;
    if (piece) $('saveLessonPiece').onclick = (e) => {
      const ok = addToBook({ title: L.lesson.title, ...piece, crowns });
      $('reviewMsg').textContent = ok ? 'Guardada en tu blackbook.' : 'No se pudo guardar: el navegador no deja almacenar más.';
      e.target.disabled = ok;
    };
    $('again').onclick = () => startLesson(L.index);
    if (next >= 0) $('nextLesson').onclick = () => startLesson(next);
    $('toMap').onclick = openMap;
  }

  // ------------------------------------------------------------------ muro libre

  function openFree() {
    L = null;
    show('stage', 'free');
    $('lessonPanel').hidden = true;
    $('freePanel').hidden = false;
    $('freeMsg').textContent = '';
    clearGuide();
    paintWall();
    engine.clear(false);
    if (freeSnapshot) engine.restore(freeSnapshot);
    inFree = true;
    inputEnabled = true;
    onStrokeStart = null; onStrokeEnd = null; onUndo = null;
    renderTools();
  }

  $('saveFree').addEventListener('click', () => {
    const title = $('pieceName').value.trim() || `Pieza de ${P.writer || 'writer'}`;
    const ok = addToBook({
      title,
      img: flatten(384, true).toDataURL('image/jpeg', 0.82),
      ink: flatten(384, false).toDataURL('image/png'),
    });
    $('freeMsg').textContent = ok ? `“${title}” guardada en tu blackbook.` : 'No se pudo guardar: el navegador no deja almacenar más.';
  });

  // ------------------------------------------------------------------ blackbook + mockups

  function addToBook(entry) {
    P.book.unshift({ id: Date.now().toString(36), date: new Date().toISOString().slice(0, 10), writer: P.writer, ...entry });
    while (P.book.length > BOOK_LIMIT) P.book.pop();
    if (save()) return true;
    P.book.shift();
    return false;
  }

  function openBook() {
    show('book', 'book');
    $('bookGrid').innerHTML = P.book.length
      ? P.book.map((b, i) => `
        <button type="button" class="piece" data-piece="${i}">
          <img src="${b.img}" alt="${esc(b.title)}">
          <span class="t">${esc(b.title)}</span>
          <span class="d">${esc(b.writer || '')} · ${esc(b.date)}${b.crowns ? ' · ' + b.crowns + '♛' : ''}</span>
        </button>`).join('')
      : `<p class="empty">Aún no hay piezas. Termina una lección y guarda tu dibujo libre, o pinta algo en el muro libre.</p>`;
  }
  $('bookGrid').addEventListener('click', (e) => {
    const b = e.target.closest('[data-piece]');
    if (b) openMockup(P.book[+b.dataset.piece]);
  });

  let mockKind = 'brick', mockPiece = null;
  function drawMockup() {
    const c = $('mockupBg'), x = c.getContext('2d');
    Walls.paint(x, mockKind, c.width);
    const g = x.createLinearGradient(0, 0, 0, c.height);
    g.addColorStop(0, 'rgba(0,0,0,0.25)'); g.addColorStop(0.6, 'rgba(0,0,0,0)'); g.addColorStop(1, 'rgba(0,0,0,0.45)');
    x.fillStyle = g; x.fillRect(0, 0, c.width, c.height);
    $('mockupPicker').innerHTML = Object.entries(Walls.KINDS).map(([k, w]) =>
      `<button type="button" data-wall="${k}" aria-pressed="${k === mockKind}">${w.name}</button>`).join('');
  }
  function openMockup(piece) {
    mockPiece = piece;
    $('mockupImg').src = piece.ink || piece.img;
    $('mockupTitle').textContent = piece.title;
    drawMockup();
    $('mockup').hidden = false;
  }
  $('mockupPicker').addEventListener('click', (e) => {
    const b = e.target.closest('[data-wall]');
    if (b) { mockKind = b.dataset.wall; drawMockup(); }
  });
  $('mockupClose').addEventListener('click', () => { $('mockup').hidden = true; });
  $('mockup').addEventListener('click', (e) => { if (e.target.id === 'mockup') $('mockup').hidden = true; });

  // ------------------------------------------------------------------ glosario

  function openGlossary() {
    show('glossary', 'glossary');
    $('glossaryList').innerHTML = CONTENT.glossary.map((g) => `
      <div class="term">
        <dt>${esc(g.term)}</dt>
        <dd>${esc(g.definition)}</dd>
        <dd class="era">Época: ${esc(g.era || '—')}${g.relatedTerms?.length ? ' · ver: ' + g.relatedTerms.map((r) => esc(glossById[r]?.term || r)).join(', ') : ''}</dd>
      </div>`).join('');
  }

  // ------------------------------------------------------------------ tienda (StoreService)

  function openStore() {
    show('store', 'store');
    $('storeGrid').innerHTML = caps.map((c) => `
      <div class="cap-card">
        <canvas width="480" height="150" data-preview="${c.id}"></canvas>
        <h3>${esc(c.name)}</h3>
        <span class="spec">cono ${c.coneAngle}° · densidad ${c.density} · overspray ${c.overspray}</span>
        ${owns(c.id)
          ? `<span class="owned">En tu mochila</span>`
          : `<button type="button" class="btn ${P.coins >= c.unlockCost ? 'primary' : 'ghost'}" data-buy="${c.id}" ${P.coins >= c.unlockCost ? '' : 'disabled'}>Comprar · ${c.unlockCost} monedas</button>`}
      </div>`).join('');
    document.querySelectorAll('canvas[data-preview]').forEach((cv) => {
      Walls.paint(cv.getContext('2d'), 'concrete', 480);
      const ink = document.createElement('canvas');
      ink.width = 480; ink.height = 150;
      const e = new SprayEngine.Engine(ink, { drips: false });
      e.distance = 0.3;
      e.begin({ cap: capById[cv.dataset.preview], paint: CONTENT.paint, color: '#ff5ea8' });
      for (let i = 0; i <= 80; i++) {
        const t = i / 80;
        e.update(0.08 + t * 0.84, 0.5 + 0.28 * Math.sin(t * Math.PI * 2), 1);
      }
      e.end();
      cv.getContext('2d').drawImage(ink, 0, 0);
    });
  }
  $('storeGrid').addEventListener('click', (e) => {
    const b = e.target.closest('[data-buy]');
    if (!b) return;
    const cap = capById[b.dataset.buy];
    if (owns(cap.id) || P.coins < cap.unlockCost) return;
    P.coins -= cap.unlockCost;
    P.unlocked.push(cap.id);
    save();
    blip([784, 1046]);
    renderStats();
    openStore();
  });

  $('resetBtn').addEventListener('click', () => { $('resetConfirm').hidden = false; });
  $('resetNo').addEventListener('click', () => { $('resetConfirm').hidden = true; });
  $('resetYes').addEventListener('click', () => {
    const keepName = P.writer;
    P = fresh();
    P.writer = keepName;
    save();
    $('resetConfirm').hidden = true;
    renderStats();
    openMap();
  });

  // ------------------------------------------------------------------ onboarding + cabecera

  function askWriter() {
    $('writerInput').value = P.writer;
    $('onboard').hidden = false;
    setTimeout(() => $('writerInput').focus(), 30);
  }
  $('onboardForm').addEventListener('submit', (e) => {
    e.preventDefault();
    const v = $('writerInput').value.trim().toUpperCase().slice(0, 10);
    if (!v) return;
    P.writer = v;
    save();
    $('onboard').hidden = true;
    ensureAudio();
    renderStats();
    if (!$('screen-map').hidden) openMap();
  });
  $('writerChip').addEventListener('click', askWriter);
  $('soundBtn').addEventListener('click', () => {
    P.sound = !P.sound;
    if (!P.sound) spray(false);
    save();
    ensureAudio();
    renderStats();
  });
  $('exitLesson').addEventListener('click', openMap);

  // ------------------------------------------------------------------ arranque

  $('footInfo').textContent =
    `Contenido generado desde el proyecto Unity (${lessons.length} lecciones, ${CONTENT.glossary.length} términos, ` +
    `${caps.length} caps, ${Object.keys(CONTENT.templates).length} plantillas) · build ${CONTENT.builtAt}`;
  paintWall();
  renderTools();
  renderStats();
  openMap();
  if (!P.writer) askWriter();
  window.__tagschool = { engine }; // depuración / tests E2E
  requestAnimationFrame(frame);
})();
