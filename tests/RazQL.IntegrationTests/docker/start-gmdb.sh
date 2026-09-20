#!/usr/bin/env bash
set -Eeuo pipefail

: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
: "${GMDB_APP_PASSWORD:?GMDB_APP_PASSWORD is required}"
: "${GMDB_ADMIN_PASSWORD:?GMDB_ADMIN_PASSWORD is required}"

if [[ "${POSTGRES_DB:-}" != gmdb ]]; then
    echo 'GMDB migrations require POSTGRES_DB=gmdb.' >&2
    exit 1
fi

ready_file=/tmp/razql-gmdb-ready
rm -f "$ready_file"

docker-entrypoint.sh "$@" &
postgres_pid=$!

cleanup() {
    status=$?
    trap - EXIT INT TERM
    rm -f "$ready_file"
    if kill -0 "$postgres_pid" 2>/dev/null; then
        kill -TERM "$postgres_pid"
        wait "$postgres_pid" || true
    fi
    exit "$status"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

postgres_ready=false
for ((attempt = 0; attempt < 120; attempt++)); do
    if pg_isready -h 127.0.0.1 -p 5432 -U "$POSTGRES_USER" -d gmdb >/dev/null 2>&1; then
        postgres_ready=true
        break
    fi

    if ! kill -0 "$postgres_pid" 2>/dev/null; then
        echo 'PostgreSQL exited before becoming ready.' >&2
        wait "$postgres_pid"
        exit 1
    fi

    sleep 1
done

if [[ "$postgres_ready" != true ]]; then
    echo 'PostgreSQL did not become ready within 120 seconds.' >&2
    exit 1
fi

export PGSQL_HOST=127.0.0.1
export PGSQL_PORT=5432
export GMDB_SUPERUSER="$POSTGRES_USER"
export GMDB_SUPERUSER_PASSWORD="$POSTGRES_PASSWORD"

echo 'Bootstrapping GMDB database.'
SPRING_PROFILES_ACTIVE=bootstrap java -jar /opt/gmdb/gmdb-liquibase.jar

echo 'Applying GMDB migrations.'
SPRING_PROFILES_ACTIVE=migrate java -jar /opt/gmdb/gmdb-liquibase.jar

touch "$ready_file"
echo 'GMDB database is ready.'

wait "$postgres_pid"
