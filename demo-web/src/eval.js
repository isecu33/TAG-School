// ---------------------------------------------------------------------------------------------
// Evaluación de trazos — port 1:1 de Lessons/Evaluation (StrokeMetrics, TraceTemplate,
// TraceEvaluator) de Unity. Todas las métricas devuelven 0..1 (1 = mejor); umbrales de coronas
// idénticos (≥0.75 → 3, ≥0.5 → 2, si no 1).
//
// Añadidos de la demo (propuestos para Unity, ver docs/DEMO-JUGABLE.md):
//  · coverage(): fracción de la plantilla que el trazo recorre. Sin esto un punto encima de la
//    plantilla saca precisión 1. Se usa como puerta: por debajo de MIN_COVERAGE se repite.
//  · alignToTemplate(): en freeform no hay guía, así que la precisión se mide tras encajar la
//    caja del dibujo en la de la plantilla de la lección (similitud de forma, no de posición).
//  · Con plantilla, la forma se puntúa SIEMPRE (precisión) y limita las coronas (peor de precisión
//    y cobertura): un trazo suave que no es la letra no saca coronas.
//  · lineWeight mide el grosor real (presión + velocidad, sprayWidth), no solo la presión: con
//    ratón o dedo la presión es siempre 1 y antes regalaba un 100 %.
// ---------------------------------------------------------------------------------------------

const Evaluation = (() => {
  const EPS = 1e-6;
  const THREE = 0.75, TWO = 0.5;
  const MIN_COVERAGE = 0.6;
  const clamp01 = (v) => (v < 0 ? 0 : v > 1 ? 1 : v);

  function distToSegment(p, a, b) {
    const abx = b[0] - a[0], aby = b[1] - a[1];
    const len = abx * abx + aby * aby;
    if (len < 1e-12) return Math.hypot(p[0] - a[0], p[1] - a[1]);
    const t = clamp01(((p[0] - a[0]) * abx + (p[1] - a[1]) * aby) / len);
    return Math.hypot(p[0] - (a[0] + t * abx), p[1] - (a[1] + t * aby));
  }

  function distToPolyline(p, pts) {
    if (!pts.length) return 0;
    if (pts.length === 1) return Math.hypot(p[0] - pts[0][0], p[1] - pts[0][1]);
    let best = Infinity;
    for (let i = 0; i < pts.length - 1; i++) {
      const d = distToSegment(p, pts[i], pts[i + 1]);
      if (d < best) best = d;
    }
    return best;
  }

  function resample(pts, step) {
    const out = [pts[0]];
    for (let i = 0; i < pts.length - 1; i++) {
      const a = pts[i], b = pts[i + 1];
      const L = Math.hypot(b[0] - a[0], b[1] - a[1]);
      const n = Math.max(1, Math.ceil(L / step));
      for (let k = 1; k <= n; k++) out.push([a[0] + ((b[0] - a[0]) * k) / n, a[1] + ((b[1] - a[1]) * k) / n]);
    }
    return out;
  }

  function length(pts) {
    let L = 0;
    for (let i = 1; i < pts.length; i++) L += Math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]);
    return L;
  }

  // ---- StrokeMetrics -----------------------------------------------------------------------

  function precision(positions, template, tolerance) {
    if (!positions.length || !template || tolerance <= EPS) return 0;
    let sum = 0;
    for (const p of positions) sum += distToPolyline(p, template);
    return clamp01(1 - sum / positions.length / tolerance);
  }

  function smoothness(positions) {
    if (positions.length < 3) return 1;
    let sumSq = 0, count = 0;
    for (let i = 1; i < positions.length - 1; i++) {
      const ax = positions[i][0] - positions[i - 1][0], ay = positions[i][1] - positions[i - 1][1];
      const bx = positions[i + 1][0] - positions[i][0], by = positions[i + 1][1] - positions[i][1];
      const la = ax * ax + ay * ay, lb = bx * bx + by * by;
      if (la < EPS * EPS || lb < EPS * EPS) continue;
      const cos = Math.max(-1, Math.min(1, (ax * bx + ay * by) / Math.sqrt(la * lb)));
      const ang = Math.acos(cos);
      sumSq += ang * ang;
      count++;
    }
    if (!count) return 1;
    const ref = (Math.PI / 2) ** 2;
    return clamp01(1 - sumSq / count / ref);
  }

  function oneMinusCv(values) {
    const n = values.length;
    let mean = 0;
    for (const v of values) mean += v;
    mean /= n;
    if (mean < EPS) return 0;
    let v2 = 0;
    for (const v of values) v2 += (v - mean) ** 2;
    return clamp01(1 - Math.sqrt(v2 / n) / mean);
  }

  function speedConsistency(positions, times) {
    if (positions.length < 3 || times.length !== positions.length) return 1;
    const speeds = [];
    for (let i = 0; i < positions.length - 1; i++) {
      const dt = times[i + 1] - times[i];
      speeds.push(dt > EPS ? Math.hypot(positions[i + 1][0] - positions[i][0], positions[i + 1][1] - positions[i][1]) / dt : 0);
    }
    return oneMinusCv(speeds);
  }

  // Grosor relativo del spray: menos presión → boquilla más lejos → más gordo; mano rápida → más
  // fino. Mismo modelo que StrokeMetrics.SprayWidth (Unity) y que el motor.
  const THIN_START = 0.25, THIN_RANGE = 1.25, MAX_THIN = 0.35; // velocidad en anchos de muro/s
  function sprayWidth(pressure, speed) {
    const dist = 1 + 0.5 * (1 - clamp01(pressure));
    return dist * (1 - MAX_THIN * clamp01((speed - THIN_START) / THIN_RANGE));
  }

  function lineWeight(positions, times, pressures) {
    const n = positions.length;
    if (n < 3 || !times || times.length !== n) return pressures.length < 2 ? 1 : oneMinusCv(pressures);
    const raw = [];
    for (let i = 0; i < n; i++) {
      const a = Math.max(0, i - 1), b = Math.min(n - 1, i + 1);
      const dt = times[b] - times[a];
      raw.push(dt > EPS ? Math.hypot(positions[b][0] - positions[a][0], positions[b][1] - positions[a][1]) / dt : 0);
    }
    const widths = [];
    for (let i = 0; i < n; i++) {
      const a = Math.max(0, i - 1), b = Math.min(n - 1, i + 1);
      let v = 0;
      for (let k = a; k <= b; k++) v += raw[k];
      widths.push(sprayWidth(pressures[i] ?? 1, v / (b - a + 1)));
    }
    return oneMinusCv(widths);
  }

  // ---- añadidos de la demo -------------------------------------------------------------------

  // Acepta un trazo (lista de puntos) o varios (lista de trazos): cuenta si CUALQUIERA pasa cerca.
  function coverage(positions, template, tolerance) {
    if (!template || !positions.length) return 0;
    const strokes = Array.isArray(positions[0][0]) ? positions : [positions];
    const probe = resample(template, 0.02);
    let hit = 0;
    for (const q of probe) if (strokes.some((s) => s.length && distToPolyline(q, s) <= tolerance)) hit++;
    return hit / probe.length;
  }

  function bbox(pts) {
    let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
    for (const [x, y] of pts) { if (x < x0) x0 = x; if (y < y0) y0 = y; if (x > x1) x1 = x; if (y > y1) y1 = y; }
    return { x0, y0, x1, y1, w: Math.max(x1 - x0, EPS), h: Math.max(y1 - y0, EPS) };
  }

  function alignToTemplate(positions, template) {
    if (!positions.length || !template) return positions;
    const a = bbox(positions), b = bbox(template);
    return positions.map(([x, y]) => [b.x0 + ((x - a.x0) / a.w) * b.w, b.y0 + ((y - a.y0) / a.h) * b.h]);
  }

  // ---- TraceEvaluator ------------------------------------------------------------------------

  const NAMES = {
    precision: 'Precisión',
    smoothness: 'Suavidad',
    speed_consistency: 'Velocidad constante',
    line_weight: 'Peso de línea',
  };

  const COVERAGE_TOL = 0.6; // la cobertura se mide con la misma banda que la puerta de la UI
  const crownsFor = (v) => (v >= THREE ? 3 : v >= TWO ? 2 : 1);

  function styleScores(sample, metrics) {
    const { positions, times, pressures } = sample;
    const scores = {};
    for (const m of metrics || []) {
      if (m === 'smoothness') scores[m] = smoothness(positions);
      else if (m === 'speed_consistency') scores[m] = speedConsistency(positions, times);
      else if (m === 'line_weight') scores[m] = lineWeight(positions, times, pressures);
    }
    return scores;
  }

  function finish(scores, cov, hasShape, hasInk) {
    const keys = Object.keys(scores);
    const overall = keys.length ? keys.reduce((s, k) => s + scores[k], 0) / keys.length : 0;
    let crowns = keys.length && hasInk ? crownsFor(overall) : 0;
    if (crowns && hasShape) crowns = Math.min(crowns, crownsFor(Math.min(scores.precision, cov)));
    return { crowns, overall, scores, coverage: cov };
  }

  // Un trazo. Con plantilla la forma se puntúa siempre, la pida la lección o no.
  function evaluate(sample, metrics, template, tolerance = 0.1) {
    const { positions } = sample;
    const scores = styleScores(sample, metrics);
    const hasShape = !!template && positions.length > 0;
    let cov = 1;
    if (hasShape) {
      scores.precision = precision(positions, template, tolerance);
      cov = coverage(positions, template, tolerance * COVERAGE_TOL);
    }
    return finish(scores, cov, hasShape, positions.length > 0);
  }

  // Varios trazos (freeform): estilo ponderado por la longitud de cada trazo; la forma se mide
  // sobre el dibujo entero (todos los trazos juntos).
  function evaluateStrokes(strokes, metrics, template, tolerance) {
    const usable = strokes.filter((s) => s.positions.length >= 2);
    if (!usable.length) return { crowns: 0, overall: 0, scores: {}, coverage: 0 };
    const total = usable.reduce((s, k) => s + length(k.positions), 0) || 1;
    const scores = {};
    for (const s of usable) {
      const r = styleScores(s, metrics);
      const w = length(s.positions) / total;
      for (const k in r) scores[k] = (scores[k] || 0) + r[k] * w;
    }
    let cov = 1;
    if (template) {
      scores.precision = precision(usable.flatMap((s) => s.positions), template, tolerance);
      cov = coverage(usable.map((s) => s.positions), template, tolerance * COVERAGE_TOL);
    }
    return finish(scores, cov, !!template, true);
  }

  return {
    evaluate, evaluateStrokes, coverage, alignToTemplate, resample, length, bbox,
    precision, smoothness, speedConsistency, lineWeight, sprayWidth, NAMES, MIN_COVERAGE,
  };
})();
