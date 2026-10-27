# Tarefas: Fundação visual e navegação

**Entrada**: Artefatos de design em `/specs/002-fundacao-visual-navegacao/`

**Pré-requisitos**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/ui.md](contracts/ui.md), [quickstart.md](quickstart.md)

**Testes**: Incluídos porque os critérios de sucesso da especificação definem resultados mensuráveis em testes e o plano exige regressão dos fluxos de autenticação e da Feature 1. Não adicionar uma nova biblioteca de testes visuais; usar xUnit/WebApplicationFactory existentes e revisão manual do roteiro de validação.

**Organização**: Fases por história de usuário, mantendo a saída HTTP do Identity e sem alterar o domínio da Feature 1.

## Formato

Cada tarefa usa `- [ ] TNNN [P?] [USn?] descrição com caminho(s) preciso(s)`. `[P]` indica que a tarefa pode ser executada em paralelo, em arquivos distintos e sem dependência pendente. Tarefas de Setup e Foundation não recebem etiqueta de história; tarefas em fases de história recebem a etiqueta correspondente.

## Fase 1: Setup compartilhado

**Objetivo**: Adicionar a dependência visual aprovada ao projeto web existente, sem gerar outro projeto ou substituir a solução.

- [X] T001 Adicionar a referência fixa `MudBlazor` versão `9.11.0` ao projeto existente `src/FinancasSaaS.Web/FinancasSaaS.Web.csproj` e restaurar as dependências para confirmar compatibilidade com `net10.0`.

## Fase 2: Fundação (bloqueia as histórias)

**Objetivo**: Configurar os ativos, o modo de renderização, os serviços, o tema comum e o roteamento autorizado antes de migrar as telas.

- [X] T002 [P] Registrar `AddMudServices()` uma única vez em `src/FinancasSaaS.Web/Program.cs` e adicionar `@using MudBlazor` uma única vez em `src/FinancasSaaS.Web/Components/_Imports.razor`, sem alterar Identity, cookies ou políticas existentes.
- [X] T003 [P] Atualizar `src/FinancasSaaS.Web/Components/App.razor` para declarar `lang="pt-BR"`, carregar `MudBlazor.min.css` antes dos estilos próprios e `MudBlazor.min.js` junto ao script Blazor usando `@Assets`, e aplicar Interactive Server a `HeadOutlet` e `Routes` mantendo a pré-renderização existente.
- [X] T004 Configurar `src/FinancasSaaS.Web/Components/Routes.razor` para usar roteamento ciente de autorização, preservando `FocusOnNavigate` e a página NotFound; remover o `@rendermode InteractiveServer` redundante de `src/FinancasSaaS.Web/Components/Pages/Counter.razor` para herdar o modo global.
- [X] T005 [P] Criar `src/FinancasSaaS.Web/Components/Layout/MudBlazorProviders.razor` com um único `MudThemeProvider` e um único `MudPopoverProvider`, centralizando tema coeso e deixando providers de diálogo/snackbar de fora até haver uso concreto.
- [X] T006 [P] Definir em `src/FinancasSaaS.Web/wwwroot/app.css` os tokens comuns de superfície, texto, tipografia, escala de espaçamento, estados semânticos e foco visível, com regras base responsivas e contraste adequado.

**Checkpoint**: pacote restaurado, árvore de rotas interativa, autorização de páginas roteadas preservada e infraestrutura comum de tema/providers disponível. Nenhuma história deve adicionar providers duplicados.

## Fase 3: História de Usuário 1 — Navegação da plataforma (Prioridade: P1)

**Objetivo**: Oferecer ao operador autenticado uma área administrativa identificável, navegação responsiva conforme as políticas existentes e saída segura da conta.

**Teste independente**: Como administrador, confirmar destinos e ações autorizados; como operador somente leitura, confirmar consulta sem mutações; como visitante, confirmar bloqueio de acesso direto; enviar logout por POST e confirmar encerramento da sessão e bloqueio subsequente. Conferir desktop 1280 px e celular 360 px.

### Testes da História de Usuário 1

- [X] T007 [US1] Primeiro estender `tests/FinancasSaaS.Web.Tests/Authentication/AuthenticationEndToEndTests.cs` com cenários de navegação autorizada para administrador e somente leitura, recusa HTTP de rota restrita para visitante/somente leitura e logout POST protegido por antifalsificação; executar e confirmar que os novos cenários falham antes das mudanças de interface, mantendo os testes existentes de cookie e políticas.

### Implementação da História de Usuário 1

- [X] T008 [P] [US1] Substituir o shell genérico por layout responsivo de plataforma em `src/FinancasSaaS.Web/Components/Layout/MainLayout.razor` e `src/FinancasSaaS.Web/Components/Layout/MainLayout.razor.css`, incluindo uma única instância do host `MudBlazorProviders` e preservando o aviso de erro/reconexão.
- [X] T009 [P] [US1] Adicionar o fluxo HTTP de encerramento de sessão Identity em `src/FinancasSaaS.Web/Pages/Account/Logout.cshtml` e `src/FinancasSaaS.Web/Pages/Account/Logout.cshtml.cs`, aceitando POST com antifalsificação, encerrando o cookie atual e retornando ao login/entrada; não aceitar logout por GET.
- [X] T010 [US1] Implementar em `src/FinancasSaaS.Web/Components/Layout/NavMenu.razor` e `src/FinancasSaaS.Web/Components/Layout/NavMenu.razor.css` navegação desktop/mobile com item atual perceptível, destinos apenas para páginas existentes, links filtrados por políticas de leitura/mutação e formulário POST de saída apontado para `/Account/Logout`.
- [X] T011 [US1] Alinhar visualmente a página HTTP existente em `src/FinancasSaaS.Web/Pages/Account/Login.cshtml` à identidade comum, adicionando a referência aos estilos da aplicação sem converter o login em componente interativo ou alterar POST, antifalsificação, validações e retorno local.

**Checkpoint**: administrador e operador somente leitura encontram apenas opções autorizadas; as políticas do servidor continuam recusando acesso direto não autorizado; a sessão só termina por POST.

## Fase 4: História de Usuário 2 — Conclusão do primeiro acesso (Prioridade: P1)

**Objetivo**: Dar à pessoa convidada uma apresentação clara e própria, responsiva, sem menu administrativo e sem alterar o fluxo de conclusão vigente.

**Teste independente**: A rota de convite apresenta campos de senha e confirmação com rótulos e validação existentes, exibe mensagens compreensíveis nos resultados e permanece utilizável em 1280 px e 360 px, sem alterar as regras do caso de uso.

### Testes da História de Usuário 2

- [X] T012 [US2] Primeiro criar `tests/FinancasSaaS.Web.Tests/Onboarding/OnboardingEndToEndTests.cs` com cobertura HTTP da página existente `/concluir-cadastro`: campos e rótulos de senha, ausência de links administrativos e mensagem de estado renderizada, sem exigir nem inventar token válido para a checagem de apresentação; executar e confirmar a falha da expectativa de layout antes da implementação.

### Implementação da História de Usuário 2

- [X] T013 [US2] Criar `src/FinancasSaaS.Web/Components/Layout/OnboardingLayout.razor` e `src/FinancasSaaS.Web/Components/Layout/OnboardingLayout.razor.css` como shell público enxuto, distinto da plataforma, que inclui uma única instância de `MudBlazorProviders` e não exibe navegação administrativa.
- [X] T014 [US2] Migrar a apresentação de `src/FinancasSaaS.Web/Components/Pages/Onboarding/CompleteRegistration.razor` para os componentes visuais aprovados e aplicar `OnboardingLayout`, preservando rota/query `token`, modelo, atributos de validação, caso de uso, bloqueio de envio duplicado e resultado funcional.

**Checkpoint**: convidado identifica propósito, campos, validação e resultado em desktop/celular; nenhuma política, token ou regra de primeiro acesso é alterada.

## Fase 5: História de Usuário 3 — Feedback das operações existentes (Prioridade: P1)

**Objetivo**: Padronizar mensagens e estados transitórios nas páginas existentes, sem transformar estado visual em resultado de negócio.

**Teste independente**: Cada estado aplicável de operação — carregamento, sucesso, validação e falha — é comunicável por texto acessível; os formulários permanecem associados aos mesmos casos de uso e resultados. A revisão responsiva cobre 360 px, 768 px e 1280 px.

### Preparação do contrato e testes da História de Usuário 3

- [X] T015 [US3] Criar o esqueleto compilável de `src/FinancasSaaS.Web/Components/Shared/OperationFeedback.razor` e o tipo de estado em `src/FinancasSaaS.Web/Components/Shared/OperationFeedbackState.cs`, expondo apenas os parâmetros necessários (`State` e `Message`) e uma apresentação neutra sem lógica para os estados finais, para permitir compilar a suíte antes dos testes de comportamento.
- [X] T016 [US3] Criar `tests/FinancasSaaS.Web.Tests/Ui/OperationFeedbackRenderingTests.cs` usando o renderer HTML disponível no ASP.NET Core e xUnit, cobrindo estados vazio/carregando/sucesso/erro/validação, conteúdo textual e semântica acessível; executar contra o esqueleto de T015 e confirmar falhas de asserção antes de implementar a apresentação final, sem adicionar framework de teste.

### Implementação da História de Usuário 3

- [X] T017 [US3] Completar `src/FinancasSaaS.Web/Components/Shared/OperationFeedback.razor` e criar `src/FinancasSaaS.Web/Components/Shared/OperationFeedback.razor.css` para estados concretos de carregamento, sucesso, erro e validação, com texto não dependente de cor/ícone e sem expor informação sensível desnecessária; fazer os testes de T016 passarem.
- [X] T018 [US3] Substituir as mensagens ad hoc pelo componente de feedback em `src/FinancasSaaS.Web/Components/Pages/Customers/Register.razor`, `src/FinancasSaaS.Web/Components/Pages/Customers/ProvisioningStatus.razor`, `src/FinancasSaaS.Web/Components/Pages/Customers/Deactivate.razor`, `src/FinancasSaaS.Web/Components/Pages/Customers/SendInvitation.razor` e `src/FinancasSaaS.Web/Components/Pages/Onboarding/CompleteRegistration.razor`, preservando mensagens de negócio, validações, comandos e prevenção de submissões duplicadas.

**Checkpoint**: formulários e consulta exibem estados coerentes e acessíveis, sem alterar os efeitos ou os resultados da Feature 1.

## Fase 6: História de Usuário 4 — Navegação do cliente (Prioridade: P2, bloqueada por dependência)

**Objetivo**: Ter navegação própria de cliente somente quando houver página autenticada real incluída no escopo.

**Teste independente**: O inventário atual não contém página financeira autenticada do cliente; portanto, a entrega não apresenta destino, layout ativo, dados nem dashboard fictícios. Quando uma página real for adicionada ao produto, seu requisito deverá ser reavaliado antes de associar um layout de cliente.

**Dependência**: Não há página autenticada de cliente em `src/FinancasSaaS.Web/Components/Pages/`. A spec e o contrato em `specs/002-fundacao-visual-navegacao/spec.md` e `specs/002-fundacao-visual-navegacao/contracts/ui.md` definem o limite e a ativação condicional. Nenhuma implementação de `CustomerLayout` ou rota fictícia é planejada nesta entrega.

## Fase 7: Polimento e validação transversal
- [X] T019 Executar restore/build e os testes web e de fluxo existentes conforme `specs/002-fundacao-visual-navegacao/quickstart.md`, incluindo testes atuais de autenticação/autorização e cenários de logout, onboarding e feedback; a suíte da solução passou com 53 testes.
- [X] T020 Fazer a revisão visual e de acessibilidade descrita em `specs/002-fundacao-visual-navegacao/quickstart.md` em 1280 × 800 px, 768 × 1024 px e 360 × 800 px; foram verificados overflow, foco/teclado, rótulos, contraste dos tokens, conteúdo fictício e assets MudBlazor.
- [X] T021 Encerrar a avaliação moderada do CS-009 por dispensa explícita do solicitante e registrar em `specs/002-fundacao-visual-navegacao/quickstart.md` que ela não foi realizada; o critério CS-009 permanece não validado e não deve ser reportado como atingido.


**Objetivo**: Demonstrar regressão preservada, responsividade, acessibilidade e ausência de duplicação de providers.


## Dependências e ordem de execução

### Dependências entre fases

- **Setup (Fase 1)**: T001; sem dependência de outras tarefas.
- **Foundation (Fase 2)**: depende de T001; T002 e T003 podem ocorrer em paralelo; T004 depende de T003; T005 depende de T002/T003; T006 depende de T003.
- **US1 (Fase 3)** e **US2 (Fase 4)**: dependem da Foundation e podem ser implementadas em paralelo por trabalharem em layouts/páginas distintas. Dentro de US1, T007 precede implementação; T008/T009/T011 podem ser paralelizadas; T010 depende de T008 e T009. Dentro de US2, T012 precede implementação e T014 depende de T013.
- **US3 (Fase 5)**: depende de US1 e US2 porque T018 atualiza páginas que ambas formatam; T015 cria o esqueleto compilável, T016 valida o comportamento antes da implementação final T017, e T018 integra o componente nas telas.
- **US4 (Fase 6)**: bloqueada até existir uma página autenticada de cliente; nenhuma tarefa de código é gerada para inventar essa superfície.
- **Polimento (Fase 7)**: depende das histórias implementadas; concluir T019 antes da revisão manual T020. T021 foi encerrada por dispensa explícita; CS-009 permanece não validado.

### Oportunidades de paralelismo

- Após T001: T002 e T003 em paralelo.
- Após T002/T003: T004, T005 e T006 podem ser paralelizados respeitando suas dependências explícitas.
- Após Foundation e T007: T008, T009 e T011 em paralelo; T010 após o layout e o endpoint de logout.
- Após Foundation e T012: US1 e US2 podem avançar em paralelo em arquivos distintos.
- T019 precede T020. T021 foi dispensada nesta entrega e não requer execução ou evidência de participantes.

### Exemplo de paralelismo — US1

```text
Depois de T007, executar em paralelo:
- T008: layout principal da plataforma em MainLayout.razor/.razor.css
- T009: endpoint de logout HTTP em Pages/Account/Logout.cshtml(.cs)
- T011: apresentação da página HTTP em Pages/Account/Login.cshtml
Depois de T008 e T009:
- T010: menu com links autorizados e POST de logout
```

## Estratégia de implementação

### MVP sugerido: História de Usuário 1

1. Concluir T001 e a Foundation (T002–T006).
2. Concluir US1 (T007–T011), mantendo os testes de Identity/políticas existentes.
3. Validar autorização por perfil, saída HTTP e navegação em desktop, tablet/intermediário e celular conforme o checkpoint da US1.
4. Não criar área financeira de cliente ou qualquer dado fictício.

### Entrega incremental

1. Setup + Foundation estabelecem o tema e o modo de renderização necessário.
2. US1 entrega navegação e shell administrativo seguros.
3. US2 entrega o shell do primeiro acesso e a reformulação visual do convite.
4. US3 padroniza os estados das operações existentes sem alterar os fluxos.
5. US4 só pode ser reaberta quando uma página autenticada real do cliente estiver disponível.
6. Executar polimento e roteiro quickstart; CS-009 permanece não validado por dispensa da avaliação moderada. Qualquer regressão na Feature 1 bloqueia a conclusão.

## Notas

- Não executar templates de MudBlazor nem criar projeto web/cliente novo; instalar a dependência no projeto web existente.
- Um layout público de onboarding não equivale ao layout autenticado do cliente.
- O estado da checklist `checklists/ux.md` é revisão da qualidade dos requisitos, não status de implementação.
- Toda tarefa deve permanecer no formato checkbox + ID sequencial + `[P]` opcional + `[USn]` obrigatório nas fases de história + descrição com caminho(s) exato(s).

## Fase 8: Convergence

- [X] T022 Auditar os usos Bootstrap restantes em `src/FinancasSaaS.Web/Components/Pages/Counter.razor`, `src/FinancasSaaS.Web/Components/Pages/Weather.razor`, `src/FinancasSaaS.Web/Components/Pages/Error.razor` e `src/FinancasSaaS.Web/wwwroot/app.css`; substituir as classes/variáveis necessárias por estilos próprios preservando o comportamento não financeiro dessas rotas, remover os seletores Bootstrap sem consumidor e então retirar a referência Bootstrap de `src/FinancasSaaS.Web/Components/App.razor`, validando as páginas da feature e as rotas demo. per plan: CSS/script decision (partial)
