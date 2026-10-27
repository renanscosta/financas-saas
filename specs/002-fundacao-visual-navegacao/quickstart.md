# Guia de Validação: Fundação visual e navegação

**Feature**: [Fundação visual e navegação](spec.md)

## Pré-requisitos

- SDK .NET 10 e dependências locais descritas no README do repositório.
- PostgreSQL e Mailpit locais ativos quando os fluxos de convite ou persistência forem verificados.
- Contas de teste existentes: administrador e operador somente leitura.
- Navegador atualizado com suporte ao Blazor Server.

## Restaurar, compilar e testar

Na raiz da solução:

```bash
dotnet restore FinancasSaaS.sln
dotnet build FinancasSaaS.sln
dotnet test tests/FinancasSaaS.Web.Tests/FinancasSaaS.Web.Tests.csproj
```

Executar a suíte completa quando forem feitas alterações compartilhadas ou quando validar a não regressão integral da Feature 1:

```bash
dotnet test FinancasSaaS.sln
```

Resultados esperados: compilação sem erros; testes de login, cookie e papéis preservados; testes de autorização continuam aprovando/recusando os mesmos destinos; testes novos cobrem logout, navegação autorizada e feedback de UI pertinente.

## Executar para inspeção visual

Inicie os serviços locais conforme [quickstart da Feature 1](../001-cadastro-provisionamento-cliente/quickstart.md) e execute:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj
```

Autentique-se pela página de login existente em `/Account/Login` e inspecione estas rotas já existentes:

- `/` — entrada.
- `/platform/customers/register` — cadastro administrativo.
- `/platform/customers/provisioning` — consulta e, para administrador, retentativa.
- `/platform/customers/deactivate` — desativação.
- `/platform/customers/invite` — convite.
- `/concluir-cadastro?token=<convite-de-teste>` — primeiro acesso, com convite de teste válido.

Não use `/counter` ou `/weather` como demonstração de funcionalidades financeiras. A página `/Account/Logout` deve ser alcançada pelo controle de saída da interface; uma navegação GET direta não deve encerrar a sessão.

## Matriz manual de aceitação

Execute cada cenário em viewport desktop de 1280 × 800 px, intermediário/tablet de 768 × 1024 px e celular de 360 × 800 px. Em larguras intermediárias adicionais entre 391 px e 1023 px, confirme também que não há rolagem horizontal.

1. Abrir `/Account/Login` e conferir identidade visual, rótulos, mensagens de validação, foco e ausência de rolagem horizontal; confirmar que a submissão permanece HTTP com Identity.
2. Abrir cada rota permitida e conferir identidade da área, título, item de navegação atual e ausência de rolagem horizontal.
3. Redimensionar a janela para celular; abrir/fechar navegação usando teclado e verificar que os mesmos destinos autorizados continuam alcançáveis.
4. Tabular pela navegação, formulários, mensagens e saída; verificar ordem lógica, foco visível e ativação pelo teclado. Revisar nomes acessíveis, associação de erros aos campos e anúncios de estado; avaliar contraste pelo nível AA das WCAG 2.1.
5. Entrar como administrador: confirmar links e ações administrativas; entrar como operador somente leitura: confirmar acesso à consulta sem links/ações de mutação.
6. Sem autenticação, tentar acessar diretamente as rotas administrativas; confirmar redirecionamento/recusa pelo servidor. Repetir com operador somente leitura em rota de mutação.
7. Em cada formulário existente, submeter dados inválidos e observar erros ligados ao campo; iniciar operação e verificar indicação de carregamento, bloqueio de duplicidade, resultado de sucesso ou erro compreensível.
8. Usar a ação de saída, confirmar encerramento do cookie/sessão e retorno à entrada; tentar abrir novamente uma rota administrativa e confirmar recusa.
9. Executar cadastro, consulta/retentativa, convite e conclusão conforme cenários de [quickstart da Feature 1](../001-cadastro-provisionamento-cliente/quickstart.md); confirmar os mesmos efeitos e mensagens de negócio essenciais, sem alteração de persistência ou regras.
10. Confirmar que nenhuma tela, ação, gráfico ou dado financeiro fictício é apresentado como funcionalidade disponível em qualquer uma das rotas cobertas; páginas demo não devem ser rotuladas como funções financeiras.
11. Verificar que cada árvore de layout ativa contém um único host dos providers MudBlazor necessários, que nenhum provider aparece duplicado em páginas e que o console não registra provider ausente ou falha de carregamento de CSS/JS.
12. Confirmar que o layout de plataforma e o layout público de onboarding são distintos; não existe layout/rota ativa de cliente até que uma tela autenticada desse perfil entre no escopo.

## Avaliação moderada de usabilidade (CS-009)

**Status nesta entrega**: Não realizada por indisponibilidade de participantes, conforme dispensa explícita do solicitante. O critério CS-009 permanece não validado; os resultados abaixo não devem ser inventados ou presumidos.

- Recrutar pelo menos cinco participantes distintos de cada perfil com interface incluída: administrador da plataforma, operador somente leitura e pessoa convidada (mínimo de 15 participantes no total).
- Para cada participante, sem orientação durante a tarefa, pedir que identifique a página/área atual e localize a ação principal disponível no fluxo correspondente ao seu perfil.
- Registrar por perfil quantas pessoas concluíram a tarefa sem ajuda. Calcular a taxa agregada como participantes bem-sucedidos dividido pelo total de participantes; o critério é atingido com pelo menos 90% no total.
- Registrar perfil, tamanho de viewport, resultado e bloqueios observados neste guia, sem incluir dados pessoais identificáveis dos participantes.
- Não incluir o perfil de cliente autenticado enquanto não existir uma página real desse perfil.

## Evidências mínimas

- Resultado do `dotnet build` e dos testes web (e suíte completa se apropriado).
- Capturas ou revisão visual dos tamanhos 1280 px, 768 px e 360 px para área administrativa e primeiro acesso.
- Resultado de autorização para administrador, somente leitura e visitante, incluindo tentativa de URL direta.
- Evidência de logout bem-sucedido e de bloqueio subsequente.
- Confirmação de que a Feature 1 preserva os resultados de negócio.
- Registro agregado, sem dados pessoais, da avaliação moderada requerida por CS-009.
