# PLAN · Fase 3 — RA + Social + soft launch

**Autor:** BLUEPRINT · **Roadmap:** `ARQUITECTURA §11` (semanas 17-22) ·
**Convenciones:** [`README.md`](README.md) · [`../GUIA-AGENTES.md`](../GUIA-AGENTES.md)

## Objetivo (criterio de fase)

El jugador **plasma su obra terminada en una pared real** vía RA (AR Foundation), la **comparte**
con replay, su **progreso sincroniza** con Firebase entre dispositivos, y el juego está listo
para **soft launch**. El módulo RA es **opcional en runtime**: se carga por Addressables solo si
el dispositivo lo soporta, con fallback a los mockups de la Fase 2.

**Entra en alcance:** módulo `ARModule` (planos verticales, anclaje, captura, light estimation),
la capa de sync Firebase (Auth anónima, Firestore/Storage, Remote Config, Crashlytics/Analytics)
tras `IProgressRepo`, y el pipeline de release. **Fuera de alcance:** RA paint-over, wildstyle,
galería comunitaria (Fase 4).

## Reglas duras de esta fase (`§3`, `§10`)

- **Nadie referencia `ARModule` directamente** (`§3`): se descubre y carga por Addressables; el
  resto del juego funciona idéntico sin él.
- Cuenta **anónima por defecto**, link a Apple/Google después; **nada de datos de menores**
  (age gate + analítica reducida < 16, `§10`).
- Offline-first se mantiene: Firebase es **sync diferido** tras `IProgressRepo`, nunca la ruta
  crítica de juego.

## Hitos y camino crítico

```
AR-01 deteccion-planos ── AR-02 anclaje/escala ── AR-03 light-est ── AR-04 captura
   (todo detrás de) AR-00 carga-condicional-Addressables ── (fallback: SOC-05 mockups)
DATA-01 Auth-anon ── DATA-02 Firestore-repo ── DATA-03 Storage ── DATA-04 RemoteConfig
CI-01 Actions+Fastlane ── CI-02 Crashlytics/Analytics ── SOFT-01 soft-launch
```
🔴 **Camino crítico:** `AR-00 → AR-01 → AR-02 → AR-04 → SOFT-01`.

---

## Módulo RA (`§6`, `§8` `ARModule/`, asmdef `PieceBook.ARModule`)

### `AR-00` · Carga condicional por Addressables 🔴
*módulo:* `Assets/_Project/ARModule` · *depende de:* —
**Qué:** el módulo RA vive tras Addressables y solo se carga si `ARSession.state` indica soporte
(`§3`, `§6`); si no, el juego usa los mockups de pared (`SOC-05`). Nadie del resto del código
tiene una referencia de assembly a `ARModule`.
**Hecho cuando:** grep confirma que ningún asmdef (salvo tests dedicados) referencia
`PieceBook.ARModule`; en un dispositivo sin RA, la app arranca y ofrece mockups sin cargar el
módulo (checklist) y sin excepciones.

### `AR-01` · Detección de planos verticales 🔴
*módulo:* `Assets/_Project/ARModule` · *depende de:* `AR-00`
**Qué:** AR Foundation 5.x (ARKit + ARCore) detectando **planos verticales** para "pegar" la
pieza en una pared real (`§2`, `§6`).
**Hecho cuando:** en dispositivo, apuntar a una pared produce un plano vertical anclable
(checklist con captura); test EditMode del wrapper: mapear un `ARPlane` vertical fixture a un
"muro válido" del juego.

### `AR-02` · Anclaje, escala y rotación de la pieza 🔴
*módulo:* `Assets/_Project/ARModule` · *depende de:* `AR-01`
**Qué:** anclar la obra terminada al plano y permitir escalar/rotar con gestos (`§6`).
**Hecho cuando:** test EditMode: aplicar gestos de escala/rotación al presenter actualiza el
transform del ancla de forma determinista; en dispositivo, la pieza queda fija al mover el
teléfono (checklist).

### `AR-03` · Light estimation
*módulo:* `Assets/_Project/ARModule` · *depende de:* `AR-02`
**Qué:** integrar la pieza con la luz ambiente (multiplicar por luminancia estimada de
ARKit/ARCore, `§6`) para que no "flote".
**Hecho cuando:** test EditMode: dada una luminancia estimada, el material de la pieza recibe el
factor correcto; comparación visual antes/después en dispositivo (checklist).

### `AR-04` · Captura foto/vídeo con la pieza anclada 🔴
*módulo:* `Assets/_Project/ARModule` · *depende de:* `AR-02`
**Qué:** capturar foto o vídeo de la escena RA con la pieza anclada, entregándolo al módulo
`Social` para compartir (marca de agua de `SOC-04`).
**Hecho cuando:** la captura produce un archivo válido que `SOC-03` puede compartir; test de
integración con `Social` (mock de plugin) verifica el hand-off.

---

## Capa de datos / Firebase (`§2`, `§7`)

### `DATA-01` · Auth anónima (link diferido)
*módulo:* `Assets/_Project/Core` (o `Social`) · *depende de:* —
**Qué:** Firebase Auth anónima por defecto; link posterior a Apple/Google (`§10`); age gate que
reduce la analítica para < 16.
**Hecho cuando:** test (mock SDK): primer arranque crea usuario anónimo; el flujo de link
preserva el `uid`/progreso; con age gate < 16 los eventos sensibles no se emiten.

### `DATA-02` · `IProgressRepo` sobre Firestore (sync diferido) 🔴
*módulo:* `Assets/_Project/Core` · *depende de:* `DATA-01`, Fase 1 (`CORE-02`)
**Qué:** segunda implementación de `IProgressRepo` (patrón Repository, `§7`) que sincroniza el
`Progress` local (SQLite) con Firestore de forma **diferida**, con resolución de conflictos
last-write-wins por campo.
**Hecho cuando:** test (emulador/mock): editar offline y luego conectar reconcilia local↔remoto
sin perder coronas; el juego funciona idéntico sin red (offline-first, `§1`).

### `DATA-03` · Storage de obras compartidas
*módulo:* `Assets/_Project/Social` · *depende de:* `DATA-02`
**Qué:** subir PNG/MP4 a Firebase Storage y obtener el `sharedUrl` del `Artwork` (`§9`).
**Hecho cuando:** test (mock): subir una obra devuelve un `sharedUrl` no vacío que se persiste en
el `Artwork`.

### `DATA-04` · Remote Config + flags
*módulo:* `Assets/_Project/Core` · *depende de:* `DATA-01`
**Qué:** Remote Config para flags (p. ej. quitar marca de agua premium de `SOC-04`, activar
capítulos), con defaults locales para funcionar offline.
**Hecho cuando:** test: sin red, se usan los defaults; con valor remoto, éste gana; ningún flag
bloquea el arranque.

---

## Release / observabilidad

### `CI-01` · Pipeline de build (GitHub Actions + Fastlane) 🔴
*módulo:* tooling (`.github/`) · *depende de:* —
**Qué:** builds automáticas a TestFlight / Play Internal con Fastlane (`§2`); tests EditMode/
PlayMode headless como gate.
**Hecho cuando:** un push a la rama de release produce una build firmada en TestFlight/Play
Internal; el workflow falla si algún test está rojo.

### `CI-02` · Crashlytics + Analytics de aprendizaje
*módulo:* `Assets/_Project/Core` · *depende de:* `DATA-01`
**Qué:** Crashlytics + eventos custom de Firebase Analytics para "medir dónde abandona la gente
cada lección" (`§2`), respetando el age gate (`§10`).
**Hecho cuando:** test: completar/abandonar una lección emite el evento custom correcto (mock);
los crashes se reportan en dispositivo (checklist).

---

## Cierre de fase

### `SOFT-01` · Soft launch 🔴
*módulo:* proceso · *depende de:* todas las anteriores
**Qué:** publicar a un mercado limitado; validar retención, crashes y el embudo de lecciones.
**Hecho cuando:** build en soft launch con Crashlytics/Analytics reportando; RA funciona en
dispositivos compatibles y degrada a mockups en el resto (checklist); sin regresiones en el
smoke test de MVP (`MVP-01`).
