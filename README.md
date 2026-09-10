# TAG-School — *learn how to graffiti*

Juego iOS/Android que enseña graffiti real. Este repositorio arranca por la **Fase 0
(spike GO/NO-GO)**: un prototipo del **Drawing Engine** en Unity para validar **latencia** y
**feel** del spray antes de comprometer la Fase 1.

- **Fuente de verdad:** [`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md). El motor implementa §4,
  respeta la API pública de §4.3 y la estructura de módulos/asmdefs de §8.
- **Proyecto Unity:** [`/unity6`](unity6) (Unity 6.0.6.0f1 · URP). La estructura interna sigue §8
  (`unity6/Assets/_Project/...`, `unity6/Assets/Tests/...`).
- **Veredicto y métricas:** [`INFORME-GO-NOGO.md`](INFORME-GO-NOGO.md).

Alcance Fase 0: solo el Drawing Engine (ENG-01…ENG-04). **No** incluye lecciones, RA, backend
ni arte final.

- **Planes de las fases siguientes:** [`docs/planes/`](docs/planes/) descompone las Fases 1-4 del
  roadmap (`ARQUITECTURA §11`) en tareas codificadas y verificables. La forma de delegarlas está
  en [`docs/GUIA-AGENTES.md`](docs/GUIA-AGENTES.md) (regla de oro de §12).

---

## Checklist Fase 0 (ENG-01 → ENG-04)

- [x] **ENG-01** · Canvas sobre RenderTexture 2048² con stamping por GPU
      (`CommandBuffer` / `DrawMeshInstanced`, **nunca `SetPixels`**)
- [x] **ENG-01** · Input táctil + soporte de presión (Apple Pencil / touch / mouse)
- [x] **ENG-02** · `StrokeConfig` + 3 `CapDef` (skinny 4° / soft 12° / fat 25°) + `PaintDef` como ScriptableObjects
- [x] **ENG-02** · Slider de distancia de boquilla que modifica cono/borde/overspray en vivo
- [x] **ENG-03** · Interpolación Catmull-Rom del input
- [x] **ENG-03** · Object pool de partículas — **cero GC allocs durante un trazo**
- [x] **ENG-04** · Drips procedurales al superar `dripThreshold`
- [x] Escena de prueba (pared texturizada + selector de 3 caps + slider de distancia)
- [x] API pública `IDrawingCanvas` exacta de §4.3
- [x] Instrumentación de aceptación en pantalla: latencia input→submit y FPS
- [x] Informe GO/NO-GO
- [ ] **Métricas medidas en dispositivo** (iPhone 11 / Android gama media) — pendiente de
      hardware; ver tabla en `INFORME-GO-NOGO.md §3`

> La única casilla abierta requiere ejecutar el Profiler en dispositivo físico, que no puede
> hacerse en el entorno de construcción. El motor y el instrumental de medición están listos.

---

## Requisitos

- **Unity 6.0.6.0f1** (versión fijada en `unity6/ProjectSettings/ProjectVersion.txt`).
- Módulos de build **iOS** y/o **Android** para desplegar en dispositivo.
- Paquetes (declarados en `unity6/Packages/manifest.json`): URP 17, **Input System** 1.20, uGUI,
  Test Framework. Se resuelven al abrir.

Al abrir por primera vez, si Unity pregunta por el backend de input, elige **Both** o
**Input System** (el código soporta ambos; la presión del Pencil solo llega vía Input System).

---

## Cómo abrir y probar (editor)

1. Unity Hub → **Add** → selecciona la carpeta **`unity6/`** de este repo. Ábrela con Unity 6.0.6.0f1.
2. Menú **`TAG-School ▸ Setup Phase 0 Spike`**. Esto:
  - genera los ScriptableObjects de `unity6/Assets/_Project/Content` (3 caps + paint),
   - registra el shader `PieceBook/SprayStamp` en *Always Included Shaders* (para builds),
  - construye y guarda la escena `unity6/Assets/_Project/Spike/Scenes/SpraySpike.unity` y la añade a Build Settings.
3. Pulsa **Play**. Pinta sobre la pared con ratón (editor) o dedo/Pencil (dispositivo).

> Atajo: crea una escena vacía, añade el componente **`SprayDemoBootstrap`** a un GameObject y
> dale a Play — se auto-ensambla (cámara, pared, canvas, input y HUD) y crea caps por defecto.

### Controles del HUD (placeholder)

- **Skinny / Soft / Fat** — cap (cono 4° / 12° / 25°).
- **Distancia de boquilla** (slider) — LA mecánica: a más distancia, cono más ancho, borde más
  suave y más overspray, en vivo (§4.1).
- **Clear / Undo / Redo** y swatches de color.
- Lectura arriba-izquierda: **FPS**, **latencia input→submit (ms)**, **drips** y **stamps/frame**.

---

## Cómo probar en dispositivo

### iOS
1. `File ▸ Build Settings ▸ iOS ▸ Switch Platform`.
2. Asegura la escena `SpraySpike` en la lista (el menú de setup ya la añade).
3. `Build` → abre el proyecto Xcode → firma con tu equipo → *Run* en iPhone/iPad. iPad + Apple
   Pencil da la presión real (presión → distancia de boquilla).

### Android
1. `File ▸ Build Settings ▸ Android ▸ Switch Platform`.
2. `Build And Run` con un dispositivo en modo desarrollador (o genera el APK).

---

## Cómo medir las métricas (criterios §4.2)

**Objetivo:** latencia input→píxel **< 30 ms** (ideal 16) y **60 fps** dibujando.

- **FPS**: HUD (media + peor frame) y `Window ▸ Analysis ▸ Profiler` dibujando un trazo continuo.
- **Latencia**: `LatencyProbe` mide input→submit del CommandBuffer (el tramo que controla el
  motor). Para el end-to-end real, graba la pantalla a alta velocidad (240 fps) y cuenta frames.
- **GC / allocs por trazo**: Profiler → columna *GC Alloc* en *Scripts*; debe ser 0 B/frame en
  trazo sostenido (buffers preasignados + pools).

Anota las cifras en `INFORME-GO-NOGO.md §3` (tabla lista para rellenar).

---

## Mapa del código

```
docs/ARQUITECTURA.md            fuente de verdad (§4 motor, §4.3 API, §8 estructura)
INFORME-GO-NOGO.md              veredicto + métricas
unity6/
  Assets/_Project/
    Core/                       PieceBook.Core        EventBus tipado + eventos
    DrawingEngine/              PieceBook.DrawingEngine (API PURA, sin UI)
      Api/IDrawingCanvas.cs     contrato EXACTO §4.3
      Config/CapDef, PaintDef   ScriptableObjects (§9)
      Canvas/DrawingCanvas.cs   RT 2048², capas, undo, Flatten, recording   [ENG-01]
      Stamping/GpuStamper.cs    CommandBuffer + DrawMeshInstanced            [ENG-01]
      Stamping/SprayEmitter.cs  trazo→partículas; distancia→cono/borde/mist  [ENG-02]
      Interpolation/CatmullRom  suavizado del input                          [ENG-03]
      Pooling/ObjectPool.cs     pool prewarmed cero-alloc                    [ENG-03]
      Drips/DripSystem.cs       drips procedurales                           [ENG-04]
      Input/StrokeInputController  pointer→UV; Pencil/touch/mouse
      Diagnostics/              FrameStats + LatencyProbe
    Content/                    caps + paint (generados)
    Spike/                      PieceBook.Spike (harness; borrar en Fase 1)
  Assets/Tests/EditMode/        pooling, Catmull-Rom, accumulator, EventBus
```

## Limitaciones conocidas (spike, a propósito)

- **Undo** = ring buffer de snapshots RT completos (N=8); producción usa tiles comprimidos (§4.1).
- **Flatten**/display componen 1 capa (la activa); multicapa es Fase 1.
- **URP**: el stamping va por CommandBuffer directo a la RT → *pipeline-agnostic* (URP o
  Built-in). Para forzar URP, crea/assigna un URP Asset en *Project Settings ▸ Graphics*.
- Sin arte final: la pared es una textura de ladrillo generada por código.
- Si en alguna plataforma el trazo sale espejado en vertical, invierte `pos.y` en
  `StrokeInputController` (origen UV de la RT).
