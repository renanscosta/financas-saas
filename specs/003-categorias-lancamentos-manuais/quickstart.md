# Guia de validação: Categorias e lançamentos manuais

**Feature**: [spec.md](spec.md)

## Pré-requisitos

- SDK .NET 10 e dependências descritas no README.
- PostgreSQL local disponível e configuração da aplicação compatível com o ambiente de desenvolvimento.
- Cliente de teste com usuário Identity ativo, tenant provisionado e convite concluído.
- Dois tenants independentes e clientes de teste para cenários de isolamento.
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

Testes de persistência e de ponta a ponta que dependem de PostgreSQL exigem o banco configurado conforme o README. Executar a suíte completa após mudanças de schema, autorização ou contratos compartilhados:

```bash
dotnet test FinancasSaaS.sln
```

Resultado esperado: build e testes passam; migrations financeiras aplicam a ambos os bancos de tenant limpos e existentes; os testes preexistentes de autenticação, provisionamento e UI continuam passando.

## Executar para inspeção visual

Iniciar PostgreSQL e serviços locais conforme [quickstart da Feature 1](../001-cadastro-provisionamento-cliente/quickstart.md). Executar:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj
```

Entrar como cliente autenticado e abrir `/cliente/lancamentos` e `/cliente/categorias`.

## Cenários de aceitação ponta a ponta

1. **Categorias iniciais**: na primeira disponibilização, preparar tenant sem categorias e confirmar presença de Lanches, Mercado e Pessoal; testar também nomes equivalentes preexistentes. Renomear ou excluir categoria padrão sem associação e repetir um rollout posterior; confirmar que a escolha do cliente permanece e que nenhuma categoria padrão reaparece.
2. **Equivalência dos nomes**: no mesmo tenant, tentar categorias repetidas variando caixa e espaços nas extremidades; confirmar recusa. Repetir o nome em outro tenant; confirmar isolamento.
3. **Categorias**: criar/renomear categoria; reutilizá-la em entrada e despesa; tentar excluir categoria usada por lançamento e última categoria; confirmar que ambas as exclusões são recusadas.
4. **Entradas e despesas**: criar uma entrada e uma despesa com os campos obrigatórios e verificar que começam previstas; tentar campos obrigatórios vazios, valor zero/negativo, acima de R$ 9.999.999.999.999.999,99 e com mais de duas casas decimais e confirmar recusa sem mutação ou arredondamento. Confirmar também que o máximo é aceito.
5. **Realização**: confirmar a entrada prevista com data efetiva, localizar como realizada e conferir seu filtro pela data efetiva.
6. **Correção auditável**: corrigir campos previstos; corrigir valor e data prevista de realizado; informar motivo; confirmar estado/data efetiva preservados e histórico com valores anteriores/novos, instante, responsável e motivo. Tentar salvar sem motivo, com valor acima de R$ 9.999.999.999.999.999,99 ou com mais de duas casas decimais e confirmar recusa sem mutação.
7. **Cancelamento**: cancelar previsto e realizado com motivos diferentes; confirmar histórico consultável, estado anterior auditável e critério de data do filtro. Esta feature não calcula nem exibe totais; agregações futuras devem excluir lançamentos cancelados.
8. **Filtros combinados**: criar lançamentos com tipo, categoria, datas e estados diferentes; combinar filtros e verificar correspondência exata pelo período conforme [modelo de dados](data-model.md).
9. **Concorrência**: abrir o mesmo lançamento em duas sessões; em cada ação de correção, realização e cancelamento, tentar salvar uma versão obsoleta após a outra sessão alterar o registro. Confirmar recusa atômica, preservação da versão recente, aviso e opção de revisar/reaplicar.
10. **Isolamento entre tenants**: com usuário A, tentar abrir ou mutar IDs de categoria, lançamento e histórico do tenant B e tentar submeter categoria do tenant B. Confirmar que nenhum dado é revelado e nenhuma alteração persiste. Repetir com URL direta e operações server-side.
11. **Autorização**: repetir acesso como visitante, cliente desativado e tenant ainda não pronto; confirmar bloqueio antes de carregar/mutar dados financeiros.
12. **Responsividade e teclado**: repetir fluxos essenciais em 360 × 800, 768 × 1024 e 1280 × 800; verificar ausência de rolagem horizontal, rótulos, erros, foco, mensagens e controles acessíveis por teclado.

## Evidências mínimas

- Resultado de build e testes de domínio, Application, integração e Web.
- Testes de tenant confirmando acesso permitido no banco próprio e recusa de IDs de outro tenant.
- Evidência de aplicação e repetição das migrations/provisionamento com categorias sem duplicatas.
- Evidência de auditoria e recusa de atualização obsoleta.
- Captura de logs estruturados de criação/realização/correção/cancelamento, manutenção de categorias, conflitos, falhas e acessos negados; confirmar campos de correlação e ausência de valores, descrições, tokens, credenciais e connection strings.
- Capturas ou revisão de interface nos tamanhos desktop, tablet e celular.

## Rollout de tenants existentes

Seguir o [runbook de rollout](../../database/finance/tenant-rollout.md) antes de implantar ou expor as novas rotas. A pausa e drenagem de provisionamentos são controles externos à aplicação; não iniciar o rollout se não for possível confirmá-las. Os comandos implementados são:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj -- --create-finance-tenant-inventory /secure/finance-tenant-inventory.json
```

O manifesto contém somente timestamp UTC e IDs de tenants prontos na captura. Preservá-lo como artefato imutável da implantação. Antes de alterar bancos, auditar o histórico e as categorias padrão:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj -- --audit-finance-tenants --tenant-inventory /secure/finance-tenant-inventory.json --output /secure/finance-tenant-audit.json
```

O relatório somente leitura informa, por tenant, o estado das migrations financeiras e de Lanches, Mercado e Pessoal (`active`, `inactive`, `missing` ou `conflict`), sem connection strings. Se a migration inicial já estiver aplicada e uma categoria padrão estiver ausente/renomeada, compare com evidência operacional de exclusão/renomeação intencional. Sem evidência que esclareça o estado, interrompa o rollout e solicite decisão explícita; não recrie categorias por backfill cego. Conflitos (`conflict`) sempre exigem resolução manual.

Execute as migrations passando o mesmo manifesto imutável:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj -- --migrate-finance-tenants --tenant-inventory /secure/finance-tenant-inventory.json
```

O comando de migration compara exatamente o conjunto de tenants prontos antes e depois. A migration inicial cria uma única vez categorias padrão ausentes e reativa as existentes que estejam inativas; a migration forward reativa apenas categorias padrão ainda presentes, sem inserir as ausentes. O provisionador mantém a regra de reativação durante a preparação inicial de cada tenant. O comando não altera o estado de provisionamento e falha sem revelar connection strings.

Após migrar, gere novo relatório de auditoria em um caminho ainda inexistente. Confirme que todas as migrations financeiras aparecem como aplicadas, nenhum estado é `conflict`, e que cada categoria padrão `missing` é explicada por exclusão/renomeação intencional comprovada. Mantenha bloqueadas criação/provisionamento e liberação das novas rotas enquanto qualquer tenant estiver inacessível, em estado conflitante ou sem explicação operacional.

## Evidência de desempenho da listagem (REV-007)

Medição executada em PostgreSQL 17 local (`docker compose`) com base sintética dedicada:

- Base: `financas_perf_rev_003`
- Cardinalidade: 200.000 lançamentos e 10 categorias
- Distribuição de estado: 60% `Planned`, 20% `Realized`, 20% `Cancelled`
- Janela de filtro testada: `2025-04-01` a `2025-06-30` (inclusive)
- Índices em uso: `IX_ManualTransactions_Status_ExpectedDate`, `IX_ManualTransactions_Status_EffectiveDate`, `IX_ManualTransactions_CategoryId`

Consultas medidas (SQL equivalente ao `ManualTransactionStore.ListAsync`):

1. **Filtro de período sem restrições opcionais**  
   Plano: `Parallel Bitmap Heap Scan` + `Hash Join` + `Gather Merge`  
   Resultado: 50.142 linhas  
   `Execution Time`: **110,197 ms**  
   `Buffers`: **shared hit=1.446**

2. **Filtro combinado (período + tipo + categoria + estado)**  
   Plano: `BitmapAnd` (`CategoryId` + índices de status/data) + `Nested Loop`  
   Resultado: 5.206 linhas  
   `Execution Time`: **19,468 ms**  
   `Buffers`: **shared hit=1.299**

Decisão registrada: **não introduzir paginação nem novos índices nesta revisão**. Com a cardinalidade medida, os índices atuais sustentam o cenário sem regressão visível; manter monitoramento em produção e reavaliar ao surgir evidência de volume/latência superior ao perfil medido.

## Validação executada durante a implementação

- `dotnet build FinancasSaaS.sln --no-restore`: passou, sem avisos.
- `dotnet ef migrations script --idempotent ...`: gerou SQL idempotente contendo as migrations financeiras, incluindo a reativação forward de categorias padrão existentes.
- `dotnet test FinancasSaaS.sln --no-build --no-restore`, com PostgreSQL local e Mailpit ativo: 115 aprovados (23 Domain, 21 Application, 37 Integration e 34 Web), sem falhas.
- Revisão responsiva com o CSS compilado e uma maquete do layout em 360, 768 e 1280 px: sem rolagem horizontal e com foco visível via teclado. Esta revisão não substitui a inspeção dos fluxos numa sessão autenticada real.
- Os testes financeiros e de rollout foram executados contra PostgreSQL real; validam bancos de tenants isolados, migrations, reativação inicial, preservação de categorias renomeadas/excluídas em migrations posteriores, auditoria, idempotência, retentativa segura e comandos operacionais.
- A auditoria dos tenants reais de rollout (T077) e o estudo moderado de usabilidade (T078) permanecem deliberadamente pendentes para a validação posterior do MVP; esta evidência local não os substitui.
