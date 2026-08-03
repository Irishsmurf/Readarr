# Changelist

All notable changes made in this fork relative to the upstream [Readarr](https://github.com/Readarr/Readarr) project are documented here.

> [!NOTE]
> Upstream Readarr was officially retired on **June 27, 2025**. This fork exists to carry forward bug fixes and maintenance patches.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

*(nothing yet)*

---

## [0.4.20.0] - 2026-08-03

### Added

- **[#8]** Real analytics reporter: opt-in, anonymous, aggregate-only usage reporting (app
  version, branch, OS family, CPU architecture, .NET runtime version, database engine, whether a
  custom metadata source is configured) to a small service this fork runs itself
  ([Readarr-Analytics](https://github.com/Irishsmurf/Readarr-Analytics)). Off by default -
  enable via Settings → General → Analytics. See `docs/analytics.md` for exactly what is and
  isn't collected, and why.
- Update checks work again out of the box in the official Docker image: `READARR__SERVICES_URL`
  now defaults to this fork's own service instead of the retired upstream host. Still off by
  default when built from source, and overridable/disableable per-install either way.
- A release-time CI step now publishes each release's binaries to the Analytics service, so the
  update-check endpoint above actually has something to report.

### Changed

- **[#8]** Crash reporting's frontend Sentry initialization no longer piggybacks on the analytics
  toggle - it's gated by its own `CrashReportingEnabled` setting, matching the backend. Fixes a
  bug where disabling analytics would have silently disabled frontend crash reporting too.
- **[#8]** `UpdatePackageProvider` no longer sends the deprecated `active` query parameter that
  used to piggyback analytics onto update-check requests.

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
