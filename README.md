# ProfanityCommentsAnalyzer

Roslyn analyzer that detects profanity, threats, slurs, insults, and harsh code-critique language in **C# comments only**.

Word lists live in **JSON files** — a manifest (`languages.json`) plus one file per language (`en.json`, `hu.json`, …). Edit those files in your project to add, change, or remove words and languages **without republishing the NuGet package**.

Built-in lists for **en**, **hu**, **de**, **ro**, and **it** are embedded in the analyzer. Customization is optional.

## Scope

Analyzed:

- `//` single-line comments
- `/* */` multi-line comments
- `///` and `/** */` documentation comments
- `.cs` files only

Not analyzed: string literals, identifiers, `.cshtml`, `.razor`, Visual Basic.

## Quick start

### Install

```xml
<ItemGroup>
  <PackageReference Include="ProfanityCommentsAnalyzer" Version="2.0.3">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Or:

```bash
dotnet add package ProfanityCommentsAnalyzer --version 2.0.3
```

### Run

```csharp
public class PaymentService
{
    // damn this legacy API          ← PCA001 warning
    private string label = "damn"; // ignored (not a comment)
}
```

```bash
dotnet build
```

Diagnostics use rule **PCA001** (warning by default):

```ini
[*.cs]
dotnet_diagnostic.PCA001.severity = warning
```

Settings also work in `.globalconfig` (`is_global = true`).

### Optional analyzer settings

```ini
profanity_comments_analyzer.min_severity = mild
profanity_comments_analyzer.languages = en,hu,de,ro,it
profanity_comments_analyzer.allow_list = hack
```

| Option | Default | Description |
|--------|---------|-------------|
| `profanity_comments_analyzer.min_severity` | `mild` | Minimum severity: `mild`, `moderate`, `severe` |
| `profanity_comments_analyzer.languages` | `en,hu,de,ro,it` | Comma-separated language codes to check |
| `profanity_comments_analyzer.allow_list` | *(empty)* | Suppress exact matches (case-insensitive) |

To fail the build on violations, treat warnings as errors and do not exclude PCA001.

## PCA001

**PCA001** is the diagnostic rule ID for this package’s only rule. **PCA** stands for **P**rofanity **C**omments **A**nalyzer; **001** is the first rule in the package.

| Property | Value |
|----------|--------|
| Title | Profanity or offensive language in comment |
| Category | CodeQuality |
| Default severity | Warning |
| Enabled by default | Yes |

When a comment matches a word list entry, the build reports **PCA001** with a message like:

```text
[profanity-in-comments] "damn" (en, severity: mild) found in comment
```

The matched text, language code, and entry severity appear in the message. String literals and identifiers are not checked — only comment text (see **Scope** above).

### Why this rule exists

The analyzer helps teams catch offensive or unprofessional language in comments before it reaches shared source control. Typical uses:

- Enforcing team or code-of-conduct standards in comments and XML docs
- Flagging profanity, threats, slurs, insults, and harsh code-critique phrasing
- Optional CI enforcement when warnings are treated as errors

### Why a rule ID is needed

Roslyn analyzers expose stable rule IDs so projects and tools can:

- Set severity in `.editorconfig` or `.globalconfig`
- Show squiggles and entries in the IDE Error List
- Suppress or enable the rule per project, directory, or file
- Track rule changes across analyzer releases

### Configure PCA001

Settings work in `.editorconfig` or `.globalconfig` (`is_global = true`):

```ini
[*.cs]
dotnet_diagnostic.PCA001.severity = warning
```

| Severity | Effect |
|----------|--------|
| `warning` | Default — reported, build succeeds |
| `error` | Build fails when a match is found |
| `none` | Rule disabled |

To fail the build on every match, set `dotnet_diagnostic.PCA001.severity = error`, or use `TreatWarningsAsErrors` without excluding PCA001.

Analyzer options (`profanity_comments_analyzer.*`) filter *what* is matched; **PCA001** controls *how* matches are reported.

## How it works

At build time the Roslyn analyzer:

1. Reads **`.editorconfig` / `.globalconfig`** options (`AnalyzerConfig`).
2. Loads word lists from **embedded JSON** in the NuGet package and from optional project **`AdditionalFiles`** (`WordListRegistry`).
3. Walks **comment trivia** in each `.cs` file (`ProfanityInCommentsAnalyzer`).
4. Matches comment text against compiled regex patterns (`ProfanityMatcher`).
5. Reports **PCA001** for each hit that passes severity and allow-list filters.

You do not reference the analyzer in application code. Install the package, build, and the analyzer runs automatically.

## Custom word lists (JSON registry)

By default the package embeds full word lists for **en**, **hu**, **de**, **ro**, and **it**. To customize languages or words without publishing a new NuGet package, copy the templates into your solution and register them as **`AdditionalFiles`**.

### Word list files

| File | Required | Purpose |
|------|----------|---------|
| `languages.json` | Yes (when customizing) | Manifest — lists which language codes are active |
| `{code}.json` | One per code in the manifest | All words/patterns for that language |
| `extra-patterns.json` | No | Extra phrases checked **in addition** to language lists |

Templates: `templates/profanity/` in this repo, or `content/templates/profanity/` in the NuGet package.

```text
profanity/
  languages.json       ← which languages exist
  en.json                ← English words (replaces built-in en when provided)
  hu.json
  de.json
  ro.json
  it.json
  extra-patterns.json    ← optional cross-language extras
```

#### `languages.json`

Declares which languages the analyzer loads. Every code in the `"languages"` array **must** have a matching `{code}.json` file (from your project or embedded in the package).

```json
{
  "languages": ["en", "hu", "de", "ro", "it"]
}
```

- **Remove a language:** delete its code from this array (and optionally delete `{code}.json`).
- **Add a language (e.g. French):** add `"fr"` here and add `fr.json` with entries.
- **Custom manifest replaces the built-in manifest** when you provide this file via `AdditionalFiles`. Only languages listed here are registered.

#### `{code}.json` (e.g. `en.json`, `hu.json`)

Contains every word/pattern for one language. When you supply `{code}.json` via `AdditionalFiles`, it **fully replaces** the built-in embedded list for that code — it does not merge or append.

```json
{
  "code": "en",
  "name": "English",
  "entries": [
    {
      "word": "damn",
      "pattern": "\\bd[a@]mn\\b",
      "severity": "mild",
      "category": "profanity"
    }
  ]
}
```

| Field | Values |
|-------|--------|
| `severity` | `mild`, `moderate`, `severe` |
| `category` | `profanity`, `threat`, `slur`, `poorCode`, `badPractice`, `confusion` (optional) |
| `pattern` | .NET regex (case-insensitive at runtime; do not use `(?i)`) |

The `"code"` property must match the file name (`en.json` → `"code": "en"`).

#### `extra-patterns.json`

**Purpose:** hold phrases that are **not tied to a single language** — company-specific banned terms, internal codenames, multi-word phrases, or patterns you want checked **alongside** every active language list.

Language files (`en.json`, etc.) are scoped to one language and are filtered by `profanity_comments_analyzer.languages`. **`extra-patterns.json` is different:**

- Entries are **language-neutral** (diagnostics show `custom` as the language label).
- They are **always included** when the analyzer runs, regardless of which languages are selected in `.editorconfig`.
- They **add** patterns on top of language lists; they do **not** replace `{code}.json`.
- The shipped template is an empty `"entries": []` file — add entries only when you need them.

```json
{
  "entries": [
    {
      "word": "company banned phrase",
      "pattern": "company\\s+banned\\s+phrase",
      "severity": "moderate"
    },
    {
      "word": "internal codename",
      "pattern": "\\binternal\\s+codename\\b",
      "severity": "severe",
      "category": "profanity"
    }
  ]
}
```

**When to use `extra-patterns.json` vs `{code}.json`:**

| Use `{code}.json` | Use `extra-patterns.json` |
|-------------------|---------------------------|
| Standard profanity/threats/slurs in a given language | Phrases that apply to all languages |
| Replacing or trimming a built-in language list | Adding a few team-specific terms without editing five language files |
| Adding a new language (e.g. `fr.json`) | Regex that should match regardless of `profanity_comments_analyzer.languages` |

### `AdditionalFiles` — passing JSON to the analyzer

**`AdditionalFiles`** is an MSBuild item type. Files marked as `AdditionalFiles` are passed to Roslyn analyzers through `AnalyzerOptions.AdditionalFiles` at compile time. They are **not** compiled into your assembly — they are configuration input for the analyzer, similar to `.editorconfig`.

**When you need it:**

| Scenario | `AdditionalFiles` required? |
|----------|----------------------------|
| Use built-in word lists only | **No** |
| Override `languages.json` or any `{code}.json` | **Yes** |
| Add entries in `extra-patterns.json` | **Yes** |
| Legacy `profanity-words.custom.json` | **Yes** |

**Register your JSON folder in the `.csproj`:**

```xml
<ItemGroup>
  <AdditionalFiles Include="profanity\**\*.json" />
</ItemGroup>
```

**Which files are picked up:**

A JSON file is loaded when its path:

- contains `profanity` (recommended — use a `profanity/` folder), **or**
- is named `languages.json`, `extra-patterns.json`, or `{code}.json` (e.g. `en.json`, `hu-custom.json`)

**Merge and replace behavior:**

| Source | Effect |
|--------|--------|
| Embedded JSON in the NuGet DLL | Default word lists; always loaded first |
| Your `languages.json` | Replaces the embedded manifest |
| Your `{code}.json` | Replaces embedded list for that code only |
| Your `extra-patterns.json` | **Appends** extra patterns (does not replace language files) |
| Other languages not overridden | Keep using embedded `{code}.json` |

**Example:** embedded lists include `en` and `hu`. You provide only `profanity/hu.json`. English keeps the built-in list; Hungarian uses your file.

Files under `profanity/` are validated strictly (see **Validation** below). Invalid JSON or missing files referenced in `languages.json` fail the build with **`WordListLoadException`**.

### Setup steps

1. Copy templates from `templates/profanity/` or `content/templates/profanity/` in the package into your solution (e.g. a `profanity/` folder).
2. Add `<AdditionalFiles Include="profanity\**\*.json" />` to your `.csproj`.
3. Edit `languages.json`, `{code}.json`, and optionally `extra-patterns.json`.

### Common tasks

| Task | Action |
|------|--------|
| Add a word | Add an entry to `{code}.json` |
| Remove a word | Delete the entry from `{code}.json` |
| Change a word or pattern | Edit the entry in `{code}.json` |
| Turn off a language | Remove its code from `languages.json` and/or `.editorconfig` |
| Add a language (e.g. French) | Add `fr.json`, add `"fr"` to `languages.json` |
| Add a cross-language phrase | Add an entry to `extra-patterns.json` |
| Remove a language | Remove its code from `languages.json`; delete `{code}.json` if you added it |

### Validation

Custom word list files are validated when the analyzer loads. Problems throw **`WordListLoadException`** with a descriptive message (the build fails and shows the message):

| Problem | Example message |
|---------|-----------------|
| `languages.json` missing (broken package) | Built-in manifest not found — reinstall the package |
| `languages.json` empty or whitespace | Manifest is empty |
| `languages.json` has no language codes | Does not list any languages |
| Code in manifest but `{code}.json` missing | References `fr` but `fr.json` was not found |
| `{code}.json` empty | Language file is empty |
| `{code}.json` invalid JSON | File is not valid JSON |
| `{code}.json` missing `code`, `name`, or `entries` | Language file is invalid |
| Entry missing `word` or `pattern` | Incomplete entry at index N |

Every code listed in `languages.json` must have a corresponding `{code}.json` with at least one complete entry. Built-in embedded files are validated the same way.

### Legacy single-file format

A single `profanity-words.custom.json` in `AdditionalFiles` is still supported (`extraPatterns`, or a full `code` / `name` / `entries` document). Prefer the `templates/profanity/` layout for new projects.

## Analyzer source files

The NuGet package ships one analyzer assembly (`ProfanityCommentsAnalyzer.dll`). These are its main source files and roles:

| File | Purpose |
|------|---------|
| **`ProfanityInCommentsAnalyzer.cs`** | Roslyn entry point. Registers on compilation start, loads config and word lists, walks comment trivia in `.cs` files, reports **PCA001**. |
| **`DiagnosticDescriptors.cs`** | Defines rule **PCA001** (title, message format, default severity, category). |
| **`AnalyzerConfig.cs`** | Reads `.editorconfig` / `.globalconfig` keys: `min_severity`, `languages`, `allow_list`. |
| **`WordListRegistry.cs`** | Loads embedded JSON from the DLL and merges/replaces with project **`AdditionalFiles`**. Builds the active language and extra-pattern sets. |
| **`WordListDocuments.cs`** | JSON document types (`languages.json`, `{code}.json`, `extra-patterns.json`) and strict parsing into `LanguageDefinition` / `ProfanityEntry`. |
| **`WordListLoadException.cs`** | Thrown when word list files are missing, empty, invalid, or incomplete; carries human-readable error messages. |
| **`ProfanityMatcher.cs`** | Compiles regex patterns and scans comment text; applies minimum severity and allow-list filtering. |
| **`LanguageRegistry.cs`** | Thin accessor over embedded defaults (used by tests and internal lookups). |
| **`Models/ProfanityEntry.cs`** | One word/pattern with severity and optional category. |
| **`Models/LanguageDefinition.cs`** | Language code, display name, and list of entries. |
| **`Models/Severity.cs`** | `Mild`, `Moderate`, `Severe` — filters which entries are reported. |
| **`Models/Category.cs`** | Entry type: profanity, threat, slur, poorCode, badPractice, confusion. |
| **`IsExternalInit.cs`** | Polyfill for `record` support on `netstandard2.0` (required for Roslyn analyzer compatibility). |

**Data flow:**

```text
.editorconfig / .globalconfig
        ↓
  AnalyzerConfig          AdditionalFiles (*.json)
        ↓                          ↓
        └────────→ WordListRegistry.Load()
                          ↓
              ProfanityMatcher (regex scan)
                          ↓
         ProfanityInCommentsAnalyzer → PCA001 diagnostic
```

Embedded defaults live in **`ProfanityCommentsAnalyzer/WordLists/`** (compiled into the DLL). Consumer templates live in **`templates/profanity/`** (copied via `AdditionalFiles`).

## Migrating from 1.0.0

Version 2.0 replaces compiled C# word lists with JSON.

1. Copy `templates/profanity/` into your solution.
2. Add `<AdditionalFiles Include="profanity\**\*.json" />`.
3. Edit JSON instead of maintaining custom C# language files.

If you only used the NuGet package without custom lists, no changes are required.

## Maintainers

### Edit embedded word lists

Word lists are **JSON files** — the source of truth for this project. There is no code-generation step.

At build time, `ProfanityCommentsAnalyzer/WordLists/*.json` is embedded into the analyzer DLL (`EmbeddedResource` in the `.csproj`). At compile time in consuming projects, `WordListRegistry.Load()` reads those embedded defaults with `System.Text.Json`, then merges or replaces them with any JSON passed via `AdditionalFiles`.

To change built-in defaults:

1. Edit `ProfanityCommentsAnalyzer/WordLists/` (`languages.json`, `{code}.json`, `extra-patterns.json`).
2. Mirror the same files in `templates/profanity/` (shipped as NuGet `content/` templates for consumers).
3. Run `dotnet test` — word list tests validate loading and matching.

### Release checklist

End-to-end steps to ship a new version to [nuget.org](https://www.nuget.org). Details for each phase are in the sections below.

#### One-time setup (do once)

- [ ] [nuget.org → Trusted Publishing](https://www.nuget.org/manage/trustedpublishers): policy for owner `lkovari`, repo `profanity-comments-analyzer`, workflow `build.yml`, environment `production`.
- [ ] GitHub repo → **Settings → Environments → New environment** → name **`production`**.

No `NUGET_API_KEY` repository secret is required — CI uses [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (OIDC via `NuGet/login@v1`).

#### Every release

1. [ ] Update word lists if needed ([Edit embedded word lists](#edit-embedded-word-lists) above).
2. [ ] Add a `[X.Y.Z]` entry to `CHANGELOG.md`.
3. [ ] Bump `<Version>` in `ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj` (must match the tag, e.g. `2.0.3`).
4. [ ] Run locally:

```bash
dotnet build -c Release
dotnet test -c Release
dotnet pack ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj -c Release -o ./artifacts
```

5. [ ] Commit and push to `main`.
6. [ ] Tag and push (replace `vX.Y.Z` with the version from the `.csproj`):

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

7. [ ] Watch [GitHub Actions](https://github.com/lkovari/profanity-comments-analyzer/actions): `ci` (build + test) must pass, then `publish` (pack + push to NuGet.org).
8. [ ] Confirm the package on [nuget.org/packages/ProfanityCommentsAnalyzer](https://www.nuget.org/packages/ProfanityCommentsAnalyzer).

#### Manual push (optional)

If you need to upload from your machine instead of CI, create a scoped [API key](https://www.nuget.org/account/apikeys) and run:

```bash
dotnet nuget push ./artifacts/ProfanityCommentsAnalyzer.X.Y.Z.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### Build, test, pack

```bash
dotnet build -c Release
dotnet test /p:CollectCoverage=true
dotnet pack ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj -c Release -o ./artifacts
```

Package contents:

- `analyzers/dotnet/cs/ProfanityCommentsAnalyzer.dll` (embedded defaults)
- `content/WordLists/*.json`
- `content/templates/profanity/*.json`
- `README.md`

### CI and publish

Workflow: [`.github/workflows/build.yml`](.github/workflows/build.yml)

| Trigger | Action |
|---------|--------|
| Pull request or push to `main` | Build and test (with coverage) |
| Push tag `v*` (e.g. `v2.0.3`) | Pack and publish to [nuget.org](https://www.nuget.org) |

### Publish to nuget.org

See **[Release checklist](#release-checklist)** for the full flow. Summary:

**Automated (recommended):** push a version tag after bumping `<Version>` in `ProfanityCommentsAnalyzer.csproj`:

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

The [`build.yml`](.github/workflows/build.yml) workflow runs `ci` on every push/PR, and on `v*` tags runs `publish` (pack + NuGet push) using trusted publishing — no long-lived API key in GitHub secrets.

**Manual push** (local): see [Release checklist → Manual push](#manual-push-optional).

## License

MIT — see [LICENSE](LICENSE).
