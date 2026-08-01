#!/bin/sh
# Entrypoint for the Readarr fork image.
#
# Honours the PUID/PGID/UMASK convention used by the linuxserver.io images so
# that an existing compose file keeps working when the image is swapped out.
set -e

PUID=${PUID:-1000}
PGID=${PGID:-1000}
UMASK=${UMASK:-002}

umask "$UMASK"

mkdir -p "$READARR_CONFIG_DIR"

# Only root can drop privileges. If the container was started with an explicit
# --user, run as whoever that is and leave ownership alone.
if [ "$(id -u)" != "0" ]; then
    exec /app/readarr/Readarr -nobrowser -data="$READARR_CONFIG_DIR" "$@"
fi

# chown is best effort: on a bind mount backed by a filesystem that does not
# support it, failing here would be worse than letting Readarr report the
# problem itself.
chown -R "$PUID:$PGID" "$READARR_CONFIG_DIR" 2>/dev/null || true

# setpriv comes from util-linux in the base image, so no extra package is
# needed. --clear-groups avoids requiring the uid to exist in /etc/passwd.
exec setpriv --reuid="$PUID" --regid="$PGID" --clear-groups \
    /app/readarr/Readarr -nobrowser -data="$READARR_CONFIG_DIR" "$@"
