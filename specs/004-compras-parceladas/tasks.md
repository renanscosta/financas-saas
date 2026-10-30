---

description: "Tarefas para implementação de Compras parceladas"

---

# Tarefas: Compras parceladas

**Entrada**: Documentos de design em `/specs/004-compras-parceladas/`

**Pré-requisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/ui.md`, `quickstart.md`

**Testes**: Incluídos porque a constituição exige cobertura automatizada para invariantes financeiras, autorização e isolamento entre tenants.

**Organização**: Tarefas agrupadas por história de usuário para permitir implementação e validação incremental.

## Formato

- **[P]**: pode ser executada em paralelo com outra tarefa sem dependência de arquivos ou trabalho incompleto em comum.
- **[USn]**: história de usuário correspondente à especificação.
- Cada tarefa indica os caminhos de código ou teste afetados.

## Fase 1: Preparação (estrutura compartilhada)

**Objetivo**: Preparar os locais de código e teste nos projetos existentes, sem criar projetos ou dependências.

- [X] T001 Criar os diretórios da feature nos projetos existentes: `src/FinancasSaaS.Domain/Finance/`, `src/FinancasSaaS.Application/Finance/InstallmentPurchases/`, `src/FinancasSaaS.Finance.Infrastructure/InstallmentPurchases/`, `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/` e `tests/FinancasSaaS.Domain.Tests/Finance/`, `tests/FinancasSaaS.Application.Tests/Finance/`, `tests/FinancasSaaS.IntegrationTests/Finance/` e `tests/FinancasSaaS.Web.Tests/Finance/`.

## Fase 2: Fundação (pré-requisito para todas as histórias)

**Objetivo**: Persistir o parcelamento e sua relação com lançamentos no armazenamento isolado por tenant antes de habilitar qualquer fluxo da feature.

- [X] T002 Definir no domínio e na persistência o parcelamento e a associação identificável entre cada parcela e seu lançamento financeiro, mantendo a categoria e o tenant existentes, em `src/FinancasSaaS.Domain/Finance/InstallmentPurchase.cs`, `src/FinancasSaaS.Finance.Infrastructure/Persistence/Entities/InstallmentPurchaseEntity.cs`, `src/FinancasSaaS.Finance.Infrastructure/Persistence/Entities/ManualTransactionEntity.cs` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/TenantDbContext.cs`; manter “Quantidade: inteiro de 2 a 1.200; permanece estável após a criação”, “Número: sequencial de 1 até a quantidade original; não renumerar” e “Controle de versão: mutação baseada em versão desatualizada é recusada sem atualização parcial”.
- [X] T003 Criar migration financeira aditiva e atualizar o snapshot para armazenar o parcelamento e sua relação com lançamentos sem alterar dados financeiros existentes, em `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/` e `src/FinancasSaaS.Finance.Infrastructure/Persistence/Migrations/TenantDbContextModelSnapshot.cs`.
- [X] T004 Adicionar testes para aplicação da migration em tenant novo e existente, repetição segura e preservação do isolamento por banco em `tests/FinancasSaaS.IntegrationTests/Persistence/InstallmentPurchaseMigrationTests.cs`.

**Checkpoint**: Os projetos existentes têm base de persistência isolada e migrável para as histórias seguintes.

## Fase 3: História de usuário 1 — Registrar uma compra parcelada (Prioridade: P1) 🎯 MVP

**Objetivo**: Criar atomicamente as despesas previstas numeradas, com valores exatos em centavos e vencimentos/competências corretos.

**Teste independente**: Criar compra válida e comprovar a quantidade, sequência, valores, vencimentos e competências; confirmar que a soma é igual ao total e que dados inválidos ou categoria de outro tenant não deixam registros parciais.

### Testes da história

- [X] T005 [P] [US1] Testar alocação exata em centavos, incluindo R$ 100,00 em três parcelas como R$ 33,34, R$ 33,33 e R$ 33,33, valor positivo por parcela e rejeição de quantidades abaixo de 2 ou acima de 1.200 em `tests/FinancasSaaS.Domain.Tests/Finance/InstallmentPurchaseTests.cs`.
- [X] T006 [P] [US1] Testar vencimentos iniciados nos dias 29, 30 e 31, ajuste para o último dia do mês curto, retomada do dia original e competência igual ao mês/ano do vencimento em `tests/FinancasSaaS.Domain.Tests/Finance/InstallmentScheduleTests.cs`.
- [X] T007 [P] [US1] Adicionar testes da persistência atômica da compra, todas as parcelas e seu evento de auditoria inicial (ator, instante, compra e dados financeiros de cada parcela), associação à categoria do tenant, recusa de categoria de outro tenant e ausência de registros parciais após falha em `tests/FinancasSaaS.IntegrationTests/Finance/InstallmentPurchaseIntegrationTests.cs`.

### Implementação da história

- [X] T008 [P] [US1] Implementar cálculo de centavos inteiros e distribuição sequencial do resto, sem ponto flutuante, em `src/FinancasSaaS.Domain/Finance/InstallmentAllocation.cs`.
- [X] T009 [P] [US1] Implementar cálculo mensal a partir do dia original, ajustando ao último dia do mês e retomando a data original quando existir, em `src/FinancasSaaS.Domain/Finance/InstallmentSchedule.cs`.
- [X] T010 [US1] Validar e construir o agregado de compra e parcelas numeradas, com descrição obrigatória, categoria, valor positivo em BRL com no máximo duas casas decimais, limite financeiro existente, quantidade inteira de 2 a 1.200 parcelas e primeiro vencimento válido, em `src/FinancasSaaS.Domain/Finance/InstallmentPurchase.cs`.
- [X] T011 [US1] Criar persistência transacional da compra, de todas as despesas previstas e de seu evento de auditoria inicial com ator, instante e dados financeiros de todas as parcelas; validar categoria no tenant atual e competência igual ao mês/ano do vencimento, sem deixar registros parciais em `src/FinancasSaaS.Finance.Infrastructure/InstallmentPurchases/InstallmentPurchaseStore.cs`.
- [X] T012 [US1] Implementar o fluxo de aplicação de criação e consulta do parcelamento usando o usuário e tenant autenticados, com logging estruturado de sucesso e falha sem dados financeiros desnecessários, em `src/FinancasSaaS.Application/Finance/InstallmentPurchases/InstallmentPurchaseService.cs` e `src/FinancasSaaS.Application/Finance/InstallmentPurchases/InstallmentPurchaseStoreContracts.cs`.
- [X] T013 [US1] Implementar a rota autenticada de criação e listagem de compras, mostrando número, valor, vencimento, competência e estado de cada parcela com feedback e componentes existentes, em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/InstallmentPurchases.razor`.
- [X] T014 [US1] Integrar a nova rota autenticada à navegação do cliente sem expor links para telas inexistentes, em `src/FinancasSaaS.Web/Components/Layout/CustomerLayout.razor`.

**Checkpoint**: Uma compra válida é consultável com todas as parcelas; falhas de validação, tenant ou persistência não criam um conjunto parcial.

## Fase 4: História de usuário 2 — Acompanhar e realizar parcelas (Prioridade: P1)

**Objetivo**: Consultar o estado de cada parcela e confirmar manualmente uma parcela prevista com sua data efetiva.

**Teste independente**: Realizar uma parcela prevista e verificar estado, data efetiva e auditoria, sem alterar parcelas vizinhas; conferir recusa de repetição e de acesso entre tenants.

### Testes da história

- [X] T015 [P] [US2] Testar transição de parcela prevista para realizada com data efetiva, rejeição de realização duplicada e preservação das parcelas não selecionadas em `tests/FinancasSaaS.Domain.Tests/Finance/InstallmentPurchaseTests.cs`.
- [X] T016 [P] [US2] Adicionar testes da gravação atômica da realização e do evento auditável, incluindo data efetiva, ator e isolamento de leitura/mutação entre dois tenants em `tests/FinancasSaaS.IntegrationTests/Finance/InstallmentPurchaseIntegrationTests.cs`.
- [X] T017 [P] [US2] Testar autorização da rota para visitante e rejeição de identificadores manipulados em `tests/FinancasSaaS.Web.Tests/Finance/InstallmentPurchaseAuthorizationTests.cs`; o isolamento entre tenants é coberto pelos testes de integração.

### Implementação da história

- [X] T018 [US2] Implementar transição de parcela prevista para realizada exigindo data efetiva e recusando estados realizados ou cancelados em `src/FinancasSaaS.Domain/Finance/InstallmentPurchase.cs`.
- [X] T019 [US2] Persistir a realização somente para a parcela selecionada, com concorrência otimista e evento de auditoria associado ao lançamento, em `src/FinancasSaaS.Finance.Infrastructure/InstallmentPurchases/InstallmentPurchaseStore.cs` e `src/FinancasSaaS.Finance.Infrastructure/ManualTransactions/ManualTransactionStore.cs`.
- [X] T020 [US2] Expor caso de uso de realização e consulta, derivando ator/tenant do contexto autenticado e registrando conflito ou falha nos logs financeiros existentes, em `src/FinancasSaaS.Application/Finance/InstallmentPurchases/InstallmentPurchaseService.cs`.
- [X] T021 [US2] Adicionar ação de realização com formulário de data efetiva e apresentar estado, data e feedback acessível sem modificar parcelas vizinhas em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/InstallmentPurchases.razor`.
- [X] T022 [P] [US2] Testar renderização dos eventos de criação, realização, edição e cancelamento com ator, instante, parcelas afetadas, dados anteriores/novos e motivo em `tests/FinancasSaaS.Web.Tests/Finance/InstallmentPurchaseHistoryTests.cs`; testar respostas assíncronas fora de ordem em `tests/FinancasSaaS.Web.Tests/Finance/InstallmentPurchaseHistoryConcurrencyTests.cs`; o acesso cruzado ao histórico é coberto nos testes de integração.
- [X] T023 [US2] Implementar consulta de eventos de auditoria por compra no tenant atual, retornando eventos em ordem cronológica e sem permitir alteração ou exclusão, em `src/FinancasSaaS.Finance.Infrastructure/InstallmentPurchases/InstallmentPurchaseStore.cs` e `src/FinancasSaaS.Application/Finance/InstallmentPurchases/InstallmentPurchaseStoreContracts.cs`.
- [X] T024 [US2] Expor consulta do histórico no serviço de aplicação, derivando usuário e tenant do contexto autenticado e registrando falhas sem dados financeiros desnecessários, em `src/FinancasSaaS.Application/Finance/InstallmentPurchases/InstallmentPurchaseService.cs`.
- [X] T025 [US2] Exibir na tela o histórico imutável do parcelamento com ator, instante, evento, parcelas afetadas e dados financeiros relevantes, sem ações de edição/exclusão e sempre limitado ao tenant autenticado em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/InstallmentPurchases.razor`.

**Checkpoint**: O cliente pode realizar cada parcela prevista isoladamente e consultar seu estado e histórico.

## Fase 5: História de usuário 3 — Editar ou cancelar parcelas futuras (Prioridade: P2)

**Objetivo**: Alterar ou cancelar apenas parcelas futuras previstas, mantendo parcelas realizadas, canceladas, vencidas ou com vencimento no dia atual e seus históricos.

**Teste independente**: Realizar uma parcela, editar os dados/valor total, cancelar as futuras e comprovar que as parcelas elegíveis mudaram conforme as regras, valores preservados não mudaram e auditoria anterior continua consultável.

### Testes da história

- [X] T026 [P] [US3] Testar elegibilidade por estado e vencimento, preservação de parcelas realizadas/canceladas/não futuras, quantidade/numeração estáveis, reprogramação mensal a partir da primeira parcela futura elegível e recusa de agenda que cruze vencimentos protegidos em `tests/FinancasSaaS.Domain.Tests/Finance/InstallmentPurchaseEditTests.cs`.
- [X] T027 [US3] Testar recálculo exato do saldo após parcelas preservadas, distribuição de centavos às elegíveis de menor número, recusa de total inferior ao preservado e recusa integral de parcela inválida em `tests/FinancasSaaS.Domain.Tests/Finance/InstallmentPurchaseEditTests.cs`.
- [X] T028 [P] [US3] Adicionar testes da atomicidade de edição/cancelamento, motivo obrigatório, auditoria imutável, conflito concorrente e isolamento por tenant em `tests/FinancasSaaS.IntegrationTests/Finance/InstallmentPurchaseIntegrationTests.cs`.
- [X] T029 [US3] Testar feedback visual quando não há parcelas elegíveis, validações de domínio e rejeição de edição/cancelamento entre tenants em `tests/FinancasSaaS.Web.Tests/Finance/TransactionFeedbackRenderingTests.cs`, `tests/FinancasSaaS.Domain.Tests/Finance/InstallmentPurchaseEditTests.cs` e `tests/FinancasSaaS.IntegrationTests/Finance/InstallmentPurchaseIntegrationTests.cs`.

### Implementação da história

- [X] T030 [US3] Implementar edição de descrição, categoria, valor total e agenda futura: receber nova data posterior à operação para a parcela elegível de menor número, recalcular as demais elegíveis em incrementos mensais pela diferença entre os números e usando o dia informado como referência, preservar parcelas não elegíveis e recusar agenda que cruze seus vencimentos; manter quantidade/numeração e recalcular apenas o saldo elegível com centavos exatos em `src/FinancasSaaS.Domain/Finance/InstallmentPurchase.cs`.
- [X] T031 [US3] Implementar cancelamento apenas das parcelas previstas com vencimento posterior à data da operação; exigir motivo e preservar estados e histórico de parcelas não elegíveis em `src/FinancasSaaS.Domain/Finance/InstallmentPurchase.cs`.
- [X] T032 [US3] Persistir edição e cancelamento em transação atômica, validar versões e categoria do tenant, e gravar ator, instante, motivo e dados anteriores/novos das parcelas afetadas em `src/FinancasSaaS.Finance.Infrastructure/InstallmentPurchases/InstallmentPurchaseStore.cs`.
- [X] T033 [US3] Implementar casos de uso de edição e cancelamento com logging estruturado de sucesso, conflito e falha, sem registrar descrição ou valores financeiros em `src/FinancasSaaS.Application/Finance/InstallmentPurchases/InstallmentPurchaseService.cs`.
- [X] T034 [US3] Adicionar formulários de edição e cancelamento com motivo obrigatório, confirmação, conflito de versão e feedback quando não houver parcelas elegíveis em `src/FinancasSaaS.Web/Components/Pages/Customer/Finance/InstallmentPurchases.razor`.

**Checkpoint**: Edição e cancelamento preservam todas as parcelas não elegíveis, aplicam mudanças integralmente às futuras previstas e mantêm histórico consultável.

## Fase 6: Acabamento e preocupações transversais

**Objetivo**: Verificar segurança, compatibilidade, rollout e regressão de ponta a ponta.

- [X] T035 [P] Atualizar o guia executável e cenários de rollout, cobrindo migrations para tenants existentes e novos, cenários de centavos, meses curtos, isolamento, auditoria consultável, edição/cancelamento e acessibilidade em `specs/004-compras-parceladas/quickstart.md`.
- [X] T036 Aplicar e verificar as migrations em todos os tenants elegíveis com o runner financeiro existente. Exige conexão administrativa configurada com segurança, inventário confirmado e pausa/drenagem de provisionamentos conforme `database/finance/tenant-rollout.md`; interromper se qualquer pré-requisito não puder ser confirmado. O ambiente local anterior foi descartado por solicitação explícita; na nova base o inventário e as auditorias pré/pós-rollout encontraram 0 tenants prontos, e o runner concluiu sem falhas. Testes de integração confirmam que novos tenants recebem as migrations financeiras durante o provisionamento.
- [X] T037 Executar `dotnet build FinancasSaaS.sln` (0 avisos/erros), testes Domain (43), Application (23) e Web focados (10).
- [X] T039 Definir protocolo reproduzível e amostra mínima para avaliar CS-005.

## Dependências e ordem sugerida

```text
T001 → T002 → T003 → T004
                    └→ US1: (T005/T006/T007 em paralelo) → T008/T009 → T010 → T011 → T012 → T013/T014
                                                                                         └→ US2: (T015/T016/T017 em paralelo) → T018 → T019 → T020 → T021 → T022 → T023 → T024 → T025
                                                                                                                                                                          └→ US3: (T026/T028 em paralelo) → T027 → T029; T030/T031 → T032 → T033 → T034
                                                                                                                                                                                                                      └→ (T035/T037 em paralelo)
                                                                                                                                                                                                                            └→ T036
```

- Fase de fundação precisa terminar antes das histórias, para que o relacionamento entre compra e lançamento e a migration estejam disponíveis.
- **US1** não depende de histórias posteriores e é o MVP sugerido.
- **US2** depende de US1 para haver parcelas persistidas e consultáveis. A consulta/renderização do histórico (T022–T025) vem depois da realização e integra os eventos da criação e da realização; os eventos posteriores de US3 também ficam disponíveis na mesma consulta.
- **US3** depende de US1 e US2 para editar/cancelar parcelas e proteger realizações já confirmadas.
- **[P]** identifica apenas tarefas em arquivos distintos sem dependência de implementação pendente. Dentro de cada história, tarefas de teste podem ser feitas em paralelo; implementação de domínio, persistência e UI segue os checkpoints e dependências listados.

## Oportunidades de execução paralela

- T004 depende de T003 e não pode começar antes da migration. Depois de T004, T005, T006 e T007 podem ser escritos em paralelo; as verificações de domínio não dependem da infraestrutura PostgreSQL.
- Na US1, T008 e T009 podem ser implementadas em paralelo em arquivos de domínio distintos; integração, serviço e UI aguardam os modelos e cálculo.
- Na US2, os testes de domínio, integração e Web (T015–T017) podem ser preparados em paralelo. A consulta e a apresentação do histórico seguem T021 em sequência (T022–T025).
- Na US3, T026 e T028 podem ser preparados em paralelo; T027 vem depois de T026 e T029 depois de T027/T028 porque compartilham arquivos de teste. Edição/cancelamento em domínio são independentes entre si antes da persistência.
- T035 e T037 podem ocorrer em paralelo após as histórias. O rollout T036 depende do preflight operacional e do inventário descritos no runbook financeiro.

## Estratégia de implementação

1. **Fundação**: adicionar relação parcelamento–lançamentos e migration financeira compatível com os tenants.
2. **MVP (US1)**: entregar criação e consulta com centavos exatos, vencimentos corretos, competência e atomicidade.
3. **Acompanhamento (US2)**: confirmar realização individual com data efetiva e auditoria.
4. **Manutenção (US3)**: editar ou cancelar somente parcelas futuras previstas, com motivos, concorrência e histórico preservado.
5. **Liberação**: validar dois tenants, aplicar rollout de migrations, executar build/testes e revisar o fluxo responsivo e acessível.

## Fase 7: Convergence

- [X] T042 CRITICAL: Registrar diagnóstico estruturado e seguro em falhas de auditoria e migration por tenant, preservando contexto técnico suficiente para investigação sem expor connection strings ou segredos, com testes correspondentes, per Constitution V (contradicts).
- [X] T043 Investigar a falha da auditoria pré-migration do inventário local de 351 tenants e, somente após confirmar a pausa de provisionamentos, inventário e auditorias sem divergências, concluir e verificar o rollout definido em T036, per plan: rollout gate (partial). A falha anterior foi SQLSTATE `3D000`; 301 de 351 registros prontos não tinham banco correspondente. Por decisão explícita, os containers e o volume PostgreSQL exclusivos deste projeto foram removidos; os demais recursos Docker foram preservados. A nova base local tem inventário vazio e auditorias pré/pós-rollout sem divergências.
- [X] T044 Executar os testes PostgreSQL de migration, atomicidade, auditoria e isolamento entre dois tenants em bancos de teste descartáveis; registrar os resultados e qualquer bloqueio operacional, per Constitution IV (partial). `scripts/dev-up.sh --no-seed --test` passou: Domain 43, Application 23, Integration 45 e Web 41; benchmarks de performance ficam excluídos. O PostgreSQL de teste usa container efêmero sem volume, porta dinâmica e conexão injetada; os testes financeiros compartilham dois bancos de tenant, serializados, e permitem um terceiro banco temporário.
