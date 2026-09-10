# CI — validar los tests en Unity 6

El workflow [`.github/workflows/unity-tests.yml`](../.github/workflows/unity-tests.yml) compila el
proyecto y ejecuta los **tests EditMode** en Unity 6.0.6.0f1 real (vía [GameCI](https://game.ci))
en cada push a `main`/`claude/**` y en cada PR que toque `unity6/**`.

## 1. Requisito: licencia de Unity (una vez)

GameCI necesita activar Unity, así que hay que dar una licencia por *secrets* del repo
(**Settings ▸ Secrets and variables ▸ Actions**).

### Opción A — licencia Personal (gratuita, lo normal)

1. Genera el archivo de activación con el propio GameCI (workflow de una sola vez) o localmente, y
   consíguelo en Unity: sigue la guía oficial
   [game.ci/docs/github/activation](https://game.ci/docs/github/activation/) (pedir `.alf` →
   subir a [license.unity3d.com/manual](https://license.unity3d.com/manual) → descargar `.ulf`).
2. Crea el secret **`UNITY_LICENSE`** y pega **el contenido completo del `.ulf`** (es XML).

### Opción B — licencia Pro/Plus (serial)

Crea tres secrets en su lugar y deja `UNITY_LICENSE` sin definir:
`UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_SERIAL`.

## 2. Ejecutar

- Automático: haz push a la rama `claude/missing-plans-l7bknn` (o abre el PR) — el job corre solo.
- Manual: pestaña **Actions ▸ Unity Tests (EditMode) ▸ Run workflow**.

Los resultados aparecen como *check* en el commit/PR ("EditMode test results") y como artefacto
descargable (`editmode-results`, un NUnit XML con cada test).

## Qué valida

Los tests EditMode de las Fases 1-2 (todos headless, sin GPU ni escena):

- **Core:** DI, round-trip de guardado, audio reacciona al bus, haptics no-op.
- **Lessons:** parser JSON, evaluador (fixtures → 3/2/1 coronas), runner de lección.
- **MetaGame:** progresión, desbloqueos, glosario, blackbook, tienda.
- **Engine:** `TileDirtyTracker` (ENG-06).
- **UI:** máquina de estados + presenters MVP.
- **Social:** export PNG, watermark, share, mockups, timeline de replay.
- **Content:** integridad de las 10 lecciones + cobertura del glosario.
- **Smokes:** `SliceSmokeEditTests` (Fase 1) y `MvpSmokeEditTests` (Fase 2), el bucle completo.

## Lo que el CI NO cubre (a propósito)

Rutas GPU, escena/prefabs, plugins nativos (MP4, share nativo) y dispositivo. Eso se valida
abriendo el proyecto en el editor y en un iPhone 11 / Android gama media — ver
[`unity6/Assets/_Project/MODULES.md`](../unity6/Assets/_Project/MODULES.md).

## Correr los tests localmente (sin CI)

1. Abre `unity6/` con Unity 6.0.6.0f1.
2. **Window ▸ General ▸ Test Runner**.
3. Pestaña **EditMode ▸ Run All**.

O por línea de comandos:

```bash
Unity -batchmode -runTests -projectPath unity6 -testPlatform EditMode \
  -testResults results.xml -logFile - -quit
```
