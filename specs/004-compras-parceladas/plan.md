# Plano de Implementação: Compras parceladas

**Branch**: `feature/004-Compras-parceladas` | **Data**: 2026-10-29 | **Especificação**: [spec.md](spec.md)

**Entrada**: Feature para criar despesas mensais parceladas, acompanhar sua realização e preservar parcelas e auditoria ao editar ou cancelar compromissos futuros.

## Resumo

Estender o domínio financeiro por tenant já existente para que uma compra gere despesas previstas numeradas, com competência no mês do vencimento, soma exata em centavos e regra estável de vencimento para meses curtos. Reutilizar categorias, autenticação e autorização, conexão financeira derivada do usuário autenticado, persistência PostgreSQL, trilha de auditoria e interface Blazor/MudBlazor. As operações de realização, edição e cancelamento devem preservar parcelas protegidas, reprogramar vencimentos futuros a partir da primeira parcela elegível sem cruzar parcelas preservadas, impedir atualizações parciais e produzir histórico auditável. Não adicionar projeto, API financeira pública, cartão/fatura, recorrência indefinida ou relatórios.

## Contexto técnico

**Linguagem/Versão**: C# 14 / .NET 10 (`net10.0`).

**Dependências principais**: ASP.NET Core Blazor Web App com Interactive Server e Identity; EF Core 10.0.3 e Npgsql EF Core 10.0.0; MudBlazor já presente; xUnit e infraestrutura de testes existente.

**Armazenamento**: PostgreSQL. Os dados financeiros ficam em bases isoladas por tenant, acessadas por `TenantDbContext` e pela fábrica de contexto financeiro. Lançamentos, categorias e eventos de auditoria já são persistidos nesse limite.

**Testes**: xUnit nos projetos Domain, Application, IntegrationTests e Web.Tests. Cobrir cálculos e transições no domínio, casos de uso e feedback na aplicação, persistência/migrations/isolamento PostgreSQL na integração e autorização/UI na Web.

**Plataforma alvo**: Aplicação web ASP.NET Core no servidor, acessada em navegadores desktop e móveis.

**Tipo de projeto**: Aplicação web Blazor existente, com projetos Domain, Application, Finance.Infrastructure, Platform.Infrastructure e Web.

**Metas de desempenho**: Não foi definido SLO específico. Listagem e mutações devem permanecer responsivas no fluxo individual já praticado pelo produto, sem introduzir metas de volume não especificadas.

**Restrições**:

- A identidade autenticada e o resolvedor de tenant existente determinam a conexão autorizada; tenant ou conexão enviados pela interface nunca são autoridade.
- Reutilizar lançamentos, categorias, Identity, componentes visuais, feedback operacional e auditoria existentes. Não criar API pública ou novo mecanismo de autenticação.
- Quantias em BRL devem manter precisão exata de centavos; nunca usar ponto flutuante nem arredondar silenciosamente.
- A quantidade de parcelas deve ser validada no servidor entre 2 e 1.200 para limitar o tamanho de cada operação em lote.
- A criação precisa ser atômica: ou a compra e todas as parcelas são registradas, ou nenhuma delas.
- A edição e o cancelamento devem preservar parcelas realizadas, canceladas e não futuras; somente parcelas previstas com vencimento posterior ao dia da operação são elegíveis.
- Alterações financeiras devem preservar histórico imutável, motivo quando exigido, ator, instante e parcelas afetadas.
- Retenção, exportação, exclusão e backup seguem as políticas técnicas existentes para dados financeiros por tenant; a feature não deve introduzir comportamento de ciclo de vida próprio.
- Mudanças no esquema financeiro precisam ser aplicadas tanto a novos tenants quanto aos tenants já prontos antes da liberação da funcionalidade, usando o processo de rollout de migrations por tenant existente.
- Não introduzir microserviços, mensageria, dependências ou abstrações sem necessidade demonstrada.

**Escala/escopo**: Compras parceladas por clientes autenticados, incluindo criação, consulta, realização manual, edição e cancelamento das parcelas futuras previstas. Fora de escopo: cartão/fatura, recorrência indefinida, dashboard e relatórios.

## Verificação da Constituição

*Gate: aprovado antes da Fase 0; reavaliado após o design da Fase 1.*

- **I. Isolamento entre tenants e segurança — PASS condicionado aos testes planejados**: todas as operações resolvem o tenant pelo usuário autenticado. Testes com dois bancos de tenant devem demonstrar que consulta, mutação, categoria e histórico não atravessam o limite, inclusive em acesso direto por identificador.
- **II. Integridade financeira e auditabilidade — PASS**: valores representam centavos exatamente; cada parcela e cada recálculo mantêm soma exata. Operações em grupo são atômicas e alterações/cancelamentos preservam os dados e eventos anteriores com motivo e ator.
- **III. Domínio pragmático e arquitetura limpa — PASS**: regra de cálculo de datas, centavos e elegibilidade permanece independente da UI e da persistência; os componentes existentes de aplicação, infraestrutura e Web adaptam as regras sem criar um projeto ou serviço paralelo.
- **IV. Testes automatizados — PASS condicionado aos gates planejados**: cobertura de limites de mês curto, divisão de centavos, transições, edição/cancelamento após realização, atomicidade, autorização e isolamento real em PostgreSQL, compatibilidade de migrations e feedback da interface.
- **V. Evolução rastreável e incremental — PASS**: a especificação e as mensagens de domínio seguem em português do Brasil; alterações financeiras são auditáveis, migrations acompanham a expansão do modelo e o roteiro de validação documenta o rollout e a regressão.
- **Gate de complexidade — PASS**: a extensão de Domain, Application, Finance.Infrastructure, Web e respectivos testes é justificada por novas invariantes e fluxos financeiros. Não há novo projeto, API pública ou infraestrutura externa.
- **Gate de rollout — PASS condicionado à operação existente**: migrar as bases de tenants já prontos por meio do runner financeiro atual antes de liberar novas rotas; manter os bloqueios operacionais e as verificações de inventário/auditoria existentes até confirmar que os tenants elegíveis estão atualizados.

## Estrutura do projeto

### Documentação desta feature

```text
specs/004-compras-parceladas/
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
├── FinancasSaaS.Domain/Finance/              # invariantes e cálculos independentes
├── FinancasSaaS.Application/
│   ├── Abstractions/                          # contratos de persistência financeira
│   └── Finance/                               # fluxos de criação e manutenção
├── FinancasSaaS.Finance.Infrastructure/
│   ├── Persistence/                           # dados e migrations por tenant
│   └── ManualTransactions/                    # integração com lançamentos financeiros
└── FinancasSaaS.Web/
    └── Components/Pages/Customer/Finance/    # jornadas autenticadas de finanças

tests/
├── FinancasSaaS.Domain.Tests/                 # calendário, centavos e estados
├── FinancasSaaS.Application.Tests/            # validação, logs e casos de uso
├── FinancasSaaS.IntegrationTests/             # PostgreSQL, atomicidade e tenants
└── FinancasSaaS.Web.Tests/                    # autorização, interface e feedback
```

**Decisão de estrutura**: Estender os projetos e áreas existentes. Uma nova área financeira de interface deve seguir a navegação e os controles de acesso atuais; a superfície desta feature é a UI interna, sem endpoint financeiro público. Os detalhes do contrato de tela e ações ficam em [contracts/ui.md](contracts/ui.md).

## Organização das fases

### Fase 0 — Pesquisa e decisões

Decisões e padrões existentes foram registrados em [research.md](research.md). Não há dependência tecnológica em aberto. As escolhas de domínio que afetam o comportamento foram confirmadas na especificação: ajuste de mês curto preservando o dia original, distribuição de centavos do menor número para o maior e, após parcela realizada, preservação das parcelas não elegíveis com distribuição do saldo apenas nas futuras previstas elegíveis.

### Fase 1 — Design e contratos

- Documentar entidades, campos conceituais, estados, relações e regras de elegibilidade em [data-model.md](data-model.md).
- Descrever rotas e interações da UI, autorização, validação e feedback em [contracts/ui.md](contracts/ui.md); não há API externa nesta feature.
- Registrar pré-requisitos, validações automatizadas, cenários ponta a ponta e rollout em [quickstart.md](quickstart.md).
- Reavaliar os gates da constituição: PASS, condicionados aos testes de domínio, isolamento, persistência e rollout descritos.

## Complexidade

Não há violações da constituição que precisem de exceção ou justificativa adicional.
