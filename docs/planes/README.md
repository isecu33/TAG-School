# Planes de fase — PIECEBOOK / TAG-School

**Autor:** BLUEPRINT (Arquitecto) · **Estado:** planificación viva ·
**Fuente de verdad técnica:** [`../ARQUITECTURA.md`](../ARQUITECTURA.md)

La [Fase 0](../../INFORME-GO-NOGO.md) (spike del Drawing Engine) validó latencia y feel y quedó
en **GO provisional**. Este directorio contiene los planes de las fases que faltan del roadmap
(`ARQUITECTURA §11`), descompuestos igual que la Fase 0: en **tareas codificadas y verificables**
que BLUEPRINT puede delegar a un agente de bajo coste (regla de oro de `§12`, ver
[`../GUIA-AGENTES.md`](../GUIA-AGENTES.md)).

Estos planes son el "qué y en qué orden". El "cómo" técnico ya está fijado por la arquitectura;
cada tarea apunta al parágrafo (`§`) que la gobierna y no lo re-litiga.

## Índice

| Fase | Semanas (§11) | Plan | Objetivo en una frase |
|---|---|---|---|
| **0. Spike** | 1-2 | [`INFORME-GO-NOGO.md`](../../INFORME-GO-NOGO.md) ✅ | Motor de spray validado (latencia/feel). |
| **1. Vertical slice** | 3-8 | [`PLAN-FASE-1.md`](PLAN-FASE-1.md) | Capítulo 1 jugable de principio a fin: 5 lecciones de tag, 3 caps, 1 alfabeto handstyle, save local, sonido + haptics. |
| **2. MVP** | 9-16 | [`PLAN-FASE-2.md`](PLAN-FASE-2.md) | 3 capítulos (tag→throw-up→color), glosario, blackbook, export imagen+vídeo, share nativo, mockups de pared. |
| **3. RA + Social** | 17-22 | [`PLAN-FASE-3.md`](PLAN-FASE-3.md) | Plasmado RA de obras, replay MP4, sync Firebase, soft launch. |
| **4. Post-launch** | 23+ | [`PLAN-FASE-4.md`](PLAN-FASE-4.md) | Wildstyle, pieces multicapa, RA paint-over, galería, jams. |

## Convenciones de las tareas

Cada plan lista tareas con esta forma:

> **`XXX-NN` · Título corto** · *depende de:* … · *módulo:* `Assets/_Project/…`
> **Qué:** una frase.
> **Hecho cuando (criterio binario):** condición verificable con un test EditMode/PlayMode o una
> comprobación objetiva. Sin criterio verificable, la tarea no se delega (`§12`).

**Prefijos de código** (uno por área/asmdef de `§8`):

| Prefijo | Área | asmdef (§8) |
|---|---|---|
| `ENG` | Drawing Engine (endurecimiento de producción) | `PieceBook.DrawingEngine` |
| `CORE` | Servicios core (Save, Audio, Haptics, DI) | `PieceBook.Core` |
| `LES` | Lesson System (runner, evaluadores, quiz) | `PieceBook.Lessons` |
| `UI` | Presenters + views (MVP) | `PieceBook.UI` |
| `META` | Meta-game (progresión, blackbook, tienda) | `PieceBook.MetaGame` |
| `SOC` | Social / export (share, replay→MP4) | `PieceBook.Social` |
| `AR` | Módulo RA (opcional, por Addressables) | `PieceBook.ARModule` |
| `DATA` | Capa de datos (SQLite, Addressables, Firebase) | reparte entre `Core`/`Social` |
| `CNT` | Contenido (ScriptableObjects: caps, paints, alfabetos, lecciones) | *assets* |
| `CI` | Build / release (GitHub Actions, Fastlane) | *tooling* |

**Definition of Done común a toda tarea** (además de su criterio binario):

1. Respeta las reglas de dependencia de `§3` (todos pueden depender de `Core`; nadie de
   `ARModule`; el Drawing Engine no conoce UI ni lecciones; comunicación por `EventBus` tipado).
2. No introduce ningún antipatrón prohibido de `§7` (singletons dispersos, `Update()` para
   eventos, lógica en Views, strings mágicos).
3. Si toca comportamiento con lógica pura, trae un test en `Tests/EditMode`; si toca
   integración de escena, un test `Tests/PlayMode`.
4. Cero `SetPixels` y cero allocs por frame en cualquier ruta de dibujo (`§4`).

## Cómo leer la prioridad

Dentro de cada fase las tareas están **topológicamente ordenadas**: una tarea solo depende de
tareas anteriores (o de fases previas). El **camino crítico** de cada fase está marcado con 🔴;
lo demás puede paralelizarse entre agentes.
