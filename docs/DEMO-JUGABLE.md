# Demo jugable — cómo jugarla y qué falta

Estado a 2026-10-15. Hay **dos demos** que comparten el mismo contenido (lecciones, glosario,
caps y plantillas de letras salen de `unity6/Assets/_Project/Content`):

| | Demo web | Demo Unity |
|---|---|---|
| Para qué | Jugar/enseñar **ya**, en cualquier navegador (móvil o PC) | Validar el juego real en el editor y en dispositivo |
| Dónde | `demo-web/dist/tag-school-demo.html` (doble clic, sin servidor) | Menú `TAG-School ▸ Build Playable Demo` → Play |
| Bucle | Mapa → lección (mira · traza · libre · quiz) → review, + muro libre, blackbook con mockups, glosario, tienda | Mapa → lección → review + tienda |
| Verificado | Smoke E2E con Playwright: 34/34 (escritorio y móvil) | Compile check 10/10 asmdefs; la matemática nueva se ejecutó aparte (10/10) |
| Sin verificar | Dispositivos reales (presión de Apple Pencil) | **No se ha pulsado Play en un Unity real** (no hay editor en este entorno) |

## Cómo jugar

### Web (lo más rápido)
1. Abre `demo-web/dist/tag-school-demo.html` en el navegador (o el enlace publicado).
2. Escribe tu nombre de writer. Empieza por *Capítulo 1 · Tu primer tag*.
3. Cada lección tiene 4 pasos: el profe (Meka) pinta la letra → la trazas encima de la guía cian
   de una pasada → la repites sin guía → quiz del glosario. Al final: coronas, monedas y
   desbloqueos.
4. "Abrir todas las lecciones" (en el mapa) desbloquea todo para revisar el contenido sin jugarlo
   en orden.

Para regenerarla tras cambiar contenido: `node demo-web/build.mjs`.
Para probarla: `NODE_PATH=$(npm root -g) node demo-web/tools/smoke.mjs`.

### Unity
1. Abre `unity6/` con Unity 6000.6.0f1.
2. Menú **TAG-School ▸ Build Playable Demo**. Genera el contenido, registra los shaders, crea los
   materiales y guarda `Assets/_Project/Scenes/DemoPlayable.unity` (primera en Build Settings).
3. **Play.** Pinta con el ratón sobre la pared; el panel de la derecha es la UI (IMGUI).

La escena usa las piezas de producción: `GameContext` (DI + guardado JSON + MetaGame + audio),
`LessonRunner`, `LessonPresenter`/`ReviewPresenter` (la demo es su *view*),
`DrawingStepController`, `StoreService` y el `DrawingCanvas` real.

## Lo que faltaba para tener una demo, y qué se ha hecho

### Bloqueantes encontrados
- **`main` no compilaba en Unity 6.** `StrokeConfig(...)` no asignaba el campo `Tool` (error
  CS0171 con C# 9). Nadie lo vio porque el CI de Unity nunca llegó a compilar: falla antes, por
  la licencia. **Arreglado.**
- **La rama `claude/repo-status-pending-plans-gjox2l` (no mergeada) tampoco compilaba**:
  `Phase1TestHarness` usaba `QuizStep.Prompt`/`QuizStep.Options`, que no existen, y
  `Phase1SceneBuilder` usaba el ensamblado Spike y uGUI sin declararlos en su asmdef. Se ha
  integrado lo aprovechable (plantillas A-Z, audio) y esos dos ficheros se han sustituido por
  `DemoGame` + `DemoSceneBuilder`.
- **No había escena jugable** del bucle completo: solo el spike de spray. Los presenters existían
  pero ninguna *view* los implementaba. **Hecho:** `Spike/DemoGame.cs`.
- **No había forma de comprobar que compila sin licencia de Unity.** **Hecho:**
  `tools/unity-compile-check` (un `.csproj` por asmdef contra DLLs de referencia de NuGet) y el
  workflow `Compile check + demo web`.

### Niveles (lecciones y plantillas)
- Las plantillas de letras eran **ondas senoidales**, no letras. La rama anterior ya traía
  polilíneas A-Z de un trazo (`HandstyleLetterTemplates`); ahora el alfabeto *bubble* usa esas
  mismas letras redondeadas con Chaikin (`BubbleLetterTemplates`) en vez de copiarlas.
- Los vídeos de *showcase* (`vid_*`) no existen. **Sustituto:** el profe pinta la plantilla con el
  motor real, y cada lección tiene un texto (`caption`, nuevo campo opcional del JSON).
- **Evaluación engañable:** la precisión solo mide trazo→plantilla, así que un punto encima de la
  línea sacaba 100 %. **Añadido:** `StrokeMetrics.Coverage` (plantilla→trazo) como puerta: hay que
  recorrer al menos el 60 % de la letra para puntuar.
- **Freeform con `precision` siempre daba 1 corona** (sin plantilla se salta la métrica y queda sin
  puntuar). **Arreglado:** se puntúa contra la plantilla de la lección tras encajar el dibujo en su
  caja (`StrokeMetrics.AlignToTemplate`). Afecta a `lesson_color_01` y `lesson_throwup_02`.
- `DrawingStepController.EvaluateCurrent()` evalúa sin enviar, para poder enseñar el resultado y
  dejar repetir antes de aceptar.

### Personajes (PJs)
El diseño (`ARQUITECTURA.md`) **no tiene personajes**: el protagonista es el jugador y su firma.
Para que la demo tenga voz, se ha añadido lo mínimo:
- **Meka**, el profe: avatar de texto que explica cada paso y da un consejo según la métrica más
  floja.
- **Nombre de writer** del jugador (web): se pide al empezar y firma las piezas del blackbook.

### Assets
- **Paredes:** generador procedural (`demo-web/src/walls.js`: ladrillo, hormigón, persiana, vagón)
  que usa la demo y que exporta los PNG de Unity (`demo-web/tools/export-assets.mjs`). Sustituyen a
  los placeholders anteriores (el hormigón tenía moiré).
- **Audio:** los WAV placeholder existían pero el `AudioCatalog` los dejaba a `null`. Ahora el
  content builder los asigna (spray en bucle, click, corona, desbloqueo). En la web, el siseo del
  spray y los sonidos se sintetizan con WebAudio.
- **Caps:** previsualización de trazo de cada cap en la tienda (web), generada con el motor.

## Qué sigue faltando (por prioridad)

1. **Pulsar Play en Unity y probar en un móvil.** Compila contra Unity 2021.3 (referencias de
   NuGet), no contra 6000.6; una API exclusiva de Unity 6 solo la detectaría el editor. Falta medir
   latencia/FPS en dispositivo (`INFORME-GO-NOGO.md`).
2. **Licencia de Unity en CI** (`UNITY_LICENSE`): el workflow de activación falló. Sin ella los tests
   EditMode no corren en GitHub. El compile check nuevo cubre la compilación mientras tanto.
3. **Letras de verdad.** Las plantillas son polilíneas geométricas de un solo trazo. Hace falta que
   un writer dibuje los alfabetos (handstyle con estilo, bubble con contorno y relleno). Propuesta:
   grabar el trazo del artista con el propio motor (`StrokeRecording`) y exportarlo a plantilla.
4. **Contenido:**
   - Tres lecciones **no dan recompensa nueva**: `lesson_throwup_01` vuelve a desbloquear
     `cap_fat` (ya ganado en `lesson_tag_03`), `lesson_throwup_03` vuelve a dar `cap_ny_fat` (de
     `lesson_tag_05`) y `lesson_color_02` da `cap_skinny`, que es gratis. Hay que decidir qué dan
     (alfabeto bubble, colores, paredes…).
   - Los quizzes no tienen enunciado ni distractores en los datos; con 2 términos son triviales.
     La demo pregunta "¿qué término es esta definición?" y añade un distractor del glosario.
   - El capítulo 3 (color por capas) no usa capas: no hay UI de capas todavía.
5. **UI de producción.** La demo Unity usa IMGUI para no depender de prefabs. Faltan las views
   reales (UI Toolkit o uGUI) con diseño, y en Unity el muro libre, el blackbook y el glosario
   (en la web ya están).
6. **Freeform con varios trazos** (p. ej. "Firma tu nombre"): Unity evalúa solo el último trazo; la
   web pondera todos por longitud. Llevar eso a `DrawingStepController`.
7. **Calibración con jugadores**: umbrales de coronas, tolerancias y la puerta de cobertura (60 %)
   están puestos a ojo. Con ratón o dedo sin presión, *peso de línea* siempre da 100 %.
8. Lo que ya estaba previsto para fases siguientes: vídeo MP4 real, share nativo, RA, Firebase.

## Ficheros nuevos o cambiados

- `demo-web/` — demo web: `src/` (motor de spray, evaluación, paredes, juego), `build.mjs`,
  `tools/smoke.mjs` (E2E), `tools/export-assets.mjs`, `dist/tag-school-demo.html`.
- `unity6/Assets/_Project/Spike/DemoGame.cs`, `Spike/Editor/DemoSceneBuilder.cs` — demo Unity.
- `unity6/Assets/_Project/Lessons/` — `Coverage`, `AlignToTemplate`, `EvaluateCurrent`,
  `CoverageOfCurrent`, `ShowcaseStep.Caption`. Tests: `Tests/EditMode/LessonsDemoEditTests.cs`.
- `unity6/Assets/_Project/Content/` — `BubbleLetterTemplates.cs`, builder con audio y
  `BuildAssets()`, `caption` en las 10 lecciones.
- `unity6/Assets/_Project/DrawingEngine/Api/StrokeConfig.cs` — fix de compilación.
- `tools/unity-compile-check/` y `.github/workflows/demo-and-compile.yml`.
