// ---------------------------------------------------------------------------------------------
// Walls — generador procedural de paredes (ladrillo, hormigón, persiana, vagón de tren).
// Lo usa la demo y también tools/export-assets.mjs para escribir los PNG de Unity
// (unity6/Assets/_Project/Art/Textures/Walls). Determinista: misma semilla → misma pared.
// ---------------------------------------------------------------------------------------------

const Walls = (() => {
  function rng(seed) {
    let s = seed >>> 0 || 1;
    return () => ((s = (s * 1664525 + 1013904223) >>> 0) / 4294967296);
  }

  // Grano fino: ruido por píxel para que nada se vea plano.
  function grain(ctx, size, amount, rand) {
    const img = ctx.getImageData(0, 0, size, size);
    const d = img.data;
    for (let i = 0; i < d.length; i += 4) {
      const n = (rand() - 0.5) * amount;
      d[i] += n; d[i + 1] += n; d[i + 2] += n;
    }
    ctx.putImageData(img, 0, 0);
  }

  function blotches(ctx, size, rand, count, color, maxR, alpha) {
    for (let i = 0; i < count; i++) {
      const x = rand() * size, y = rand() * size, r = maxR * (0.3 + rand());
      const g = ctx.createRadialGradient(x, y, 0, x, y, r);
      g.addColorStop(0, color.replace('A', String(alpha * rand())));
      g.addColorStop(1, color.replace('A', '0'));
      ctx.fillStyle = g;
      ctx.fillRect(x - r, y - r, r * 2, r * 2);
    }
  }

  function brick(ctx, size, rand) {
    ctx.fillStyle = '#b9b2a6';
    ctx.fillRect(0, 0, size, size);
    const rows = 16, bh = size / rows, bw = bh * 2.6, gap = Math.max(2, size / 220);
    for (let r = 0; r < rows; r++) {
      const off = r % 2 ? -bw / 2 : 0;
      for (let x = off; x < size; x += bw) {
        const base = [150 + rand() * 40, 70 + rand() * 25, 52 + rand() * 18];
        ctx.fillStyle = `rgb(${base[0] | 0},${base[1] | 0},${base[2] | 0})`;
        ctx.fillRect(x + gap / 2, r * bh + gap / 2, bw - gap, bh - gap);
        ctx.fillStyle = 'rgba(0,0,0,0.12)';
        ctx.fillRect(x + gap / 2, r * bh + bh - gap * 1.5, bw - gap, gap);
      }
    }
    blotches(ctx, size, rand, 26, 'rgba(30,24,20,A)', size * 0.12, 0.25);
    grain(ctx, size, 26, rand);
  }

  function concrete(ctx, size, rand) {
    ctx.fillStyle = '#8d8a84';
    ctx.fillRect(0, 0, size, size);
    blotches(ctx, size, rand, 60, 'rgba(60,58,54,A)', size * 0.18, 0.22);
    blotches(ctx, size, rand, 40, 'rgba(190,186,178,A)', size * 0.14, 0.18);
    // juntas de encofrado
    ctx.strokeStyle = 'rgba(40,38,35,0.35)';
    ctx.lineWidth = Math.max(1, size / 400);
    for (let k = 1; k < 3; k++) {
      ctx.beginPath(); ctx.moveTo(0, (size * k) / 3); ctx.lineTo(size, (size * k) / 3); ctx.stroke();
    }
    // poros
    for (let i = 0; i < size * 1.2; i++) {
      ctx.fillStyle = `rgba(30,28,26,${0.15 + rand() * 0.35})`;
      const r = rand() * size * 0.0025 + 0.5;
      ctx.beginPath(); ctx.arc(rand() * size, rand() * size, r, 0, Math.PI * 2); ctx.fill();
    }
    grain(ctx, size, 34, rand);
  }

  function shutter(ctx, size, rand) {
    const ribs = 22, h = size / ribs;
    for (let i = 0; i < ribs; i++) {
      const g = ctx.createLinearGradient(0, i * h, 0, i * h + h);
      g.addColorStop(0, '#9aa3a8');
      g.addColorStop(0.45, '#c8ced1');
      g.addColorStop(0.55, '#7f888d');
      g.addColorStop(1, '#5d6569');
      ctx.fillStyle = g;
      ctx.fillRect(0, i * h, size, h);
    }
    blotches(ctx, size, rand, 30, 'rgba(110,80,50,A)', size * 0.08, 0.35); // óxido
    grain(ctx, size, 18, rand);
  }

  function train(ctx, size, rand) {
    ctx.fillStyle = '#b8bcbf';
    ctx.fillRect(0, 0, size, size);
    const g = ctx.createLinearGradient(0, 0, 0, size);
    g.addColorStop(0, 'rgba(255,255,255,0.18)');
    g.addColorStop(0.5, 'rgba(0,0,0,0)');
    g.addColorStop(1, 'rgba(0,0,0,0.2)');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, size, size);
    // paneles + remaches
    const cols = 3, pw = size / cols;
    ctx.strokeStyle = 'rgba(40,44,48,0.55)';
    ctx.lineWidth = Math.max(1.5, size / 300);
    for (let c = 1; c < cols; c++) { ctx.beginPath(); ctx.moveTo(c * pw, 0); ctx.lineTo(c * pw, size); ctx.stroke(); }
    ctx.fillStyle = 'rgba(60,64,68,0.7)';
    const inset = size * 0.012;
    const rivetXs = [];
    for (let c = 0; c <= cols; c++) {
      if (c > 0) rivetXs.push(c * pw - inset);
      if (c < cols) rivetXs.push(c * pw + inset);
    }
    for (const x of rivetXs) {
      for (let y = size * 0.03; y < size; y += size / 24) {
        ctx.beginPath(); ctx.arc(x, y, size * 0.004, 0, Math.PI * 2); ctx.fill();
      }
    }
    // franja de ventanas abajo y suciedad
    ctx.fillStyle = 'rgba(70,74,78,0.35)';
    ctx.fillRect(0, size * 0.9, size, size * 0.1);
    blotches(ctx, size, rand, 24, 'rgba(70,60,50,A)', size * 0.1, 0.2);
    grain(ctx, size, 16, rand);
  }

  const KINDS = {
    brick: { name: 'Ladrillo', paint: brick, seed: 11 },
    concrete: { name: 'Hormigón', paint: concrete, seed: 23 },
    shutter: { name: 'Persiana', paint: shutter, seed: 37 },
    train: { name: 'Vagón', paint: train, seed: 41 },
  };

  function paint(ctx, kind, size) {
    const k = KINDS[kind] || KINDS.concrete;
    ctx.save();
    k.paint(ctx, size, rng(k.seed));
    ctx.restore();
  }

  return { KINDS, paint };
})();
