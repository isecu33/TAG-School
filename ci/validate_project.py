#!/usr/bin/env python3
"""
License-free sanity checks for the unity6/ project — catches the most common compile-breakers
and content errors without needing Unity or a license. Real compilation still runs in the GameCI
workflow once UNITY_LICENSE is set; this gives fast, always-on signal on every push/PR.

Checks:
  1. Every .asmdef is valid JSON, has a name, and its PieceBook.* references resolve.
  2. Every JSON (manifest, lockfile, content) parses.
  3. Lesson JSONs have an id and steps of known types (§5).
  4. Architecture guards (ARQUITECTURA §3/§4):
       - DrawingEngine never uses SetPixels;
       - nobody references the isolated PieceBook.ARModule;
       - DrawingEngine does not reference UI or Lessons (pure API);
       - tests live under Assets/ (else Unity won't compile them).
Exit code 1 on any error.
"""
import glob
import json
import os
import sys

ROOT = "unity6"
errors = []


def load_json(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


# ---- 1. asmdefs ----------------------------------------------------------
asmdefs = glob.glob(f"{ROOT}/**/*.asmdef", recursive=True)
parsed = {}
defined = {}
for a in asmdefs:
    try:
        d = load_json(a)
    except Exception as e:  # noqa: BLE001
        errors.append(f"asmdef is not valid JSON: {a}: {e}")
        continue
    parsed[a] = d
    name = d.get("name")
    if not name:
        errors.append(f"asmdef has no 'name': {a}")
    else:
        defined[name] = a

for a, d in parsed.items():
    for ref in d.get("references", []):
        if ref.startswith("GUID:"):
            continue  # GUID refs can't be resolved here; skip
        if ref.startswith("PieceBook.") and ref not in defined:
            errors.append(f"{a}: references undefined project assembly '{ref}'")

# ---- 2. JSON parses ------------------------------------------------------
json_files = glob.glob(f"{ROOT}/Assets/**/*.json", recursive=True)
for extra in (f"{ROOT}/Packages/manifest.json", f"{ROOT}/Packages/packages-lock.json"):
    if os.path.exists(extra):
        json_files.append(extra)
for j in json_files:
    try:
        load_json(j)
    except Exception as e:  # noqa: BLE001
        errors.append(f"invalid JSON: {j}: {e}")

# ---- 3. lesson schema ----------------------------------------------------
STEP_TYPES = {"showcase", "trace", "freeform", "quiz"}
for j in sorted(glob.glob(f"{ROOT}/Assets/_Project/Content/Lessons/*.json")):
    try:
        d = load_json(j)
    except Exception:  # noqa: BLE001
        continue  # already reported above
    if not d.get("id"):
        errors.append(f"lesson missing 'id': {j}")
    steps = d.get("steps")
    if not isinstance(steps, list) or not steps:
        errors.append(f"lesson has no steps: {j}")
    else:
        for k, s in enumerate(steps):
            t = s.get("type")
            if t not in STEP_TYPES:
                errors.append(f"lesson {j}: step {k} has unknown type '{t}' (§5)")

# ---- 4. architecture guards ---------------------------------------------
for cs in glob.glob(f"{ROOT}/Assets/_Project/DrawingEngine/**/*.cs", recursive=True):
    with open(cs, "r", encoding="utf-8") as f:
        for i, line in enumerate(f, 1):
            code = line.split("//", 1)[0]
            if "SetPixels(" in code:
                errors.append(f"DrawingEngine uses SetPixels ({cs}:{i}) — forbidden (§4)")

for a, d in parsed.items():
    name = d.get("name")
    refs = d.get("references", [])
    if name != "PieceBook.ARModule" and "PieceBook.ARModule" in refs:
        errors.append(f"{a} references PieceBook.ARModule — it must stay isolated (§3)")
    if name == "PieceBook.DrawingEngine":
        for bad in ("PieceBook.UI", "PieceBook.Lessons"):
            if bad in refs:
                errors.append(f"DrawingEngine references {bad} — its API must stay pure (§3)")

if not os.path.isdir(f"{ROOT}/Assets/Tests"):
    errors.append("missing unity6/Assets/Tests — tests must live under Assets/ to compile")
if os.path.isdir(f"{ROOT}/Tests"):
    errors.append("unity6/Tests exists outside Assets/ — Unity would ignore those tests")

# ---- report --------------------------------------------------------------
if errors:
    print(f"PROJECT VALIDATION FAILED ({len(errors)} issue(s)):")
    for e in errors:
        print(f"  - {e}")
    sys.exit(1)

print(f"Project validation passed: {len(defined)} assemblies, {len(json_files)} JSON files checked.")
