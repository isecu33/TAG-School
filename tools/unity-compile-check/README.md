# Unity compile check (sin licencia)

Compila cada `.asmdef` de `unity6/Assets` con `dotnet build` contra DLLs de referencia de
UnityEngine/UnityEditor descargadas de NuGet. Detecta errores de C# y referencias de asmdef que
faltan sin abrir Unity ni tener licencia.

```bash
sudo apt-get install -y dotnet-sdk-8.0   # una vez
python3 tools/unity-compile-check/compile_check.py
```

- Las referencias son de Unity 2021.3 (módulos) y 2021.1 (UnityEditor): una API que solo exista en
  Unity 6 daría un falso error.
- uGUI, Input System y Test Runner no están en NuGet: `stubs/` declara solo lo que usa el proyecto.
  Si un fichero nuevo usa más API de uGUI, añádela al stub con la misma firma.
- Se compila con `UNITY_EDITOR` y `ENABLE_LEGACY_INPUT_MANAGER` (el código de Input System va
  bajo `#if ENABLE_INPUT_SYSTEM` y queda fuera).
- No sustituye a los tests EditMode en Unity (`.github/workflows/unity-tests.yml`).
