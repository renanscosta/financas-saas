# Contrato de interface: Categorias e lançamentos manuais

**Feature**: [spec.md](../spec.md)

Este contrato descreve as jornadas da interface para o cliente autenticado. Não estabelece API financeira pública nem substitui autorização, validação ou isolamento no servidor.

## Rotas e acesso

| Rota proposta | Conteúdo | Acesso |
|---|---|---|
| `/cliente/lancamentos` | Lista, filtros e ações de cadastro, correção, realização, cancelamento e consulta de histórico | Cliente autenticado com acesso ativo a um tenant pronto |
| `/cliente/categorias` | Lista, cadastro, edição e tentativa de exclusão de categorias | Cliente autenticado com acesso ativo a um tenant pronto |

As rotas utilizam shell de cliente independente da navegação administrativa, introduzido porque esta feature cria a primeira página financeira real do cliente. Não há links para telas futuras sem conteúdo real.

## Identidade, autorização e tenant

- A UI nunca solicita um tenantId para escolher o conjunto de dados.
- A identidade autenticada no servidor determina a resolução de tenant e a conexão financeira autorizada.
- Rotas, carregamentos, histórico e cada mutação verificam acesso no servidor. Ocultar controles não é autorização.
- Requisições diretas com ID de lançamento ou categoria alheios ao tenant não expõem dados nem alteram registros.
- Categoria enviada por formulário é resolvida no mesmo tenant; uma categoria inexistente, inativa ou de outro tenant é recusada.
- Usuário sem autenticação é encaminhado ao login existente; conta sem acesso ativo ou tenant pronto recebe uma resposta de acesso indisponível sem revelar configuração interna.

## Jornada de lançamentos

1. A lista permite combinar período, tipo, categoria e estado. Aplicar o período exige data inicial e final, ambas inclusivas; data inicial posterior à final é rejeitada com validação, sem aplicar o filtro. O período usa data prevista para previstos, data efetiva para realizados e a data baseada no estado anterior para cancelados. Histórico cancelado continua localizável.
2. Cadastro solicita tipo entrada/despesa, descrição, valor BRL positivo com no máximo duas casas decimais, categoria, competência e data prevista de recebimento/pagamento. Valores com mais casas decimais são recusados sem arredondamento. Novo item aparece como previsto.
3. Ação de realização só está disponível para previsto e solicita data efetiva. Concluída com sucesso, apresenta estado realizado e a data informada.
4. Correção solicita os campos editáveis e motivo obrigatório. É permitida em previsto ou realizado; preserva o estado atual e, se realizado, sua data efetiva. Valores com mais de duas casas decimais são recusados sem arredondamento. Exibe no histórico os dados alterados antes/depois, instante, responsável e motivo.
5. Cancelamento de previsto ou realizado exige confirmação explícita e motivo. Apresenta o item como cancelado no histórico, mantém a data de filtro relativa ao estado anterior e o exclui dos totais recalculados após o cancelamento.
6. Eventos de histórico podem ser consultados pelo cliente do tenant, sem controles de edição ou exclusão.

## Jornada de categorias

- Exibir Lanches, Mercado e Pessoal após disponibilização do tenant sem duplicar nomes equivalentes.
- Permitir criar e renomear categoria com nome obrigatório e único dentro do tenant, ignorando caixa e espaços nas extremidades.
- Permitir associação de qualquer categoria a entrada ou despesa.
- Lançamentos existentes exibem o nome atual da categoria; renomear uma categoria atualiza os rótulos desses lançamentos.
- Se a categoria estiver associada a qualquer lançamento, inclusive cancelado, informar que não pode ser excluída.
- Se for a última categoria do tenant, impedir exclusão.

## Validação, feedback e acessibilidade

- Apresentar estados de carregamento, validação, sucesso e erro de forma explícita, seguindo os componentes/estilo de feedback da Feature 2.
- Exibir erros compreensíveis junto aos campos e preservar os dados digitados em falhas que não concluíram mutação.
- Desabilitar submissão duplicada durante operação em andamento.
- Não indicar sucesso antes da confirmação do caso de uso; em falha técnica, informar falha sem apresentar resultado de sucesso.
- Se a versão do lançamento estiver desatualizada em uma correção, realização ou cancelamento, recusar a gravação atomicamente, informar que há alteração mais recente e permitir recarregar/revisar antes de reaplicar.
- Campos possuem rótulos programáticos, mensagens associadas, foco visível, ordem de teclado previsível e semântica de anúncio para leitores de tela.
- Em 360 px, 768 px, 1280 px e larguras intermediárias, ações essenciais permanecem alcançáveis sem rolagem horizontal, respeitando o padrão responsivo da Feature 2.

## Estados vazios e erros

- Sem categorias: mostrar mensagem explicativa e opção de criar categoria; a funcionalidade deve garantir categorias iniciais antes de habilitar o cadastro de lançamento.
- Sem lançamentos: informar que a lista está vazia e oferecer cadastro, sem criar valores demonstrativos.
- Critério de filtro inválido: apontar o campo e manter a lista sem alterações.
- Período incompleto ou invertido: exigir ambas as datas, apontar o campo ausente ou inválido e manter a lista sem aplicar o filtro.
- Versão desatualizada: recusar a mutação, manter os dados e oferecer atualização/revisão.
- Valor com mais de duas casas decimais: recusar sem arredondar ou persistir parcialmente.
- Falha de comunicação ou persistência: preservar formulário quando possível, exibir erro e não afirmar que a alteração ocorreu.
