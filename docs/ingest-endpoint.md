# Ingest endpoint — requirements

This is the requirements document for [#7](https://github.com/Irishsmurf/Readarr/issues/7). It
specifies exactly what the service behind `READARR__SERVICES_URL` must implement. There are two
independent contracts it serves from a single base URL:

1. **Update-check** — already fully specified by this fork's existing client code
   (`UpdatePackageProvider`). Implementing this re-enables in-app update notifications pointing at
   this fork's own releases. This is usable today, independent of analytics.
2. **Analytics ingest** — a new contract, specified here for the first time, that
   [#8](https://github.com/Irishsmurf/Readarr/issues/8)'s reporter will implement against. The
   field list, defaults, identity model and retention rules come from
   [`docs/analytics.md`](analytics.md) (issue #6's decision record); this document adds the wire
   contract (route, method, payload shape) that #6 didn't need to specify.

Both share `READARR__SERVICES_URL` as their base — see [Shared gating](#shared-gating-with-update-checks)
for why analytics reuses the update-check env var instead of getting its own.

## 1. Update-check API

This contract already exists in code today (`src/NzbDrone.Core/Update/UpdatePackageProvider.cs`) —
nothing here is proposed, it's transcribed from what the client already sends and expects.

### 1.1 `GET /update/{branch}`

| Query param | Type | Example | Notes |
|---|---|---|---|
| `version` | string (Version) | `0.4.19.5` | current installed version |
| `os` | string, lowercase enum | `windows`, `linux`, `osx`, `linuxmusl`, `bsd` | see `NzbDrone.Common.EnvironmentInfo.Os` |
| `arch` | string | `X64`, `Arm64` | `RuntimeInformation.OSArchitecture.ToString()` |
| `runtime` | string constant | `netcore` | always this value today |
| `runtimeVer` | string | `10.0.0` | .NET runtime version |
| `dbType` | string | `SQLite`, `PostgreSQL` | `NzbDrone.Core.Datastore.DatabaseType` enum name |
| `includeMajorVersion` | bool | `true` | always sent as `true` today |
| `active` | bool, lowercase | `true` | **deprecated** — sent today only if `AnalyticsEnabled`; #8 removes it entirely. The endpoint must accept requests with or without it and must never require it. |

`{branch}` is a path segment, not an enum — accept arbitrary strings (`develop`, `main`, `nightly`,
etc.) and treat an unrecognized branch as "no update available" (see [Error handling](#error-handling--resilience)),
not as a 404/error.

Response body (`200`, JSON, camelCase — this codebase serializes with Newtonsoft's
`CamelCasePropertyNamesContractResolver`, see `NzbDrone.Common/Serializer/Newtonsoft.Json/Json.cs`):

```jsonc
// An update is available
{
  "available": true,
  "updatePackage": {
    "version": "0.4.20.0",
    "releaseDate": "2026-08-01T00:00:00Z",
    "fileName": "Readarr.develop.0.4.20.0.linux-core-x64.tar.gz",
    "url": "https://github.com/Irishsmurf/Readarr/releases/download/v0.4.20.0/Readarr.develop.0.4.20.0.linux-core-x64.tar.gz",
    "hash": "sha256:...",
    "branch": "develop",
    "changes": {
      "new": ["Added X"],
      "fixed": ["Fixed Y"]
    }
  }
}
```

```jsonc
// No update available (also the response for an unrecognized branch, or when the
// requesting version is already current/newer)
{
  "available": false,
  "updatePackage": null
}
```

The client (`UpdatePackageProvider.GetLatestUpdate`) reads only the `available` flag, then returns
`updatePackage` unmodified — every field in `updatePackage` above must be present and correctly
typed whenever `available` is `true`, or deserialization throws.

### 1.2 `GET /update/{branch}/changes`

Same query params as 1.1, minus `dbType`/`includeMajorVersion`, plus one addition:

| Query param | Type | Notes |
|---|---|---|
| `prevVersion` | string (Version), optional | only sent when the client has a previous version different from `version` — used to return the changelog range between the two |

Response body (`200`): a JSON array of the same `UpdatePackage` shape used above (not wrapped in an
`available`/`updatePackage` envelope this time):

```jsonc
[
  { "version": "0.4.20.0", "releaseDate": "...", "fileName": "...", "url": "...", "hash": "...", "branch": "develop", "changes": { "new": [...], "fixed": [...] } },
  { "version": "0.4.19.6", "releaseDate": "...", "fileName": "...", "url": "...", "hash": "...", "branch": "develop", "changes": { "new": [...], "fixed": [...] } }
]
```

An empty array (`[]`) is a valid, expected response when there's nothing newer.

### 1.3 Error handling & resilience (hard requirement)

Neither caller of this API has any exception handling around it today —
`CheckUpdateService.AvailableUpdate()` calls straight through, and
`HealthCheck/Checks/UpdateCheck.cs` calls `AvailableUpdate()` directly inside a health check with no
try/catch. `NzbDrone.Common.Http.HttpClient` throws `HttpException` on any non-2xx response or an
HTML body (`HttpClient.cs:118,289`).

**Consequence: a non-2xx response from either route will surface as an unhandled exception in a
scheduled health check**, which is exactly the "scary log line" issue #8 explicitly says analytics
(and, by the same logic, update checks) must never produce.

Requirement: the endpoint must return `200` with a well-formed body for every *expected* case —
unknown branch, no update available, empty changelog range — reserving non-2xx strictly for genuine
outages. There's no way to fully eliminate the risk of a real outage throwing here without also
patching the Readarr-side callers (out of scope for this endpoint, but worth flagging back to #8 or
a follow-up issue if it becomes a real problem).

### 1.4 Traffic shape

Scheduled via `TaskManager` at a 6-hour interval per install (`ApplicationUpdateCheckCommand`,
360-minute `Interval`). This is low, predictable, per-install traffic — design rate limiting
accordingly (see [§3.2](#32-rate-limiting)); there's no legitimate reason for one install to call
this more than a handful of times an hour.

## 2. Analytics ingest API (new contract, for #8 to implement against)

### 2.1 `POST /analytics`

Request body (JSON, camelCase, `Content-Type: application/json`) — exactly the field list from
[`docs/analytics.md`](analytics.md#field-allow-list), no more, no less:

```json
{
  "version": "0.4.19.5",
  "branch": "develop",
  "os": "linux",
  "arch": "X64",
  "runtimeVersion": "10.0.0",
  "dbType": "sqlite",
  "usingCustomMetadataSource": false
}
```

Field notes (reconciling `docs/analytics.md`'s illustrative examples with the concrete types
already established by the update-check API above, for consistency between the two contracts):

| Field | Type | Source |
|---|---|---|
| `version` | string | `BuildInfo.Version` |
| `branch` | string | `ConfigFileProvider.Branch` |
| `os` | string, lowercase enum | `NzbDrone.Common.EnvironmentInfo.Os` — `windows`/`linux`/`osx`/`linuxmusl`/`bsd`, same values as the update-check `os` param |
| `arch` | string | `RuntimeInformation.OSArchitecture.ToString()`, same as update-check `arch` |
| `runtimeVersion` | string | .NET runtime version |
| `dbType` | string, lowercase | `sqlite` / `postgres` |
| `usingCustomMetadataSource` | boolean | whether `ConfigFileProvider.MetadataSource` differs from the built-in default — never the URL itself |

`docs/analytics.md` additionally mentions "docker" as an illustrative `os` value; in practice
Docker-ness is orthogonal to OS (a container still reports `linux`). If Docker-vs-bare-metal is
worth distinguishing, add a separate `isDocker: boolean` field (backed by the existing
`IPlatformInfo.IsDocker`) rather than overloading `os` — flagged here as an open question for
whoever implements #8, not decided by this document.

Response: `204 No Content` (or `200` with an empty body). The reporter must not depend on any
response body — per issue #8's acceptance criteria, failures are silent and non-blocking, so the
client only cares whether the request succeeded well enough to not retry, never about payload
content.

No authentication header. No cookies. No CORS requirements — this is a server-to-server call from
the Readarr backend process, never from a browser.

### 2.2 Shared gating with update checks

The reporter in #8 should reuse `IReadarrCloudRequestBuilder.Services` — the same base URL and the
same `ServicesConfigured` guard `UpdatePackageProvider` already uses — rather than introducing a
second environment variable. Concretely: analytics only ever fires when **both**
`ConfigFileProvider.AnalyticsEnabled` is `true` **and** `READARR__SERVICES_URL` is set. This means:

- An operator who hasn't set `READARR__SERVICES_URL` sends nothing, ever, regardless of the
  `AnalyticsEnabled` toggle — no new way to accidentally contact a host that was never configured.
- One environment variable governs "does this install talk to any service I don't control," rather
  than splitting that decision across two.
- This is the same shape the old `active` query parameter used (gated on `AnalyticsEnabled`,
  transported over the already-configured `Services` request factory) — just given its own route
  and payload instead of being piggybacked onto an unrelated request.

## 3. Storage & operational requirements

### 3.1 Storage

Per `docs/analytics.md`'s retention decision: **aggregate-only**. Each `POST /analytics` should be
folded into a daily counter keyed by
`(date, version, branch, os, arch, runtimeVersion, dbType, usingCustomMetadataSource)` —
an `INSERT ... ON CONFLICT DO UPDATE SET count = count + 1`-style upsert, not an append-only events
table. The individual request is not retained after the counter is updated. No column for IP
address, user agent, or any other per-request identifier belongs in this table.

### 3.2 Rate limiting

Must not require identifying individual installs (no install ID exists to key on — see
`docs/analytics.md`'s identity decision). An ephemeral, in-memory/edge-level limiter keyed by
source IP (e.g. a sliding-window or token-bucket check that is never written to durable storage) is
sufficient — given the traffic shape in [§1.4](#14-traffic-shape), legitimate traffic per install is
a handful of requests per hour at most across both APIs combined.

### 3.3 No IP retention

No IP address may be persisted beyond the lifetime of an in-flight rate-limit check. This includes
default web-server/platform access logs — if the hosting choice logs client IPs by default (most
do), that logging must be disabled or reduced to a IP-free format for this service.

### 3.4 Failure behavior

Endpoint downtime must degrade silently from Readarr's perspective for the analytics route (see
issue #8) and must not return non-2xx for the update-check routes' *expected* cases (see
[§1.3](#13-error-handling--resilience-hard-requirement)). No specific uptime SLA — this is
explicitly "the least important thing the application does."

## 4. Hosting

Not mandated by this document — issue #7 leaves this open, and the decision doesn't have to be made
before #8 is built against the contract above. Options worth taking seriously, in rough order of
operational simplicity:

1. **Cloudflare Worker + D1.** A good fit for both halves: the update-check routes are simple reads
   (from a small releases table populated at release time, or proxied from a GitHub Releases API
   call), and D1's SQL makes the daily-counter upsert in [§3.1](#31-storage) a single statement.
   Free tier is very likely sufficient given the traffic shape in [§1.4](#14-traffic-shape).
   Cloudflare's edge network gives IP-based rate limiting close to free.
2. **Static file (update-check only) + a separate minimal analytics service.** Issue #7 itself notes
   the update half can be served with zero infrastructure — a JSON file per branch, published to a
   GitHub release or Pages, with the client's `{branch}` path segment mapping to a filename. This
   fully solves 1.1/1.2 with no server at all, but doesn't help with 2.1 (`POST` needs *something*
   to receive it) — would still need a small service for analytics alone.
3. **A small self-hosted container** next to whatever else is already self-hosted, with SQLite for
   the counters table. More ops burden than the other two, but avoids a third-party platform
   entirely.

Whichever is chosen, the source must live in this repo or a linked repo per issue #7's acceptance
criteria.

## 5. Acceptance criteria (from issue #7, unchanged)

- Endpoint deployed, with its source in this repo or a linked repo.
- `READARR__SERVICES_URL` pointed at it in a test install; update notification appears for a newer
  tag.
- Documented in the README next to the existing environment-variable table (already has an
  Analytics row from #6 pointing at `docs/analytics.md`; add/adjust once this endpoint is live and
  has a real URL to reference).
