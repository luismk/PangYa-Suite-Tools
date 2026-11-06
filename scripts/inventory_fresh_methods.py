"""Inventory out-of-line Fresh C++ methods without executing/building the sources.

Run from the repository root with Python 3. Existing audit columns are preserved
for the same source file, class, method and declaration line. This inventory is
deliberately not a coverage verdict: inline header methods need a separate audit.
"""

import argparse
import csv
import hashlib
import json
from pathlib import Path
import re


DECLARATION = re.compile(
    r"^[^\r\n;{}()]*?\b(?P<class>Fr\w+(?:::\w+)*)::(?P<method>operator\s*[^\s(]+|~?\w+)\s*\(", re.MULTILINE
)
FIELDS = ["File", "Line", "Class", "Method", "Signature", "Status",
          "Implementation", "Evidence", "Notes"]
AUDIT_FIELDS = ["Status", "Implementation", "Evidence", "Notes"]


def key(row):
    return tuple(row[field] for field in ["File", "Line", "Class", "Method"])


def declarations(path):
    source = path.read_text(encoding="utf-8", errors="replace")
    for match in DECLARATION.finditer(source):
        # Ignore declarations ending in ';', including forward declarations.
        # The source corpus uses ordinary parenthesized parameter lists here.
        opening = source.index("(", match.start(), match.end())
        depth = 1
        position = opening + 1
        while depth and position < len(source):
            if source[position] == "(":
                depth += 1
            elif source[position] == ")":
                depth -= 1
            position += 1
        if depth:
            raise ValueError(f"Unclosed parameter list: {path}:{match.start()}")
        tail = source[position:]
        if not re.match(r"\s*(?:(?:const|noexcept|override|final)\b\s*)*(?:\{|:)", tail):
            continue
        brace = tail.find("{")
        semicolon = tail.find(";")
        if brace < 0 or 0 <= semicolon < brace:
            continue
        signature = " ".join(source[match.start():position].split())
        yield {
            "File": path.name,
            "Line": str(source.count("\n", 0, match.start()) + 1),
            "Class": match["class"],
            "Method": match["method"],
            "Signature": signature,
            "Status": "unreviewed",
            "Implementation": "",
            "Evidence": "",
            "Notes": "",
        }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--reference", type=Path, default=Path(".codex-build/fresh-reference"))
    parser.add_argument("--output", type=Path, default=Path("docs/fresh-method-coverage.csv"))
    args = parser.parse_args()
    sources = sorted(args.reference.glob("fr*.cpp"))
    if not sources:
        parser.error(f"No fr*.cpp sources in {args.reference}")
    previous = {}
    if args.output.exists():
        with args.output.open(encoding="utf-8", newline="") as stream:
            previous = {key(row): row for row in csv.DictReader(stream)}
    rows = []
    files = []
    for source in sources:
        methods = list(declarations(source))
        for row in methods:
            if key(row) in previous:
                row.update({field: previous[key(row)].get(field, "") for field in AUDIT_FIELDS})
        rows.extend(methods)
        files.append({"file": source.name, "sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
                      "out_of_line_methods": len(methods)})
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=FIELDS, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)
    manifest = {"scope": "Out-of-line Fr-class definitions in fr*.cpp; header inline methods excluded",
                "reference_commit": "81a6969942fe007bcaccbb33b53b3081e9cee67f",
                "files": files, "method_count": len(rows)}
    args.output.with_suffix(".sources.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Inventoried {len(rows)} out-of-line methods in {len(sources)} files: {args.output}")
    print("Header inline methods and per-method implementation verification remain separate audit work.")


if __name__ == "__main__":
    main()
