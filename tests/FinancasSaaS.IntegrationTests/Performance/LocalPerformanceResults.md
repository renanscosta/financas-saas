# Resultados de desempenho local

**Data**: 2026-09-29  
**Ambiente**: Linux, .NET 10.0.103, aplicação local em `http://localhost:5080`  
**Amostras**: 30 requisições por endpoint, sem indisponibilidade de dependências

| Endpoint | p95 HTTP |
|---|---:|
| `/` | 9,221 ms |
| `/health` | 47,769 ms |

Os dois endpoints ficaram abaixo da meta local de 2 segundos. A medição representa o tempo
de resposta HTTP das rotas públicas; não substitui uma medição de evento interativo dentro do
circuito Blazor. A suíte completa executada no mesmo ambiente terminou com 30 testes aprovados.

O quickstart também foi validado: Compose, build, migrations via testes de integração,
provisionamento idempotente, convites, conclusão de cadastro, desativação, isolamento e rota
de login responderam conforme os cenários automatizados e o smoke test HTTP.

## Consulta de status de provisionamento

**Amostras**: 30 consultas EF Core com `AsNoTracking`, filtro por `TenantId`, projeção dos
campos de status, volume de 250 tenants e 8 consultas concorrentes.  
**p95 observado**: **3,326 ms**.  
**SQL**: `SELECT p."TenantId", p."Status", p."Attempt", p."LastAttemptAt" FROM "Provisionings" AS p WHERE p."TenantId" = @tenantId`.

O resultado ficou abaixo da meta de 2 segundos por consulta. O plano observado foi `Seq Scan`
com 108 linhas removidas pelo filtro, 2 buffers compartilhados e 0,019 ms de execução SQL.
Como o volume local é pequeno, não foi aplicada otimização prematura; o índice único existente
em `Provisionings.TenantId` permanece disponível para volumes maiores.