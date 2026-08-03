# Changelist

All notable changes made in this fork relative to the upstream [Readarr](https://github.com/Readarr/Readarr) project are documented here.

> [!NOTE]
> Upstream Readarr was officially retired on **June 27, 2025**. This fork exists to carry forward bug fixes and maintenance patches.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

### Security

- **[Dependencies]** Dropped the `Servarr.FluentMigrator.Runner` meta-package, which pulled
  in every database provider FluentMigrator supports — including SqlServer via
  `Microsoft.Data.SqlClient` 2.1.2, the source of four NuGet audit findings (one critical:
  `System.Drawing.Common` GHSA-rxg9-xrhp-64gj). Readarr only uses SQLite and Postgres, both
  already referenced directly, so nothing else changes. Removed the four corresponding
  `NuGetAuditSuppress` entries from `src/Directory.Build.props`. ([#22](https://github.com/Irishsmurf/Readarr/issues/22))

---

## [0.4.19.2] - 2026-08-01

### Fixed

- **[CI]** Release workflow failed on all platforms due to a frontend build race condition.
  All 6 matrix jobs start simultaneously; non-x64 legs tried to restore a GHA cache that
  the x64 leg hadn't saved yet (`actions/cache` silently misses rather than blocking).
  Additionally, `build.sh --backend` clears `_output/` at startup, so even a cache hit
  would have been wiped before packaging. Fix: dedicated `build-frontend` job builds the
  UI once and uploads it as a workflow artifact; each matrix leg downloads it after its
  backend build. Also set `fail-fast: false` so one platform failure no longer cancels
  all others.

---

## [0.4.19.1] - 2026-08-01

### Fixed

- **[#28]** `System.ArgumentNullException` in `QualityProfileController` when `FormatItems` is `null`
  - The FluentValidation `Must()` rule now short-circuits with a user-facing `400` validation error
    ("Try refreshing your browser") instead of crashing with a `500` when `FormatItems` is `null`.
  - The `Custom()` rule now guards against a `null` or empty `FormatItems` list before evaluating
    min-format-score logic, and uses `.DefaultIfEmpty()` before `.Max()` to prevent an
    `InvalidOperationException` on empty sequences.
  - **File:** `src/Readarr.Api.V1/Profiles/Quality/QualityProfileController.cs`
  - **PR:** [#29](https://github.com/Irishsmurf/Readarr/pull/29)

---

## Legend

| Symbol | Meaning |
|--------|---------|
| **Added** | New features or files |
| **Changed** | Changes to existing behaviour |
| **Fixed** | Bug fixes |
| **Removed** | Removed features or files |
| **Security** | Security-related changes |
| **Deprecated** | Soon-to-be removed features |

[Unreleased]: https://github.com/Irishsmurf/Readarr/compare/v0.4.19.2...HEAD
[0.4.19.2]: https://github.com/Irishsmurf/Readarr/releases/tag/v0.4.19.2
[0.4.19.1]: https://github.com/Irishsmurf/Readarr/releases/tag/v0.4.19.1
