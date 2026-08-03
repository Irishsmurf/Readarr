#!/usr/bin/env python3
"""Publish this release's binaries to the Readarr-Analytics service.

Called from .github/workflows/release.yml after the GitHub Release is
created. Populates the `releases` table that GET /update/:branch reads from
(see docs/ingest-endpoint.md) - nothing else does this, so until this step
runs successfully for a given version, installs never see it as an update.

One POST /releases call per os/arch binary this workflow's build matrix
produces (see release.yml's `build` job) - the combos list below mirrors
that matrix exactly and must be kept in sync with it.
"""

import json
import os
import re
import sys
import urllib.error
import urllib.request

COMBOS = [
    {"rid": "linux-x64", "asset_arch": "x64", "os": "linux", "client_arch": "X64"},
    {"rid": "linux-arm64", "asset_arch": "arm64", "os": "linux", "client_arch": "Arm64"},
    {"rid": "linux-arm", "asset_arch": "arm", "os": "linux", "client_arch": "Arm"},
    {"rid": "win-x64", "asset_arch": "win-x64", "os": "windows", "client_arch": "X64"},
    {"rid": "osx-x64", "asset_arch": "osx-x64", "os": "osx", "client_arch": "X64"},
    {"rid": "osx-arm64", "asset_arch": "osx-arm64", "os": "osx", "client_arch": "Arm64"},
]


def archive_name(version, asset_arch):
    ext = "zip" if asset_arch.startswith("win-") else "tar.gz"
    return f"Readarr.{version}.{asset_arch}.{ext}"


def parse_changes(changelist_path, version):
    """Returns {"new": [...], "fixed": [...]} for `version`'s CHANGELIST.md
    section. "Added" bullets become `new`; every other Keep-a-Changelog
    heading (Fixed, Changed, Removed, Deprecated, Security) becomes `fixed` -
    the wire contract only has two buckets. Falls back to empty lists if the
    version has no changelog section (e.g. a manually re-run workflow_dispatch
    for an already-released tag)."""
    text = open(changelist_path, encoding="utf-8").read()

    section_pattern = rf"## \[{re.escape(version)}\].*?\n(.*?)(?=\n## \[|\Z)"
    section_match = re.search(section_pattern, text, re.DOTALL)
    if not section_match:
        return {"new": [], "fixed": []}

    body = section_match.group(1)
    new_items = []
    fixed_items = []

    for heading, heading_body in re.findall(r"### (\w+)\n(.*?)(?=\n### |\Z)", body, re.DOTALL):
        bucket = new_items if heading == "Added" else fixed_items
        for line in heading_body.splitlines():
            if line.startswith("- "):
                bucket.append(line[2:].strip())

    return {"new": new_items, "fixed": fixed_items}


def parse_hashes(sha256sums_path):
    hashes = {}
    with open(sha256sums_path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            digest, _, filename = line.partition("  ")
            hashes[filename.strip()] = f"sha256:{digest.strip()}"
    return hashes


def publish(service_url, token, payload):
    request = urllib.request.Request(
        url=service_url.rstrip("/") + "/releases",
        data=json.dumps(payload).encode("utf-8"),
        method="POST",
        headers={
            "Content-Type": "application/json",
            "Authorization": f"Bearer {token}",
        },
    )
    with urllib.request.urlopen(request, timeout=30) as response:
        return response.status


def main():
    service_url = os.environ["ANALYTICS_SERVICE_URL"]
    token = os.environ["ANALYTICS_RELEASE_TOKEN"]
    version = os.environ["VERSION"]
    tag = os.environ["TAG"]
    repository = os.environ["GITHUB_REPOSITORY"]
    assets_dir = os.environ.get("RELEASE_ASSETS_DIR", "release-assets")

    changes = parse_changes("CHANGELIST.md", version)
    hashes = parse_hashes(os.path.join(assets_dir, "SHA256SUMS.txt"))
    release_date = os.environ["RELEASE_DATE"]

    failures = []
    for combo in COMBOS:
        file_name = archive_name(version, combo["asset_arch"])
        asset_path = os.path.join(assets_dir, file_name)
        if not os.path.isfile(asset_path):
            failures.append(f"{file_name}: expected build artifact is missing")
            continue

        if file_name not in hashes:
            failures.append(f"{file_name}: no checksum in SHA256SUMS.txt")
            continue

        payload = {
            "branch": "main",
            "version": version,
            "os": combo["os"],
            "arch": combo["client_arch"],
            "releaseDate": release_date,
            "fileName": file_name,
            "url": f"https://github.com/{repository}/releases/download/{tag}/{file_name}",
            "hash": hashes[file_name],
            "changes": changes,
        }

        try:
            status = publish(service_url, token, payload)
            print(f"published {file_name} ({combo['os']}/{combo['client_arch']}): {status}")
        except urllib.error.URLError as exc:
            failures.append(f"{file_name}: {exc}")

    if failures:
        for failure in failures:
            print(f"::error::{failure}")
        sys.exit(f"{len(failures)} of {len(COMBOS)} release(s) failed to publish")


if __name__ == "__main__":
    main()
