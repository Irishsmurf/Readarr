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
* **No official builds.** Upstream's Azure pipeline does not run here. Build from source (see below) or keep using an existing Readarr install and apply changes yourself.
* **Issues and PRs are welcome**, and are read — just don't expect a fast turnaround. Please do not open issues on the archived upstream repository; use [this fork's issue tracker](https://github.com/Irishsmurf/Readarr/issues).
* **Not affiliated with the Servarr team.** Please don't ask them to support anything in this fork.

### You still need a metadata mirror

The official Readarr metadata server is gone, and nothing in this fork replaces it. To have a usable install you need a third-party metadata mirror; the most widely used one is [rreading-glasses](https://github.com/blampe/rreading-glasses). Those mirrors are maintained by other people — this fork is not involved with them and cannot support them. Use them at your own risk.

### Branches

* `main` — the primary branch for this fork. Work lands here.
* `develop` — retained to line up with upstream's original branch layout and history.

## Changes in This Fork

Changes made here on top of the final upstream commit (`0b79d30`, "Retirement announcement"):

* **qBittorrent 5.2.0+ login fix** — qBittorrent 5.2.0 changed a successful `POST /api/v2/auth/login` from `200 OK` with the body `Ok.` to `204 No Content` with an empty body. Readarr only accepted the literal `Ok.` body, so every successful login against qBittorrent 5.2.0 or newer was reported as an authentication failure even with correct credentials. Both proxy versions now accept `204 No Content` as success. ([#1](https://github.com/Irishsmurf/Readarr/issues/1), [#2](https://github.com/Irishsmurf/Readarr/pull/2))

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

## Building From Source

Requirements:

* .NET SDK 6.0
* Node.js and Yarn (for the frontend)

```bash
git clone https://github.com/Irishsmurf/Readarr.git
cd Readarr
yarn install
./build.sh --backend --frontend --packages
```

Useful flags: `--backend`, `--frontend`, `--packages`, `--lint`. Build output lands in `_output/`. Run the test suites with `./test.sh`.

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
