#!/bin/sh
# Runs at container start, before nginx starts. One image runs in every
# Environment this way.
#
# 1. Writes /config.json from every PLATFORM_* setting and every setting whose
#    name starts with PUBLIC_, and nothing else, so no secret reaches the
#    browser. The app reads it in src/lib/config.ts.
# 2. Writes the /api upstream into the nginx configuration: API_UPSTREAM when
#    it is set (docker-compose.yml sets http://api:8080), otherwise the api app
#    of the stamp, http://ca-${PLATFORM_STAMP}-api. Stops with an error when
#    neither is there.
set -eu

out=/usr/share/nginx/config/config.json

awk '
  function quote(s) {
    gsub(/\\/, "\\\\", s)
    gsub(/"/, "\\\"", s)
    gsub(/\n/, "\\n", s)
    gsub(/\r/, "\\r", s)
    gsub(/\t/, "\\t", s)
    return "\"" s "\""
  }
  BEGIN {
    printf "{"
    sep = ""
    for (name in ENVIRON) {
      if (name ~ /^(PLATFORM|PUBLIC)_[A-Za-z0-9_]+$/) {
        printf "%s%s:%s", sep, quote(name), quote(ENVIRON[name])
        sep = ","
      }
    }
    print "}"
  }
' > "$out"

echo "$0: wrote $out"

if [ -n "${API_UPSTREAM:-}" ]; then
  upstream=$API_UPSTREAM
elif [ -n "${PLATFORM_STAMP:-}" ]; then
  upstream="http://ca-${PLATFORM_STAMP}-api"
else
  echo "$0: error: set API_UPSTREAM, or PLATFORM_STAMP in a stamp, so /api reaches the API" >&2
  exit 1
fi

# Only a scheme, a host name and a port: the path stays the request's own.
if ! printf '%s' "$upstream" | grep -Eq '^https?://[A-Za-z0-9.-]+(:[0-9]+)?$'; then
  echo "$0: error: the /api upstream '$upstream' isn't http(s)://<host>[:<port>]" >&2
  exit 1
fi

printf 'proxy_pass %s;\n' "$upstream" > /etc/nginx/upstream/api.conf

echo "$0: /api goes to $upstream"
