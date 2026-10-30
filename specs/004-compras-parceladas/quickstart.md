# Guia de validação: Compras parceladas

**Feature**: [spec.md](spec.md)

## Pré-requisitos

- SDK .NET 10 e dependências da solução.
- PostgreSQL local disponível conforme o README e a configuração de desenvolvimento.
- Cliente autenticado com tenant financeiro pronto e categoria ativa.
- Segundo tenant independente e usuário de teste para validar isolamento.
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

Os testes de persistência e isolamento usam PostgreSQL descartável e isolamento por tenant. Para executar a suíte do MVP sem testes de performance:

```bash
scripts/dev-up.sh --no-seed --test
```

O comando inicia um PostgreSQL isolado sem volume, aplica as migrations da plataforma, injeta a connection string dinâmica nos testes e remove o container ao final. Os testes financeiros compartilham dois bancos de tenant e executam em série; pode existir um terceiro banco temporário durante cenários de rollout. Testes de performance ficam fora da validação do MVP.

Execute os testes de integração somente pelo comando descartável ou contra bancos PostgreSQL isolados configurados por `PLATFORM_CONNECTION_STRING`. Não aponte os testes que limpam tenants para bancos de produção. Se não houver conexão de teste disponível, registre a execução como pendente em `tasks.md` e não trate a compilação dos testes como evidência de integração validada.

## Executar para inspeção

Iniciar PostgreSQL e serviços locais conforme o README e o [guia de validação da Feature 1](../001-cadastro-provisionamento-cliente/quickstart.md). Executar:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj
```

Entrar como cliente autenticado, abrir `/cliente/compras-parceladas` e conferir os fluxos descritos em [contracts/ui.md](contracts/ui.md).

## Cenários de aceitação ponta a ponta

1. **Criação e centavos**: criar uma compra de R$ 100,00 em três parcelas; conferir valores de R$ 33,34, R$ 33,33 e R$ 33,33, soma exata, numeração e estado previsto. Tentar valores inválidos, quantidade menor que duas ou maior que 1.200 e confirmar recusa sem registro parcial.
2. **Meses curtos**: criar séries iniciadas em 29, 30 e 31 de janeiro, abrangendo ano bissexto e não bissexto; confirmar ajuste para o último dia do mês e retomada do dia original nos meses seguintes. Validar que cada competência corresponde ao mês de vencimento.
3. **Realização**: confirmar manualmente uma parcela prevista com data efetiva; verificar estado, data e auditoria, garantindo que nenhuma parcela vizinha mudou.
4. **Edição após realização**: realizar uma parcela e editar descrição, categoria, total e vencimentos futuros. Para a agenda, fornecer nova data futura para a primeira parcela prevista elegível e conferir que as demais elegíveis são recalculadas mensalmente pela diferença entre seus números. Confirmar que a parcela realizada e seu histórico não mudaram, que a quantidade/numeração original foram mantidas e que o saldo futuro soma corretamente. Testar data-base que cruze vencimento protegido e total inferior ao montante preservado; ambos devem ser recusados integralmente.
5. **Datas não elegíveis**: tentar editar ou cancelar parcelas vencidas, com vencimento no dia da operação, realizadas ou já canceladas; confirmar que permanecem inalteradas. Testar parcelamento sem parcelas futuras elegíveis e conferir feedback explícito.
6. **Cancelamento**: cancelar compra com motivo; verificar que somente parcelas futuras previstas são canceladas e que parcelas pagas, canceladas anteriormente e não futuras continuam consultáveis com história preservada. Repetir sem motivo e confirmar recusa.
7. **Auditoria**: conferir que criação, realização, edição e cancelamento registram ator, instante, parcelas e dados afetados, com motivo obrigatório nas operações especificadas. Confirmar que eventos anteriores permanecem consultáveis.
8. **Isolamento**: com usuário A, tentar listar, consultar, realizar, editar, cancelar e consultar histórico de IDs do tenant B e usar sua categoria. Confirmar que nenhuma informação é revelada e nada é alterado; repetir no nível de persistência com dois bancos de teste.
9. **Concorrência e atomicidade**: usar duas sessões para alterar o mesmo parcelamento; salvar uma versão concorrente e tentar reaplicar a versão antiga. Confirmar conflito sem sobrescrever dados atuais ou deixar somente algumas parcelas atualizadas.
10. **UI e acessibilidade**: conferir validação, estados carregando/sucesso/erro, prevenção de submissão duplicada, navegação por teclado, foco e layout responsivo em celular, tablet e desktop.
11. **Histórico concorrente**: iniciar a consulta do histórico de uma compra, abrir outra compra antes de a primeira resposta chegar e concluir as respostas em ordens diferentes; confirmar que somente o histórico da compra atualmente aberta aparece e que uma consulta antiga não encerra o carregamento atual.

## Rollout de tenants existentes

Antes de liberar a rota, seguir o processo de rollout descrito em [quickstart da Feature 3](../003-categorias-lancamentos-manuais/quickstart.md) e no runbook financeiro existente. Confirmar a pausa operacional de provisionamentos, criar e auditar o inventário de tenants, aplicar as migrations financeiras com o runner existente, verificar todos os tenants elegíveis e só então publicar/liberar a versão com a feature. Os tenants provisionados posteriormente recebem o esquema atualizado pelo provisionador. Qualquer divergência ou falha deve interromper o rollout e bloquear a UI para os tenants ainda não atualizados.

## Evidências mínimas

- Resultados de build e testes Domain, Application, integração PostgreSQL e Web.
- Casos de vencimentos de dias 29, 30 e 31 em fevereiro bissexto e comum.
- Soma de centavos exata em criação e edição após uma parcela realizada.
- Testes de isolamento entre dois tenants para compras, parcelas, categorias e auditoria.
- Evidência de atomicidade, motivo obrigatório e preservação do histórico após edição/cancelamento.
- Evidência de migration aplicada em tenants existentes e novos sem falha parcial.

## Protocolo de usabilidade CS-005

- Recrutar pelo menos 10 clientes que representem o público atual dos lançamentos financeiros, sem experiência prévia com esta tela. Usar ambiente e dados descartáveis, nunca dados financeiros reais.
- Entregar a cada participante o mesmo cenário e os dados necessários para registrar uma compra parcelada válida. Não demonstrar a tela, oferecer dicas nem permitir uma segunda tentativa.
- Contar como sucesso somente o primeiro envio aceito e persistido com os dados do cenário. Erro de validação, falha técnica, ajuda do facilitador ou necessidade de novo envio contam como insucesso.
- Registrar apenas quantidade de participantes, sucessos, insucessos e problemas observados, sem nomes, contatos ou outros dados pessoais.
- Calcular `sucessos / participantes * 100`; CS-005 passa com pelo menos 90%. Com a amostra mínima de 10 participantes, isso exige pelo menos 9 sucessos.
- Guardar a evidência agregada em registro de teste/release. A avaliação não faz parte da execução operacional solicitada nesta etapa e CS-005 não deve ser marcado como validado sem essa sessão.

## Protocolo diagnóstico de desempenho

- Executar somente após a validação de integração PostgreSQL em ambiente isolado, com dados sintéticos: 100 compras de até 1.200 parcelas cada (até 120.000 parcelas), além de dados necessários para as relações da consulta.
- Medir a consulta de listagem e a conversão para o modelo de tela, registrando latência, alocação/memória observável e plano de execução PostgreSQL (`EXPLAIN (ANALYZE, BUFFERS)`); repetir as medições e registrar ambiente, cardinalidade e resultados.
- Comparar o comportamento da consulta na cardinalidade total e inspecionar filtros, ordenação, leituras e buffers para recomendar paginação e/ou índices somente quando os dados indicarem necessidade.
- Esta é uma investigação diagnóstica, não um gate de release: o plano não define SLO ou limite absoluto de latência/memória. Registrar a recomendação e sua evidência em relatório de teste, sem executar benchmark ou plano de análise em produção.
