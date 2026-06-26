# ProfanityCommentsAnalyzer

Roslyn analyzer that detects profanity, threats, slurs, insults, and harsh code-critique language in **C# comments only**.

Word lists live in **JSON files** — a manifest (`languages.json`) plus one file per language (`en.json`, `hu.json`, …). Edit those files in your project to add, change, or remove words and languages **without republishing the NuGet package**.

Built-in lists for **en**, **hu**, **de**, **ro**, and **it** are embedded in the analyzer. Customization is optional.

**NuGet:** [ProfanityCommentsAnalyzer](https://www.nuget.org/packages/ProfanityCommentsAnalyzer)

---

## Table of contents

- [Tech stack](#tech-stack)
  - [Platform compatibility](#platform-compatibility)
- [Overview](#overview)
- [Getting started](#getting-started)
- [Diagnostic PCA001](#diagnostic-pca001)
- [How it works](#how-it-works)
- [Custom word lists (JSON registry)](#custom-word-lists-json-registry)
- [Migrating from 1.0.0](#migrating-from-100)
- [For maintainers](#for-maintainers)
- [License](#license)

---

## Tech stack

This project is a **build-time code analyzer** distributed as a **NuGet development dependency**. It does not run in your application at runtime.

### Roslyn analyzer (.NET Compiler Platform)

A **Roslyn analyzer** is a component built on the [.NET Compiler Platform](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) (also called **Roslyn**). Roslyn is the compiler infrastructure behind C# and Visual Basic: it parses source code into syntax trees, performs semantic analysis, and emits diagnostics.

Analyzers plug into that pipeline. They inspect syntax and semantics **during compilation** and report violations as diagnostics (warnings or errors). Because they run inside the compiler host, they also work in the IDE — squiggles, Error List entries, and live feedback while you type — without waiting for a full build.

Key characteristics relevant to this package:

| Aspect | Detail |
|--------|--------|
| **When it runs** | At compile time (`dotnet build`, MSBuild, Visual Studio build) and in the IDE during editing |
| **What it inspects** | Source code via Roslyn APIs (`SyntaxTree`, `SyntaxTrivia`, etc.) |
| **What it produces** | Diagnostics with stable rule IDs (here: **PCA001**) |
| **Runtime impact** | None — analyzers are not referenced by or shipped with your application assembly |

ProfanityCommentsAnalyzer implements a [`DiagnosticAnalyzer`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticanalyzer) that walks **comment trivia** in `.cs` files and reports matches against configurable word lists.

**Official references — Roslyn analyzers:**

- [.NET Compiler Platform SDK overview](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [Code analysis using Roslyn analyzers](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview)
- [Tutorial: Write your first analyzer and code fix](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [Customize Roslyn analyzer rules (severity, suppression)](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers)
- [Configure code analysis rules (`.editorconfig`, `globalconfig`)](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options)
- [DiagnosticAnalyzer API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticanalyzer)

### NuGet package

[**NuGet**](https://learn.microsoft.com/en-us/nuget/what-is-nuget/) is the package manager for .NET. A NuGet package (`.nupkg`) is a versioned, signed archive of files consumed by `dotnet restore` and MSBuild.

For **Roslyn analyzers**, NuGet is the standard distribution channel. When you add `ProfanityCommentsAnalyzer` to a project, NuGet restores the package and MSBuild wires its analyzer assembly into the compiler. You do **not** add `using` statements or call the analyzer from application code — it runs automatically on every build.

This package is configured as a **development dependency**:

| Property | Purpose |
|----------|---------|
| `DevelopmentDependency` | Signals that the package is for build/analysis only, not a runtime library |
| `IncludeBuildOutput=false` | The analyzer DLL is **not** placed under `lib/` (it is not a reference assembly) |
| `SuppressDependenciesWhenPacking=true` | Transitive analyzer dependencies are not exposed to consumers |
| `analyzers/dotnet/cs/` layout | Standard folder where the C# compiler discovers analyzer assemblies |

**Package contents** (what `dotnet pack` produces):

| Path in `.nupkg` | Role |
|------------------|------|
| `analyzers/dotnet/cs/ProfanityCommentsAnalyzer.dll` | The analyzer assembly (embedded default word lists) |
| `content/WordLists/*.json` | Reference copies of embedded defaults |
| `content/templates/profanity/*.json` | Starter templates for consumer customization |
| `README.md` | Package documentation (shown on [nuget.org](https://www.nuget.org/packages/ProfanityCommentsAnalyzer)) |

**Why `PackageReference` uses special `IncludeAssets`:**

Analyzer packages must expose the `analyzers` asset to MSBuild while keeping the package out of your app's runtime dependency graph. The recommended reference shape ensures the compiler receives the analyzer without linking it into your output:

```xml
<PackageReference Include="ProfanityCommentsAnalyzer" Version="2.0.6">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Official references — NuGet and analyzer packages:**

- [What is NuGet?](https://learn.microsoft.com/en-us/nuget/what-is-nuget/)
- [Install and manage NuGet packages in Visual Studio](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)
- [Analyzer NuGet formats and folder conventions](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions)
- [NuGet PackageReference in project files](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)
- [Package on nuget.org: ProfanityCommentsAnalyzer](https://www.nuget.org/packages/ProfanityCommentsAnalyzer)

### Other technologies

| Technology | Role in this project | Reference |
|------------|----------------------|-----------|
| **.NET Standard 2.0** | Analyzer target framework — broad compatibility with Roslyn hosts and IDE versions | [.NET Standard](https://learn.microsoft.com/en-us/dotnet/standard/net-standard) |
| **Microsoft.CodeAnalysis.CSharp** | Roslyn C# compiler APIs used to traverse syntax trees and report diagnostics | [Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp) |
| **Microsoft.CodeAnalysis.Analyzers** | Analyzer authoring rules and release-tracking for shipped diagnostics | [Microsoft.CodeAnalysis.Analyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Analyzers) |
| **System.Text.Json** | Parses embedded and `AdditionalFiles` word-list JSON at build time | [System.Text.Json overview](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview) |
| **`.editorconfig` / `.globalconfig`** | Configures PCA001 severity and analyzer options (`profanity_comments_analyzer.*`) | [EditorConfig for code analysis](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) |
| **`AdditionalFiles` (MSBuild)** | Passes optional JSON word lists from the consumer project to the analyzer | [AdditionalFiles item type](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items#additionalfiles) |
| **GitHub Actions + NuGet trusted publishing** | CI build/test; automated publish on version tags via OIDC | [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) |

### Platform compatibility

The analyzer assembly targets **.NET Standard 2.0** and runs at **build time** in the Roslyn compiler host — not in your application's runtime. In principle it can be used from consumer projects that target **.NET Framework** (including 4.7), **.NET Core**, or **.NET 5+**, as long as the build uses a Roslyn version compatible with `Microsoft.CodeAnalysis.CSharp` 4.3 (Visual Studio 2022 17.3+ or a recent .NET SDK).

**Testing in this repository:** automated tests run against **.NET 9** only (`ProfanityCommentsAnalyzer.Tests` targets `net9.0`). **.NET Framework 4.7 has not been tested here.** It will likely work for .NET Framework 4.7 projects built with Visual Studio 2022 17.3+ or `dotnet build` and a `PackageReference` to this package, but that combination is not verified by CI. If you rely on .NET Framework 4.7, validate PCA001 locally after install.

See [Roslyn version support](https://learn.microsoft.com/en-us/visualstudio/extensibility/roslyn-version-support) for compiler host requirements.

---

## Overview

### Analyzed

- `//` single-line comments
- `/* */` multi-line comments
- `///` and `/** */` documentation comments
- `.cs` files only

### Not analyzed

String literals, identifiers, `.cshtml`, `.razor`, and Visual Basic source.

---

## Getting started

### Install

```xml
<ItemGroup>
  <PackageReference Include="ProfanityCommentsAnalyzer" Version="2.0.6">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Or:

```bash
dotnet add package ProfanityCommentsAnalyzer --version 2.0.6
```

See [Install and use a NuGet package with the dotnet CLI](https://learn.microsoft.com/en-us/nuget/quickstart/install-and-use-a-package-using-the-dotnet-cli).

### Example

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

After install, `dotnet build` reports **PCA001** warnings for profane or offensive words found in comments. Each match shows the word, language code, and severity:

![Build output showing PCA001 profanity-in-comments warnings across en, hu, de, ro, and it test files](https://raw.githubusercontent.com/lkovari/profanity-comments-analyzer/main/docs/nuget-build-output.png)

### Analyzer options

Optional settings in `.editorconfig` or `.globalconfig` (`is_global = true`):

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

To fail the build on violations, set PCA001 to `error` or use `TreatWarningsAsErrors` without excluding PCA001. See [Configure rule severity](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#severity-level).

---

## Diagnostic PCA001

**PCA001** is the only diagnostic rule in this package. **PCA** = **P**rofanity **C**omments **A**nalyzer; **001** = first rule.

| Property | Value |
|----------|--------|
| Title | Profanity or offensive language in comment |
| Category | CodeQuality |
| Default severity | Warning |
| Enabled by default | Yes |

Example message:

```text
[profanity-in-comments] "damn" (en, severity: mild) found in comment
```

The matched text, language code, and entry severity appear in the message. Only comment text is checked (see [Overview](#overview)).

### Why this rule exists

The analyzer helps teams catch offensive or unprofessional language in comments before it reaches shared source control:

- Enforce team or code-of-conduct standards in comments and XML docs
- Flag profanity, threats, slurs, insults, and harsh code-critique phrasing
- Optional CI enforcement when warnings are treated as errors

### Why a stable rule ID matters

Roslyn diagnostics use stable IDs so projects and tools can configure severity, show IDE squiggles, suppress rules per scope, and track behavior across analyzer releases. See [Roslyn analyzer rule IDs](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#rule-id).

### Configure PCA001 severity

In `.editorconfig` or `.globalconfig`:

```ini
[*.cs]
dotnet_diagnostic.PCA001.severity = warning
```

| Severity | Effect |
|----------|--------|
| `warning` | Default — reported, build succeeds |
| `error` | Build fails when a match is found |
| `none` | Rule disabled |

Analyzer options (`profanity_comments_analyzer.*`) filter *what* is matched; **PCA001** controls *how* matches are reported.

---

## How it works

At build time the Roslyn analyzer:

1. Reads **`.editorconfig` / `.globalconfig`** options (`AnalyzerConfig`).
2. Loads word lists from **embedded JSON** in the NuGet package and from optional project **`AdditionalFiles`** (`WordListRegistry`).
3. Walks **comment trivia** in each `.cs` file (`ProfanityInCommentsAnalyzer`).
4. Matches comment text against compiled regex patterns (`ProfanityMatcher`).
5. Reports **PCA001** for each hit that passes severity and allow-list filters.

You do not reference the analyzer in application code. Install the package, build, and the analyzer runs automatically.

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

Embedded defaults live in `ProfanityCommentsAnalyzer/WordLists/` (compiled into the DLL). Consumer templates live in `templates/profanity/` (copied via `AdditionalFiles`).

### Analyzer source files

| File | Purpose |
|------|---------|
| `ProfanityInCommentsAnalyzer.cs` | Roslyn entry point — registers on compilation start, walks comment trivia, reports PCA001 |
| `DiagnosticDescriptors.cs` | Defines rule PCA001 (title, message format, default severity, category) |
| `AnalyzerConfig.cs` | Reads `.editorconfig` / `.globalconfig` keys |
| `WordListRegistry.cs` | Loads embedded JSON and merges/replaces with `AdditionalFiles` |
| `WordListDocuments.cs` | JSON document types and strict parsing |
| `WordListLoadException.cs` | Build failures for invalid or incomplete word lists |
| `ProfanityMatcher.cs` | Regex compilation, scanning, severity and allow-list filtering |
| `LanguageRegistry.cs` | Embedded defaults accessor |
| `Models/*` | `ProfanityEntry`, `LanguageDefinition`, `Severity`, `Category` |
| `IsExternalInit.cs` | Polyfill for `record` support on `netstandard2.0` |

---

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
  languages.json
  en.json
  hu.json
  de.json
  ro.json
  it.json
  extra-patterns.json
```

#### `languages.json`

Declares which languages the analyzer loads. Every code in the `"languages"` array **must** have a matching `{code}.json` file.

```json
{
  "languages": ["en", "hu", "de", "ro", "it"]
}
```

- **Remove a language:** delete its code from this array (and optionally delete `{code}.json`).
- **Add a language (e.g. French):** add `"fr"` here and add `fr.json` with entries.
- **Custom manifest replaces the built-in manifest** when provided via `AdditionalFiles`.

#### `{code}.json` (e.g. `en.json`, `hu.json`)

When supplied via `AdditionalFiles`, a language file **fully replaces** the built-in embedded list for that code — it does not merge.

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

Phrases **not tied to a single language** — company-specific banned terms, internal codenames, or multi-word patterns checked **alongside** every active language list.

- Entries are **language-neutral** (diagnostics show `custom` as the language label).
- They are **always included**, regardless of `profanity_comments_analyzer.languages`.
- They **append** on top of language lists; they do **not** replace `{code}.json`.

```json
{
  "entries": [
    {
      "word": "company banned phrase",
      "pattern": "company\\s+banned\\s+phrase",
      "severity": "moderate"
    }
  ]
}
```

| Use `{code}.json` | Use `extra-patterns.json` |
|-------------------|---------------------------|
| Standard profanity/threats/slurs in a given language | Phrases that apply to all languages |
| Replacing or trimming a built-in language list | Adding team-specific terms without editing every language file |
| Adding a new language (e.g. `fr.json`) | Regex that should match regardless of language filter |

### `AdditionalFiles` — passing JSON to the analyzer

**`AdditionalFiles`** is an MSBuild item type. Files marked as `AdditionalFiles` are passed to Roslyn analyzers through `AnalyzerOptions.AdditionalFiles` at compile time. They are **not** compiled into your assembly — they are configuration input for the analyzer, similar to `.editorconfig`.

See [Common MSBuild project items — AdditionalFiles](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items#additionalfiles).

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
- is named `languages.json`, `extra-patterns.json`, or `{code}.json`

**Merge and replace behavior:**

| Source | Effect |
|--------|--------|
| Embedded JSON in the NuGet DLL | Default word lists; always loaded first |
| Your `languages.json` | Replaces the embedded manifest |
| Your `{code}.json` | Replaces embedded list for that code only |
| Your `extra-patterns.json` | **Appends** extra patterns |
| Other languages not overridden | Keep using embedded `{code}.json` |

**Example:** embedded lists include `en` and `hu`. You provide only `profanity/hu.json`. English keeps the built-in list; Hungarian uses your file.

Invalid JSON or missing files referenced in `languages.json` fail the build with **`WordListLoadException`**.

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

### Validation

Custom word list files are validated when the analyzer loads. Problems throw **`WordListLoadException`** and fail the build:

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

Every code listed in `languages.json` must have a corresponding `{code}.json` with at least one complete entry.

### Legacy single-file format

A single `profanity-words.custom.json` in `AdditionalFiles` is still supported (`extraPatterns`, or a full `code` / `name` / `entries` document). Prefer the `templates/profanity/` layout for new projects.

---

## Migrating from 1.0.0

Version 2.0 replaces compiled C# word lists with JSON.

1. Copy `templates/profanity/` into your solution.
2. Add `<AdditionalFiles Include="profanity\**\*.json" />`.
3. Edit JSON instead of maintaining custom C# language files.

If you only used the NuGet package without custom lists, no changes are required.

---

## For maintainers

### Edit embedded word lists

Word lists are **JSON files** — the source of truth for this project. There is no code-generation step.

At build time, `ProfanityCommentsAnalyzer/WordLists/*.json` is embedded into the analyzer DLL (`EmbeddedResource` in the `.csproj`). At compile time in consuming projects, `WordListRegistry.Load()` reads those embedded defaults with `System.Text.Json`, then merges or replaces them with any JSON passed via `AdditionalFiles`.

To change built-in defaults:

1. Edit `ProfanityCommentsAnalyzer/WordLists/` (`languages.json`, `{code}.json`, `extra-patterns.json`).
2. Mirror the same files in `templates/profanity/` (shipped as NuGet `content/` templates for consumers).
3. Run `dotnet test` — word list tests validate loading and matching.

### Release checklist

End-to-end steps to ship a new version to [nuget.org](https://www.nuget.org).

#### One-time setup (do once)

- [ ] [nuget.org → Trusted Publishing](https://www.nuget.org/manage/trustedpublishers): policy for owner `lkovari`, repo `profanity-comments-analyzer`, workflow `build.yml`, environment `production`.
- [ ] GitHub repo → **Settings → Environments → New environment** → name **`production`**.

No `NUGET_API_KEY` repository secret is required — CI uses [trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) (OIDC via `NuGet/login@v1`).

#### Every release

1. [ ] Update word lists if needed ([Edit embedded word lists](#edit-embedded-word-lists)).
2. [ ] Add a `[X.Y.Z]` entry to `CHANGELOG.md`.
3. [ ] Bump `<Version>` in `ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj` — this is the **NuGet package version** (not the git tag alone).
4. [ ] Run locally (**does not publish** — verification only):

```bash
dotnet build -c Release
dotnet test -c Release
dotnet pack ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj -c Release -o ./artifacts
```

5. [ ] **Commit and push to `main` before tagging.** The tag must point at a commit that already contains the bumped `<Version>`.
6. [ ] **Publish to NuGet** — push a version tag (replace `X.Y.Z` with the value from the `.csproj`):

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

7. [ ] Watch [GitHub Actions](https://github.com/lkovari/profanity-comments-analyzer/actions): job `ci` (build + test), then job `publish` (pack + push).
8. [ ] Wait a few minutes for NuGet indexing, then confirm at [nuget.org/packages/ProfanityCommentsAnalyzer](https://www.nuget.org/packages/ProfanityCommentsAnalyzer).

#### Fix a mistagged release

If you pushed `v2.0.3` but forgot to commit the `.csproj` bump, CI published the **previous** `<Version>`. Fix:

```bash
git add ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj README.md CHANGELOG.md
git commit -m "Bump version to X.Y.Z"
git push origin main

git tag -d vX.Y.Z
git push origin :refs/tags/vX.Y.Z
git tag vX.Y.Z
git push origin vX.Y.Z
```

#### Manual push (optional)

**Publishes from your machine** — not used by CI. Create a scoped [API key](https://www.nuget.org/account/apikeys), pack locally, then:

```bash
dotnet pack ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj -c Release -o ./artifacts
dotnet nuget push ./artifacts/ProfanityCommentsAnalyzer.X.Y.Z.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

See [Publish a NuGet package with the dotnet CLI](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package).

### How publishing works

#### Which commands publish?

| Command / action | Publishes to NuGet? |
|------------------|---------------------|
| `dotnet build` | No |
| `dotnet test` | No |
| `dotnet pack` (local) | No — creates `.nupkg` in `./artifacts` only |
| `git push origin main` | No — runs `ci` (build + test) only |
| `git push origin vX.Y.Z` | **Yes** — triggers `publish` job |
| `dotnet nuget push ...` (local) | **Yes** — manual upload |

#### Automated flow (CI)

Workflow: [`.github/workflows/build.yml`](.github/workflows/build.yml)

```text
git push origin vX.Y.Z
        ↓
   job: ci
   restore → build → test (coverage gate)
        ↓ (must pass)
   job: publish  (only on v* tag pushes)
   environment: production
        ↓
   checkout @ tag commit
        ↓
   dotnet pack  →  ProfanityCommentsAnalyzer.{Version}.nupkg
        ↓
   NuGet/login@v1  (OIDC → short-lived API key)
        ↓
   dotnet nuget push  →  nuget.org
```

| Trigger | GitHub Actions jobs |
|---------|---------------------|
| Pull request or push to `main` | `ci` only |
| Push tag `v*` (e.g. `v2.0.3`) | `ci`, then `publish` |

**Trusted publishing:** the `publish` job runs in GitHub environment **`production`**, requests an OIDC token from GitHub, and exchanges it with nuget.org via `NuGet/login@v1` for a **temporary** API key (about 1 hour). No long-lived secret is stored in the repository.

**Version source:** `dotnet pack` reads `<Version>` from `ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj` on the **tagged commit**. The git tag name (`v2.0.3`) is only the trigger — it does not set the package version. Tag and `<Version>` should match.

#### Where the version lives

| File | Role |
|------|------|
| `ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj` → `<Version>` | **NuGet package version** (source of truth for `dotnet pack`) |
| Git tag `vX.Y.Z` | Triggers CI publish; should match `<Version>` |
| `CHANGELOG.md` | Release notes |
| `README.md` install examples | Documentation — update to match current release |

### Build, test, pack

Local verification (**does not publish**):

```bash
dotnet build -c Release
dotnet test /p:CollectCoverage=true
dotnet pack ProfanityCommentsAnalyzer/ProfanityCommentsAnalyzer.csproj -c Release -o ./artifacts
```

---

## License

MIT — see [LICENSE](LICENSE).
