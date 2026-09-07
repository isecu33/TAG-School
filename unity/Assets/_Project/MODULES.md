# _Project modules (ARQUITECTURA §8)

The folder map below mirrors §8. Phase 0 only implements what the spike needs; the rest
are intentional stubs so the structure — and the assembly boundaries — are established from
day one. Each module becomes its own asmdef when it gets real code.

| Folder | asmdef | Status |
|---|---|---|
| `Core/` | `PieceBook.Core` | ✅ EventBus + events, **DI + Save + Audio + Haptics (Fase 1: CORE-01..04)** |
| `DrawingEngine/` | `PieceBook.DrawingEngine` | ✅ Spike engine + **multicapa, Strategy de herramientas, tile-tracker (Fase 1: ENG-05..07)** |
| `DrawingEngine/Editor` | *(none yet)* | — |
| `Content/` | `PieceBook.Content.Editor` (builder) | ✅ Caps + paint + **alfabeto handstyle + 5 lecciones (Fase 1: CNT-01..03)** |
| `Spike/` | `PieceBook.Spike` (+ `.Editor`) | ⏳ Sigue presente — borrar (ENG-08) cuando la escena de `UI` esté validada en editor |
| `Lessons/` | `PieceBook.Lessons` | ✅ **Fase 1: modelo + parser + evaluador + runner (LES-01..05)** |
| `ARModule/` | `PieceBook.ARModule` | ⏳ Phase 3 (loaded via Addressables) |
| `Social/` | `PieceBook.Social` | ⏳ Phase 2/3 |
| `MetaGame/` | `PieceBook.MetaGame` | ✅ **Fase 1: progresión + desbloqueos (META-01, META-02)** |
| `UI/` | `PieceBook.UI` | ✅ **Fase 1: navegación + presenters MVP (UI-01..04)** · views-prefab pendientes de escena |

> **Fase 1 (vertical slice) — estado.** Toda la lógica está implementada con tests EditMode
> (incluido el smoke end-to-end SLICE-01). Pendiente de hacerse **en el editor de Unity** (no
> se puede en el entorno de construcción): montar la escena y los prefabs de views que
> implementan las interfaces de `UI`, ejecutar el content builder (`TAG-School ▸ Build Chapter 1
> Content`), y validar en dispositivo las rutas GPU (composición multicapa ENG-05, diferencia
> spray/marker ENG-07) y el copy-on-write de tiles del undo (ENG-06). Ver
> [`../../../docs/planes/PLAN-FASE-1.md`](../../../docs/planes/PLAN-FASE-1.md).

Dependency rule (§3): everyone may depend on `Core`; nobody depends on `ARModule`; the
Drawing Engine exposes a pure API and never references UI or Lessons. Cross-module talk goes
through the typed `EventBus`, never direct references.
