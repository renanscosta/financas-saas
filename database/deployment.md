# Configuração de implantação

## Segredos e connection strings

Gere senhas aleatórias com pelo menos 32 bytes de entropia:

```bash
openssl rand -hex 32
```

Armazene o resultado no cofre de segredos do ambiente de implantação. Não o coloque em
arquivos versionados, imagens Docker, manifests públicos, logs ou argumentos registrados pelo
CI. As expressões `SUBSTITUIR` abaixo são marcadores: troque cada uma por um valor gerado e
fornecido pelo gerenciador de segredos no ambiente de execução.

```text
ConnectionStrings__Platform=Host=SUBSTITUIR;Port=5432;Database=financas_platform;Username=SUBSTITUIR;Password=SUBSTITUIR
TenantDatabases__PlatformConnectionString=Host=SUBSTITUIR;Port=5432;Database=financas_platform;Username=SUBSTITUIR;Password=SUBSTITUIR
IdentitySeed__Enabled=false
IdentitySeed__AdministratorEmail=SUBSTITUIR
IdentitySeed__AdministratorPassword=SUBSTITUIR
```

`ConnectionStrings__Platform` acessa o banco da plataforma. `TenantDatabases__PlatformConnectionString`
é usado como base para derivar o nome do banco de cada tenant, preservando host, porta e
credenciais. Entradas específicas em `TenantDatabases__Connections__<databaseName>` podem
sobrescrever bancos individuais. Se nenhum mapeamento nem conexão base estiver configurado,
o tenant não terá conexão resolvida.

O provisionador precisa criar bancos de tenant. Configure as permissões necessárias para essa
operação conforme o provedor e a arquitetura operacional aprovados; não reutilize uma credencial
superuser em produção sem aprovação explícita. A senha aleatória de desenvolvimento criada pelo
Compose não deve ser promovida para outro ambiente.

O seed de identidade deve permanecer desabilitado em implantação normal. Se for necessário
provisionar um administrador inicial, habilite-o apenas durante uma operação controlada,
forneça e-mail e senha por segredos do ambiente e desabilite-o após a criação. Nunca use valores
de exemplo em produção.

## Ambiente local

Na raiz do repositório, execute:

```bash
scripts/dev-up.sh --no-seed
```

O script cria `.env` com modo `0600` e segredos aleatórios quando o arquivo ainda não existe,
inicia PostgreSQL e Mailpit vinculados a `127.0.0.1`, aplica migrations da plataforma e inicia a
aplicação. O arquivo `.env` está ignorado pelo Git.

Para criar também as roles administrativas e um usuário local:

```bash
scripts/dev-up.sh --seed
```

O comando exibe a senha aleatória do administrador local no terminal. Ela também fica no `.env`
local. Para apenas preparar os serviços e aplicar migrations, sem iniciar a aplicação:

```bash
scripts/dev-up.sh --no-seed --prepare-only
```

Para executar a suíte com as conexões locais configuradas:

```bash
scripts/dev-up.sh --no-seed --test
```

O modo de testes usa o usuário PostgreSQL criado pelo Compose, que possui permissões de
desenvolvimento para criar e remover bancos temporários. Não configure este modo contra bancos
compartilhados ou de produção.
