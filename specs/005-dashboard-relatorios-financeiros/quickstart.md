# Guia de validação: Dashboard e relatórios financeiros

**Feature**: [spec.md](spec.md)

## Pré-requisitos

- SDK .NET 10 e dependências da solução.
- PostgreSQL local disponível conforme o README e a configuração de desenvolvimento.
- Dois tenants financeiros prontos, cada um com cliente autenticável, para validar isolamento.
- No primeiro tenant: categorias ativas, lançamentos avulsos de entrada e despesa, ao menos uma compra parcelada e lançamentos nos estados previsto, realizado e cancelado, com datas de competência, previstas e efetivas em meses diferentes.
- Navegador atualizado para inspeção da experiência Blazor Server.

## Restaurar, compilar e testar

Na raiz da solução:

```bash
dotnet restore FinancasSaaS.sln
dotnet build FinancasSaaS.sln
dotnet test tests/FinancasSaaS.Domain.Tests/FinancasSaaS.Domain.Tests.csproj
dotnet test tests/FinancasSaaS.Application.Tests/FinancasSaaS.Application.Tests.csproj
dotnet test tests/FinancasSaaS.IntegrationTests/FinancasSaaS.IntegrationTests.csproj
dotnet test tests/FinancasSaaS.Web.Tests/FinancasSaaS.Web.Tests.csproj
```

Para a suíte do MVP com PostgreSQL descartável e sem testes de desempenho:

```bash
scripts/dev-up.sh --no-seed --test
```

Para medir a agregação com 5.000 lançamentos em um PostgreSQL descartável já configurado:

```bash
dotnet test tests/FinancasSaaS.IntegrationTests/FinancasSaaS.IntegrationTests.csproj --filter FullyQualifiedName~FinanceReportPerformanceTests --logger 'console;verbosity=detailed'
```

Execute testes de integração somente com esse comando ou contra bancos isolados configurados por `PLATFORM_CONNECTION_STRING`; nunca contra bancos de produção. Sem conexão de teste disponível, registre a execução como pendente em `tasks.md` e não trate a compilação como evidência de integração validada.

## Executar para inspeção

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj
```

Entrar como cliente autenticado e abrir `/cliente/dashboard` e `/cliente/relatorios`, conferindo os fluxos de [contracts/ui.md](contracts/ui.md).

## Cenários de aceitação ponta a ponta

1. **Totais do mês**: com um conjunto conhecido de lançamentos e parcelas, abrir o mês e conferir quatro totais, saldo realizado e saldo "ainda a realizar", nos dois critérios, comparando com o cálculo manual. Conferir o aviso de que previsão não é saldo bancário e o sinal textual do saldo negativo.
2. **Mês vazio e mês só com cancelados**: selecionar mês sem lançamentos e mês com apenas cancelados; confirmar estado de ausência de dados, R$ 0,00 e nenhuma informação fictícia.
3. **Falha de consulta**: simular falha da consulta; confirmar mensagem de erro, ação de tentar novamente, ausência de zeros e de detalhes técnicos, e registro operacional da falha.
4. **Despesas por categoria**: conferir previsto e realizado por categoria, inclusive categoria renomeada exibida com o nome atual e despesas de parcelas na categoria correta; entradas não aparecem nos agrupamentos.
5. **Relatório e filtros**: combinar período, categoria e estado; conferir somente linhas que atendem a todos os filtros, a inclusão das datas-limite (pagamento/recebimento), a seleção por meses de calendário (competência) e que cancelados não são somados.
6. **Validação de período**: tentar data ausente, data inicial posterior à final e intervalo de 25 meses; confirmar recusa sem totais. Confirmar que 24 meses exatos são aceitos.
7. **Conciliação**: para cada total do dashboard e do relatório, abrir a composição e verificar que a soma dos itens é idêntica ao total da própria lista, inclusive parcelas identificadas por compra e número. Sem mudança nos dados, confirmar também a igualdade com o total anterior; se os totais diferirem após uma alteração, confirmar o aviso de atualização.
8. **Mudança de estado de parcela**: realizar uma parcela prevista (com data efetiva em outro mês) pelas telas da Feature 4; reconsultar e confirmar que ela sai do previsto e entra no realizado, no mês correto de cada critério, e que totais e listas continuam conciliados.
9. **Cancelamento**: cancelar um lançamento e uma parcela; reconsultar e confirmar que somem de totais, agrupamentos e listas.
10. **Dados alterados com a lista aberta**: abrir o dashboard, alterar um lançamento em outra sessão e abrir a composição; confirmar lista e total da lista conciliados e aviso de dados atualizados.
11. **Critério e preferência**: sem preferência, o critério inicial é competência; trocar o critério sem salvar não persiste; salvar como padrão persiste entre sessões; falha de gravação mantém a preferência anterior e informa o erro; a explicação do critério em uso é sempre visível. Em teste de persistência com dois usuários no mesmo tenant, salvar critérios diferentes e confirmar a independência; se a interface ainda não suportar múltiplos usuários, registrar essa limitação na verificação de ponta a ponta.
12. **Isolamento**: com o usuário do tenant A, usar identificadores de categoria e parâmetros de URL do tenant B, e confirmar recusa neutra, nenhum dado de B e preferência de A independente da de B. Repetir no nível de persistência com dois bancos de teste. Tentar acessar as rotas sem autenticação e sem acesso ativo.
13. **Apresentação e acessibilidade**: validar 360 px, 768 px e 1280 px sem rolagem horizontal, navegação por teclado, foco visível, mensagens anunciadas, formato brasileiro de valores e datas, e que nenhum significado depende só de cor.
14. **Respostas concorrentes**: abrir uma composição e outra antes de a primeira responder; confirmar que apenas a mais recente é exibida e que a antiga não encerra o carregamento atual.
15. **Desempenho (fora do gate do MVP)**: com 5.000 lançamentos em um mês, o dashboard apresenta totais em até 3 segundos em ambiente de teste (CS-011).

## Protocolo moderado de usabilidade

- Recrutar pelo menos cinco clientes pessoa física ou participantes de perfil equivalente. Usar ambiente de teste com dados de teste previamente cadastrados, incluindo uma despesa por categoria e uma parcela identificável.
- Apresentar a cada participante, sem demonstrar a interface: identificar o saldo realizado do mês, dizer se os valores previstos são saldo bancário e identificar a regra de data em uso (CS-008); abrir a composição de um total de despesa por categoria e localizar uma parcela de compra parcelada (CS-009).
- O moderador lê as mesmas tarefas para todos, não indica controles, caminhos ou respostas e não corrige o participante durante a execução. Registrar qualquer orientação como falha na tarefa correspondente; perguntas de esclarecimento sobre a tarefa não podem revelar a solução.
- Para CS-008, registrar se o participante identifica corretamente os três elementos sem orientação. Para CS-009, cronometrar desde o fim da leitura da tarefa até a localização da parcela; sucesso exige completar ambos os passos sem orientação em até dois minutos.
- Calcular separadamente o percentual de participantes bem-sucedidos em cada critério. Cada um exige pelo menos 90% de sucesso; com cinco participantes, todos devem ter sucesso. Registrar perfil, quantidade, roteiro, resultados individuais sem dados pessoais desnecessários, tempos de CS-009 e quaisquer orientações ou interrupções.
- Se não houver participantes suficientes ou a avaliação não for realizada, registrar a pendência; não declarar CS-008 ou CS-009 atendidos. Os resultados só podem ser registrados após a implementação e a execução do estudo.

## Rollout de tenants existentes

A feature adiciona uma migration ao esquema do tenant (preferência e índice). Antes de liberar as rotas, seguir o [runbook de rollout](../../database/finance/tenant-rollout.md) e o [quickstart da Feature 3](../003-categorias-lancamentos-manuais/quickstart.md): pausar provisionamentos, gerar e auditar o inventário, aplicar as migrations com o runner existente, confirmar todos os tenants prontos e só então publicar a versão com as rotas. Tenants provisionados depois recebem o esquema pelo provisionador. Qualquer divergência interrompe o rollout e mantém as rotas bloqueadas para tenants ainda não atualizados.

## Evidências mínimas

- Build e testes de Domain, Application, integração PostgreSQL e Web.
- Teste comparando agregações do banco com a regra de domínio aplicada a um conjunto gerado, nos dois critérios.
- Conciliação total versus composição em dashboard e relatório, com parcelas.
- Testes de dois tenants para agregações, listas, categoria por identificador e preferência.
- Testes de mudança de estado de parcela, cancelamento, mês vazio, falha de consulta e limites de período.
- Evidência do rollout da migration em tenants novos e existentes antes da publicação operacional; esse trabalho foi adiado para uma feature dedicada e não bloqueia o encerramento da implementação.

## Resultado local da implementação

- Build da solução e 245 testes sem desempenho passaram com PostgreSQL 17 descartável e Mailpit pelo comando `scripts/dev-up.sh --no-seed --test`.
- Agregação de 5.000 lançamentos: 163 ms em uma amostra local; detalhes em `tests/FinancasSaaS.IntegrationTests/Performance/LocalPerformanceResults.md`. O tempo de apresentação no navegador não foi medido.
- A inspeção visual autenticada, o tempo percebido no navegador e a avaliação moderada permanecem pendentes como acompanhamentos pós-encerramento, sem bloquear o fechamento da implementação. Os cenários funcionais e de segurança continuam cobertos pelos testes automatizados e não são dispensados.
- O rollout operacional foi aceito para adiamento a uma feature dedicada de testes/rollout, condicionada a autorização, inventário, conexão administrativa segura e pausa ou drenagem dos provisionamentos. Nenhum banco operacional foi migrado nesta execução.

## Ponto de parada: 05/11/2026

- A implementação automatizada está encerrada: T036, T043, T048 e T061 foram concluídas; build e 245 testes passaram. T056 foi aceito como adiamento para uma feature dedicada de testes/rollout; não houve migração operacional.
- Inspeção autenticada de foco, teclado, responsividade e tempo percebido no navegador, além do recrutamento para o protocolo moderado CS-008/CS-009, permanecem como acompanhamento pós-encerramento, sem bloquear a implementação. Não declarar CS-008/CS-009 ou a experiência CS-011 aprovados sem essas medições.
- O cliente local `cliente@financas.local` foi criado, concluiu o convite e possui tenant provisionado. Após uma falha de login causada pela ausência de `SecurityStamp`, o usuário local foi reparado sem alterar sua senha e a conclusão de cadastro foi corrigida. O usuário confirmou o login e o uso do formulário de lançamento após as correções.
- A aplicação local foi encerrada após a verificação de 05/11/2026. O banco de desenvolvimento é persistente: não apagar volumes nem alterar outros tenants. Não registrar credenciais neste guia.
- A suíte oficial atualizada passou com 245 testes; detalhes e os testes de regressão estão registrados em “Verificações retomadas”.
- CS-008 e CS-009 permanecem não avaliados: falta recrutar participantes e executar o protocolo moderado. O rollout não foi autorizado nem executado e será planejado em uma feature dedicada; o provisionamento do tenant local novo não equivale ao rollout de tenants existentes.
- Nesta primeira fase, questões legais e constitucionais devem ser anotadas para confirmação posterior, sem bloquear a retomada; isolamento de tenant e correção financeira continuam obrigatórios.

## Verificações retomadas: 05/11/2026

- `dotnet build FinancasSaaS.sln --no-restore` passou; `scripts/dev-up.sh --no-seed --test` passou com 245 testes e nenhum erro, usando PostgreSQL descartável.
- `tests/FinancasSaaS.Web.Tests/Finance/ManualTransactionFormValidationTests.cs` verifica que data prevista ausente produz validação antes de chamar o serviço e que o limite decimal é aceito sob cultura `pt-BR`.
- Os `EditForm` têm nomes únicos. Os limites de `Range` são interpretados invariavelmente, evitando exceção de conversão decimal sob `pt-BR`.
- O Web SDK estava incluindo saídas `bin/obj` como conteúdo, causando cópia recursiva. `FinancasSaaS.Web.csproj` agora remove esses itens; o inventário de `Content` lista zero itens dessas pastas e o build completo passou.
- Permanecem como acompanhamento pós-encerramento: inspeção autenticada de responsividade, teclado e foco; medição do tempo até os totais aparecerem no navegador; avaliação moderada com participantes; e uma feature dedicada de testes/rollout para a migration operacional. A aplicação real continua condicionada à autorização e aos pré-requisitos confirmados.

## Verificações de tela pós-encerramento (não bloqueantes)

- Inspecionar dashboard, relatórios e composição autenticados em 360 px, 768 px e 1280 px, cobrindo rolagem horizontal, foco visível e navegação por teclado.
- Medir o tempo até os totais aparecerem no navegador com 5.000 lançamentos. A medição SQL de 163 ms já foi registrada, mas não representa o tempo da interface; CS-011 permanece sem avaliação de experiência.
- Recrutar pelo menos cinco participantes e executar o protocolo moderado CS-008/CS-009. A pendência de recrutamento está registrada; não declarar esses critérios aprovados antes do estudo.
