# Pesquisa e decisões: Categorias e lançamentos manuais

**Feature**: [spec.md](spec.md)

## Contexto observado

- A solução já é .NET 10/C# 14, organizada em Domain, Application, Finance.Infrastructure, Platform.Infrastructure e Web.
- `TenantDbContext` mantém `Categories` por banco de tenant. A categoria atual tem identificador, nome limitado a 120 caracteres e estado ativo; hoje o índice único compara o texto literal.
- `PostgresTenantProvisioner` aplica migrations por tenant e insere Lanches, Mercado e Pessoal quando o texto exato ainda não existe.
- `PlatformTenantConnectionResolver` recebe um ID de usuário, verifica vínculo e acesso habilitado, verifica estado ativo/pronto e devolve uma conexão configurada. O handler de autorização de tenant já consulta esse resolvedor pela identidade autenticada.
- O projeto Web usa Identity, Blazor Interactive Server e MudBlazor; as páginas administrativas demonstram o padrão de componentes e feedback existente.
- A solução já possui testes de integração PostgreSQL e testes Web para autenticação e autorização. Não há página financeira autenticada de cliente até esta feature.

## Decisões

### Manter a arquitetura e o armazenamento isolado por tenant

**Decisão**: Persistir categorias, lançamentos e histórico no `TenantDbContext` do tenant autorizado; adicionar casos de uso ao projeto Application, regras independentes de UI/persistência ao Domain e adaptadores ao Finance.Infrastructure. Apresentar os fluxos em páginas Blazor da aplicação Web existente.

**Racional**: Reutiliza o limite de dados já estabelecido, evita introduzir outra estratégia multi-tenant e mantém o domínio desacoplado de Blazor/EF Core.

**Alternativas consideradas**:

- Criar uma base compartilhada com coluna tenant: rejeitada, pois contradiz o isolamento por banco já implementado.
- Criar API ou projeto Web separado: rejeitado, pois a feature só necessita de interação na aplicação atual.
- Colocar regras financeiras em componentes ou entidades EF: rejeitado, pois dificultaria teste independente e violaria a separação de domínio estabelecida.

### Resolver conexão usando a identidade autenticada

**Decisão**: Obter o identificador do usuário autenticado do contexto de segurança do servidor e reutilizar `ITenantConnectionResolver`; falhar explicitamente se o usuário não tiver acesso ativo ou conexão autorizada. Nunca usar tenantId, nome de banco ou connection string enviados pela UI como autoridade.

**Racional**: O resolvedor já consulta a base de plataforma e valida vínculo e estado do cliente/tenant. A política atual de autorização de tenant é um ponto de partida, mas as páginas e todas as operações de dados devem exigir acesso do cliente autenticado.

**Alternativas consideradas**:

- Aceitar tenantId no formulário ou na URL para selecionar a base: rejeitada por permitir manipulação entre clientes.
- Confiar somente em esconder links ou proteger a rota: rejeitada, pois não protege mutações e consultas chamadas diretamente.

### Unicidade e categorias iniciais idempotentes

**Decisão**: Identificar o nome canônico removendo espaços nas extremidades e comparando sem distinção entre maiúsculas/minúsculas, apenas dentro da base do tenant. Aplicar a mesma equivalência na criação, edição e reconciliação de Lanches, Mercado e Pessoal. Impor a regra também no armazenamento para proteger concorrência. Na disponibilização inicial, reativar a categoria padrão existente e inativa, preservando ID e nome; em rollouts posteriores não recriar ausentes/renomeadas nem reativar categorias inativadas após a primeira disponibilização.

**Racional**: Atende à regra clarificada de não permitir categorias duplicadas e impede inserções simultâneas conflitantes. A estratégia deve alinhar validação e regra persistida, em vez de depender somente da UI.

**Alternativas consideradas**:

- Manter comparação literal: rejeitada porque `mercado`, `Mercado` e ` Mercado ` seriam duplicatas operacionais.
- Comparar somente na aplicação: rejeitada porque duas criações concorrentes poderiam violar a regra.
- Mesclar ou excluir categorias legadas automaticamente: rejeitada para não apagar dados do cliente silenciosamente.

**Compatibilidade da migração**: Antes de ativar a restrição normalizada, detectar nomes legados equivalentes dentro de cada tenant. Não mesclar nem excluir automaticamente. Se houver conflito, falhar a migração de forma explícita e manter os dados para resolução antes de retentar.

**Atualização dos tenants existentes**: O provisionador atual não reaplica migrations em tenants marcados como prontos. O rollout desta feature precisa executar e acompanhar migrations para cada base financeira existente antes de disponibilizar as rotas; o provisionamento normal continua responsável por bancos novos. Uma falha em qualquer tenant deve ser explícita e impedir que esse tenant use a feature até sua atualização ser resolvida.

**Auditoria pré-rollout**: Gerar relatório somente leitura do histórico das migrations e do estado (`active`, `inactive`, `missing` ou `conflict`) das categorias padrão para os tenants do manifesto. A migration forward reativa apenas os padrões ainda presentes, sem inserir ausentes; se a migration inicial já estiver aplicada e uma ausência/renomeação não puder ser distinguida de seed faltante por evidência operacional, bloquear o rollout e não executar backfill automático.

### Representação e validação financeira

**Decisão**: Usar apenas BRL, guardar quantias em representação decimal exata com precisão de centavos e exigir valor estritamente positivo. Tratar entrada e despesa como tipos explícitos, sem converter despesa em valor negativo.

**Racional**: Preserva significado do valor, evita arredondamentos silenciosos e segue a constituição. Valores com mais de duas casas decimais são recusados tanto na criação quanto na correção; não são arredondados. O sentido do fluxo é carregado pelo tipo; apresentação pode distinguir entradas e despesas.

**Alternativas consideradas**:

- Ponto flutuante: rejeitado por não representar quantias com exatidão.
- Arredondar frações menores que um centavo: rejeitado para que o sistema não altere silenciosamente um valor informado pelo cliente.
- Valor negativo para representar despesa: rejeitado por conflitar com a regra de valor positivo e misturar tipo com sinal.
- Múltiplas moedas ou conversão: rejeitadas, fora do escopo clarificado.

### Estados, auditoria e correções

**Decisão**: Manter estados distintos previsto, realizado e cancelado. Registrar confirmação manual com data efetiva. Uma correção pode alterar tipo, descrição, valor, categoria, competência e data prevista; não pode mudar o estado. Para realizados, não pode alterar a data efetiva. Toda correção e cancelamento exige motivo e cria histórico imutável com ator, instante UTC e dados anteriores/novos; o cancelamento registra também o estado anterior e preserva o item consultável.

**Racional**: Assegura a regra de negócio clarificada e permite explicar reconstruções de estado sem sobrescrever a trilha anterior.

**Alternativas consideradas**:

- Sobrescrever o registro sem evento: rejeitada por perder auditabilidade.
- Permitir corrigir estado ou data efetiva junto com outros campos: rejeitada por poder alterar retroativamente o fato de realização; nova confirmação/cancelamento são transições explícitas.
- Excluir fisicamente um lançamento: rejeitada porque o requisito pede histórico consultável.

### Período e totais após cancelamento

**Decisão**: Exigir data inicial e final no filtro por período, incluir ambos os limites e rejeitar intervalo em que a data inicial seja posterior à final. Filtrar previstos pela data prevista de pagamento/recebimento e realizados pela data efetiva; para cancelados, manter a data associada ao estado que tinham imediatamente antes do cancelamento. Excluir cancelados de qualquer total calculado depois do cancelamento, mesmo em períodos passados. Não criar dashboard ou relatório agregado nesta feature.

**Racional**: Aplica a decisão registrada na especificação e preserva a data que explica a posição do registro no histórico.

**Alternativas consideradas**:

- Filtrar sempre pela competência: rejeitada pela decisão explícita do usuário.
- Manter cancelados em totais históricos recalculados: rejeitada pela regra explícita do usuário.
- Atualizar permanentemente snapshots de períodos anteriores: fora do escopo; o requisito trata de totais recalculados após o cancelamento.

### Concorrência

**Decisão**: Usar controle otimista de versão do lançamento em toda mutação: correção, realização e cancelamento. Rejeitar atomicamente qualquer operação baseada em versão anterior e informar o usuário para revisar a versão atual antes de reaplicar.

**Racional**: Evita perda silenciosa de alterações. Nenhuma infraestrutura de bloqueio ou serialização global é necessária.

**Alternativas consideradas**:

- Última gravação vence: rejeitada porque uma mudança financeira poderia desaparecer sem aviso.
- Mesclagem automática de campos: rejeitada por não ser segura para valores, estado e datas financeiras.
- Bloqueio pessimista por sessão: rejeitado por complexidade e por manter recursos presos durante interação humana.

### Rollout das bases de tenants existentes

**Decisão**: Adicionar modos operacionais explícitos à aplicação Web existente para gerar um manifesto JSON imutável contendo somente timestamp UTC e IDs de tenants prontos, e para aplicar migrations financeiras tenant a tenant usando esse manifesto. O runner compara exatamente os IDs do manifesto com os tenants prontos antes da primeira migration e novamente após todas as migrations; tenant ausente/inesperado, mudança de estado, falta de conexão ou falha impede sucesso e bloqueia a liberação das novas rotas. A equipe de implantação usa os controles do ambiente para bloquear criação/provisionamento de tenants, drenar execuções iniciadas e confirmar zero provisionamentos ativos antes de gerar o manifesto. Mantém a pausa até a versão atualizada estar ativa e só então retoma criação/provisionamento. O processo não altera o estado de provisionamento, não registra connection strings e pode ser repetido com segurança.

**Racional**: O provisionador existente aceita um tenant por chamada e não reaplica migrations a tenants `Ready`. O manifesto fornece um conjunto esperado verificável, enquanto a comparação pré/pós detecta divergência do inventário. Os modos explícitos permitem rollout controlado sem migração automática no startup normal e sem criar um projeto independente; o bloqueio operacional fecha a corrida entre geração, execução e implantação.

**Alternativas consideradas**:

- Confiar no provisionador de novos tenants: rejeitada porque tenants `Ready` não são migrados novamente.
- Migrar automaticamente no startup da aplicação: rejeitada porque mistura operação de release com inicialização normal e não oferece uma barreira explícita antes da exposição das rotas.
- Criar um novo projeto executável: rejeitada para preservar a arquitetura atual; o entrypoint operacional fica na aplicação existente.

### Interface e contrato

**Decisão**: Implementar as jornadas como páginas interativas da Web existente, com layout de cliente separado, rotas protegidas e feedback acessível. Não expor API financeira pública nesta feature.

**Racional**: Reutiliza o padrão visual da Feature 2 e o runtime de UI existente sem expandir contratos de integração que não foram pedidos.

**Alternativas consideradas**:

- Criar uma nova aplicação frontend/API: rejeitada por custo e complexidade sem benefício requerido.
- Usar links como controle de autorização: rejeitada; todas as operações têm autorização e escopo aplicados no servidor.

## Questões não bloqueantes

- Não há SLO de disponibilidade, latência ou limite máximo de registros definido; nenhum será inventado no plano. O desenho deve usar filtros e índices adequados e a capacidade deve ser avaliada se houver meta de produção.
- O projeto atual não define uma política de retenção financeira específica. O histórico fica preservado conforme o requisito e a política geral de retenção existente, sem uma regra nova de expurgo nesta feature.
