# Modelo de dados: Compras parceladas

**Feature**: [spec.md](spec.md)

Os registros financeiros pertencem à base isolada do tenant resolvida pela identidade autenticada. Os conceitos abaixo descrevem dados e invariantes do domínio e não prescrevem nomes de classes ou tabelas.

## Compra parcelada

Representa a intenção original de compra que agrupa as despesas mensais correspondentes.

| Campo | Significado | Regras |
|---|---|---|
| Identificador | Identidade do parcelamento | Único na base do tenant |
| Descrição | Texto informado para identificar a compra | Obrigatória; refletida nas despesas geradas |
| Categoria | Classificação existente | Obrigatória e pertencente ao mesmo tenant |
| Valor total | Total informado para a compra | BRL, positivo, no máximo duas casas decimais e limitado pelas regras financeiras existentes |
| Quantidade | Número de parcelas | Inteiro entre 2 e 1.200; permanece estável após a criação |
| Primeiro vencimento original | Data informada na criação | Obrigatória; preservada como dado de origem do parcelamento |
| Data de criação | Instante em que o parcelamento foi registrado | Definida pelo servidor e auditável |
| Controle de versão | Versão observada na leitura | Mutação baseada em versão desatualizada é recusada sem atualização parcial |

Uma compra agrupa exatamente a quantidade informada de despesas numeradas sequencialmente a partir de 1. Os dados originais e o histórico de mudanças não podem ser descartados por correção ou cancelamento de parcelas.

## Parcela / despesa mensal

Representa um lançamento financeiro de despesa associado a um parcelamento.

| Campo | Significado | Regras |
|---|---|---|
| Identificador | Identidade da despesa | Único na base do tenant |
| Parcelamento | Compra de origem | Obrigatório; pertence à mesma base/tenant |
| Número | Posição da parcela | Sequencial de 1 até a quantidade original; não renumerar |
| Descrição | Identificação exibida da compra | Inicialmente igual à descrição do parcelamento; edição só afeta parcelas elegíveis |
| Categoria | Classificação da despesa | Inicialmente igual à categoria informada; toda associação precisa pertencer ao mesmo tenant |
| Valor | Parcela em BRL | Positivo e com precisão de centavos; parcelas da criação somam exatamente o total |
| Vencimento | Data de pagamento prevista | Gerado mensalmente a partir do primeiro vencimento e do dia original de referência |
| Competência | Mês contábil da despesa | Mês e ano do próprio vencimento |
| Estado | Situação do pagamento | Previsto, realizada ou cancelada; inicia prevista |
| Data efetiva | Dia em que o pagamento ocorreu | Obrigatória para uma parcela realizada; ausente enquanto prevista |
| Controle de versão | Versão observada na leitura | Usado para recusar mutações concorrentes obsoletas |

### Cálculo de valores

1. Converter o valor total em centavos inteiros, sem ponto flutuante nem arredondamento.
2. Para `N` parcelas, atribuir a cada uma o quociente inteiro de `totalCentavos / N`.
3. Atribuir um centavo adicional às primeiras `totalCentavos % N` parcelas, em ordem crescente.
4. Verificar que todas as parcelas atendem às regras de valor positivo do lançamento e que sua soma é idêntica ao total.
5. Se o valor informado não puder gerar parcelas válidas, recusar a operação completa sem gravação parcial.

Na edição do total, somar os valores de todas as parcelas preservadas (realizadas, canceladas ou previstas não futuras), subtrair esse total do novo total e distribuir o saldo entre as parcelas futuras previstas elegíveis em ordem crescente. Se o novo total for inferior ao valor preservado, não houver elegíveis ou a distribuição gerar parcela inválida, recusar a edição completa.

### Cálculo de vencimentos

- Para cada número de parcela, determinar o mês correspondente diretamente a partir do mês do primeiro vencimento, sem encadear o dia já ajustado no mês anterior.
- Preservar o dia original como referência; quando ausente no mês de destino, usar o último dia desse mês.
- Nos meses seguintes em que o dia original existir, voltar a utilizá-lo.
- Calcular a competência a partir do ano e mês da data de vencimento resultante.

Exemplos: vencimento inicial em 31 de janeiro produz 28 ou 29 de fevereiro conforme o ano e volta a 31 de março; vencimento inicial em 30 de janeiro usa o último dia de fevereiro e volta a 30 de março; vencimento inicial em 29 de janeiro também usa 28 em fevereiro comum e volta a 29 em março.

### Edição da agenda de vencimentos

- O cliente informa uma nova data para a parcela prevista elegível de menor número; essa data precisa ser posterior à data da operação.
- Essa data passa a ser a referência da agenda futura recalculada. Para cada outra parcela prevista elegível, somar à nova data tantos meses quantos forem a diferença entre seu número e o número da primeira parcela elegível, aplicando a regra de mês curto a partir do dia da nova data.
- Parcelas realizadas, canceladas ou previstas não futuras mantêm seus vencimentos originais. A data original de início do parcelamento permanece preservada como dado histórico; a nova referência rege somente as parcelas futuras elegíveis desta edição.
- Os vencimentos recalculados precisam continuar estritamente crescentes pela numeração: cada parcela recalculada deve vencer depois de toda parcela protegida de número menor e antes de toda parcela protegida de número maior. Se qualquer data for igual ou cruzar a data de uma parcela protegida, recusar a edição completa.
- Recalcular a agenda não altera a quantidade, a numeração ou a competência, que continua determinada pelo mês/ano do vencimento vigente.

### Transições

| Estado atual | Ação | Estado resultante | Regras |
|---|---|---|---|
| Inexistente | Criar compra | Previsto | Criar atomicamente todas as despesas numeradas e auditá-las |
| Previsto | Confirmar realização | Realizada | Exige data efetiva; altera apenas a parcela selecionada |
| Previsto futuro | Editar compra | Previsto | Atualiza somente parcelas futuras previstas elegíveis; motivo obrigatório |
| Realizada | Editar ou cancelar compra | Realizada | Parcela preservada, sem alterar valor, vencimento, estado ou data efetiva |
| Cancelada | Editar ou cancelar compra | Cancelada | Parcela preservada; não reativar nem sobrescrever histórico |
| Prevista na data atual ou vencida | Editar/cancelar compra | Prevista | Parcela preservada por não ser futura |
| Previsto futuro | Cancelar compra | Cancelada | Cancela somente parcelas futuras previstas; motivo obrigatório |
| Realizada ou cancelada | Confirmar realização | Sem transição permitida | Estado e data efetiva permanecem preservados |

Operações sobre o conjunto devem verificar versão e elegibilidade no servidor, atualizar parcelas e escrever auditoria atomicamente. Uma operação baseada em leitura obsoleta é recusada, informa conflito e não altera nenhum item.

## Evento de auditoria

Evento imutável relacionado à compra ou às despesas afetadas. A criação da compra e de todas as despesas, junto com seu evento de auditoria inicial, é uma única operação atômica: se qualquer gravação falhar, nenhum dos elementos pode permanecer persistido.

| Campo | Significado | Regras |
|---|---|---|
| Identificador | Identidade do evento | Único |
| Compra e parcelas | Alvo da operação | Identificam compra e conjunto efetivamente afetado, no mesmo tenant |
| Tipo de evento | Criação, realização, edição ou cancelamento | Obrigatório |
| Alterações | Dados anteriores e novos | Na criação, incluir dados financeiros iniciais da compra e de todas as parcelas; nas demais mudanças, incluir campos/valores afetados, vencimentos, valores e estados pertinentes |
| Instante | Momento da operação | UTC, definido pelo servidor |
| Responsável | Usuário autenticado | Derivado do contexto confiável do servidor |
| Motivo | Explicação do cliente | Obrigatório para edição e cancelamento; pode ser vazio para criação e realização |

Os eventos são consultáveis somente pelo tenant proprietário, não podem ser sobrescritos nem removidos pela interface e não devem conter credenciais, tokens ou dados de outro tenant.

## Relações e restrições

- Uma compra parcelada referencia uma categoria do mesmo tenant.
- Uma compra agrega a quantidade original de parcelas; cada parcela pertence a uma única compra.
- Uma categoria pode ser utilizada por diversas compras e lançamentos no mesmo tenant.
- Não expor a escolha de tenant na interface. Consultas e mutações derivam o banco autorizado da identidade e validam associações no servidor.
- Migrações aplicam-se às bases dos tenants por meio do processo existente; qualquer falha bloqueia a liberação da feature para o conjunto de tenants afetado.
- Cartão/fatura, recorrência indefinida, dashboard e relatórios permanecem fora deste modelo.
