# Guia de Validação Local

## Pré-requisitos

- Linux com VS Code.
- .NET 10 SDK.
- Docker Engine e Docker Compose.
- Portas locais livres para a aplicação, PostgreSQL e Mailpit.

## Subir dependências

Na raiz do repositório:

```bash
docker compose -f docker/compose.yaml up -d postgres mailpit
```

O PostgreSQL fornece a instância local para o banco da plataforma e os bancos dos tenants. O
Mailpit captura as mensagens sem enviá-las para destinatários reais.

## Restaurar, compilar e testar

```bash
dotnet restore
dotnet build
dotnet test
```

A suíte deve incluir testes unitários de estados e invariantes, testes de aplicação para
idempotência e testes de integração com PostgreSQL.

## Aplicar migrations

As migrations devem ser aplicadas separadamente:

1. Aplicar as migrations do `PlatformDbContext` no banco da plataforma.
2. Executar o fluxo de provisionamento de um cliente, que cria o banco do tenant e aplica as
   migrations do `TenantDbContext` nesse banco.
3. Reexecutar o provisionamento e confirmar que não há migration ou categoria duplicada.

Na raiz do repositório, aplique as migrations da plataforma com:

```bash
dotnet ef database update \
   --project src/FinancasSaaS.Platform.Infrastructure \
   --startup-project src/FinancasSaaS.Web \
   --context PlatformDbContext
```

O banco de cada tenant recebe as migrations de `TenantDbContext` durante o provisionamento;
não usar `EnsureCreated` como substituto de migrations versionadas.

## Executar a aplicação

```bash
dotnet run --project src/FinancasSaaS.Web
```

Alternativamente, executar a aplicação pelo Compose conforme o README da raiz quando o
Dockerfile estiver disponível.

O login administrativo fica em `/Account/Login` e o health check agregado em `/health`.

## Cenários de aceitação manuais

1. Autenticar como operador e cadastrar um cliente.
2. Confirmar que o cliente aparece como não apto enquanto o provisionamento não termina.
3. Confirmar no Mailpit que o e-mail contém somente o link, sem senha.
4. Abrir o link válido dentro de 24 horas, concluir o cadastro e confirmar que o acesso foi
   habilitado.
5. Tentar reutilizar o link e confirmar recusa.
6. Simular ou provocar falha no provisionamento, retentar no mesmo tenant e confirmar que
   dados válidos e categorias não são duplicados.
7. Criar dois clientes, autenticar cada usuário e confirmar que cada um acessa somente seu
   próprio tenant, inclusive ao manipular identificadores.
8. Desativar um cliente e confirmar preservação dos dados, invalidação dos convites e recusa
   de acesso.

## Cenários automatizados mínimos

- Provisionamento bem-sucedido cria exatamente Lanches, Mercado e Pessoal.
- Falha antes da conclusão mantém o tenant não apto.
- Retentativa no mesmo tenant é idempotente.
- Convite expira em 24 horas, é de uso único e é invalidado por novo convite ou desativação.
- Acesso de usuário A ao banco do tenant B é recusado sem retorno de dados.
- Usuário de cliente desativado permanece bloqueado.

Os resultados da medição local de latência e da suíte completa ficam em
`tests/FinancasSaaS.IntegrationTests/Performance/LocalPerformanceResults.md`.
