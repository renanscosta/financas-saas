# Rollout das migrations financeiras por tenant

Este procedimento atualiza tenants já provisionados antes da liberação das rotas financeiras. Execute-o em uma janela de manutenção e use os controles operacionais do ambiente para suspender novas criações e provisionamentos.

## Preflight obrigatório

1. Bloqueie novas criações e provisionamentos pelos controles operacionais do ambiente.
2. Drene os provisionamentos já iniciados e confirme que não há execução ativa.
3. Se a pausa e a drenagem não puderem ser confirmadas, interrompa o rollout sem executar os comandos abaixo.
4. Configure as mesmas conexões da plataforma e dos bancos de tenant usadas pela versão atual. Nunca copie connection strings para o manifesto ou para o registro operacional.

## Inventário e aplicação

Gere e preserve o manifesto em local operacional protegido:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj -- --create-finance-tenant-inventory /secure/finance-tenant-inventory.json
```

O arquivo contém apenas `capturedAtUtc` e `tenantIds`. Confira e preserve uma cópia imutável do manifesto da janela.

Antes de migrar, gere uma auditoria somente leitura usando o mesmo manifesto e um caminho de saída ainda inexistente:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj -- --audit-finance-tenants --tenant-inventory /secure/finance-tenant-inventory.json --output /secure/finance-tenant-audit-before.json
```

O relatório informa o estado de cada migration financeira e das categorias padrão (`active`, `inactive`, `missing` ou `conflict`), sem connection strings. Quando a migration inicial já estiver aplicada e uma categoria estiver ausente/renomeada, exija evidência operacional de que a ausência foi intencional; se não for possível distinguir exclusão intencional de seed faltante, mantenha o rollout bloqueado e peça decisão explícita. Não execute backfill cego. Conflitos entre nomes equivalentes sempre precisam de resolução manual.

Execute as migrations usando o mesmo manifesto imutável:

```bash
dotnet run --project src/FinancasSaaS.Web/FinancasSaaS.Web.csproj -- --migrate-finance-tenants --tenant-inventory /secure/finance-tenant-inventory.json
```

O processo compara exatamente o conjunto de tenants prontos com o manifesto antes e depois. A migration inicial cria categorias padrão ausentes e reativa as existentes que estejam inativas; a migration forward reativa somente as categorias padrão ainda presentes e não recria categorias ausentes/renomeadas. O provisionador aplica a reativação durante a preparação inicial. O comando não altera estado de provisionamento e informa os IDs processados sem connection strings. Nenhuma categoria é mesclada ou removida automaticamente.

## Critérios de liberação

- Código de saída zero e confirmação de todos os IDs do manifesto.
- Conjunto de tenants prontos permanece idêntico ao inventário.
- Todas as migrations financeiras concluíram e uma nova auditoria pós-migration confirma o estado aplicado em cada tenant.
- Nenhum conflito permanece; categorias padrão ausentes estão explicadas por exclusão/renomeação intencional documentada.

Em qualquer falha ou divergência, mantenha bloqueadas a criação/provisão e a liberação das novas rotas. Investigue a causa sem expor connection strings, resolva conflitos manualmente e reinicie o procedimento com um manifesto revisto conforme a situação. Não marque manualmente tenants como prontos.

Após todos os critérios passarem, ative a versão que contém as rotas financeiras e só então retome criação e provisionamento de tenants.
