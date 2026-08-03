# syntax=docker/dockerfile:1
#
# Image for this Readarr fork.
#
# This expects the application to have been built already and staged into
# docker/artifacts/<arch>/ — see .github/workflows/docker.yml. Building the app
# inside the Dockerfile would mean running the whole .NET SDK under emulation for
# the arm64 image; staging prebuilt output keeps the emulated work down to a
# single apt layer.
#
# To build locally:
#   ./build.sh --backend --frontend --packages -f net10.0 -r linux-x64
#   mkdir -p docker/artifacts/amd64
#   cp -r _artifacts/linux-x64/net10.0/Readarr/. docker/artifacts/amd64/
#   docker build -t readarr:local .

# The distro is pinned rather than taking the bare 10.0 tag, which resolves to a
# different base than the 6.0 image used: noble is Ubuntu 24.04, so the package
# names below and setpriv in the entrypoint behave as they did on jammy. The
# patch level is deliberately left floating - the build sets
# TargetLatestRuntimePatch, so the app asks for whatever patch it was built
# against and a pinned base would have to be bumped in lockstep with the SDK.
ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-noble
FROM ${BASE_IMAGE}

# Readarr.Host uses Microsoft.NET.Sdk.Web, so it carries an implicit framework
# reference to Microsoft.AspNetCore.App. The aspnet base image is required —
# dotnet/runtime alone is not enough.

ARG TARGETARCH
ARG READARR_VERSION=unknown
ARG VCS_REF=unknown

LABEL org.opencontainers.image.title="Readarr (Irishsmurf fork)" \
      org.opencontainers.image.description="Book manager and automation, continued as a personal fork after the upstream project was archived" \
      org.opencontainers.image.source="https://github.com/Irishsmurf/Readarr" \
      org.opencontainers.image.licenses="GPL-3.0-only" \
      org.opencontainers.image.version="${READARR_VERSION}" \
      org.opencontainers.image.revision="${VCS_REF}"

ENV READARR_CONFIG_DIR=/config \
    XDG_CONFIG_HOME=/config \
    PUID=1000 \
    PGID=1000 \
    UMASK=002 \
    DOTNET_EnableDiagnostics=0

# This fork's own update-check + analytics service (see docs/ingest-endpoint.md
# and https://github.com/Irishsmurf/Readarr-Analytics) - not a third party, so
# defaulting to it here doesn't violate the "never talk to a host the operator
# didn't choose" posture the rest of this fork holds to. Turns on update checks
# by default; analytics reporting stays opt-in via the separate AnalyticsEnabled
# setting (Settings > General) - the two share this one gate by design, see
# docs/analytics.md's "shared gating" decision. Override or disable with:
#   docker run -e READARR__SERVICES_URL=https://your-own-endpoint ...
#   docker run -e READARR__SERVICES_URL= ...   # disables both entirely
ENV READARR__SERVICES_URL=https://readarr-analytics.paddez.com

# System.Data.SQLite P/Invokes the system libsqlite3 rather than bundling it —
# AssemblyLoader maps "sqlite3" to "libsqlite3.so.0" on Linux — and the aspnet
# base image does not ship it. Without this the app builds and starts, then dies
# with DllNotFoundException the moment it opens the database.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libsqlite3-0 \
    && rm -rf /var/lib/apt/lists/*

# The staging step drops Readarr.Update: the in-app updater would replace these
# binaries with an upstream build that does not carry this fork's fixes. Image
# updates come from pulling a new tag instead.
COPY --chmod=0755 docker/artifacts/${TARGETARCH}/ /app/readarr/
COPY --chmod=0755 docker/entrypoint.sh /usr/local/bin/entrypoint.sh

VOLUME /config
EXPOSE 8787

# /dev/tcp is a bash builtin — /bin/sh is dash on this base image.
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD [ "/bin/bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8787" ]

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
