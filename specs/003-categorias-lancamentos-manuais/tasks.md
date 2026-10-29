# Tarefas: Categorias e lançamentos manuais

**Entrada**: Documentos de design em `/specs/003-categorias-lancamentos-manuais/`

**Pré-requisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md` e `contracts/ui.md`

**Organização**: Tarefas agrupadas por história para permitir implementação e validação incremental. Testes automatizados estão incluídos porque a constituição do projeto os exige para invariantes financeiras, autorização e isolamento.

## Formato

- Cada tarefa tem checkbox, ID sequencial e caminho de arquivo.
- `[P]` indica trabalho independente de tarefas incompletas e em arquivos distintos.
- `[US1]` a `[US4]` identificam a história atendida; setup, fundação e polimento não recebem rótulo de história.

## Fase 1: Setup

**Objetivo**: Preparar o trabalho incremental na solução e os recursos de validação compartilhados.

- [X] T001 [P] Criar a estrutura inicial de domínio financeiro em `src/FinancasSaaS.Domain/Finance/` para acomodar tipos e regras das histórias sem dependência de UI ou persistência.
- [X] T002 [P] Preparar fixtures de dois bancos de tenant independentes e limpeza isolada para testes financeiros em `tests/FinancasSaaS.IntegrationTests/Infrastructure/TenantDatabaseFixture.cs`, reutilizando a configuração PostgreSQL existente em `tests/FinancasSaaS.IntegrationTests/Infrastructure/PostgresFixture.cs`.

## Fase 2: Fundação

**Objetivo**: Garantir que todas as jornadas financeiras resolvam o tenant pela identidade autenticada e usem a base correta, sem confiar em tenant ou conexão enviados pelo cliente.

- [X] T003 Definir a abstração de criação do contexto financeiro junto à infraestrutura em `src/FinancasSaaS.Finance.Infrastructure/Persistence/ITenantFinanceContextFactory.cs`; ela é mantida fora de Application para não introduzir dependência de EF Core no contrato da camada de aplicação.
- [X] T004 Implementar a resolução do contexto financeiro da sessão usando `ITenantConnectionResolver` em `src/FinancasSaaS.Finance.Infrastructure/Persistence/TenantFinanceContextFactory.cs`, recusando usuário sem tenant ativo, pronto ou conexão configurada.
- [X] T005 Registrar as dependências do contexto financeiro e proteger as rotas do cliente com a política de acesso a tenant existente em `src/FinancasSaaS.Web/Program.cs` e `src/FinancasSaaS.Web/Security/PlatformAuthorization.cs`, preservando as políticas administrativas existentes.
- [X] T006 Preparar em `database/finance/tenant-rollout.md` a estrutura do runbook e o preflight operacional para bloquear criação/provisionamento de tenants, drenar execuções em andamento, confirmar ausência de provisionamentos ativos e manter o bloqueio em falha; deixar o procedimento final com comandos, manifesto, reconciliação e critérios de liberação para T051.

**Checkpoint**: Nenhuma história inicia até que o contexto autenticado e o plano de atualização dos bancos existentes estejam definidos.

## Fase 3: História 1 — Registrar e acompanhar lançamentos (Prioridade: P1)

**Objetivo**: Permitir criar entradas e despesas em BRL como previstas, consultá-las e confirmar a realização com data efetiva.

**Teste independente**: Com tenant pronto e categorias iniciais disponíveis, criar uma entrada e uma despesa com todos os campos, consultá-las como previstas e confirmar uma delas como realizada com data efetiva. Validar também campos ausentes, datas inválidas, valor zero/negativo, rejeição sem arredondamento de valores com mais de duas casas decimais e recusa de realização baseada em versão obsoleta.

### Testes da história

- [X] T007 [P] [US1] Escrever testes de domínio para criação de entrada/despesa prevista, rejeição de valores BRL não positivos ou com mais de duas casas decimais sem arredondamento e transição previsto→realizado em `tests/FinancasSaaS.Domain.Tests/Finance/ManualTransactionTests.cs`.
- [X] T008 [P] [US1] Escrever testes de integração com dois tenants para persistir/consultar lançamentos, recusar leitura ou mutação por ID do outro tenant e rejeitar realização concorrente baseada em versão obsoleta em `tests/FinancasSaaS.IntegrationTests/Finance/ManualTransactionIntegrationTests.cs`.
- [X] T009 [P] [US1] Escrever testes de autorização para visitante, cliente ativo e cliente sem acesso às rotas e operações de lançamentos da US1: `tests/FinancasSaaS.Web.Tests/Finance/ManualTransactionsAuthorizationTests.cs` verifica acesso à página e IDs arbitrários na URL; `tests/FinancasSaaS.Web.Tests/Security/TenantAccessAuthorizationHandlerTests.cs` cobre cliente com/sem tenant; `tests/FinancasSaaS.IntegrationTests/Finance/ManualTransactionIntegrationTests.cs` cobre leitura e mutações por ID entre tenants.

### Implementação

- [X] T010 [US1] Criar os tipos de domínio de entrada/despesa e estados previsto/realizado/cancelado, com transição inicial prevista, em `src/FinancasSaaS.Domain/Finance/ManualTransactionType.cs` e `src/FinancasSaaS.Domain/Finance/ManualTransactionStatus.cs`.
- [X] T011 [US1] Criar o conceito de lançamento no domínio em `src/FinancasSaaS.Domain/Finance/ManualTransaction.cs`, exigindo descrição, tipo, valor BRL positivo com no máximo duas casas decimais sem arredondamento, categoria, competência e data prevista; aceitar data efetiva somente na transição para realizado.
- [X] T012 [US1] Criar a entidade de persistência e mapeamentos em `src/FinancasSaaS.Finance.Infrastructure/Persistence/Entities/ManualTransactionEntity.cs` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/TenantDbContext.cs`, mantendo valores em precisão decimal exata de centavos e rejeitando valores com frações menores que um centavo antes da persistência.
- [X] T013 [US1] Gerar e revisar a migration de lançamentos prevista/realizada em `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/`, preservando o schema existente de categorias e permitindo aplicação por tenant.
- [X] T014 [US1] Definir contratos e operações de persistência para criar, consultar por ID/lista e confirmar realização usando a versão esperada em `src/FinancasSaaS.Application/Abstractions/ManualTransactionStoreContracts.cs`.
- [X] T015 [US1] Implementar casos de uso de cadastro, listagem básica e confirmação da realização com data efetiva e versão esperada em `src/FinancasSaaS.Application/Finance/ManualTransactions/`, recusando atomicamente a confirmação baseada em versão obsoleta.
- [X] T016 [US1] Implementar o adaptador financeiro para persistência e isolamento do tenant, incluindo verificação atômica de versão na realização, em `src/FinancasSaaS.Finance.Infrastructure/ManualTransactions/ManualTransactionStore.cs` e registrá-lo em `src/FinancasSaaS.Finance.Infrastructure/DependencyInjection.cs`.
- [X] T017 [US1] Registrar os casos de uso e a dependência do tenant nas páginas Web em `src/FinancasSaaS.Web/Program.cs`, sem alterar o fluxo Identity ou as rotas de plataforma.
- [X] T018 [US1] Criar a página autenticada de listagem e cadastro com estado inicial previsto, confirmação de realização e data efetiva em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Transactions.razor`, seguindo feedback e responsividade da Feature 2.

**Checkpoint**: Entrada e despesa podem ser criadas, consultadas e realizadas sem depender de histórias posteriores.

## Fase 4: História 2 — Organizar categorias (Prioridade: P1)

**Objetivo**: Garantir as três categorias iniciais sem duplicatas e permitir criar, editar e proteger categorias do tenant.

**Teste independente**: Em tenant novo ou existente, obter Lanches, Mercado e Pessoal sem duplicação; criar/renomear categoria, associá-la a entrada e despesa, e verificar recusa de nome duplicado, exclusão de categoria associada e exclusão da última categoria.

**Dependência**: Usa o contexto financeiro da Fase 2; a associação a lançamentos já criados na História 1 é necessária para cobrir a regra de exclusão de categoria associada.

### Testes da história

- [X] T019 [P] [US2] Escrever testes de domínio para nomes obrigatórios, limite atual de 120 caracteres, unicidade após trim e comparação sem distinção de caixa em `tests/FinancasSaaS.Domain.Tests/Finance/CategoryTests.cs`.
- [X] T020 [P] [US2] Testar seed idempotente, nomes duplicados concorrentes, migração com nomes legados equivalentes, proteção contra exclusão, associação de categoria entre tenants e acesso de visitante em `tests/FinancasSaaS.IntegrationTests/Finance/ManualTransactionIntegrationTests.cs`, `tests/FinancasSaaS.Web.Tests/Finance/CategoryAuthorizationTests.cs` e `tests/FinancasSaaS.IntegrationTests/Finance/ManualTransactionIntegrationTests.cs`. Não há URL pública com ID de categoria; consultas e mutações por ID são limitadas ao tenant no serviço de persistência.

### Implementação

- [X] T021 [US2] Definir no domínio a regra de nome canônico da categoria, preservando o rótulo de exibição e validando nome obrigatório com máximo de 120 caracteres, em `src/FinancasSaaS.Domain/Finance/Category.cs`.
- [X] T022 [US2] Atualizar a configuração de `Category` e o índice único para ignorar caixa e espaços nas extremidades dentro do banco do tenant em `src/FinancasSaaS.Finance.Infrastructure/Persistence/TenantDbContext.cs`.
- [X] T023 [US2] Gerar migration para a unicidade normalizada em `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/`, detectando conflito legado e falhando sem mesclar ou remover categorias.
- [X] T024 [US2] Atualizar a reconciliação de Lanches, Mercado e Pessoal para comparar nomes canônicos e manter provisionamento idempotente em `src/FinancasSaaS.Finance.Infrastructure/Provisioning/PostgresTenantProvisioner.cs`.
- [X] T025 [US2] Definir contratos de persistência e casos de uso para listar, criar, editar e excluir categorias com verificação de associação/última categoria em `src/FinancasSaaS.Application/Finance/Categories/` e `src/FinancasSaaS.Application/Abstractions/CategoryStoreContracts.cs`.
- [X] T026 [US2] Implementar persistência de categorias, validação de associação com lançamentos e unicidade concorrente em `src/FinancasSaaS.Finance.Infrastructure/Categories/CategoryStore.cs`.
- [X] T027 [US2] Criar a página de manutenção de categorias em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Categories.razor`, permitindo criar/editar e apresentando a razão da exclusão recusada.
- [X] T028 [US2] Atualizar a seleção de categoria na página de lançamentos para usar apenas categorias do tenant e permitir sua utilização em entradas e despesas em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Transactions.razor`.

**Checkpoint**: Categorias podem ser mantidas de forma independente e usadas pelos fluxos de entrada/despesa.

## Fase 5: História 3 — Corrigir e auditar lançamentos (Prioridade: P2)

**Objetivo**: Permitir correções com motivo obrigatório e preservar histórico consultável com dados anteriores/novos, instante e responsável; recusar atualizações obsoletas.

**Teste independente**: Corrigir campos de lançamento previsto e realizado, confirmar que estado e data efetiva de realizado permanecem, consultar o histórico, tentar correção sem motivo e verificar rejeição de correção baseada em versão obsoleta.

### Testes da história

- [X] T029 [P] [US3] Escrever testes de domínio para campos corrigíveis, preservação do estado/data efetiva, exigência de motivo e rejeição de valores com mais de duas casas decimais sem arredondamento em `tests/FinancasSaaS.Domain.Tests/Finance/ManualTransactionCorrectionTests.cs`.
- [X] T030 [P] [US3] Testar histórico auditável com ator/instante/motivo/diferenças, recusa de consulta entre tenants e correção baseada em versão obsoleta em `tests/FinancasSaaS.IntegrationTests/Finance/ManualTransactionIntegrationTests.cs`; `tests/FinancasSaaS.Web.Tests/Finance/ManualTransactionHistoryAuthorizationTests.cs` verifica acesso não autenticado. O histórico é consultado dentro da página, sem rota pública de ID; o serviço aplica o mesmo escopo de tenant do lançamento.

### Implementação

- [X] T031 [US3] Criar a entidade de evento de auditoria em `src/FinancasSaaS.Finance.Infrastructure/Persistence/Entities/ManualTransactionAuditEventEntity.cs`, incluindo lançamento, tipo de evento, diferenças anteriores/novas, estado anterior quando pertinente, instante UTC, responsável e motivo não vazio.
- [X] T032 [US3] Atualizar `TenantDbContext` e gerar migration para eventos de auditoria e controle de versão do lançamento em `src/FinancasSaaS.Finance.Infrastructure/Persistence/TenantDbContext.cs` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/`.
- [X] T033 [US3] Definir os resultados de correção, conflitos de versão e consulta de histórico em `src/FinancasSaaS.Application/Abstractions/ManualTransactionStoreContracts.cs`.
- [X] T034 [US3] Implementar a operação de correção em `src/FinancasSaaS.Application/Finance/ManualTransactions/ManualTransactionService.cs`, exigindo motivo, derivando o ator da identidade autenticada, usando versão esperada e preservando estado/data efetiva.
- [X] T035 [US3] Implementar persistência atômica da correção, evento de auditoria e verificação da versão esperada, recusando gravação obsoleta sem sobrescrita em `src/FinancasSaaS.Finance.Infrastructure/ManualTransactions/ManualTransactionStore.cs`.
- [X] T036 [US3] Integrar formulário de correção, tratamento de conflito sem sobrescrita e consulta do histórico em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Transactions.razor`.

**Checkpoint**: Correções explicáveis e verificáveis permanecem seguras sob concorrência.

## Fase 6: História 4 — Filtrar, consultar histórico e cancelar (Prioridade: P2)

**Objetivo**: Filtrar lançamentos por período, tipo, categoria e estado, cancelar logicamente previstos/realizados com motivo e preservar consulta do histórico.

**Teste independente**: Consultar lançamentos com diferentes datas/tipos/categorias/estados, combinar filtros com ambas as datas, verificar inclusão dos limites e rejeição de datas ausentes ou intervalo invertido, cancelar lançamentos previstos e realizados e verificar histórico, ausência de totais nesta feature e rejeição de cancelamento baseado em versão obsoleta.

### Testes da história

- [X] T037 [P] [US4] Escrever testes de consulta para filtros combinados e data aplicável por estado, incluindo estado anterior de cancelados, inclusão das datas inicial/final, exigência de ambas as datas no contrato e rejeição de intervalo invertido, em `tests/FinancasSaaS.Application.Tests/Finance/ManualTransactionFilterTests.cs`.
- [X] T038 [P] [US4] Escrever testes de integração com dois tenants para cancelamento auditável, consulta de cancelados e recusa de cancelamento baseado em versão obsoleta sem perda da versão recente em `tests/FinancasSaaS.IntegrationTests/Finance/ManualTransactionIntegrationTests.cs`; agregados e totais ficam fora do escopo da feature.

### Implementação

- [X] T039 [US4] Definir critérios de busca por período, tipo, categoria e estado em `src/FinancasSaaS.Application/Finance/ManualTransactions/ManualTransactionFilter.cs`, exigindo ambas as datas, incluindo os limites, rejeitando intervalo invertido e usando prevista para previstos, efetiva para realizados e estado anterior para cancelados.
- [X] T040 [US4] Implementar leitura filtrada e critério da data por estado em `src/FinancasSaaS.Finance.Infrastructure/ManualTransactions/ManualTransactionStore.cs`.
- [X] T041 [US4] Implementar a operação de cancelamento lógico em `src/FinancasSaaS.Application/Finance/ManualTransactions/ManualTransactionService.cs`, com motivo obrigatório, versão esperada, estado anterior preservado e registro auditável; agregações futuras devem excluir cancelados, sem implementar totais nesta feature.
- [X] T042 [US4] Implementar cancelamento atômico com verificação da versão esperada, recusando conflito sem sobrescrever dados e mantendo o registro e histórico consultáveis em `src/FinancasSaaS.Finance.Infrastructure/ManualTransactions/ManualTransactionStore.cs`.
- [X] T043 [US4] Integrar filtros, cancelamento com motivo e consulta de eventos de auditoria na lista em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Transactions.razor`.

**Checkpoint**: Consultas combinadas e cancelamentos preservam o comportamento definido sem incluir relatórios agregados.

## Fase 7: Polimento e validação transversal

**Objetivo**: Completar as superfícies visuais, a operação de rollout e a evidência de não regressão.

- [X] T044 [P] Integrar o shell e a navegação do cliente autenticado em `src/FinancasSaaS.Web/Components/Layout/CustomerLayout.razor`; manter o `NavMenu.razor` administrativo separado e sem destinos financeiros.
- [X] T045 [P] Revisar critérios de teclado, foco visível, mensagens acessíveis e larguras de 360 px, 768 px e 1280 px nas páginas financeiras; corrigir CSS isolado para atingir conteúdo filho, garantir quebras em larguras estreitas, expor estado expandido do histórico e corrigir box sizing/margem global que causavam overflow.
- [X] T046 Estender `ITenantProvisioningCatalog` e `PlatformTenantProvisioningCatalog` para enumerar tenants prontos e suas conexões configuradas, sem retornar segredos fora do fluxo de migração, em `src/FinancasSaaS.Application/Abstractions/PlatformPorts.cs` e `src/FinancasSaaS.Platform.Infrastructure/Tenancy/PlatformTenantProvisioningCatalog.cs`.
- [X] T047 Implementar `TenantFinanceMigrationRunner` em `src/FinancasSaaS.Finance.Infrastructure/Provisioning/TenantFinanceMigrationRunner.cs` para receber um manifesto de inventário sem segredos, comparar exatamente seus IDs com os tenants prontos antes de migrar, migrar somente o conjunto validado de forma idempotente, repetir a comparação após as migrations e retornar sucesso somente se os conjuntos continuarem idênticos e todos estiverem migrados; emitir resultado por ID/estado sem connection strings. Qualquer divergência, falta de conexão ou falha deve interromper o processo com erro sem alterar o estado de provisionamento.
- [X] T048 Adicionar em `src/FinancasSaaS.Web/Program.cs` modos operacionais explícitos `--create-finance-tenant-inventory <caminho>` e `--migrate-finance-tenants --tenant-inventory <caminho>`, que escrevam/leiam manifesto JSON contendo apenas instante UTC de captura e IDs de tenant, executem fora do startup normal do servidor e retornem código diferente de zero em caso de erro ou divergência.
- [X] T049 [P] Escrever testes de integração de migração por tenant em `tests/FinancasSaaS.IntegrationTests/Persistence/TenantFinanceMigrationRunnerTests.cs`, cobrindo correspondência exata do inventário antes e depois, tenant ausente/inesperado, mudança de estado durante rollout, idempotência, interrupção em falha parcial, retentativa segura, conflitos legados e ausência de connection strings/segredos nos manifestos e resultados.
- [X] T050 [P] Escrever testes dos modos operacionais em `tests/FinancasSaaS.Web.Tests/Platform/FinanceMigrationCommandTests.cs`, comprovando geração/leitura do manifesto JSON, ausência de segredos, validação do caminho, retorno de códigos de falha e encerramento do processo antes de iniciar o servidor Web.
- [X] T051 Documentar em `database/finance/tenant-rollout.md` e no `specs/003-categorias-lancamentos-manuais/quickstart.md` os comandos exatos e o procedimento final da janela de manutenção: usar os controles do ambiente para bloquear criação/provisionamento, drenar e confirmar zero execuções ativas, gerar e preservar o manifesto de IDs, executar migrations com esse manifesto, conferir correspondência exata antes/depois, manter o bloqueio e não liberar rotas em qualquer falha/divergência, ativar a nova versão após sucesso completo e retomar criação/provisionamento depois da ativação; registrar verificações sem segredos. T006 prepara somente a estrutura e o preflight inicial do runbook; T051 completa o procedimento após implementação e testes do comando.
- [X] T052 [P] Conferir compatibilidade das migrations financeiras, seed e fluxos existentes de provisionamento em `tests/FinancasSaaS.IntegrationTests/Platform/ProvisioningIntegrationTests.cs` e `tests/FinancasSaaS.IntegrationTests/Persistence/MigrationsIsolationTests.cs`.
- [X] T053 [P] Escrever testes de logs operacionais estruturados das mutações financeiras em `tests/FinancasSaaS.Application.Tests/Finance/ManualTransactionOperationalLoggingTests.cs`, cobrindo sucesso/falha de criação e realização, conflito obsoleto e exceções de persistência em correção/cancelamento, validando evento, resultado, instante, ator, tenant confiável e correlation ID sem descrição, valor, token ou connection string.
- [X] T054 [P] Escrever testes de logs estruturados de segurança e categorias em `tests/FinancasSaaS.Web.Tests/Finance/FinanceSecurityLoggingTests.cs` e `tests/FinancasSaaS.Application.Tests/Finance/CategoryOperationalLoggingTests.cs`, cobrindo autorização negada/cross-tenant e sucesso/falha em criar, editar e excluir categorias sem registrar payloads ou segredos.
- [X] T055 Instrumentar casos de uso financeiros e de categorias com logs estruturados de resultado, ator confiável, tenant confiável, operação, instante UTC e correlation ID derivado do contexto de atividade/requisição, registrando sucessos e recusas/conflitos esperados sem payload financeiro; registrar exceções de persistência nos limites específicos da operação e relançá-las, sem capturas genéricas, em `src/FinancasSaaS.Application/Finance/ManualTransactions/`, `src/FinancasSaaS.Application/Finance/Categories/` e `src/FinancasSaaS.Web/Security/PlatformAuthorization.cs`. Confirmar que falhas não tratadas continuam registradas pelo pipeline de exceções existente.
- [X] T056 Executar e registrar a validação automatizada da solução em `specs/003-categorias-lancamentos-manuais/quickstart.md`, incluindo testes Domain, Application, integração PostgreSQL, Web, reconciliação do inventário e testes dos logs estruturados.

## Fase 8: Correções da revisão da implementação

**Objetivo**: Resolver os achados REV-001 a REV-007 aprovados, alinhando rollout, validação monetária, opções de categoria, feedback, manifesto e evidência de desempenho.

### Testes

- [X] T057 [REV-001] Em `tests/FinancasSaaS.IntegrationTests/Persistence/TenantFinanceMigrationRunnerTests.cs`, cobrir migration inicial que semeia categorias ausentes e repetição posterior do runner após cliente renomear ou excluir categoria padrão, garantindo que a preferência não seja desfeita.
- [X] T058 [P] [REV-002] Em `tests/FinancasSaaS.Domain.Tests/Finance/ManualTransactionTests.cs` e `ManualTransactionCorrectionTests.cs`, testar aceitação do máximo R$ 9.999.999.999.999.999,99 e rejeição de R$ 10.000.000.000.000.000,00 tanto na criação quanto na correção, sem mutação.
- [X] T059 [P] [REV-003] Em `tests/FinancasSaaS.IntegrationTests/Finance/ManualTransactionIntegrationTests.cs`, testar que categorias inativas não são listadas como disponíveis e que um ID inativo continua recusado ao criar lançamento.
- [X] T060 [P] [REV-004] Adicionar teste Web para gravação bem-sucedida seguida de falha ao recarregar a lista; apresentar sucesso da mutação e erro de atualização como resultados distintos sem sugerir repetição da gravação.
- [X] T061 [P] [REV-005] Adicionar teste Web para falha na consulta de histórico; não apresentar o estado vazio de histórico quando a leitura falhou.
- [X] T062 [P] [REV-006] Em `tests/FinancasSaaS.Web.Tests/Platform/FinanceMigrationCommandTests.cs`, testar que criação de manifesto em caminho existente falha e preserva o conteúdo original.

### Implementação e validação

- [X] T063 [REV-001] Semear Lanches, Mercado e Pessoal uma única vez na migration de disponibilização inicial e remover a reconciliação de categorias padrão do runner de rollouts posteriores; preservar a seed do fluxo de provisionamento inicial. Atualizar o runbook e testes para distinguir provisionamento inicial de migration repetida.
- [X] T064 [REV-002] Aplicar no domínio e nos formulários de cadastro e correção o mesmo máximo de R$ 9.999.999.999.999.999,99; rejeitar excedentes antes da persistência.
- [X] T065 [REV-003] Filtrar categorias inativas da lista de categorias disponíveis usada pelos formulários, mantendo a validação server-side de status no store financeiro.
- [X] T066 [REV-004] Separar o feedback da mutação financeira do feedback de refresh da lista em `Transactions.razor`, comunicando sucesso persistido mesmo quando a consulta seguinte falhar.
- [X] T067 [REV-005] Representar estados de carregamento, erro e resultado vazio da consulta de histórico separadamente em `Transactions.razor`.
- [X] T068 [REV-006] Criar manifestos com abertura exclusiva; não substituir um arquivo que já exista.
- [X] T069 [REV-007] Medir a consulta de lançamentos com dados sintéticos e registrar cardinalidade, SQL gerado, plano e buffers em PostgreSQL; documentar evidência e decisão sobre necessidade de paginação/índices em `specs/003-categorias-lancamentos-manuais/quickstart.md`, sem adicionar otimização sem medição.
- [X] T070 [REV-008] Alinhar a especificação e a documentação ao escopo: manter cancelamentos consultáveis e estabelecer exclusão em agregações futuras, sem exigir nem exibir totais nesta feature.

## Fase 9: Gates de rollout e decisões da revisão

**Objetivo**: Aplicar as decisões aprovadas para categorias inativas e nomes históricos, e permitir auditoria somente leitura do estado de migrations/categorias por tenant antes da liberação.

### Testes

- [X] T071 [REV-011] Testar migration e provisionamento inicial com categorias padrão preexistentes inativas: reativar as existentes preservando IDs e nomes; confirmar que categorias padrão ausentes/renomeadas não são recriadas por uma migration forward.
- [X] T072 [REV-012] Testar que lançamento existente passa a apresentar o nome atual após renomeação da categoria associada.
- [X] T073 [REV-013] Testar o relatório de auditoria por tenant para migration aplicada/não aplicada e estado de cada categoria padrão (ativa, inativa ou ausente), sem expor connection strings.

### Implementação e operação

- [X] T074 [REV-011] Reativar categorias padrão existentes na migration inicial e no provisionamento inicial, preservando identificador/nome; criar migration forward que reative somente categorias padrão ainda presentes, sem inserir padrões ausentes.
- [X] T075 [REV-012] Registrar na documentação de domínio/UI que lançamentos exibem o nome atual da categoria e não preservam snapshot anterior.
- [X] T076 [REV-013] Acrescentar comando operacional somente leitura para auditar, a partir de manifesto de tenants, histórico da migration financeira e estados das categorias padrão; documentar como revisar resultados e bloquear liberação quando o estado não puder ser explicado.
- [ ] T077 [REV-013] Executar auditoria no ambiente real de rollout para todos os tenants elegíveis; documentar a explicação operacional de categorias padrão ausentes/renomeadas e bloquear a liberação se houver estado inacessível, conflitante ou sem explicação.
- [X] T079 [REV-009] Separar o feedback da gravação de categorias do feedback de atualização da lista em `Categories.razor`, preservando a confirmação da mutação quando a consulta subsequente falhar.
- [X] T080 [REV-010] Distinguir conflito de nome duplicado de falha ao resolver contexto de usuário/tenant em `CategoryStore` e `CategoryService`; registrar conflito somente para a exceção tipada de nome duplicado e registrar outras falhas como erro.

## Fase 10: Validação moderada de usabilidade

**Objetivo**: Medir os critérios CS-005 e CS-006 com participantes representativos antes de considerar a experiência validada.

- [ ] T078 [CS-005, CS-006] Definir e executar um teste moderado com participantes representativos de clientes pessoa física; registrar previamente o tamanho da amostra e o roteiro. Medir, a partir da apresentação da tarefa até o lançamento aparecer identificado na lista, o tempo para registrar e localizar uma entrada ou despesa; medir também a proporção de participantes que conclui sem orientação o cadastro, a realização manual e a busca filtrada. Registrar método e resultados em `specs/003-categorias-lancamentos-manuais/quickstart.md`; considerar os critérios aprovados somente se o tempo for de até dois minutos e pelo menos 90% dos participantes concluírem as tarefas sem orientação.

## Dependências e ordem de execução

### Dependências entre fases

- **Setup (Fase 1)**: sem dependências; prepara estrutura e fixture.
- **Fundação (Fase 2)**: depende do setup e bloqueia todas as histórias.
- **US1 (Fase 3)**: depende da fundação e das categorias iniciais já fornecidas pelo provisionamento existente.
- **US2 (Fase 4)**: depende da fundação e usa lançamento da US1 para demonstrar bloqueio de exclusão de categoria associada.
- **US3 (Fase 5)**: depende do modelo e fluxo de lançamento da US1; requer categorias da US2 para selecionar/reclassificar categorias.
- **US4 (Fase 6)**: depende do modelo e listagem da US1, categorias da US2 e eventos de auditoria introduzidos na US3.
- **Polimento (Fase 7)**: depende das histórias que serão liberadas. T046–T050 implementam e testam o rollout; T051 documenta a operação. O comando de migração precisa terminar com sucesso para todos os tenants elegíveis antes de implantar/liberar as novas rotas.

### Ordem das histórias

- **US1 (P1)**: primeira fatia funcional, após fundação; testável com categorias iniciais existentes.
- **US2 (P1)**: pode iniciar após fundação, mas integração com lançamentos vinculados é completada depois da US1.
- **US3 (P2)**: após US1 e US2 por depender de lançamentos e categorias corrigíveis.
- **US4 (P2)**: após US1–US3 para reutilizar consulta, estado e auditoria.

### Oportunidades de paralelismo

- **Setup**: T001 e T002 usam arquivos/projetos diferentes.
- **US1**: T007, T008 e T009 são testes em projetos diferentes e podem ser escritos juntos; T008 e T009 reutilizam a fixture multi-tenant preparada em T002. T009 cobre apenas as superfícies entregues pela US1; os testes de categoria/histórico são executados com as respectivas histórias em T020/T030. Implementação de domínio, migration/persistência e UI só paraleliza após os contratos compartilhados estarem definidos.
- **US2**: T019 e T020 são testes em projetos diferentes; T020 inclui integração e autorização Web de categoria e só depende das superfícies correspondentes da US2 estarem disponíveis para executar os testes Web. Trabalho de regra de categoria no domínio e testes de migration pode avançar em paralelo após acertar a regra de canonicalização, mas T022–T024 compartilham schema/provisionamento e devem ser integrados sequencialmente.
- **US3**: T029 e T030 são independentes; T030 inclui integração e autorização Web do histórico e só depende da superfície de consulta do histórico estar disponível para executar os testes Web. Entidade/contrato de auditoria deve estar acordado antes do trabalho paralelo em persistência e UI. T030 usa a fixture multi-tenant de T002.
- **US4**: T037 e T038 podem avançar em paralelo; filtros e cancelamento compartilham store, então alterações T040 e T042 devem ser integradas sequencialmente. T038 usa a fixture multi-tenant de T002.
- **Polimento/rollout/logging**: T049 e T050 são testes em projetos distintos e podem avançar em paralelo após T046–T048; T051 depende dos comandos, manifesto e controles operacionais definidos. T052 pode avançar após as migrations e o provisionador; T053 e T054 podem avançar em paralelo após identificar o contrato de logs; T055 integra a instrumentação validada por esses testes. T056 executa a validação integral depois que T049–T052 e T053–T055 estiverem concluídas.
- Histórias não são totalmente independentes: US3 requer US1 e US2; US4 requer US1–US3.
- **Correções da revisão (Fase 8)**: T057–T062 são testes e precedem T063–T068; T069 depende da implementação da consulta e do PostgreSQL de teste, mas não pressupõe mudança de schema; T070 alinha os artefatos ao escopo sem alterar comportamento persistido. Atualizar documentação do rollout depois que T063 estiver validada.
- **Gates de rollout e decisões (Fase 9)**: T071–T073 são testes; T074 e T076 dependem dos testes correspondentes e são mudanças sequenciais no provisionamento/migrations/comando; T075 atualiza a documentação após o registro das decisões. T077 é gate operacional da liberação: executar auditoria em todas as bases elegíveis e explicar categorias ausentes/renomeadas com evidência, sem backfill automático.
- **Validação moderada de usabilidade (Fase 10)**: T078 depende das jornadas financeiras implementadas e da disponibilidade de participantes representativos; definir o protocolo e a amostra antes de iniciar as sessões, medir os critérios CS-005/CS-006 e registrar os resultados no quickstart.

## Estratégia de implementação

### MVP — História 1

1. Concluir Setup e Fundação.
2. Entregar cadastro/listagem de entrada e despesa prevista e confirmação de realização, usando as categorias iniciais atuais.
3. Validar a US1 independentemente e preservar as políticas Identity/tenant e testes de provisionamento existentes.

### Entrega incremental

1. Setup + Fundação: caminho autenticado de acesso ao banco do tenant.
2. US1: lançamentos previstos e realização manual — MVP.
3. US2: gestão e unicidade de categorias, seed reconciliado e proteções de exclusão.
4. US3: correções com auditoria e conflitos de versão.
5. US4: filtros combinados e cancelamento lógico auditável.
6. Polimento: migração explícita de tenants existentes, responsividade, acessibilidade e validação integral.

Cada fase deve passar pelos testes e gates aplicáveis antes da seguinte. A liberação de novas rotas a tenants existentes depende da conclusão confirmada das migrations de cada base e da resolução explícita de conflitos de categorias legadas. A janela de rollout exige bloquear a criação/provisionamento de tenants pelos controles operacionais do ambiente, drenar execuções ativas e validar o inventário completo; se esses controles ou a pausa não puderem ser confirmados, não iniciar o rollout. Reabrir criação/provisionamento somente após a nova versão estar ativa.
