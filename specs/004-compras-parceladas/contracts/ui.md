# Contrato de interface: Compras parceladas

**Feature**: [spec.md](../spec.md)

Este contrato descreve a jornada do cliente na aplicação Blazor existente. Não cria API financeira pública e não substitui autorização, validação ou isolamento no servidor.

## Rota e acesso

| Rota proposta | Conteúdo | Acesso |
|---|---|---|
| `/cliente/compras-parceladas` | Consulta de parcelamentos e parcelas, criação, realização, edição e cancelamento | Cliente autenticado com acesso ativo a um tenant pronto |

A tela usa o layout e os componentes financeiros existentes. Não solicita `tenantId` ou informação de conexão como seletor de dados.

## Identidade, autorização e tenant

- A identidade autenticada no servidor determina o contexto financeiro e a conexão de tenant.
- A rota, as consultas, o histórico e cada mutação verificam acesso no servidor; esconder controles não constitui autorização.
- Identificadores de compras, parcelas ou categorias de outro tenant não podem revelar dados ou permitir alterações.
- Uma categoria enviada por formulário deve ser validada como pertencente ao tenant autorizado.
- Usuário sem autenticação ou sem acesso ativo/pronto segue o tratamento existente de autorização sem exposição de configuração interna.

## Jornada de criação e consulta

1. O formulário de criação solicita descrição, categoria, valor total em BRL, quantidade inteira de parcelas e primeiro vencimento.
2. A quantidade deve estar entre duas e 1.200; os campos são obrigatórios. Valores inválidos ou acima do limite já aceito pelo sistema são recusados sem arredondamento.
3. Ao salvar, o sistema informa explicitamente sucesso somente depois de criar atomicamente a compra e todas as parcelas.
4. Cada parcela apresenta descrição, categoria, número, valor, vencimento, competência e estado.
5. A interface indica a distribuição dos centavos ao usuário; parcelas somam exatamente o valor total informado.
6. Vencimentos seguem o dia original do primeiro vencimento, ajustam para o último dia do mês curto e retomam o dia original quando possível.

## Jornada de realização

- A ação de realizar aparece apenas para parcelas previstas.
- A confirmação solicita a data efetiva do pagamento.
- Ao concluir, somente a parcela selecionada passa a realizada; a data efetiva é exibida e o evento é auditado.
- Falhas de validação, autorização ou persistência não podem apresentar sucesso nem deixar alterações parciais.

## Jornada de edição e cancelamento

- Editar permite alterar descrição, categoria, valor total e a agenda de vencimentos futuros, mantendo a quantidade original e os números.
- Para reprogramar vencimentos, o formulário solicita a nova data da parcela futura prevista elegível de menor número. A data precisa ser posterior à data da operação; as demais parcelas futuras elegíveis são deslocadas mensalmente conforme a diferença entre seus números, usando o dia informado como referência.
- Somente parcelas previstas com vencimento posterior à data da operação são atualizadas. Parcelas realizadas, canceladas, vencidas ou com vencimento no dia da operação são preservadas.
- Se a agenda recalculada coincidir com ou cruzar o vencimento de uma parcela protegida na ordem numérica, recusar a alteração integralmente e manter os vencimentos atuais.
- A edição solicita motivo. Se o total novo for inferior à soma das parcelas preservadas, não houver parcela futura elegível ou o recálculo gerar valor inválido, recusar tudo sem gravação parcial e explicar o problema.
- Centavos residuais do novo saldo são atribuídos primeiro às parcelas elegíveis de menor número; os valores preservados não mudam.
- Cancelar solicita motivo e confirmação explícita; somente parcelas previstas com vencimento posterior à data da operação são canceladas.
- Histórico permanece consultável pelo tenant e mostra ator, instante, parcelas afetadas, valores/dados anteriores e novos e motivo aplicável.
- Uma versão obsoleta ou conflito concorrente é recusado atomicamente e pede que o cliente revise os dados atuais antes de reaplicar.

## Validação, feedback e acessibilidade

- Reutilizar feedback, estilos, validações, estados de carregamento e mensagens já adotados nas páginas financeiras.
- Associar erros aos campos, preservar valores digitados quando seguro e não indicar sucesso antes da confirmação da operação.
- Desabilitar submissão duplicada durante operação em andamento.
- Usar rótulos programáticos, ordem de teclado previsível, foco visível e mensagens acessíveis.
- Preservar comportamento responsivo dos componentes financeiros atuais em larguras de celular, tablet e desktop.

## Estados vazios e erros

- Sem parcelamentos, oferecer o cadastro e não exibir exemplos financeiros fictícios.
- Categoria ausente, inativa ou de outro tenant: explicar que uma categoria válida do próprio tenant é necessária, sem revelar dados externos.
- Dados ausentes, formato monetário inválido, valor total insuficiente para gerar parcelas válidas ou quantidade inválida: apontar validação e não persistir.
- Sem parcelas futuras previstas elegíveis: preservar todos os dados e explicar que nenhuma parcela futura pode ser alterada ou cancelada.
- Falha técnica ou de comunicação: exibir erro, preservar os dados digitados quando possível e nunca sugerir que a operação foi aplicada.
