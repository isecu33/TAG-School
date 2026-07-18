# Informe Fase 0 — GO / NO-GO · Drawing Engine

**Proyecto:** TAG-School (PIECEBOOK) · **Autor:** BLUEPRINT · **Fecha:** 2026-07-18
**Alcance:** validar latencia y feel del spray del motor de pintura. Criterios §4.2:
latencia input→píxel **< 30 ms** (ideal 16) y **60 fps** sostenidos dibujando.

---

## 1. Recomendación

> ## ✅ GO (provisional, condicionado a la medición en dispositivo)

El diseño no tiene ningún riesgo estructural que ponga en duda los presupuestos: el camino
crítico (input → interpolación → stamping instanciado por GPU → RT) es el mismo que usan los
brush engines móviles que sí cumplen 60 fps / <30 ms, y se ha implementado evitando los tres
asesinos clásicos de rendimiento (SetPixels, allocs por frame, y draw-calls por partícula).
Queda **una** acción obligatoria antes de cerrar el GO: **capturar las cifras reales en un
iPhone 11 / Android gama media** (dispositivo mínimo objetivo, §2) y pegarlas en la tabla §3.

**Por qué provisional y no un GO firmado:** este entorno de construcción no ejecuta el editor
de Unity ni el Profiler, así que **no hay números medidos por mí**. Sería deshonesto reportar
un fps o una latencia inventados. El motor y el instrumental de medición están listos; la
medición la ejecuta el equipo con el proyecto abierto (pasos en §2 del README). Si las cifras
caen dentro de presupuesto — que es lo esperado por el análisis de §4 — el GO queda firme.

---

## 2. Qué se entrega (verificable)

| Item | Estado | Evidencia |
|---|---|---|
| ENG-01 · Canvas RenderTexture 2048² | ✅ | `DrawingCanvas.cs` crea RT ARGB32 2048² con capas |
| ENG-01 · Stamping por GPU (CommandBuffer/DrawMeshInstanced) | ✅ | `GpuStamper.cs` — **cero `SetPixels`** en todo el repo |
| ENG-01 · Input táctil + presión | ✅ | `StrokeInputController.cs` (Pen/Touch/Mouse, Input System) |
| ENG-02 · StrokeConfig + 3 CapDef + PaintDef (SO) | ✅ | `Config/*`, `SpikeDefaults`, assets en `Content/` |
| ENG-02 · Distancia de boquilla en vivo → cono/borde/overspray | ✅ | slider HUD → `SprayEmitter.CoreRadius`/hardness/mist |
| ENG-03 · Interpolación Catmull-Rom | ✅ | `Interpolation/CatmullRom.cs` (+ test) |
| ENG-03 · Object pool, cero GC por trazo | ✅ | `ObjectPool`, buffers preasignados en `GpuStamper`/`SprayEmitter` |
| ENG-04 · Drips procedurales sobre dripThreshold | ✅ | `Drips/DripSystem.cs` + `PaintAccumulator.cs` |
| Escena de prueba (pared + 3 caps + slider) | ✅ | `SprayDemoBootstrap` + `DemoHud` |
| API pública §4.3 exacta | ✅ | `IDrawingCanvas.cs` (firma literal) |
| Instrumentación de métricas | ✅ | `FrameStats` (fps) + `LatencyProbe` (input→submit) en HUD |

---

## 3. Métricas — TABLA A RELLENAR EN DISPOSITIVO

Rellena tras `Play`/build + Profiler (ver README §"Cómo medir"). Dispositivo objetivo: iPhone 11
o Android Snapdragon 720G / 4 GB.

| Métrica | Objetivo (§4.2) | Editor (PC) | iPhone 11 | Android gama media | ¿Pasa? |
|---|---|---|---|---|---|
| FPS dibujando (media) | 60 | | | | |
| FPS dibujando (peor frame) | ≥ ~55 | | | | |
| Latencia input→submit (media) | < 30 ms | | | | |
| Latencia input→submit (peor) | < 30 ms | | | | |
| Latencia end-to-end (cámara 240fps)¹ | < 30 ms ideal 16 | | | | |
| GC Alloc por trazo (Scripts) | 0 B/frame | | | | |
| Memoria total | < 1 GB | | | | |

¹ La sonda del motor mide input→submit; la latencia percibida añade muestreo del SO + escaneo
del panel. Mide el end-to-end con captura de pantalla a alta velocidad para el dato "real".

---

## 4. Análisis de riesgo (por qué se espera cumplir)

- **Latencia.** El input se captura en `Update` y se estampa en `LateUpdate` del **mismo
  frame** (no hay frame extra de cola). El coste por muestra es aritmético (Catmull-Rom +
  matrices) y un `DrawMeshInstanced`. A 60 fps el frame dura 16.6 ms; el trabajo de stamping
  cabe muy por debajo, dejando margen sobre el presupuesto de 30 ms. Riesgo: **bajo**.
- **FPS / fill-rate.** El coste dominante es el fill-rate de las partículas (overdraw sobre la
  RT), no la CPU. Se acota con: pocas partículas por muestra (density·0.25 core + overspray
  proporcional a la distancia), un único shader unlit con blend simple, y batching instanciado
  (hasta 1023/draw). Palanca de ajuste inmediata si un dispositivo sufre: bajar `density`/
  `overspray` en los CapDef (data-driven, sin recompilar). Riesgo: **medio-bajo**, mitigable
  desde datos.
- **GC.** Buffers `Matrix4x4[1023]`/`Vector4[1023]`/`float[1023]` reutilizados, pools
  prewarmed para drips, listas con capacidad reservada. Objetivo 0 allocs por frame en trazo.
  Riesgo: **bajo** (verificable en Profiler; hay test EditMode del pool).
- **Memoria.** RT 2048²·RGBA32 = 16 MB/capa; undo ring N=8 = 128 MB (snapshots completos).
  Cómodo bajo 1 GB en el spike, pero **producción debe** pasar a snapshots por tiles (§4.1)
  para escalar capas y iPad 4096². Riesgo: **conocido y planificado**, fuera de alcance Fase 0.

---

## 5. Discrepancia con el brief (resuelta)

El brief (ENG-02) lista `dripThreshold` "en los CapDef". La fuente de verdad §9 lo asigna a
**`PaintDef`** (los drips son propiedad de la pintura/material, no del cap). Se ha seguido §9:
`dripThreshold` vive en `PaintDef`; `CapDef` mantiene exactamente los campos de §9
`{id, name, coneAngle, density, falloff, flowRate, overspray, unlockCost}`. El efecto para el
spike es idéntico (el `StrokeConfig` combina cap + paint), y respeta el contrato de datos.

---

## 6. Recomendaciones para Fase 1 (si GO firme)

1. Sustituir undo por snapshots por tiles comprimidos (memoria + iPad 4096²).
2. Compositing multicapa real (boceto/fill/outline/detalles, §4.1) para `Flatten`/display.
3. Estrategia `Strategy` de herramientas (marker, roller) sobre el mismo stamper (§7).
4. Export del `StrokeRecording` a MP4 (re-render offline, §4.1) — ya se graban los inputs.
5. Borrar el assembly `PieceBook.Spike` y mover la UI al módulo `UI` (MVP, §7).
