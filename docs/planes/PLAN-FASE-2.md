# PLAN · Fase 2 — MVP

**Autor:** BLUEPRINT · **Roadmap:** `ARQUITECTURA §11` (semanas 9-16) ·
**Convenciones:** [`README.md`](README.md) · [`../GUIA-AGENTES.md`](../GUIA-AGENTES.md)

## Objetivo (criterio de fase)

Un **MVP publicable sin RA ni cuentas**: **3 capítulos** completos (tag → throw-up → color),
**glosario** y **blackbook** navegables, **export de imagen y vídeo** de la obra, **share nativo**,
y **mockups fotográficos de pared** (persianas, trenes, halls) para "plasmar" sin cámara. Es el
"80 % del valor con el 20 % del riesgo" de `§6`.

**Entra en alcance:** completar `MetaGame` (blackbook, glosario, tienda con moneda), el módulo
`Social` (export imagen/vídeo, share, replay→MP4 offline), más contenido (2 capítulos nuevos,
más caps/alfabetos), y mockups de pared. **Fuera de alcance:** RA real y sync Firebase (Fase 3).

## Hitos y camino crítico

```
(Fase 1) ── SOC-01 replay→MP4 ── SOC-02 export-img ── SOC-03 share ── SOC-04 marca-agua
                                        └─ SOC-05 mockups-pared
META-03 blackbook ── META-04 glosario ── META-05 tienda+moneda
CNT-04 cap2 throw-up ── CNT-05 cap3 color ── CNT-06 alfabetos/caps extra
```
🔴 **Camino crítico:** `SOC-01 → SOC-02 → SOC-03 → MVP-01`.

---

## Social / Export (`§8` `Social/`, asmdef `PieceBook.Social`)

### `SOC-01` · Replay del `StrokeRecording` → MP4 (offline) 🔴
*módulo:* `Assets/_Project/Social` · *depende de:* Fase 1 (`ENG-05`)
**Qué:** re-renderizar el `StrokeRecording` (API `§4.3`, ya se graban inputs no frames) a MP4 a
cualquier resolución, offline (`§4.1`, `§6.4`). El vídeo del proceso es "el contenido más viral"
(`§2`).
**Hecho cuando:** test EditMode: dado un `StrokeRecording` fixture, el renderer produce un
número determinista de frames y un archivo MP4 no vacío; re-render de la misma grabación es
idéntico (determinismo).

### `SOC-02` · Export de imagen (PNG) 🔴
*módulo:* `Assets/_Project/Social` · *depende de:* Fase 1 (`ENG-05`)
**Qué:** `Flatten()` (API `§4.3`) → PNG a resolución de export, con el contexto de pared
(`wallContext` de `Artwork`, `§9`) compuesto debajo.
**Hecho cuando:** test EditMode: exportar un canvas produce un PNG de las dimensiones pedidas
cuyo hash es estable para el mismo canvas.

### `SOC-03` · Share nativo 🔴
*módulo:* `Assets/_Project/Social` · *depende de:* `SOC-01`, `SOC-02`
**Qué:** hoja de compartir nativa (iOS/Android) para PNG y MP4 (`§2` Native Share). Solo se
comparte **hacia fuera**; no hay galería interna en MVP (`§10`).
**Hecho cuando:** test EditMode del presenter: `Share(path)` invoca el plugin nativo (mock) con
la ruta correcta y el tipo MIME correcto; en dispositivo, hoja real (casilla de checklist).

### `SOC-04` · Marca de agua sutil en exports
*módulo:* `Assets/_Project/Social` · *depende de:* `SOC-02`
**Qué:** marca de agua discreta del juego en imagen y vídeo (`§6`, crecimiento orgánico).
**Hecho cuando:** test EditMode: el PNG exportado contiene la marca en la región esperada
(muestreo de píxeles); sin marca si el usuario tiene el flag premium (Remote Config, Fase 3, hoy
constante).

### `SOC-05` · Mockups de pared (sin RA)
*módulo:* `Assets/_Project/Social` + `Content/` · *depende de:* `SOC-02`
**Qué:** catálogo de fondos fotográficos con perspectiva (persianas, trenes en depósito legal,
halls, `§6`) sobre los que se plasma la obra terminada; funciona en cualquier dispositivo.
**Hecho cuando:** ≥ 3 mockups seleccionables; test EditMode: componer la obra sobre un mockup
respeta su transform de perspectiva declarado en el `ScriptableObject`.

---

## Meta-game (completo)

### `META-03` · Blackbook (bocetero)
*módulo:* `Assets/_Project/MetaGame` · *depende de:* Fase 1 (`CORE-02`)
**Qué:** colección paginada de obras del jugador (`blackbookPages[]` de `Progress`, `§9`),
persistida por `IProgressRepo`, con miniaturas.
**Hecho cuando:** test PlayMode: guardar una obra añade una página; reabrir el repo la recupera
con su miniatura.

### `META-04` · Glosario navegable
*módulo:* `Assets/_Project/MetaGame` + `Content/` · *depende de:* Fase 1 (`LES-04`)
**Qué:** `GlossaryEntry` de `§9` (`{id, term, definition, era, media?, relatedTerms[]}`)
navegable con enlaces cruzados; los quiz de lección referencian estas entradas.
**Hecho cuando:** test EditMode: cargar el glosario resuelve todos los `relatedTerms[]` a
entradas existentes (sin referencias colgantes) y cubre todos los `glossaryRefs[]` de las
lecciones publicadas.

### `META-05` · Tienda de desbloqueos + moneda
*módulo:* `Assets/_Project/MetaGame` · *depende de:* Fase 1 (`META-02`)
**Qué:** activar el `unlockCost` de `CapDef` (`§9`): una moneda blanda que se gana con coronas
compra caps/paints/alfabetos. Publica `ItemUnlocked` (`§3`).
**Hecho cuando:** test PlayMode: con saldo suficiente, comprar un ítem lo añade a
`unlockedItems`, descuenta el coste y publica el evento; con saldo insuficiente, no.

---

## Contenido (2 capítulos nuevos + extras)

### `CNT-04` · Capítulo 2 — throw-ups
*módulo:* `Assets/_Project/Content` · *depende de:* Fase 1 (`CNT-03`)
**Qué:** lecciones de throw-up (bubble/semi-wild, `§9` `AlphabetDef.style`), con sus templates,
`unlocks[]` y `glossaryRefs[]`.
**Hecho cuando:** las lecciones del Cap. 2 cargan y el runner las recorre end-to-end en un test
PlayMode; introducen al menos un cap nuevo desbloqueable.

### `CNT-05` · Capítulo 3 — color/fills
*módulo:* `Assets/_Project/Content` · *depende de:* `CNT-04`, Fase 1 (`ENG-05` multicapa)
**Qué:** lecciones de color: fill + outline + detalles usando las 4 capas de `§4.1` y `PaintDef`
con `opacity`/`glossiness` variados (`§9`).
**Hecho cuando:** una lección de color exige pintar en ≥ 2 capas y el `Flatten` resultante las
compone correctamente (test PlayMode).

### `CNT-06` · Caps y alfabetos extra
*módulo:* `Assets/_Project/Content` · *depende de:* `CNT-05`
**Qué:** ampliar el catálogo de caps/paints/alfabetos que los 3 capítulos desbloquean.
**Hecho cuando:** todos los `unlocks[]` referenciados por las lecciones publicadas resuelven a un
asset existente (sin referencias colgantes; test EditMode de integridad de catálogo).

---

## Cierre de fase

### `MVP-01` · Smoke test de MVP + checklist de tienda 🔴
*módulo:* `Assets/Tests/PlayMode` · *depende de:* todas las anteriores
**Qué:** test end-to-end de los 3 capítulos + export imagen y vídeo + share (mock) + un mockup de
pared; y checklist de requisitos de tienda de `§10` (juego educativo/creativo, muros legales,
age gate, analítica reducida < 16).
**Hecho cuando:** el test pasa en CI; el checklist de `§10` está todo marcado o justificado; una
build interna (TestFlight / Play Internal, `§2`) instala y arranca.
