# _Project modules (ARQUITECTURA §8)

The folder map below mirrors §8. Phase 0 only implements what the spike needs; the rest
are intentional stubs so the structure — and the assembly boundaries — are established from
day one. Each module becomes its own asmdef when it gets real code.

| Folder | asmdef | Status in Phase 0 |
|---|---|---|
| `Core/` | `PieceBook.Core` | ✅ EventBus + drawing events |
| `DrawingEngine/` | `PieceBook.DrawingEngine` | ✅ Full spike engine (canvas, stamping, caps, drips, replay data) |
| `DrawingEngine/Editor` | *(none yet)* | — |
| `Content/` | *(assets only)* | ✅ Caps + paint (generated) |
| `Spike/` | `PieceBook.Spike` (+ `.Editor`) | ✅ Phase-0 harness — delete when the vertical slice lands |
| `Lessons/` | `PieceBook.Lessons` | ⏳ Phase 1 |
| `ARModule/` | `PieceBook.ARModule` | ⏳ Phase 3 (loaded via Addressables) |
| `Social/` | `PieceBook.Social` | ⏳ Phase 2/3 |
| `MetaGame/` | `PieceBook.MetaGame` | ⏳ Phase 1+ |
| `UI/` | `PieceBook.UI` | ⏳ Phase 1 (spike uses a placeholder HUD instead) |

Dependency rule (§3): everyone may depend on `Core`; nobody depends on `ARModule`; the
Drawing Engine exposes a pure API and never references UI or Lessons. Cross-module talk goes
through the typed `EventBus`, never direct references.
