# Contrato de interface: Dashboard e relatórios financeiros

**Feature**: [spec.md](../spec.md)

Este contrato descreve a jornada do cliente na aplicação Blazor existente. Não cria API financeira pública e não substitui autorização, validação ou isolamento no servidor.

## Rotas e acesso

| Rota | Conteúdo | Acesso |
|---|---|---|
| `/cliente/dashboard` | Resumo mensal, saldos, despesas por categoria, critério de data e composição dos totais | Cliente autenticado com acesso ativo a um tenant pronto |
| `/cliente/relatorios` | Despesas por mês e categoria com filtros e composição dos totais | Idem |

Ambas usam o layout e a navegação do cliente (dois novos destinos: "Dashboard" e "Relatórios") e a política de acesso ao tenant já usada nas telas financeiras. Nenhuma tela pede ou aceita `tenantId` ou dados de conexão.

## Parâmetros de consulta (conveniência)

| Parâmetro | Rota | Valores | Tratamento |
|---|---|---|---|
| `mes` | dashboard | `AAAA-MM` | Inválido: usa o mês corrente e informa |
| `criterio` | ambas | `competencia` ou `pagamento` | Inválido: usa a preferência do cliente |
| `inicio`, `fim` | relatórios | datas `AAAA-MM-DD` | Validados como o formulário |
| `categoria` | relatórios | identificador | Verificado no tenant autenticado |
| `estado` | relatórios | `previsto` ou `realizado` | Inválido: ignora e informa |

Parâmetros nunca determinam tenant e todos são revalidados no servidor; ausência de parâmetros é um acesso válido.

Para o critério de data, um parâmetro válido na URL prevalece. Sem parâmetro, usa-se o critério em uso na sessão e, se ainda não houver, a preferência salva do usuário; sem preferência salva, usa-se competência. Um parâmetro inválido usa a preferência salva ou, na sua ausência, competência. O critério aplicado passa a ser o critério em uso na sessão; isso não altera a preferência salva.

## Dashboard

1. Seletor de mês e ano (rótulo "Mês", exibição em português, ex.: "outubro de 2026"), com navegação para mês anterior e seguinte.
2. Seletor de critério de data ("Competência" e "Pagamento/recebimento") e ação separada "Salvar como padrão". Trocar o critério recalcula a consulta e não grava.
3. Texto de regra do critério em uso, visível junto aos seletores:
   - Competência: "Cada lançamento conta no mês da sua competência; cada parcela conta no mês do vencimento."
   - Pagamento/recebimento: "Lançamentos previstos contam na data prevista; lançamentos realizados contam na data em que foram efetivamente pagos ou recebidos."
4. Quatro totais separados: entradas previstas, entradas realizadas, despesas previstas e despesas realizadas, em R$.
5. Saldo realizado e saldo previsto com o rótulo "ainda a realizar", cada um com texto de apoio. Saldo negativo mostra sinal e a palavra "negativo".
6. Aviso permanente: "Os valores refletem apenas os lançamentos registrados. Previsto e realizado não são o saldo da sua conta bancária."
7. Tabela de despesas por categoria com colunas Categoria, Previsto e Realizado.
8. Cada um dos quatro totais e cada valor (previsto ou realizado) por categoria oferece a ação "Ver lançamentos" que abre a composição. Os saldos não têm composição própria; cada saldo indica os totais que o compõem.

## Relatórios

1. Filtros: data inicial, data final, categoria ("Todas" ou uma categoria do tenant), estado ("Todos", "Previsto", "Realizado") e critério de data com a mesma explicação.
2. Validações exibidas junto aos campos: data ausente, data inicial posterior à final e intervalo acima de 24 meses (mensagem cita o limite). Em qualquer validação falha, nenhuma consulta é feita e nenhum total novo é exibido.
3. Tabela de despesas por mês e categoria: um grupo por mês (em português) com total do mês e linhas por categoria, cada uma com Previsto e Realizado; total geral ao final.
4. "Ver lançamentos" em cada célula, total de mês, total de categoria e total geral.
5. Limpar filtros retorna ao intervalo inicial e remove os filtros de categoria e estado, preservando o critério de data em uso e a preferência salva.

Intervalo inicial sugerido: do primeiro dia do mês corrente ao último dia do mês corrente.

## Composição dos totais

- Abre em um painel dentro da própria página, identificado por título que descreve o total ("Despesas realizadas em Mercado, outubro de 2026, por competência").
- Lista: descrição, tipo, categoria, valor, estado, competência, data prevista, data efetiva (quando houver) e, para parcelas, compra de origem e "parcela N de M".
- Mostra o total calculado dos itens e a quantidade de itens. Se o total diferir do exibido antes da abertura, informa: "Os dados foram atualizados desde a última consulta."
- Lista vazia informa a ausência de lançamentos para aquele total.
- Cada item oferece link para a tela existente (Lançamentos ou Compras parceladas) para corrigir ou consultar histórico; o link abre a tela, sem garantir a abertura direta do item. Esta feature não executa mutações.
- A abertura de outra composição enquanto a anterior carrega descarta a resposta antiga.

## Identidade, autorização e tenant

- A identidade autenticada no servidor determina o banco financeiro; esconder controles não é autorização.
- A preferência é individual do usuário autenticado, inclusive entre usuários do mesmo tenant; nenhum parâmetro da interface seleciona o usuário titular.
- Identificadores de categoria são validados no servidor; um identificador inexistente no tenant gera mensagem neutra, sem revelar existência em outro tenant.
- Usuário sem autenticação ou sem acesso ativo segue o tratamento existente de autorização.

## Estados e erros

| Situação | Comportamento |
|---|---|
| Carregando | Indicador de carregamento identificável, com texto; resultado anterior não é apresentado como do novo mês/filtro |
| Ausência de dados | Mensagem explícita e orientação para registrar lançamentos (link para Lançamentos); em relatórios, cita que filtros podem ter restringido o resultado e oferece "Limpar filtros" |
| Falha de consulta | Mensagem compreensível e ação "Tentar novamente"; sem zeros, totais parciais nem detalhes técnicos |
| Sem preferência salva | Competência é o padrão, observada a precedência do critério da URL e da sessão |
| Salvando preferência | Indicador de salvamento em andamento |
| Falha ao salvar preferência | Mensagem de erro e opção de tentar salvar novamente; preferência anterior permanece e o critério escolhido continua em uso |
| Preferência salva | Confirmação textual |

Os estados reutilizam os componentes de feedback existentes. Mensagens e rótulos não dependem apenas de cor ou ícone.

## Formatação e apresentação

- Valores em reais no formato brasileiro (R$ 1.234,56), datas em dd/MM/yyyy, meses por extenso em português.
- Layout reutiliza a identidade visual e componentes existentes; sem rolagem horizontal em 360 px, 768 px e 1280 px; tabelas largas se reorganizam em blocos empilhados em telas estreitas.
- Navegação por teclado, foco visível, rótulos programáticos e anúncio de mudanças de resultado a tecnologias assistivas.
- Sem gráficos, valores ou dados fictícios; nenhuma dependência nova de interface.
