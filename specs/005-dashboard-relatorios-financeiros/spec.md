# Especificação da Feature: Dashboard e relatórios financeiros

**Feature Branch**: `não definida`

**Criada**: 2026-10-30

**Status**: Rascunho

**Entrada**: Criar a feature “Dashboard e relatórios financeiros” para que o cliente acompanhe sua situação financeira mensal e entenda de quais lançamentos vêm os valores exibidos, reutilizando as regras já implementadas de lançamentos avulsos (Feature 3), compras parceladas (Feature 4), categorias, auditoria, isolamento por tenant e identidade visual (Feature 2), sem reimplementá-las nem alterá-las.

## Reutilização e divergências identificadas

Esta seção registra o que foi verificado nas especificações e no código existentes antes de especificar. Nada abaixo altera regras das Features 1 a 4; as divergências são pontos que a nova feature precisa respeitar ou que exigem decisão explícita.

### Regras existentes reutilizadas sem alteração

- **Estados do lançamento**: previsto, realizado e cancelado. Lançamento realizado possui data efetiva; cancelado preserva o estado anterior (Feature 3). Cancelados continuam consultáveis, mas as especificações 3 e 4 já determinam que agregações futuras os excluam.
- **Datas do lançamento**: cada lançamento possui data de competência e data prevista de recebimento ou pagamento; ao ser realizado, passa a ter também a data efetiva (Feature 3).
- **Parcelas**: cada parcela é uma despesa individual com número, vencimento (que funciona como data prevista de pagamento) e competência correspondente ao mês e ano do vencimento; pode ser prevista, realizada ou cancelada (Feature 4). Uma compra parcelada não possui valor próprio nos totais: somente suas parcelas contam.
- **Valores e moeda**: somente reais brasileiros (BRL), com duas casas decimais e o limite de R$ 9.999.999.999.999.999,99 por lançamento (Feature 3).
- **Categorias**: pertencem a um único tenant; o nome atual da categoria é sempre exibido, inclusive nos lançamentos existentes, sem cópia histórica do nome (Feature 3).
- **Isolamento e autenticação**: o tenant é derivado do contexto autenticado no servidor; identificadores de outro tenant, mesmo por URL, não revelam dados (Features 1, 3 e 4).
- **Identidade visual e navegação**: área do cliente com navegação própria, tipografia, componentes, estados de carregamento, sucesso, validação e falha, responsividade e acessibilidade já estabelecidos (Feature 2). A Feature 2 proibia dashboards e dados fictícios enquanto não houvesse tela real; esta feature passa a oferecer tela real, e os dados exibidos devem ser exclusivamente reais.

### Divergências e pontos de atenção encontrados

- **D-001 — Listas separadas de lançamentos e parcelas**: a consulta de lançamentos avulsos da Feature 3 não apresenta as parcelas de compras parceladas; estas são consultadas na área da Feature 4. Para atender ao requisito de abrir os lançamentos que compõem cada total, a lista de composição precisa reunir lançamentos avulsos e parcelas sem alterar as regras nem as telas das Features 3 e 4.
- **D-002 — Duas regras de competência**: no lançamento avulso a competência é uma data informada pelo cliente; na parcela, é o mês do vencimento. Para esta feature, o “mês de competência” é o mês e o ano da data de competência do lançamento, o que é compatível com ambas as regras e com a exigência de que cada parcela pertença ao mês de seu vencimento.
- **D-003 — Critério de data já existente apenas em parte**: o filtro por período da Feature 3 já usa a data prevista para previstos e a data efetiva para realizados, o que equivale ao critério “pagamento/recebimento” desta feature; não existe hoje consulta por competência. A Feature 3 exige data inicial e final e rejeita intervalo invertido; essa regra continua valendo para o filtro por intervalo dos relatórios.
- **D-004 — Cancelados**: a Feature 3 usa, para filtrar cancelados, a data do estado anterior ao cancelamento. Nesta feature os cancelados não entram em nenhum total; seu tratamento no filtro da Feature 3 permanece inalterado.
- **D-005 — Preferência por cliente inexistente**: não há hoje preferência de critério de data por cliente. Trata-se de um dado novo que precisa respeitar o isolamento por tenant e as regras de retenção, exportação, exclusão e backup já aplicáveis aos dados do cliente.
- **D-006 — Estado de realização e mês**: um lançamento previsto realizado em outro mês muda de mês no critério por pagamento/recebimento (data prevista versus data efetiva). Esse efeito sobre os totais é uma consequência das regras existentes e foi esclarecido: a parcela apenas migra de estado e de mês, sem preservar a previsão original nos relatórios.
- **D-007 — Exclusões anteriores de escopo**: as Features 3 e 4 colocaram dashboard e relatórios fora de seu escopo; não há conflito, pois esta feature é a consumidora prevista dessas agregações.

## Clarificações

### Sessão 2026-10-31

- Q: A preferência de critério é compartilhada pelo tenant ou individual por usuário? → A: É individual por usuário, inclusive quando mais de um usuário acessa o mesmo tenant.

### Sessão 2026-10-30

- Q: O “saldo previsto” do mês deve considerar apenas os lançamentos que ainda estão previstos, ou também os já realizados no mês? → A: Somente lançamentos que continuam previstos (entradas previstas menos despesas previstas no estado atual), exibido com o rótulo “ainda a realizar”.
- Q: Quando uma parcela passa de prevista para realizada, os relatórios devem apenas mover o valor do total previsto para o realizado ou continuar mostrando o valor originalmente previsto? → A: Apenas migra: a parcela passa a contar como realizada, sai do total previsto e entra no realizado, no mês indicado pelo critério de data; os relatórios não mantêm o valor previsto original.
- Q: Qual é o intervalo máximo que o cliente pode consultar nos relatórios, e o dashboard deve permitir qualquer mês? → A: O dashboard aceita qualquer mês; os relatórios aceitam até 24 meses de calendário por consulta; intervalos maiores são recusados com mensagem, sem ajuste automático.

As demais dúvidas em aberto estão na seção “Dúvidas em aberto”.

## Cenários e testes do usuário *(obrigatório)*

### História de usuário 1 — Acompanhar a situação financeira do mês no dashboard (Prioridade: P1)

Como cliente pessoa física, quero selecionar um mês e ver, em uma única tela, quanto está previsto e quanto já foi realizado de entradas e despesas, para entender minha situação financeira no período sem confundi-la com meu saldo bancário.

**Por que esta prioridade**: É o valor central da feature e a primeira pergunta do cliente: “como está meu mês?”.

**Teste independente**: Com lançamentos avulsos e parcelas em estados e meses variados, selecionar um mês e conferir manualmente cada total e cada saldo.

**Cenários de aceite**:

1. **Dado** um cliente autenticado com lançamentos no mês selecionado, **quando** abre o dashboard, **então** vê, separadamente, entradas previstas, entradas realizadas, despesas previstas e despesas realizadas do mês, em reais, conforme o critério de data em uso.
2. **Dado** os totais do mês, **quando** o dashboard é exibido, **então** o saldo realizado é igual às entradas realizadas menos as despesas realizadas, e o saldo previsto (rotulado “ainda a realizar”) é igual às entradas previstas menos as despesas previstas, considerando somente lançamentos que continuam previstos; lançamentos já realizados não entram no saldo previsto.
3. **Dado** um saldo negativo (realizado ou ainda a realizar), **quando** é exibido, **então** o valor é apresentado com sinal e texto que indique que é negativo, sem depender apenas de cor.
4. **Dado** que o dashboard exibe saldo realizado e saldo previsto, **quando** o cliente os visualiza, **então** a tela informa por texto visível que os valores refletem apenas os lançamentos registrados e que previsão e realização não representam o saldo bancário.
5. **Dado** lançamentos de despesa com categorias distintas no mês, **quando** o dashboard é exibido, **então** as despesas aparecem agrupadas por categoria, com o valor previsto e o valor realizado de cada categoria apresentados separadamente.
6. **Dado** que o cliente seleciona outro mês, **quando** a consulta termina, **então** todos os totais, saldos e agrupamentos passam a refletir exclusivamente o novo mês.
7. **Dado** um mês sem lançamentos não cancelados, **quando** o cliente o seleciona, **então** a tela exibe um estado explícito de ausência de dados (também quando todos os lançamentos do mês estão cancelados), com orientação para registrar lançamentos, nenhum valor diferente de R$ 0,00 e nenhum dado ou gráfico fictício.
8. **Dado** que a consulta falha, **quando** o cliente seleciona um mês, **então** a tela exibe uma mensagem de erro compreensível, com opção de tentar novamente, sem apresentar zeros ou valores antigos como se fossem resultado válido e sem expor detalhes técnicos.
9. **Dado** que a consulta está em andamento, **quando** o cliente aguarda, **então** um indicador de carregamento identificável é exibido e valores do mês anterior não são apresentados como se pertencessem ao mês novo.
10. **Dado** um lançamento cancelado no mês, **quando** os totais são calculados, **então** o cancelado não entra em nenhum total, saldo ou agrupamento.

---

### História de usuário 2 — Analisar despesas por mês e categoria nos relatórios (Prioridade: P1)

Como cliente pessoa física, quero ver minhas despesas agrupadas por mês e por categoria e filtrá-las por período, categoria e estado, para identificar para onde meu dinheiro está indo ao longo do tempo.

**Por que esta prioridade**: Complementa o dashboard com a visão comparativa entre meses e categorias, necessária para análise além do mês corrente.

**Teste independente**: Criar despesas em vários meses e categorias, previstas e realizadas, aplicar combinações de filtros e conferir os totais por mês e categoria.

**Cenários de aceite**:

1. **Dado** despesas em meses e categorias distintos, **quando** o cliente abre o relatório, **então** vê o total de despesas por mês e, dentro de cada mês, por categoria, com previsto e realizado distinguidos.
2. **Dado** um período informado, **quando** o filtro é aplicado, **então** somente os meses e lançamentos que pertencem ao período, conforme o critério de data em uso, são considerados.
3. **Dado** uma categoria selecionada, **quando** o filtro é aplicado, **então** somente despesas dessa categoria são consideradas, e totais de outras categorias não aparecem.
4. **Dado** o estado “previsto” ou “realizado” selecionado, **quando** o filtro é aplicado, **então** apenas despesas nesse estado são consideradas e o outro estado não é somado nem exibido como zerado indevidamente.
5. **Dado** que o cliente combina período, categoria e estado, **quando** aplica o filtro, **então** o resultado atende a todos os critérios simultaneamente.
6. **Dado** que o período informado não tem data inicial ou final, ou tem data inicial posterior à final, **quando** o cliente tenta aplicar o filtro, **então** o sistema aponta o problema, não aplica o filtro.
7. **Dado** que nenhuma despesa atende aos filtros, **quando** o relatório é exibido, **então** a tela apresenta um estado de ausência de dados que cita que os filtros podem ter restringido o resultado e permite limpá-los.
8. **Dado** que a consulta do relatório falha, **quando** o cliente aplica filtros, **então** a tela apresenta o estado de erro com opção de tentar novamente, sem mostrar totais parciais como válidos.
9. **Dado** uma categoria renomeada, **quando** o relatório é exibido, **então** o nome atual da categoria é apresentado em todos os meses.
10. **Dado** lançamentos cancelados que atenderiam aos filtros, **quando** os totais são calculados, **então** eles não são somados.
11. **Dado** um intervalo que abranja mais de 24 meses de calendário (contando o mês da data inicial e o da data final), **quando** o cliente tenta aplicá-lo, **então** o sistema recusa a consulta com mensagem que informa o limite de 24 meses, não ajusta o intervalo automaticamente e não apresenta totais; um intervalo de até 24 meses é aceito.

---

### História de usuário 3 — Entender de quais lançamentos vêm os valores (Prioridade: P1)

Como cliente pessoa física, quero abrir a lista de lançamentos que compõem cada total, inclusive as parcelas de compras parceladas, para conferir e confiar nos números apresentados.

**Por que esta prioridade**: Sem a conferência, os totais não são auditáveis pelo cliente; é parte do objetivo declarado da feature.

**Teste independente**: Para cada total exibido, abrir sua lista e comparar a soma dos itens com o total, sob os mesmos filtros e o mesmo critério de data.

**Cenários de aceite**:

1. **Dado** um total do dashboard ou do relatório (por tipo, estado, mês ou categoria), **quando** o cliente solicita ver seus lançamentos, **então** vê a lista dos lançamentos que o compõem, com descrição, tipo, categoria, valor, estado, data de competência, data prevista e, quando houver, data efetiva.
2. **Dado** um total que inclui parcelas de compras parceladas, **quando** o cliente abre sua lista, **então** as parcelas aparecem como itens, identificadas pela compra de origem e pelo número da parcela em relação ao total de parcelas.
3. **Dado** qualquer lista de composição, **quando** seus valores são somados, **então** a soma é exatamente igual ao total exibido na própria lista, sob os mesmos filtros, período, categoria, estado e critério de data; sem mudança nos dados, o total da lista também é igual ao total anterior; se diferir, a tela informa a atualização.
4. **Dado** uma parcela prevista incluída no total do mês, **quando** o cliente a realiza em outra sessão e depois consulta novamente, **então** os totais e as listas passam a refletir o novo estado da parcela e continuam conciliados entre si: a parcela deixa de compor o total previsto, passa a compor o total realizado no mês indicado pelo critério de data em uso, e nenhum valor previsto original é mantido nos relatórios.
5. **Dado** um lançamento realizado (avulso ou parcela) com data efetiva em mês diferente do mês da competência ou da data prevista, **quando** a consulta usa o critério por pagamento/recebimento, **então** o lançamento aparece no mês da data efetiva; **e quando** usa o critério por competência, **então** aparece no mês de sua competência (no caso de parcela, o mês de seu vencimento), nunca em dois meses para o mesmo critério.
6. **Dado** um lançamento cancelado depois de uma consulta anterior, **quando** o cliente consulta novamente, **então** o lançamento não aparece na lista nem nos totais.
7. **Dado** que o cliente abriu uma lista e que os dados mudaram desde a exibição do total, **quando** a lista é carregada, **então** a lista e seu total apresentado na própria lista permanecem conciliados entre si, e a tela informa que os dados foram atualizados se o total diferir do exibido anteriormente.
8. **Dado** uma lista de composição vazia, **quando** o cliente a abre, **então** a tela informa a ausência de lançamentos para aquele total.
9. **Dado** que o cliente deseja corrigir ou consultar o histórico de um lançamento da lista, **quando** aciona a ação, **então** é direcionado às telas e regras já existentes das Features 3 e 4, sem que esta feature duplique essas ações.

---

### História de usuário 4 — Escolher o critério de data e definir minha preferência (Prioridade: P2)

Como cliente pessoa física, quero consultar meus valores por competência ou por pagamento/recebimento e ter um critério padrão salvo, para ver os números do jeito que faz sentido para mim sem escolher a cada consulta.

**Por que esta prioridade**: Os totais já têm valor com um critério único, mas as duas visões respondem a perguntas diferentes e o padrão por cliente reduz atrito.

**Teste independente**: Criar lançamentos cujas datas de competência e de pagamento caem em meses diferentes, consultar com cada critério e confirmar a diferença e a explicação exibida.

**Cenários de aceite**:

1. **Dado** um cliente que nunca escolheu um critério, **quando** abre o dashboard ou um relatório, **então** a consulta usa o critério padrão inicial definido nas premissas e a tela informa qual critério está em uso.
2. **Dado** que a consulta está aberta, **quando** o cliente seleciona outro critério, **então** os totais e as listas são recalculados com o critério selecionado, sem alterar a preferência salva.
3. **Dado** que o cliente seleciona o critério por competência, **quando** a consulta é executada, **então** cada lançamento previsto ou realizado pertence ao mês e ao ano de sua data de competência, e cada parcela pertence ao mês de seu vencimento.
4. **Dado** que o cliente seleciona o critério por pagamento/recebimento, **quando** a consulta é executada, **então** lançamentos previstos usam a data prevista e lançamentos realizados usam a data efetiva.
5. **Dado** o critério selecionado, **quando** a tela é exibida, **então** há um texto visível e compreensível explicando a regra de data em linguagem do cliente (por exemplo, que por pagamento/recebimento os previstos valem pela data prevista e os realizados pela data em que de fato foram pagos ou recebidos).
6. **Dado** qualquer critério, **quando** os totais são exibidos, **então** a distinção entre previsto e realizado é preservada em todos os totais, saldos, agrupamentos e listas.
7. **Dado** que o usuário salva um critério como padrão, **quando** retorna em outra sessão, **então** o critério salvo é o utilizado inicialmente, sem afetar a preferência de outro usuário, inclusive no mesmo tenant.
8. **Dado** que o salvamento da preferência falha, **quando** o cliente tenta salvar, **então** é informado do erro e a preferência anterior permanece.

---

### História de usuário 5 — Ter meus dados financeiros isolados dos de outros clientes (Prioridade: P1)

Como cliente pessoa física, quero que dashboard, relatórios e listas mostrem somente meus dados, para que minhas informações financeiras permaneçam privadas.

**Por que esta prioridade**: O produto é multi-tenant e esta feature consolida dados financeiros em um só lugar; um vazamento teria impacto direto de confidencialidade.

**Teste independente**: Criar lançamentos, parcelas e categorias em dois tenants distintos, consultar como cada cliente e tentar acessar dados do outro por identificadores e URLs manipulados.

**Cenários de aceite**:

1. **Dado** dois tenants com lançamentos, parcelas e categorias distintos nos mesmos meses, **quando** cada cliente consulta o dashboard, os relatórios e as listas, **então** cada um vê totais, agrupamentos e itens exclusivamente do próprio tenant.
2. **Dado** um identificador de categoria de outro tenant informado em um filtro ou em uma URL, **quando** o cliente aplica a consulta, **então** a consulta é recusada sem revelar existência, nome ou valores dessa categoria.
3. **Dado** qualquer parâmetro de tenant, período ou critério manipulado em uma URL, **quando** a consulta é feita, **então** o tenant continua sendo o do contexto autenticado e nunca o informado pelo cliente.
4. **Dado** um usuário não autenticado, sem acesso à área financeira do cliente, de cliente desativado ou de tenant ainda não pronto, **quando** tenta acessar dashboard ou relatórios diretamente, ou **quando** uma sessão já aberta tenta nova consulta, **então** o acesso é recusado no servidor.
5. **Dado** a preferência de critério de um cliente, **quando** outro cliente consulta, **então** só a preferência do próprio cliente é lida e alterada.

### Casos extremos

- Mês sem lançamentos, ou apenas com lançamentos cancelados: estado de ausência de dados; nenhum valor fictício.
- Compra parcelada com parcelas em estados mistos (realizadas, previstas e canceladas): somente as parcelas não canceladas entram, cada uma em seu estado.
- Parcela prevista que vence no último dia do mês, ou parcela de mês curto que teve o vencimento ajustado: pertence ao mês do vencimento efetivo; nenhum valor é duplicado ou omitido na virada de mês.
- Lançamento realizado com data efetiva em mês diferente da data prevista ou da competência: aparece em meses diferentes conforme o critério e nunca em dois meses para o mesmo critério.
- Entradas e despesas na mesma categoria: o agrupamento de despesas por categoria considera somente despesas; entradas não alteram os valores de categoria.
- Mudança de estado, correção ou cancelamento de um lançamento entre a exibição do total e a abertura da lista: lista e total da lista permanecem conciliados e o cliente é informado da atualização.
- Soma de muitos lançamentos acima do limite individual de R$ 9.999.999.999.999.999,99: o total nunca é arredondado, truncado ou apresentado de forma incorreta; se não for possível exibir com exatidão, a consulta falha explicitamente em vez de inventar valor.
- Categoria renomeada: nome atual exibido em todos os resultados.
- Saldo negativo, saldo zero e valores muito grandes devem manter formatação monetária legível e consistente.
- Falha parcial de consulta (por exemplo, totais carregados e lista indisponível): o erro é isolado e identificado, sem apresentar o resultado parcial como completo.
- Mudança de critério de data com filtros ativos: filtros de categoria e estado são preservados; o período é reinterpretado conforme o novo critério.
- Cliente sem categorias com despesas no período: o agrupamento por categoria exibe estado de ausência de dados e não categorias vazias fictícias.

## Requisitos *(obrigatório)*

### Requisitos funcionais

- **RF-001**: A funcionalidade deve estar disponível a clientes autenticados e reutilizar, sem reimplementar nem alterar, as regras de lançamentos avulsos, compras parceladas, categorias, auditoria, isolamento por tenant e identidade visual existentes, tratando as divergências D-001 a D-007 sem alterar as regras das Features 1 a 4.
- **RF-002**: O dashboard deve permitir selecionar um período mensal (mês e ano) e exibir, para esse mês, os totais separados de entradas previstas, entradas realizadas, despesas previstas e despesas realizadas. O período mensal compreende os lançamentos cuja data de referência, conforme o critério de data em uso (RF-012 e RF-013), cai entre o primeiro e o último dia do mês selecionado.
- **RF-003**: O dashboard deve exibir o saldo realizado, calculado como entradas realizadas menos despesas realizadas, e o saldo previsto, rotulado “ainda a realizar” e calculado como entradas previstas menos despesas previstas, considerando somente lançamentos que continuam previstos (estado atual); lançamentos já realizados não entram nesse saldo. A tela deve explicar em texto que esse saldo representa o que ainda falta entrar ou sair no período. Quando não houver lançamentos previstos no período, o saldo “ainda a realizar” é R$ 0,00 e mantém o rótulo e o texto de apoio. Saldos negativos são exibidos com sinal de menos e a palavra “negativo”, tanto o saldo realizado quanto o saldo “ainda a realizar”.
- **RF-004**: A interface deve indicar de forma textual, permanentemente visível junto aos saldos (sem depender de interação como passar o ponteiro sobre um elemento), que saldos e totais representam apenas os lançamentos registrados e que valores previstos ou realizados não correspondem ao saldo bancário do cliente.
- **RF-005**: O dashboard deve exibir as despesas do período agrupadas por categoria, distinguindo valores previstos de realizados em cada categoria.
- **RF-006**: Os relatórios devem exibir despesas agrupadas por mês e por categoria, com previsto e realizado distinguidos. Entradas não compõem agrupamentos por categoria nem aparecem nos relatórios; elas constam apenas nos totais do dashboard.
- **RF-007**: Os relatórios devem permitir filtrar por período, categoria e estado (previsto ou realizado), combinando os critérios informados. O período dos relatórios exige data inicial e final, inclui ambos os limites e rejeita intervalo em que a data inicial seja posterior à final, em alinhamento com a regra da Feature 3. Os relatórios aceitam intervalos de até 24 meses de calendário (contando o mês da data inicial e o da data final); intervalos maiores são recusados com mensagem, sem ajuste automático. O dashboard aceita qualquer mês, inclusive meses futuros com parcelas. A contagem de 24 meses de calendário inclui o mês da data inicial e o da data final: de 01/01/2025 a 31/12/2026 são 24 meses (aceito); de 01/01/2025 a 01/01/2027 são 25 meses (recusado). A aplicação do período depende do critério: no critério por pagamento/recebimento, a data de referência deve estar entre a data inicial e a final, inclusive; no critério por competência, o período é aplicado em meses de calendário, isto é, entram todos os lançamentos cuja competência está entre o mês da data inicial e o mês da data final.
- **RF-008**: O cliente deve poder abrir a lista dos lançamentos que compõem cada um dos quatro totais do dashboard, cada valor (previsto ou realizado) por categoria no dashboard e cada total de mês, total de categoria, célula mês/categoria e total geral dos relatórios. Os saldos realizado e “ainda a realizar” não têm lista própria; a tela indica os totais que os compõem. A lista inclui lançamentos avulsos e parcelas de compras parceladas e cada item apresenta descrição, tipo, categoria, valor, estado, data de competência, data prevista e, quando houver, data efetiva; para parcelas, apresenta também a compra de origem e o número da parcela em relação ao total de parcelas. A lista é sempre completa para o total (sem truncamento); a apresentação pode ser paginada ou rolável, mas o total exibido e a quantidade de itens se referem ao conjunto completo. O volume de referência é de até 5.000 itens por lista.
- **RF-009**: O total exibido em cada lista de composição deve conciliar exatamente, em centavos, com a soma de seus lançamentos, sob os mesmos filtros, período, estado, categoria e critério de data. Sem mudança nos dados, deve também coincidir com o total anterior; se diferir, a tela informa a atualização. Totais e saldos não podem ser arredondados nem calculados com representação inexata; a unidade de conta é o centavo e valores são somados sem conversão aproximada. Se uma soma não puder ser representada com exatidão, a consulta falha explicitamente com o estado de erro, sem exibir valor aproximado.
- **RF-010**: Lançamentos cancelados não devem entrar em nenhum total, saldo, agrupamento ou lista de composição desta feature.
- **RF-011**: A consulta deve permitir escolher entre os critérios “competência” e “pagamento/recebimento”.
- **RF-012**: Pelo critério de competência, cada lançamento previsto ou realizado pertence ao mês e ano de sua data de competência, e cada parcela pertence ao mês e ano de seu vencimento. Lançamentos cancelados não têm data de referência e nunca são considerados (RF-010).
- **RF-013**: Pelo critério de pagamento/recebimento, lançamentos previstos devem ser considerados na data prevista e lançamentos realizados na data efetiva. Lançamentos cancelados não têm data de referência e nunca são considerados (RF-010).
- **RF-014**: A interface deve explicar, em português claro e junto à consulta, a regra de data do critério selecionado, incluindo que, no critério por pagamento/recebimento, previstos usam a data prevista e realizados usam a data efetiva. A explicação deve ficar permanentemente visível (sem depender de interação) e, para cada critério, dizer qual data é usada para lançamentos previstos e para realizados e, no critério por competência, que cada parcela conta no mês do seu vencimento.
- **RF-015**: Cada usuário deve ter uma preferência padrão de critério de data independente, inclusive dos demais usuários do mesmo tenant, aplicada ao abrir a consulta e alterável na própria consulta; o critério inicial para quem nunca escolheu é o definido nas premissas. A identidade do usuário e o tenant são obtidos do contexto autenticado no servidor. A alteração pontual na consulta não modifica a preferência salva; a preferência só muda por ação explícita do usuário. Se a preferência do mesmo usuário for salva em duas sessões quase ao mesmo tempo, prevalece a última gravação e as consultas seguintes usam sempre o valor mais recente.
- **RF-016**: A distinção entre previsto e realizado deve ser preservada em qualquer visão, critério, total, saldo, agrupamento e lista.
- **RF-017**: Quando um lançamento ou parcela mudar de estado, os totais e listas devem refletir o estado vigente na consulta e permanecer conciliados entre si. A parcela que passou de prevista a realizada apenas migra: deixa de compor o total previsto e passa a compor o realizado, no mês indicado pelo critério de data em uso, sem que os relatórios mantenham o valor previsto original nem totais de comparação entre previsto e realizado.
- **RF-018**: O dashboard e os relatórios devem apresentar estados distintos e claros para carregamento, ausência de dados, erro de consulta e resultado, seguindo os padrões da Feature 2. Falhas nunca devem ser exibidas como zero nem como sucesso. Esses estados devem existir separadamente para o dashboard, o relatório, a composição de totais e o salvamento da preferência. Se a composição falhar, os totais já exibidos permanecem e o erro é indicado na região da composição; se os totais falharem, nenhuma composição é oferecida. A ação “tentar novamente” repete a mesma consulta com os mesmos parâmetros. O salvamento da preferência apresenta estados de salvamento em andamento, sucesso e erro; na ausência de preferência salva, competência é o padrão, observada a precedência do critério da URL e da sessão. Se o salvamento falhar, a preferência anterior permanece salva, o critério escolhido na tela continua aplicado à consulta e o usuário pode tentar salvá-lo novamente.
- **RF-019**: Todas as consultas, listas, filtros e preferências devem ser restritos ao tenant autenticado no servidor, derivado do contexto autenticado, inclusive em acessos diretos por URL, filtros e identificadores manipulados. Identificadores de categoria de outro tenant devem ser recusados sem revelar dados; a resposta deve ser indistinguível da resposta a uma categoria inexistente. Usuários não autenticados, sem acesso ativo, de cliente desativado ou de tenant ainda não pronto são tratados conforme a autorização existente da Feature 1; em uma sessão já aberta, a próxima consulta ou gravação de preferência é recusada.
- **RF-020**: A feature é somente de leitura sobre lançamentos, parcelas e categorias: não pode criar, alterar, realizar, corrigir nem cancelar nenhum deles. Alterações permanecem nas Features 3 e 4. A composição pode oferecer apenas links de navegação para as telas existentes dessas features; nenhuma ação de alteração é executada nesta feature.
- **RF-021**: Valores monetários devem ser exibidos em reais no formato brasileiro, com símbolo R$, ponto como separador de milhar e vírgula como separador decimal sempre com duas casas (por exemplo, R$ 1.234,56 e R$ 0,00); valores negativos recebem sinal de menos (por exemplo, -R$ 200,00) e a palavra “negativo” quando forem saldos. Datas no formato dia/mês/ano (por exemplo, 30/10/2026), de forma consistente em todas as telas, totais, listas e mensagens. Meses devem ser apresentados por nome e ano em português (por exemplo, “outubro de 2026”).
- **RF-022**: A apresentação deve reutilizar a identidade visual, os componentes e a navegação da área do cliente já existentes, adaptar-se a celular (360 px), tablet (768 px) e desktop (1280 px) sem rolagem horizontal para alcançar conteúdo ou ações, e ser utilizável por teclado, com rótulos e mensagens perceptíveis por tecnologias assistivas e sem depender apenas de cor. Em larguras estreitas, tabelas e listas devem se reorganizar em blocos empilhados que mantêm todas as informações acessíveis. Mudanças de resultado (carregamento concluído, erro, ausência de dados) devem ser anunciadas a tecnologias assistivas; ao abrir a composição, o foco vai para o seu título e, ao fechá-la, retorna ao controle que a abriu; erros de filtro são associados aos respectivos campos.
- **RF-023**: A alteração da preferência de critério deve gerar registro estruturado com responsável e instante, sem dados pessoais desnecessários, em linha com os registros de ações relevantes já adotados no produto. A leitura de dashboard e relatórios não gera eventos de auditoria financeira.
- **RF-024**: Erros e falhas de consulta devem ser registrados em logs estruturados sem expor valores financeiros, segredos ou dados pessoais além do necessário, e a mensagem apresentada ao cliente não deve revelar detalhes técnicos. Os registros podem conter operação, resultado, identificadores do ator e do tenant, instante e correlação; não podem conter valores financeiros, descrições de lançamentos, nomes de categorias, senhas ou tokens. Mensagens de erro e parâmetros de URL não devem conter valores financeiros; a URL carrega apenas mês, critério, período, categoria e estado.
- **RF-025**: A feature não deve exibir gráficos, valores ou dados fictícios nem depender deles; qualquer elemento visual deve representar dados reais conciliáveis com as listas.
- **RF-026**: Os lançamentos de um mês ou período devem ser contados uma única vez por consulta: nenhum lançamento pode aparecer em dois meses sob o mesmo critério nem ser somado duas vezes em um mesmo total.
- **RF-027**: O dashboard e os relatórios devem ser acessíveis pela navegação da área do cliente, com os destinos “Dashboard” e “Relatórios”; o destino inicial da área do cliente permanece inalterado. O critério de data em uso é mantido ao alternar entre as duas telas na mesma sessão; mês, período e filtros são independentes em cada tela. Ao trocar o critério com filtros ativos, os filtros de categoria e estado são preservados e o período é reinterpretado conforme o novo critério. Ao limpar os filtros do relatório, o intervalo retorna ao mês corrente inicial e os filtros de categoria e estado são removidos, sem alterar o critério em uso nem a preferência salva.
- **RF-028**: A data corrente que define o mês inicial do dashboard e o intervalo inicial dos relatórios (mês corrente completo) é a data do servidor da aplicação, seguindo a convenção das telas existentes.

### Definições de termos

- **Previsto**: lançamento ou parcela ainda no estado previsto, isto é, que não foi realizado nem cancelado.
- **Realizado**: lançamento ou parcela no estado realizado, com data efetiva.
- **Ainda a realizar**: rótulo exibido ao cliente para o saldo previsto (entradas previstas menos despesas previstas, somente previstos).
- **Data de referência**: a data que define a que mês um lançamento pertence, conforme o critério (RF-012 e RF-013).
- **Composição**: lista completa dos lançamentos que formam um total (RF-008).

### Entidades principais

- **Período mensal**: mês e ano selecionados no dashboard; delimita os lançamentos considerados segundo o critério de data.
- **Intervalo de relatório**: período com data inicial e final, usado nos relatórios, sujeito ao limite de 24 meses de calendário.
- **Critério de data**: regra que define a qual mês um lançamento pertence: competência ou pagamento/recebimento.
- **Preferência de critério do usuário**: critério padrão individual do usuário autenticado, pertencente ao seu tenant, com responsável e instante da última alteração.
- **Lançamento considerado**: lançamento avulso ou parcela, previsto ou realizado, não cancelado, do tenant autenticado, com tipo, valor, categoria, estado e datas.
- **Total agregado**: soma, em centavos exatos, de lançamentos considerados por tipo, estado, mês e/ou categoria, sempre acompanhada da lista que o compõe.
- **Saldo realizado e saldo previsto**: diferenças entre entradas e despesas realizadas e previstas (ainda a realizar), respectivamente, conforme RF-003.
- **Parcela**: despesa mensal numerada de uma compra parcelada, conforme Feature 4.
- **Categoria**: classificação existente no tenant, conforme Feature 3.

## Dúvidas em aberto

Todas as dúvidas registradas na especificação inicial foram esclarecidas e estão resolvidas na seção “Clarificações”. Nenhuma dúvida de domínio permanece em aberto.

- **DV-001 — Resolvida** (ver Clarificações, sessão 2026-10-30): saldo previsto “ainda a realizar”, somente com lançamentos que continuam previstos.
- **DV-002 — Resolvida** (ver Clarificações, sessão 2026-10-30): dashboard sem limite de mês; relatórios com até 24 meses de calendário por consulta, com recusa por mensagem acima disso.
- **DV-003 — Resolvida** (ver Clarificações, sessão 2026-10-30): a parcela apenas migra de previsto para realizado, sem preservar a previsão original nos relatórios.

## Critérios de sucesso *(obrigatório)*

### Resultados mensuráveis

- **CS-001**: Em 100% dos testes com conjuntos conhecidos de lançamentos e parcelas, os totais de entradas e despesas previstas e realizadas e o saldo realizado do mês conferem exatamente, em centavos, com o resultado esperado calculado de forma independente, nos dois critérios de data.
- **CS-002**: Em 100% dos testes de conciliação, a soma dos itens da lista de composição de cada total, dos agrupamentos por categoria e dos agrupamentos por mês é exatamente igual ao total exibido na própria lista sob os mesmos filtros e critério; sem mudança nos dados, também coincide com o total anterior; quando diferir dele, a atualização é informada.
- **CS-003**: Em 100% dos testes, lançamentos cancelados, de qualquer origem, não alteram nenhum total, saldo, agrupamento ou lista.
- **CS-004**: Em 100% dos testes com dois tenants, nenhuma consulta, lista, filtro, identificador ou URL manipulada revela ou altera dados, categorias ou preferências do outro tenant.
- **CS-005**: Em 100% dos testes de mudança de estado de uma parcela (prevista para realizada, inclusive com data efetiva em outro mês), totais e listas permanecem conciliados após a mudança, e a parcela deixa de compor o total previsto e passa a compor o realizado.
- **CS-006**: Em 100% dos testes de critério de data, cada lançamento e cada parcela aparece no mês correto conforme as regras de competência e de pagamento/recebimento, e a distinção entre previsto e realizado é preservada.
- **CS-007**: Em 100% dos testes de mês ou filtro sem dados e de falha de consulta, a tela apresenta, respectivamente, o estado de ausência de dados e o estado de erro, e nenhum deles é apresentado como valores zerados de sucesso.
- **CS-008**: Em um teste moderado de usabilidade com pelo menos cinco clientes pessoa física (ou perfil equivalente), pelo menos 90% identificam, sem orientação do moderador, o saldo realizado do mês, que os valores previstos não são saldo bancário e qual regra de data está em uso.
- **CS-009**: Em um teste moderado de usabilidade com pelo menos cinco clientes pessoa física (ou perfil equivalente), pelo menos 90% conseguem, sem orientação do moderador, abrir a lista de lançamentos que compõem um total de despesa de uma categoria e localizar uma parcela de compra parcelada nela, em até dois minutos.
- **CS-010**: Em 100% dos testes de apresentação em 360 px, 768 px e 1280 px, dashboard, relatórios e listas são utilizáveis sem rolagem horizontal, por teclado e com valores e datas no formato brasileiro.
- **CS-011**: O dashboard de um mês com até 5.000 lançamentos do cliente apresenta seus totais em até 3 segundos em uso normal, com indicador de carregamento enquanto aguarda.
- **CS-012**: Em 100% dos testes de limite de período, intervalos de relatório de até 24 meses de calendário são aceitos, intervalos maiores são recusados com mensagem sem ajuste automático, e o dashboard aceita qualquer mês.

## Premissas

- O público é o mesmo das Features 3 e 4: clientes pessoa física autenticados que possuem contexto de tenant e acesso ativo.
- As regras de estado, datas, valores, categorias, cancelamento, parcelamento e auditoria das Features 3 e 4 são dependências existentes e permanecem inalteradas.
- O critério de data inicial para clientes que ainda não definiram preferência é a competência (premissa assumida, ainda não confirmada). Esta premissa pode ser alterada em clarificação, sem impacto nas demais regras.
- A aplicação do período por critério (competência em meses de calendário; pagamento/recebimento por data exata), a data corrente do servidor (RF-028) e o volume de referência de 5.000 itens por lista (RF-008) são decisões provisórias, mantidas até revisão.
- A preferência de critério só é alterada por ação explícita do cliente; escolher outro critério em uma consulta não a modifica.
- Os relatórios abrangem somente despesas por mês e por categoria, conforme pedido; entradas são apresentadas apenas nos totais do dashboard.
- O filtro de estado dos relatórios oferece somente “previsto” e “realizado”; cancelados nunca são exibidos nesta feature.
- Somente lançamentos do tenant autenticado e de categorias do mesmo tenant são considerados.
- Retenção, exportação, exclusão e backup da preferência de critério seguem as políticas já existentes para dados do cliente; esta feature não cria exceções nem ciclos de vida independentes.
- O mês é uma unidade de calendário sem hora; datas são datas de calendário, sem fuso horário, como nos lançamentos existentes.
- A meta de tempo de resposta de CS-011 é um objetivo de experiência e pode ser revisada no planejamento com base em medições.

## Fora de escopo

- Exportação em CSV, PDF ou outro formato.
- Integração bancária e conciliação com extrato bancário.
- Gestão de cartões de crédito e faturas.
- Cobrança de assinatura e planos.
- Previsões inteligentes, projeções automáticas ou recomendações.
- Gráficos que dependam de dados fictícios ou que não possam ser conciliados com as listas de lançamentos.
- Criação, correção, realização ou cancelamento de lançamentos e parcelas a partir desta feature.
- Alteração das regras das Features 1 a 4.
- Relatórios de entradas por categoria, comparações entre períodos e metas ou orçamentos.
