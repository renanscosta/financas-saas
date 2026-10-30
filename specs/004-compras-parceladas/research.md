# Pesquisa e decisões: Compras parceladas

**Feature**: [spec.md](spec.md)

## Contexto observado

- A solução existente usa C# 14/.NET 10 e projetos separados de Domain, Application, Finance.Infrastructure, Platform.Infrastructure e Web.
- Os dados financeiros residem em bases PostgreSQL independentes por tenant. `TenantFinanceContextFactory` e `ITenantConnectionResolver` obtêm o contexto financeiro a partir da identidade atual; a interface usa a política já existente de acesso ao tenant.
- Categorias, lançamentos manuais, versões de concorrência, eventos de auditoria, feedback da UI e logging financeiro estruturado já estão implementados.
- As telas financeiras de cliente usam Blazor Interactive Server, Identity, MudBlazor, `CustomerLayout` e rotas sob `/cliente/`.
- Há testes unitários de domínio/aplicação, testes de integração PostgreSQL com dois tenants e testes Web para autorização, histórico e feedback.
- O provisionador aplica migrations financeiras a novos tenants. Um runner já permite inventariar, auditar e migrar em lote tenants prontos, com verificação de conjunto e interrupção em falha.

## Decisões

### Reutilizar a arquitetura financeira por tenant

**Decisão**: Adicionar regras e fluxos de parcelamento aos projetos existentes de domínio, aplicação, infraestrutura financeira, Web e testes. Cada operação resolve o contexto de tenant pelo usuário autenticado e usa categorias e lançamentos já existentes.

**Racional**: Mantém a fronteira de segurança e armazenamento que já protege as finanças do cliente; não cria API, persistência paralela, projeto ou autenticação duplicados.

**Alternativas consideradas**:

- Uma base compartilhada com tenant escolhido no formulário: rejeitada por romper o isolamento e permitir seleção de dados alheios.
- Uma API ou serviço separado: rejeitado por não ser necessário para os fluxos da aplicação web existente.
- Regras de negócio na UI ou em tipos de persistência: rejeitado por misturar domínio, interface e infraestrutura.

### Gerar vencimentos sem deriva de mês curto

**Decisão**: Guardar como referência o dia originalmente escolhido e calcular cada vencimento a partir do ano/mês correspondente e desse dia original. Se o dia não existir, usar o último dia do mês; nos meses posteriores, retomar o dia original quando existir.

**Racional**: Implementa a decisão confirmada e impede que o ajuste de fevereiro desloque permanentemente as parcelas seguintes. A competência deriva do mês e ano do vencimento calculado.

**Alternativas consideradas**:

- Propagar o último dia ajustado para os meses subsequentes: rejeitado pela decisão confirmada, pois um vencimento em 31 de janeiro passaria a dia 28 após fevereiro.
- Rejeitar dias inexistentes: rejeitado pela decisão confirmada, que exige ajuste automático.

### Distribuir valores em centavos exatamente

**Decisão**: Representar o total em unidades inteiras de centavo durante a alocação. Distribuir o quociente igualmente e atribuir um centavo adicional às primeiras parcelas, em ordem crescente, até consumir o resto. Em edição, recalcular apenas as parcelas futuras previstas elegíveis, distribuindo o total revisado menos a soma das parcelas preservadas.

**Racional**: Mantém exatidão sem ponto flutuante, conserva os valores das parcelas já protegidas e não altera o total solicitado.

**Alternativas consideradas**:

- Arredondar todas as parcelas ao valor médio: rejeitado porque pode fazer a soma divergir do total.
- Ajustar a última parcela: rejeitado porque a distribuição sequencial de resíduo já está definida na especificação.
- Recalcular parcelas realizadas ou não futuras: rejeitado por destruir dados financeiros que devem permanecer preservados.

### Preservar parcelas protegidas ao editar e cancelar

**Decisão**: Somente parcelas no estado previsto e com vencimento posterior à data da operação são elegíveis. Parcelas realizadas, canceladas ou previstas com vencimento no próprio dia ou no passado permanecem inalteradas; a quantidade e numeração originais não mudam. Edição financeira exige que o total novo cubra a soma das parcelas preservadas; o saldo é distribuído entre as parcelas elegíveis. Se não houver parcelas elegíveis ou se o recálculo não puder ser aplicado integralmente, recusar a alteração sem efeitos parciais.

**Racional**: Aplica a decisão confirmada de proteger parcelas realizadas e mantém o conjunto de lançamentos rastreável, inclusive estados cancelados. Define comportamento estável para datas-limite e falhas de redistribuição.

**Alternativas consideradas**:

- Renumerar ou apagar parcelas realizadas: rejeitado por violar a preservação do histórico financeiro.
- Alterar parcelas vencidas ou com vencimento no dia atual: rejeitado porque o critério aprovado limita alterações a parcelas futuras não realizadas.
- Aceitar uma alteração parcial se algumas parcelas forem inválidas: rejeitado pela integridade financeira e atomicidade.

### Reprogramar vencimentos futuros

**Decisão**: Para editar a agenda, o cliente informa a nova data da parcela prevista elegível de menor número. Essa data precisa ser posterior à data da operação e passa a ser referência das parcelas futuras previstas elegíveis; as demais datas são calculadas em incrementos mensais segundo a diferença entre os números das parcelas, usando o dia da nova data como referência. Parcelas protegidas e a data original do primeiro vencimento permanecem inalteradas. Recusar atomicamente se as novas datas não forem estritamente crescentes pela numeração em relação a qualquer parcela protegida.

**Racional**: Mantém uma agenda mensal numerada, oferece um único ponto de reprogramação para o conjunto futuro e evita ajustes independentes que poderiam criar duplicidade ou inverter a ordem das despesas. Preserva as datas históricas das parcelas não elegíveis.

**Alternativas consideradas**:

- Alterar cada parcela individualmente: rejeitada para evitar intervalos irregulares ou inversões entre número e vencimento.
- Editar apenas o primeiro vencimento sem recalcular a agenda: rejeitada porque deixaria as parcelas seguintes na agenda antiga.
- Permitir qualquer data-base mesmo que esteja no passado ou cruze vencimentos protegidos: rejeitada por conflitar com a definição de parcela futura e com a ordem numerada.

### Usar auditoria e concorrência do domínio financeiro

**Decisão**: Registrar criação, realização, edição e cancelamento como mudanças financeiras auditáveis, incluindo ator, instante, parcelas afetadas, dados/valores anteriores e novos e motivo obrigatório nas edições e cancelamentos. Aplicar mutações do parcelamento como operação atômica e detectar versão desatualizada no conjunto afetado.

**Racional**: Reutiliza práticas de auditoria e controle de concorrência de lançamentos existentes e impede sobrescritas silenciosas ou parcelamentos parcialmente alterados.

**Alternativas consideradas**:

- Alterar parcelas sem registrar eventos: rejeitado pela constituição e pela especificação.
- Persistir cada parcela em uma operação independente: rejeitado por permitir conjunto inconsistente após falha intermediária.
- Deixar a validação de concorrência somente na tela: rejeitado porque acesso direto deve continuar seguro no servidor.

### Interface sem API externa

**Decisão**: Expor a feature por uma rota de cliente autenticada e ações da UI Blazor, reutilizando o layout, política de autorização, mensagens de validação e feedback existentes. Não criar endpoints HTTP financeiros públicos.

**Racional**: A aplicação já apresenta lançamentos diretamente na UI server-side e não há consumidor externo especificado.

**Alternativas consideradas**:

- Criar endpoints REST sem consumidor definido: rejeitado por aumentar superfície pública e escopo desnecessariamente.
- Criar rota acessível sem validação server-side: rejeitado por não garantir isolamento nem autorização.

### Evoluir e disponibilizar o esquema nos tenants existentes

**Decisão**: Criar migration financeira aditiva compatível com o modelo atual e reaproveitar o runner financeiro existente para auditoria e aplicação às bases de tenants prontos. A nova versão só deve expor a UI após o rollout completo dos tenants elegíveis; tenants provisionados posteriormente recebem a migration pelo provisionador existente.

**Racional**: O provisionador habitual não é, por si só, a estratégia de atualização de todos os bancos já prontos. O runner e o processo operacional foram criados precisamente para atualizações multi-tenant verificáveis.

**Alternativas consideradas**:

- Migrar somente durante provisionamento de novos tenants: rejeitado porque tenants existentes permaneceriam sem o esquema necessário.
- Expor a feature enquanto parte dos tenants não foi migrada: rejeitado por falhar para usuários válidos e por criar estados operacionais inconsistentes.
- Criar uma ferramenta paralela de migração: rejeitado porque o runner existente cobre o escopo.

## Clarificações incorporadas

- Mês curto: manter o dia original como referência, ajustar ao último dia do mês quando o dia não existir e voltar ao dia original nos meses subsequentes que o possuam.
- Quantidade máxima: limitar a operação a 1.200 parcelas para controlar o tamanho do lote, mantendo o mínimo de duas.
- Edição após parcela realizada: manter a quantidade original, preservar as parcelas realizadas, permitir mudar descrição/categoria/valor total/vencimentos futuros e redistribuir o saldo nas futuras previstas; não aceitar total inferior à soma já protegida.
- Edição de vencimentos: informar a nova data da primeira parcela futura prevista elegível e recalcular as demais elegíveis mês a mês por diferença de número, preservando as não elegíveis e recusando agenda fora de ordem.
- Cancelamento e edição só afetam parcelas futuras previstas, exigem motivo e mantêm os eventos anteriores.
- Ciclo de vida dos dados: retenção, exportação, exclusão e backup herdam integralmente as políticas técnicas existentes para dados financeiros do tenant; esta feature não define exceções.
