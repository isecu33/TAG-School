# TAG-School — Drawing Engine Spike (Fase 0)

Prototipo del **Drawing Engine** de *PIECEBOOK / TAG-School* para validar **latencia** y
**feel** del spray antes de comprometer la Fase 1. Es un spike GO/NO-GO: motor de pintura
real (stamping por GPU, caps data-driven, drips procedurales) + una escena de prueba con
instrumentación de métricas. **No** incluye lecciones, RA, backend ni arte final.

> Fuente de verdad: `ARQUITECTURA.md`. Este repo implementa §4 (Drawing Engine), respeta la
> API pública de §4.3 y la estructura de §8. Ver `Assets/_Project/MODULES.md`.

---

## Requisitos

- **Unity 2022.3 LTS** (probado contra 2022.3.40f1; cualquier parche 2022.3.x sirve).
- Módulos de build **iOS** y/o **Android** si vas a desplegar en dispositivo.
- Paquetes (ya declarados en `Packages/manifest.json`, se resuelven al abrir):
  URP 14, **Input System** 1.7, uGUI, Test Framework.

Al abrir por primera vez, si Unity pregunta por el backend de input, elige **Both** o
**Input System** (el código soporta ambos, pero el Pencil/presión solo llega vía Input System).

---

## Cómo abrir y probar (editor)

1. Abre el proyecto con Unity 2022.3 LTS (Unity Hub → *Add* → esta carpeta).
2. Menú **`TAG-School ▸ Setup Phase 0 Spike`**. Esto:
   - genera los ScriptableObjects de `_Project/Content` (3 caps + paint),
   - registra el shader `PieceBook/SprayStamp` en *Always Included Shaders* (para builds),
   - construye y guarda la escena `Assets/_Project/Spike/Scenes/SpraySpike.unity` y la mete en Build Settings.
3. Pulsa **Play**. Pinta sobre la pared con ratón (editor) o dedo/Pencil (dispositivo).

> Atajo total: también puedes crear una escena vacía, añadir el componente
> **`SprayDemoBootstrap`** a un GameObject y darle a Play — se auto-ensambla (cámara, pared,
> canvas, input y HUD) y crea caps por defecto si no hay assets.

### Controles del HUD (placeholder)

- **Skinny / Soft / Fat** — selecciona el cap (cono 4° / 12° / 25°).
- **Distancia de boquilla** (slider) — LA mecánica: a más distancia, cono más ancho, borde
  más suave y más overspray, en vivo (§4.1).
- **Clear / Undo / Redo** y swatches de color.
- Lectura arriba-izquierda: **FPS**, **latencia input→submit (ms)**, **drips activos** y
  **stamps/frame**.

---

## Cómo probar en dispositivo

### iOS
1. `File ▸ Build Settings ▸ iOS ▸ Switch Platform`.
2. Asegura la escena `SpraySpike` en la lista (el menú de setup ya la añade).
3. `Build` → abre el proyecto Xcode → firma con tu equipo → *Run* en un iPhone/iPad.
   iPad + Apple Pencil es la experiencia con presión real (presión → distancia de boquilla).

### Android
1. `File ▸ Build Settings ▸ Android ▸ Switch Platform`.
2. `Build And Run` con un dispositivo en modo desarrollador (o genera el APK).

En dispositivo, el HUD muestra las métricas en tiempo real; para el número oficial usa el
Profiler (abajo).

---

## Cómo medir las métricas (criterios de aceptación §4.2)

**Objetivo:** latencia input→píxel **< 30 ms** (ideal 16) y **60 fps** sostenidos dibujando.

- **FPS**: el HUD muestra media y peor frame. Para la cifra sólida, `Window ▸ Analysis ▸
  Profiler`, categoría *Rendering*/*Scripts*, dibujando un trazo continuo.
- **Latencia**: `LatencyProbe` mide el tramo que controla el motor (input recibido → submit
  del CommandBuffer a la GPU). **No** es la latencia motion-to-photon completa (falta muestreo
  del SO + escaneo del panel). Para el número end-to-end real, graba la pantalla a alta
  velocidad (240 fps) tocando y cuenta frames hasta que aparece el píxel.
- **GC / allocs por trazo**: Profiler → *Memory* / columna *GC Alloc* en *Scripts*. Durante un
  trazo sostenido, `SprayEmitter`/`GpuStamper`/pools no deben generar allocs (buffers
  preasignados; ver ENG-03).

Anota tus cifras en `INFORME-GO-NOGO.md` (tiene una tabla lista para rellenar).

---

## Mapa del código (qué implementa qué)

```
Assets/_Project/
  Core/                         PieceBook.Core
    Events/EventBus.cs          pub/sub tipado, sin allocs (§3, §7)
    Events/DrawingEvents.cs     StrokeStarted / StrokeCompleted / DripSpawned
  DrawingEngine/                PieceBook.DrawingEngine  (API PURA, sin UI)
    Api/IDrawingCanvas.cs       contrato EXACTO §4.3
    Api/LayerId, StrokeConfig, StrokeRecording
    Config/CapDef.cs            ScriptableObject (§9)
    Config/PaintDef.cs          ScriptableObject con dripThreshold (§9)
    Canvas/DrawingCanvas.cs     RenderTexture 2048², capas, undo ring, Flatten, recording  [ENG-01]
    Stamping/GpuStamper.cs      CommandBuffer + DrawMeshInstanced (NUNCA SetPixels)         [ENG-01]
    Stamping/SprayEmitter.cs    trazo→partículas; distancia→cono/borde/overspray            [ENG-02]
    Shaders/SprayStamp.shader   dot instanciado con falloff suave
    Interpolation/CatmullRom.cs suavizado del input                                          [ENG-03]
    Pooling/ObjectPool.cs       pool prewarmed cero-alloc                                    [ENG-03]
    Drips/DripSystem.cs         drips procedurales sobre dripThreshold                       [ENG-04]
    Input/StrokeInputController pointer→UV por raycast; Pencil/touch/mouse
    Diagnostics/FrameStats, LatencyProbe
  Content/                      caps + paint (generados)
  Spike/                        PieceBook.Spike  (harness; borrar en Fase 1)
    SprayDemoBootstrap.cs       ensambla la escena en código
    DemoHud.cs                  UI placeholder + lecturas de métricas
    Editor/SpikeSetup.cs        menú TAG-School (assets + shader + escena)
Tests/EditMode/                 pruebas de pooling, Catmull-Rom, accumulator, EventBus
```

## Limitaciones conocidas (spike, a propósito)

- **Undo** = ring buffer de snapshots RT completos (N=8). Producción usa snapshots por tiles
  comprimidos (§4.1); aquí prioriza simplicidad sobre memoria.
- **Flatten** y display componen 1 capa (la activa). El compositing multicapa es Fase 1.
- **URP**: el stamping va por CommandBuffer directo a la RT, así que es *pipeline-agnostic*
  (funciona bajo URP o Built-in). Si tu proyecto no tiene un URP Asset asignado, la escena
  igual corre; para forzar URP, crea/assigna un URP Asset en *Project Settings ▸ Graphics*.
- Sin arte final: la pared es una textura de ladrillo generada por código.
- Si en alguna plataforma el trazo sale espejado en vertical, invierte `pos.y` en
  `StrokeInputController` (diferencia de origen UV de la RT).

Ver el veredicto y las métricas en **`INFORME-GO-NOGO.md`**.
