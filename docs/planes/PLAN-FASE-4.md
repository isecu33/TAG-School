# PLAN · Fase 4 — Post-launch

**Autor:** BLUEPRINT · **Roadmap:** `ARQUITECTURA §11` (semanas 23+) ·
**Convenciones:** [`README.md`](README.md) · [`../GUIA-AGENTES.md`](../GUIA-AGENTES.md)

## Objetivo (criterio de fase)

Profundizar el juego tras el lanzamiento: **wildstyle** y **pieces multicapa** completas,
**RA paint-over** (pintar directamente sobre el plano detectado), **galería comunitaria** con
moderación, y **eventos ("jams")**. Es crecimiento sobre una base ya lanzada, así que cada
epígrafe se activa por métricas de la Fase 3 (retención/embudo), no "porque toca".

**Nota de altitud:** la Fase 4 es intencionadamente menos granular que las Fases 1-3 — el
producto lanzado y sus datos reordenarán prioridades. Estas tareas son direcciones con criterio
binario, no un cronograma cerrado.

## Reglas que se mantienen

- El **clasificador de estilo on-device** (Sentis/CoreML, `§5`) es el primer candidato de ML;
  entra solo si mejora el feedback medible, nunca como requisito.
- La galería comunitaria **exige moderación** antes de ser pública (`§10`): Cloud Function +
  hash de imágenes reportadas. Nada de menores expuestos.
- RA paint-over es "post-MVP" por diseño (`§6`): más riesgo, se aísla tras `ARModule`.

---

## Estado tras Fase 3 — qué está listo para empezar

La Fase 3 dejó **fronteras + stubs** sobre los que la Fase 4 engancha sin bloquearse en dispositivo:
`ARModule` (stub `IArPlacementSession`), `IArtworkUploader` (Storage), `RemoteConfig`, `IAnalytics`,
`ProgressSyncService`. Por eso cada tarea de Fase 4 tiene un **núcleo testeable ahora** (lógica pura,
como hicimos en Fases 1-3) y una **parte de dispositivo/backend** que se valida en editor/servidor.

| Tarea | Núcleo testeable ahora (EditMode) | Parte dispositivo/backend | Depende de |
|---|---|---|---|
| `CNT-07` Wildstyle | alfabeto+lecciones (parser/runner ya existen) | arte de letterforms | Fase 2 CNT-05 |
| `ENG-09` Multicapa 8 | orden de `Flatten`, presupuesto de memoria (aserción bytes) | GPU 4096²/iPad | Fase 1 ENG-05/06 |
| `AR-05` Paint-over RA | mapeo pantalla→plano→UV (math pura) | anclaje/persistencia RA | Fase 3 AR-02 |
| `SOC-06` Galería+moderación | `IModerationGate` (oculta hasta aprobar; oculta hashes reportados) | Cloud Function + galería | Fase 3 DATA-03 (`IArtworkUploader`) |
| `META-06` Jams | ventana activa/expirada por tiempo + `RemoteConfig` (pura) | orquestación remota del evento | `SOC-06`, DATA-04 |
| `LES-06` Clasificador estilo | interfaz `IStyleClassifier` + fusión con métricas | modelo Sentis/CoreML on-device | Fase 1 LES-03 |

**Orden de arranque sugerido (primeras tareas 100% testeables sin dispositivo):**
1. `META-06` núcleo (lógica de ventana de jam sobre `RemoteConfig`).
2. `SOC-06` núcleo (`IModerationGate` + estados de visibilidad).
3. `AR-05` núcleo (mapeo pantalla→plano→UV con un plano fixture).
4. `LES-06` interfaz + fusión (sin el modelo real).

Cada una sigue el patrón ya usado: lógica pura + frontera, con su test EditMode; la parte de
dispositivo/backend queda detrás de la interfaz y se valida aparte.

---

## Contenido avanzado

### `CNT-07` · Wildstyle 🔴
*módulo:* `Assets/_Project/Content` · *depende de:* Fase 2 (`CNT-05`)
**Qué:** alfabeto y lecciones `wildstyle` (`§9` `AlphabetDef.style`), con templates de mayor
`difficulty` y strokeOrder complejos.
**Hecho cuando:** las lecciones wildstyle cargan y el runner las recorre end-to-end (test
PlayMode); usan las 4 capas de `§4.1`.

### `ENG-09` · Pieces multicapa completas
*módulo:* `Assets/_Project/DrawingEngine` · *depende de:* Fase 1 (`ENG-05`, `ENG-06`)
**Qué:** llevar el compositing multicapa al máximo del presupuesto (`§4.1`: hasta 8 capas en
iPad, 4096²), apoyado en el undo por tiles (`ENG-06`) para que la memoria escale (`§4.2` < 1 GB).
**Hecho cuando:** test: componer 8 capas a 4096² se mantiene bajo el presupuesto de memoria de
`§4.2` (aserción sobre bytes); `Flatten` respeta el orden de las 8.

---

## RA avanzada

### `AR-05` · Paint-over en RA 🔴
*módulo:* `Assets/_Project/ARModule` · *depende de:* Fase 3 (`AR-02`)
**Qué:** pintar directamente sobre el plano vertical detectado en RA (no solo plasmar la obra
terminada), reproyectando el input del `IDrawingCanvas` (`§4.3`) al plano (`§6`).
**Hecho cuando:** en dispositivo, un trazo hecho en modo RA aparece anclado al plano y persiste
al mover el teléfono (checklist); test EditMode del mapeo pantalla→plano→UV con un plano fixture.

---

## Comunidad

### `SOC-06` · Galería comunitaria + moderación 🔴
*módulo:* `Assets/_Project/Social` + backend · *depende de:* Fase 3 (`DATA-03`)
**Qué:** galería pública de obras compartidas; **toda** obra pasa moderación (Cloud Function +
hash de reportadas) antes de ser visible (`§10`). Es el primer canal "hacia dentro" del juego.
**Hecho cuando:** una obra no aparece en la galería hasta pasar moderación (test del gate); una
obra con hash reportado queda oculta automáticamente.

### `META-06` · Eventos / jams
*módulo:* `Assets/_Project/MetaGame` · *depende de:* `SOC-06`
**Qué:** eventos temporales ("jams") con tema y ventana temporal, dirigidos por Remote Config
(`DATA-04`), que premian participación con desbloqueos.
**Hecho cuando:** un jam activo por Remote Config aparece y expira en su ventana; participar
otorga la recompensa una sola vez (test PlayMode).

---

## ML (opcional, dirigido por datos)

### `LES-06` · Clasificador de estilo on-device
*módulo:* `Assets/_Project/Lessons` · *depende de:* Fase 1 (`LES-03`)
**Qué:** clasificador ligero on-device (Sentis/CoreML, `§5`) que añade feedback de **estilo**
sobre las métricas geométricas existentes; nunca sustituye la evaluación determinista, la
complementa.
**Hecho cuando:** el clasificador corre on-device bajo el presupuesto de frame (`§4.2`) sin
tocar la ruta de dibujo; A/B mide si mejora la retención antes de activarlo por defecto
(criterio de datos, no solo test).
