// ---------------------------------------------------------------------------------------------
// SprayEngine — port web (Canvas 2D) del Drawing Engine de Unity (ARQUITECTURA §4).
// Mismos conceptos: cap {coneAngle, density, falloff, flowRate, overspray}, paint {opacity,
// dripThreshold}, distancia de boquilla 0..1, interpolación Catmull-Rom, drips procedurales por
// acumulación de pintura, undo/redo por snapshots. Coordenadas normalizadas [0,1], (0,0) abajo-izq.
// ---------------------------------------------------------------------------------------------

const SprayEngine = (() => {
  const DRIP_GRID = 40;          // celdas por lado del acumulador de pintura (PaintAccumulator)
  const DRIP_GAIN = 1.5;         // cuánto suma cada pasada al acumulador
  const DRIP_DECAY = 0.985;      // "secado" por frame
  const MAX_DRIPS = 48;
  const UNDO_STEPS = 12;

  const clamp = (v, a, b) => (v < a ? a : v > b ? b : v);

  function catmull(p0, p1, p2, p3, t) {
    const t2 = t * t, t3 = t2 * t;
    const f = (a, b, c, d) =>
      0.5 * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3);
    return [f(p0[0], p1[0], p2[0], p3[0]), f(p0[1], p1[1], p2[1], p3[1])];
  }

  class Engine {
    constructor(canvas, { drips = true } = {}) {
      this.canvas = canvas;
      this.ctx = canvas.getContext('2d');
      this.S = canvas.width;           // escala de radios (ancho)
      this.H = canvas.height;
      this.sprites = new Map();
      this.dripsEnabled = drips;
      this.drips = [];
      this.dripsSpawned = 0;
      this.acc = new Float32Array(DRIP_GRID * DRIP_GRID);
      this.undoStack = [];
      this.redoStack = [];
      this.distance = 0.25;       // NozzleDistance (slider)
      this.pressure = 1;
      this.active = false;
      this.cfg = null;
      this.pts = [];
      this.carry = 0;
      this.movedThisFrame = false;
      this.stampsThisFrame = 0;
      this.onChange = null;
    }

    // ------------------------------------------------------------------ API (≈ IDrawingCanvas)

    begin(cfg, pressure = 1) {
      this.pushUndo();
      this.cfg = cfg;
      this.pressure = pressure;
      this.active = true;
      this.pts = [];
      this.carry = 0;
    }

    update(x, y, pressure = 1) {
      if (!this.active) return;
      this.pressure = pressure;
      const p = [x * this.S, (1 - y) * this.H];
      const last = this.pts[this.pts.length - 1];
      if (last && Math.hypot(p[0] - last[0], p[1] - last[1]) < 0.5) return;
      this.pts.push(p);
      this.movedThisFrame = true;
      const n = this.pts.length;
      if (n === 1) { this.stamp(p[0], p[1]); return; }
      const p0 = this.pts[n - 3] || this.pts[n - 2];
      const p1 = this.pts[n - 2];
      const p2 = this.pts[n - 1];
      this.strokeSegment(p0, p1, p2, p2);
    }

    end() {
      if (!this.active) return;
      this.active = false;
      this.onChange && this.onChange();
    }

    undo() {
      if (!this.undoStack.length) return;
      this.redoStack.push(this.snapshot());
      this.restore(this.undoStack.pop());
      this.drips.length = 0;
      this.onChange && this.onChange();
    }

    redo() {
      if (!this.redoStack.length) return;
      this.undoStack.push(this.snapshot());
      this.restore(this.redoStack.pop());
      this.onChange && this.onChange();
    }

    clear(keepHistory = true) {
      if (keepHistory) this.pushUndo();
      else { this.undoStack.length = 0; this.redoStack.length = 0; }
      this.ctx.clearRect(0, 0, this.S, this.H);
      this.drips.length = 0;
      this.acc.fill(0);
      this.onChange && this.onChange();
    }

    get canUndo() { return this.undoStack.length > 0; }
    get canRedo() { return this.redoStack.length > 0; }
    get activeDrips() { return this.drips.length; }

    // Llamar una vez por frame: spray "en parado" (acumula → drips) + animación de drips.
    tick() {
      if (this.active && !this.movedThisFrame && this.pts.length) {
        const p = this.pts[this.pts.length - 1];
        this.stamp(p[0], p[1], 0.6);
      }
      this.movedThisFrame = false;
      for (let i = 0; i < this.acc.length; i++) this.acc[i] *= DRIP_DECAY;
      this.stepDrips();
      const s = this.stampsThisFrame;
      this.stampsThisFrame = 0;
      return s;
    }

    // ------------------------------------------------------------------ stamping

    effDistance() {
      // En Pencil la presión acerca la boquilla (§4.1): más presión → más cerca.
      const d = this.distance + (1 - this.pressure) * 0.5;
      return clamp(d, 0, 1);
    }

    radius(cap, d) {
      const half = (cap.coneAngle * Math.PI) / 360;
      return this.S * (0.0065 + Math.tan(half) * (0.04 + 0.22 * d));
    }

    strokeSegment(p0, p1, p2, p3) {
      const cap = this.cfg.cap;
      const R = this.radius(cap, this.effDistance());
      const spacing = Math.max(1, R * 0.35);
      const len = Math.hypot(p2[0] - p1[0], p2[1] - p1[1]);
      const steps = Math.max(1, Math.ceil(len / 2));
      let prev = p1;
      for (let i = 1; i <= steps; i++) {
        const q = catmull(p0, p1, p2, p3, i / steps);
        let seg = Math.hypot(q[0] - prev[0], q[1] - prev[1]);
        while (this.carry + seg >= spacing) {
          const need = spacing - this.carry;
          const t = need / seg;
          const x = prev[0] + (q[0] - prev[0]) * t;
          const y = prev[1] + (q[1] - prev[1]) * t;
          this.stamp(x, y);
          prev = [x, y];
          seg -= need;
          this.carry = 0;
        }
        this.carry += seg;
        prev = q;
      }
    }

    sprite(color, falloff) {
      const bucket = Math.round(falloff * 4);
      const key = color + '|' + bucket;
      let s = this.sprites.get(key);
      if (s) return s;
      s = document.createElement('canvas');
      s.width = s.height = 64;
      const g = s.getContext('2d');
      const grad = g.createRadialGradient(32, 32, 0, 32, 32, 32);
      const hard = 0.1 + (bucket / 4) * 0.65;
      grad.addColorStop(0, color);
      grad.addColorStop(hard, color);
      grad.addColorStop(1, 'rgba(0,0,0,0)');
      g.fillStyle = grad;
      g.fillRect(0, 0, 64, 64);
      this.sprites.set(key, s);
      return s;
    }

    stamp(x, y, weight = 1) {
      const { cap, paint, color } = this.cfg;
      const ctx = this.ctx;
      const d = this.effDistance();
      const R = this.radius(cap, d);
      const flow = clamp(cap.flowRate * (1.15 - 0.75 * d), 0.04, 0.5) * (paint.opacity ?? 1) * weight;
      this.stampsThisFrame++;

      // Núcleo: dos manchas suaves con jitter (la dureza del borde viene de falloff).
      const spr = this.sprite(color, cap.falloff);
      ctx.globalAlpha = flow * 0.55;
      for (let k = 0; k < 2; k++) {
        const jx = (Math.random() - 0.5) * R * 0.25;
        const jy = (Math.random() - 0.5) * R * 0.25;
        ctx.drawImage(spr, x + jx - R, y + jy - R, R * 2, R * 2);
      }

      // Partículas: textura granulada del spray.
      ctx.fillStyle = color;
      const n = Math.round(cap.density * 0.7 * weight);
      for (let i = 0; i < n; i++) {
        const a = Math.random() * Math.PI * 2;
        const r = R * Math.sqrt(Math.random()) * 1.05;
        const sz = 0.8 + Math.random() * (0.6 + R * 0.05);
        ctx.globalAlpha = clamp(flow * 1.4 * (1 - (r / R) * 0.55), 0, 1);
        ctx.fillRect(x + Math.cos(a) * r, y + Math.sin(a) * r, sz, sz);
      }

      // Overspray: niebla exterior, crece con la distancia (§4.1).
      const m = Math.round(cap.overspray * cap.density * (0.4 + 1.6 * d) * weight);
      ctx.globalAlpha = clamp(0.1 + 0.08 * d, 0, 1) * (paint.opacity ?? 1);
      for (let i = 0; i < m; i++) {
        const a = Math.random() * Math.PI * 2;
        const r = R * (1 + Math.random() * 1.6);
        const sz = 0.8 + Math.random() * 0.9;
        ctx.fillRect(x + Math.cos(a) * r, y + Math.sin(a) * r, sz, sz);
      }
      ctx.globalAlpha = 1;

      if (this.dripsEnabled) this.accumulate(x, y, R, flow, R * 0.35);
    }

    // ------------------------------------------------------------------ drips (ENG-04)

    accumulate(x, y, R, flow, spacing) {
      const cell = this.S / DRIP_GRID;
      const cx = clamp(Math.floor(x / cell), 0, DRIP_GRID - 1);
      const cy = clamp(Math.floor(y / (this.H / DRIP_GRID)), 0, DRIP_GRID - 1);
      const i = cy * DRIP_GRID + cx;
      this.acc[i] += flow * (spacing / cell) * DRIP_GAIN * (R / (this.S * 0.012));
      const threshold = this.cfg.paint.dripThreshold ?? 1.2;
      if (this.acc[i] > threshold && this.drips.length < MAX_DRIPS) {
        this.acc[i] = 0.25;
        this.dripsSpawned++;
        const vy = 0.5 + Math.random() * 0.9;
        this.drips.push({
          x: x + (Math.random() - 0.5) * R * 0.6,
          y: y + R * 0.5,
          vy,
          w: 1.4 + R * 0.12,
          life: Math.round((25 + Math.random() * 60) / vy),
          color: this.cfg.color,
        });
      }
    }

    stepDrips() {
      if (!this.drips.length) return;
      const ctx = this.ctx;
      ctx.lineCap = 'round';
      for (let i = this.drips.length - 1; i >= 0; i--) {
        const dp = this.drips[i];
        ctx.globalAlpha = 0.85;
        ctx.strokeStyle = dp.color;
        ctx.lineWidth = dp.w;
        ctx.beginPath();
        ctx.moveTo(dp.x, dp.y);
        dp.y += dp.vy;
        ctx.lineTo(dp.x, dp.y);
        ctx.stroke();
        dp.vy *= 0.99;
        dp.w *= 0.996;
        if (--dp.life <= 0) {
          ctx.fillStyle = dp.color;
          ctx.beginPath();
          ctx.arc(dp.x, dp.y + dp.w * 0.4, dp.w * 0.8, 0, Math.PI * 2);
          ctx.fill();
          this.drips.splice(i, 1);
        }
      }
      ctx.globalAlpha = 1;
    }

    // ------------------------------------------------------------------ undo (snapshots)

    snapshot() {
      const c = document.createElement('canvas');
      c.width = this.S;
      c.height = this.H;
      c.getContext('2d').drawImage(this.canvas, 0, 0);
      return c;
    }

    restore(c) {
      this.ctx.clearRect(0, 0, this.S, this.H);
      this.ctx.drawImage(c, 0, 0);
    }

    pushUndo() {
      this.undoStack.push(this.snapshot());
      if (this.undoStack.length > UNDO_STEPS) this.undoStack.shift();
      this.redoStack.length = 0;
    }
  }

  return { Engine, catmull };
})();
