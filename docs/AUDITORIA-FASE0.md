# Auditoría Fase 0 — Repo TAG-School (Spikes A & B)

Auditoría de solo lectura sobre `main` (estado completo con ambos spikes). Se traduce en 5
work packages que se aplican en la rama `chore/audit-improvements` (PR contra `main`).

## Resumen

- **URP presente pero no asignado** → render roto (magenta) en checkout limpio. `MaterialFactory`
  resuelve `URP/Lit` sin pipeline URP activo; `ProjectSettings/` solo tiene `ProjectVersion.txt`.
- **Rama por defecto (`claude/…`) sin CitySim** (0 vs 31 archivos): recomendación de repo-admin
  poner `main` como default.
- **`docs/ARQUITECTURA.md` es v1.0**; el resto cita v1.1 (CitySim §7/§8/§9). Falta la fuente de verdad.
- **Sin tests de CitySim**; PlayMode vacío.
- **Cápsulas se hunden**: `CharacterController.center=(0,1,0)` vs pivote centrado → `center=Vector3.zero`.
- **Sin gizmos** del grafo/waypoints/rango de visión.
- Estructura sólida: asmdefs y dependencias correctos (§3), eventos struct sin allocs, data-driven real.

## Work packages

- **WP1 — URP & render (primero).** `MaterialFactory` pipeline-aware, setup de URP asset por
  menú de editor, shader `PieceBook/SprayStamp` en Always-Included. Debe preceder a WP2.
- **WP2 — Legibilidad & gizmos.** Fix `center` de cápsulas, luz de relleno/ambiente, `CitySimGizmos`
  (`OnDrawGizmos`: nodos/aristas/waypoints/facing/rango), z-offset y material URP unlit del cono/anillo.
- **WP3 — Estructura/docs/branch.** Commitear ARQUITECTURA v1.1; mover `LoopPhase` a `Loop/`;
  quitar cref muerto en `ObjectPool`; `.editorconfig`; recomendación default branch = `main`.
- **WP4 — Tests & CI.** Tests EditMode de CitySim (StreetGraph, VisionSensor, PaintingWindow,
  ZoneBlackboard); workflow GameCI EditMode.
- **WP5 — Correctitud.** Guardar `stampShader` null en `DrawingCanvas.Awake`; `Destroy` vs
  `DestroyImmediate`; validar en GPU el path de instancing (nota, requiere editor real).

Detalle completo por hallazgo (severidad, archivo, fix) en el PR de mejoras.
