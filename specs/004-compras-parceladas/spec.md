# Especificação da Feature: Compras parceladas

**Feature Branch**: `feature/004-Compras-parceladas`

**Criada**: 2026-10-29

**Status**: Rascunho

**Entrada**: Criar a feature “Compras parceladas” para registrar uma compra como despesas mensais numeradas, acompanhar cada parcela até sua realização e preservar o histórico financeiro ao editar ou cancelar parcelas futuras.

## Cenários e testes do usuário *(obrigatório)*

### História de usuário 1 — Registrar uma compra parcelada (Prioridade: P1)

Como cliente autenticado, quero registrar uma compra informando descrição, categoria, valor total, quantidade de parcelas e primeiro vencimento, para organizar o compromisso financeiro em despesas mensais.

**Por que esta prioridade**: A criação do parcelamento é o fluxo central e disponibiliza os lançamentos necessários para acompanhamento posterior.

**Teste independente**: Registrar uma compra válida e conferir que todas as parcelas foram criadas, numeradas, classificadas no mês de seus vencimentos e somam exatamente o valor total informado.

**Cenários de aceite**:

1. **Dado** um cliente autenticado com uma categoria do próprio tenant, **quando** informa descrição, categoria, valor total positivo, quantidade inteira entre 2 e 1.200 parcelas e primeiro vencimento válido, **então** o sistema cria as despesas previstas numeradas em sequência e todas ficam associadas à compra.
2. **Dado** uma compra de R$ 100,00 dividida em três parcelas, **quando** o sistema cria as despesas, **então** seus valores são R$ 33,34, R$ 33,33 e R$ 33,33, nessa ordem, e a soma é exatamente R$ 100,00.
3. **Dado** um primeiro vencimento em 31 de janeiro, **quando** as parcelas seguintes venceriam em meses que não têm dia 31, **então** vencem no último dia desses meses e voltam ao dia 31 nos meses seguintes que o possuam; cada despesa pertence à competência do mês de seu próprio vencimento.
4. **Dado** uma compra criada com sucesso, **quando** o cliente consulta seu histórico, **então** encontra um evento de auditoria com ator, instante, compra e todas as parcelas criadas, incluindo seus dados financeiros iniciais.
5. **Dado** que descrição, categoria, valor, quantidade ou vencimento estão ausentes ou inválidos, **quando** o cliente tenta registrar a compra, **então** o sistema informa a validação correspondente e não cria a compra, parcelas parciais nem evento de auditoria de criação.
6. **Dado** que a categoria informada pertence a outro tenant, **quando** o cliente tenta registrar a compra, **então** a operação é recusada sem expor dados da categoria nem criar despesas ou evento de auditoria de criação.

### História de usuário 2 — Acompanhar e realizar parcelas (Prioridade: P1)

Como cliente autenticado, quero consultar as parcelas previstas e confirmar manualmente cada pagamento com sua data efetiva, para manter meus compromissos e pagamentos atualizados.

**Por que esta prioridade**: O acompanhamento individual distingue valores previstos de pagamentos realmente efetuados e mantém os lançamentos úteis ao longo do tempo.

**Teste independente**: Consultar as parcelas de uma compra, confirmar uma como realizada informando a data efetiva e verificar seu estado e histórico sem alterar as demais.

**Cenários de aceite**:

1. **Dado** uma compra parcelada, **quando** o cliente consulta suas despesas, **então** cada parcela exibe descrição, categoria, número, valor, vencimento, competência e estado previsto.
2. **Dado** uma parcela prevista do próprio tenant, **quando** o cliente confirma manualmente sua realização e informa a data efetiva do pagamento, **então** somente essa parcela passa a realizada, conserva a data informada e registra a alteração na auditoria.
3. **Dado** uma parcela realizada, **quando** o cliente tenta confirmá-la novamente como prevista ou realizada, **então** o sistema impede uma transição duplicada e preserva o estado e a data efetiva já registrados.
4. **Dado** um cliente de outro tenant, **quando** tenta listar, consultar ou realizar uma parcela por acesso direto ou identificador, **então** não obtém dados da compra e não altera seu estado.

### História de usuário 3 — Editar ou cancelar parcelas futuras (Prioridade: P2)

Como cliente autenticado, quero corrigir ou cancelar as parcelas futuras previstas sem apagar pagamentos concluídos, para ajustar compromissos futuros sem perder a rastreabilidade financeira.

**Por que esta prioridade**: As correções e cancelamentos preservam o histórico e permitem que o cliente mantenha apenas os compromissos futuros que ainda são válidos.

**Teste independente**: Criar um parcelamento, realizar uma parcela, editar dados e cancelar; conferir que somente parcelas futuras previstas foram afetadas e que os pagamentos realizados e seus históricos continuam consultáveis.

**Cenários de aceite**:

1. **Dado** um parcelamento com uma parcela realizada e parcelas futuras previstas, **quando** o cliente edita descrição, categoria, valor total ou vencimentos futuros, **então** a parcela realizada permanece inalterada e as mudanças afetam somente as parcelas futuras previstas e elegíveis; a quantidade original de parcelas é mantida.
2. **Dado** uma edição do valor total após parcelas protegidas, **quando** o novo total é pelo menos igual à soma dos valores dessas parcelas, **então** o saldo restante é distribuído exatamente entre as parcelas futuras elegíveis, com os centavos residuais atribuídos às primeiras parcelas elegíveis.
3. **Dado** uma edição cujo novo total seja inferior à soma das parcelas realizadas e de outras parcelas preservadas, **quando** o cliente tenta salvar, **então** o sistema recusa a alteração, informa o conflito e mantém todas as parcelas inalteradas.
4. **Dado** um parcelamento com parcelas futuras previstas elegíveis, **quando** o cliente altera o vencimento da parcela elegível de menor número, **então** o sistema usa essa data como nova referência e recalcula os vencimentos das demais parcelas futuras elegíveis em incrementos mensais correspondentes à diferença de numeração, mantendo a regra de mês curto e as parcelas protegidas inalteradas.
5. **Dado** uma nova data de referência que não seja posterior à data da operação ou que faça um vencimento recalculado coincidir com ou ultrapassar, na ordem numérica, o vencimento de uma parcela protegida, **quando** o cliente tenta salvar, **então** o sistema recusa toda a edição e mantém os dados originais.
6. **Dado** parcelas previstas com vencimento posterior à data da operação, **quando** o cliente cancela o parcelamento e informa o motivo, **então** somente essas parcelas futuras previstas ficam canceladas, e as parcelas realizadas, já canceladas, atuais ou vencidas permanecem preservadas e consultáveis.
7. **Dado** uma alteração ou cancelamento de parcela futura, **quando** a operação é concluída, **então** o histórico registra o ator, instante, motivo, parcelas afetadas e valores ou dados anteriores e novos, sem apagar os eventos anteriores.
8. **Dado** uma alteração ou cancelamento sem motivo, **quando** o cliente tenta confirmar a operação, **então** o sistema informa que o motivo é obrigatório e mantém as parcelas inalteradas.
9. **Dado** um cliente de outro tenant, **quando** tenta editar, cancelar ou consultar o histórico do parcelamento, **então** não recebe dados nem altera parcelas ou auditoria.

### Casos extremos

- Em fevereiro de ano bissexto, um vencimento no dia 31 ajusta para 29; em fevereiro de ano não bissexto, ajusta para 28; nos meses seguintes, o dia original volta a ser usado quando existir.
- Vencimentos no dia 29 ou 30 também se ajustam ao último dia do mês quando necessário e retomam o dia original nos meses seguintes.
- Para uma divisão com centavos residuais, distribuir um centavo adicional por parcela, começando pela primeira, sem ultrapassar o total.
- Recusar valor total zero, negativo, com mais de duas casas decimais ou superior ao limite já aceito para lançamentos do sistema.
- Recusar quantidade fracionária, inferior a duas ou superior a 1.200 parcelas, ou dado obrigatório inválido, sem criar registros parciais.
- Na edição, parcelas realizadas e parcelas não futuras não são recalculadas nem renumeradas; a quantidade total original é mantida.
- Para alterar vencimentos, o cliente informa a nova data da parcela futura prevista elegível de menor número; as demais datas elegíveis são recalculadas mensalmente pela diferença entre os números das parcelas, com o dia da nova data como referência.
- Recusar alteração da agenda se a nova data-base não for posterior à data da operação ou se qualquer vencimento recalculado não ficar estritamente entre os vencimentos das parcelas protegidas imediatamente anteriores e posteriores na sequência numérica.
- Se não houver parcelas futuras previstas elegíveis, edição financeira e cancelamento não alteram parcelas preservadas e informam que não há parcelas futuras para modificar.
- Se o saldo restante não puder ser distribuído sem tornar uma parcela futura elegível inválida, a edição é recusada sem alteração parcial.
- Acesso direto a parcelas, compra, categoria ou histórico de outro tenant não pode revelar dados nem permitir alterações.
- Falhas durante a criação, realização, edição ou cancelamento não podem deixar apenas parte da operação financeira aplicada.

## Requisitos *(obrigatório)*

### Requisitos funcionais

- **RF-001**: A funcionalidade deve estar disponível a clientes autenticados e reutilizar os lançamentos, categorias, autenticação, isolamento por tenant, auditoria e componentes visuais já existentes.
- **RF-002**: O cliente deve poder registrar uma compra parcelada com descrição, categoria do próprio tenant, valor total positivo, quantidade inteira de 2 a 1.200 parcelas e data do primeiro vencimento.
- **RF-003**: O sistema deve validar os dados obrigatórios, aceitar valores em reais brasileiros com no máximo duas casas decimais e respeitar o limite de valor já aplicado aos lançamentos existentes; qualquer dado inválido deve ser informado sem criar registros parciais.
- **RF-004**: O sistema deve criar para cada compra a quantidade solicitada de despesas previstas, numeradas sequencialmente a partir de 1, usando a descrição, categoria e associação à compra informadas pelo cliente.
- **RF-005**: A soma dos valores das parcelas deve ser exatamente igual ao total informado. Quando a divisão deixar centavos residuais, o sistema deve distribuí-los, um por vez, a partir da primeira parcela.
- **RF-006**: Cada parcela deve ter como vencimento a data mensal correspondente ao primeiro vencimento. Se o dia original não existir em um mês, deve usar o último dia desse mês e retomar o dia original nos meses seguintes que o possuam.
- **RF-007**: A competência de cada despesa deve corresponder ao mês e ano de seu próprio vencimento.
- **RF-008**: O cliente deve poder confirmar manualmente como realizada uma parcela prevista do próprio tenant, informando sua data efetiva de pagamento. A realização deve afetar somente a parcela selecionada e ser auditada.
- **RF-009**: Ao editar uma compra, o sistema deve alterar somente parcelas previstas com vencimento posterior à data da operação. Parcelas realizadas, canceladas e parcelas com vencimento na data da operação ou anterior devem permanecer inalteradas.
- **RF-010**: Após a criação, a quantidade total e a numeração original devem ser mantidas. Edições de descrição, categoria e valor total devem se aplicar somente às parcelas futuras previstas elegíveis; parcelas preservadas devem manter seus dados e histórico. Para alterar vencimentos, o cliente deve informar a nova data da parcela elegível de menor número; o sistema deve recalcular as demais datas elegíveis pela diferença entre seus números de parcela, em incrementos mensais a partir dessa data e usando o dia informado como referência, sem alterar parcelas protegidas.
- **RF-011**: Ao alterar o valor total, o sistema deve recusar um novo total inferior à soma dos valores das parcelas preservadas. O saldo restante deve ser distribuído exatamente entre as parcelas futuras previstas elegíveis, com centavos residuais atribuídos primeiro às parcelas elegíveis de menor número; se a alteração não puder ser aplicada sem valor inválido ou parcial, deve ser recusada integralmente.
- **RF-012**: Ao cancelar uma compra, o sistema deve cancelar somente parcelas previstas com vencimento posterior à data da operação. Parcelas realizadas, já canceladas e parcelas com vencimento na data da operação ou anterior devem permanecer consultáveis e inalteradas.
- **RF-013**: Criação, realização, edição e cancelamento devem produzir histórico auditável que identifique o ator, instante, compra ou parcelas afetadas e os dados financeiros relevantes. A criação deve registrar a compra e todas as despesas iniciais no mesmo evento atômico que cria o parcelamento. Edições e cancelamentos devem exigir e registrar motivo, bem como dados anteriores e novos. Eventos anteriores não podem ser apagados ou sobrescritos.
- **RF-014**: O sistema deve derivar o tenant do contexto autenticado e restringir criação, consulta, alteração, cancelamento e acesso à auditoria aos dados do próprio tenant, inclusive em acessos diretos por identificador.
- **RF-015**: A interface deve apresentar claramente o número, valor, vencimento, competência e estado de cada parcela, seguindo os padrões visuais já existentes.
- **RF-016**: A feature não deve incluir cartão de crédito ou fatura, recorrência indefinida, dashboard ou relatórios.

### Entidades principais

- **Compra parcelada**: compromisso informado pelo cliente, com descrição, categoria, valor total, quantidade de parcelas e primeiro vencimento; agrupa as despesas mensais resultantes.
- **Parcela**: despesa mensal numerada, com valor, vencimento, competência, estado previsto, realizada ou cancelada e, quando realizada, data efetiva de pagamento.
- **Categoria**: classificação existente no tenant do cliente, reutilizada para todas as despesas associadas à compra.
- **Registro de auditoria**: histórico imutável das mudanças financeiras, com ator, instante, dados afetados e motivo quando aplicável.

## Critérios de sucesso *(obrigatório)*

### Resultados mensuráveis

- **CS-001**: Em 100% das compras válidas testadas, a soma em centavos das parcelas corresponde exatamente ao valor total informado, inclusive quando a divisão tem resto.
- **CS-002**: Em 100% dos testes de vencimento para dias 29, 30 e 31, as datas respeitam a regra de ajuste ao último dia do mês e retomada do dia original.
- **CS-003**: Em 100% dos testes de edição e cancelamento após pagamento, as parcelas realizadas e seu histórico permanecem inalterados.
- **CS-004**: Em 100% dos testes com dois tenants, um cliente não consegue consultar ou alterar compras, parcelas, categorias ou auditoria de outro tenant.
- **CS-005**: Em um teste de usabilidade, pelo menos 90% dos clientes conseguem registrar uma compra parcelada válida na primeira tentativa, sem ajuda externa.

## Premissas

- O público é o mesmo dos lançamentos financeiros já existentes, e a funcionalidade reutiliza suas regras de autenticação, moeda (reais brasileiros), categorias e tenant.
- A quantidade informada deve ser um inteiro entre duas e 1.200 parcelas; o limite é técnico e restringe o tamanho de uma operação em lote.
- O saldo em centavos é distribuído sequencialmente: parcelas de menor número recebem primeiro os centavos residuais; na edição, aplica-se a mesma regra às parcelas futuras elegíveis de menor número.
- Para identificar parcelas futuras elegíveis, considera-se futura a parcela prevista com vencimento posterior à data da operação. Parcelas realizadas, canceladas, vencidas ou com vencimento no próprio dia da operação são preservadas.
- Após a criação, a quantidade e os números das parcelas permanecem estáveis. Edições posteriores ao pagamento preservam as parcelas realizadas e distribuem o saldo atualizado somente entre as parcelas futuras elegíveis.
- Alterações e cancelamentos exigem motivo, em alinhamento ao comportamento já adotado para correções e cancelamentos de lançamentos.
- A data efetiva informada ao confirmar uma realização representa o dia em que o pagamento ocorreu.
- Retenção, exportação, exclusão e backup dos registros de compras parceladas seguem integralmente as políticas técnicas existentes para dados financeiros do tenant; esta feature não cria exceções nem ciclos de vida independentes.
- Cartão/fatura, recorrência sem fim, dashboards e relatórios estão fora do escopo desta versão.
