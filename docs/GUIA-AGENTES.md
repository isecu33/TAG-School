# GUÍA DE AGENTES — trabajo con agentes de bajo coste

**Autor:** BLUEPRINT (Arquitecto) · **Referenciada desde:** `ARQUITECTURA.md §12` ·
**Aplica a:** todos los planes en [`planes/`](planes/)

> **Regla de oro (§12):** cada tarea que BLUEPRINT delega debe caber en **< 2k tokens de
> contexto** y ser **verificable con un test o un criterio binario**.

Esta guía existe para que una tarea pueda ejecutarla un agente barato (o un humano con poco
contexto) sin leerse el repo entero, y para que el resultado se pueda aceptar o rechazar sin
opinión.

---

## 1. El arquitecto planifica; el ejecutor implementa

- **BLUEPRINT** mantiene la fuente de verdad (`ARQUITECTURA.md`) y descompone cada fase en
  tareas atómicas (los `PLAN-FASE-*.md`). No escribe la implementación.
- **El ejecutor** (agente/humano) recibe **una** tarea, su criterio de "hecho", y los `§`
  relevantes. No re-diseña: si la tarea contradice la arquitectura, lo **reporta**, no lo parchea.

## 2. Anatomía de una tarea delegable

Una tarea bien formada trae, y solo trae, esto:

1. **Código** (`XXX-NN`) y **título** de una frase.
2. **Qué**: una frase imperativa.
3. **Dónde**: la carpeta/asmdef exacta de `§8` y los archivos que puede tocar.
4. **Contrato**: los `§` que gobiernan (p. ej. la API de `§4.3`, el modelo de datos `§9`).
5. **Hecho cuando**: un criterio **binario** — un test que pasa, un `bool` observable, un
   archivo que existe con una forma concreta. Nunca "cuando quede bien".
6. **Dependencias**: qué tareas anteriores deben existir.

Si algo de esto falta o no cabe en ~2k tokens, la tarea está mal cortada: **se parte**, no se
delega a ciegas.

## 3. Criterios binarios: ejemplos buenos vs malos

| ❌ No verificable | ✅ Verificable |
|---|---|
| "El undo debe ir fluido." | "Tras 20 trazos, `Undo()`×20 deja la RT idéntica (hash) al canvas vacío; test EditMode." |
| "Añade sonido al spray." | "`BeginStroke` publica `StrokeStarted`; `AudioService` reproduce `sfx_spray_loop`; test PlayMode afirma que el `AudioSource` está `isPlaying` durante el trazo." |
| "Evalúa el trazo." | "`TraceEvaluator.Score(polyline, template)` devuelve 1-3 coronas; test con 3 polylines fixture da 3/2/1 respectivamente." |

## 4. Límites que el ejecutor NUNCA cruza

- No cambia la **API pública** (`§4.3`) ni el **modelo de datos** (`§9`) sin que BLUEPRINT
  actualice antes la arquitectura. Discrepancia detectada → se documenta como en
  `INFORME-GO-NOGO.md §5`, no se decide en silencio.
- No introduce **antipatrones** de `§7` (singletons dispersos, `Update()` para eventos, lógica
  en Views, strings mágicos).
- No rompe las **reglas de dependencia** de `§3` (nadie referencia `ARModule`; el Drawing Engine
  no conoce UI ni lecciones; todo cruce va por `EventBus`).
- No usa `SetPixels` ni genera **allocs por frame** en rutas de dibujo (`§4`).

## 5. Presupuesto de contexto (~2k tokens)

Para que una tarea quepa, el ejecutor debería necesitar leer **como mucho**:

- El bloque de la tarea en su `PLAN-FASE-*.md`.
- Los 1-2 `§` citados de `ARQUITECTURA.md`.
- Las **interfaces** (no las implementaciones) de los tipos que toca — p. ej. `IDrawingCanvas`,
  no `DrawingCanvas.cs` entero.

Si para hacer la tarea hay que leer tres implementaciones completas, la frontera del módulo está
mal (o la tarea es demasiado grande). Eso es señal para BLUEPRINT, no para el ejecutor.

## 6. Aceptación

Una tarea se acepta cuando (y solo cuando):

1. Su **criterio binario** pasa (test verde / comprobación objetiva).
2. Cumple la **Definition of Done común** de [`planes/README.md`](planes/README.md).
3. El diff toca **solo** los archivos que la tarea declaró.

Si el criterio no puede ejecutarse en el entorno (p. ej. mide en dispositivo físico, como la
tabla de `INFORME-GO-NOGO.md §3`), se entrega el instrumental + los pasos, y la casilla queda
**abierta y honesta** — nunca se reporta un número inventado.
