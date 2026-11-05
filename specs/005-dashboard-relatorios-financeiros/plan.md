# Plano de Implementação: Dashboard e relatórios financeiros

**Branch**: `005-dashboard-relatorios-financeiros` | **Data**: 2026-10-30 | **Especificação**: [spec.md](spec.md)

**Entrada**: Feature para o cliente acompanhar sua situação financeira mensal e conferir, por totais conciliados com listas, de quais lançamentos vêm os valores, por competência ou por pagamento/recebimento.

## Resumo

Adicionar uma camada somente de leitura sobre os lançamentos (avulsos e parcelas), categorias e isolamento por tenant já existentes. O domínio ganha a regra de data de referência por critério, a validação de período (mês e intervalo de até 24 meses) e o cálculo de saldos em centavos exatos. A aplicação expõe um serviço de leitura (resumo mensal, despesas por mês e categoria, composição de totais e preferência de critério). A infraestrutura financeira agrega no banco do tenant usando o mesmo predicado para totais e listas; a composição concilia com seu próprio total e informa diferenças em relação ao total anterior. A preferência do cliente é guardada em uma nova estrutura no banco do tenant, com migration aplicada a novos tenants e aos já prontos. A Web recebe as páginas `/cliente/dashboard` e `/cliente/relatorios` e dois destinos na navegação do cliente. Não há alteração de regras das Features 1 a 4, API pública, biblioteca de gráficos ou dados fictícios.

## Contexto técnico

**Linguagem/Versão**: C# 14 / .NET 10 (`net10.0`).

**Dependências principais**: ASP.NET Core Blazor Web App com Interactive Server e Identity; EF Core 10.0.3 e Npgsql EF Core 10.0.0; MudBlazor já presente; xUnit e infraestrutura de testes existentes. Nenhuma dependência nova.

**Armazenamento**: PostgreSQL por tenant (`TenantDbContext`, acessado por `ITenantFinanceContextFactory`). Leituras usam os lançamentos existentes; uma nova estrutura guarda a preferência de critério individual, com um registro por usuário em cada tenant, e um novo índice cobre consultas por competência e estado.

**Testes**: xUnit em Domain, Application, IntegrationTests (PostgreSQL com dois tenants) e Web.Tests. Cobrir regra de data, período, saldos e conciliação no domínio e na aplicação; agregações, oráculo SQL × domínio, isolamento e migration na integração; autorização, estados de UI e formatação na Web.

**Plataforma alvo**: Aplicação web ASP.NET Core, navegadores desktop e móveis.

**Tipo de projeto**: Aplicação web Blazor existente.

**Metas de desempenho**: Totais do dashboard em até 3 segundos para um mês com até 5.000 lançamentos (CS-011), medidos em uso normal com dados semeados; o critério permanece pendente até ser verificado. Agregações no banco, sem carregar todos os lançamentos em memória.

**Restrições**:

- O tenant vem da identidade autenticada; nenhum parâmetro de tela determina tenant ou conexão.
- A preferência pertence ao usuário autenticado e é independente da preferência de outros usuários, inclusive no mesmo tenant; a interface não escolhe o usuário titular.
- Valores em decimal exato; sem ponto flutuante nem arredondamento; soma que não possa ser representada falha explicitamente.
- Somente leitura sobre lançamentos, parcelas e categorias; nenhuma mutação financeira nova.
- Cancelados fora de totais, agrupamentos e listas; leituras de categoria não filtram por atividade.
- Totais e listas derivam do mesmo predicado; o total exibido na lista é calculado de seus itens.
- Retenção, exportação, exclusão e backup da preferência seguem as políticas existentes dos dados do tenant.
- Mudança de esquema aplicada a tenants novos e existentes pelo processo de migrations por tenant antes de liberar as rotas.
- Sem microserviços, mensageria, biblioteca de gráficos ou abstrações sem necessidade demonstrada.

**Escala/escopo**: Cliente pessoa física autenticado, período mensal no dashboard, intervalo de até 24 meses nos relatórios. Fora de escopo: exportação, integração bancária, cartões e faturas, cobrança, previsões inteligentes, gráficos fictícios, mutações financeiras.

## Verificação da Constituição

**Status nesta fase: suspensa por decisão do responsável.** A conformidade com a constituição não é avaliada nem bloqueia o avanço; os itens abaixo permanecem apenas como anotação para uma revisão posterior.

*Anotação: avaliação preliminar feita antes da suspensão.*

- **I. Isolamento entre tenants e segurança — PASS condicionado aos testes planejados**: todas as leituras e a preferência usam o banco do tenant da identidade autenticada; categoria por identificador é verificada nesse banco com mensagem neutra. Testes com dois bancos de tenant demonstram que totais, listas, categorias e preferência não atravessam o limite, inclusive com parâmetros de URL manipulados.
- **II. Integridade financeira e auditabilidade — PASS**: valores decimais exatos, composição conciliada com seu próprio total, cancelados excluídos e falha explícita em vez de valor inventado. A feature não altera dados financeiros; a alteração de preferência gera registro estruturado com ator e instante.
- **III. Domínio pragmático e arquitetura limpa — PASS**: regra de data, período e saldos ficam no domínio, independentes de UI e EF; infraestrutura traduz o mesmo predicado para SQL; Web só apresenta. Nenhum projeto ou serviço novo.
- **IV. Testes automatizados — PASS condicionado aos gates planejados**: regras de domínio, serviço, oráculo SQL × domínio, conciliação, mudança de estado de parcela, cancelamento, mês vazio, falha, limites de período, dois tenants e UI.
- **V. Evolução rastreável e incremental — PASS**: especificação e mensagens em português do Brasil; três dúvidas de domínio resolvidas na spec; uma decisão de plano mantida provisoriamente (granularidade da competência) registrada em [research.md](research.md); migration e rollout documentados.
- **Gate de complexidade — PASS**: extensão de Domain, Application, Finance.Infrastructure, Web e testes; sem nova infraestrutura.
- **Gate de rollout — PASS condicionado à operação existente**: migrar os tenants prontos com o runner antes de liberar as rotas; bloquear a UI para tenants não migrados.

## Estrutura do projeto

### Documentação desta feature

```text
specs/005-dashboard-relatorios-financeiros/
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
src/
├── FinancasSaaS.Domain/Finance/              # critério de data, período, saldos
├── FinancasSaaS.Application/
│   ├── Abstractions/                          # contratos de leitura e preferência
│   └── Finance/Reports/                       # serviço de leitura e resultados
├── FinancasSaaS.Finance.Infrastructure/
│   ├── Reports/                               # consultas agregadas e preferência
│   └── Persistence/                           # entidade de preferência, índice, migration
└── FinancasSaaS.Web/
    ├── Components/Pages/Customer/Finance/    # dashboard e relatórios
    ├── Components/Layout/CustomerLayout.razor # novos destinos de navegação
    └── Program.cs                             # registro do serviço de leitura

tests/
├── FinancasSaaS.Domain.Tests/                 # regra de data, período, saldos
├── FinancasSaaS.Application.Tests/            # serviço, validação, logs
├── FinancasSaaS.IntegrationTests/             # PostgreSQL, oráculo, conciliação, tenants, migration
└── FinancasSaaS.Web.Tests/                    # autorização, estados de UI, formatação
```

**Decisão de estrutura**: Estender os projetos e áreas existentes. A superfície é a UI interna; os detalhes de rotas, parâmetros, estados e acessibilidade estão em [contracts/ui.md](contracts/ui.md).

## Organização das fases

A História 1 é um incremento parcial; a entrega completa inclui composição, isolamento e escolha de critério, além das demais capacidades e verificações obrigatórias da especificação.

### Fase 0 — Pesquisa e decisões

Decisões e padrões existentes estão em [research.md](research.md): regra única de data de referência, agregação exata no banco, composição entregue por inteiro com total próprio, preferência no banco do tenant, ausência de gráficos e desempenho. Não há tecnologia em aberto. Decisão mantida provisoriamente pelo responsável: granularidade mensal da competência nos relatórios (impacta só a regra de filtro e seus testes); revisão posterior opcional, conforme [research.md](research.md).

### Fase 1 — Design e contratos

- Entidades, regras de período, saldos, composição e transições em [data-model.md](data-model.md).
- Rotas, parâmetros, jornadas, estados e acessibilidade em [contracts/ui.md](contracts/ui.md); não há API externa.
- Pré-requisitos, comandos, cenários ponta a ponta e rollout em [quickstart.md](quickstart.md).
- Reavaliação dos gates da constituição: PASS, condicionados aos testes e ao rollout descritos.

## Complexidade

Não há violações da constituição que exijam justificativa adicional.
