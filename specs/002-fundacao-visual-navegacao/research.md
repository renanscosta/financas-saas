# Pesquisa Técnica: Fundação visual e navegação

**Data**: 2026-09-30
**Feature**: [spec.md](spec.md)

## Decisões

### 1. Biblioteca visual e versão

- **Decisão**: Adotar MudBlazor `9.11.0`, estável mais recente exibida no NuGet em 2026-09-30, como dependência do projeto web já existente `src/FinancasSaaS.Web`.
- **Evidência**: A página NuGet lista `9.11.0` como versão atual e informa que o pacote tem como alvo .NET 8.0, é compatível com essa versão ou superior e que a linha MudBlazor 9 tem suporte completo a .NET 8, 9 e 10.
- **Racional**: O projeto web já usa `net10.0`; a compatibilidade declarada cobre .NET 10. Instalar o pacote no projeto existente evita criar outra aplicação ou substituir autenticação, rotas e configuração já entregues.
- **Alternativas consideradas**: versões preview/nightly (não escolhidas por não serem estáveis); executar o template `MudBlazor.Templates` (rejeitado porque gera outro esqueleto e pode sobrescrever/substituir o que o usuário pediu para preservar).

### 2. Integração de renderização com MudBlazor

- **Decisão**: Usar a renderização interativa no servidor já habilitada pelo host como modo global da árvore de rotas da aplicação. Manter a pré-renderização padrão e o projeto único; colocar os provedores MudBlazor em um único componente compartilhado que cada layout mutuamente exclusivo inclui uma vez. O `HeadOutlet` acompanha o modo de `Routes` para que títulos e metadados continuem atualizáveis.
- **Estado observado no repositório**: `Program.cs` já chama `AddInteractiveServerComponents()` e `AddInteractiveServerRenderMode()`, porém `App.razor` renderiza `Routes` e `HeadOutlet` sem `@rendermode`. Portanto, a interatividade está disponível, mas a árvore roteada é estática por padrão; `Counter.razor` é a exceção que solicita explicitamente `InteractiveServer`. Não se deve confundir o registro do endpoint com um modo interativo automaticamente aplicado às páginas.
- **Evidência de compatibilidade**: A documentação oficial do MudBlazor declara que renderização estática não é suportada para seus componentes interativos e requer que os provedores e os componentes que deles dependem usem o mesmo modo interativo. Para interatividade global aplicada a `Routes`, recomenda provedores nos layouts. A documentação ASP.NET Core .NET 10 confirma que o modo padrão é estático e que interatividade global pode ser atribuída a `Routes` (e `HeadOutlet`).
- **Racional**: Layout e navegação compartilhados precisam continuar interativos em todas as telas web abrangidas; registrar o serviço sozinho não os torna interativos. Não introduzir WebAssembly ou projeto `.Client`, ausentes da solução.
- **Alternativas consideradas**: deixar as telas estáticas (incompatível com controles MudBlazor); adicionar interatividade página por página (fragmenta providers/layout e exige esforço repetido); criar um projeto cliente/WebAssembly (fora do escopo e desnecessário para o Blazor Server existente).

### 3. Compatibilidade com autenticação e autorização atuais

- **Decisão**: Reutilizar ASP.NET Core Identity, cookie de autenticação, papéis e políticas existentes. Trocar o roteador para uma visualização de rota ciente de autorização para respeitar os mesmos atributos de autorização durante navegação interativa, sem alterar os requisitos das políticas. A interface filtra links por política apenas para conveniência; a autorização de servidor continua autoritativa.
- **Saída da conta**: Integrar uma ação de saída que use o mecanismo de Identity existente por uma solicitação HTTP segura com antifalsificação, preservando a tela de login como Razor Page estática. Não executar tarefas de login/logout de Identity dentro de componentes interativos.
- **Evidência**: Microsoft Learn recomenda Razor Pages para tarefas HTTP de Identity, como entrar e sair, e documenta a autorização de componentes roteados, `AuthorizeRouteView` e a diferença entre ocultar controles e impor autorização no servidor. A página de login existente já usa Razor Pages, cookie Identity e token antifalsificação.
- **Racional**: Atende à navegação global sem trocar identidade, sessão, credenciais, papéis, políticas ou fluxos da Feature 1. Requer testes antes/depois para evitar regressão de acesso e logout.
- **Alternativas consideradas**: implementar autenticação própria dentro de MudBlazor (rejeitada); confiar somente em links ocultos (rejeitada); tornar o login interativo (rejeitada, desnecessária e divergente da orientação de Identity).

### 4. Ativos e estilos

- **Decisão**: Fazer registro manual e único de `AddMudServices()` em `Program.cs`, adicionar `@using MudBlazor` uma vez a `_Imports.razor`, incluir `MudBlazor.min.css` no `<head>` e `MudBlazor.min.js` junto ao script Blazor em `App.razor`, referenciados por `@Assets`. O host já usa `MapStaticAssets()`. O host compartilhado deve renderizar exatamente um `MudThemeProvider` e um `MudPopoverProvider` por árvore/layout ativo; incluir `MudDialogProvider` ou `MudSnackbarProvider` somente se houver componente de diálogo/snackbar em uso. As páginas não adicionam providers próprios. Centralizar o tema em MudBlazor e CSS da aplicação. Evitar que Bootstrap e MudBlazor controlem simultaneamente os mesmos componentes; retirar o stylesheet Bootstrap da composição principal depois de migrar as telas e revisar usos legados.
- **Evidência**: O guia oficial de instalação do MudBlazor descreve instalação manual NuGet, imports, `AddMudServices`, CSS/JS e os providers necessários/opcionais, incluindo integração com `@Assets` e `MapStaticAssets()` em .NET 9 ou posterior. Para interatividade global, recomenda providers em layouts; com interatividade por página, o layout estático exigiria providers por página.
- **Racional**: Reutiliza os ativos e a infraestrutura da aplicação atual, sem template gerador ou frontend separado.
- **Alternativas consideradas**: referências externas a CDN (não preferidas por disponibilidade, cache e controle operacional); registrar serviços ou instanciar providers em mais de um ponto da mesma árvore (rejeitado para evitar duplicação); manter dois sistemas de componentes concorrentes nas telas migradas (evitado para reduzir conflito visual).

### 5. Dados e validação

- **Decisão**: Não criar entidades persistidas nem modificar dados do domínio. Tratar feedback de formulários e navegação como estado efêmero da interface. Usar os testes xUnit e `WebApplicationFactory` já presentes; validar layout por revisão do navegador nas larguras da especificação, sem adicionar uma biblioteca de testes de navegador nesta feature.
- **Racional**: A feature é exclusivamente visual/navegação; os fluxos de negócio e persistência pertencem à Feature 1.

## Fontes consultadas

- NuGet, MudBlazor: https://www.nuget.org/packages/MudBlazor/ — versão atual e frameworks suportados, consultado em 2026-09-30.
- NuGet, MudBlazor 9.11.0: https://www.nuget.org/packages/MudBlazor/9.11.0 — metadados fixos da versão e compatibilidade.
- Instalação oficial do MudBlazor: https://mudblazor.com/getting-started/installation — instalação manual, ativos, serviços e restrição de renderização estática.
- Microsoft Learn, modos de renderização .NET 10: https://learn.microsoft.com/pt-br/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0 — modo estático padrão, renderização interativa global e propagação.
- Microsoft Learn, autenticação e autorização Blazor .NET 10: https://learn.microsoft.com/pt-br/aspnet/core/blazor/security/?view=aspnetcore-10.0 — integração com Identity, autorização de rotas e saída.
- Código da aplicação: `src/FinancasSaaS.Web/FinancasSaaS.Web.csproj`, `Program.cs`, `Components/App.razor`, `Components/Routes.razor`, `Components/Layout/MainLayout.razor`, `Components/Pages/Counter.razor` e `Pages/Account/Login.cshtml`.
- Testes atuais: `tests/FinancasSaaS.Web.Tests/Authentication/AuthenticationEndToEndTests.cs` e `tests/FinancasSaaS.Web.Tests/Security/`.

## Questões pendentes

Nenhuma questão técnica bloqueante. A área autenticada de finanças do cliente não existe no código observado e permanece dependência fora da entrega; o plano não cria rotas, dados ou dashboard simulados.
