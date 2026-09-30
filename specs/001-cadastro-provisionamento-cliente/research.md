# Pesquisa e Decisões Técnicas

## 1. Monólito modular em .NET 10

**Decisão**: Usar uma única solução .NET 10 e um único processo web ASP.NET Core, com módulos
Platform e Finance separados por projetos e dependências.

**Racional**: A feature exige fronteiras claras entre gestão de tenants e dados financeiros,
mas não exige implantação independente. Um monólito reduz complexidade operacional, mantém
transações locais simples e segue a constituição que rejeita microserviços sem necessidade
demonstrada.

**Alternativas consideradas**: Microserviços foram rejeitados por adicionarem rede,
discovery, observabilidade e consistência distribuída sem benefício necessário nesta etapa.
Um único projeto sem separação foi rejeitado porque enfraquece a fronteira entre domínio,
Platform e Finance.

## 2. Blazor Web App com Interactive Server

**Decisão**: Usar Blazor Web App com Interactive Server para as telas do operador e do primeiro
acesso, com autenticação e autorização no ASP.NET Core.

**Racional**: O modo Interactive Server atende a um SaaS administrativo com UI server-side,
permite manter o fluxo simples e evita duplicar regras no navegador. A UI chama casos de uso
da aplicação; não acessa `DbContext` diretamente.

**Alternativas consideradas**: WebAssembly e uma SPA separada foram rejeitados por criarem uma
segunda superfície de autenticação e distribuição sem requisito funcional correspondente.

## 3. Banco da plataforma e banco por tenant

**Decisão**: Manter um banco PostgreSQL da plataforma e um banco PostgreSQL independente por
tenant. Em desenvolvimento local, todos os bancos podem usar a mesma instância PostgreSQL,
mas com nomes/credenciais e históricos de migration distintos.

**Racional**: A separação física por banco reduz o risco de vazamento entre tenants e torna a
fronteira de conexão explícita. O banco da plataforma contém apenas identidade, catálogo de
tenants, convites, provisionamento e auditoria da feature; o banco do tenant contém o domínio
Finance futuro e as categorias iniciais desta feature.

**Alternativas consideradas**: Um único banco com coluna `TenantId` foi rejeitado para esta
feature porque não atende ao requisito explícito de banco por tenant. Uma instância
PostgreSQL por tenant foi rejeitada para desenvolvimento local por custo e operação
excessivos; a separação lógica por banco permanece.

## 4. Entity Framework Core e migrations

**Decisão**: Usar dois modelos EF Core independentes: `PlatformDbContext` e `TenantDbContext`,
com projetos de migrations separados e versionados.

**Racional**: O contexto da plataforma tem ciclo de vida e permissões diferentes do contexto
financeiro. O `TenantDbContext` deve ser aplicado repetidamente, uma vez por banco de tenant,
sem misturar o histórico de migrations da plataforma. A aplicação deve usar migrations
explícitas durante provisionamento e comandos de operação, nunca criação automática baseada
em `EnsureCreated`.

**Alternativas consideradas**: Um único `DbContext` foi rejeitado por misturar limites de
segurança e migrations. `EnsureCreated` foi rejeitado porque não oferece histórico versionado
nem evolução segura do schema.

## 5. Resolução server-side do tenant

**Decisão**: Após autenticação e autorização, resolver o tenant pelo usuário autenticado e
pelo catálogo confiável da plataforma; somente então obter a configuração da conexão do
tenant.

**Racional**: O identificador recebido pela UI nunca escolhe diretamente banco ou tenant. A
resolução deve verificar que o usuário está vinculado ao tenant, que o tenant está ativo e que
a conexão pertence ao catálogo da plataforma. O Finance recebe um contexto já autorizado.

**Alternativas consideradas**: Aceitar `tenantId` da rota ou do formulário como fonte de
verdade foi rejeitado por permitir enumeração e acesso cruzado.

## 6. Provisionamento idempotente sem transação distribuída

**Decisão**: Modelar o provisionamento como máquina de estados persistida na plataforma, com
uma tentativa identificável, passos idempotentes e retentativa no mesmo tenant. O orquestrador
cria/aplica o banco do tenant, executa suas migrations, cria categorias padrão e registra o
resultado; falhas deixam o tenant não apto ao uso.

**Racional**: PostgreSQL não deve ser coordenado por uma transação distribuída entre banco da
plataforma e banco do tenant. O estado persistido permite retomar dados válidos já criados,
evita duplicação por chaves naturais/únicas e mantém histórico de falhas e tentativas.

**Alternativas consideradas**: Recriar o tenant completo a cada falha foi rejeitado por
duplicar recursos e perder rastreabilidade. Mensageria/outbox foi deixada fora desta feature
porque a constituição exige justificar sua necessidade; uma orquestração síncrona e explícita
é suficiente para o primeiro fluxo.

## 7. Convites e e-mail local

**Decisão**: Persistir convite com token não reversível, validade de 24 horas, uso único,
status e associação ao cliente; enviar somente um link pelo serviço de e-mail. Usar Mailpit
no Docker Compose para captura local.

**Racional**: O token não deve permitir descobrir dados do tenant e deve ser invalidado após
uso, expiração, substituição por novo convite ou desativação. Mailpit permite testar o fluxo
sem enviar e-mails reais e pode ser substituído por um provedor por trás da mesma porta da
aplicação.

**Alternativas consideradas**: Enviar senha por e-mail foi rejeitado por segurança. Manter
vários convites ativos foi rejeitado porque dificulta revogação e aumenta a superfície de
acesso.

## 8. Testes de isolamento

**Decisão**: Criar testes de integração que provisionem dois tenants em bancos distintos,
criem usuários vinculados a cada um e comprovem acesso permitido ao próprio banco e recusado
ao banco do outro, inclusive com identificadores manipulados.

**Racional**: O isolamento é uma propriedade de segurança e não pode ser validado apenas por
testes unitários. Os testes devem executar a mesma resolução server-side usada pela aplicação,
verificar tenant desativado e confirmar que nenhum dado do tenant incorreto é retornado.

**Alternativas consideradas**: Testar apenas filtros em memória foi rejeitado porque não
prova a resolução da conexão nem o comportamento do PostgreSQL.
