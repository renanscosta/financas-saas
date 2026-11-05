# Modelo de dados: Dashboard e relatórios financeiros

**Feature**: [spec.md](spec.md)

Os dados pertencem ao banco isolado do tenant resolvido pela identidade autenticada. Os conceitos abaixo descrevem dados e invariantes e não prescrevem nomes de classes ou tabelas. A feature lê lançamentos, parcelas e categorias existentes sem alterá-los; o único dado novo é a preferência de critério.

## Dados existentes consumidos (somente leitura)

| Conceito | Campos usados | Observação |
|---|---|---|
| Lançamento (avulso ou parcela) | tipo (entrada/despesa), valor BRL com duas casas, categoria, data de competência, data prevista, estado (previsto/realizado/cancelado), data efetiva, estado anterior ao cancelamento | Parcelas e avulsos são lidos em conjunto |
| Parcela | compra de origem, número, vencimento (data prevista) | Competência = primeiro dia do mês do vencimento |
| Compra parcelada | descrição, quantidade de parcelas | Exibida no item de composição |
| Categoria | identificador, nome atual | Lida independentemente do indicador de atividade |

## Preferência de critério do usuário (novo)

| Campo | Significado | Regras |
|---|---|---|
| Usuário | Identificador do usuário titular da preferência | Obtido da identidade autenticada no servidor; único no banco do tenant |
| Critério | Critério padrão de data do usuário | Competência ou pagamento/recebimento |
| Instante da alteração | Momento UTC da última gravação | Definido no servidor |
| Responsável | Usuário autenticado que alterou | Definido no servidor |

- Existe no máximo um registro por usuário em cada tenant; usuários do mesmo tenant têm preferências independentes. Sem registro para o usuário, vale a competência (premissa da especificação).
- Leituras e gravações usam exclusivamente o identificador do usuário autenticado e o banco do tenant autenticado; o identificador não é escolhido por parâmetros da interface.
- Só é gravada por ação explícita de salvar como padrão; mudar o critério em uma consulta não grava.
- Não há histórico da preferência; a alteração gera registro operacional estruturado (RF-023).
- Migration no esquema do tenant, aplicada a novos tenants pelo provisionador e aos existentes pelo runner. Também inclui um índice por competência e estado para as consultas por competência.

## Regras de domínio

### Critério de data

| Critério | Lançamento previsto | Lançamento realizado | Cancelado |
|---|---|---|---|
| Competência | competência | competência | fora |
| Pagamento/recebimento | data prevista | data efetiva | fora |

A data resultante é a **data de referência**; o **mês de referência** é o mês e ano dela. Cada lançamento tem uma única data de referência por critério, portanto aparece em um único mês (RF-026).

### Período

- **Período mensal** (dashboard): primeiro ao último dia de um mês de calendário; qualquer mês é aceito.
- **Intervalo de relatório**: data inicial e final obrigatórias, inclusivas, inicial menor ou igual à final.
- **Limite**: no máximo 24 meses de calendário, contando o mês da data inicial e o da data final. Exemplo: 01/01/2025 a 31/12/2026 (24 meses) é aceito; 01/01/2025 a 01/01/2027 (25 meses) é recusado.
- **Aplicação por critério**: pagamento/recebimento compara a data de referência com as datas exatas; competência compara o mês de referência com os meses de calendário do intervalo (decisão mantida provisoriamente, ver [research.md](research.md)).

### Totais e saldos

Para um conjunto de lançamentos considerados:

- Entradas previstas, entradas realizadas, despesas previstas e despesas realizadas são somas exatas em centavos.
- Saldo realizado = entradas realizadas − despesas realizadas.
- Saldo previsto ("ainda a realizar") = entradas previstas − despesas previstas, considerando somente lançamentos que continuam previstos.
- Saldos podem ser negativos ou zero; nenhum valor é arredondado.
- Estado vazio: nenhum lançamento considerado no período (ausência de dados), distinto de falha de consulta.

### Agrupamentos

| Visão | Chave | Valores |
|---|---|---|
| Despesas por categoria (dashboard) | categoria | previsto e realizado separados |
| Despesas por mês e categoria (relatório) | mês de referência + categoria | previsto e realizado separados; total por mês e total por categoria derivados das mesmas linhas |

Somente despesas entram em agrupamentos por categoria. O filtro de estado do relatório aceita previsto, realizado ou ambos; cancelado nunca é oferecido.

### Composição de um total

Conjunto de lançamentos que atende, ao mesmo tempo, ao critério, ao período, ao tipo, ao estado, à categoria e ao mês de referência que definem o total. Cada item traz:

| Campo | Fonte |
|---|---|
| Descrição, tipo, valor, estado | lançamento |
| Categoria (nome atual) | categoria |
| Data de competência, data prevista, data efetiva (se houver) | lançamento |
| Compra de origem, número e quantidade de parcelas (se for parcela) | parcela e compra |
| Data de referência usada | regra de critério |

Invariante de conciliação: o total exibido na lista é calculado a partir de seus itens. Sem mudança nos dados, ele coincide com o total do qual a lista se originou; se diferir, a interface informa a atualização.

## Transições de estado e efeito nos relatórios

| Mudança no lançamento ou parcela | Efeito |
|---|---|
| Previsto → realizado | Sai do total previsto e entra no realizado, no mês de referência do critério em uso; nenhum valor original é mantido |
| Previsto ou realizado → cancelado | Sai de todos os totais, agrupamentos e listas |
| Correção de valor, categoria ou datas (Feature 3) | Refletida na próxima consulta |

Nenhuma transição é iniciada por esta feature.

## Entidades de resposta (conceituais)

- **Resumo mensal**: quatro totais, saldo realizado, saldo previsto, indicador de ausência de dados e despesas por categoria.
- **Relatório de despesas**: linhas por mês e categoria, totais por mês e por categoria, total geral por estado, indicador de ausência de dados.
- **Composição**: itens, total calculado dos itens e quantidade de itens.
- **Preferência**: critério efetivo e se é o padrão inicial ou foi definido pelo cliente.

## Isolamento

Todas as leituras e a gravação da preferência acontecem no banco do tenant da identidade autenticada. Identificador de categoria recebido do cliente é verificado nesse banco; um identificador inexistente no tenant é recusado com mensagem neutra, sem distinguir "inexistente" de "de outro tenant".
