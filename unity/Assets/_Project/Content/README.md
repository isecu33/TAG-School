# Content (ScriptableObject catalogs)

Data-driven catalogs per ARQUITECTURA §8: caps, paints, alphabets, lessons, palettes.

For the Phase-0 spike this folder holds the spray caps + paint. Generate them with the
menu **TAG-School ▸ Setup Phase 0 Spike** (or *Create Content Assets Only*), which writes:

- `Cap_Skinny.asset` — cone 4°
- `Cap_Soft.asset` — cone 12°
- `Cap_Fat.asset` — cone 25°
- `Paint_Default.asset` — carries `dripThreshold`

If these assets are absent at runtime, `SprayDemoBootstrap` builds equivalent defaults in
memory (see `SpikeDefaults`), so the scene still runs with zero setup.
