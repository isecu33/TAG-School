# PlayMode tests

Reserved for runtime tests that need a live canvas (GPU stamping, zero-GC verification
under the Profiler, drip lifetime). Phase-0 logic tests live in `Tests/EditMode`.

Planned PlayMode checks:
- `GC.CollectionCount` stays flat across a synthetic 5-second stroke (ENG-03 acceptance).
- A stroke over a wet cell spawns ≥1 drip and it reaches the canvas bottom (ENG-04).
- `LatencyProbe.AverageMs < 30` under a scripted input burst (§4.2).
