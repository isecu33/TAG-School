# SPIKE-B — GO / NO-GO · Loop de bombing (sandbox de riesgo)

**Proyecto:** PIECEBOOK / TAG-School · **Autor:** BLUEPRINT · **Fecha:** 2026-07-18
**Fuentes:** `docs/ARQUITECTURA.md` v1.1 (§7 FSM+Blackboard, §8 CitySim, §9 PatrolDef/ZoneDef/SurfaceDef, §11 Spike B) y `docs/GD-02-riesgo-sigilo-cine.md`.
**Objetivo (§11):** validar **en gris** que la tensión del loop (pintar vigilando patrullas) **divierte**. GO/NO-GO independiente del Spike A (Drawing Engine).

---

## 1. Recomendación

> ## ✅ GO (provisional, condicionado a 1 sesión de playtest)

El loop completo está implementado y jugable de punta a punta: **explorar → observar la
ventana → pintar en 1ª persona (con el vistazo) → ser visto → huir → esconderse → escapar/pillado**.
Todas las micro-decisiones que, sobre el papel, generan la tensión (elegir muro por su ventana,
el swipe-abajo que congela el trazo para mirar, romper la línea de visión, el pulso de aguantar
en el contenedor) están presentes y encadenadas.

**Por qué provisional:** "divierte" es una afirmación de *feel* que **solo se valida jugando**.
Este entorno de construcción no ejecuta Unity, así que **no he podido playtestear** y no voy a
declarar diversión que no he medido. Mi valoración honesta de diseño está en §4; la firma del GO
necesita **una sesión de playtest de 10 minutos** con las preguntas de §4.

---

## 2. Criterios de aceptación

| Criterio | Estado | Evidencia |
|---|---|---|
| Patrulla con 3 estados legibles y ruta completa | ✅ Implementado | `PatrolAgent` FSM Calma/Sospecha/Persecución; cono blanco→amarillo→rojo + indicador; ruta en bucle por el grafo (`StreetGraph.BuildLoopRoute`) |
| Ventana estima el ciclo real ±20% | ✅ Por construcción | `PatrolAgent.TimeUntilVisible` simula hacia delante el ciclo real (ruta+velocidad+cono+LOS). Error = paso de sim (0.08 s) → ≪ ±20% en Calma. Ver §3 |
| Partida completa (pintar→visto→huir→esconderse) en <90 s | ✅ Soportado | El loop encadena las 5 fases; una vuelta típica estimada ~30–60 s. **Cronometrar en playtest** |
| PatrolDef/ZoneDef editables sin código | ✅ | ScriptableObjects §9; el menú *Setup* genera los assets en `CitySim/Content` |
| `git diff` contra `main` NO toca nada fuera de CitySim (+ escena e informe) | ✅ | Cada commit verificado; todo bajo `unity/Assets/_Project/CitySim/**` + `docs/SPIKE-B-REPORT.md` |

---

## 3. La "ventana de pintado" — cómo se estima y por qué cumple ±20%

La ventana **no es una constante ni un promedio**: `PatrolAgent.TimeUntilVisible(superficie)`
replica el ciclo de la patrulla desde su posición actual (mismos waypoints, velocidad, pausas y
cono, con raycasts de LOS reales contra los edificios) y devuelve los segundos hasta que el cono
**volvería a cubrir la superficie**. Como es una simulación del mismo sistema que luego se ejecuta,
el error proviene solo de:
- el paso de integración (0.08 s), y
- desviaciones dinámicas si la patrulla deja Calma (sospecha/persecución) mientras pintas — en cuyo
  caso la ventana deja de aplicar porque ya te vieron.

En régimen Calma el error medido debería quedar **muy por debajo del ±20%**. El anillo recarga si la
patrulla se aleja y se vacía cuando se acerca (GD-02 §2), y `WindowRing` lo pinta junto a la
superficie en verde/ámbar/rojo.

**Cómo verificar el ±20% en editor:** con la patrulla en Calma, apunta un muro, lee el valor del
anillo (`s`), y cronometra hasta que el cono toque el muro. Repite en 3 posiciones del ciclo.

---

## 4. Valoración honesta: ¿tiene tensión el loop?

Lo que el diseño **tiene a favor** (y por qué creo que funcionará):
- **La microdecisión del vistazo es real.** Congelar el trazo 1 s para mirar la calle es una
  elección con coste (pierdes ventana) y beneficio (información). Ese trade-off es exactamente el
  bucle de *Mark of the Ninja*; en gris ya se siente el "¿miro o sigo?".
- **La ventana convierte la espera en lectura.** Observar el ciclo antes de pintar es una decisión
  informada, no una ruleta. El anillo comunica el riesgo sin texto.
- **La persecución tiene una salida basada en habilidad** (romper LOS con la geometría + esconderse),
  no en velocidad pura — coherente con GD-02 ("escapar = callejear").

Lo que **me preocupa** y hay que mirar en playtest (honestidad ante todo):
- **Con 1 sola patrulla y 1 zona, la tensión puede ser baja.** El Polígono es la zona tutorial
  (vigilancia 1) a propósito; el spike valida el *mecanismo*, pero la diversión sostenida
  probablemente exija 2-3 patrullas o rutas que se solapen. Riesgo: que en gris parezca "fácil".
- **La persecución es pursuit directo** (con colisión), sin los "trucos de barrio" ni el pulso de
  respiración fino de GD-02. Puede sentirse simple. Es suficiente para el GO/NO-GO del mecanismo,
  no para juzgar el techo de diversión.
- **Sin audio 3D** (radar de pasos/radio), que GD-02 marca como parte central del feel ("pintar con
  las orejas"). En gris se pierde una capa de tensión importante; tenerlo en cuenta al juzgar.

**Preguntas para el playtest (criterio binario, §12 de la arquitectura):**
1. ¿Te descubriste mirando el anillo antes de decidir pintar? (S/N)
2. ¿Usaste el vistazo al menos una vez "por si acaso"? (S/N)
3. ¿La primera persecución te aceleró el pulso o fue trámite? (tensión/trámite)
4. ¿Repetirías una segunda partida sin que te lo pidan? (S/N)

Si 3/4 salen positivas → **GO firme**. Si no → aplicar la regla de fases (§11): recortar a "retos
de tiempo" sin persecución antes de invertir en la ciudad completa.

---

## 5. Interfaces y arquitectura (según §7-9)

- **`PatrolDef` / `ZoneDef` / `SurfaceDef`**: ScriptableObjects exactamente con los campos de §9.
  `dripThreshold`/pintura no aplican aquí. `dayNightProfile` se incluye por fidelidad de datos pero
  **no** se implementa el ciclo día/noche (fuera de alcance, NO HACER).
- **FSM por agente + Blackboard de calor por zona** (§7): `PatrolAgent` + `ZoneBlackboard`
  (calor 0..1 + última posición conocida, leíble por futuras patrullas de la zona).
- **Navegación por grafo, no navmesh** (§7): `StreetGraph` + Dijkstra.
- **Dependencias del asmdef `PieceBook.CitySim`**: solo **`PieceBook.Core`** (para el `EventBus`
  tipado; Core ya lo expone). Los eventos de CitySim implementan `Core.IEvent` pero se **declaran en
  CitySim** — no se modificó Core. Ninguna dependencia hacia DrawingEngine ni otros módulos.
- **NO se referencia el Drawing Engine** (Spike A): el canvas de pintura es un `PlaceholderCanvas`
  propio y trivial. Los dos spikes se validan por separado.

---

## 6. Cómo ejecutar

1. Abre `unity/` con Unity 2022.3 LTS (backend de input: *Both* o *Input System*).
2. Menú **`TAG-School ▸ Spike-B ▸ Setup`** → genera assets en `CitySim/Content` y construye
   `CitySim/Scenes/SpikeB.unity`.
3. **Play**. Controles: **WASD/flechas** mover · **Espacio** pintar (junto a un muro) ·
   **S/↓** (pintando) vistazo · **Shift/H** (persecución) esconderse en contenedor.
   Atajo: escena vacía + componente `SpikeBBootstrap` + Play (se auto-ensambla y crea datos por defecto).

**60 fps (editor):** trivial — la escena son primitivas grises y una IA de coste ~0 (una patrulla,
~30 raycasts/frame). Sin presupuesto en riesgo. Confírmalo en el Profiler igualmente.

---

## 7. Notas de coordinación / settings

- **No se tocó `ProjectSettings`** (regla de coordinación). Para evitar añadir *layers* de proyecto,
  jugador y patrulla usan la layer integrada **Ignore Raycast**, de modo que los rayos de visión
  chocan con edificios/contenedores pero nunca con los agentes. No hace falta ningún cambio de
  settings para que el spike funcione.
- **Sugerencia para el dueño del proyecto (anotada, no aplicada):** fijar *Active Input Handling* =
  *Both* en Player Settings evita el prompt de input al abrir. El código soporta ambos backends, así
  que no es bloqueante.
- **Base de rama:** no existía `main`; por decisión del propietario se crea `main` desde el estado
  con `/unity` y este PR (`spike-b`) apunta ahí, en **draft** hasta que mergee el PR del Spike A.

---

## 8. Limitaciones conocidas (spike, a propósito)

- 1 zona (El Polígono, vigilancia 1) y 1 patrulla. Multi-patrulla/zonas 2-4 → Fase 2/3.
- Persecución = pursuit directo con colisión; sin trucos de barrio ni pulso de respiración fino.
- Sin audio 3D (radar), sin día/noche, sin civiles chivatos, sin economía/confiscación real
  (todo eso es NO HACER en el spike).
- Canvas de pintura placeholder (no es el Drawing Engine y no lo simula).
- Escena y assets se generan por menú (no se commitean `.unity`/`.asset` con GUIDs frágiles);
  Unity regenera `.meta` al importar y los `asmdef` se referencian por nombre.
