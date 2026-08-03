# Analytics: scope, retention and privacy posture

This document is the decision record for [#6](https://github.com/Irishsmurf/Readarr/issues/6).
It is the contract that the ingest endpoint ([#7](https://github.com/Irishsmurf/Readarr/issues/7))
and the reporting client ([#8](https://github.com/Irishsmurf/Readarr/issues/8)) implement against.
Nothing outside this document gets collected, and nothing in this document gets collected unless
the operator opts in.

## Why this exists

Upstream's version of this fork carried an `AnalyticsEnabled` flag that did almost nothing: it
gated a single `active=true|false` query parameter piggybacked onto the update-check request, with
no payload, no schedule, and no transport of its own. Since update checks are off by default in
this fork (they only ever offered upstream builds, which don't carry this fork's fixes), that flag
had no transport left at all — it was dead code with a misleading UI toggle.

This fork decided to build a real, small analytics reporter instead of deleting the feature
outright, because there are a few genuinely useful things to know about installs of this fork that
nothing else surfaces: which versions are actually in use (so old, unsupported builds can be
deprecated), and a rough OS/runtime/mirror breakdown (so platform support decisions aren't guesses).

## What it's for

- **Version adoption.** Which app versions are actually running, so old builds can be safely
  deprecated.
- **Metadata mirror usage.** Whether installs are using the default metadata mirror or a custom
  one, in aggregate — not which one.
- **Platform breakdown.** OS family, CPU architecture, .NET runtime version, database engine.

It is explicitly **not** a crash-reporting or error-telemetry system — that's what Sentry already
is (see [Relationship to crash reporting](#relationship-to-crash-reporting) below). Duplicating
Sentry's job here would be scope creep and a second, redundant privacy surface.

## Field allow-list

This is the entire payload. If a field isn't listed here, it is never sent, full stop.

| Field | Description | Example |
|---|---|---|
| `version` | Application version | `0.4.19.5` |
| `branch` | Configured update branch | `develop` |
| `os` | Coarse platform family — **not** a full OS version string | `linux`, `windows`, `macos`, `docker` |
| `arch` | CPU architecture | `x64`, `arm64` |
| `runtimeVersion` | .NET runtime version | `10.0.0` |
| `dbType` | Database engine in use | `sqlite`, `postgres` |
| `usingCustomMetadataSource` | Whether the metadata mirror has been overridden from the built-in default | `true` / `false` |

Notably absent, on purpose: an install identifier of any kind, library contents, file or folder
paths, indexer or download-client names or URLs, API keys, and the actual metadata mirror URL (an
operator's self-hosted mirror is itself potentially identifying — only whether it was customized is
sent, never the value).

## Default: opt-in

`AnalyticsEnabled` defaults to **off**. This is a personal fork collecting data from other people's
servers; upstream's opt-out default was never revisited on its own merits, and isn't the right call
here. An operator has to explicitly flip the setting (or set the equivalent environment variable)
before anything is ever sent.

## Identity: anonymous, no stable ID

Every report is a stateless snapshot with no persistent identifier attached. The ingest endpoint
aggregates incoming reports by `(date, version, branch, os, arch, runtimeVersion, dbType,
usingCustomMetadataSource)` into daily counters. There is no way — even in principle, even for the
endpoint operator — to tell that two reports came from the same install. This was a deliberate
trade-off: it makes "how many installs upgraded this week" unanswerable, in exchange for a
materially simpler privacy story. That trade was judged worth it.

## Retention: aggregate only

Individual reports are folded into daily aggregate counters and are not retained as individual
records. No IP address is stored beyond what's needed for in-flight rate limiting at the edge, and
that is never written to durable storage.

## Failure behavior

Analytics is the least important thing this application does. Reporting failures — network
errors, endpoint downtime, timeouts — are silent and non-blocking: no startup delay, no health
check warning, no log line above debug level. A dropped report is simply a dropped report.

## Relationship to crash reporting

This fork also has a real, working Sentry crash-reporting pipeline (`READARR__SENTRY_DSN`), unrelated
to and pre-dating this decision. Historically it shared the same `AnalyticsEnabled` flag as this
piggybacked `active` parameter, which meant there was no way to opt into crash reporting without
also opting into the (dead) analytics flag, or vice versa. That coupling has been removed: crash
reporting is now controlled by its own `CrashReportingEnabled` flag (default **on**, matching
today's behavior — it still only activates if `READARR__SENTRY_DSN` is set). `AnalyticsEnabled` now
governs the reporter described in this document, and nothing else.

## Known cleanup pending #8

A few pieces of the old, dead wiring reference `AnalyticsEnabled` today and will be removed as part
of implementing the real reporter, not here:

- `InitializeJsonController` currently writes an unused `analytics` boolean and a `userHash`
  (a stable, machine-derived anonymous token) into `initialize.json` for a frontend tracker that
  was never built. That conflicts with the "no stable ID" decision above and has no consumer — it's
  slated for deletion.
- `UpdatePackageProvider` still appends the piggybacked `active` query parameter to update-check
  requests; #8 removes it once the real reporter exists.
- The `AnalyticsSettings` UI toggle's help text overstates what was ever collected (it claims
  browser and page-level tracking that never existed); #8 rewrites it to match this document.
