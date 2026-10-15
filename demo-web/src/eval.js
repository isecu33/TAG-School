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

  function lineWeight(pressures) {
    if (pressures.length < 2) return 1;
    return oneMinusCv(pressures);
  }

  // ---- añadidos de la demo -------------------------------------------------------------------

  function coverage(positions, template, tolerance) {
    if (!template || !positions.length) return 0;
    const probe = resample(template, 0.02);
    let hit = 0;
    for (const q of probe) if (distToPolyline(q, positions) <= tolerance) hit++;
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

  function evaluate(sample, metrics, template, tolerance = 0.1) {
    const { positions, times, pressures } = sample;
    const scores = {};
    for (const m of metrics || []) {
      if (m === 'precision') { if (template) scores[m] = precision(positions, template, tolerance); }
      else if (m === 'smoothness') scores[m] = smoothness(positions);
      else if (m === 'speed_consistency') scores[m] = speedConsistency(positions, times);
      else if (m === 'line_weight') scores[m] = lineWeight(pressures);
    }
    const keys = Object.keys(scores);
    const overall = keys.length ? keys.reduce((s, k) => s + scores[k], 0) / keys.length : 0;
    const crowns = keys.length && positions.length ? (overall >= THREE ? 3 : overall >= TWO ? 2 : 1) : 0;
    return { crowns, overall, scores };
  }

  // Varios trazos (freeform): media de cada métrica ponderada por la longitud de cada trazo.
  function evaluateStrokes(strokes, metrics, template, tolerance) {
    const usable = strokes.filter((s) => s.positions.length >= 2);
    if (!usable.length) return { crowns: 0, overall: 0, scores: {} };
    const total = usable.reduce((s, k) => s + length(k.positions), 0) || 1;
    const acc = {};
    for (const s of usable) {
      const r = evaluate(s, metrics, template, tolerance);
      const w = length(s.positions) / total;
      for (const k in r.scores) acc[k] = (acc[k] || 0) + r.scores[k] * w;
    }
    const keys = Object.keys(acc);
    const overall = keys.length ? keys.reduce((s, k) => s + acc[k], 0) / keys.length : 0;
    const crowns = keys.length ? (overall >= THREE ? 3 : overall >= TWO ? 2 : 1) : 0;
    return { crowns, overall, scores: acc };
  }

  return {
    evaluate, evaluateStrokes, coverage, alignToTemplate, resample, length, bbox,
    precision, smoothness, speedConsistency, lineWeight, NAMES, MIN_COVERAGE,
  };
})();
