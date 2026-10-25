#!/usr/bin/env python3
"""Read-only checks for this repository's pinned, project-local Agent Skills.

Standard library only; no subprocesses, downloads, installs, or writes. This
checks the portable name/description frontmatter subset, not arbitrary YAML.
It does not claim that a running Codex session has refreshed its skill index.
"""

from __future__ import annotations

import argparse
import ast
import hashlib
import json
import re
from pathlib import Path
from urllib.parse import unquote, urlsplit


UPSTREAM_NAMES = {
    "unity-csharp-scripting", "save-systems", "unity-build-pipeline", "create-game-assets"
}
PROJECT_SKILL = "idle-game-project-rules"
NAME_RE = re.compile(r"[a-z0-9]+(?:-[a-z0-9]+)*")
MD_LINK_RE = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
RESOURCE_RE = re.compile(r"`((?:references|scripts|assets)/[^`\s]+)`")


def frontmatter(text: str) -> dict[str, str]:
    """Read the flat fields/folded descriptions used by these five skills."""
    lines = text.splitlines()
    if not lines or lines[0] != "---":
        raise ValueError("missing YAML frontmatter")
    end = next((i for i in range(1, len(lines)) if lines[i] == "---"), None)
    if end is None:
        raise ValueError("unterminated YAML frontmatter")
    fields: dict[str, str] = {}
    index = 1
    while index < end:
        match = re.fullmatch(r"([a-z_]+):\s*(.*)", lines[index])
        if not match:
            raise ValueError("unsupported frontmatter field syntax")
        key, value = match.groups()
        if key in fields:
            raise ValueError("duplicate frontmatter key: " + key)
        index += 1
        if value in {">", ">-", "|", "|-"}:
            block = []
            while index < end and (not lines[index] or lines[index][0].isspace()):
                block.append(lines[index].strip())
                index += 1
            value = " ".join(block).strip()
        fields[key] = value.strip("\"'")
    return fields


def verify(root: Path) -> dict:
    errors: list[str] = []
    checks: list[str] = []

    def require(condition: bool, message: str) -> None:
        if not condition:
            errors.append(message)

    def local(relative: str) -> Path:
        path = root / relative
        if not path.resolve().is_relative_to(root):
            raise ValueError("path escapes project: " + relative)
        if path.is_symlink():
            raise ValueError("unexpected symlink: " + relative)
        return path

    lock_path = local("Docs/AgentSkills/upstream-lock.json")
    lock = json.loads(lock_path.read_text(encoding="utf-8"))
    require(lock.get("schema") == 1, "unsupported lock schema")
    require(bool(re.fullmatch(r"[0-9a-f]{40}", lock.get("commit", ""))), "invalid upstream SHA")
    require(lock.get("update_policy") == "manual-review-only", "unexpected update policy")
    require(lock.get("license") == "Apache-2.0", "review upstream license change")
    entries = lock.get("skills", [])
    require(len(entries) == 4 and {x["name"] for x in entries} == UPSTREAM_NAMES,
            "lock must contain exactly the four approved upstream skills")
    verified_files = 0
    for entry in entries:
        expected_dir = ".agents/skills/" + entry["name"]
        require(entry["local_path"] == expected_dir, "unexpected skill install path")
        directory = local(entry["local_path"])
        actual_files = {p.relative_to(directory).as_posix() for p in directory.rglob("*") if p.is_file()}
        require(actual_files == set(entry["files"]), "upstream file set changed: " + entry["name"])
        for relative, expected_hash in entry["files"].items():
            path = local(entry["local_path"] + "/" + relative)
            require(path.is_file(), "missing upstream file: " + str(path.relative_to(root)))
            if path.is_file():
                digest = hashlib.sha256(path.read_bytes()).hexdigest()
                require(digest == expected_hash, "upstream content changed: " + str(path.relative_to(root)))
                verified_files += 1
    require({x["source_path"] for x in lock.get("legal_files", [])} == {"LICENSE", "NOTICE"},
            "LICENSE and NOTICE must be pinned")
    for entry in lock.get("legal_files", []):
        path = local(entry["local_path"])
        require(path.is_file(), "missing upstream legal file: " + entry["local_path"])
        if path.is_file():
            require(hashlib.sha256(path.read_bytes()).hexdigest() == entry["sha256"],
                    "legal file changed: " + entry["local_path"])
    checks.append(f"pinned upstream: {verified_files} files plus LICENSE/NOTICE")

    base = local(".agents/skills")
    skill_files = sorted(base.glob("*/SKILL.md"))
    require({p.parent.name for p in skill_files} == UPSTREAM_NAMES | {PROJECT_SKILL},
            "expected exactly five installed skills, without the broad router")
    for path in skill_files:
        text = path.read_text(encoding="utf-8")
        fields = frontmatter(text)
        name, description = fields.get("name", ""), fields.get("description", "")
        require(set(fields) == {"name", "description"}, "review frontmatter keys: " + str(path))
        require(bool(NAME_RE.fullmatch(name)) and len(name) <= 64 and name == path.parent.name,
                "invalid skill name: " + str(path))
        require(0 < len(description) <= 1024 and "<" not in description and ">" not in description,
                "invalid description: " + str(path))
        require(len(text.splitlines()) < 500, "skill body too long: " + str(path))
        ui_path = path.parent / "agents/openai.yaml"
        if ui_path.is_file():
            ui = ui_path.read_text(encoding="utf-8")
            values = dict(re.findall(r'^  ([a-z_]+): "([^"\n]+)"$', ui, re.M))
            require(ui.startswith("interface:\n"), "invalid OpenAI UI metadata: " + name)
            require({"display_name", "short_description", "default_prompt"} <= values.keys(),
                    "incomplete OpenAI UI metadata: " + name)
            require(25 <= len(values.get("short_description", "")) <= 64,
                    "invalid short description: " + name)
            require("$" + name in values.get("default_prompt", ""), "invalid default prompt: " + name)
    checks.append(f"portable frontmatter and optional UI metadata: {len(skill_files)} skills")

    agents = local("AGENTS.md").read_text(encoding="utf-8")
    for path in skill_files:
        require(path.relative_to(root).as_posix() in agents, "AGENTS.md must route to " + path.parent.name)
    docs = [local("AGENTS.md"), *local("Docs/AgentSkills").rglob("*.md"), *base.rglob("*.md")]
    link_count = 0
    for path in docs:
        text = path.read_text(encoding="utf-8")
        # Python MIGRATIONS[v](data) inside a fenced example is not a link.
        prose = re.sub(r"^```[^\n]*\n.*?^```[ \t]*$", "", text, flags=re.M | re.S)
        for target in MD_LINK_RE.findall(prose):
            target = target.strip().strip("<>")
            url = urlsplit(target)
            if url.scheme or not url.path:
                continue
            resolved = (path.parent / unquote(url.path)).resolve()
            require(resolved.is_relative_to(root) and resolved.exists(),
                    f"missing/escaping local link in {path.relative_to(root)}: {target}")
            link_count += 1
        # references/* paths in bundled prose resolve from the owning skill,
        # even when mentioned inside references/, rather than from the page.
        owner = next((p.parent for p in skill_files if path.is_relative_to(p.parent)), None)
        if owner is not None:
            for relative in RESOURCE_RE.findall(text):
                resolved = (owner / relative).resolve()
                require(resolved.is_relative_to(root) and resolved.is_file(),
                        f"missing bundled dependency in {path.relative_to(root)}: {relative}")
    checks.append(f"AGENTS.md routing, bundled resources and {link_count} local Markdown links")
    for path in base.rglob("*.json"):
        json.loads(path.read_text(encoding="utf-8"))
    for path in base.rglob("*.py"):
        ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
    checks.append("bundled JSON and Python syntax; no tools executed")
    return {"ok": not errors, "skills": [p.parent.name for p in skill_files],
            "upstream_commit": lock["commit"], "checks": checks, "errors": errors,
            "native_codex_auto_activation_verified": False}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    try:
        report = verify(args.project_root.resolve())
    except (OSError, ValueError, KeyError, TypeError) as exc:
        report = {"ok": False, "errors": [str(exc)]}
    if args.json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        for check in report.get("checks", []):
            print("CHECK " + check)
        for error in report["errors"]:
            print("ERROR " + error)
        print("PASS" if report["ok"] else "FAIL")
        print("Native Codex auto-activation is a separate, unverified check.")
    return 0 if report["ok"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
