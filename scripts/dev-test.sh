#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"
COMPOSE_FILE="$ROOT_DIR/docker/compose.yaml"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "Execute scripts/dev-up.sh --no-seed para criar a configuração local antes dos testes." >&2
    exit 1
fi

TEST_CONTAINER_NAME="financas-test-$(openssl rand -hex 8)"
TEST_POSTGRES_USER=financas_test
TEST_POSTGRES_PASSWORD="$(openssl rand -hex 32)"
TEST_POSTGRES_DB=financas_test_platform
TEST_POSTGRES_HOST=127.0.0.1
MAILPIT_WAS_RUNNING=false

if [[ -n "$(docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" ps --status running -q mailpit)" ]]; then
    MAILPIT_WAS_RUNNING=true
fi

cleanup_test_environment() {
    docker stop "$TEST_CONTAINER_NAME" >/dev/null 2>&1 || true
    if [[ "$MAILPIT_WAS_RUNNING" == false ]]; then
        docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" stop mailpit >/dev/null 2>&1 || true
    fi
}
trap cleanup_test_environment EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

docker run --detach --rm \
    --name "$TEST_CONTAINER_NAME" \
    --env "POSTGRES_USER=$TEST_POSTGRES_USER" \
    --env "POSTGRES_PASSWORD=$TEST_POSTGRES_PASSWORD" \
    --env "POSTGRES_DB=$TEST_POSTGRES_DB" \
    --publish 127.0.0.1::5432 \
    --tmpfs /var/lib/postgresql/data:rw,nosuid,nodev,size=1g \
    postgres:17-alpine >/dev/null

POSTGRES_PORT="$(docker port "$TEST_CONTAINER_NAME" 5432/tcp | awk -F: 'END {print $NF}')"
if [[ -z "$POSTGRES_PORT" ]]; then
    echo "Não foi possível determinar a porta do PostgreSQL descartável." >&2
    exit 1
fi

for attempt in $(seq 1 60); do
    if docker exec "$TEST_CONTAINER_NAME" pg_isready -U "$TEST_POSTGRES_USER" -d "$TEST_POSTGRES_DB" >/dev/null 2>&1; then
        break
    fi
    if ((attempt == 60)); then
        echo "PostgreSQL descartável não ficou pronto dentro do tempo esperado." >&2
        exit 1
    fi
    sleep 1
done

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d mailpit

PLATFORM_CONNECTION_STRING="Host=$TEST_POSTGRES_HOST;Port=$POSTGRES_PORT;Database=$TEST_POSTGRES_DB;Username=$TEST_POSTGRES_USER;Password=$TEST_POSTGRES_PASSWORD"
export PLATFORM_CONNECTION_STRING
export TENANT_ADMIN_CONNECTION_STRING="$PLATFORM_CONNECTION_STRING"
export ConnectionStrings__Platform="$PLATFORM_CONNECTION_STRING"
export TenantDatabases__PlatformConnectionString="$PLATFORM_CONNECTION_STRING"

set -a
. "$ENV_FILE"
set +a

export PLATFORM_CONNECTION_STRING
export TENANT_ADMIN_CONNECTION_STRING
export ConnectionStrings__Platform
export TenantDatabases__PlatformConnectionString
export Email__Host="$MAILPIT_SMTP_HOST"
export Email__Port="$MAILPIT_SMTP_PORT"
export Mailpit__Host="$MAILPIT_UI_HOST"
export Mailpit__UiPort="$MAILPIT_UI_PORT"

cd "$ROOT_DIR"
dotnet ef database update \
    --project src/FinancasSaaS.Platform.Infrastructure \
    --startup-project src/FinancasSaaS.Web \
    --context PlatformDbContext
dotnet test FinancasSaaS.sln --filter 'FullyQualifiedName!~Performance'
