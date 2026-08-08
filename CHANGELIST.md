# Changelist

All notable changes made in this fork relative to the upstream [Readarr](https://github.com/Readarr/Readarr) project are documented here.

> [!NOTE]
> Upstream Readarr was officially retired on **June 27, 2025**. This fork exists to carry forward bug fixes and maintenance patches.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

*(nothing yet)*

---

## [0.4.20.1] - 2026-08-08

### Fixed

- **[#56]** `Newtonsoft.Json.JsonReaderException` aborted the daily author refresh whenever the
  metadata server answered with something that wasn't JSON.
  - The scheduled refresh asks the metadata server which authors changed since the last run, so it
    can skip the rest. `BookInfoProxy.GetChangedAuthors` sets `SuppressHttpError` precisely so a
    failing server degrades gracefully — it checks `Resource == null` and returns `null`, which
    makes the caller fall back to its per-author heuristic. That guard could never run:
    `HttpResponse<T>` deserializes in its constructor, so a plain-text `Service Unavailable` body
    threw first. The call also sat outside the per-author `try/catch`, so one bad response took
    down the entire refresh. Installs with no configured metadata mirror hit this against the
    retired `api.bookinfo.club` host.
  - `HttpResponse<T>` now raises `InvalidJsonResponseException`, naming the URL, status code,
    content type and a truncated body sample, and keeping the original reader exception as
    `InnerException`. It is the single point `Get<T>`/`Post<T>`, `CachedHttpResponseService.Get<T>`
    and `GazelleParser` all funnel through. Deliberately **no** content-type allowlist: valid JSON
    served with a wrong or missing `Content-Type` still parses exactly as before.
  - A metadata outage now degrades to "refresh the stale authors" and logs a warning, instead of
    failing the task. Note this means the task no longer shows as **Failed** in System → Tasks;
    the failure is recorded in the log at `Warn`.
  - Also fixes a latent `InvalidOperationException` on the same line: `message.LastStartTime.Value`
    was guarded only by `LastExecutionTime.HasValue`, though the two are independent nullables.
  - The other `READARR__SERVICES_URL` consumers got the same treatment, since the Docker image
    points that variable at this fork's analytics service and only `/analytics` is specified:
    `UpdatePackageProvider` degrades to "no update available" rather than failing the update check
    every six hours, and `SystemTimeCheck` now skips when no services URL is configured (it was
    contacting the retired upstream host unconditionally, contrary to the README) and no longer
    reports a clock problem when it cannot reach a clock.
  - `HealthCheckService` runs each check in its own `try/catch`. One check that threw used to
    discard the results of every other check in that run.
  - **PR:** [#56](https://github.com/Irishsmurf/Readarr/pull/56)

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

[Unreleased]: https://github.com/Irishsmurf/Readarr/compare/v0.4.20.1...HEAD
[0.4.20.1]: https://github.com/Irishsmurf/Readarr/releases/tag/v0.4.20.1
[0.4.20.0]: https://github.com/Irishsmurf/Readarr/releases/tag/v0.4.20.0
[0.4.19.2]: https://github.com/Irishsmurf/Readarr/releases/tag/v0.4.19.2
[0.4.19.1]: https://github.com/Irishsmurf/Readarr/releases/tag/v0.4.19.1
