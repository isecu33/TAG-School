#!/usr/bin/env python3
"""Compile check del proyecto Unity SIN licencia ni editor de Unity.

Genera un .csproj por cada .asmdef de unity6/Assets (respetando sus referencias, así detecta
también referencias de asmdef que faltan) y lo compila con `dotnet build` contra DLLs de
referencia de UnityEngine/UnityEditor descargadas de NuGet. Los paquetes que no están en NuGet
(uGUI, Input System, Test Runner) se sustituyen por stubs mínimos en ./stubs.

Limitaciones (es un filtro rápido, no sustituye a Unity):
  - Las DLLs de referencia son de Unity 2021.3/2021.1: una API exclusiva de Unity 6 daría un
    falso error (y se vería claramente en el log).
  - No compila shaders ni valida escenas/prefabs ni serialización.

Uso:
  python3 tools/unity-compile-check/compile_check.py            # compila todo
  python3 tools/unity-compile-check/compile_check.py --keep     # deja los .csproj en build/
Requisitos: .NET SDK 8 (`apt-get install dotnet-sdk-8.0`) y acceso a api.nuget.org.
"""
import json
import os
import shutil
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
ASSETS = REPO / "unity6" / "Assets"
CACHE = HERE / ".cache"
BUILD = HERE / "build"

PACKAGES = {
    # nombre → (url del nupkg, prefijo de las DLLs dentro del zip)
    "modules": ("https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg", "lib/net45/"),
    "editor": ("https://api.nuget.org/v3-flatcontainer/unity3d.sdk/2021.1.14.1/unity3d.sdk.2021.1.14.1.nupkg", "lib/"),
}

# Símbolos del editor (los asmdef de runtime también compilan con UNITY_EDITOR dentro del editor).
DEFINES = "UNITY_EDITOR;UNITY_2021_3_OR_NEWER;ENABLE_LEGACY_INPUT_MANAGER;UNITY_INCLUDE_TESTS;DEBUG;TRACE"

# Referencias de asmdef que no son del proyecto → stub local (o se ignoran si no hacen falta).
EXTERNAL = {
    "UnityEngine.UI": "UnityEngine.UI",
    "Unity.InputSystem": "Unity.InputSystem",
    "UnityEngine.TestRunner": "UnityEngine.TestRunner",
    "UnityEditor.TestRunner": None,
}


def fetch_refs() -> Path:
    refs = CACHE / "refs"
    if (refs / "UnityEngine.CoreModule.dll").exists() and (refs / "UnityEditor.dll").exists():
        return refs
    refs.mkdir(parents=True, exist_ok=True)
    for name, (url, prefix) in PACKAGES.items():
        pkg = CACHE / f"{name}.nupkg"
        if not pkg.exists():
            print(f"Descargando {url}")
            urllib.request.urlretrieve(url, pkg)
        with zipfile.ZipFile(pkg) as z:
            for n in z.namelist():
                if n.startswith(prefix) and n.endswith(".dll") and "/" not in n[len(prefix):]:
                    target = refs / Path(n).name
                    if name == "editor" and Path(n).name == "UnityEngine.dll":
                        continue  # el facade bueno es el de los módulos 2021.3
                    target.write_bytes(z.read(n))
    return refs


def find_asmdefs():
    defs = {}
    for p in ASSETS.rglob("*.asmdef"):
        d = json.loads(p.read_text(encoding="utf-8-sig"))
        defs[d["name"]] = {"dir": p.parent, "data": d}
    # cada .cs pertenece al asmdef más profundo que lo contiene
    dirs = sorted(((v["dir"], k) for k, v in defs.items()), key=lambda t: len(str(t[0])), reverse=True)
    files = {k: [] for k in defs}
    for cs in ASSETS.rglob("*.cs"):
        for d, name in dirs:
            if d in cs.parents:
                files[name].append(cs)
                break
    return defs, files


def csproj(name, sources, project_refs, dll_refs, extra=""):
    comp = "\n".join(f'    <Compile Include="{s}" />' for s in sorted(map(str, sources)))
    prefs = "\n".join(f'    <ProjectReference Include="{p}" />' for p in project_refs)
    drefs = "\n".join(
        f'    <Reference Include="{d.stem}"><HintPath>{d}</HintPath><Private>false</Private></Reference>'
        for d in dll_refs
    )
    return f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>{name}</AssemblyName>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <DefineConstants>{DEFINES}</DefineConstants>
    <NoWarn>CS0067;CS0168;CS0169;CS0414;CS0649;CS1701;CS1702;MSB3277</NoWarn>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <ItemGroup>
{comp}
  </ItemGroup>
  <ItemGroup>
{prefs}
  </ItemGroup>
  <ItemGroup>
{drefs}
  </ItemGroup>
{extra}
</Project>
"""


def main():
    keep = "--keep" in sys.argv
    refs = fetch_refs()
    dlls = sorted(refs.glob("*.dll"))
    if BUILD.exists():
        shutil.rmtree(BUILD)
    BUILD.mkdir(parents=True)

    stub_projects = {}
    for stub in (HERE / "stubs").iterdir():
        if stub.is_dir():
            out = BUILD / stub.name / f"{stub.name}.csproj"
            out.parent.mkdir(parents=True)
            out.write_text(csproj(stub.name, list(stub.glob("*.cs")), [], dlls))
            stub_projects[stub.name] = out

    defs, files = find_asmdefs()
    projects = {}
    for name, info in defs.items():
        prefs = []
        extra = ""
        for r in info["data"].get("references", []):
            r = r.replace("GUID:", "")
            if r in defs:
                prefs.append(BUILD / r / f"{r}.csproj")
            elif r in EXTERNAL:
                if EXTERNAL[r]:
                    prefs.append(stub_projects[EXTERNAL[r]])
            else:
                print(f"AVISO: {name} referencia '{r}', que no es del proyecto ni tiene stub")
        if "nunit.framework.dll" in info["data"].get("precompiledReferences", []):
            extra = '  <ItemGroup><PackageReference Include="NUnit" Version="3.13.3" /></ItemGroup>'
        out = BUILD / name / f"{name}.csproj"
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(csproj(name, files[name], prefs, dlls, extra))
        projects[name] = out

    failed = []
    for name, proj in sorted(projects.items()):
        r = subprocess.run(
            ["dotnet", "build", str(proj), "-nologo", "-clp:NoSummary;ErrorsOnly", "-v:q"],
            capture_output=True, text=True,
        )
        errors = sorted({l.strip() for l in (r.stdout + r.stderr).splitlines() if "error" in l})
        status = "OK " if r.returncode == 0 else "ERR"
        print(f"[{status}] {name} ({len(files[name])} ficheros)")
        for e in errors:
            print("      " + e.replace(str(REPO) + "/", ""))
        if r.returncode != 0:
            failed.append(name)

    if not keep:
        shutil.rmtree(BUILD, ignore_errors=True)
    print(f"\n{len(projects) - len(failed)}/{len(projects)} ensamblados compilan")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
