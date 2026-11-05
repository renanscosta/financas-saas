---

description: "Tarefas para implementação de Dashboard e relatórios financeiros"

---

# Tarefas: Dashboard e relatórios financeiros

**Entrada**: Documentos de design em `/specs/005-dashboard-relatorios-financeiros/`

**Pré-requisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/ui.md`, `quickstart.md`

**Testes**: Incluídos porque a especificação define critérios de sucesso verificáveis (CS-001 a CS-012) de cálculo, conciliação, isolamento entre tenants e interface.

**Organização**: Tarefas agrupadas por história de usuário para permitir implementação e validação incremental. A verificação da constituição está suspensa nesta fase por decisão do responsável; pendências derivadas dela são apenas anotadas.

## Formato

- **[P]**: pode ser executada em paralelo com outra tarefa sem dependência de arquivos ou trabalho incompleto em comum.
- **[USn]**: história de usuário correspondente à especificação.
- Cada tarefa indica os caminhos de código ou teste afetados.
- Sem projetos, dependências ou bibliotecas de gráficos novos; a feature só lê lançamentos, parcelas e categorias.

## Fase 1: Preparação (estrutura compartilhada)

**Objetivo**: Preparar os locais de código e teste nos projetos existentes.

- [X] T001 Criar os diretórios da feature nos projetos existentes: `src/FinancasSaaS.Application/Finance/Reports/`, `src/FinancasSaaS.Finance.Infrastructure/Reports/`, `tests/FinancasSaaS.Application.Tests/Finance/`, `tests/FinancasSaaS.IntegrationTests/Finance/`, `tests/FinancasSaaS.Web.Tests/Finance/` e `tests/FinancasSaaS.Web.Tests/Ui/` (os demais diretórios já existem).

## Fase 2: Fundação (pré-requisito para todas as histórias)

**Objetivo**: Regras de domínio, contratos de leitura, filtro compartilhado, persistência da preferência, serviço-base e utilitários de apresentação que todas as histórias reutilizam.

**⚠️ CRÍTICO**: Nenhuma história pode começar antes de concluir esta fase.

- [X] T002 [P] Implementar no domínio o critério de data e a data de referência em `src/FinancasSaaS.Domain/Finance/ReportDateCriterion.cs` e `src/FinancasSaaS.Domain/Finance/ReportDateRule.cs`: competência usa a data de competência para previsto e realizado; pagamento/recebimento usa a data prevista para previsto e a data efetiva para realizado; cancelado não tem data de referência e nunca é considerado.
- [X] T003 [P] Implementar em `src/FinancasSaaS.Domain/Finance/ReportPeriod.cs` o período mensal (qualquer mês e ano) e o intervalo de relatório: “data inicial e final obrigatórias, inclusivas, inicial menor ou igual à final”; “no máximo 24 meses de calendário, contando o mês da data inicial e o da data final” (01/01/2025 a 31/12/2026 aceito; 01/01/2025 a 01/01/2027 recusado); pagamento/recebimento compara a data de referência com as datas exatas e competência compara o mês de referência com os meses de calendário do intervalo.
- [X] T004 [P] Implementar em `src/FinancasSaaS.Domain/Finance/FinancialSummary.cs` os quatro totais (entradas e despesas, previstas e realizadas) em decimal exato, “saldo realizado = entradas realizadas − despesas realizadas” e “saldo previsto (ainda a realizar) = entradas previstas − despesas previstas, considerando somente lançamentos que continuam previstos”, aceitando saldos negativos e zero, sem arredondamento.
- [X] T005 [P] Testar a data de referência por critério e estado, a exclusão de cancelados e lançamento realizado com data efetiva em mês diferente da competência em `tests/FinancasSaaS.Domain.Tests/Finance/ReportDateRuleTests.cs`.
- [X] T006 [P] Testar período mensal, limite de 24 meses (24 aceito, 25 recusado), intervalo invertido, datas-limite inclusivas no pagamento/recebimento e meses de calendário na competência em `tests/FinancasSaaS.Domain.Tests/Finance/ReportPeriodTests.cs`.
- [X] T007 [P] Testar totais e saldos em centavos exatos, saldo negativo, saldo zero, mês sem previstos e que realizados não entram no saldo previsto em `tests/FinancasSaaS.Domain.Tests/Finance/FinancialSummaryTests.cs`.
- [X] T008 Definir os contratos de leitura e os modelos de consulta/resultado em `src/FinancasSaaS.Application/Abstractions/FinanceReportStoreContracts.cs` (porta de leitura: resumo mensal, relatório de despesas, categorias do filtro, composição, leitura e gravação da preferência) e `src/FinancasSaaS.Application/Finance/Reports/FinanceReportModels.cs` (consultas com critério, período, tipo, estado, categoria e mês de referência; resultados com totais, linhas mês/categoria, itens de composição com compra de origem, número e quantidade de parcelas, indicador de ausência de dados e preferência efetiva), conforme `data-model.md`.
- [X] T009 Implementar e testar o predicado compartilhado de consulta em `src/FinancasSaaS.Finance.Infrastructure/Reports/ReportQueryFilter.cs`, aplicando a regra de T002 e o período de T003 sobre a mesma tabela de lançamentos (avulsos e parcelas), excluindo cancelados e sem filtrar categorias por indicador de atividade. Sua reutilização em totais e composição permanece obrigatória em T022 e T037, sem presumir que consultas separadas observam o mesmo instante.
- [X] T062 Verificar como `src/FinancasSaaS.Application/Abstractions/ICurrentUserContext.cs` fornece a identidade autenticada e como ela é vinculada ao tenant, para usar essa identidade na preferência individual definida em `specs/005-dashboard-relatorios-financeiros/data-model.md`.
- [X] T010 Definir a preferência de critério na persistência em `src/FinancasSaaS.Finance.Infrastructure/Persistence/Entities/FinanceReportPreferenceEntity.cs` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/TenantDbContext.cs`, com identificador do usuário autenticado único no tenant, critério, instante UTC e responsável definidos no servidor; adicionar índice por competência e estado para as consultas por competência.
- [X] T011 Criar migration financeira aditiva e atualizar o snapshot para a preferência e o índice, sem alterar dados existentes, em `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/TenantDbContextModelSnapshot.cs`.
- [X] T012 Incluir a nova tabela de preferência na limpeza dos testes em `tests/FinancasSaaS.IntegrationTests/Infrastructure/TenantDatabaseFixture.cs` e criar o semeador de dados de relatório (lançamentos avulsos e parcelas em estados e datas variados, direto no contexto do tenant) em `tests/FinancasSaaS.IntegrationTests/Infrastructure/ReportDataSeeder.cs`.
- [X] T013 Testar a aplicação da migration em tenant novo e existente, repetição segura, preservação dos dados financeiros e existência do índice em `tests/FinancasSaaS.IntegrationTests/Persistence/FinanceReportMigrationTests.cs`.
- [X] T014 Criar o serviço-base em `src/FinancasSaaS.Application/Finance/Reports/FinanceReportService.cs` (ator e tenant obtidos do contexto autenticado, relógio do sistema e logger) e acrescentar mensagens de log estruturado para falha de consulta e para alteração de preferência em `src/FinancasSaaS.Application/Finance/FinanceLog.cs`, “sem valores financeiros, descrições, nomes de categorias, senhas ou tokens”.
- [X] T015 [P] Implementar o utilitário de formatação brasileira em `src/FinancasSaaS.Web/Components/Shared/BrazilianFormat.cs`: reais com símbolo R$, ponto de milhar e vírgula decimal sempre com duas casas (R$ 1.234,56 e R$ 0,00), negativos com sinal de menos (-R$ 200,00) e texto “negativo” para saldos, datas dd/MM/yyyy e meses por extenso em português (“outubro de 2026”).
- [X] T016 [P] Testar a formatação de valores, negativos, zero, valores muito grandes, datas e meses em `tests/FinancasSaaS.Web.Tests/Ui/BrazilianFormatTests.cs`.
- [X] T017 [P] Implementar a leitura segura dos parâmetros de consulta `mes`, `criterio`, `inicio`, `fim`, `categoria` e `estado` em `src/FinancasSaaS.Web/Components/Shared/ReportQueryParameters.cs`: validar período e categoria sem substituí-los silenciosamente, aplicar os padrões explícitos do contrato aos demais parâmetros, nunca aceitar tenant e não incluir valores financeiros na URL.
- [X] T018 Registrar a porta de leitura e a implementação da infraestrutura em `src/FinancasSaaS.Finance.Infrastructure/DependencyInjection.cs` e o `FinanceReportService` em `src/FinancasSaaS.Web/Program.cs`; criar `src/FinancasSaaS.Finance.Infrastructure/Reports/FinanceReportStore.cs` compilável com implementações provisórias que falham explicitamente para operações ainda não desenvolvidas, substituídas em T022, T029, T037, T049 e T060 antes de disponibilizar cada capacidade.

**Checkpoint**: Domínio, contratos, filtro compartilhado, migration, serviço-base e formatação prontos; as histórias podem começar.

## Fase 3: História de usuário 1 — Acompanhar a situação financeira do mês no dashboard (Prioridade: P1; incremento parcial)

**Objetivo**: Exibir, para um mês selecionado, os quatro totais, os dois saldos, o aviso de que previsão não é saldo bancário e as despesas por categoria, com estados de carregamento, ausência de dados e erro.

**Teste independente**: Com avulsos e parcelas em estados e meses variados, abrir o mês e conferir totais, saldos e categorias; abrir mês vazio e simular falha de consulta.

### Testes da história

- [X] T019 [P] [US1] Testar totais, saldos e despesas por categoria do mês nos dois critérios, inclusão de parcelas, exclusão de cancelados, mês vazio e mês só com cancelados, contagem única de cada lançamento e comparação do resultado do banco com a regra de domínio sobre um conjunto gerado (oráculo, CS-001) em `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs`; simular na fronteira de persistência um resultado de soma não representável e verificar falha explícita, sem exigir trilhões de registros.
- [X] T020 [P] [US1] Testar no serviço o resumo mensal (ator e tenant obrigatórios, repasse do critério, registro de falha sem dados financeiros, inclusive soma não representável, e erro propagado como falha, nunca zero) em `tests/FinancasSaaS.Application.Tests/Finance/FinanceReportServiceDashboardTests.cs`.
- [X] T021 [P] [US1] Testar a rota do dashboard (visitante redirecionado ao login, parâmetros manipulados) em `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportDashboardAuthorizationTests.cs` e a renderização dos estados, anúncios acessíveis, mês inicial pela data do servidor e descarte de respostas fora de ordem ao trocar o mês em `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportDashboardRenderingTests.cs`; cobrir carregamento, ausência de dados, erro com “tentar novamente”, saldo negativo e aviso de saldo bancário.

### Implementação da história

- [X] T022 [US1] Implementar a consulta de resumo mensal em uma única agregação por tipo, estado e categoria, somando decimais exatos e falhando explicitamente em soma não representável, em `src/FinancasSaaS.Finance.Infrastructure/Reports/FinanceReportStore.cs`.
- [X] T023 [US1] Implementar o caso de uso do resumo mensal (combina agregação e `FinancialSummary`, indica ausência de dados quando não há lançamentos considerados, registra falhas) em `src/FinancasSaaS.Application/Finance/Reports/FinanceReportService.cs`.
- [X] T024 [US1] Implementar a página `/cliente/dashboard` em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Dashboard.razor` (política de acesso ao tenant, `CustomerLayout`, seletor de mês com navegação anterior/seguinte, mês corrente do servidor como padrão, quatro totais, saldo realizado, saldo “ainda a realizar” com texto de apoio, aviso permanente de que previsão não é saldo bancário, tabela de despesas por categoria com previsto e realizado, indicador do critério em uso e estados de carregamento, ausência de dados e erro); anunciar mudanças de estado a tecnologias assistivas e descartar respostas obsoletas ao trocar o mês, usando `BrazilianFormat` e `ReportQueryParameters`.
- [X] T025 [US1] Adicionar o destino “Dashboard” à navegação do cliente e os estilos responsivos de cartões e tabelas empilhadas em largura estreita em `src/FinancasSaaS.Web/Components/Layout/CustomerLayout.razor` e `src/FinancasSaaS.Web/Components/Layout/CustomerLayout.razor.css`, mantendo o destino inicial da área do cliente.

**Checkpoint**: O cliente abre o dashboard de qualquer mês com valores conferíveis e estados claros.

## Fase 4: História de usuário 2 — Analisar despesas por mês e categoria nos relatórios (Prioridade: P1)

**Objetivo**: Exibir despesas agrupadas por mês e categoria com filtros de período, categoria e estado, validação de período e limite de 24 meses.

**Teste independente**: Criar despesas em vários meses e categorias, aplicar combinações de filtros e conferir totais, validações e estados.

### Testes da história

- [X] T026 [P] [US2] Testar o agrupamento mês × categoria, filtros combinados de período, categoria e estado, datas-limite no pagamento/recebimento, meses de calendário na competência, contagem única de cada lançamento, exclusão de cancelados, categoria renomeada com nome atual e intervalo de 24 meses em `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs`.
- [X] T027 [P] [US2] Testar no serviço a validação do período (data ausente, inicial posterior à final, 25 meses), o filtro de categoria inexistente com mensagem neutra e a ausência de consulta ao banco quando a validação falha em `tests/FinancasSaaS.Application.Tests/Finance/FinanceReportServiceDashboardTests.cs`.
- [X] T028 [P] [US2] Testar a rota de relatórios (visitante redirecionado, parâmetros manipulados) em `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportDashboardAuthorizationTests.cs` e a renderização dos filtros, mensagens associadas aos campos, anúncios acessíveis, intervalo inicial pela data do servidor, ausência de dados, erro e descarte de respostas fora de ordem ao trocar filtros em `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportDashboardRenderingTests.cs`.

### Implementação da história

- [X] T029 [US2] Implementar a consulta do relatório de despesas por mês de referência, categoria e estado, reutilizando `ReportQueryFilter`, em `src/FinancasSaaS.Finance.Infrastructure/Reports/FinanceReportStore.cs`.
- [X] T030 [US2] Implementar o caso de uso do relatório (valida período com `ReportPeriod`, valida a categoria no tenant com mensagem neutra, calcula totais por mês, por categoria e geral a partir das mesmas linhas, indica ausência de dados e registra falhas) em `src/FinancasSaaS.Application/Finance/Reports/FinanceReportService.cs`.
- [X] T060 [US2] Implementar uma leitura de categorias para os filtros dos relatórios, isolada no tenant e incluindo categorias inativas associadas a lançamentos, sem alterar `src/FinancasSaaS.Finance.Infrastructure/Categories/CategoryStore.cs`; cobrir seleção e exibição do nome atual em `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs` e `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportDashboardRenderingTests.cs`.
- [X] T031 [US2] Implementar a página `/cliente/relatorios` em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Reports.razor` (política de acesso ao tenant, `CustomerLayout`, filtros de data inicial, data final, categoria e estado “Todos/Previsto/Realizado”, intervalo inicial igual ao mês corrente do servidor, validações junto aos campos, tabela por mês, total geral e ação “limpar filtros”); anunciar carregamento, ausência de dados e erro a tecnologias assistivas e descartar respostas obsoletas ao trocar filtros.
- [X] T032 [US2] Adicionar o destino “Relatórios” à navegação do cliente em `src/FinancasSaaS.Web/Components/Layout/CustomerLayout.razor`.

**Checkpoint**: O cliente analisa despesas por mês e categoria com filtros válidos e limites aplicados.

## Fase 5: História de usuário 3 — Entender de quais lançamentos vêm os valores (Prioridade: P1)

**Objetivo**: Abrir a lista completa de lançamentos (avulsos e parcelas) que compõem cada total do dashboard e dos relatórios, conciliada em centavos com o total.

**Teste independente**: Para cada total exibido, abrir sua lista e conferir que a soma dos itens é idêntica ao total da própria lista, inclusive após realizar uma parcela e após cancelar; se diferir do total anterior, verificar o aviso de atualização.

### Testes da história

- [X] T033 [P] [US3] Testar a conciliação entre cada total (quatro totais do dashboard, categoria × estado, mês, categoria, célula mês/categoria e total geral do relatório) e sua composição, nos dois critérios e com filtros, incluindo parcelas identificadas por compra, número e quantidade, em `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs`.
- [X] T034 [P] [US3] Testar mudança de estado de uma parcela (realizada com data efetiva em outro mês, pelo `InstallmentPurchaseStore`), cancelamento de lançamento e de parcela (pelos stores existentes) e alteração concorrente entre o total e a abertura da lista; confirmar que a lista concilia com seu próprio total, que a parcela sai do previsto e entra no realizado e que a diferença em relação ao total anterior gera aviso de dados atualizados, em `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs`.
- [X] T035 [P] [US3] Testar no serviço a composição (total calculado dos itens, quantidade, comparação com o total esperado para indicar dados atualizados, lista vazia) em `tests/FinancasSaaS.Application.Tests/Finance/FinanceReportServiceDashboardTests.cs`.
- [X] T036 [P] [US3] Testar respostas assíncronas fora de ordem da composição, estado vazio, parcela “N de M”, aviso de dados atualizados, links para Lançamentos e Compras parceladas, alvo focável do título, falha mantendo o resumo do dashboard e retry com os mesmos parâmetros, em `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportDashboardRenderingTests.cs`.

### Implementação da história

- [X] T037 [US3] Implementar a consulta de composição em uma única leitura que devolve todos os itens do total solicitado, com compra de origem, número e quantidade de parcelas, usando o mesmo `ReportQueryFilter`, e calcula o total a partir dos itens, em `src/FinancasSaaS.Finance.Infrastructure/Reports/FinanceReportStore.cs`.
- [X] T038 [US3] Implementar o caso de uso de composição (recebe o total esperado, indica dados atualizados quando diferir, registra falhas) em `src/FinancasSaaS.Application/Finance/Reports/FinanceReportService.cs`.
- [X] T039 [US3] Implementar o componente de composição em `src/FinancasSaaS.Web/Components/Ui/ReportCompositionPanel.razor` (título descritivo do total, itens com descrição, tipo, categoria, valor, estado, competência, data prevista, data efetiva e parcela “N de M”, total e quantidade, aviso de dados atualizados, ausência de itens, erro com “tentar novamente”, descarte de resposta obsoleta, foco no título ao abrir e retorno ao controle de origem ao fechar, links para `/cliente/lancamentos` e `/cliente/compras-parceladas` sem ações de alteração); anunciar carregamento, ausência de dados e erro a tecnologias assistivas, sem presumir que as rotas abrem um item específico.
- [X] T040 [US3] Integrar “Ver lançamentos” aos quatro totais e às despesas por categoria do dashboard (saldos indicam os totais que os compõem, sem lista própria) em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Dashboard.razor`.
- [X] T041 [US3] Integrar “Ver lançamentos” às células mês/categoria, aos totais de mês, aos totais de categoria e ao total geral dos relatórios em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Reports.razor`.

**Checkpoint**: Todo total é conferível por uma lista completa e conciliada.

## Fase 6: História de usuário 5 — Ter meus dados financeiros isolados dos de outros clientes (Prioridade: P1)

**Objetivo**: Garantir que dashboard, relatórios, composições, categorias por identificador e logs respeitem o tenant autenticado.

**Teste independente**: Dois tenants com dados distintos nos mesmos meses; cada cliente só enxerga o próprio conjunto; identificadores e parâmetros manipulados não revelam nada.

### Testes da história

- [X] T042 [P] [US5] Testar com dois bancos de tenant que resumo mensal, relatório e composição retornam só dados do tenant autenticado, que categoria de outro tenant é recusada com resposta indistinguível de categoria inexistente e que nenhum identificador manipulado revela dados, em `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs`.
- [X] T043 [P] [US5] Testar a política das duas rotas autenticadas, identidade de tenant independente dos parâmetros adulterados, ausência de eco de valores financeiros da URL e bloqueio das consultas/leitura/gravação de preferência após desativação real, preservando o valor salvo, em `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportSecurityTests.cs` e `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs`. A recusa neutra entre categoria inexistente e de outro tenant permanece coberta por T042.
- [X] T044 [P] [US5] Testar que logs de falha e de preferência contêm apenas operação, resultado, ator, tenant, instante e correlação, sem valores, descrições, nomes de categorias, senhas ou tokens, em `tests/FinancasSaaS.Application.Tests/Finance/FinanceReportServiceDashboardTests.cs`.

### Implementação da história

- [X] T045 [US5] Aplicar validação neutra de categoria e revalidação do acesso a cada consulta ou gravação de preferência em `src/FinancasSaaS.Application/Finance/Reports/FinanceReportService.cs`; manter `PlatformPolicies.TenantAccess` nas páginas `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Dashboard.razor` e `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Reports.razor` e remover dados sensíveis de mensagens, URLs e registros em `src/FinancasSaaS.Application/Finance/FinanceLog.cs`.

**Checkpoint**: Nenhuma leitura atravessa o limite do tenant; a validação definitiva da preferência é repetida após T050.

## Fase 7: História de usuário 4 — Escolher o critério de data e definir minha preferência (Prioridade: P2)

**Objetivo**: Alternar entre competência e pagamento/recebimento, ver a regra em linguagem clara e salvar o critério padrão do cliente.

**Teste independente**: Com lançamentos cujas datas caem em meses diferentes, consultar com cada critério, salvar a preferência e retornar em outra sessão.

### Testes da história

- [X] T046 [P] [US4] Testar a preferência no banco: padrão “competência” sem registro, upsert com responsável e instante UTC, última gravação do mesmo usuário prevalece, leitura do valor mais recente, independência de dois usuários no mesmo tenant e isolamento entre dois tenants, em `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs`.
- [X] T047 [P] [US4] Testar no serviço a leitura e a gravação da preferência (usuário obtido do contexto autenticado, sem escolher outro titular, ator obrigatório, registro estruturado da alteração, falha mantém a preferência anterior e é registrada) em `tests/FinancasSaaS.Application.Tests/Finance/FinanceReportServiceDashboardTests.cs`.
- [X] T048 [P] [US4] Testar textos das regras nos dois critérios, troca sem salvar, salvamento em andamento, confirmação, falha e retry com preferência anterior preservada, continuidade da sessão entre dashboard e relatórios, preservação e limpeza de filtros e precedência de URL/sessão/preferência em `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportDashboardRenderingTests.cs`.

### Implementação da história

- [X] T049 [US4] Implementar a leitura e o upsert da preferência por usuário no banco do tenant autenticado, com identificador do usuário, responsável e instante UTC definidos no servidor, em `src/FinancasSaaS.Finance.Infrastructure/Reports/FinanceReportStore.cs`.
- [X] T050 [US4] Implementar os casos de uso de ler e salvar a preferência (critério efetivo e indicação de padrão inicial, registro estruturado da alteração e das falhas) em `src/FinancasSaaS.Application/Finance/Reports/FinanceReportService.cs`.
- [X] T051 [US4] Implementar o estado de critério da sessão em `src/FinancasSaaS.Web/Components/Shared/ReportCriterionSession.cs` (escopo da sessão, registrado em `src/FinancasSaaS.Web/Program.cs`) e o componente `src/FinancasSaaS.Web/Components/Ui/DateCriterionSelector.razor` com os critérios “Competência” e “Pagamento/recebimento”, a ação separada “Salvar como padrão”, os textos de regra de cada critério, indicador acessível de salvamento em andamento, feedback de sucesso e erro e opção de tentar salvar novamente.
- [X] T052 [US4] Integrar o seletor ao dashboard e aos relatórios: aplicar a precedência entre parâmetro `criterio`, sessão e preferência salva definida em `specs/005-dashboard-relatorios-financeiros/contracts/ui.md`, recalcular totais e composição ao trocar, preservar filtros de categoria e estado e reinterpretar o período, em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Dashboard.razor` e `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/Reports.razor`.

**Checkpoint**: O cliente consulta pelos dois critérios e mantém seu padrão entre sessões.

## Fase 8: Acabamento e preocupações transversais

**Objetivo**: Validar desempenho, rollout e regressão de ponta a ponta.

- [X] T053 [P] Executar o teste de agregação com 5.000 lançamentos em `tests/FinancasSaaS.IntegrationTests/Performance/FinanceReportPerformanceTests.cs` e registrar ambiente, método e resultado em `tests/FinancasSaaS.IntegrationTests/Performance/LocalPerformanceResults.md`. A medição do tempo até os totais aparecerem na interface interativa fica como acompanhamento pós-encerramento, sem bloquear o fechamento da implementação.
- [X] T055 [P] Atualizar o guia executável com os comandos e cenários finais, incluindo rollout, em `specs/005-dashboard-relatorios-financeiros/quickstart.md`.
- [X] T056 Aceitar o adiamento do rollout de migrations de tenants existentes para uma feature dedicada de testes/rollout após a implementação. Nenhuma migration operacional foi aplicada nesta feature. A feature futura deverá validar tenants novos e existentes com o runner financeiro e seguir `database/finance/tenant-rollout.md`, exigindo autorização, conexão administrativa segura, inventário confirmado e pausa/drenagem de provisionamentos.
- [X] T057 Executar `dotnet build FinancasSaaS.sln` e os testes de Domain, Application, Web e integração (`scripts/dev-up.sh --no-seed --test`), registrando os resultados; sem conexão de teste disponível, registrar a execução de integração como pendente.
- [X] T058 Conferir a evidência automatizada para os critérios funcionais CS-001 a CS-007 e CS-012 e registrar as decisões provisórias (competência em meses de calendário, data corrente do servidor, volume de 5.000 itens). Inspeção visual, avaliação moderada CS-008/CS-009 e medição interativa CS-011 são acompanhamentos pós-encerramento e não bloqueiam o fechamento da implementação.
- [X] T059 Executar o protocolo moderado de `specs/005-dashboard-relatorios-financeiros/quickstart.md` para CS-008 e CS-009 com pelo menos cinco participantes do perfil especificado, tarefas sem orientação, sucesso mínimo de 90% e limite de dois minutos para CS-009; registrar os resultados ou a pendência de recrutamento sem afirmar que os critérios passaram. Pendência de recrutamento registrada no quickstart; CS-008 e CS-009 não foram avaliados.
- [X] T061 Testar que consultas não criam nem alteram lançamentos, parcelas ou categorias, estados vazios e de erro não exibem dados fictícios e cliente autenticado consegue navegar nas duas rotas sem aceitar tenant da URL, em `tests/FinancasSaaS.IntegrationTests/Finance/FinanceReportDashboardIntegrationTests.cs`, `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportDashboardRenderingTests.cs` e `tests/FinancasSaaS.Web.Tests/Finance/FinanceReportSecurityTests.cs`.

## Acompanhamento pós-encerramento (não bloqueante)

- Revisar dashboard, relatórios e composição em 360 px, 768 px e 1280 px; conferir ausência de rolagem horizontal, foco visível e navegação por teclado (antiga T054).
- Medir no navegador o tempo até os totais aparecerem com 5.000 lançamentos, incluindo o indicador de carregamento (parcela de interface de T053; CS-011 permanece sem resultado de experiência).
- Recrutar participantes e executar o protocolo moderado CS-008/CS-009. Até a avaliação, não declarar esses critérios de usabilidade aprovados.

## Dependências e ordem sugerida

```text
T001 → T002/T003/T004 [P]; T005/T006/T007 [P] após suas regras
  T008 + T002 + T003 → T009; T008 → T014; T008 + T014 → T018
  T062 → T010 → T011 → T012 → T013; T015/T016/T017 [P]
Fundação concluída →
  US1: (T019/T020/T021 [P]) → T022 → T023 → T024 → T025
  US2: (T026/T027/T028 [P]) → T029 → T030 → T060 → T031 → T032
  US3: (T033/T034/T035/T036 [P]) → T037 → T038 → T039 → T040/T041   (depende de US1 e US2)
  US5: (T042/T043/T044 [P]) → T045                                   (depende de US1, US2 e US3)
  US4: (T046/T047/T048 [P]) → T049 → T050 → repetir T043 → T051 → T052 (depende de US1 e US2)
Polish: T053/T055 [P]; T061 → T057 → aceite de adiamento T056
  T055 → T059 → T058; T053 + T057 → T058
```

- A fundação bloqueia todas as histórias.
- **US1** é um incremento inicial parcial, não a feature completa; **US2** é independente de US1 após a fundação, mas compartilha o arquivo de serviço e de armazenamento, portanto as tarefas de implementação de US1 e US2 devem ser feitas em sequência nesses arquivos.
- **US3** precisa de dashboard e relatórios existentes para integrar “Ver lançamentos”; a consulta de composição (T037, T038) pode ser feita logo após a fundação.
- **US5** consolida a validação de isolamento; seus testes dependem das consultas das histórias anteriores.
- **US4** é P2: até ser concluída, as telas usam “competência” como critério e mostram o critério em uso, sem seletor.
- As garantias de autorização e isolamento valem desde a primeira capacidade disponibilizada; US5 amplia sua validação, não adia sua implementação.
- O rollout operacional não faz parte do encerramento desta implementação: T056 registra o aceite do adiamento para uma feature dedicada, sem executar migrations. A feature futura só pode aplicar mudanças após T011, T013, T057 e todos os pré-requisitos do runbook; T059 registra a pendência de participantes antes da conferência das evidências em T058.

## Oportunidades de execução paralela

- Na fundação, T002, T003 e T004 (arquivos de domínio distintos) e seus testes T005, T006 e T007; T015, T016 e T017 (Web) em paralelo com T010 a T013 (persistência).
- Em cada história, todos os testes marcados [P] em arquivos distintos podem ser escritos juntos antes da implementação.
- A marcação [P] permite escrita independente; a execução dos testes de integração deve respeitar a serialização e limpeza da fixture compartilhada.
- Após a fundação, US1 e US2 podem avançar em paralelo na camada Web (T024 e T031) e nos testes; implementações em `FinanceReportStore.cs` e `FinanceReportService.cs` exigem coordenação por serem os mesmos arquivos.

### Exemplo paralelo: História 1

```text
Tarefas: "T019 testes de integração do dashboard", "T020 testes do serviço", "T021 testes Web de autorização e renderização"
Depois: T022 → T023 → T024 → T025
```

## Estratégia de implementação

### Primeiro incremento (História 1)

1. Concluir a Fase 1 e a Fase 2 (fundação).
2. Concluir a História 1 (dashboard) e validar com os cenários de `quickstart.md` 1 a 4.
3. Validar o incremento com o critério padrão “competência” exibido na tela, sem considerá-lo uma entrega completa da feature.

### Entrega incremental

1. Fundação → US1 (incremento parcial) → US2 (relatórios) → US3 (conferência por composição) → US5 (isolamento consolidado) → US4 (critério e preferência) → Acabamento; a entrega completa exige as capacidades e verificações obrigatórias de todas as histórias.
2. Cada história acrescenta valor sem alterar as regras das Features 1 a 4.

## Notas

- A feature é somente de leitura sobre lançamentos, parcelas e categorias; nenhuma tarefa altera `ManualTransactionStore`, `InstallmentPurchaseStore` ou `CategoryStore`.
- Decisões provisórias mantidas pelo responsável: competência em meses de calendário nos relatórios, data corrente do servidor e volume de referência de 5.000 itens por lista.
- A verificação da constituição está suspensa nesta fase; pendências derivadas dela são apenas anotadas.
