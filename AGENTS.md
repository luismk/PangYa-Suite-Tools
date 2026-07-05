# AGENTS.md

This file gives coding agents the working rules for this repository. It applies to the entire repository unless a more specific `AGENTS.md` exists below the file being changed.

## Project overview

PangYa Suite Tools is a Windows-only C#/.NET 10 solution for inspecting and modifying PangYa game formats.

- `PangYa-Suite-Tools/` contains the WinForms application.
- `PangyaAPI/PangyaAPI.PAK/` contains PAK parsing, extraction, and writing logic.
- `PangyaAPI/PangyaAPI.UpdateList/` contains update-list parsing and generation logic.
- `PangyaAPI/PangyaAPI.IFF/` contains streaming IFF schemas, records, containers, and atomic writers.
- `PangyaAPI/PangyaAPI.Utilities/` contains shared binary and cryptographic utilities.
- `PangYa-Suite-Tools.Tests/` contains WinForms and localization tests.
- `PangyaAPI.Tests/` contains binary-format, round-trip, invalid-input, and performance tests.

The solution entry point is `PangYa-Suite-Tools.slnx`. Nullable reference types and implicit usings are enabled, and warnings are treated as errors through `Directory.Build.props`.

## Working principles

- Inspect the relevant implementation and tests before editing. Keep changes focused on the requested behavior.
- Preserve unrelated local modifications. Never discard, overwrite, or reformat user work outside the task.
- Do not commit generated output such as `bin/`, `obj/`, `Debug/`, `Release/`, `.vs/`, packages, or temporary PAK/update-list fixtures.
- Keep format-handling code deterministic and backward compatible. PAK offsets, sizes, filename encodings, compression modes, encryption keys, and byte order are part of the file contract.
- Treat files read from outside the application as untrusted. Validate lengths, offsets, counts, and extraction paths before allocating, seeking, or writing.
- Dispose streams, readers, writers, forms, and temporary resources. Tests should use isolated temporary directories and clean them up.

## Build and test

Run commands from the repository root:

```powershell
dotnet restore PangYa-Suite-Tools.slnx
dotnet build PangYa-Suite-Tools.slnx --configuration Release --no-restore
dotnet test PangYa-Suite-Tools.slnx --configuration Release --no-build
```

During iteration, run the smallest relevant test project first:

```powershell
dotnet test PangyaAPI.Tests/PangyaAPI.Tests.csproj
dotnet test PangYa-Suite-Tools.Tests/PangYa-Suite-Tools.Tests.csproj
```

The WinForms project and its tests require Windows. Before handing work back, run the full solution tests when practical. Report commands that were not run and any failures accurately; do not claim success based only on code inspection.

## C# conventions

- Follow the style of the surrounding file; the codebase contains both file-scoped and block-scoped namespaces.
- Use nullable annotations honestly. Prefer validation and explicit handling over the null-forgiving operator.
- Use `PascalCase` for types and public members, `camelCase` for locals and parameters, and `_camelCase` for private fields.
- Prefer clear, narrowly scoped methods and early validation for malformed input.
- Avoid drive-by style cleanup, broad renames, or wholesale modernization in feature and bug-fix changes.
- Add or update xUnit tests for behavior changes and regressions. Prefer byte-for-byte round-trip assertions for binary formats.

## WinForms rules

- Never perform expensive file I/O, compression, cryptography, directory scans, or network work on the UI thread. Use the project's asynchronous/Task-based patterns and marshal UI updates back to the UI thread.
- WinForms controls must only be accessed from their owning thread. Tests that construct forms or controls must run on an STA thread, following `LocalizationTests`.
- Treat `*.Designer.cs` and form `*.resx` files as designer-owned. Edit them only when the requested UI change requires it, preserve the designer structure, and keep control declarations and initialization synchronized.
- Keep business and binary-format logic in the API projects when it can be independent of the UI. Forms should coordinate user interaction rather than duplicate parsers or cryptography.
- Preserve cancellation, progress reporting, error handling, and form responsiveness when modifying long-running workflows.

## Localization

User-visible UI text must use the localization system rather than new hard-coded strings.

- Neutral English resources live in `PangYa-Suite-Tools/Localization/Strings.resx`.
- Brazilian Portuguese resources live in `Strings.pt-BR.resx`.
- Swedish resources live in `Strings.sv.resx`; `SwedishInlineTranslations.cs` may provide project-specific support for Swedish text.
- Strongly typed accessors are in `Strings.cs`, and culture switching is coordinated by `LocalizationManager`.
- Add the same resource key to every supported culture and preserve format placeholders such as `{0}` across translations.
- Forms must refresh their visible text when `LocalizationManager.CultureChanged` fires and unsubscribe when disposed where applicable.
- Extend localization tests when adding resources, changing culture behavior, or adding a form. Do not hand-edit generated resource metadata unless required by the existing mechanism.

## Binary formats and security

- Use explicit encodings. PAK filenames may use EUC-KR, Shift-JIS, UTF-8, or another user-selected encoding; do not silently replace the reader/writer's configured encoding.
- Use checked bounds and sufficiently wide arithmetic when deriving offsets or sizes. Reject truncated or inconsistent input with useful exceptions.
- Prevent path traversal during extraction: an archive entry must never escape the selected destination.
- Do not change region keys, XTEA/XOR behavior, compression semantics, header layout, or entry-version rules without targeted fixtures and round-trip tests.
- Avoid tests that depend on a user's PangYa installation, network services, registry state, administrator privileges, current culture, or persistent application data.
- Performance probes are not substitutes for correctness tests and should not introduce flaky timing assertions.

## Documentation and delivery

- Update `README.md` when user-facing behavior, supported formats, setup, or CLI/build requirements change.
- Follow Conventional Commit-style subjects when asked to commit, for example `fix(pak): reject truncated entry names`.
- Summarize changed files and validation performed. Mention platform limitations or manual WinForms checks that remain.
