# PIECEBOOK — Arquitectura Técnica
**Autor:** BLUEPRINT (Arquitecto) · **Versión:** 1.0 · **Estado:** Fuente de verdad técnica

---

## 1. Visión técnica en una frase
Juego móvil offline-first construido en Unity, con un motor de pintura propio sobre RenderTextures + shaders, contenido 100% data-driven, módulo RA opcional vía AR Foundation y capa social ligera sobre Firebase.

---

## 2. Stack tecnológico

| Capa | Tecnología | Justificación |
|---|---|---|
| Motor | **Unity 6.0.6.0f1 (URP)** | Único motor con brush engine performante + AR Foundation (ARKit/ARCore unificado) + build iOS/Android desde un código. Godot 4 se descartó por madurez inferior de su stack RA. |
| Lenguaje | C# (.NET Standard 2.1) | Estándar Unity. |
| Render | Universal Render Pipeline | Shaders custom del spray, buen rendimiento en gama media. |
| RA | AR Foundation 5.x (ARKit + ARCore) | Detección de planos verticales, anclaje de piezas, oclusión donde el hardware lo permita. |
| Input | Unity Input System + Apple Pencil (presión/tilt vía UIKit plugin) | Latencia mínima; en iPad, presión = distancia de boquilla. |
| Datos de contenido | ScriptableObjects + JSON (Addressables) | Lecciones/materiales/alfabetos editables sin recompilar; DLC de contenido sin actualizar la app. |
| Persistencia local | SQLite (progreso) + PNG/capa propia (obras) | Robusto, portable, backup sencillo. |
| Backend | **Firebase** (Auth, Firestore, Storage, Remote Config, Crashlytics, Analytics) | Serverless, coste ~0 en MVP, escalable. |
| Compartir en redes | Native Share (plugin) + export MP4 (replay del trazado) | El vídeo del proceso es el contenido más viral. |
| CI/CD | GitHub Actions + Fastlane; Unity Build Automation opcional | Builds automáticas TestFlight / Play Internal. |
| Analítica de aprendizaje | Firebase Analytics + eventos custom | Medir dónde abandona la gente cada lección. |

**Dispositivo mínimo objetivo:** iPhone 11 / Android 2021 gama media (Snapdragon 720G, 4 GB RAM). iPad + Pencil es la experiencia premium, no la mínima.

---

## 3. Arquitectura de módulos

```
┌─────────────────────────────────────────────────────┐
│                     META-GAME                        │
│   progresión · desbloqueos · blackbook · glosario    │
├──────────────┬──────────────┬───────────────────────┤
│ LESSON       │ DRAWING      │ AR MODULE             │
│ SYSTEM       │ ENGINE       │ (opcional en runtime) │
│ lecciones    │ core crítico │ planos verticales,    │
│ evaluación   │ 60fps        │ anclaje, captura      │
├──────────────┴──────────────┴───────────────────────┤
│ SOCIAL / EXPORT      │  DATA LAYER                   │
│ share, replay MP4    │  SQLite + Addressables + FB   │
├──────────────────────┴───────────────────────────────┤
│                 CORE / SERVICES                       │
│  EventBus · SaveService · AudioService · Haptics      │
└───────────────────────────────────────────────────────┘
```

Cada módulo = un **Assembly Definition** propio. Reglas de dependencia:
- Todos pueden depender de `Core`. Nadie depende de `AR Module` (se carga por Addressables solo si el dispositivo lo soporta).
- `Drawing Engine` no conoce lecciones ni UI: expone una API pura.
- Comunicación entre módulos: **EventBus tipado** (pub/sub), nunca referencias directas cruzadas.

---

## 4. Drawing Engine (el corazón)

### 4.1 Diseño
- **Canvas = RenderTexture** (2048×2048 en gama media, 4096 en iPad Pro) con **capas** (máx. 4 en móvil, 8 en iPad): boceto, fill, outline, detalles.
- **Trazo = stamping por GPU**: se interpolan puntos del input (Catmull-Rom) y se estampan sprites de partícula de spray vía `CommandBuffer` / `Graphics.DrawMeshInstanced`. Nunca `SetPixels`.
- **Simulación de spray por parámetros** (todo viene del ScriptableObject del cap/material):
  - `coneAngle` (skinny ≈ 4°, fat ≈ 25°), `density`, `falloff` (dureza del borde), `flowRate`, `dripThreshold` (píxeles de pintura acumulada que disparan un drip procedural), `overspray` (niebla exterior), `pressureCurve` (respuesta a presión Pencil o a slider táctil).
- **Distancia de la boquilla**: en táctil, se controla con presión (Pencil) o con gesto de dos dedos/slider lateral; a más distancia → cono mayor, borde más suave, más overspray. Esta es LA mecánica que enseña control real.
- **Drips procedurales**: sistema de partículas 2D que nace cuando se supera `dripThreshold`; los drips son configurables (las lecciones avanzadas enseñan a usarlos a propósito).
- **Undo**: ring buffer de N=20 snapshots comprimidos por tiles (solo tiles modificados), en memoria + spill a disco.
- **Replay**: se graba la secuencia de inputs (no frames) → permite re-renderizar el proceso a MP4 en cualquier resolución. Barato en disco, oro para compartir.

### 4.2 Presupuestos
| Métrica | Objetivo |
|---|---|
| Latencia input→píxel | < 30 ms (ideal 16) |
| FPS dibujando | 60 sostenidos |
| FPS en RA | 30 |
| Memoria total | < 1 GB |
| Tamaño build inicial | < 300 MB (resto por Addressables) |

### 4.3 API pública (contrato)
```csharp
interface IDrawingCanvas {
    LayerId AddLayer(); void SetActiveLayer(LayerId id);
    void BeginStroke(StrokeConfig cfg);      // cap+pintura+color+presión inicial
    void UpdateStroke(Vector2 pos, float pressure, float tilt);
    void EndStroke();
    void Undo(); void Redo();
    Texture2D Flatten(); StrokeRecording GetRecording();
}
```

---

## 5. Lesson System (data-driven)

Una lección es un JSON/ScriptableObject con una lista de **pasos**, cada paso con un tipo:

```json
{
  "id": "lesson_tag_03",
  "title": "Flow: conecta las letras",
  "unlocks": ["cap_ny_fat"],
  "steps": [
    {"type": "showcase", "media": "vid_flow_intro", "maxSeconds": 12},
    {"type": "trace", "template": "tpl_tag_flow_A", "tolerance": 0.15,
     "metrics": ["smoothness", "speed_consistency", "line_weight"]},
    {"type": "freeform", "prompt": "Ahora tu tag, sin guía", "evaluate": ["smoothness"]},
    {"type": "quiz", "glossaryRefs": ["flow", "handstyle"]}
  ]
}
```

### Evaluación de trazos (sin ML en MVP)
Métricas geométricas sobre la polyline del trazo:
- **Precisión**: distancia media a la plantilla (campo de distancia precalculado por template).
- **Suavidad**: varianza de curvatura (jitter).
- **Consistencia de velocidad**: desviación estándar de la velocidad (los trazos buenos son decididos).
- **Peso de línea**: estabilidad de presión/distancia.

Puntuación → 1-3 "coronas". Post-MVP: clasificador ligero on-device (Sentis/CoreML) para feedback de estilo.

---

## 6. Módulo RA

- AR Foundation: detección de **planos verticales** → el jugador "pega" su piece en una pared real, la escala/rota, y captura foto o vídeo con la pieza anclada.
- Modo "paint over": pintar directamente en RA sobre el plano detectado (post-MVP; el MVP solo plasma obras terminadas — es el 80% del valor con el 20% del riesgo).
- Light estimation de ARKit/ARCore para integrar la pieza (multiplicar por luminancia ambiente).
- Fallback sin RA: mockups fotográficos de paredes (persianas, trenes en depósito legal, halls) con perspectiva — funciona en cualquier dispositivo.
- Marca de agua sutil del juego en exports (crecimiento orgánico).

---

## 7. Patrones de diseño (dónde y por qué)

| Patrón | Uso |
|---|---|
| **MVP (Model-View-Presenter)** | Toda la UI. Views tontas (prefabs), Presenters testeables sin Unity. |
| **State Machine (jerárquica)** | Flujo de app (Menu→Lesson→Paint→Review) y estados del trazo. |
| **Strategy** | Cada tipo de herramienta (spray, marker, roller) es una estrategia de stamping intercambiable. |
| **Flyweight + Object Pool** | Partículas de spray y drips. Cero allocs durante un trazo. |
| **Command** | Cada trazo es un Command → habilita undo/redo y replay gratis. |
| **Observer (EventBus tipado)** | Desacople módulos: `StrokeCompleted`, `LessonPassed`, `ItemUnlocked`. |
| **ScriptableObject como catálogo** | Caps, pinturas, alfabetos, lecciones, paletas. Diseño añade contenido sin código. |
| **Service Locator ligero (o VContainer)** | Servicios core (Save, Audio, Haptics, Analytics). Preferencia: **VContainer** (DI real, testeable). |
| **Repository** | Acceso a SQLite/Firestore tras interfaz única (`IProgressRepo`), permite offline-first con sync diferido. |

**Antipatrones prohibidos:** Singletons dispersos, `Update()` polling para lógica de eventos, lógica en Views, strings mágicos (usar constantes generadas de los catálogos).

---

## 8. Estructura de carpetas

```
Assets/
  _Project/
    Core/            (asmdef) EventBus, DI, Save, Audio, Haptics
    DrawingEngine/   (asmdef) canvas, brushes, shaders, drips, replay
    Lessons/         (asmdef) runner, evaluadores, quiz
    ARModule/        (asmdef) solo se referencia por Addressables
    Social/          (asmdef) export, share, replay-to-mp4
    MetaGame/        (asmdef) progresión, blackbook, tienda de desbloqueos
    UI/              (asmdef) MVP presenters + views
    Content/         ScriptableObjects: caps, paints, alphabets, lessons, palettes
  Art/  Audio/  AddressableGroups/
Tests/
  EditMode/  PlayMode/
```

---

## 9. Modelo de datos (núcleo)

```
CapDef        {id, name, coneAngle, density, falloff, flowRate, overspray, unlockCost}
PaintDef      {id, brand-ish name, opacity, glossiness, dripThreshold, colorRange}
AlphabetDef   {id, style (handstyle|blockbuster|bubble|semi-wild|wildstyle),
               letters[26] → {templatePath, strokeOrder[], difficulty}}
LessonDef     {id, chapter, steps[], unlocks[], glossaryRefs[]}
GlossaryEntry {id, term, definition, era, media?, relatedTerms[]}
Artwork       {id, layers[], strokeRecording, wallContext, createdAt, sharedUrl?}
Progress      {lessonId → crowns, unlockedItems[], streak, blackbookPages[]}
```

---

## 10. Seguridad, privacidad y tiendas
- Cuenta opcional (anónima por defecto, link a Apple/Google después). Nada de datos de menores: age gate + analytics reducida bajo 16.
- Contenido compartido pasa moderación básica (Cloud Function + hash de imágenes reportadas) antes de galerías públicas (post-MVP; en MVP solo se comparte HACIA fuera, no hay galería interna).
- Posicionamiento en tiendas: juego educativo/creativo; los muros del juego son espacios legales — importante para review de Apple.

---

## 11. Roadmap técnico

| Fase | Semanas | Entregable |
|---|---|---|
| **0. Spike** | 1-2 | Prototipo brush engine: latencia y feel del spray validados en dispositivo real. GO/NO-GO. |
| **1. Vertical slice** | 3-8 | Cap. 1 completo (5 lecciones de tag), 3 caps, 1 alfabeto handstyle, save local, sonido+haptics. |
| **2. MVP** | 9-16 | 3 capítulos (tag→throw-up→color), glosario, blackbook, export imagen+vídeo, share nativo, mockups de pared (sin RA). |
| **3. RA + Social** | 17-22 | Plasmado RA de obras terminadas, replay MP4, Firebase sync, soft launch. |
| **4. Post-launch** | 23+ | Wildstyle, pieces multicapa, RA paint-over, galería comunitaria, eventos ("jams"). |

---

## 12. Trabajo con agentes de bajo coste
Ver `GUIA-AGENTES.md`. Regla de oro: cada tarea que BLUEPRINT delega debe caber en <2k tokens de contexto y ser verificable con un test o criterio binario.
