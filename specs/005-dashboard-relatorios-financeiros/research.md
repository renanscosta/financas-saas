# Pesquisa e decisões: Dashboard e relatórios financeiros

**Feature**: [spec.md](spec.md)

## Contexto observado no código

- A solução usa C# 14/.NET 10 com projetos Domain, Application, Finance.Infrastructure, Platform.Infrastructure e Web. Os dados financeiros ficam em bancos PostgreSQL isolados por tenant; `TenantFinanceContextFactory` resolve a conexão a partir da identidade autenticada (`ICurrentUserContext`).
- Lançamentos avulsos e parcelas compartilham a mesma tabela de lançamentos (parcelas têm identificador e número de compra preenchidos). A consulta de lançamentos avulsos da Feature 3 exclui explicitamente as parcelas; a Feature 4 as consulta por compra. Isso resolve D-001 sem alterar as Features 3 e 4: a nova leitura consulta o conjunto completo.
- A tabela de lançamentos já possui data de competência, data prevista, data efetiva, estado, estado anterior ao cancelamento e valor decimal com precisão (18,2). Há índices por estado + data prevista e estado + data efetiva, mas não por competência.
- Parcelas guardam competência como o primeiro dia do mês do vencimento; lançamentos avulsos guardam a data de competência informada. Ambos podem ser agrupados pelo mês e ano da competência (D-002).
- Categorias são excluídas fisicamente somente quando não há lançamentos; categorias com lançamentos sempre existem. O indicador de atividade pode estar falso em categorias padrão ainda não reativadas, por isso as leituras não podem filtrar categorias por atividade.
- As páginas de cliente usam Blazor Interactive Server, `CustomerLayout`, política `PlatformPolicies.TenantAccess`, rotas `/cliente/...`, `EditForm` com rótulos e componentes de feedback existentes. Formatação em pt-BR é feita no ponto de uso (`CultureInfo.GetCultureInfo("pt-BR")`, `dd/MM/yyyy`).
- O registro operacional usa `FinanceLog` (`OperationCompleted`, `PersistenceFailed`) com ator, tenant, instante e correlação.
- O provisionador e o runner de migrations aplicam alterações de esquema a novos tenants e a tenants prontos (inventário, auditoria e migração em lote).
- A data corrente nas telas existentes vem de `DateTime.Today` do servidor; o relógio de serviço (`ISystemClock`) é UTC.
- Não existe hoje preferência por cliente, biblioteca de gráficos, nem leitura agregada.

## Decisões

### Reutilizar a fronteira financeira por tenant e criar apenas uma camada de leitura

**Decisão**: Adicionar regras de agrupamento/saldo ao domínio, um serviço de aplicação de leitura, um repositório de leitura na infraestrutura financeira e três páginas Web. Nenhuma operação de escrita sobre lançamentos, parcelas ou categorias é criada ou alterada.

**Racional**: Preserva o isolamento já provado, evita duplicar regras de estado e atende RF-020 (somente leitura).

**Alternativas consideradas**:

- Estender `IManualTransactionStore` com agregações: rejeitado porque misturaria leitura analítica com a persistência de mutações e arriscaria alterar o comportamento da Feature 3.
- API HTTP pública ou serviço separado de relatórios: rejeitado por não haver consumidor externo e por criar nova superfície de autorização.
- Banco ou visão materializada de relatórios: rejeitado; não há problema de desempenho demonstrado (constituição, princípio III).

### Regra única de data de referência, compartilhada por totais e listas

**Decisão**: Definir no domínio a regra que associa cada lançamento não cancelado a uma data de referência por critério:

| Critério | Previsto | Realizado |
|---|---|---|
| Competência | data de competência | data de competência |
| Pagamento/recebimento | data prevista | data efetiva |

Lançamentos cancelados nunca são considerados. A infraestrutura traduz a mesma regra em um único filtro de consulta reutilizado por agregações e por listas de composição.

**Racional**: O predicado compartilhado mantém as regras de totais e listas consistentes, mas consultas separadas podem observar dados em momentos diferentes. A composição concilia com seu próprio total e informa diferenças em relação ao total anterior. Um teste de integração compara o resultado do SQL com a regra de domínio aplicada a um conjunto gerado (oráculo independente, CS-001/CS-002).

**Alternativas consideradas**:

- Calcular totais e listas com regras de filtro distintas: rejeitado por permitir divergência entre as regras.
- Carregar todos os lançamentos na memória e agregar em C#: rejeitado para o volume previsto (CS-011) e para períodos de até 24 meses; viável apenas como oráculo de teste.

### Granularidade do filtro de período por critério (mantida provisoriamente)

**Decisão**: No critério por pagamento/recebimento, o período compara a data de referência exatamente com a data inicial e a final, inclusive. No critério por competência, o período é aplicado a meses de calendário: considera todos os lançamentos cuja competência está entre o mês da data inicial e o mês da data final.

**Racional**: Competência é um conceito mensal e as parcelas guardam o primeiro dia do mês; comparar datas exatas excluiria parcelas que o cliente enxerga no mês. O limite de 24 meses da especificação já é contado em meses de calendário.

**Impacto/risco**: A RF-007 distingue explicitamente a aplicação por meses de competência e por datas exatas de pagamento/recebimento. A decisão foi mantida provisoriamente pelo responsável em 2026-10-30 e pode ser revista depois; uma mudança exige atualizar a especificação, a regra de filtro e os testes de período.

**Alternativas consideradas**:

- Data exata também na competência: rejeitada por causa da inconsistência entre avulsos (dia livre) e parcelas (dia 1).
- Exigir seleção por mês nos relatórios: rejeitada por contrariar a especificação (data inicial e final).

### Agregação no banco, sem arredondamento

**Decisão**: Somar valores como decimal exato no PostgreSQL, agrupando por mês de referência, tipo, estado e categoria, e calcular saldos no domínio com `decimal`. Valores nunca passam por ponto flutuante. Uma soma que não caiba na representação falha explicitamente (a consulta é tratada como erro, nunca exibe valor aproximado).

**Racional**: Atende o princípio II e o caso extremo de soma acima do limite individual.

**Alternativas consideradas**: soma em centavos inteiros no cliente; rejeitada por duplicar conversões sem ganho.

### Lista de composição entregue por inteiro e com total próprio

**Decisão**: A lista de composição retorna todos os itens do total solicitado, e o total exibido na lista é calculado a partir desses mesmos itens, em uma única consulta. A tela compara esse total com o total que originou a abertura e, se diferirem, informa que os dados foram atualizados (cenário 7 da História 3). Paginação, se necessária, é apenas de apresentação, sobre o conjunto completo.

**Racional**: Garante que a soma da lista seja idêntica ao total exibido nela, mesmo com mudanças concorrentes.

**Alternativas consideradas**: paginar no banco e exibir somente subtotal da página; rejeitada porque quebraria a conferência pelo cliente.

### Itens de parcela com contexto de compra

**Decisão**: Cada item da lista de composição de uma parcela inclui a descrição da compra, o número da parcela e a quantidade total de parcelas, lidos da compra existente. Ações de correção ou histórico apontam para as telas das Features 3 e 4; esta feature não as duplica.

**Racional**: Atende RF-008 e a cenário 9 da História 3 sem reimplementar regras.

### Armazenar a preferência de critério no banco do tenant

**Decisão**: Guardar a preferência em uma estrutura própria no banco financeiro do tenant, com o identificador do usuário titular obtido da identidade autenticada, o critério, o instante UTC e o usuário da última alteração. Na ausência de registro para o usuário, vale o critério inicial das premissas (competência). A gravação é um upsert por usuário no tenant, com unicidade por identificador de usuário.

**Racional**: A preferência é individual mesmo quando usuários compartilham um tenant, conforme decisão do responsável em 2026-10-31. O dado herda o isolamento do banco do tenant e é aplicado a tenants existentes pelo processo de migrations por tenant já operado.

**Alternativas consideradas**:

- Banco da plataforma por usuário: rejeitado por separar dado financeiro do seu ciclo de vida e exigir novo caminho de isolamento.
- Cookie ou armazenamento do navegador: rejeitado; a preferência deve persistir entre sessões e dispositivos (História 4, cenário 7).

### Registro operacional da alteração de preferência

**Decisão**: Registrar a alteração por `FinanceLog.OperationCompleted` com operação, resultado, ator, tenant, instante e correlação, sem tabela de auditoria financeira. Falhas de consulta usam `PersistenceFailed`. Consultas bem-sucedidas não são registradas.

**Racional**: RF-023 e RF-024 pedem registro estruturado sem eventos de auditoria financeira para leituras.

### Sem biblioteca de gráficos

**Decisão**: Apresentar totais e agrupamentos em cartões e tabelas com os componentes existentes. Nenhuma dependência de gráficos é adicionada.

**Racional**: A especificação veda dados fictícios e exige conciliação com listas; tabelas atendem sem nova dependência.

**Alternativas consideradas**: adicionar biblioteca de gráficos; rejeitada por ausência de necessidade demonstrada.

### Rotas e navegação

**Decisão**: Páginas `/cliente/dashboard` e `/cliente/relatorios`, protegidas pela política existente de acesso ao tenant e usando `CustomerLayout`. A navegação do cliente ganha dois destinos. Mês, critério, período, categoria e estado podem ser informados por parâmetros de consulta apenas como conveniência; todos são revalidados no servidor e nunca determinam o tenant.

**Alternativas consideradas**: página única com abas; rejeitada por dificultar foco e títulos distintos para acessibilidade.

### Mês corrente padrão

**Decisão**: O mês inicial do dashboard é o mês da data corrente do servidor, seguindo a convenção das telas existentes (`DateTime.Today`). Limitação conhecida: perto da meia-noite, o servidor pode divergir do dia do cliente.

**Alternativas consideradas**: fuso fixo da cultura brasileira; adiável porque as demais telas já adotam a convenção.

### Desempenho

**Decisão**: Adicionar índice para consulta por competência e estado e confiar nos índices existentes para datas prevista e efetiva. Validar CS-011 com teste de desempenho semeado com 5.000 lançamentos, fora da suíte de gate do MVP como os demais testes de desempenho.

## Questões em aberto para o plano

Anotadas, não bloqueantes nesta fase (verificação da constituição suspensa por decisão do responsável):

- Granularidade da competência no filtro de período: mantida a decisão acima por ora; revisão posterior opcional.
- Mudança do link da marca em `CustomerLayout` para o dashboard: não decidida; mantém-se o destino atual.
