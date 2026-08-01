# Readarr (Community Fork)

[![License: GPL v3](https://img.shields.io/badge/license-GPL%20v3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Fork status: personally maintained](https://img.shields.io/badge/fork-personally%20maintained-orange.svg)](#about-this-fork)

Readarr is an ebook and audiobook collection manager for Usenet and BitTorrent users. It can monitor multiple RSS feeds for new books from your favorite authors and will grab, sort, and rename them.

Note that only one type of a given book is supported. If you want both an audiobook and an ebook of a given book you will need multiple instances.

## About This Fork

The upstream [Readarr/Readarr](https://github.com/Readarr/Readarr) project was [retired and archived by the Servarr team in June 2025](https://github.com/Readarr/Readarr/blob/develop/README.md). Their reasoning, in short: the official metadata service had become unusable, there was no time left to rebuild it, and the community effort to move to Open Library had stalled.

This repository is a personal fork of that codebase, maintained by [@Irishsmurf](https://github.com/Irishsmurf). It exists so that the bugs I hit in my own setup actually get fixed instead of sitting in an archived repo forever.

**What that means in practice:**

* **Best effort, no guarantees.** This is a spare-time project. There is no roadmap, no release schedule, and no support commitment.
* **Bug fixes over new features.** The focus is keeping an already-working install working — download client compatibility, breakages against newer third-party APIs, and similar. Large new features are unlikely.
* **Docker images are published from this repo.** Every push to `main` builds and publishes a multi-arch image to GHCR (see [Installation](#installation)). Upstream's Azure pipeline does not run here.
* **Issues and PRs are welcome**, and are read — just don't expect a fast turnaround. Please do not open issues on the archived upstream repository; use [this fork's issue tracker](https://github.com/Irishsmurf/Readarr/issues).
* **Not affiliated with the Servarr team.** Please don't ask them to support anything in this fork.

### You still need a metadata mirror

The official Readarr metadata server is gone, and nothing in this fork replaces it. To have a usable install you need a third-party metadata mirror; the most widely used one is [rreading-glasses](https://github.com/blampe/rreading-glasses). Those mirrors are maintained by other people — this fork is not involved with them and cannot support them. Use them at your own risk.

### Upstream services are off by default

The codebase was wired to three hosts belonging to the retired project. This fork does not contact any of them unless you ask it to:

| What | Default here | How to enable |
| --- | --- | --- |
| Crash reporting (`sentry.servarr.com`) | **Off** — upstream's DSNs are gone | Set `READARR__SENTRY_DSN` to your own Sentry DSN |
| Update checks (`readarr.servarr.com`) | **Off** — no request is made | Set `READARR__SERVICES_URL` to your own update endpoint |
| Metadata (`api.bookinfo.club`) | Fallback only | Settings → Metadata Source, e.g. an [rreading-glasses](https://github.com/blampe/rreading-glasses) instance |

Crash reports previously went to the Servarr team's Sentry instance, and update checks could only ever offer upstream builds — which lack this fork's fixes, so installing one would quietly undo them. Both now require opting in to infrastructure you control.

### Branches

* `main` — the primary branch for this fork. Work lands here.
* `develop` — retained to line up with upstream's original branch layout and history.

## Changes in This Fork

Changes made here on top of the final upstream commit (`0b79d30`, "Retirement announcement"):

* **qBittorrent 5.2.0+ login fix** — qBittorrent 5.2.0 changed a successful `POST /api/v2/auth/login` from `200 OK` with the body `Ok.` to `204 No Content` with an empty body. Readarr only accepted the literal `Ok.` body, so every successful login against qBittorrent 5.2.0 or newer was reported as an authentication failure even with correct credentials. Both proxy versions now accept `204 No Content` as success. ([#1](https://github.com/Irishsmurf/Readarr/issues/1), [#2](https://github.com/Irishsmurf/Readarr/pull/2))
* **Security update to ImageSharp** — `SixLabors.ImageSharp` 3.1.7 → 3.1.12 ([GHSA-rxmq-m78w-7wmc](https://github.com/advisories/GHSA-rxmq-m78w-7wmc)). ImageSharp decodes cover art fetched from third-party metadata mirrors, so it handles untrusted input. `MailKit` remains on 4.8.0 with a known advisory ([GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr)) that **cannot be fixed while the project targets .NET 6** — 4.8.0 is the last release with a `net6.0` target, and the fix landed in 4.9.0. Tracked in [#9](https://github.com/Irishsmurf/Readarr/issues/9).
* **No telemetry to upstream** — crash reporting and update checks no longer contact the retired project's infrastructure. See [Upstream services are off by default](#upstream-services-are-off-by-default).
* **CI** — every push and pull request builds the app, runs the unit test suite, and builds and smoke-tests the Docker image.

## Major Features Include

* Can watch for better quality of the ebooks and audiobooks you have and do an automatic upgrade. *e.g. from PDF to AZW3*
* Support for major platforms: Windows, Linux, macOS, Raspberry Pi, etc.
* Automatically detects new books
* Can scan your existing library and download any missing books
* Automatic failed download handling will try another release if one fails
* Manual search so you can pick any release or to see why a release was not downloaded automatically
* Advanced customization for profiles, such that Readarr will always download the copy you want
* Fully configurable book renaming
* SABnzbd, NZBGet, QBittorrent, Deluge, rTorrent, Transmission, uTorrent, and other download clients are supported and integrated
* Full integration with Calibre (add to library, conversion) (Requires Calibre Content Server)
* And a beautiful UI

## Installation

Multi-arch images (`linux/amd64`, `linux/arm64`) are published to GHCR on every push to `main`:

```bash
docker pull ghcr.io/irishsmurf/readarr:latest
```

Tags: `latest` tracks `main`, and each build also gets an immutable `0.4.19.<build>` tag plus a `sha-<short>` tag. Pin to a version tag if you'd rather upgrade deliberately.

The image follows the linuxserver.io conventions (`PUID`, `PGID`, `UMASK`, config at `/config`, port `8787`), so an existing LSIO compose file works with only the image line changed:

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

Two differences from the linuxserver.io image worth knowing:

* **The in-app updater is removed.** It would download an upstream build and overwrite this fork's fixes. Update by pulling a new image tag.
* **Back up `/config/readarr.db` before your first switch.** There are no schema migrations between 0.4.18 and current `main`, so rolling back to the LSIO image works, but a backup costs nothing.

## Building From Source

Requirements:

* .NET SDK 6.0 — `global.json` pins the build to the 6.0 band, so a newer SDK alone will not do
* Node.js 20 and Yarn 1.x (for the frontend)

```bash
git clone https://github.com/Irishsmurf/Readarr.git
cd Readarr
yarn install
./build.sh --backend --frontend --packages
```

Useful flags: `--backend`, `--frontend`, `--packages`, `--lint`. Note that `--backend` clears `_output/` at the start of every run, so it must come before `--frontend` — the ordering above is deliberate. Packaged output lands in `_artifacts/<rid>/net6.0/Readarr/`. Run the test suites with `./test.sh`.

To build the Docker image locally, stage the packaged output where the `Dockerfile` expects it:

```bash
mkdir -p docker/artifacts/amd64
cp -r _artifacts/linux-x64/net6.0/Readarr/. docker/artifacts/amd64/
rm -rf docker/artifacts/amd64/Readarr.Update
docker build -t readarr:local .
```

The upstream [development wiki page](https://wiki.servarr.com/readarr/contributing) still describes the general layout of the codebase and remains a reasonable reference, even though the project itself is retired.

## Documentation & Support

The upstream documentation still applies to the application itself, with the caveat that anything about official metadata or official support is no longer accurate:

* [Servarr wiki (Readarr)](https://wiki.servarr.com/readarr) — setup and configuration reference
* [API documentation](https://readarr.com/docs/api/)

Support for **this fork** is via [GitHub issues on this repository](https://github.com/Irishsmurf/Readarr/issues) only. The Servarr Discord does not support this fork.

## Credits

All of the real work here was done by the Readarr and Servarr teams and their contributors, and by the Sonarr project this codebase descends from. This fork is a thin layer on top of many years of other people's work.

* [Upstream contributors](https://github.com/Readarr/Readarr/graphs/contributors)
* [Readarr on Open Collective](https://opencollective.com/Readarr) — upstream's backers and sponsors

## License

* [GNU GPL v3](http://www.gnu.org/licenses/gpl.html)
* Copyright 2010-2025
