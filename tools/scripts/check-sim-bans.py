#!/usr/bin/env python3
"""Fail if Sim/Data sources use architecture §2.2 banned APIs.

This is the S8 CI gate for float-in-Sim (and related nondeterminism hazards).
A full Roslyn analyzer can replace/augment this later; the gate must stay green.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCAN_DIRS = [
    ROOT / "src" / "JewelBlitzArena.Sim",
    ROOT / "src" / "JewelBlitzArena.Data",
]

# Whole-word / API bans (architecture.md §2.2). Comments and strings are stripped first.
# Shared bans apply to Sim + Data. Unordered-collection bans are Sim-only: Data catalogs
# may key-lookup via Dictionary/HashSet (S5); iteration order must never drive MatchState.
BANS_SHARED: list[tuple[str, str]] = [
    (r"\bfloat\b", "float (use Fix / Q48.16)"),
    (r"\bdouble\b", "double (use Fix / Q48.16)"),
    (r"\bdecimal\b", "decimal (use Fix / integers)"),
    (r"\bSystem\.Math\b", "System.Math (use Sim math / Fix)"),
    (r"\bMath\.(Sin|Cos|Tan|Sqrt|Pow|Floor|Ceiling|Round|Abs)\b", "System.Math float API"),
    (r"\bSystem\.Random\b", "System.Random (use xoshiro streams in MatchState)"),
    (r"\bnew\s+Random\b", "new Random (use xoshiro streams in MatchState)"),
    (r"\bDateTime\b", "DateTime (non-deterministic)"),
    (r"\bStopwatch\b", "Stopwatch (non-deterministic)"),
    (r"\bEnvironment\.TickCount\b", "Environment.TickCount (non-deterministic)"),
    (r"string\.GetHashCode\s*\(|\"[^\"]*\"\.GetHashCode\s*\(", "string.GetHashCode() (randomized per process)"),
    (r"\bArray\.Sort\b", "Array.Sort (unstable across runtimes)"),
    (r"\bList<.*>\.Sort\b", "List.Sort (unstable across runtimes)"),
    (r"\bOrderBy\b", "LINQ OrderBy (unstable across runtimes)"),
    (r"\bParallel\.", "Parallel.* inside Sim"),
    (r"\bTask\.(Run|Factory)\b", "Task parallelism inside Sim"),
    (r"\bUnityEngine\b", "UnityEngine reference (Sim/Data must stay engine-agnostic)"),
]

BANS_SIM_ONLY: list[tuple[str, str]] = [
    (r"\bDictionary\s*<", "Dictionary<> (iteration order undefined)"),
    (r"\bHashSet\s*<", "HashSet<> (iteration order undefined)"),
]

SIM_DIR = ROOT / "src" / "JewelBlitzArena.Sim"

COMMENT_LINE = re.compile(r"//.*?$", re.M)
COMMENT_BLOCK = re.compile(r"/\*.*?\*/", re.S)
STRINGS = re.compile(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"')


def strip_noise(text: str) -> str:
    text = COMMENT_BLOCK.sub(" ", text)
    text = COMMENT_LINE.sub(" ", text)
    text = STRINGS.sub('""', text)
    return text


def main() -> int:
    files: list[Path] = []
    for d in SCAN_DIRS:
        if not d.is_dir():
            print(f"error: missing scan dir {d}", file=sys.stderr)
            return 2
        files.extend(sorted(d.rglob("*.cs")))

    violations: list[str] = []
    for path in files:
        raw = path.read_text(encoding="utf-8")
        rel = path.relative_to(ROOT).as_posix()
        bans = list(BANS_SHARED)
        try:
            path.relative_to(SIM_DIR)
            bans.extend(BANS_SIM_ONLY)
        except ValueError:
            pass
        for lineno, line in enumerate(raw.splitlines(), start=1):
            code = line
            if "//" in code:
                code = code[: code.index("//")]
            code = STRINGS.sub('""', code)
            for pattern, label in bans:
                if re.search(pattern, code):
                    violations.append(f"{rel}:{lineno}: banned {label}")

    if violations:
        print("Sim/Data ban check FAILED:")
        for v in violations:
            print(f"  {v}")
        print(f"{len(violations)} violation(s)")
        return 1

    print(f"Sim/Data ban check OK ({len(files)} files scanned)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
