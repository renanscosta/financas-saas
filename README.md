# Finanças SaaS

Solução .NET 10 para um SaaS de finanças pessoais, organizada como monólito modular.

## Estrutura

- `src/FinancasSaaS.Web`: Blazor Web App com Interactive Server.
- `src/FinancasSaaS.Domain`: regras de domínio sem dependências de infraestrutura.
- `src/FinancasSaaS.Application`: casos de uso e portas da aplicação.
- `src/FinancasSaaS.Platform.Infrastructure`: persistência e Identity da plataforma.
- `src/FinancasSaaS.Finance.Infrastructure`: persistência dos bancos dos tenants.
- `src/FinancasSaaS.AppHost`: composição local fina, sem serviço independente.
- `tests/`: projetos de testes unitários, de integração e web.

## Pré-requisitos

- Linux com VS Code.
- .NET SDK 10.
- Docker Engine e Docker Compose.

## Configuração local

O comando cria `.env` com credenciais aleatórias (permissão `0600`) na primeira execução,
inicia PostgreSQL e Mailpit, aplica as migrations da plataforma e configura conexões
administrativas para os bancos de tenant:

```bash
scripts/dev-up.sh --no-seed
```

O PostgreSQL local será usado pela plataforma e, posteriormente, pelos bancos individuais dos
tenants. O Mailpit captura e-mails localmente em `http://localhost:8025` e não envia mensagens
reais. Os serviços ficam vinculados a `127.0.0.1`. Para criar também um administrador local,
use `scripts/dev-up.sh --seed`; a senha gerada é exibida no terminal. Use
`scripts/dev-up.sh --no-seed --prepare-only` para preparar serviços/migrations sem iniciar a
aplicação ou `scripts/dev-up.sh --no-seed --test` para executar a suíte com as conexões locais.
Sem `--seed`, o administrador não é criado.

O arquivo `.env` é local, ignorado pelo Git, e não deve ser compartilhado ou usado em produção.
`.env.example` contém somente marcadores `SUBSTITUIR`. Instruções para segredos de implantação
estão em [database/deployment.md](database/deployment.md).

O banco do ambiente de desenvolvimento persiste entre reinicializações para não perder dados
de trabalho. Para começar de novo intencionalmente, execute:

```bash
docker compose --env-file .env -f docker/compose.yaml down --volumes
scripts/dev-up.sh --no-seed
```

O comando `down --volumes` apaga permanentemente os dados locais do PostgreSQL deste projeto;
não o use se precisar preservar usuários, tenants ou lançamentos locais.

## Migrations

As migrations da plataforma e dos tenants são independentes. A plataforma deve ser atualizada
antes de executar a aplicação:

```bash
dotnet ef database update \
	--project src/FinancasSaaS.Platform.Infrastructure \
	--startup-project src/FinancasSaaS.Web \
	--context PlatformDbContext
```
Após revisar dados legados, valide as referências da plataforma com o script somente leitura
em `database/platform/validate-referential-integrity.sql`. Ele lista órfãos e só deve executar
`VALIDATE CONSTRAINT` depois que todos os contadores estiverem zerados e a remediação tiver
sido aprovada; nenhum dado é apagado automaticamente.

O provisionamento cria o banco lógico do tenant, aplica as migrations de `TenantDbContext` e
cria as categorias `Lanches`, `Mercado` e `Pessoal` de forma idempotente. Não use
`EnsureCreated`.

## Executar

```bash
dotnet run --project src/FinancasSaaS.Web --urls http://localhost:5080
```

Endpoints locais úteis:

- Aplicação: `http://localhost:5080`
- Health check agregado: `http://localhost:5080/health`
- Mailpit: `http://localhost:8025`

O health check não expõe mensagens de exceção, strings de conexão, credenciais ou dados de
tenant. Ele verifica a conexão com PostgreSQL e a porta da interface do Mailpit.

## Acesso administrativo local

O login fica em `http://localhost:5080/Account/Login`. Por padrão, o seed de identidade está
desabilitado. Para criar as roles `platform.readonly` e `platform.admin` e um administrador
local, configure as variáveis somente no ambiente de desenvolvimento:

```bash
export IdentitySeed__Enabled=true
export IdentitySeed__AdministratorEmail=admin@example.com
export IdentitySeed__AdministratorPassword='use-uma-senha-local-forte'
dotnet run --project src/FinancasSaaS.Web --urls http://localhost:5080
```

O seed é idempotente e não aceita credenciais versionadas no repositório. A senha nunca é
enviada por e-mail; o cliente define a própria senha pelo convite.

## Testes

```bash
scripts/dev-up.sh --no-seed --test
```

O modo de teste inicia um PostgreSQL novo, isolado em `tmpfs` e sem volume persistente,
aplica as migrations e remove o container ao final. A porta é dinâmica, e a conexão de teste
é injetada nos projetos de integração. Os testes de performance ficam excluídos do fluxo
normal do MVP. Os testes financeiros compartilham dois bancos de tenant, executam em série e
podem criar no máximo um banco temporário adicional. Para executar testes sem iniciar essa
infraestrutura descartável, configure `PLATFORM_CONNECTION_STRING` antes de chamar `dotnet test`.

## Troubleshooting

- PostgreSQL indisponível: execute `docker compose -f docker/compose.yaml ps` e confirme que
	a porta `5432` está livre.
- Mailpit indisponível: confirme a porta `8025` e inicie o serviço com
	`docker compose -f docker/compose.yaml up -d mailpit`.
- Migrations pendentes: aplique a migration da plataforma e repita o fluxo de provisionamento;
	o banco do tenant é atualizado pelo próprio provisionador.
- Acesso bloqueado: confirme que o cliente está ativo, o tenant está pronto e o cadastro pelo
	convite foi concluído. A conexão do tenant é sempre resolvida pelo servidor.
