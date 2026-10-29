# Especificação da Feature: Categorias e lançamentos manuais

**Feature Branch**: `não definida`

**Criada**: 2026-10-27

**Status**: Rascunho

**Entrada**: Criar a feature “Categorias e lançamentos financeiros manuais” para que clientes pessoa física organizem categorias e registrem, consultem e acompanhem entradas e despesas avulsas, preservando as features anteriores e reutilizando a autenticação, o isolamento entre tenants e o padrão visual da Feature 2.

## Clarificações

### Sessão 2026-10-27

- Q: O motivo deve ser obrigatório para cada correção e cancelamento de lançamento? → A: Sim. Cada correção e cancelamento exige que o cliente informe um motivo.
- Q: Quando um cliente cria ou renomeia uma categoria, o mesmo nome deve ser permitido mais de uma vez dentro do tenant, inclusive com diferenças de maiúsculas ou espaços nas extremidades? → A: Não. Nomes iguais sem diferenciar maiúsculas de minúsculas e ignorando espaços nas extremidades são duplicados dentro do mesmo tenant.
- Q: Em qual moeda o cliente deve registrar os valores dos lançamentos nesta feature? → A: Somente reais brasileiros (BRL), sem escolha de outra moeda ou conversão nesta feature.
- Q: Se um lançamento for alterado em outra sessão enquanto o cliente ainda está editando uma versão anterior, o que deve acontecer quando ele tentar salvar? → A: Recusar a gravação desatualizada e pedir que o cliente revise a versão atual antes de reaplicar sua correção.
- Q: O filtro por período deve exigir as duas datas e como tratar os limites e um intervalo invertido? → A: Exigir data inicial e final, incluir ambas as datas no período e rejeitar data inicial posterior à final com validação.

### Sessão 2026-10-28

- Q: O que deve ocorrer com Lanches, Mercado e Pessoal se o cliente renomear ou excluir uma dessas categorias e houver um rollout posterior? → A: São categorias iniciais; após a disponibilização inicial, rollouts posteriores não devem recriar categorias padrão que o cliente renomeou ou excluiu.
- Q: Qual é o valor máximo permitido para cadastro e correção de lançamentos? → A: O máximo representável por `numeric(18,2)`, R$ 9.999.999.999.999.999,99, aplicado igualmente ao cadastro e à correção.

### Sessão 2026-10-29

- Q: O que deve ocorrer se uma categoria padrão já existir, mas estiver inativa na primeira disponibilização da funcionalidade? → A: Reativar a categoria existente, preservando seu identificador e nome; não recriar categorias padrão ausentes ou renomeadas em rollouts posteriores.
- Q: O que deve ser exibido nos lançamentos existentes quando sua categoria for renomeada? → A: Exibir o nome atual da categoria também nos lançamentos existentes; não preservar uma cópia histórica do nome.

## Cenários e testes do usuário *(obrigatório)*

### História de usuário 1 — Registrar e acompanhar lançamentos (Prioridade: P1)

Como cliente pessoa física, quero registrar entradas e despesas avulsas como previstas e acompanhá-las até a realização, para manter meus compromissos financeiros organizados.

**Por que esta prioridade**: O registro e o acompanhamento são o valor central da feature; sem eles não há lançamentos para consultar, corrigir ou cancelar.

**Teste independente**: Com uma conta de cliente e ao menos uma categoria, registrar uma entrada e uma despesa previstas, consultá-las e confirmar a realização de uma delas com sua data efetiva.

**Cenários de aceite**:

1. **Dado** que um cliente autenticado possui categorias disponíveis, **quando** registra uma entrada com descrição, valor, categoria, data de competência e data prevista de recebimento, **então** ela fica prevista e pode ser consultada pelo cliente.
2. **Dado** que um cliente autenticado possui categorias disponíveis, **quando** registra uma despesa com descrição, valor, categoria, data de competência e data prevista de pagamento, **então** ela fica prevista e pode ser consultada pelo cliente.
3. **Dado** um lançamento previsto, **quando** o cliente confirma que foi realizado e informa a data efetiva, **então** o lançamento passa a realizado e conserva a data efetiva informada.
4. **Dado** um lançamento, **quando** o cliente tenta registrá-lo sem descrição, valor, categoria, data de competência ou data prevista, **então** o sistema informa o dado inválido ou ausente e não cria o lançamento.
5. **Dado** um valor zero ou negativo, **quando** o cliente tenta registrar ou corrigir um lançamento, **então** o sistema recusa a operação e informa que o valor deve ser positivo.
6. **Dado** um valor com mais de duas casas decimais ou acima do máximo de R$ 9.999.999.999.999.999,99, **quando** o cliente tenta registrar um lançamento, **então** o sistema recusa o valor sem arredondá-lo.
7. **Dado** que duas sessões carregaram a mesma versão de um lançamento previsto, **quando** uma delas o realiza e a outra tenta realizar a versão desatualizada, **então** a segunda operação é recusada e a versão atual deve ser revisada antes de uma nova tentativa.
8. **Dado** um valor no máximo de R$ 9.999.999.999.999.999,99, **quando** o cliente registra um lançamento, **então** o valor é aceito; se exceder esse máximo, a operação é recusada sem persistência.

### História de usuário 2 — Organizar categorias (Prioridade: P1)

Como cliente pessoa física, quero criar e editar minhas categorias e usar cada categoria tanto em entradas quanto em despesas, para organizar meus lançamentos segundo minhas necessidades.

**Por que esta prioridade**: Categorias são necessárias para classificar os lançamentos e devem estar disponíveis desde o primeiro uso da área financeira.

**Teste independente**: Em um tenant recém-disponibilizado, consultar as três categorias iniciais; criar e editar outra categoria; atribuí-la a uma entrada e a uma despesa; testar as restrições de exclusão.

**Cenários de aceite**:

1. **Dado** um tenant que ainda não dispõe das categorias iniciais, **quando** a funcionalidade fica disponível para esse tenant, **então** Lanches, Mercado e Pessoal ficam disponíveis sem ação manual do cliente.
2. **Dado** um tenant que já possui uma ou mais das categorias iniciais, **quando** a funcionalidade é disponibilizada pela primeira vez, **então** as categorias ausentes são disponibilizadas e as existentes não são duplicadas, mesmo que o nome existente varie em maiúsculas ou espaços nas extremidades.
3. **Dado** um cliente autenticado, **quando** cria ou edita uma categoria do próprio tenant com nome ainda não utilizado, **então** a categoria fica disponível para seus lançamentos.
4. **Dado** uma categoria associada a um ou mais lançamentos, **quando** o cliente tenta excluí-la, **então** a exclusão é impedida e os lançamentos permanecem associados.
5. **Dado** que o tenant possui somente uma categoria, **quando** o cliente tenta excluí-la, **então** a operação é impedida e a categoria permanece disponível.
6. **Dado** uma categoria, **quando** o cliente a usa em uma entrada e em uma despesa, **então** ambas as associações são aceitas.
7. **Dado** uma categoria existente no tenant, **quando** o cliente tenta criar ou renomear outra categoria com o mesmo nome, desconsiderando maiúsculas e minúsculas e espaços nas extremidades, **então** o sistema recusa a operação e mantém as categorias existentes.
8. **Dado** um cliente de outro tenant, **quando** tenta consultar ou editar uma categoria por identificador ou URL, **então** não obtém dados da categoria nem altera o registro.
9. **Dado** um lançamento associado a uma categoria, **quando** o cliente renomeia essa categoria, **então** o lançamento existente passa a exibir o nome atualizado da categoria.

### História de usuário 3 — Corrigir e auditar lançamentos (Prioridade: P2)

Como cliente pessoa física, quero corrigir informações de um lançamento e consultar o histórico das mudanças, para manter meus registros confiáveis e entender quem alterou o quê e quando.

**Por que esta prioridade**: Correções são necessárias para lidar com erros de registro sem apagar a rastreabilidade financeira.

**Teste independente**: Corrigir um lançamento e verificar que o histórico consultável apresenta o valor anterior e o novo, o instante, o responsável e o motivo da alteração.

**Cenários de aceite**:

1. **Dado** um lançamento do próprio tenant, **quando** o cliente o corrige, **então** o histórico mantém o valor anterior, o novo valor, o instante da mudança, o responsável e o motivo, e o cliente consegue consultá-lo.
2. **Dado** um lançamento realizado, **quando** o cliente corrige seus dados, **então** a correção é permitida com auditoria, o estado realizado e a data efetiva são mantidos, e o histórico apresenta os dados anteriores e novos, o instante, o responsável e o motivo.
3. **Dado** um cliente de outro tenant, **quando** tenta consultar ou corrigir um lançamento por identificador ou URL, **então** não obtém os dados nem altera o registro.
4. **Dado** um cliente que está corrigindo um lançamento, **quando** tenta confirmar a correção sem informar o motivo, **então** o sistema recusa a operação e mantém o lançamento inalterado.
5. **Dado** um valor com mais de duas casas decimais, **quando** o cliente tenta corrigir um lançamento, **então** o sistema recusa o valor sem arredondá-lo e mantém o lançamento inalterado.
6. **Dado** que o lançamento foi alterado em outra sessão desde que o cliente leu sua versão, **quando** tenta corrigir essa versão desatualizada, **então** o sistema recusa a gravação, informa que há uma versão mais recente e permite que o cliente a revise antes de reaplicar a correção.
7. **Dado** um valor acima de R$ 9.999.999.999.999.999,99, **quando** o cliente tenta corrigir um lançamento, **então** o sistema recusa a operação e mantém o lançamento inalterado.

### História de usuário 4 — Filtrar, consultar histórico e cancelar (Prioridade: P2)

Como cliente pessoa física, quero localizar lançamentos por período, tipo, categoria e estado e cancelar um lançamento sem apagar seu histórico, para acompanhar meus registros e corrigir compromissos que não ocorrerão.

**Por que esta prioridade**: A consulta filtrada torna os registros úteis no dia a dia; o cancelamento lógico preserva a rastreabilidade financeira.

**Teste independente**: Criar lançamentos com diferentes períodos, tipos, categorias e estados; aplicar filtros; cancelar um lançamento; confirmar que ele continua consultável no histórico e que a feature não apresenta totais ou agregações.

**Cenários de aceite**:

1. **Dado** lançamentos com datas, tipos, categorias e estados distintos, **quando** o cliente combina filtros por período, tipo, categoria e estado, informando data inicial e final, **então** a lista contém somente os lançamentos que atendem a todos os filtros informados; o período inclui as datas inicial e final e usa a data prevista para previstos, a data efetiva para realizados e a data correspondente ao estado anterior para cancelados.
2. **Dado** que o cliente não informou uma das datas do período, **quando** tenta aplicar o filtro, **então** o sistema aponta a data ausente e não aplica o filtro.
3. **Dado** que a data inicial é posterior à data final, **quando** o cliente tenta aplicar o filtro, **então** o sistema rejeita o intervalo com uma mensagem de validação e não aplica o filtro.
4. **Dado** um lançamento previsto, **quando** o cliente o cancela informando o motivo, **então** ele fica marcado como cancelado e continua consultável com histórico que identifica instante, responsável e motivo.
5. **Dado** um lançamento realizado, **quando** o cliente o cancela informando o motivo, **então** ele fica marcado como cancelado e permanece consultável com histórico que identifica instante, responsável e motivo.
6. **Dado** um cliente de outro tenant, **quando** tenta listar ou consultar o histórico de lançamentos por URL ou identificador, **então** não recebe dados desse tenant.
7. **Dado** uma categoria pertencente a outro tenant, **quando** o cliente tenta usá-la em um lançamento, **então** a operação é recusada.
8. **Dado** um cliente que está cancelando um lançamento, **quando** tenta confirmar o cancelamento sem informar o motivo, **então** o sistema recusa a operação e mantém o estado do lançamento inalterado.
9. **Dado** que o lançamento foi alterado em outra sessão desde que o cliente leu sua versão, **quando** tenta cancelar essa versão desatualizada, **então** o sistema recusa a operação, preserva a versão atual e permite que o cliente a revise antes de tentar novamente.

### Casos extremos

- Impedir a duplicação das categorias iniciais mesmo que uma categoria de mesmo nome já exista no tenant.
- Impedir nomes de categorias duplicados no tenant, desconsiderando maiúsculas e minúsculas e espaços no início ou no fim.
- Impedir exclusão de categoria vinculada a qualquer lançamento, inclusive cancelado, pois o histórico do lançamento deve continuar consultável.
- Impedir exclusão da última categoria disponível no tenant.
- Recusar datas inválidas ou dados obrigatórios ausentes sem criar ou alterar parcialmente um lançamento.
- Recusar qualquer correção, realização ou cancelamento baseado em uma versão desatualizada, sem sobrescrever a alteração mais recente.
- Rejeitar identificadores de categorias ou lançamentos pertencentes a outro tenant, mesmo em acesso direto por URL.
- Manter o lançamento cancelado e seu histórico consultáveis. Qualquer agregação futura que consuma esses registros deve excluir cancelados; esta feature não calcula nem exibe totais ou agregações.
- Ao filtrar lançamentos cancelados por período, usar a data prevista se estavam previstos antes do cancelamento e a data efetiva se estavam realizados.
- Exigir data inicial e final para aplicar o filtro por período, incluir ambos os limites e recusar intervalo em que a data inicial seja posterior à final.

## Requisitos *(obrigatório)*

### Requisitos funcionais

- **RF-001**: A funcionalidade deve estar disponível a clientes pessoa física autenticados, reutilizando a autenticação, o isolamento multi-tenant e os padrões visuais já existentes, sem recriar esses recursos.
- **RF-002**: Cada tenant deve dispor das categorias Lanches, Mercado e Pessoal quando a funcionalidade for disponibilizada pela primeira vez; nessa disponibilização, categorias iniciais existentes não podem ser duplicadas e categorias padrão existentes, mas inativas, devem ser reativadas preservando identificador e nome. Rollouts posteriores não devem recriar categorias padrão que o cliente renomeou ou excluiu nem reativar categorias que o cliente tenha tornado inativas após a disponibilização inicial.
- **RF-003**: O cliente deve poder criar e editar categorias pertencentes ao próprio tenant. Dentro de um tenant, nomes iguais após desconsiderar maiúsculas/minúsculas e espaços nas extremidades não podem ser usados em mais de uma categoria; tentativas de criação ou renomeação duplicada devem ser recusadas. A mesma regra deve ser aplicada ao identificar categorias iniciais já existentes.
- Renomear uma categoria atualiza o nome exibido em todos os lançamentos associados, inclusive os já existentes; o sistema não mantém snapshot do nome anterior da categoria.
- **RF-004**: A mesma categoria deve poder ser associada a uma entrada ou a uma despesa.
- **RF-005**: O sistema deve impedir a exclusão de categoria associada a qualquer lançamento e impedir que um tenant fique sem categorias.
- **RF-006**: O cliente deve poder criar lançamentos individuais do tipo entrada ou despesa em reais brasileiros (BRL), com descrição, valor positivo com no máximo duas casas decimais e no máximo R$ 9.999.999.999.999.999,99, categoria do próprio tenant, data de competência e data prevista de recebimento ou pagamento. Valores acima do máximo ou com mais de duas casas decimais devem ser recusados, sem arredondamento. Esta feature não deve permitir escolher outra moeda nem converter valores.
- **RF-007**: Todo lançamento deve iniciar no estado previsto.
- **RF-008**: O cliente deve poder confirmar manualmente a realização de um lançamento previsto, informando a data efetiva de recebimento ou pagamento.
- **RF-009**: O cliente deve poder corrigir lançamentos previstos ou realizados e deve informar um motivo para cada correção. A correção pode alterar o tipo, a descrição, o valor, a categoria, a data de competência e a data prevista de recebimento ou pagamento, mas não pode alterar o estado. Para lançamentos realizados, deve manter o estado realizado e a data efetiva registrada. Cada correção deve preservar para consulta os dados anteriores e novos de cada campo alterado, o instante da mudança, o responsável e o motivo.
- **RF-010**: O cliente deve poder cancelar logicamente um lançamento previsto ou realizado e deve informar um motivo para cada cancelamento. Lançamentos cancelados devem permanecer consultáveis por seu tenant, inclusive seu histórico, e o cancelamento deve preservar o estado anterior, o instante, o responsável e o motivo. Agregações futuras devem excluir lançamentos cancelados; esta feature não calcula nem exibe totais ou agregações.
- **RF-011**: O cliente deve poder listar e filtrar lançamentos por período, tipo, categoria e estado, combinando os critérios informados. Aplicar o filtro por período exige data inicial e final, inclui ambas as datas e recusa intervalo em que a data inicial seja posterior à final. O período deve usar a data prevista para lançamentos previstos e a data efetiva para realizados; para cancelados, deve usar a data prevista se estavam previstos antes do cancelamento ou a data efetiva se estavam realizados.
- **RF-012**: Todas as consultas e alterações de categorias, lançamentos e históricos devem ser restritas ao tenant autenticado no servidor, inclusive em acessos diretos por identificador ou URL.
- **RF-013**: O sistema deve recusar a associação a categorias de outro tenant e não deve revelar dados de lançamentos de outros tenants.
- **RF-014**: O sistema deve informar dados obrigatórios ausentes, datas inválidas, valores não positivos, valores acima de R$ 9.999.999.999.999.999,99 e valores com mais de duas casas decimais sem aceitar a operação nem arredondar o valor, tanto no cadastro quanto na correção.
- **RF-015**: Se um lançamento for alterado após o cliente ler a versão usada para uma correção, realização ou cancelamento, o sistema deve recusar atomicamente a operação baseada na versão desatualizada, não sobrescrever a versão mais recente, informar o conflito e permitir que o cliente revise a versão atual antes de tentar novamente.

### Entidades principais

- **Categoria**: agrupamento pertencente a um tenant, utilizável por entradas e despesas; pode ser criada e editada, e não pode ser excluída enquanto associada a lançamentos nem se for a última categoria do tenant.
- **Lançamento financeiro**: registro individual de entrada ou despesa em reais brasileiros (BRL), com descrição, valor, categoria, data de competência, data prevista de recebimento ou pagamento, estado previsto/realizado/cancelado e, quando realizado, data efetiva.
- **Histórico de alterações**: registro consultável pelo tenant do lançamento corrigido, contendo os dados anteriores e novos de cada campo alterado, instante, responsável e motivo.

## Critérios de sucesso *(obrigatório)*

### Resultados mensuráveis

- **CS-001**: Na disponibilização inicial da funcionalidade, cada tenant dispõe de Lanches, Mercado e Pessoal, sem duplicatas quando alguma delas já existia; em rollouts posteriores, categorias padrão renomeadas ou excluídas pelo cliente não reaparecem.
- **CS-002**: Em todos os testes de autorização, tentativas de consulta ou alteração de categorias, lançamentos ou históricos de outro tenant por identificador ou URL não revelam dados nem alteram registros.
- **CS-003**: Em 100% das correções aceitas nos testes, o cliente consegue consultar os dados anteriores e novos de cada campo alterado, instante, responsável e motivo.
- **CS-004**: Em 100% dos testes, lançamentos cancelados e seu histórico permanecem consultáveis somente pelo tenant proprietário; a feature não apresenta totais nem agregações.
- **CS-005**: Um cliente consegue registrar uma entrada ou despesa prevista e localizar o lançamento correspondente em até dois minutos, em um teste moderado de usabilidade.
- **CS-006**: Pelo menos 90% dos participantes de um teste moderado conseguem concluir sem orientação as tarefas de cadastrar um lançamento, confirmar sua realização e localizar um lançamento aplicando filtros.
- **CS-007**: Em 100% dos testes de filtro, a lista apresenta somente lançamentos que correspondem a todos os critérios informados e à regra de data escolhida; inclui lançamentos nos limites inicial e final e rejeita datas ausentes ou intervalos invertidos.

## Premissas

- A feature destina-se a clientes pessoa física que já concluíram autenticação e possuem contexto de tenant.
- A autenticação, o isolamento multi-tenant e os padrões visuais e de navegação da Feature 2 são dependências existentes e devem ser reutilizados.
- Categorias iniciais ausentes são disponibilizadas na primeira disponibilização da funcionalidade para o tenant; categorias padrão existentes, mas inativas, são reativadas nessa disponibilização. Rollouts posteriores não recriam categorias padrão renomeadas ou excluídas nem alteram a atividade definida posteriormente pelo cliente.
- Valores devem ser positivos e expressos somente em reais brasileiros (BRL); não há escolha de moeda nem conversão monetária nesta feature.
- O valor máximo é R$ 9.999.999.999.999.999,99 para cadastro e correção; valores acima desse limite são recusados.
- Lançamentos cancelados permanecem consultáveis e devem ser excluídos de agregações futuras; a feature não implementa totalização.
- A edição de lançamento realizado mantém seu estado e sua data efetiva; o cancelamento de realizado preserva estado anterior e histórico.
- O filtro por período usa a data prevista para previstos e a data efetiva para realizados; lançamentos cancelados usam a data correspondente ao estado anterior ao cancelamento.
- O filtro por período exige data inicial e final, inclui os dois limites e rejeita intervalo em que a data inicial seja posterior à final.
- A experiência deve seguir as telas e o padrão visual estabelecidos na Feature 2, sem introduzir uma fundação visual paralela.

## Fora de escopo

- Compras parceladas.
- Recorrência indefinida.
- Gestão de cartão ou fatura.
- Dashboard.
- Relatórios agregados.
- Exportação de dados.
