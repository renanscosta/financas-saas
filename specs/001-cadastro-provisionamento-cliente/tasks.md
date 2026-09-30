---
description: "Tarefas de implementação do cadastro e provisionamento de cliente"
---

# Tarefas: Cadastro e Provisionamento de Cliente

**Entrada**: Documentos de design em `/specs/001-cadastro-provisionamento-cliente/`

**Pré-requisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/` e `quickstart.md`

**Testes**: incluídos porque a especificação exige testes automatizados para invariantes,
idempotência, segurança e isolamento entre tenants.

**Estratégia**: cada história de usuário possui testes e um incremento demonstrável. O MVP é
a História de Usuário 1 após Setup e Fundação.

## Formato: `[ID] [P?] [Story] Descrição`

- **[P]**: pode ser executada em paralelo por atuar em arquivos diferentes e sem depender de tarefas incompletas.
- **[Story]**: identifica a história de usuário relacionada.
- Todas as tarefas informam caminhos concretos do repositório.

## Convenções de Caminhos

- Código de produção em `src/`.
- Testes em `tests/`.
- Infraestrutura local em `docker/`, `database/` e arquivos de configuração da raiz.
- Migrations ficam nos projetos de infraestrutura correspondentes.

---

## Fase 1: Setup (Infraestrutura Compartilhada)

**Objetivo**: criar a solução .NET 10, os projetos do monólito modular, testes e execução local.

- [X] T001 Criar a solução .NET 10 `FinancasSaaS.sln`, o projeto `src/FinancasSaaS.AppHost/` e as estruturas `src/`, `tests/`, `database/` e `docker/` conforme `specs/001-cadastro-provisionamento-cliente/plan.md`.
- [X] T002 [P] Criar `src/FinancasSaaS.Domain/FinancasSaaS.Domain.csproj` e configurar o projeto sem dependências de UI, EF Core ou infraestrutura.
- [X] T003 [P] Criar `src/FinancasSaaS.Application/FinancasSaaS.Application.csproj` e configurar referências somente para domínio e contratos necessários.
- [X] T004 [P] Criar `src/FinancasSaaS.Platform.Infrastructure/FinancasSaaS.Platform.Infrastructure.csproj` com EF Core PostgreSQL, Identity e migrations da plataforma.
- [X] T005 [P] Criar `src/FinancasSaaS.Finance.Infrastructure/FinancasSaaS.Finance.Infrastructure.csproj` com EF Core PostgreSQL e migrations dos bancos de tenant.
- [X] T006 [P] Criar `src/FinancasSaaS.Contracts/FinancasSaaS.Contracts.csproj` para contratos internos sem lógica de persistência.
- [X] T007 [P] Criar `src/FinancasSaaS.Web/FinancasSaaS.Web.csproj` como Blazor Web App com Interactive Server e referências aos módulos permitidos.
- [X] T008 [P] Criar os projetos `tests/FinancasSaaS.Domain.Tests`, `tests/FinancasSaaS.Application.Tests`, `tests/FinancasSaaS.IntegrationTests` e `tests/FinancasSaaS.Web.Tests` com xUnit.
- [X] T009 [P] Criar `Dockerfile` multiestágio para a aplicação e `docker/compose.yaml` com PostgreSQL e Mailpit sem credenciais reais versionadas.
- [X] T010 [P] Criar `.env.example`, `.gitignore`, `Directory.Build.props` e configurações de análise/nullable em `/.env.example`, `/.gitignore` e `/Directory.Build.props`.
- [X] T011 Criar `README.md` inicial com requisitos de Linux/VS Code, comandos Docker Compose e comandos .NET em `/README.md`.

**Checkpoint**: a solução restaura e compila, e o Compose inicia PostgreSQL e Mailpit localmente.

---

## Fase 2: Fundação (Pré-requisitos Bloqueantes)

**Objetivo**: estabelecer domínio, persistência, autenticação, resolução server-side e suporte de testes que todas as histórias utilizam.

**⚠️ Nenhuma implementação de história deve começar antes desta fase.**

- [X] T012 [P] Criar estados, invariantes e value objects de cliente, tenant, usuário, convite e provisionamento em `src/FinancasSaaS.Domain/Platform/`.
- [X] T013 [P] Criar contratos de relógio, hash de token, e-mail, catálogo de tenant, provisionamento e auditoria em `src/FinancasSaaS.Application/Abstractions/`; manter migrations concretas exclusivamente nos projetos de infraestrutura.
- [X] T014 [P] Criar entidades EF Core da plataforma, incluindo Identity, em `src/FinancasSaaS.Platform.Infrastructure/Persistence/PlatformDbContext.cs` e `src/FinancasSaaS.Platform.Infrastructure/Persistence/Entities/`.
- [X] T015 [P] Criar `TenantDbContext` e a entidade Categoria em `src/FinancasSaaS.Finance.Infrastructure/Persistence/TenantDbContext.cs` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/Entities/Category.cs`, com unicidade de nome.
- [X] T016 Criar migrations versionadas separadas em `src/FinancasSaaS.Platform.Infrastructure/Persistence/Migrations/` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/`.
- [X] T017 Criar opções de conexão e catálogo protegido de tenants em `src/FinancasSaaS.Platform.Infrastructure/Configuration/` e `src/FinancasSaaS.Application/Abstractions/ITenantConnectionResolver.cs`.
- [X] T018 Criar autenticação, autorização do operador e vínculo usuário-tenant em `src/FinancasSaaS.Web/Security/` e `src/FinancasSaaS.Platform.Infrastructure/Identity/`.
- [X] T019 Criar resolução server-side do tenant após autenticação e autorização em `src/FinancasSaaS.Application/Tenancy/` e `src/FinancasSaaS.Web/Tenancy/`, recusando tenant fornecido livremente pela UI.
- [X] T020 Criar tratamento de erros, logging estruturado e auditoria sem segredos em `src/FinancasSaaS.Web/Infrastructure/` e `src/FinancasSaaS.Platform.Infrastructure/Auditing/`.
- [X] T021 [P] Criar fixture PostgreSQL, criação de bancos de tenant e aplicação controlada de migrations em `tests/FinancasSaaS.IntegrationTests/Infrastructure/PostgresFixture.cs`.
- [X] T022 [P] Criar fixture de autenticação com operador e dois usuários de tenants distintos em `tests/FinancasSaaS.IntegrationTests/Infrastructure/IdentityFixture.cs`.
- [X] T023 Criar teste de fundação para confirmar que contextos de plataforma e tenant usam migrations e históricos separados em `tests/FinancasSaaS.IntegrationTests/Persistence/MigrationsIsolationTests.cs`.

**Checkpoint**: domínio, dois contextos, autenticação, resolução server-side e fixtures de isolamento estão prontos para as histórias.

---

## Fase 3: História de Usuário 1 - Cadastrar e Provisionar Cliente (Prioridade: P1) 🎯 MVP

**Objetivo**: permitir que o operador cadastre um cliente, acompanhe o provisionamento e retente com idempotência no mesmo tenant.

**Teste independente**: cadastrar um cliente, provocar sucesso e falha de provisionamento, retentar a falha e confirmar estado, categorias, histórico e ausência de duplicação.

### Testes da História de Usuário 1

- [X] T024 [P] [US1] Criar testes unitários de transições e invariantes de provisionamento em `tests/FinancasSaaS.Domain.Tests/Platform/ProvisioningStateTests.cs`.
- [X] T025 [P] [US1] Criar teste de contrato do cadastro manual e consulta de provisionamento em `tests/FinancasSaaS.Application.Tests/Platform/CustomerProvisioningContractTests.cs`.
- [X] T026 [P] [US1] Criar teste de integração de provisionamento bem-sucedido com Lanches, Mercado e Pessoal em `tests/FinancasSaaS.IntegrationTests/Platform/ProvisioningIntegrationTests.cs`.
- [X] T027 [P] [US1] Criar teste de integração de falha parcial e retentativa no mesmo tenant em `tests/FinancasSaaS.IntegrationTests/Platform/ProvisioningFailureTests.cs`.
- [X] T028 [P] [US1] Criar teste de idempotência concorrente sem duplicar cliente, usuário, tenant ou categorias em `tests/FinancasSaaS.IntegrationTests/Platform/RegistrationConcurrencyTests.cs`.

### Implementação da História de Usuário 1

- [X] T029 [P] [US1] Implementar casos de uso de cadastro manual e consulta de estado em `src/FinancasSaaS.Application/Platform/RegisterCustomer/` e `src/FinancasSaaS.Application/Platform/GetProvisioningStatus/`.
- [X] T030 [US1] Implementar o orquestrador de provisionamento idempotente em `src/FinancasSaaS.Application/Platform/ProvisionTenant/`, preservando sem recriar o tenant já criado, o usuário associado, as migrations aplicadas e as categorias padrão persistidas corretamente, e retomando passos pendentes.
- [X] T031 [US1] Implementar adaptadores de criação de banco, migrations do tenant e categorias padrão em `src/FinancasSaaS.Finance.Infrastructure/Provisioning/`.
- [X] T032 [US1] Implementar persistência de tentativas, falhas seguras e auditoria em `src/FinancasSaaS.Platform.Infrastructure/Provisioning/` e `src/FinancasSaaS.Platform.Infrastructure/Auditing/`.
- [X] T033 [US1] Criar tela de cadastro manual do operador em `src/FinancasSaaS.Web/Components/Pages/Customers/Register.razor`.
- [X] T034 [US1] Criar tela de consulta e retentativa do provisionamento em `src/FinancasSaaS.Web/Components/Pages/Customers/ProvisioningStatus.razor`.
- [X] T035 [US1] Integrar validação, autorização de operador e mensagens seguras nas telas Platform em `src/FinancasSaaS.Web/Components/Pages/Customers/`.

**Checkpoint**: US1 é demonstrável independentemente com cadastro, sucesso, falha, consulta e retentativa idempotente.

---

## Fase 4: História de Usuário 2 - Concluir Cadastro e Primeiro Acesso (Prioridade: P1)

**Objetivo**: enviar convite sem senha, concluir cadastro com validade e uso único, e habilitar acesso somente após provisionamento pronto.

**Teste independente**: usar fixture de tenant provisionado, convite válido, expirado, utilizado, substituído e bloqueado por desativação.

### Testes da História de Usuário 2

- [X] T036 [P] [US2] Criar testes unitários de expiração, uso único, substituição e invalidação de convite em `tests/FinancasSaaS.Application.Tests/Platform/InvitationLifecycleTests.cs`.
- [X] T037 [P] [US2] Criar teste de contrato do convite, conclusão de cadastro e ativação em `tests/FinancasSaaS.Application.Tests/Platform/InvitationContractTests.cs`.
- [X] T038 [P] [US2] Criar teste de integração com Mailpit garantindo link sem senha e teste de falha de envio mantendo convite não utilizado e acesso bloqueado em `tests/FinancasSaaS.Application.Tests/Platform/SendInvitationUseCaseTests.cs`.
- [X] T039 [P] [US2] Criar testes de integração para convite expirado em 24 horas, reutilizado, substituído e bloqueado por desativação em `tests/FinancasSaaS.IntegrationTests/Platform/InvitationInvalidStateIntegrationTests.cs` e `RegistrationCompletionIntegrationTests.cs`.
- [X] T040 [US2] Criar teste de integração que confirme que cadastro pendente ou provisionamento falho não habilita acesso em `tests/FinancasSaaS.IntegrationTests/Platform/RegistrationCompletionIntegrationTests.cs`.

### Implementação da História de Usuário 2

- [X] T041 [P] [US2] Implementar emissão, hash, expiração em 24 horas e invalidação de convites em `src/FinancasSaaS.Application/Platform/Invitations/`.
- [X] T042 [US2] Implementar adaptador de e-mail com Mailpit local e abstração substituível em `src/FinancasSaaS.Platform.Infrastructure/Email/`, sem marcar convite como utilizado ou acesso como habilitado quando o envio falhar.
- [X] T043 [US2] Implementar conclusão de cadastro e ativação condicionada a tenant pronto, cliente ativo e convite válido em `src/FinancasSaaS.Application/Platform/CompleteRegistration/`.
- [X] T044 [US2] Criar telas de convite e conclusão de cadastro sem senha em `src/FinancasSaaS.Web/Components/Pages/Onboarding/CompleteRegistration.razor`.
- [X] T045 [US2] Integrar o estado de autenticação com o bloqueio de acesso a tenant não pronto ou desativado em `src/FinancasSaaS.Web/Security/`.

**Checkpoint**: US2 permite primeiro acesso seguro e recusa todos os convites inválidos ou estados não aptos.

---

## Fase 5: História de Usuário 3 - Desativar Cliente e Proteger Dados (Prioridade: P1)

**Objetivo**: desativar sem exclusão, invalidar convites e demonstrar isolamento entre dois tenants.

**Teste independente**: desativar um cliente ativo, verificar preservação, bloquear acesso e tentar acesso cruzado com identificadores manipulados.

### Testes da História de Usuário 3

- [X] T046 [P] [US3] Criar teste unitário da transição de cliente ativo para desativado e bloqueio de acesso em `tests/FinancasSaaS.Domain.Tests/Platform/CustomerDeactivationTests.cs`.
- [X] T047 [P] [US3] Criar teste de contrato de desativação e invalidação imediata de convites em `tests/FinancasSaaS.Application.Tests/Platform/DeactivationContractTests.cs`.
- [X] T048 [P] [US3] Criar teste de integração de isolamento com dois tenants e identificadores manipulados em `tests/FinancasSaaS.IntegrationTests/Tenancy/CrossTenantIsolationTests.cs`.
- [X] T049 [P] [US3] Criar teste de integração de preservação de dados e recusa de acesso após desativação em `tests/FinancasSaaS.IntegrationTests/Platform/DeactivationTests.cs`.

### Implementação da História de Usuário 3

- [X] T050 [US3] Implementar caso de uso de desativação, bloqueio de usuário e invalidação transacional dos convites da plataforma em `src/FinancasSaaS.Application/Platform/DeactivateCustomer/`.
- [X] T051 [US3] Integrar a desativação à resolução server-side do tenant e às políticas de autorização em `src/FinancasSaaS.Web/Tenancy/` e `src/FinancasSaaS.Web/Security/`.
- [X] T052 [US3] Criar tela de desativação e consulta de estado do cliente em `src/FinancasSaaS.Web/Platform/Customers/Deactivate.razor`.
- [X] T053 [US3] Garantir que nenhum fluxo de convite ou primeiro acesso reative cliente desativado em `src/FinancasSaaS.Application/Platform/Invitations/` e `src/FinancasSaaS.Application/Platform/CompleteRegistration/`.

**Checkpoint**: US3 preserva dados, bloqueia o cliente desativado, invalida convites e prova isolamento com dois tenants.

---

## Fase 6: Polish e Preocupações Transversais

**Objetivo**: consolidar documentação, execução local, segurança, observabilidade e validação completa.

- [X] T054 [P] Atualizar `README.md` com fluxo completo de migrations da plataforma e tenants, Mailpit, testes e troubleshooting.
- [X] T055 [P] Documentar variáveis e configuração segura em `.env.example` e `README.md`, sem valores secretos reais.
- [X] T056 [P] Adicionar health checks seguros para PostgreSQL e Mailpit em `src/FinancasSaaS.Web/Infrastructure/HealthChecks/`.
- [X] T057 [P] Adicionar logs estruturados e correlação para cadastro, provisionamento, convite e desativação em `src/FinancasSaaS.Web/Infrastructure/Logging/`.
- [X] T058 [P] Revisar exposição de segredos, tokens e dados entre tenants em `src/FinancasSaaS.Web/`, `src/FinancasSaaS.Platform.Infrastructure/` e `src/FinancasSaaS.Finance.Infrastructure/`.
- [X] T059 Executar `dotnet test`, validar os cenários do `specs/001-cadastro-provisionamento-cliente/quickstart.md` e medir a identificação do resultado em até 1 minuto e as ações interativas comuns em até 2 segundos no p95, registrando os resultados em `tests/FinancasSaaS.IntegrationTests/`.
- [X] T060 Executar uma revisão final de rastreabilidade entre `spec.md`, `plan.md`, `data-model.md`, `contracts/fluxos.md` e `tasks.md` em `specs/001-cadastro-provisionamento-cliente/`.

---

## Dependências e Ordem de Execução

### Dependências entre fases

- **Setup (Fase 1)**: nenhuma dependência; cria a solução e a execução local.
- **Fundação (Fase 2)**: depende da Fase 1 e bloqueia todas as histórias.
- **US1 (Fase 3)**: depende da Fundação; é o MVP e cria o tenant provisionado usado pelos testes posteriores.
- **US2 (Fase 4)**: depende da Fundação e usa fixtures/contratos de provisionamento da US1; pode desenvolver testes e contratos em paralelo com US1 após T023.
- **US3 (Fase 5)**: depende da Fundação e usa o vínculo tenant-usuário; seus testes de autorização podem ser preparados após T022 e integrados após T019.
- **Polish (Fase 6)**: depende das histórias desejadas e da conclusão dos cenários automatizados.

### Dependências entre histórias

- **US1 (P1)**: começa após a Fundação; não depende de outra história.
- **US2 (P1)**: começa após a Fundação; a ativação usa o estado de provisionamento definido na US1, podendo usar doubles/fixtures antes da integração final.
- **US3 (P1)**: começa após a Fundação; depende do vínculo tenant-usuário e da resolução server-side para a validação integrada de isolamento.

### Ordem dentro de cada história

- Testes de domínio e contratos devem ser criados antes da implementação correspondente.
- Entidades e estados devem existir antes de casos de uso e adaptadores.
- Casos de uso devem existir antes das telas Blazor.
- Testes de integração devem provar cada checkpoint antes da história ser considerada concluída.

## Oportunidades de Execução Paralela

- Após T001, T002-T010 podem ser distribuídas em arquivos/projetos diferentes.
- Após T014-T015, T016-T022 podem ser paralelizadas conforme os projetos não compartilhem arquivos.
- Dentro de cada história, os testes marcados `[P]` podem ser escritos em paralelo.
- Após a Fundação, US2 e US3 podem preparar testes e contratos em paralelo com US1; a integração final respeita as dependências listadas.
- Na Fase 6, documentação, health checks, logging e revisão de segurança podem ocorrer em paralelo antes de T059-T060.

## Exemplo de Paralelismo: US1

```text
T024 ProvisioningStateTests.cs
T025 CustomerProvisioningContractTests.cs
T026 ProvisioningSuccessTests.cs
T027 ProvisioningRetryTests.cs
T028 ProvisioningIdempotencyTests.cs
```

## Exemplo de Paralelismo: US2 e US3

```text
T036 InvitationStateTests.cs       | T046 CustomerDeactivationTests.cs
T037 InvitationContractTests.cs    | T047 DeactivationContractTests.cs
T038 InvitationEmailTests.cs       | T048 CrossTenantIsolationTests.cs
T039 InvitationLifecycleTests.cs   | T049 DeactivationTests.cs
```

## Estratégia de Implementação

### MVP primeiro: somente US1

1. Completar Setup e Fundação.
2. Implementar cadastro, provisionamento, consulta, falha e retentativa da US1.
3. Executar os testes unitários e de integração da US1.
4. Parar no checkpoint e validar o MVP independentemente.

### Entrega incremental

1. Setup + Fundação: solução compilável e infraestrutura local pronta.
2. US1: cadastro e provisionamento idempotente demonstráveis.
3. US2: convite sem senha e primeiro acesso condicionado ao provisionamento.
4. US3: desativação, invalidação de convites e isolamento comprovado.
5. Polish: documentação, observabilidade, revisão de segurança e quickstart completo.

## Notas

- Todos os itens seguem o formato `- [ ] T### [P?] [US?] descrição com caminho`.
- `[P]` indica somente tarefas paralelizáveis sem dependência incompleta.
- `[US1]`, `[US2]` e `[US3]` rastreiam tarefas às histórias da especificação.
- As tarefas não criam microserviços, mensageria ou transação distribuída.
- O arquivo `specs/001-cadastro-provisionamento-cliente/checklists/security.md` é revisão de qualidade de requisitos e não substitui os testes de implementação.

## Phase 7: Convergence

As tarefas abaixo foram acrescentadas após comparar a implementação atual com a especificação,
o plano, os contratos, a constituição e as tarefas existentes. O Setup está presente, mas os
projetos funcionais ainda contêm somente templates; estas tarefas tornam explícito o trabalho
restante que o próximo `/speckit-implement` deve concluir.

- [ ] T061 [CRITICAL] Implementar estados e invariantes de Cliente, Tenant, Usuário e Provisionamento sem dependências de UI, EF Core ou infraestrutura em `src/FinancasSaaS.Domain/Platform/`, conforme RF-001, RF-002, RF-003, RF-009, RF-010 e Constituição III (missing).
- [ ] T062 [CRITICAL] Criar as portas de aplicação para relógio, hash de token, e-mail, catálogo de tenant, provisionamento, auditoria e resolução server-side em `src/FinancasSaaS.Application/Abstractions/`, conforme RF-006A, RF-011, RF-014 e Constituição I/III (missing).
- [ ] T063 [CRITICAL] Implementar `PlatformDbContext`, entidades de plataforma e Identity em `src/FinancasSaaS.Platform.Infrastructure/Persistence/`, mantendo identidade, tenants, convites, provisionamento e auditoria fora dos bancos financeiros, conforme RF-002, RF-014 e plan: storage decision (missing).
- [ ] T064 [CRITICAL] Implementar `TenantDbContext` e Categoria em `src/FinancasSaaS.Finance.Infrastructure/Persistence/`, com unicidade para Lanches, Mercado e Pessoal, conforme RF-005, CS-005 e data-model.md: Categoria (missing).
- [ ] T065 [CRITICAL] Criar e validar migrations versionadas separadas para plataforma e tenants em `src/FinancasSaaS.Platform.Infrastructure/Persistence/Migrations/` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/`, sem `EnsureCreated`, conforme plan: migrations decision e Constituição III (missing).
- [ ] T066 [CRITICAL] Implementar catálogo protegido e resolução server-side da conexão do tenant após autenticação e autorização em `src/FinancasSaaS.Application/Tenancy/`, `src/FinancasSaaS.Platform.Infrastructure/Configuration/` e `src/FinancasSaaS.Web/Tenancy/`, recusando identificadores fornecidos livremente pela UI, conforme RF-011, CS-003 e Constituição I (missing).
- [ ] T067 [HIGH] Implementar autenticação, autorização de operador e vínculo usuário-tenant em `src/FinancasSaaS.Platform.Infrastructure/Identity/` e `src/FinancasSaaS.Web/Security/`, bloqueando usuários sem tenant válido ou com tenant desativado, conforme RF-002, RF-013 e contracts/fluxos.md (missing).
- [ ] T068 [HIGH] Implementar auditoria, tratamento de erros e logging estruturado sem tokens, senhas, strings de conexão ou dados de outro tenant em `src/FinancasSaaS.Platform.Infrastructure/Auditing/` e `src/FinancasSaaS.Web/Infrastructure/`, conforme RF-014, RF-015 e Constituição I/V (missing).
- [ ] T069 [HIGH] Criar fixtures PostgreSQL que provisionem banco da plataforma e dois bancos de tenant, apliquem migrations controladas e suportem autenticação de operador e usuários em `tests/FinancasSaaS.IntegrationTests/Infrastructure/`, conforme plan: testes de isolamento (missing).
- [ ] T070 [HIGH] Implementar cadastro manual idempotente, consulta de estado e orquestração de provisionamento em `src/FinancasSaaS.Application/Platform/RegisterCustomer/`, `GetProvisioningStatus/` e `ProvisionTenant/`, conforme RF-001, RF-003, RF-004, RF-009 e RF-010 (missing).
- [ ] T071 [HIGH] Implementar criação do banco do tenant, migrations do tenant, categorias padrão e retomada de passos pendentes em `src/FinancasSaaS.Finance.Infrastructure/Provisioning/`, preservando tenant, usuário, migrations e categorias válidos, conforme RF-005, RF-009, CS-001 e CS-002 (missing).
- [ ] T072 [HIGH] Implementar persistência de tentativas, falhas seguras, idempotência concorrente e auditoria do provisionamento em `src/FinancasSaaS.Platform.Infrastructure/Provisioning/` e `Auditing/`, conforme RF-009, RF-010, RF-014 e US1/AC3-AC4 (missing).
- [ ] T073 [HIGH] Criar testes unitários, de contrato e de integração da US1 para sucesso, falha parcial, retentativa no mesmo tenant, categorias iniciais e concorrência em `tests/FinancasSaaS.Domain.Tests/Platform/`, `tests/FinancasSaaS.Application.Tests/Platform/` e `tests/FinancasSaaS.IntegrationTests/Platform/`, conforme CS-001, CS-002, CS-005 e Constituição IV (missing).
- [ ] T074 [HIGH] Implementar emissão, hash, validade de 24 horas, uso único, substituição e invalidação de convites em `src/FinancasSaaS.Application/Platform/Invitations/`, conforme RF-006, RF-007, RF-007A, RF-008 e RF-013A (missing).
- [ ] T075 [HIGH] Implementar adaptador de e-mail Mailpit e tratamento de falha de envio em `src/FinancasSaaS.Platform.Infrastructure/Email/`, mantendo convite não utilizado e acesso bloqueado, conforme RF-006A e contracts/fluxos.md: Serviço de e-mail (missing).
- [ ] T076 [HIGH] Implementar conclusão de cadastro e ativação somente com tenant pronto, cliente ativo e convite válido em `src/FinancasSaaS.Application/Platform/CompleteRegistration/` e `src/FinancasSaaS.Web/Platform/Onboarding/`, conforme RF-005A, RF-007, RF-008 e US2/AC1-AC6 (missing).
- [ ] T077 [HIGH] Implementar desativação sem exclusão, bloqueio de usuário e invalidação imediata de convites em `src/FinancasSaaS.Application/Platform/DeactivateCustomer/`, conforme RF-012, RF-013 e RF-013A (missing).
- [ ] T078 [CRITICAL] Criar testes de integração com dois tenants para acesso próprio, tentativa com identificadores manipulados, cliente desativado, convite inválido e preservação de dados em `tests/FinancasSaaS.IntegrationTests/Tenancy/` e `Platform/`, conforme RF-011, CS-003, CS-004, CS-006 e Constituição I/IV (missing).
- [ ] T079 [MEDIUM] Criar telas Platform de cadastro, consulta/retentativa e desativação em `src/FinancasSaaS.Web/Platform/Customers/`, com autorização e mensagens seguras, conforme US1/AC1-AC4, US3/AC1-AC4 e RF-004/RF-015 (missing).
- [ ] T080 [MEDIUM] Adicionar validação observável das metas de 1 minuto para resultado do provisionamento e 2 segundos p95 para ações interativas, documentando resultados em `tests/FinancasSaaS.IntegrationTests/` e `README.md`, conforme CS-008 e plan: performance goals (missing).
- [ ] T081 [MEDIUM] Completar README, configuração segura, health checks, logging/correlação e revisão de exposição de segredos em `README.md`, `.env.example`, `src/FinancasSaaS.Web/Infrastructure/` e projetos de infraestrutura, conforme RF-015, plan: polish e Constituição V (missing).

## Phase 8: Convergence

Nova avaliação após a implementação parcial da Fundação e da US1. As tarefas abaixo cobrem
somente gaps ainda observáveis no código atual.

- [ ] T082 [CRITICAL] Implementar entidades e invariantes de convite, cadastro e ativação no domínio e na plataforma, incluindo uso único, expiração em 24 horas, substituição e invalidação por desativação, conforme RF-005A, RF-006, RF-007, RF-007A, RF-008 e RF-013A (missing).
- [ ] T083 [CRITICAL] Implementar desativação de cliente, bloqueio de usuário e invalidação de convites ativos, preservando tenant, dados e auditoria, conforme RF-012, RF-013, RF-013A e US3 (missing).
- [X] T084 [HIGH] Implementar autenticação funcional de operadores, criação/seed controlado de roles somente leitura e administrador e aplicação efetiva das policies nas operações administrativas, conforme RF-001A e Constituição I (partial).
- [ ] T085 [HIGH] Criar teste de concorrência do cadastro/provisionamento com restrições únicas e retentativas simultâneas, garantindo um único cliente, tenant, usuário e conjunto de categorias, conforme RF-009, CS-002 e T028 (missing).
- [ ] T086 [HIGH] Criar testes de isolamento com dois tenants usando a resolução server-side real, incluindo tentativa de identificador manipulado e tenant sem conexão, conforme RF-004A, RF-011, CS-003 e Constituição I/IV (missing).
- [ ] T087 [HIGH] Integrar envio de convite sem senha, Mailpit e comportamento de falha de envio mantendo convite não utilizado e acesso bloqueado, conforme RF-006, RF-006A e T075 (missing).
- [ ] T088 [HIGH] Implementar conclusão de cadastro e habilitação de acesso somente quando convite válido, tenant pronto e cliente ativo, conforme RF-005A, RF-007, RF-008 e US2 (missing).
- [ ] T089 [MEDIUM] Corrigir e validar as referências documentais e caminhos das tarefas concluídas, incluindo a anotação de que `checklists/security.md` é revisão de requisitos e não evidência de implementação, conforme plan, tasks e quickstart (partial).

## Phase 9: Convergence

Pendências identificadas ao comparar a implementação atual com a especificação, o plano, a
constituição e todas as fases anteriores de tarefas.

- [X] T090 [HIGH] Criar fluxo administrativo autenticado de emissão e reenvio de convite usando `SendInvitationUseCase`, com policy de administrador, link sem senha e mensagens seguras, conforme FR-006 e US2/AC5 (missing).
- [X] T091 [HIGH] Validar elegibilidade e vínculo entre cliente, tenant e usuário antes de persistir convite, recusando cliente desativado, tenant não pronto ou identificadores inconsistentes, conforme FR-006, RF-007A e contratos/fluxos.md (partial).
- [X] T092 [HIGH] Derivar o ator das claims do usuário autenticado nas mutações administrativas e nos registros de auditoria, removendo atores literais como `"operator"`, conforme RF-001A, RF-014 e Constituição V (partial).
- [X] T093 [HIGH] Garantir que a auditoria da desativação seja persistida na mesma transação do banco da plataforma ou por mecanismo durável equivalente antes da confirmação da mutação, conforme RF-014 e Constituição II/V (partial).
- [X] T094 [HIGH] Fazer a resolução server-side verificar explicitamente o estado do cliente além do tenant e adicionar teste negativo para cliente desativado com tenant ativo, conforme RF-011, RF-013 e Constituição I (partial).
- [X] T095 [MEDIUM] Adicionar teste de integração real com Mailpit para envio do convite e falha SMTP, confirmando convite não utilizado, acesso bloqueado e possibilidade de reenvio, conforme RF-006A e US2/AC5 (partial).
- [X] T096 [MEDIUM] Adicionar teste end-to-end de POST de login, cookie de autenticação e aplicação das policies de operador somente leitura e administrador, conforme RF-001A e T084 (missing).
- [X] T097 [LOW] Reconciliar as referências e os status das fases históricas de convergência que ainda descrevem como ausentes itens já implementados, preservando o histórico append-only e a rastreabilidade documental, conforme T089 (partial).

## Phase 10: Convergence

Pendências identificadas após a implementação da Fase 9, concentradas no ciclo de vida e na
auditabilidade dos convites.

- [X] T098 [HIGH] Derivar ou validar no servidor o destinatário do convite contra o e-mail do usuário/cliente elegível, removendo a possibilidade de envio para pessoa diferente da destinatária prevista, conforme RF-006, RF-008 e US2/AC5 (partial).
- [X] T099 [HIGH] Auditar emissão, uso e invalidação de convites com ator autenticado, tenant, momento, resultado e motivo seguro, conforme RF-014, data-model.md e Constituição V (partial).
- [X] T100 [HIGH] Validar elegibilidade antes de invalidar o convite ativo e tornar a invalidação/criação do novo convite uma operação atômica, preservando o convite anterior quando a emissão falhar antes de criar um substituto válido, conforme RF-007A e contratos/fluxos.md (partial).

## Phase 11: Convergence

Pendência residual identificada na normalização do destinatário do convite.

- [X] T101 [HIGH] Comparar destinatários de convite de forma normalizada e case-insensitive, mantendo consistência com o cadastro de e-mail normalizado e adicionando teste para variação de caixa, conforme RF-006, US2/AC5 e Constituição I (partial).

## Phase 12: Convergence

Pendência residual de consistência entre cadastro e auditoria.

- [X] T102 [HIGH] Persistir o cadastro do cliente, tenant, usuário, provisionamento inicial e evento `customer.registered` na mesma transação da plataforma ou em mecanismo durável equivalente, com teste de falha de auditoria, conforme RF-014 e Constituição II/V (partial).

## Phase 13: Approved Review Remediation

Achados REV-* aprovados para implementação posterior após revisão técnica da feature. Esta
fase não substitui nem reescreve as tarefas históricas.

- [X] REV-001 [HIGH] Tornar atomicamente consistente a transição de estado e a auditoria de início, sucesso e falha do provisionamento, impedindo estado final sem evento correspondente, conforme RF-014, Constituição II/V e `PostgresTenantProvisioner` (defeito comprovado).
- [X] REV-002 [HIGH] Aplicar autorização server-side na operação de retentativa de provisionamento, impedindo que operador somente leitura altere estado mesmo sem o botão da UI, conforme RF-001A, Constituição I e `ProvisioningStatus.razor` (defeito comprovado).
- [X] REV-003 [HIGH] Serializar ou condicionar o início de provisionamentos concorrentes do mesmo tenant, garantindo uma única execução efetiva e tentativas/auditoria consistentes, conforme RF-009, CS-002 e `PlatformTenantProvisioningStateStore` (risco a validar).
- [X] REV-004 [MEDIUM] Mapear relações e chaves estrangeiras entre cliente, tenant, usuário, provisioning, convite e auditoria, criar migration compatível e testar rejeição de referências inválidas, conforme data-model.md, Constituição II/III e `PlatformDbContext` (risco a validar).
- [X] REV-005 [LOW] Medir roundtrips e p95 do provisionamento com SQL/plano de execução e dados representativos antes de qualquer otimização, validando CS-008 e a meta de 2 segundos do plan.md sem otimização prematura (risco a validar).

## Phase 14: Approved Review Remediation

Achados REV-* aprovados para implementação posterior após revisão técnica atualizada. Esta
fase preserva o histórico das fases anteriores e não altera checklists.

- [X] REV-006 [HIGH] Tornar recuperável a falha da própria auditoria do provisionamento, evitando tenant preso em `InProgress` sem diagnóstico seguro e permitindo retentativa, conforme RF-009, RF-010, RF-014 e Constituição II/V (defeito comprovado).
- [X] REV-007 [MEDIUM] Garantir consistência atômica entre `TenantEntity` e `ProvisioningEntity` nas transições de início, sucesso e falha, com teste de rollback/interrupção entre atualizações, conforme RF-009, data-model.md e Constituição II (risco a validar).
- [X] REV-008 [MEDIUM] Sanear referências órfãs existentes e validar as FKs criadas como `NOT VALID`, garantindo constraints validadas no PostgreSQL sem apagar dados silenciosamente, conforme REV-004, data-model.md e Constituição II/III (risco a validar).
- [X] REV-009 [MEDIUM] Registrar ator explícito nos eventos automáticos de início, sucesso e falha do provisionamento, usando ator autenticado quando disponível e identidade de sistema quando a operação for automática, conforme RF-014, data-model.md e Constituição V (defeito comprovado).
- [X] REV-010 [LOW] Ampliar o benchmark do provisionamento para cardinalidade, concorrência e p95 representativos, mantendo `EXPLAIN (ANALYZE, BUFFERS)` e evitando otimização sem evidência, conforme CS-008 e plan.md: metas de desempenho (risco a validar).
