# CLAUDE.md — TAG-School (PIECEBOOK)

Contexto específico de este repo. Los estándares genéricos (estilo, tests, git, seguridad,
delegación en agentes) viven en `~/.claude/CLAUDE.md` y `team-claude/rules/` — no se repiten aquí.

## Qué es

Juego móvil (iOS/Android) que enseña graffiti real. Este repo contiene **Fase 0: dos spikes
técnicos de validación**, no el juego completo:

- **Spike A — Drawing Engine** (`unity/Assets/_Project/DrawingEngine/`): motor de spray sobre
  RenderTexture, foco del `README.md`. Veredicto en `INFORME-GO-NOGO.md`.
- **Spike B — CitySim** (`unity/Assets/_Project/CitySim/`): loop de "bombing" — pintar mientras
  evitas patrullas con IA/FSM. **No aparece en el README** (desactualizado); documentado en
  `docs/SPIKE-B-REPORT.md`.

Fuente de verdad de arquitectura: `docs/ARQUITECTURA.md` (visión §1, stack §2, módulos §3,
contrato del Drawing Engine §4.3, estructura de carpetas §8).

## Stack

Unity **2022.3.40f1 LTS** · URP 14.0.11 · Input System 1.7.0 · C# / .NET Standard 2.1.
Sin build system externo (no package.json/Makefile) — todo se abre/compila/testea desde el
editor Unity.

## Abrir y ejecutar

1. Unity Hub → Add → carpeta `unity/` → abrir con 2022.3.40f1 (o cualquier parche 2022.3.x).
2. Spike A: menú `TAG-School ▸ Setup Phase 0 Spike` — genera ScriptableObjects de caps/paint,
   registra el shader `PieceBook/SprayStamp` en Always Included Shaders, construye
   `SpraySpike.unity` y la añade a Build Settings.
3. Spike B: setup equivalente en `CitySim/Editor/SpikeBSetup.cs` (genera `PatrolDef`/`ZoneDef`).
4. Play. Atajo Spike A: añadir el componente `SprayDemoBootstrap` a un GameObject en una escena
   vacía — se auto-ensambla (cámara, pared, canvas, input, HUD).

Si Unity pregunta por el backend de input al abrir, elegir **Both** o **Input System** (la
presión del Apple Pencil solo llega vía Input System).

## Tests

Unity Test Framework, no hay CLI/CI en el repo:
- `unity/Tests/EditMode/DrawingEngineEditTests.cs` — Window ▸ General ▸ Test Runner ▸ EditMode.
- `unity/Tests/PlayMode/` — solo contiene un README, sin tests reales aún.

## Arquitectura de módulos

Un Assembly Definition (`.asmdef`) por carpeta bajo `unity/Assets/_Project/`. Regla de
dependencia (ARQUITECTURA §3): todos pueden depender de `Core`; **los módulos nunca se
referencian entre sí directamente** — solo vía `Core.Events.EventBus` (pub/sub tipado,
eventos como `struct` que implementan `IEvent`, cero allocs por publish).

- `Core/` (`PieceBook.Core`) — `EventBus`, `DrawingEvents`.
- `DrawingEngine/` (`PieceBook.DrawingEngine`) — motor puro, sin UI ni lecciones.
  - `Api/IDrawingCanvas.cs` — **contrato público exacto** (copiado de ARQUITECTURA §4.3). No
    añadir conceptos de UI/lecciones aquí; extender vía eventos en Core en su lugar.
    Convención de coordenadas: `UpdateStroke(pos, ...)` usa espacio normalizado [0,1],
    origen (0,0) = esquina inferior-izquierda, independiente del tamaño real de la RT.
  - `Canvas/DrawingCanvas.cs` — RT 2048², capas, undo, Flatten.
  - `Stamping/GpuStamper.cs` — `CommandBuffer` + `DrawMeshInstanced`. **Nunca `SetPixels`**.
  - `Stamping/SprayEmitter.cs` — trazo→partículas; distancia de boquilla → cono/borde/mist.
  - `Interpolation/CatmullRom.cs`, `Pooling/ObjectPool.cs`, `Drips/DripSystem.cs`.
  - `Input/StrokeInputController.cs` — pointer→UV.
- `CitySim/` (`PieceBook.CitySim`, Spike B) — `AI/PatrolAgent.cs` (FSM Calma/Sospecha/
  Persecución), `Graph/StreetGraph.cs`, `Paint/FirstPersonPaint.cs`, `UI/CitySimHud.cs`.
- `Spike/` (`PieceBook.Spike`) — arnés de demo de Spike A (`SprayDemoBootstrap`, `DemoHud`).
  **Se borra en Fase 1** — no añadir lógica de producto aquí.

## Gotchas específicos de este repo

- El **README.md está desactualizado**: su "Mapa del código" solo describe Spike A y no
  menciona `CitySim/` en absoluto. No asumir que el README refleja el estado real del repo.
- Hot path sin allocs: `SprayEmitter`, `DripSystem`, `StrokeInputController` deben mantenerse
  a 0 B/frame de GC alloc en trazo sostenido (Profiler → columna GC Alloc en Scripts). Usar
  pools/buffers preasignados, no asumir que "funciona" sin comprobar el Profiler.
- Trazo espejado en vertical en alguna plataforma → invertir `pos.y` en
  `StrokeInputController` (origen UV de la RT).
- Undo actual = ring buffer de snapshots completos de la RT (N=8), a propósito simplificado
  para el spike; producción usará tiles comprimidos (§4.1 ARQUITECTURA). No "arreglarlo" sin
  saber que es una limitación intencional documentada.
- El stamping es pipeline-agnostic (va directo a la RT vía CommandBuffer): funciona con URP o
  Built-in. Para forzar URP hace falta asignar un URP Asset en Project Settings ▸ Graphics.
- Convención de aislamiento por spike: los commits de Spike B no deben tocar nada fuera de
  `CitySim/**` + `docs/SPIKE-B-REPORT.md` (y viceversa para Spike A) — mantener esa separación
  al modificar cualquiera de los dos.
- Objetivo de aceptación (no genérico, es la métrica de este proyecto): latencia
  input→píxel < 30ms (ideal 16ms) y 60fps sostenidos dibujando; medir con el HUD en pantalla
  (`FrameStats`, `LatencyProbe`) + Profiler, y anotar en `INFORME-GO-NOGO.md §3` /
  `docs/SPIKE-B-REPORT.md`.
