# Plano de Implementação: Categorias e lançamentos manuais

**Branch**: `003-categorias-lancamentos-manuais` | **Data**: 2026-10-27 | **Especificação**: [spec.md](spec.md)

**Entrada**: Feature para clientes pessoa física organizarem categorias e registrarem, consultarem e acompanharem entradas e despesas avulsas.

## Resumo

Estender o domínio financeiro já hospedado por tenant, que hoje contém categorias na base de dados isolada de cada cliente, para suportar categorias únicas e lançamentos avulsos em BRL com estado previsto/realizado/cancelado, correções e cancelamentos auditáveis e concorrência otimista nas operações de correção, realização e cancelamento de lançamentos. A interface será adicionada à aplicação Blazor Web existente, reutilizando autenticação Identity, resolução confiável da conexão do tenant, autorização no servidor e a fundação visual da Feature 2; não serão criados projetos de aplicação, uma autenticação paralela ou endpoints públicos independentes. O rollout terá um modo operacional explícito, de execução única, na aplicação existente para atualizar as bases dos tenants prontos antes da liberação das novas rotas.

## Contexto técnico

**Linguagem/Versão**: C# 14 / .NET 10 (`net10.0`).

**Dependências principais**: ASP.NET Core Blazor Web App com Interactive Server; EF Core 10.0.3 e Npgsql EF Core 10.0.0; ASP.NET Core Identity já existente; MudBlazor 9.11.0 já presente no projeto Web; xUnit e `Microsoft.AspNetCore.Mvc.Testing` para testes.

**Armazenamento**: PostgreSQL. `FinancasSaaS.Finance.Infrastructure` já mantém `TenantDbContext`, a entidade `Category` e migrations financeiras. Cada cliente dispõe de uma base isolada; a base de plataforma associa o usuário autenticado a um tenant ativo e à conexão autorizada.

**Testes**: Testes de domínio/aplicação em `FinancasSaaS.Domain.Tests` e `FinancasSaaS.Application.Tests`, testes de persistência e isolamento em `FinancasSaaS.IntegrationTests` com PostgreSQL, e testes HTTP/autorização/UI em `FinancasSaaS.Web.Tests`. Executar a menor suíte relacionada durante o desenvolvimento e a solução completa na validação de regressão.

**Plataforma alvo**: Aplicação web ASP.NET Core executada no servidor Linux, acessada em navegadores desktop e móveis.

**Tipo de projeto**: Aplicação web Blazor existente, organizada em projetos de domínio, aplicação, infraestrutura financeira, infraestrutura de plataforma e Web.

**Metas de desempenho**: Não foi definido SLO de latência ou volume para esta feature. Preservar a meta observável da especificação de localizar um lançamento em até dois minutos no teste moderado de usabilidade; consultas devem respeitar tenant e filtros e permanecer responsivas para uso individual sem prometer limites de escala não definidos.

**Restrições**:

- Derivar o usuário e a conexão de tenant da identidade autenticada confiável; nunca aceitar tenant ou conexão enviados pela interface como autorização.
- Manter autenticação, isolamento multi-tenant, rotas administrativas e fundação visual existentes. A autorização de rota e de cada operação deve ser validada no servidor.
- Valores somente em BRL, positivos, até R$ 9.999.999.999.999.999,99, com exatidão decimal de centavos; recusar valores acima do máximo ou com mais de duas casas decimais sem arredondar, tanto no cadastro quanto na correção; nunca usar ponto flutuante.
- Categorias iniciais Lanches, Mercado e Pessoal devem ser reconciliadas sem duplicidade apenas na disponibilização inicial da feature; nessa disponibilização, categorias padrão já existentes e inativas são reativadas sem mudar ID/nome. Uma migration forward pode reativar apenas categorias padrão ainda presentes em tenants já migrados; não insere categorias ausentes/renomeadas. Nome é único no tenant ignorando maiúsculas/minúsculas e espaços no início/fim.
- Correções e cancelamentos exigem motivo e mantêm histórico imutável com responsável, instante UTC e dados anteriores/novos pertinentes.
- Correções, realizações e cancelamentos baseados em versão obsoleta são recusados atomicamente, sem sobrescrever dados mais recentes.
- Criar/editar/excluir categorias e criar/realizar/corrigir/cancelar lançamentos produzem logs operacionais estruturados de resultado. Falhas e recusas relevantes de autorização também são rastreáveis; logs não incluem tokens, credenciais, connection strings, conteúdo financeiro nem dados pessoais desnecessários. O histórico de domínio de correções/cancelamentos permanece distinto dos logs operacionais.
- O cancelamento conserva o lançamento consultável; agregações futuras que consumam esses registros devem excluir cancelados. Esta feature não calcula nem exibe totais, dashboard ou relatórios agregados.
- Reutilizar os projetos e convenções atuais; não introduzir microserviços, mensageria, novo framework de UI ou infraestrutura sem necessidade demonstrada.

**Escala/escopo**: Fluxos de manutenção de categorias e lançamentos avulsos por cliente autenticado, filtros por período/tipo/categoria/estado, trilha de auditoria e prevenção de alterações concorrentes perdidas. Não inclui recorrência indefinida, parcelamento, cartões/faturas, dashboard, relatórios agregados ou exportação.

## Verificação da Constituição

*Gate: aprovado antes da Fase 0; reavaliado após o design da Fase 1.*

- **I. Isolamento entre tenants e segurança — PASS condicionado aos testes planejados**: o contexto confiável vem do usuário Identity e do resolvedor de conexão existente. O acesso às categorias, lançamentos e auditoria ocorre somente na base do tenant resolvida no servidor. Fixtures com dois tenants e testes negativos devem provar que IDs/URLs de categoria, lançamento e histórico do tenant B não revelam dados nem alteram registros quando acessados pelo tenant A, inclusive ao associar categoria alheia.
- **II. Integridade financeira e auditabilidade — PASS**: moeda fixa BRL, representação decimal exata, validações antes de persistir, eventos imutáveis para correções e cancelamentos, motivo obrigatório, ator e instante UTC. Cancelamento preserva o histórico e marca o registro; agregações futuras devem excluir cancelados, mas não há totalização nesta feature.
- **III. Domínio pragmático e arquitetura limpa — PASS**: regras de categoria e lançamento ficam independentes de Blazor e EF Core; infraestrutura financeira adapta persistência; Web apresenta os fluxos existentes. Reutiliza a base por tenant e os projetos atuais; não adiciona camada técnica sem necessidade.
- **IV. Testes automatizados — PASS condicionado aos gates de domínio, autorização e banco**: cobrir transições, validação de precisão monetária, trilha, logs operacionais estruturados em mutações/sucessos/falhas/recusas, conflito concorrente em correção/realização/cancelamento, migrações, filtros, rollout retomável e isolamento entre bases de tenant. Preservar testes existentes de Identity, provisionamento e Feature 2.
- **V. Evolução rastreável e incremental — PASS condicionado à instrumentação prevista**: alterações financeiras e migrações ficam nesta feature; decisões são documentadas em português do Brasil e validação segue roteiro repetível. Mutações financeiras e eventos de segurança/falha devem gerar logs estruturados rastreáveis, sem segredos ou dados pessoais desnecessários. Mudanças existentes de categorias precisam de migração compatível e verificável em todos os tenants.
- **Gate de complexidade — PASS**: novos dados e comportamento justificam mudanças no domínio, Application, infraestrutura Finance e Web existentes. Não há novo projeto nem dependência externa. A unicidade normalizada substitui a comparação atual de nome literal e deve ser migrada sem apagar dados.
- **Readiness operacional da migração — PASS com condição de rollout**: o provisionador atual ignora tenants já prontos. A equipe de implantação precisa usar os controles do ambiente para bloquear criação/provisionamento de tenants, drenar execuções em andamento, confirmar que não há provisionamentos ativos e gerar um manifesto imutável de IDs de tenants elegíveis antes de executar o modo de migração. O runner compara o conjunto do manifesto com os tenants prontos antes e depois das migrations e o modo somente leitura audita o histórico de cada migration e o estado das categorias padrão, sem connection strings. Categorias ausentes/renomeadas após a migration inicial só podem ser consideradas resolvidas com evidência operacional de exclusão/renomeação intencional; sem evidência, manter o rollout bloqueado e não aplicar backfill cego. Falha, divergência ou impossibilidade de confirmar a pausa bloqueia a liberação das rotas. Manter a pausa até a versão atualizada estar ativa e só então retomar criação/provisionamento.

## Estrutura do projeto

### Documentação desta feature

```text
specs/003-categorias-lancamentos-manuais/
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
├── FinancasSaaS.Domain/
│   └── Finance/                         # regras puras de categorias e lançamentos
├── FinancasSaaS.Application/
│   ├── Abstractions/                    # portas para conexão tenant e persistência financeira
│   └── Finance/                         # casos de uso e políticas de validação
├── FinancasSaaS.Finance.Infrastructure/
│   ├── Persistence/
│   │   ├── Entities/                    # categorias, lançamentos e eventos de auditoria
│   │   ├── Migrations/                  # expansão do schema por tenant
│   │   └── TenantDbContext.cs
│   └── Provisioning/                    # seed idempotente das categorias iniciais
└── FinancasSaaS.Web/
    ├── Components/
    │   ├── Layout/                      # layout já existente e shell autorizado do cliente
    │   └── Pages/Customer/Finance/      # páginas de lançamentos e categorias
    ├── Security/                        # reutiliza a política existente de acesso ao tenant
    └── Program.cs                       # DI, rotas e modo operacional explícito de rollout

tests/
├── FinancasSaaS.Domain.Tests/           # invariantes e transições
├── FinancasSaaS.Application.Tests/      # casos de uso e conflito/validação
├── FinancasSaaS.IntegrationTests/       # migração, persistência e isolamento por tenant
└── FinancasSaaS.Web.Tests/              # autorização, rotas e fluxos de interface
```

**Decisão de estrutura**: Usar a solução modular existente. Não adicionar projetos. A feature inaugura páginas autenticadas reais de cliente, permitindo o shell de cliente previsto como possibilidade na Feature 2; esse shell será separado da navegação administrativa e só apresentará rotas efetivamente implementadas. Casos de uso não recebem tenant arbitrário da interface: a identidade e o resolvedor existentes estabelecem a conexão autorizada.

## Organização das fases

### Fase 0 — Pesquisa e decisões

Registrar as decisões e alternativas em [research.md](research.md). Não há decisão de stack em aberto: dependências e padrões são determinados pela solução atual; as ambiguidades de domínio relevantes foram resolvidas na sessão de clarificação anotada na especificação.

### Fase 1 — Design e contratos

- Documentar entidades, relações, invariantes, auditoria, concorrência e estados em [data-model.md](data-model.md).
- Definir o contrato de interface e autorização do cliente em [contracts/ui.md](contracts/ui.md). A aplicação atual não expõe API financeira pública; os contratos são as rotas e ações da UI Blazor, sem criar um serviço HTTP independente.
- Descrever cenários executáveis e verificações de regressão em [quickstart.md](quickstart.md).
- Reavaliar os gates da constituição: PASS; sem violações ou novos mecanismos externos.

## Complexidade

Não há violações da constituição que precisem de exceção ou justificativa adicional.
