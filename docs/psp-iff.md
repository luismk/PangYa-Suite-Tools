# PSP IFF support

The IFF Manager and `PangyaAPI.IFF` support the PSP version-1 tables inside
`PangYaPSP.iff`, a plain ZIP archive. Auto detects the PSP header; **PSP** is
also available in the schema selector. Each PSP schema has its own `.PSP.json`
file and never falls back to a PC layout. Existing local schemas are preserved
by the normal schema-default installation mechanism.

## File contract

The eight-byte header is little endian: a 32-bit record count followed by a
32-bit version equal to 1. Records have fixed sizes per table, listed in
`PspIffFormat.cs` and the bundled schemas. Both header words are written back
with their PSP widths. PC headers retain their 16-bit count limit.

`IffHeader.RecordCount`, `IffReaderOptions.MaximumRecordCount`, and
`IffCatalogOptions.MaximumRecordCount` now use `int` to accommodate the wider
PSP count. The default read limit remains 65,535; callers can raise it explicitly.
Values beyond `int.MaxValue` are rejected before allocation.

The inspected archive also contains a **headerless Title array**, with 88-byte
records. Detection checks the filename, exact length, every enabled word, and
every category-12 ID. `IffHeader.IsHeaderless` and `DataOffset` retain this layout
when saving, without adding an eight-byte header. Removing every record from
this headerless variant is rejected because an empty file cannot identify its
format. Normal headered Title tables are supported as well.

Known PSP table sizes are checked exactly. Truncation, inconsistent counts,
incorrect strides, and configured size/count-limit violations are rejected.
Unknown bytes remain in `IffRecord.Bytes`; editing a known field does not
normalize adjacent data. In particular, saves do not emulate the game's price-like
field adjustments, Quest runtime resets, or synthetic course insertion.

## Evidence and scope

Static evidence: Ghidra program `EBOOT.BIN.dec`, PSP ELF,
`Allegrex:LE:32:default`, image base zero. The file is not executed.

- `001c963c`: archive loading and dispatch to 17 named readers.
- `001c9c2c` through `001cade8`: eight-byte header and fixed record strides.
- `001db1a4`, `001db11c`, `001dc3b4`: full-record copies and map keys.
- `001caeb8` through `001cb874`, `001cb97c`: category lookups and ID dispatch.
- `00152f48`: Character model basename at `0x74` used for camera assets.
- `001c7020`: Parts model and three texture/replacement pairs.
- `001c8340`, `001c873c`: Character slot counts, Parts slot mask, requirement.
- `002249f4`: five signed stat contributions from Character, ClubSet, Ball,
  AuxPart, and Caddie. Fields remain `Stat1`..`Stat5` until individual names
  are established. `Requirement` deliberately does not claim to be a level.
- `001ca558`: course name, map asset, and property XML string offsets.
- `001cbca8`: HairStyle character selector at record offset `0x75`.

The actual supplied archive corroborates the version-1 header and record sizes
for 12 headered tables, and the headerless Title variant. SetItem, QuestDrop,
Quest, and Desc sizes come from the binary readers and have synthetic round-trip
tests; they are not present in that archive. Unknown fields remain hidden raw
ranges. This is partial semantic recovery, not a claim that every field has
been named. The 40-byte name and 512-byte description fields use code page 949
for the inspected Korean data; the editor's selected string encoding can
override it for other localized data.

## Reusable scripts

- `scripts/generate_psp_iff_schemas.py`: regenerate only bundled PSP schemas
  with `python scripts/generate_psp_iff_schemas.py`; no third-party modules.
- `scripts/verify-psp-iff.cs`: read-only verification of every table's exact
  round trip and catalog indexing. Requires .NET 10. Run
  `dotnet run --file scripts/verify-psp-iff.cs -- <archive-or-loose-table>`.
  This does not modify the supplied files or execute the game.

Automated regression tests use isolated synthetic tables and archives; they
do not require a PangYa installation or persistent application data.

Validation on 2026-11-05: solution restore and Release build succeeded (existing
NuGet dependency warnings remain). The real archive's 13 tables / 1,881 records
round-tripped byte for byte and indexed successfully. The 31 new PSP regression
cases passed. Full solution tests passed 750 cases and failed the two existing
`ShopTests.UiRenderer_PreviewsLayoutBackgroundAndStaticCaption` cases (both
`useImage` values), in the separately modified UI-renderer work. No manual
WinForms interaction or PSP/emulator execution was performed for this change.
