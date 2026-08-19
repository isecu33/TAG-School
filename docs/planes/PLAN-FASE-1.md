# PLAN · Fase 1 — Vertical slice

**Autor:** BLUEPRINT · **Roadmap:** `ARQUITECTURA §11` (semanas 3-8) ·
**Convenciones:** [`README.md`](README.md) · [`../GUIA-AGENTES.md`](../GUIA-AGENTES.md)

## Objetivo (criterio de fase)

El **Capítulo 1 se juega de principio a fin en dispositivo**: el jugador abre la app, entra en
la lección `lesson_tag_01`, completa los pasos (showcase → trace → freeform → quiz),
recibe coronas, **desbloquea** el siguiente ítem, y su progreso **sobrevive a cerrar la app**.
Con **3 caps**, **1 alfabeto handstyle**, **sonido + haptics**, sin RA ni backend.

**Entra en alcance:** módulos `Core` (Save/Audio/Haptics/DI), `Lessons`, `UI`, `MetaGame`
(mínimo), endurecimiento del `DrawingEngine` que el slice necesita, y contenido del Cap. 1.
**Fuera de alcance:** export/share (Fase 2), glosario/blackbook completos (Fase 2), RA (Fase 3),
Firebase (Fase 3).

## Hitos y camino crítico

```
CORE-01 DI ─┬─ ENG-05 multicapa ─ ENG-06 undo-tiles ─ ENG-07 tool-strategy
            ├─ CORE-02 SaveService ── META-01 Progress ── META-02 Unlocks
            ├─ CORE-03 AudioService ─┐
            └─ CORE-04 Haptics ──────┴─ (enganchan por EventBus, no bloquean)
LES-01 modelo ─ LES-02 runner ─ LES-03 evaluador ─ LES-04 quiz ─ LES-05 pasos-trace/freeform
UI-01 shell ─ UI-02 lesson-view ─ UI-03 paint-view ─ UI-04 review-view
CNT-01 caps ─ CNT-02 alfabeto ─ CNT-03 5 lecciones
```
🔴 **Camino crítico:** `CORE-01 → ENG-05 → LES-02 → UI-03 → CNT-03 → SLICE-01`.

---

## Core / servicios

### `CORE-01` · Contenedor DI (VContainer) 🔴
*módulo:* `Assets/_Project/Core` · *depende de:* —
**Qué:** introducir VContainer como Service Locator real (`§7`) con un `LifetimeScope` raíz que
registra los servicios core como interfaces (`ISaveService`, `IAudioService`, `IHaptics`).
**Hecho cuando:** un test EditMode resuelve `ISaveService` desde el contenedor y obtiene una
instancia no nula; ningún servicio core se accede por `static`/singleton (grep sin
`Instance =>`).

### `CORE-02` · SaveService (SQLite, offline-first)
*módulo:* `Assets/_Project/Core` · *depende de:* `CORE-01`
**Qué:** `ISaveService` sobre SQLite persistiendo el agregado `Progress` de `§9`
(`lessonId → crowns, unlockedItems[], streak, blackbookPages[]`), tras interfaz `IProgressRepo`
(patrón Repository, `§7`) para permitir el sync diferido de Fase 3.
**Hecho cuando:** test EditMode: `Save(progress)` seguido de reabrir el repo y `Load()` devuelve
un `Progress` igual (round-trip); el esquema se crea si no existe.

### `CORE-03` · AudioService
*módulo:* `Assets/_Project/Core` · *depende de:* `CORE-01`
**Qué:** `IAudioService` que reacciona a eventos del `EventBus` (`§3`): loop de spray mientras
hay trazo, one-shots de UI y de "corona". Catálogo de clips por `ScriptableObject` (`§7`), sin
strings mágicos.
**Hecho cuando:** test PlayMode: al publicar `StrokeStarted` el `AudioSource` de spray queda
`isPlaying`; al publicar `StrokeEnded` deja de sonar.

### `CORE-04` · Haptics
*módulo:* `Assets/_Project/Core` · *depende de:* `CORE-01`
**Qué:** `IHaptics` con impactos ligeros (inicio de trazo, corona ganada) multiplataforma
(iOS/Android), no-op en editor.
**Hecho cuando:** test EditMode: llamar `IHaptics.Light()` en editor no lanza excepción y es
no-op; en dispositivo, disparo verificable a mano (casilla de checklist).

---

## Drawing Engine — endurecimiento de producción

Recogidas de `INFORME-GO-NOGO.md §6` ("Recomendaciones para Fase 1").

### `ENG-05` · Compositing multicapa real 🔴
*módulo:* `Assets/_Project/DrawingEngine/Canvas` · *depende de:* —
**Qué:** implementar las 4 capas de `§4.1` (boceto/fill/outline/detalles) y hacer que
`Flatten()` (API `§4.3`) las componga en orden, respetando la capa activa (`SetActiveLayer`).
**Hecho cuando:** test EditMode: pintar en 2 capas distintas y `Flatten()` produce una textura
donde ambos aportes son visibles; ocultar la capa superior cambia el resultado.

### `ENG-06` · Undo por tiles comprimidos
*módulo:* `Assets/_Project/DrawingEngine/Canvas` · *depende de:* `ENG-05`
**Qué:** sustituir el ring buffer de snapshots RT completos por snapshots **por tiles**
(solo tiles modificados), en memoria con spill a disco (`§4.1`), para escalar a más capas y a
iPad 4096².
**Hecho cuando:** test EditMode: 20 trazos + 20 `Undo()` dejan la RT idéntica (hash de píxeles)
al canvas vacío inicial; el pico de memoria del historial es menor que con snapshots completos
(aserción sobre bytes reservados).

### `ENG-07` · Herramientas como Strategy
*módulo:* `Assets/_Project/DrawingEngine/Stamping` · *depende de:* `ENG-05`
**Qué:** extraer una `IStampStrategy` para que spray/marker/roller sean estrategias
intercambiables sobre el mismo `GpuStamper` (`§7`). Fase 1 entrega **spray** (existente) +
**marker** (borde duro, sin overspray) como prueba del patrón.
**Hecho cuando:** test EditMode: cambiar de estrategia entre trazos altera el resultado de
stamping (marker sin niebla exterior vs spray con overspray) sin tocar `DrawingCanvas`.

### `ENG-08` · Retirar el harness de spike
*módulo:* `Assets/_Project/Spike` (borrado) · *depende de:* `UI-03`
**Qué:** borrar el assembly `PieceBook.Spike` y mover el HUD de demo a `UI` (`§6.5` del informe).
**Hecho cuando:** `PieceBook.Spike*` ya no existe; la escena de pintura arranca desde `UI`; el
proyecto compila sin referencias colgantes.

---

## Lesson System (data-driven)

### `LES-01` · Modelo `LessonDef` (SO + JSON)
*módulo:* `Assets/_Project/Lessons` · *depende de:* —
**Qué:** `LessonDef` y los tipos de paso de `§5` (`showcase`, `trace`, `freeform`, `quiz`) como
ScriptableObjects cargables desde el JSON de ejemplo de `§5`, campos exactos de `§9`
(`{id, chapter, steps[], unlocks[], glossaryRefs[]}`).
**Hecho cuando:** test EditMode: deserializar el JSON de `lesson_tag_03` de `§5` produce un
`LessonDef` con 4 pasos de los tipos correctos.

### `LES-02` · Lesson runner (state machine) 🔴
*módulo:* `Assets/_Project/Lessons` · *depende de:* `LES-01`
**Qué:** máquina de estados jerárquica (`§7`) que recorre los pasos de una lección
(`showcase → trace → freeform → quiz`), avanza al superar cada uno y publica `LessonPassed` en el
`EventBus` al terminar.
**Hecho cuando:** test PlayMode con una lección fixture de 4 pasos: el runner llega al estado
final y publica `LessonPassed` exactamente una vez.

### `LES-03` · Evaluador de trazos (sin ML)
*módulo:* `Assets/_Project/Lessons` · *depende de:* `LES-01`
**Qué:** métricas geométricas de `§5` sobre la polyline del trazo — precisión (distancia media
al campo de distancia del template), suavidad (varianza de curvatura), consistencia de velocidad,
peso de línea — y mapeo a **1-3 coronas**.
**Hecho cuando:** test EditMode con 3 polylines fixture (perfecta / media / mala contra un
template) devuelve 3 / 2 / 1 coronas respectivamente.

### `LES-04` · Quiz de glosario
*módulo:* `Assets/_Project/Lessons` · *depende de:* `LES-01`
**Qué:** paso `quiz` que referencia `glossaryRefs` y valida respuestas. Glosario mínimo de Fase 1
(los términos que citan las 5 lecciones); el glosario completo es Fase 2.
**Hecho cuando:** test EditMode: un quiz con respuesta correcta marca el paso superado; con
incorrecta, no.

### `LES-05` · Pasos `trace` y `freeform` sobre el motor
*módulo:* `Assets/_Project/Lessons` · *depende de:* `LES-02`, `LES-03`, `ENG-05`
**Qué:** conectar los pasos de dibujo al `IDrawingCanvas` (`§4.3`): `trace` muestra plantilla +
tolerancia y evalúa contra ella; `freeform` deja pintar libre y evalúa solo las métricas pedidas.
**Hecho cuando:** test PlayMode: completar un paso `trace` con un trazo dentro de `tolerance`
lo marca superado y adjunta el nº de coronas de `LES-03`.

---

## UI (MVP: presenters testeables, views tontas)

### `UI-01` · App shell + state machine de navegación 🔴
*módulo:* `Assets/_Project/UI` · *depende de:* `CORE-01`
**Qué:** flujo `Menu → Lesson → Paint → Review` como state machine (`§7`), presenters sin
dependencias de Unity, views como prefabs tontos (`§7`).
**Hecho cuando:** test EditMode del presenter: la transición `Menu→Lesson` cambia el estado
expuesto sin instanciar ningún `GameObject`.

### `UI-02` · Lesson view (showcase + quiz)
*módulo:* `Assets/_Project/UI` · *depende de:* `UI-01`, `LES-02`
**Qué:** view que renderiza pasos `showcase` (media + `maxSeconds`) y `quiz`, dirigida por el
runner (`LES-02`) vía presenter.
**Hecho cuando:** test EditMode del presenter: recibe un paso `showcase` y expone su `media`/
`maxSeconds`; recibe un `quiz` y expone sus opciones.

### `UI-03` · Paint view (HUD real) 🔴
*módulo:* `Assets/_Project/UI` · *depende de:* `UI-01`, `ENG-07`
**Qué:** HUD de pintura de producción sustituyendo al `DemoHud` del spike: selector de cap,
control de distancia de boquilla (slider/gesto, `§4.1`), undo/redo, capas. Presenter habla con
`IDrawingCanvas` (`§4.3`), nunca con la implementación.
**Hecho cuando:** test EditMode del presenter: pulsar "undo" invoca `IDrawingCanvas.Undo()`
(mock) exactamente una vez; cambiar de cap actualiza el `StrokeConfig` activo.

### `UI-04` · Review view (coronas + desbloqueos)
*módulo:* `Assets/_Project/UI` · *depende de:* `UI-01`, `META-02`
**Qué:** pantalla de resultado tras `LessonPassed`: coronas obtenidas y qué se desbloqueó,
con sonido/haptic de recompensa (`CORE-03`/`CORE-04`).
**Hecho cuando:** test EditMode del presenter: dado un `LessonResult` de 3 coronas + 1 unlock,
expone ambos; publica el evento que dispara audio/haptic de recompensa.

---

## Meta-game (mínimo de Fase 1)

### `META-01` · Progresión y coronas
*módulo:* `Assets/_Project/MetaGame` · *depende de:* `CORE-02`
**Qué:** consumir `LessonPassed` (EventBus) y persistir coronas + `streak` en `Progress`
(`§9`) vía `IProgressRepo`.
**Hecho cuando:** test PlayMode: publicar `LessonPassed{lesson, crowns=2}` deja `Progress` con
2 coronas en esa lección tras `Save`/`Load`.

### `META-02` · Desbloqueos (tienda mínima)
*módulo:* `Assets/_Project/MetaGame` · *depende de:* `META-01`
**Qué:** aplicar los `unlocks[]` de la lección superada (p. ej. `cap_ny_fat`) y publicar
`ItemUnlocked` (`§3`). El `unlockCost` de `CapDef` (`§9`) queda cableado pero la moneda es Fase 2.
**Hecho cuando:** test PlayMode: superar una lección con `unlocks:["cap_x"]` añade `cap_x` a
`unlockedItems` y publica `ItemUnlocked` una vez.

---

## Contenido (ScriptableObjects, `§8` `Content/`)

### `CNT-01` · 3 caps + paints del Cap. 1
*módulo:* `Assets/_Project/Content` · *depende de:* `ENG-07`
**Qué:** 3 `CapDef` reales (skinny / soft / fat, campos de `§9`) y sus `PaintDef`, más allá de
los generados del spike.
**Hecho cuando:** existen 3 assets `CapDef` válidos (cargan sin error) con `coneAngle`
distintos y `unlockCost` definido.

### `CNT-02` · Alfabeto handstyle
*módulo:* `Assets/_Project/Content` · *depende de:* `LES-03`
**Qué:** un `AlphabetDef` estilo `handstyle` (`§9`) con las 26 letras → `{templatePath,
strokeOrder[], difficulty}` y sus campos de distancia precalculados para el evaluador (`§5`).
**Hecho cuando:** el `AlphabetDef` tiene 26 entradas; cada template carga un campo de distancia
no vacío que `LES-03` puede consumir.

### `CNT-03` · 5 lecciones de tag (Capítulo 1) 🔴
*módulo:* `Assets/_Project/Content` · *depende de:* `LES-05`, `CNT-02`
**Qué:** las 5 `LessonDef` del Cap. 1 (progresión de dificultad de tag), cada una con sus pasos
`showcase/trace/freeform/quiz`, `unlocks[]` y `glossaryRefs[]` (`§5`, `§9`).
**Hecho cuando:** las 5 lecciones cargan; el runner (`LES-02`) las recorre de principio a fin en
un test PlayMode encadenado sin errores.

---

## Cierre de fase

### `SLICE-01` · Smoke test de vertical slice 🔴
*módulo:* `Assets/Tests/PlayMode` · *depende de:* todas las anteriores
**Qué:** test PlayMode end-to-end: arrancar en menú → entrar en `lesson_tag_01` → completar los
4 tipos de paso → ganar coronas → ver el desbloqueo → confirmar que `Progress` persiste tras
recargar el `SaveService`.
**Hecho cuando:** el test pasa en verde en CI (EditMode/PlayMode headless); el checklist de
"jugable en dispositivo" queda listo para verificación manual en iPhone 11 / Android gama media.
