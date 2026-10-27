# Plano de Implementação: Fundação visual e navegação

**Branch**: `002-fundacao-visual-navegacao` | **Data**: 2026-09-30 | **Especificação**: [spec.md](spec.md)

**Entrada**: Plano da Feature 2 solicitado para a solução .NET 10/Blazor Web App existente. Usar MudBlazor sem recriar o projeto, substituir a autenticação ou alterar a Feature 1.

## Resumo

Aplicar uma identidade visual consistente e navegação responsiva às telas existentes de administração, login e conclusão de cadastro por convite. A integração adiciona MudBlazor 9.11.0 ao projeto FinancasSaaS.Web atual e usa o Interactive Server já habilitado pelo host, que será efetivamente propagado à árvore roteada para satisfazer o requisito de interatividade de MudBlazor. O fluxo de Identity permanece nas Razor Pages e com os mesmos cookies, papéis e políticas. O plano define fronteiras distintas de layout para plataforma e cliente; como não há páginas financeiras autenticadas de cliente, a implementação do layout do cliente fica condicionada à entrada de uma página real no escopo, sem criar casca vazia ou destino fictício. Nenhum dado financeiro, domínio ou persistência é adicionado.

## Estado Atual Inspecionado

- `App.razor` carrega Bootstrap, CSS da aplicação e `FinancasSaaS.Web.styles.css`; `HeadOutlet` e `Routes` não declaram modo de renderização. Carrega o modal de reconexão e `blazor.web.js`.
- `_Imports.razor` importa os namespaces padrão de componentes e o tipo `RenderMode`, mas não importa MudBlazor.
- `Program.cs` registra `AddInteractiveServerComponents()` e mapeia `AddInteractiveServerRenderMode()`, além de Identity/cookies, políticas e `AddCascadingAuthenticationState()`. A árvore de rotas não se torna interativa só por esses registros; `Counter.razor` é a única página examinada com `@rendermode InteractiveServer` explícito e esse modo se torna redundante se a interatividade global for aplicada.
- Há um `MainLayout` genérico, um `NavMenu` com links hard-coded sem filtragem por política e um modal de reconexão. A conclusão do convite usa hoje o layout genérico.
- As páginas de cadastro, provisionamento, desativação e convite já têm atributos/políticas de autorização e mensagens básicas, mas apresentam estados e formulários com HTML simples e sem um padrão visual comum.
- Login é uma Razor Page HTTP (`Layout = null`) que usa Identity, POST e antifalsificação. Não foi encontrada ação de logout.
- O inventário contém páginas demo `Counter` e `Weather`, mas não contém rota, página ou layout autenticado de finanças para cliente.

## Contexto Técnico

**Linguagem/Versão**: C# 14 / .NET 10 (`net10.0` nos projetos existentes).

**Dependências Principais**: Aplicação web ASP.NET Core/Blazor existente; ASP.NET Core Identity e políticas existentes; MudBlazor 9.11.0, versão estável mais recente verificada no NuGet em 2026-09-30, com suporte completo declarado a .NET 10. Adicionar o pacote ao projeto web existente, sem usar templates geradores.

**Armazenamento**: Nenhuma alteração. PostgreSQL e EF Core permanecem inalterados; não há novas entidades ou migrations.

**Testes**: xUnit e `Microsoft.AspNetCore.Mvc.Testing` no projeto FinancasSaaS.Web.Tests. Regressão HTTP/autorização automatizada e inspeção manual no navegador para teclado, estados e tamanhos de viewport definidos pela especificação; não há atualmente framework de teste visual/browser no repositório.

**Plataforma Alvo**: Web no servidor Linux, com navegadores desktop e mobile; Interactive Server por circuito Blazor.

**Tipo de Projeto**: Aplicação web Blazor Web App existente, monólito modular .NET, sem novo frontend ou projeto cliente.

**Metas de Desempenho**: Sem novo SLO de backend ou dados. Manter carregamento inicial pré-renderizado e não acrescentar chamadas de rede de dados à navegação; comunicar operações em curso pelas regras da especificação.

**Restrições**: Preservar ASP.NET Core Identity, cookie de aplicação, Razor Page de login, políticas e handlers do servidor; filtros visuais não substituem autorização no servidor. Mudanças só na camada web e testes/documentação relacionados. Não alterar domínio, isolamento multi-tenant, dados ou comportamento da Feature 1. MudBlazor não dá suporte a SSR estática para seus componentes interativos; todos os providers e componentes MudBlazor usados juntos precisam compartilhar modo interativo.

**Escala/Escopo**: Layout de plataforma e navegação das páginas administrativas existentes, identidade do login, layout público separado para conclusão por convite, feedback de formulários/consultas e experiência celular, intermediária/tablet e desktop. O contrato define um layout autenticado de cliente distinto, mas sua implementação/uso depende de uma página real de cliente, ausente hoje. Não inclui dashboard financeiro nem páginas de cliente fictícias.

## Verificação da Constituição

*Gate: aprovado antes da Fase 0; reavaliar após o design da Fase 1.*

- **I. Isolamento entre tenants e segurança — PASS**: nenhuma consulta ou decisão de tenant é movida para o cliente. Preservar políticas/handlers existentes e verificar acesso direto e navegação interativa. `AuthorizeRouteView` deve respeitar os atributos e políticas atuais; a navegação condicional é somente UX.
- **II. Integridade financeira e auditabilidade — PASS / não aplicável a novos dados**: não há operações financeiras, valores, mutações, persistência ou trilha de auditoria adicionada. Feedback não inventa sucesso e reflete o resultado original do caso de uso.
- **III. Domínio pragmático e arquitetura limpa — PASS**: MudBlazor fica limitado a FinancasSaaS.Web. Nenhum projeto novo nem dependência de UI nos projetos Domain/Application.
- **IV. Testes automatizados — PASS com gate de regressão**: manter os testes de Identity e autorização e adicionar cobertura para logout e navegação/handlers autorizados que forem alterados; fazer validação visual e de teclado nos tamanhos da especificação.
- **V. Evolução rastreável e incremental — PASS**: pacote fixado e justificado por pesquisa NuGet; mudanças concentradas em UI, com roteiro de validação e sem mudança de autenticação.
- **Gate de complexidade — PASS**: não há violação que justifique novos projetos, infraestrutura, persistência ou bibliotecas de teste. Uma mudança para interatividade global é necessária para que provedores/layout MudBlazor funcionem juntos; validar rigorosamente SSR, login, autorização e fluxos da Feature 1 antes de integração.

## Estrutura do Projeto

### Documentação desta feature

```text
specs/002-fundacao-visual-navegacao/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ui.md
├── checklists/
│   └── requirements.md
└── tasks.md             # criado posteriormente por /speckit-tasks
```

### Código-fonte (raiz do repositório)

```text
src/FinancasSaaS.Web/
├── Components/
│   ├── App.razor                       # ativos MudBlazor e modos de renderização
│   ├── Routes.razor                    # autorização durante roteamento interativo
│   ├── _Imports.razor                  # imports de UI
│   ├── Layout/
│   │   ├── MainLayout.razor            # shell/layout da plataforma
│   │   ├── NavMenu.razor               # navegação por política/viewport
│   │   ├── OnboardingLayout.razor      # shell enxuto do fluxo de convite
│   │   ├── MudBlazorProviders.razor    # providers comuns, uma vez por layout ativo
│   │   └── ReconnectModal.razor        # reconexão com feedback coerente
│   ├── Shared/
│   │   ├── OperationFeedback.razor     # componente concreto usado pelos formulários existentes
│   │   └── OperationFeedbackState.cs
│   └── Pages/
│       ├── Home.razor
│       ├── Customers/                  # cadastro, status, desativação e convite
│       └── Onboarding/CompleteRegistration.razor
├── Pages/Account/                      # login Identity existente + saída HTTP
│   └── Login.cshtml                    # identidade visual alinhada, fluxo HTTP intacto
├── wwwroot/app.css                     # tokens e ajustes próprios da aplicação
├── FinancasSaaS.Web.csproj             # MudBlazor 9.11.0 somente neste projeto
└── Program.cs                          # serviço MudBlazor, sem substituir Identity

tests/FinancasSaaS.Web.Tests/
├── Authentication/AuthenticationEndToEndTests.cs
├── Security/                           # políticas/handlers existentes
├── Onboarding/                         # teste HTTP do fluxo de convite
└── Ui/                                 # teste de renderização do feedback
```

**Decisão de Estrutura**: Trabalhar nos componentes e ativos do único projeto web e nos testes web existentes. A instalação será manual no `FinancasSaaS.Web.csproj`; não executar `dotnet new mudblazor` nem atualizar/reorganizar a solução. `MainLayout` é o shell da plataforma; `OnboardingLayout` é o shell público e enxuto da rota de convite. Um layout autenticado de cliente não faz parte da árvore de arquivos desta entrega: sua criação e associação ficam condicionadas à entrada de uma página real e autorizada no escopo, sem criar rota nem casca sem consumidor.

## Decisões de Design

1. **Versão e instalação**: fixar MudBlazor `9.11.0`, no projeto web atual. A página NuGet consultada lista essa versão como atual em 2026-09-30, indica alvo .NET 8.0 compatível com essa versão ou superior e suporte completo da linha 9 a .NET 10. Referências e racional em [research.md](research.md).
2. **Interatividade real**: o host registra e mapeia Interactive Server, mas `App.razor` deixa `Routes` e `HeadOutlet` sem modo explícito; o modo padrão da árvore é SSR estática, exceto páginas que declaram o próprio modo. Aplicar Interactive Server global a `Routes` e `HeadOutlet` mantendo prerender padrão. Remover do `Counter` o `@rendermode` redundante para que as páginas herdem o modo da rota sem fronteiras de renderização aninhadas. Não ativar WebAssembly.
3. **Autorização sob navegação interativa**: substituir a visualização simples de rota pela variante consciente de autorização, conservando os atributos, políticas e conteúdo protegido existentes. Verificar com requests HTTP e navegação dentro do circuito que visitante/operador somente leitura não alcança ações administrativas.
4. **Identity e sair da conta**: conservar login como Razor Page com POST e antifalsificação; alinhá-lo à identidade visual compartilhada sem alterar seu modelo ou fluxo. Como a página de login é SSR HTTP separada do roteador de componentes, usar os tokens/estilos compartilhados e HTML semântico, sem inserir componentes MudBlazor dependentes de interatividade nela. Adicionar saída HTTP explícita, também protegida contra CSRF, que encerra a sessão pelo Identity atual e força navegação/requisição completa ao login; nunca executar `SignOut` dentro do circuito nem usar GET para mudar sessão.
5. **Registro e providers MudBlazor**: chamar `AddMudServices()` uma única vez em `Program.cs`; adicionar `@using MudBlazor` uma única vez em `_Imports.razor`. Incluir um provider host compartilhado nos layouts concretos de plataforma e onboarding; cada render tree ativa contém exatamente um `MudThemeProvider` e um `MudPopoverProvider`. Não colocar cópias em páginas ou filhos. Incluir `MudDialogProvider`/`MudSnackbarProvider` somente se forem adotados diálogos/snackbars concretos; o feedback padrão deve ser inline e persistente. Um `CustomerLayout` futuro usará o mesmo host compartilhado uma única vez quando houver página cliente.
6. **CSS e script**: definir `lang="pt-BR"` no documento, incluir `MudBlazor.min.css` no `<head>` antes de `app.css`/CSS de aplicação e `MudBlazor.min.js` junto ao script Blazor em `App.razor`, servidos uma vez pelo asset pipeline existente (`@Assets` e `MapStaticAssets()`). Migrar primeiro as telas cobertas, validar dependências de CSS em rotas demo e então remover a referência global de Bootstrap que conflita com o tema; não manter controles de tela migrada estilizados simultaneamente por Bootstrap e MudBlazor. Login HTTP reutiliza tokens de tema/CSS sem componentes MudBlazor interativos.
7. **Tema e responsividade**: estabelecer tokens semânticos de cor/superfície, tipografia, escala de espaçamento, foco e estados; direção visual com cor primária teal/azul-petróleo, neutros claros, texto de alto contraste e cores semânticas de sucesso/aviso/erro. Usar família tipográfica de sistema com fallback, sem dependência de carregamento externo. Navegação expansível acessível em mobile e estrutura lateral em desktop, com a mesma lista autorizada.
8. **Componentes reutilizáveis**: criar um componente de feedback de operação somente após mapear seu uso nas páginas existentes de cadastro, provisionamento, desativação, convite e conclusão de cadastro; cobrir carregando/sucesso/erro/validação e anúncio acessível. Não criar wrappers genéricos de campo/cabeçalho sem uso repetido demonstrado. Manter a lógica do modal de reconexão e alinhar texto, visual e acessibilidade.
9. **Layouts separados**: aplicar `MainLayout` às rotas de plataforma; `OnboardingLayout` à rota pública de convite. Definir no contrato de UI o `CustomerLayout` distinto, sem links/admin actions, mas adiar arquivo/uso enquanto o repositório não tiver tela autenticada de cliente. O onboarding não é tratado como área autenticada do cliente.
10. **Regressão e validação**: preservar formulários, comandos, mensagens funcionais essenciais, rotas, tokens de convite e validações. Testar login/cookie, papéis, acesso permitido/negado, logout e fluxos da Feature 1. Fazer inspeção visual em 1280 px desktop, 768 px intermediário/tablet e 360 px mobile; revisar navegação por teclado, foco, labels, erros associados, estados, contraste e ausência de telas/dados fictícios. Executar a avaliação moderada definida pelo CS-009 para os perfis com telas existentes. Aparência/estados transitórios não mudam casos de uso nem decisões de segurança.

## Verificação da Constituição após o Design

- **Isolamento e segurança — PASS condicionado aos testes**: autenticação permanece no servidor; o roteador interativo aplica as mesmas políticas e os casos de teste cobrem acesso direto, navegação e ações de mutação. Nenhum tenant é enviado como autoridade pela UI.
- **Integridade financeira e auditabilidade — PASS**: sem entidades nem mutações financeiras; falhas exibidas não são convertidas em êxito.
- **Domínio e arquitetura — PASS**: dependência MudBlazor restrita ao projeto Web; nenhuma mudança nos projetos de domínio/aplicação/infraestrutura.
- **Testes — PASS condicionado à validação descrita**: executar testes web e de autorização, acrescentar cobertura de logout/fluxos afetados e confirmar visualmente teclado/mobile/desktop. Se teste de login ou política falhar, não aceitar o plano como implementado até correção.
- **Evolução incremental — PASS**: dependência única verificada, instalação não destrutiva e sem scaffold de template. Quickstart preserva validação da Feature 1.

## Rastreamento de Complexidade

Nenhuma violação da constituição foi identificada. A ativação do Interactive Server na árvore comum não adiciona serviço/projeto; é o requisito de integração dos componentes MudBlazor compartilhados, com a regressão de autenticação e fluxos definida como gate. Nenhuma linha de complexidade extraordinária precisa de justificativa.
