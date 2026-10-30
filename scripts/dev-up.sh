#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"
COMPOSE_FILE="$ROOT_DIR/docker/compose.yaml"
MODE=run
SEED=false

usage() {
    cat <<'EOF'
Uso: scripts/dev-up.sh [--seed|--no-seed] [--prepare-only|--test]

Gera .env local protegido quando necessário, inicia PostgreSQL e Mailpit,
aplica migrations da plataforma e então executa a aplicação. A opção --test
usa um PostgreSQL descartável sem volume, injeta a conexão na suíte e exclui
testes de performance. O padrão é iniciar sem usuário administrador seed.
EOF
}

while (($#)); do
    case "$1" in
        --seed)
            SEED=true
            ;;
        --no-seed)
            SEED=false
            ;;
        --prepare-only)
            MODE=prepare
            ;;
        --test)
            MODE=test
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            printf 'Argumento desconhecido: %s\n' "$1" >&2
            usage >&2
            exit 2
            ;;
    esac
    shift
done

if [[ ! -e "$ENV_FILE" ]]; then
    command -v openssl >/dev/null || {
        echo "openssl é necessário para gerar credenciais locais." >&2
        exit 1
    }

    umask 077
    {
        printf 'POSTGRES_USER=financas\n'
        printf 'POSTGRES_PASSWORD=%s\n' "$(openssl rand -hex 32)"
        printf 'POSTGRES_DB=financas_platform\n'
        printf 'POSTGRES_HOST=localhost\n'
        printf 'POSTGRES_PORT=5432\n'
        printf 'POSTGRES_BIND_ADDRESS=127.0.0.1\n'
        printf 'MAILPIT_SMTP_HOST=localhost\n'
        printf 'MAILPIT_SMTP_PORT=1025\n'
        printf 'MAILPIT_UI_PORT=8025\n'
        printf 'MAILPIT_UI_HOST=127.0.0.1\n'
        printf 'MAILPIT_BIND_ADDRESS=127.0.0.1\n'
        printf 'IdentitySeed__Enabled=false\n'
        printf 'IdentitySeed__AdministratorEmail=admin@financas.local\n'
        printf 'IdentitySeed__AdministratorPassword=%s\n' "$(openssl rand -hex 32)"
        printf 'ASPNETCORE_ENVIRONMENT=Development\n'
    } > "$ENV_FILE"
    chmod 600 "$ENV_FILE"
    echo "Credenciais locais geradas em .env (arquivo ignorado pelo Git, permissao 600)."
fi

if [[ -L "$ENV_FILE" ]]; then
    echo ".env nao pode ser um link simbolico." >&2
    exit 1
fi
chmod 600 "$ENV_FILE"

declare -A env_values=()
while IFS='=' read -r key value || [[ -n "${key:-}" ]]; do
    [[ -z "${key:-}" || "$key" == \#* ]] && continue
    case "$key" in
        POSTGRES_USER|POSTGRES_PASSWORD|POSTGRES_DB|POSTGRES_HOST|POSTGRES_PORT|POSTGRES_BIND_ADDRESS|\
        MAILPIT_SMTP_HOST|MAILPIT_SMTP_PORT|MAILPIT_UI_PORT|MAILPIT_UI_HOST|MAILPIT_BIND_ADDRESS|\
        IdentitySeed__Enabled|IdentitySeed__AdministratorEmail|IdentitySeed__AdministratorPassword|ASPNETCORE_ENVIRONMENT)
            env_values["$key"]="$value"
            ;;
        *)
            printf 'Chave nao permitida no .env local: %s\n' "$key" >&2
            exit 1
            ;;
    esac
done < "$ENV_FILE"

required_values=(
    POSTGRES_USER POSTGRES_PASSWORD POSTGRES_DB POSTGRES_HOST POSTGRES_PORT
    POSTGRES_BIND_ADDRESS MAILPIT_SMTP_HOST MAILPIT_SMTP_PORT MAILPIT_UI_PORT
    MAILPIT_UI_HOST MAILPIT_BIND_ADDRESS IdentitySeed__AdministratorEmail
    IdentitySeed__AdministratorPassword ASPNETCORE_ENVIRONMENT
)
for key in "${required_values[@]}"; do
    if [[ -z "${env_values[$key]:-}" || "${env_values[$key]}" == *SUBSTITUIR* ]]; then
        printf 'Configure %s em .env ou execute novamente para gerar um ambiente local novo.\n' "$key" >&2
        exit 1
    fi
done

if [[ ! "${env_values[POSTGRES_PASSWORD]}" =~ ^[a-fA-F0-9]{64}$ ||
      ! "${env_values[IdentitySeed__AdministratorPassword]}" =~ ^[a-fA-F0-9]{64}$ ]]; then
    echo "As senhas locais devem conter 64 caracteres hexadecimais; gere um novo .env ou ajuste os valores." >&2
    exit 1
fi
if [[ ! "${env_values[POSTGRES_USER]}" =~ ^[a-zA-Z_][a-zA-Z0-9_]*$ ||
      ! "${env_values[POSTGRES_DB]}" =~ ^[a-zA-Z_][a-zA-Z0-9_]*$ ]]; then
    echo "POSTGRES_USER e POSTGRES_DB devem conter somente letras, numeros e underscore." >&2
    exit 1
fi

if [[ "$SEED" == true ]]; then
    env_values[IdentitySeed__Enabled]=true
else
    env_values[IdentitySeed__Enabled]=false
fi

for key in "${!env_values[@]}"; do
    export "$key=${env_values[$key]}"
done

cd "$ROOT_DIR"
if [[ "$MODE" == test ]]; then
    exec "$ROOT_DIR/scripts/dev-test.sh"
fi

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d postgres mailpit

for attempt in $(seq 1 60); do
    if docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T postgres \
        pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; then
        break
    fi
    if ((attempt == 60)); then
        echo "PostgreSQL nao ficou pronto dentro do tempo esperado." >&2
        exit 1
    fi
    sleep 1
done

# Docker only applies POSTGRES_PASSWORD when initializing an empty data volume.
# Refresh the local role password over the container's trusted Unix socket.
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T postgres \
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d postgres <<SQL
ALTER ROLE "$POSTGRES_USER" WITH PASSWORD '$POSTGRES_PASSWORD';
SQL

PLATFORM_CONNECTION_STRING="Host=$POSTGRES_HOST;Port=$POSTGRES_PORT;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
export PLATFORM_CONNECTION_STRING
export TENANT_ADMIN_CONNECTION_STRING="$PLATFORM_CONNECTION_STRING"
export ConnectionStrings__Platform="$PLATFORM_CONNECTION_STRING"
export TenantDatabases__PlatformConnectionString="$PLATFORM_CONNECTION_STRING"
export Email__Host="$MAILPIT_SMTP_HOST"
export Email__Port="$MAILPIT_SMTP_PORT"
export Mailpit__Host="$MAILPIT_UI_HOST"
export Mailpit__UiPort="$MAILPIT_UI_PORT"

dotnet ef database update \
    --project src/FinancasSaaS.Platform.Infrastructure \
    --startup-project src/FinancasSaaS.Web \
    --context PlatformDbContext

if [[ "$MODE" == test ]]; then
    dotnet test FinancasSaaS.sln
    exit $?
fi

if [[ "$MODE" == prepare ]]; then
    echo "Ambiente preparado. PostgreSQL, Mailpit e migrations da plataforma estao prontos."
    echo "PostgreSQL: localhost:$POSTGRES_PORT | Mailpit: http://$MAILPIT_UI_HOST:$MAILPIT_UI_PORT"
    exit 0
fi

if [[ "$SEED" == true ]]; then
    printf 'Administrador local seed: %s\nSenha gerada: %s\n' \
        "$IdentitySeed__AdministratorEmail" "$IdentitySeed__AdministratorPassword"
fi

dotnet run --project src/FinancasSaaS.Web --urls http://localhost:5080
