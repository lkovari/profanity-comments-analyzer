# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.3] - 2026-06-24

### Added

- GitHub Actions workflow [`.github/workflows/build.yml`](.github/workflows/build.yml) — CI on push/PR; NuGet publish on `v*` tags via `NUGET_API_KEY` secret.

### Changed

- README: expanded documentation for **`extra-patterns.json`** (cross-language extras vs per-language files), **`AdditionalFiles`** (MSBuild wiring, merge/replace behavior, when required), build-time data flow, and **analyzer source file** reference.
- README: maintainer workflow documents editing JSON directly; embedded lists are loaded by `WordListRegistry` via `System.Text.Json`.

### Removed

- `tools/port-languages.py` — word lists are maintained as JSON in the repository, not generated from external TypeScript sources.

## [2.0.2] - 2026-06-24

### Changed

- **Breaking for custom word lists:** `WordListRegistry` now validates JSON strictly and throws **`WordListLoadException`** with descriptive messages when `languages.json` is missing or empty, a listed language file is missing or empty, JSON is invalid, or word entries are incomplete. Silent skip/fallback for these cases is removed.
- README: document word list validation errors.

### Added

- `WordListLoadException` with human-readable messages for all word list load failures.

## [2.0.1] - 2026-06-24

### Changed

- README: document **PCA001** — rule naming, message format, purpose, why a stable rule ID is required, and severity configuration (`warning`, `error`, `none`).

### Removed

- Legacy template `templates/profanity-words.custom.json` (use `templates/profanity/` instead).
- Empty `ProfanityCommentsAnalyzer/Languages/` directory left over from v1 C# word lists.
- Unused C# generation code from `tools/port-languages.py`.
- Unused `LanguageRegistry.BuildCompiledPatterns` API.

## [2.0.0] - 2026-06-24

### Changed

- **Breaking:** Word lists are loaded from JSON instead of compiled C# source (`Languages/*.cs` removed from the analyzer).
- Built-in languages (en, hu, de, ro, it) ship as **embedded JSON** in the analyzer assembly.
- Project `AdditionalFiles` **replace** built-in lists per language when you provide `{code}.json` or a custom `languages.json`.
- NuGet package ships `content/WordLists/` and `content/templates/profanity/` instead of `content/Languages/*.cs`.

### Added

- `WordListRegistry` — loads embedded defaults plus JSON from `AdditionalFiles`.
- JSON registry layout: `languages.json`, `{code}.json`, `extra-patterns.json`.
- Template folder `templates/profanity/` in the repository.
- `tools/port-languages.py` generates JSON from the ESLint TypeScript word lists.
- Tests for JSON override, manifest editing, and legacy `profanity-words*.json` compatibility.

### Removed

- Compiled language files (`English.cs`, `Hungarian.cs`, etc.) and `AdditionalWordsLoader` append-only model.
- NuGet `content/Languages/*.cs` reference copies (replaced by JSON).

### Migration

- **Consumers with no custom lists:** upgrade the package; no project changes needed.
- **Consumers with custom words:** copy `templates/profanity/`, add `<AdditionalFiles Include="profanity\**\*.json" />`, edit JSON files.
- **Forks that edited C# language files:** regenerate from TS via `python3 tools/port-languages.py` or edit JSON directly.

## [1.0.0] - 2026-06-24

### Added

- Initial release of **ProfanityCommentsAnalyzer** Roslyn analyzer (rule **PCA001**).
- Comment-only analysis for C# single-line, multi-line, and documentation comments.
- Built-in word lists for English, Hungarian, German, Romanian, and Italian, including code-critique phrases.
- Configurable minimum severity, language filter, and allow list via `.editorconfig` / `.globalconfig`.
- Supplemental words via `AdditionalFiles` JSON (`profanity-words*.json`).
- NuGet package with analyzer assembly and language source templates.

[2.0.3]: https://github.com/lkovari/profanity-comments-analyzer/releases/tag/v2.0.3
[2.0.2]: https://github.com/lkovari/profanity-comments-analyzer/releases/tag/v2.0.2
[2.0.1]: https://github.com/lkovari/profanity-comments-analyzer/releases/tag/v2.0.1
[2.0.0]: https://github.com/lkovari/profanity-comments-analyzer/releases/tag/v2.0.0
[1.0.0]: https://github.com/lkovari/profanity-comments-analyzer/releases/tag/v1.0.0
