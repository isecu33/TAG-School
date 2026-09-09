# Plan de migración a Unity 6 (6000.6.0f1)

> **Versión exacta = la de `unity6/ProjectSettings/ProjectVersion.txt`** que suba el usuario.
> El objetivo es **6000.6.0f1**; el CI y cualquier referencia se fijan a lo que diga ese fichero
> tras el push, para no depender de una errata de versión.

**Objetivo:** llevar todo el código de las Fases 0-2 (hoy en `unity/`, Unity 2022.3) a un proyecto
nuevo **`unity6/`** en **Unity 6000.6.0f1 (URP)**, respetando la arquitectura (`docs/ARQUITECTURA.md`),
la estructura de módulos (`§8`) y las reglas de dependencia (`§3`).

**Autoría del trabajo:** commits a nombre del usuario (`isecu33`), como en el resto de la rama.

---

## 0. Por qué es de bajo riesgo

Un escaneo del código encontró que **no usamos ningún API que Unity 6 haya roto**:

- El único candidato, `CommandBuffer.DrawMeshInstanced` (`GpuStamper.cs`), **sigue siendo válido**
  en Unity 6 (lo obsoleto es el estático `Graphics.DrawMeshInstanced`, que no usamos).
- Los shaders usan `CGPROGRAM` + `#include "UnityCG.cginc"`, que Unity 6 aún compila (built-in y URP).
- Todo lo demás es API estándar y estable: `MonoBehaviour`, `ScriptableObject`, `RenderTexture`,
  `Texture2D`, `JsonUtility`, `ImageConversion`, Input System, NUnit.

Por tanto la migración es, en su mayoría, **copiar los módulos verbatim** + dos correcciones
estructurales (abajo). **No se reescribe lógica.**

## 1. Correcciones estructurales que SÍ se aplican

1. **Tests dentro de `Assets/`.** Hoy están en `unity/Tests/` (hermano de `Assets/`), fuera del
   área que Unity compila → nunca se ejecutaron. En `unity6/` van a **`unity6/Assets/Tests/`**.
2. **Manifest con los paquetes necesarios** (§4). El shell debe declararlos o no compila.

## 2. El shell (lo crea el usuario en el Hub)

Crear en Unity Hub un proyecto **Universal 3D (URP)** con **Unity 6000.6.0f1**, carpeta `unity6/`
dentro del repo, y subirlo a la rama `claude/missing-plans-l7bknn`. Debe incluir:
`unity6/ProjectSettings/`, `unity6/Packages/manifest.json`, `unity6/Assets/` (lo que genere el
template). No subir `Library/`, `Temp/`, `obj/` (ya los ignora `.gitignore`).

## 3. Paquetes requeridos en `unity6/Packages/manifest.json`

El template URP trae URP + uGUI + Test Framework. Hay que asegurar además (se añaden tras el push
si faltan; Unity resuelve la versión para 6000.0):

```jsonc
"com.unity.inputsystem": "...",              // StrokeInputController (presión Pencil/touch)
"com.unity.render-pipelines.universal": "...", // shaders del spray + URP asset
"com.unity.ugui": "...",                      // UI + TextMeshPro (integrado en Unity 6)
"com.unity.test-framework": "...",            // tests EditMode
"com.unity.modules.imageconversion": "1.0.0", // EncodeToPNG / LoadImage (Social)
"com.unity.modules.jsonserialize": "1.0.0"    // JsonUtility (parsers, save)
```

## 4. Módulos a migrar (una tarea de subagente por módulo)

Copia **verbatim** con operaciones de fichero deterministas (`cp`), **sin reescribir contenido**.
Origen `unity/Assets/_Project/<M>/` → destino `unity6/Assets/_Project/<M>/`.

| Tarea | Módulo | Origen → Destino | Depende de |
|---|---|---|---|
| `M-CORE`   | Core        | `_Project/Core/` | — |
| `M-ENGINE` | DrawingEngine | `_Project/DrawingEngine/` (incl. `Shaders/`) | Core |
| `M-LESSONS`| Lessons     | `_Project/Lessons/` | Core, DrawingEngine |
| `M-META`   | MetaGame    | `_Project/MetaGame/` | Core |
| `M-SOCIAL` | Social      | `_Project/Social/` | Core, DrawingEngine |
| `M-UI`     | UI          | `_Project/UI/` | Core, DrawingEngine, Lessons, MetaGame |
| `M-CONTENT`| Content     | `_Project/Content/` (JSON + `Editor/` builder) | todos los anteriores |
| `M-SPIKE`  | Spike       | `_Project/Spike/` (+ `Spike/Editor/`) | Core, DrawingEngine |
| `M-TESTS`  | Tests       | `unity/Tests/**` → **`unity6/Assets/Tests/**`** (reubica) | todos |
| `M-DOCS`   | Docs/meta   | `MODULES.md` → `unity6/Assets/_Project/MODULES.md` | — |

Notas:
- Los `.asmdef` se copian **tal cual** (referencias por nombre; no dependen de la versión).
- **No** se copian `.meta` (el repo no versiona `.meta`; Unity los regenera al abrir). Igual que en `unity/`.
- **No** se copia `unity/ProjectSettings` ni `unity/Packages` (los aporta el shell del Hub).

## 5. Definition of Done por tarea (criterio binario)

1. Todos los `.cs`, `.shader`, `.asmdef` y `.json` del módulo existen en el destino y son
   **idénticos** al origen (`diff -r` sin diferencias salvo la ruta).
2. `M-TESTS`: los tests quedan bajo `unity6/Assets/Tests/` (no fuera de `Assets/`).
3. No se añadió ni quitó lógica; `git diff` del contenido de cada fichero vs. su origen = vacío.

## 6. Validación final (tras integrar todos los módulos)

1. `unity6/` abre en Unity 6000.6.0f1 sin errores de compilación (consola limpia).
2. **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All** en verde (ahora los tests SÍ aparecen).
3. Actualizar el CI (`.github/workflows/unity-tests.yml`): `projectPath: unity6`,
   `unityVersion: 6000.6.0f1`.
4. Cuando `unity6/` esté validado, `unity/` (2022.3) puede archivarse o borrarse.
