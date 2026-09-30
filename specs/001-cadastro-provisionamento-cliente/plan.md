# Plano de Implementação: Cadastro e Provisionamento de Cliente

**Branch**: `001-cadastro-provisionamento-cliente` | **Data**: 2026-09-29 | **Especificação**: [spec.md](spec.md)

**Entrada**: Especificação funcional em [spec.md](spec.md) e decisões registradas na sessão de clarificação.

## Resumo

Criar o primeiro fluxo de ciclo de vida de um cliente pessoa física: cadastro manual pelo
operador, provisionamento idempotente de um tenant, convite de primeiro acesso, ativação
somente após provisionamento e conclusão do cadastro, e desativação sem exclusão de dados.

O desenho usa um monólito modular .NET 10 com os módulos Platform e Finance. O banco da
plataforma guarda tenants, usuários, convites e estado de provisionamento; cada tenant possui
um banco PostgreSQL próprio para seus dados financeiros. Em desenvolvimento local, os bancos
podem compartilhar a mesma instância PostgreSQL. O serviço de e-mail será capturado pelo
Mailpit no Docker Compose.

## Contexto Técnico

**Linguagem/Versão**: C# com .NET 10 e ASP.NET Core 10

**Dependências Principais**: Blazor Web App com Interactive Server, ASP.NET Core Identity,
Entity Framework Core 10, Npgsql, PostgreSQL, Docker Compose e Mailpit

**Armazenamento**: Um banco PostgreSQL da plataforma e um banco PostgreSQL por tenant; em
desenvolvimento local, todos podem estar na mesma instância

**Testes**: xUnit, testes unitários de domínio/aplicação, testes de integração com PostgreSQL
em contêiner e testes de UI/fluxo no nível da aplicação quando necessário

**Plataforma Alvo**: Servidor Linux e desenvolvimento em VS Code no Linux; execução local por
Docker Compose

**Tipo de Projeto**: Aplicação web monolítica modular com Blazor Web App Interactive Server

**Metas de Desempenho**: O operador deve identificar o resultado de uma tentativa de
provisionamento em até 1 minuto; em ambiente local, ações interativas comuns devem responder
em até 2 segundos no percentil 95, excluindo indisponibilidade do serviço de e-mail

**Restrições**: O domínio não depende de Blazor, EF Core ou infraestrutura; a conexão do
tenant é resolvida no servidor após autenticação e autorização; não há transação distribuída
entre banco da plataforma e banco do tenant; migrations da plataforma e dos tenants são
separadas e versionadas; segredos locais não devem ser commitados

**Escala/Escopo**: Feature inicial para pessoa física, com um usuário inicial por tenant e
três categorias padrão. O desenho deve suportar crescimento do número de tenants sem criar
microserviços; limites de capacidade e estratégia de hospedagem produtiva ficam fora desta
feature

## Verificação da Constituição

*Gate: deve passar antes da pesquisa da Fase 0 e ser reavaliado após o design da Fase 1.*

* **Isolamento entre tenants e segurança**: PASS. O tenant é obtido do contexto autenticado
  no servidor; o usuário não informa livremente o banco ou tenant consultado. Testes com dois
  tenants cobrem acesso cruzado e manipulação de identificadores.
* **Integridade e auditabilidade**: PASS. A feature não cria valores monetários; estados,
  convites, tentativas e desativação preservam histórico e não há exclusão de dados na
  desativação.
* **Domínio e arquitetura limpa**: PASS. Domain/Application não referenciam Blazor, EF Core,
  PostgreSQL ou infraestrutura; Platform e Finance são módulos no mesmo processo.
* **Testes automatizados**: PASS. O plano inclui testes unitários e de integração para
  idempotência, falhas, convites, autorização e isolamento com dois tenants.
* **Evolução incremental e simplicidade**: PASS. Não são introduzidos microserviços,
  mensageria ou abstrações sem necessidade; o provisionamento é uma orquestração explícita
  com estado persistido e retentativa.
* **Gate de complexidade**: PASS. Dois bancos lógicos são exigidos pelo isolamento da
  feature; compartilhar a instância PostgreSQL local reduz custo operacional sem misturar os
  limites de dados.

## Estrutura do Projeto

### Documentação desta feature

```text
specs/001-cadastro-provisionamento-cliente/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md             # criado posteriormente por /speckit-tasks
```

### Código-fonte (raiz do repositório)

```text
FinancasSaaS.sln
src/
├── FinancasSaaS.AppHost/                 # composição local; não é serviço independente
├── FinancasSaaS.Domain/                  # regras e estados independentes de tecnologia
├── FinancasSaaS.Application/             # casos de uso e portas
├── FinancasSaaS.Platform.Infrastructure/ # Identity, plataforma e migrations do platform
├── FinancasSaaS.Finance.Infrastructure/  # acesso aos bancos dos tenants e migrations
├── FinancasSaaS.Web/                     # Blazor Web App Interactive Server
└── FinancasSaaS.Contracts/               # contratos internos compartilhados
tests/
├── FinancasSaaS.Domain.Tests/
├── FinancasSaaS.Application.Tests/
├── FinancasSaaS.IntegrationTests/
└── FinancasSaaS.Web.Tests/
database/
├── platform/                             # scripts/artefatos de execução das migrations
└── tenant/
docker/
├── compose.yaml
└── mailpit/
README.md
.env.example
.gitignore
Dockerfile
```

**Decisão de Estrutura**: Um monólito modular com projetos separados por dependência e
responsabilidade. Platform e Finance são módulos de negócio no mesmo aplicativo e processo;
Domain/Application permanecem independentes de infraestrutura. As migrations ficam em
projetos de infraestrutura distintos, permitindo aplicar a mesma versão do modelo de tenant
em bancos diferentes sem misturar o histórico do banco da plataforma. O `AppHost` é apenas uma
composição local fina para configuração e execução; não cria um serviço, processo ou fronteira
de implantação adicional e não exige microserviços ou uma plataforma de orquestração.

## Verificação da Constituição após o Design

* **Isolamento entre tenants e segurança**: PASS. [data-model.md](data-model.md) define a
  separação dos bancos e [contracts/fluxos.md](contracts/fluxos.md) impede que a UI escolha a
  conexão. [quickstart.md](quickstart.md) exige validação automatizada com dois tenants.
* **Integridade e auditabilidade**: PASS. Estados de provisionamento, convites, desativação e
  registros de auditoria estão definidos sem exclusão de dados ou ativação após falha.
* **Domínio e arquitetura limpa**: PASS. A pesquisa mantém Domain/Application sem dependência
  de Blazor, EF Core ou PostgreSQL, e concentra detalhes de persistência nos adaptadores.
* **Testes automatizados**: PASS. O quickstart lista invariantes, idempotência, expiração,
  bloqueio e acesso cruzado como cenários automatizados mínimos.
* **Evolução incremental e simplicidade**: PASS. Não há microserviços, mensageria ou transação
  distribuída; o banco por tenant é a única complexidade exigida pelo requisito de isolamento.

## Rastreamento de Complexidade

Nenhuma violação da constituição foi identificada; portanto, não há itens de complexidade a
justificar.
