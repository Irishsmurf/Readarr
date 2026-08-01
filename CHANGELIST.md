# Changelist

All notable changes made in this fork relative to the upstream [Readarr](https://github.com/Readarr/Readarr) project are documented here.

> [!NOTE]
> Upstream Readarr was officially retired on **June 27, 2025**. This fork exists to carry forward bug fixes and maintenance patches.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

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

[Unreleased]: https://github.com/Irishsmurf/Readarr/compare/main...HEAD
