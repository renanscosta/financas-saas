# Modelo de dados: Categorias e lançamentos manuais

**Feature**: [spec.md](spec.md)

Os registros financeiros residem na base PostgreSQL isolada do tenant obtida do usuário autenticado. O identificador do tenant não é selecionável pela interface. Os campos abaixo descrevem os conceitos persistidos, não classes de implementação.

## Categoria

Representa uma classificação que pode ser usada tanto em entradas quanto em despesas.

| Campo | Significado | Regras |
|---|---|---|
| Identificador | Identidade da categoria | Único na base do tenant |
| Nome | Rótulo escolhido pelo cliente | Obrigatório; máximo atual de 120 caracteres; nomes distintos após remoção de espaços nas extremidades e comparação sem distinção de maiúsculas/minúsculas |
| Disponibilidade | Uso em novos lançamentos | Categoria precisa pertencer ao mesmo tenant; categorias associadas a lançamentos não podem ser excluídas; tenant não pode ficar sem categorias |

**Categorias iniciais**: Lanches, Mercado e Pessoal. Na disponibilização inicial da funcionalidade, cada categoria é reconciliada sem criar duplicata segundo a regra canônica. Essa reconciliação não deve reaplicar-se em rollouts posteriores, para preservar renomeações e exclusões feitas pelo cliente. A mesma regra canônica se aplica a criação e renomeação. Persistência e checagem de aplicação devem impedir conflitos concorrentes.

A categoria padrão preexistente e inativa é reativada na disponibilização inicial, preservando identificador e nome. Rollouts posteriores não reativam categorias que tenham sido tornadas inativas depois da disponibilização inicial. Esta feature não adiciona fluxo de ativação/desativação.

Lançamentos associados referenciam a categoria e exibem seu nome atual; renomear a categoria altera o rótulo apresentado também para lançamentos existentes. Não é mantida cópia histórica do nome da categoria.

## Lançamento financeiro

Representa uma entrada ou despesa avulsa em BRL.

| Campo | Significado | Regras |
|---|---|---|
| Identificador | Identidade do lançamento | Único na base do tenant; consultas e mutações sempre limitadas ao tenant autenticado |
| Tipo | Entrada ou despesa | Obrigatório; corrigível sem alterar estado |
| Descrição | Texto explicativo | Obrigatório |
| Valor | Quantia em BRL | Positiva; no máximo duas casas decimais; máximo de R$ 9.999.999.999.999.999,99, igual ao maior valor representável por `numeric(18,2)`; excedentes e valores mais precisos são recusados sem arredondamento; sem ponto flutuante |
| Categoria | Classificação | Obrigatória; referência a categoria do mesmo tenant; utilizável por ambos os tipos |
| Data de competência | Data contábil/competência informada pelo cliente | Obrigatória; corrigível |
| Data prevista | Data prevista para recebimento ou pagamento | Obrigatória; corrigível; rege o filtro enquanto previsto |
| Estado | Previsto, realizado ou cancelado | Inicialmente previsto; transições explícitas; cancelamento lógico |
| Data efetiva | Data em que o recebimento/pagamento aconteceu | Obrigatória ao confirmar realização; não se altera em correção; rege filtro quando realizado |
| Estado anterior ao cancelamento | Estado previsto ou realizado imediatamente anterior ao cancelamento | Preservado para definir a data do filtro e a história do item cancelado |
| Controle de versão | Versão observada na leitura | Alterações baseadas em versão obsoleta são rejeitadas, sem sobrescrita |

### Transições

| Estado atual | Ação | Estado resultante | Regras |
|---|---|---|---|
| Novo | Criar entrada/despesa | Previsto | Campos requeridos e valores válidos |
| Previsto | Confirmar realização | Realizado | Cliente informa data efetiva |
| Previsto | Corrigir | Previsto | Motivo obrigatório; auditar campos alterados |
| Realizado | Corrigir | Realizado | Motivo obrigatório; estado e data efetiva preservados |
| Previsto ou realizado | Cancelar | Cancelado | Motivo obrigatório; registrar estado anterior; manter consultável |
| Cancelado | Corrigir ou cancelar novamente | Sem transição permitida | Estado final; histórico permanece consultável |

Toda correção, realização ou cancelamento verifica atomicamente a versão lida para detectar conflito. A interface informa que há versão mais recente e exige revisão antes da reaplicação.

## Evento de auditoria

Registro imutável de cada correção aceita ou cancelamento.

| Campo | Significado | Regras |
|---|---|---|
| Identificador | Identidade do evento | Único |
| Lançamento | Registro financeiro afetado | Obrigatório; mesma base/tenant |
| Tipo de evento | Correção ou cancelamento | Obrigatório |
| Alterações | Campos e valores anteriores/novos; para cancelamento, inclui o estado anterior | Captura somente os campos afetados e o contexto necessário |
| Instante | Momento da operação | UTC; definido pelo servidor |
| Responsável | Usuário autenticado que fez a alteração | Derivado da identidade confiável no servidor |
| Motivo | Explicação do cliente | Obrigatório e não vazio; consultável pelo tenant |

O evento não registra credenciais, tokens ou dados de outro tenant. Consultas de histórico aplicam o mesmo escopo de tenant do lançamento.

## Consultas e filtros

- Os critérios de período, tipo, categoria e estado podem ser combinados.
- Data de filtro: prevista para estado previsto; efetiva para realizado; data determinada pelo estado anterior ao cancelamento para cancelado.
- O filtro por período exige data inicial e final, inclui ambas as datas e rejeita intervalo em que a data inicial seja posterior à final.
- Cancelados são consultáveis sob o filtro de estado/histórico. Agregações futuras que consumam esses registros devem excluí-los.
- Não há totalização, dashboard ou relatório agregado nesta feature.

## Relações e restrições

- Uma categoria pode ser referenciada por vários lançamentos de entrada ou despesa do mesmo tenant.
- Um lançamento possui uma categoria; a categoria não pode ser removida se houver lançamento associado, inclusive cancelado.
- Um lançamento pode ter vários eventos de auditoria. Eventos não são editáveis nem removidos pela interface do cliente.
- Isolamento é garantido pelo banco/connection string de tenant resolvido no servidor e novamente por validação de categoria e lançamento em cada operação.
- Migrações e reconciliação de categorias executam por banco de tenant. Conflitos legados de nomes equivalentes devem falhar explicitamente e exigir resolução, sem remoção ou mesclagem automática.
- Antes da implantação que exponha as novas rotas, um modo operacional explícito deve percorrer as bases de tenants prontos enquanto o provisionamento de novos tenants estiver suspenso. A execução é por tenant, segura para repetição, encerra com erro em falha parcial e não expõe segredos; a nova versão só é liberada após sucesso para todos os tenants elegíveis e a janela de provisionamento é retomada após a ativação da versão atualizada.
