"""Regenerate PSP IFF schemas from the static reader/consumer evidence.

Run from any directory with Python 3. Outputs only Schemas/Defaults/*.PSP.json.
See docs/psp-iff.md for evidence addresses, limitations, and field naming.
"""

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "PangyaAPI/PangyaAPI.IFF/Schemas/Defaults"
SIZES = {
    "Character": 0x160, "Parts": 0x1CC, "Club": 0xAC, "ClubSet": 0xB0,
    "Ball": 0x2E4, "Item": 0xAC, "Caddie": 0xB0, "CaddieItem": 0xD0,
    "SetItem": 0xAC, "Course": 0x11C, "Title": 0x58, "Skin": 0xAC,
    "HairStyle": 0x78, "AuxPart": 0xC4, "QuestDrop": 0xD8,
    "Quest": 0xD8, "Desc": 0x204,
}


def field(name, offset, width, kind="UInt32"):
    result = dict(name=name, offset=offset, width=width, type=kind, isEditable=True)
    if kind in ("FixedString", "LongString"):
        result["encodingCodePage"] = 949
    return result


def strings(name, offset, count, width=40):
    return [field(f"{name}{i + 1}", offset + i * width, width, "FixedString")
            for i in range(count)]


def stats(offset, kind="Int32"):
    # Order is confirmed; individual gameplay stat names have not been traced.
    width = 2 if kind == "Int16" else 4
    return [field(f"Stat{i + 1}", offset + i * width, width, kind) for i in range(5)]


def write(name, size, fields, inherit=False):
    definition = dict(schemaVersion=2, defaultRevision=1, fileName=f"{name}.iff",
                      region="PSP", minimumRecordSize=size, isEditable=True,
                      defaultStringSize=40, keyField="ItemId", fields=fields)
    if inherit:
        definition["base"] = dict(name="Common", region="PSP")
    (OUTPUT / f"{name}.PSP.json").write_text(
        json.dumps(definition, indent=2) + "\n", encoding="utf-8")


def main():
    write("Common", 0x30, [field("Enabled", 0, 1, "Byte"),
                           field("ItemId", 4, 4), field("Name", 8, 40, "FixedString")])
    local = {
        "Character": [field("Model", 0x74, 40, "FixedString"), *stats(0x114),
                      field("PrimarySlotCount", 0x128, 1, "Byte"),
                      field("AdditionalSlotCount", 0x129, 1, "Byte")],
        "Parts": [field("Model", 0x74, 40, "FixedString"),
                  field("SlotMask", 0xA0, 4), *strings("Texture", 0xA8, 3),
                  *strings("OriginalTexture", 0x120, 3)],
        "ClubSet": stats(0x84), "Ball": stats(0x2D4, "Int16"),
        "AuxPart": stats(0x88), "Caddie": stats(0xA0, "Int16"),
        "Course": [field("MapAsset", 0x38, 40, "FixedString"),
                   field("PropertyFile", 0xC8, 40, "FixedString")],
        "HairStyle": [field("CharacterId", 0x75, 1, "Byte")],
    }
    for name, size in SIZES.items():
        if name == "Desc":
            write(name, size, [field("ItemId", 0, 4),
                               field("Description", 4, 512, "LongString")])
        else:
            fields = local.get(name, []).copy()
            if name in {"Character", "Parts", "Club", "ClubSet", "Ball", "Item",
                        "Caddie", "CaddieItem", "SetItem", "Course", "HairStyle"}:
                fields += [field("Requirement", 0x30, 4)]
            write(name, size, fields, inherit=True)


if __name__ == "__main__":
    main()
