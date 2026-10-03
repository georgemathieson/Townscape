#!/usr/bin/env python3
"""Creates missing Unity .meta files under Assets/ with deterministic GUIDs.

Unity would generate random GUIDs on first import, which makes cross-references (like the
scene's link to TownscapeBootstrap.cs) impossible to author outside the editor and causes churn
between machines. Here the GUID is derived from the asset's path, so it is stable and known.

    python3 tools/generate_meta.py          # create missing metas
    python3 tools/generate_meta.py --check  # exit 1 if any are missing or orphaned
    python3 tools/generate_meta.py --guid Assets/Townscape/Scripts/Runtime/TownscapeBootstrap.cs

Existing .meta files are never rewritten, so moving a file must move its .meta with it.
"""
import hashlib
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "Assets"

FOLDER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

IMPORTERS = {
    ".cs": """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
    ".asmdef": """fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
    ".shader": """fileFormatVersion: 2
guid: {guid}
ShaderImporter:
  externalObjects: {{}}
  defaultTextures: []
  nonModifiableTextures: []
  preprocessorOverride: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
}

TEXT = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

DEFAULT = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

TEXT_EXTENSIONS = {".md", ".txt", ".json", ".xml", ".csv", ".yaml"}


def guid_for(path: Path) -> str:
    relative = path.resolve().relative_to(ROOT).as_posix()
    return hashlib.md5(f"townscape:{relative}".encode("utf-8")).hexdigest()


def template_for(path: Path) -> str:
    if path.is_dir():
        return FOLDER
    if path.suffix in IMPORTERS:
        return IMPORTERS[path.suffix]
    if path.suffix in TEXT_EXTENSIONS:
        return TEXT
    return DEFAULT


def assets():
    for path in sorted(ASSETS.rglob("*")):
        if path.suffix == ".meta" or path.name.startswith(".") or path.name.endswith("~"):
            continue
        if any(part.startswith(".") or part.endswith("~") for part in path.relative_to(ASSETS).parts):
            continue
        yield path


def main(argv):
    if len(argv) == 3 and argv[1] == "--guid":
        print(guid_for(ROOT / argv[2]))
        return 0

    check = "--check" in argv
    missing, orphaned = [], []
    for path in assets():
        meta = path.with_name(path.name + ".meta")
        if not meta.exists():
            missing.append(path)
            if not check:
                meta.write_text(template_for(path).format(guid=guid_for(path)), encoding="utf-8")

    for meta in sorted(ASSETS.rglob("*.meta")):
        if not meta.with_name(meta.name[: -len(".meta")]).exists():
            orphaned.append(meta)

    for path in missing:
        print(("missing " if check else "created ") + str(path.relative_to(ROOT)) + ".meta")
    for meta in orphaned:
        print("orphaned " + str(meta.relative_to(ROOT)))

    return 1 if check and (missing or orphaned) else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
