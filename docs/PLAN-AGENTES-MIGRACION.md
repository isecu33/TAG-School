# Plan de agentes — migración a `unity6/` (listo para ejecutar)

Complementa [`MIGRACION-UNITY6.md`](MIGRACION-UNITY6.md). Aquí están los **prompts exactos** de
cada subagente. Cuando el shell de `unity6/` esté subido, ejecutar = lanzar estos agentes y luego
la integración final. Nada que rediseñar.

- **Modelo de cada agente:** `haiku` (barato; no consume los tokens caros de la sesión).
- **Tipo:** `general-purpose` (necesitan Bash para `cp`/`diff`).
- **Ejecución:** en paralelo (cada agente escribe en una subcarpeta distinta de `unity6/`, sin
  solaparse). El copiado es **determinista con `cp`**, no reescritura → sin riesgo de alucinación.

## Orquestación (qué hace quién)

1. **Usuario:** sube el shell `unity6/` (Unity 6000.6.0f1, URP). → hecho aparte.
2. **Yo (orquestador):** `git pull`; leo `unity6/ProjectSettings/ProjectVersion.txt`; **lanzo los
   9 agentes**; espero sus reportes.
3. **Yo (integración):** parcheo `unity6/Packages/manifest.json` (paquetes §3 del plan), copio el
   `.gitignore` si hace falta, actualizo el CI (`projectPath: unity6`, `unityVersion` real), commit
   + push. Validación en Test Runner / CI.

## Contrato común (aplica a todos los agentes)

> Trabajas en el repo `/home/user/TAG-School`. Migras código del proyecto Unity 2022 (`unity/`) al
> nuevo proyecto Unity 6 (`unity6/`). **Regla de oro: copia VERBATIM con `cp`. No reescribas, no
> edites, no “mejores” ningún fichero.** El código es agnóstico de versión (ya verificado; sin APIs
> rotas en Unity 6). **No** crees ficheros `.meta`. **No** toques `ProjectSettings/`, `Packages/`,
> ni carpetas de otros módulos. **No** hagas `git add/commit/push` (de eso me encargo yo).
> Al terminar, **reporta**: comando(s) ejecutados, salida de la verificación `diff`, y nº de
> ficheros copiados. Si el `diff` no sale vacío, dilo explícitamente y no intentes arreglarlo.

---

## Agente 1 — `M-CORE`

```
Migra el módulo Core de TAG-School de unity/ a unity6/ (copia verbatim, no reescribas nada).
Ejecuta:
  mkdir -p unity6/Assets/_Project/Core
  cp -a unity/Assets/_Project/Core/. unity6/Assets/_Project/Core/
Verifica que es idéntico:
  diff -r unity/Assets/_Project/Core unity6/Assets/_Project/Core
La verificación debe salir vacía. Reporta el nº de .cs/.asmdef copiados
(find unity6/Assets/_Project/Core -type f | wc -l) y la salida del diff.
No crees .meta, no toques otros módulos, no hagas commit.
```

## Agente 2 — `M-ENGINE`

```
Migra el módulo DrawingEngine (incluye Shaders/) de TAG-School de unity/ a unity6/, copia verbatim.
Ejecuta:
  mkdir -p unity6/Assets/_Project/DrawingEngine
  cp -a unity/Assets/_Project/DrawingEngine/. unity6/Assets/_Project/DrawingEngine/
Verifica:
  diff -r unity/Assets/_Project/DrawingEngine unity6/Assets/_Project/DrawingEngine
Debe salir vacío. Confirma que existen los shaders unity6/Assets/_Project/DrawingEngine/Shaders/
SprayStamp.shader y LayerComposite.shader. Reporta nº de ficheros y salida del diff.
No crees .meta, no toques otros módulos, no hagas commit.
```

## Agente 3 — `M-LESSONS`

```
Migra el módulo Lessons de TAG-School de unity/ a unity6/, copia verbatim.
Ejecuta:
  mkdir -p unity6/Assets/_Project/Lessons
  cp -a unity/Assets/_Project/Lessons/. unity6/Assets/_Project/Lessons/
Verifica:
  diff -r unity/Assets/_Project/Lessons unity6/Assets/_Project/Lessons
Debe salir vacío. Reporta nº de ficheros y salida del diff.
No crees .meta, no toques otros módulos, no hagas commit.
```

## Agente 4 — `M-META`

```
Migra el módulo MetaGame de TAG-School de unity/ a unity6/, copia verbatim.
Ejecuta:
  mkdir -p unity6/Assets/_Project/MetaGame
  cp -a unity/Assets/_Project/MetaGame/. unity6/Assets/_Project/MetaGame/
Verifica:
  diff -r unity/Assets/_Project/MetaGame unity6/Assets/_Project/MetaGame
Debe salir vacío. Reporta nº de ficheros y salida del diff.
No crees .meta, no toques otros módulos, no hagas commit.
```

## Agente 5 — `M-SOCIAL`

```
Migra el módulo Social de TAG-School de unity/ a unity6/, copia verbatim.
Ejecuta:
  mkdir -p unity6/Assets/_Project/Social
  cp -a unity/Assets/_Project/Social/. unity6/Assets/_Project/Social/
Verifica:
  diff -r unity/Assets/_Project/Social unity6/Assets/_Project/Social
Debe salir vacío. Reporta nº de ficheros y salida del diff.
No crees .meta, no toques otros módulos, no hagas commit.
```

## Agente 6 — `M-UI`

```
Migra el módulo UI de TAG-School de unity/ a unity6/, copia verbatim.
Ejecuta:
  mkdir -p unity6/Assets/_Project/UI
  cp -a unity/Assets/_Project/UI/. unity6/Assets/_Project/UI/
Verifica:
  diff -r unity/Assets/_Project/UI unity6/Assets/_Project/UI
Debe salir vacío. Reporta nº de ficheros y salida del diff.
No crees .meta, no toques otros módulos, no hagas commit.
```

## Agente 7 — `M-CONTENT`

```
Migra el módulo Content de TAG-School (incluye Lessons/*.json, Glossary/*.json y Editor/) de unity/
a unity6/, copia verbatim.
Ejecuta:
  mkdir -p unity6/Assets/_Project/Content
  cp -a unity/Assets/_Project/Content/. unity6/Assets/_Project/Content/
Verifica:
  diff -r unity/Assets/_Project/Content unity6/Assets/_Project/Content
Debe salir vacío. Confirma que existen los 10 JSON de lecciones en
unity6/Assets/_Project/Content/Lessons/ y unity6/Assets/_Project/Content/Glossary/glossary.json.
Reporta nº de ficheros y salida del diff. No crees .meta, no toques otros módulos, no hagas commit.
```

## Agente 8 — `M-SPIKE`

```
Migra el módulo Spike (incluye Spike/Editor/) de TAG-School de unity/ a unity6/, copia verbatim.
Ejecuta:
  mkdir -p unity6/Assets/_Project/Spike
  cp -a unity/Assets/_Project/Spike/. unity6/Assets/_Project/Spike/
Verifica:
  diff -r unity/Assets/_Project/Spike unity6/Assets/_Project/Spike
Debe salir vacío. Reporta nº de ficheros y salida del diff.
No crees .meta, no toques otros módulos, no hagas commit.
```

## Agente 9 — `M-TESTS` (reubica Tests dentro de Assets/)

```
Migra los tests de TAG-School. IMPORTANTE: en el proyecto viejo están en unity/Tests/ (FUERA de
Assets/, por eso Unity no los compilaba). En unity6 deben ir DENTRO de Assets. Copia verbatim.
Ejecuta:
  mkdir -p unity6/Assets/Tests
  cp -a unity/Tests/. unity6/Assets/Tests/
Verifica que el contenido es idéntico (solo cambia la ruta):
  diff -r unity/Tests unity6/Assets/Tests
Debe salir vacío. Confirma que existe el asmdef en
unity6/Assets/Tests/EditMode/PieceBook.Tests.EditMode.asmdef y que hay ~10 ficheros *EditTests.cs.
Reporta nº de ficheros y salida del diff. No crees .meta, no toques nada fuera de Assets/Tests,
no hagas commit.
```

## Extra — `M-DOCS` (lo hago yo, trivial)

`cp -a unity/Assets/_Project/MODULES.md unity6/Assets/_Project/MODULES.md` (nota de estado del
proyecto; conviene actualizar rutas/versión tras la migración).

---

## Integración final (mía, tras los reportes)

1. **manifest:** añadir a `unity6/Packages/manifest.json` (si el template URP no los trae):
   `com.unity.inputsystem`, `com.unity.modules.imageconversion`, `com.unity.modules.jsonserialize`
   (URP, ugui y test-framework ya vienen). Versiones: las que resuelva Unity 6000.6.
2. **CI:** en `.github/workflows/unity-tests.yml` → `projectPath: unity6`, `unityVersion:` = valor
   real de `unity6/ProjectSettings/ProjectVersion.txt`, y disparadores en `unity6/**`.
3. **Verificación global:** `diff -r unity/Assets/_Project unity6/Assets/_Project` (salvo MODULES.md)
   y `diff -r unity/Tests unity6/Assets/Tests` deben salir vacíos.
4. **commit + push** (autoría del usuario), y validar en Test Runner / CI (los tests ya aparecen).
5. Cuando `unity6/` valide, `unity/` (2022.3) se puede archivar/borrar.
```
