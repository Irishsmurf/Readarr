<div align="center">

<img src="Logo/Readarr.svg" width="200" alt="Readarr Logo" />

# Readarr — Community Fork

**Book manager and automation for your ebook and audiobook library.**
A community-maintained fork of the retired [Readarr](https://github.com/Readarr/Readarr) project.

[![Tests](https://github.com/Irishsmurf/Readarr/actions/workflows/test.yml/badge.svg?branch=main)](https://github.com/Irishsmurf/Readarr/actions/workflows/test.yml)
[![Docker](https://github.com/Irishsmurf/Readarr/actions/workflows/docker.yml/badge.svg?branch=main)](https://github.com/Irishsmurf/Readarr/actions/workflows/docker.yml)
[![Release](https://github.com/Irishsmurf/Readarr/actions/workflows/release.yml/badge.svg)](https://github.com/Irishsmurf/Readarr/actions/workflows/release.yml)
[![GitHub Release](https://img.shields.io/github/v/release/Irishsmurf/Readarr?include_prereleases&label=latest)](https://github.com/Irishsmurf/Readarr/releases/latest)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue)](LICENSE.md)

<img src="frontend/src/Content/Images/poster-dark.png" alt="Readarr poster" height="160" />

</div>

---

> [!IMPORTANT]
> Upstream Readarr was **officially retired on June 27, 2025** by the Servarr team.
> This fork carries forward security updates and bug fixes. It is **not affiliated with
> the Servarr team** — please don't ask them to support anything here.

---

## ✨ What This Fork Adds

All changes on top of the final upstream commit (`0b79d30` — "Retirement announcement"):

| Change | Details |
|--------|---------|
| 🐛 **qBittorrent 5.2.0+ login fix** | qBit changed a successful auth response from `200 Ok.` to `204 No Content`. Readarr only accepted the literal body, so every login against qBit 5.2.0+ was reported as an auth failure. ([#1](https://github.com/Irishsmurf/Readarr/issues/1), [#2](https://github.com/Irishsmurf/Readarr/pull/2)) |
| 🔒 **ImageSharp security update** | `SixLabors.ImageSharp` 3.1.7 → 3.1.12 ([GHSA-rxmq-m78w-7wmc](https://github.com/advisories/GHSA-rxmq-m78w-7wmc)). ImageSharp decodes cover art from third-party mirrors — untrusted input. Also `MailKit` 4.8.0 → 4.17.0 ([GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr)). ([#9](https://github.com/Irishsmurf/Readarr/issues/9)) |
| ⚡ **.NET 10** | All projects now target `net10.0`. .NET 6 reached end-of-life in November 2024 and no longer receives security patches. ([#10](https://github.com/Irishsmurf/Readarr/issues/10)) |
| 🔕 **No upstream telemetry** | Crash reporting and update checks no longer contact the retired project's infrastructure. Both require explicit opt-in. |
| 🐛 **Quality Profile null fix** | `ArgumentNullException` when `FormatItems` was null in the validator — now returns a user-friendly error instead of a 500. ([#28](https://github.com/Irishsmurf/Readarr/issues/28), [#29](https://github.com/Irishsmurf/Readarr/pull/29)) |
| 🚀 **CI/CD overhaul** | Tests on every PR; Docker images only on `main` and release tags; automated multi-platform GitHub Releases. |

See [CHANGELIST.md](CHANGELIST.md) for the full versioned history.

---

## 🚀 Quick Start

### Docker (recommended)

Multi-arch images (`linux/amd64`, `linux/arm64`) are published to GHCR:

```bash
docker pull ghcr.io/irishsmurf/readarr:latest
```

**Docker Compose:**

```yaml
services:
  readarr:
    image: ghcr.io/irishsmurf/readarr:latest
    container_name: readarr
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Etc/UTC
    volumes:
      - /path/to/config:/config
      - /path/to/books:/books
      - /path/to/downloads:/downloads
    ports:
      - 8787:8787
    restart: unless-stopped
```

The image follows linuxserver.io conventions (`PUID`, `PGID`, `UMASK`, config at `/config`, port `8787`), so an existing LSIO compose file works with only the image line changed.

> [!WARNING]
> **The in-app updater is removed.** It would download an upstream build and silently undo this fork's fixes. Update by pulling a new image tag instead.

> [!TIP]
> **Back up `/config/readarr.db` before your first switch.** There are no schema migrations between 0.4.18 and current `main`, so rolling back works — but a backup costs nothing.

### Image Tags

| Tag | When it updates |
|-----|----------------|
| `latest` | Every push to `main` |
| `0.4.19.1` | Pinned release tag (recommended for production) |
| `sha-<short>` | Exact commit — maximum reproducibility |

---

## ⚙️ Upstream Services

The codebase was wired to three hosts belonging to the retired project. Built from source, this
fork does not contact any of them unless you explicitly opt in. The one exception is the official
Docker image, which bakes in `READARR__SERVICES_URL` pointing at this fork's own
[Readarr-Analytics](https://github.com/Irishsmurf/Readarr-Analytics) service — not a third
party — so update checks work out of the box; analytics reporting stays opt-in regardless (see
below), since the two are gated separately even though they share that one URL.

| Service | Default | How to enable / disable |
|---------|---------|---------------|
| Crash reporting (`sentry.servarr.com`) | **Off** — upstream DSNs are gone | Set `READARR__SENTRY_DSN` to your own Sentry DSN |
| Update checks (`readarr.servarr.com` upstream) | **Off** from source. **On** in the official Docker image, against this fork's own service | Source: set `READARR__SERVICES_URL` yourself. Docker: override it, or set it to an empty string to disable — see the `ENV` in [`Dockerfile`](Dockerfile) |
| Metadata (`api.bookinfo.club`) | Fallback only | Settings → Metadata Source |
| Analytics (this fork's own endpoint) | **Off** — opt-in, everywhere, regardless of `READARR__SERVICES_URL` | Settings → General → Analytics. Scope, field list and retention: [`docs/analytics.md`](docs/analytics.md) |

### You still need a metadata mirror

The official Readarr metadata server is gone and nothing in this fork replaces it. The most widely used third-party mirror is **[rreading-glasses](https://github.com/blampe/rreading-glasses)**. It is maintained by other people — this fork is not involved and cannot support it. Use it at your own risk.

---

## 🔨 Building from Source

**Requirements:**
- .NET SDK 10.0
- Node.js 20 and Yarn 1.x

```bash
git clone https://github.com/Irishsmurf/Readarr.git
cd Readarr
yarn install
./build.sh --backend --frontend --packages
```

Packaged output lands in `_artifacts/<rid>/net10.0/Readarr/`. Run tests with `./test.sh`.

> [!NOTE]
> `--backend` clears `_output/` at the start of every run, so it must come before `--frontend`.
> The ordering above is deliberate.

**Build the Docker image locally:**

```bash
mkdir -p docker/artifacts/amd64
cp -r _artifacts/linux-x64/net10.0/Readarr/. docker/artifacts/amd64/
rm -rf docker/artifacts/amd64/Readarr.Update
docker build -t readarr:local .
```

---

## 🌟 Features

<div align="center">
<img src="frontend/src/Content/Images/poster-dark-square.png" alt="Readarr UI" height="180" />
</div>

- 📖 **Automatic upgrades** — watches for better quality editions and upgrades automatically *(e.g. PDF → AZW3)*
- 🔍 **Library scanning** — scans your existing library and downloads missing books
- 📡 **Indexer support** — Newznab, Torznab, and more
- ⬇️ **Download client integration** — SABnzbd, NZBGet, qBittorrent, Deluge, rTorrent, Transmission, uTorrent
- 📚 **Calibre integration** — add to library and convert formats *(requires Calibre Content Server)*
- 🔁 **Automatic failed download handling** — tries another release if one fails
- 🎛️ **Advanced quality profiles** — always get the copy you want
- ✏️ **Fully configurable renaming**
- 🌍 **Cross-platform** — Windows, Linux, macOS, Raspberry Pi

---

## 🤝 Branches

| Branch | Purpose |
|--------|---------|
| `main` | Primary branch — all work lands here |
| `develop` | Retained to align with upstream's original layout and history |

---

## 📚 Documentation & Support

The upstream documentation still applies to the application itself (excluding anything about official metadata or official support):

- [Servarr Wiki (Readarr)](https://wiki.servarr.com/readarr) — setup and configuration reference
- [API documentation](https://readarr.com/docs/api/)
- [Contributing guide](CONTRIBUTING.md)
- [Security policy](SECURITY.md)

Support for **this fork** is via [GitHub Issues](https://github.com/Irishsmurf/Readarr/issues) only.
The Servarr Discord does not support this fork.

---

## 🙏 Credits

All of the real work here was done by the Readarr and Servarr teams, their contributors, and the Sonarr project this codebase descends from. This fork is a thin maintenance layer on top of many years of other people's work.

- [Upstream contributors](https://github.com/Readarr/Readarr/graphs/contributors)
- [Readarr on Open Collective](https://opencollective.com/Readarr) — upstream's backers and sponsors

---

## 📄 License

[GNU GPL v3](LICENSE.md) — Copyright 2010-2025
