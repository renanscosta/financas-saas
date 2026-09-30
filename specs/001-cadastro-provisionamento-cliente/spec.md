# Especificação da Feature: Cadastro e Provisionamento de Cliente

**Diretório da feature**: `001-cadastro-provisionamento-cliente`

**Criado em**: 2026-09-29

**Status**: Rascunho

**Entrada**: Cadastro manual, provisionamento e primeiro acesso de um cliente em um SaaS de finanças pessoais.

## Clarificações

### Sessão 2026-09-29

- Q: Operadores da plataforma devem ter acesso às mesmas operações de cadastro, retentativa, emissão de convite e desativação? → A: Separar operadores somente leitura de operadores administradores; apenas administradores executam mutações.
- Q: Quando a conexão de um tenant estiver ausente, inválida ou indisponível, o sistema deve bloquear o acesso e manter o tenant não apto, ou permitir apenas consulta administrativa do estado? → A: Bloquear acesso financeiro e permitir somente consulta administrativa segura do estado e da falha.
- Q: Em que momento o usuário deve ser considerado apto a acessar a área do próprio tenant? → A: Somente após provisionamento bem-sucedido e conclusão do cadastro pelo convite.
- Q: Quando o provisionamento falhar, a retentativa deve continuar usando o mesmo tenant e os dados já criados parcialmente? → A: Retentar no mesmo tenant, preservando dados válidos e concluindo o que falta.
- Q: Qual deve ser o prazo de validade do convite enviado ao cliente? → A: O convite expira em 24 horas.
- Q: Após um convite expirar, o operador deve poder emitir um novo convite para o mesmo cliente? → A: Permitir novo convite, invalidando o anterior e mantendo cliente, tenant e usuário.
- Q: Ao desativar um cliente, os convites ainda válidos ou pendentes devem ser invalidados imediatamente? → A: Invalidar imediatamente convites ativos e bloquear ativação e acesso do cliente desativado.

## Cenários de Usuário e Testes

### História de Usuário 1 - Cadastrar e provisionar cliente (Prioridade: P1)

Como operador da plataforma, quero cadastrar manualmente uma pessoa cliente e acompanhar o provisionamento, para disponibilizar uma conta isolada e pronta para o primeiro acesso.

**Por que esta prioridade**: É o fluxo que inicia a relação com o cliente e cria as condições para que ele use o produto.

**Teste independente**: Pode ser testado cadastrando um cliente, acompanhando cada estado do provisionamento e verificando que a conta só fica disponível após a conclusão bem-sucedida.

**Cenários de aceitação**:

1. **Dado** que o operador informa os dados necessários de uma pessoa cliente, **quando** confirma o cadastro, **então** o sistema registra o cliente como não pronto para uso e inicia o provisionamento.
2. **Dado** que o provisionamento termina com sucesso, **quando** o operador consulta o estado, **então** o cliente aparece como provisionado, mas o usuário só fica apto ao acesso após concluir o cadastro pelo convite, e as categorias Lanches, Mercado e Pessoal estão disponíveis inicialmente.
3. **Dado** que o provisionamento falha, **quando** o operador consulta o estado, **então** o cliente não aparece como pronto para uso e a falha pode ser identificada para orientar uma retentativa.
4. **Dado** que o operador solicita uma retentativa após uma falha, **quando** o provisionamento é executado novamente no mesmo tenant, **então** dados válidos já criados são preservados, somente o que falta é concluído, e não há duplicação nem liberação de acesso indevido.

---

### História de Usuário 2 - Concluir cadastro e realizar primeiro acesso (Prioridade: P1)

Como cliente convidado, quero concluir o cadastro por um link recebido por e-mail e acessar minha área, para começar a usar minhas finanças pessoais sem receber uma senha por e-mail.

**Por que esta prioridade**: O primeiro acesso é necessário para transformar o provisionamento concluído em uso efetivo pelo cliente.

**Teste independente**: Pode ser testado com um convite válido, um convite expirado e um convite já utilizado, verificando as mensagens e os acessos resultantes.

**Cenários de aceitação**:

1. **Dado** que o provisionamento terminou com sucesso e o cliente possui um link de convite válido e ainda não utilizado, **quando** conclui o cadastro, **então** sua conta é ativada para o acesso e o link não pode ser reutilizado.
2. **Dado** que o provisionamento ainda está pendente ou falhou, **quando** o cliente tenta concluir o cadastro pelo convite, **então** o sistema não ativa o acesso à área do tenant.
3. **Dado** que o cliente possui um link expirado, **quando** tenta concluir o cadastro, **então** o sistema recusa a operação e não concede acesso.
4. **Dado** que o cliente já utilizou o link, **quando** tenta utilizá-lo novamente, **então** o sistema recusa a operação e não altera o cadastro concluído.
5. **Dado** que o convite é enviado, **quando** o cliente recebe a mensagem, **então** ela contém um caminho para concluir o cadastro e não contém uma senha.
6. **Dado** que o convite expirou, **quando** o operador emite um novo convite para o mesmo cliente, **então** o convite anterior deixa de ser válido, o novo convite expira em 24 horas, e cliente, tenant e usuário são preservados.

---

### História de Usuário 3 - Desativar cliente e proteger seus dados (Prioridade: P1)

Como operador da plataforma, quero desativar um cliente sem apagar seus dados e impedir seu acesso, para controlar o ciclo de vida da conta preservando o histórico.

**Por que esta prioridade**: A desativação precisa proteger os dados financeiros e impedir o uso de uma conta que deixou de estar ativa.

**Teste independente**: Pode ser testado desativando um cliente apto ao uso, tentando acessar sua área e verificando que os dados permanecem preservados.

**Cenários de aceitação**:

1. **Dado** que um cliente está ativo, **quando** o operador o desativa, **então** o cliente deixa de poder acessar sua área e seus dados não são apagados.
2. **Dado** que um cliente está desativado, **quando** tenta acessar sua área, **então** o acesso é recusado independentemente do identificador utilizado.
3. **Dado** que existem dois clientes distintos, **quando** um usuário tenta manipular identificadores para consultar dados do outro, **então** a consulta é recusada e nenhum dado do outro cliente é exposto.
4. **Dado** que um cliente possui um convite ativo ou pendente, **quando** o operador o desativa, **então** o convite é invalidado imediatamente e não pode ativar o acesso.

### Casos de Erro e Limite

- Um convite expirado ou já utilizado não pode concluir o cadastro, criar uma nova sessão ou reativar uma conta.
- Uma falha parcial no provisionamento não pode resultar em estado apto ao uso.
- Uma retentativa após falha deve continuar no mesmo tenant, preservar dados válidos já criados e não duplicar o cliente, o usuário, as categorias iniciais ou qualquer outro dado provisionado.
- Um cliente desativado não pode acessar sua área, mas seus dados devem permanecer preservados.
- Um identificador ausente, inválido ou pertencente a outro tenant não pode permitir consulta ou alteração de dados.
- Um convite não pode ser usado para concluir o cadastro de uma pessoa diferente da destinatária prevista.
- O reenvio ou tratamento de um convite não pode enviar senha por e-mail.
- Após a expiração, o operador pode emitir um novo convite para o mesmo cliente; o convite anterior é invalidado e não há criação de novo tenant ou usuário.
- Ao desativar um cliente, todos os convites ativos ou pendentes são invalidados imediatamente e não podem ativar o acesso.

## Requisitos

### Requisitos Funcionais

- **RF-001**: O sistema precisa permitir que um operador cadastre manualmente uma pessoa cliente.
- **RF-001A**: O sistema precisa distinguir operadores somente leitura de operadores administradores; somente administradores podem cadastrar clientes, retentar provisionamentos, emitir convites ou desativar clientes.
- **RF-002**: O sistema precisa associar cada cliente a um tenant isolado e a um usuário correspondente.
- **RF-003**: O sistema precisa iniciar o provisionamento sem apresentar o cliente como pronto para uso antes da conclusão bem-sucedida.
- **RF-004**: O sistema precisa permitir que o operador consulte o estado atual do provisionamento e identifique falhas.
- **RF-004A**: Quando a conexão de um tenant estiver ausente, inválida ou indisponível, o sistema precisa bloquear o acesso financeiro, manter o tenant não apto e permitir ao administrador consultar somente o estado e uma descrição segura da falha.
- **RF-005**: O sistema precisa disponibilizar inicialmente as categorias Lanches, Mercado e Pessoal quando o provisionamento for concluído com sucesso.
- **RF-005A**: O sistema precisa ativar o acesso à área do tenant somente depois que o provisionamento for concluído com sucesso e o cliente concluir o cadastro pelo convite.
- **RF-006**: O sistema precisa enviar ao cliente um e-mail com um link para concluir o cadastro da conta.
- **RF-006A**: Se o envio do e-mail falhar, o sistema precisa manter o convite não utilizado,
  o acesso bloqueado e o cliente apto a receber uma nova tentativa, sem marcar a operação
  como concluída.
- **RF-007**: O link de conclusão precisa expirar 24 horas após sua emissão e precisa deixar de ser aceito após o primeiro uso bem-sucedido.
- **RF-007A**: O sistema precisa permitir que o operador emita um novo convite para o mesmo cliente após a expiração, invalidando o convite anterior e preservando o mesmo tenant e usuário.
- **RF-008**: O sistema precisa permitir que o cliente conclua o cadastro sem receber uma senha por e-mail.
- **RF-009**: O sistema precisa permitir retentativas de provisionamento no mesmo tenant, preservando dados válidos já criados, concluindo o que estiver pendente, sem duplicar dados ou conceder acesso indevido.
- **RF-010**: O sistema precisa impedir que uma falha de provisionamento apresente o cliente como apto ao uso.
- **RF-011**: O sistema precisa impedir o acesso de um usuário aos dados de outro cliente, inclusive quando identificadores forem manipulados.
- **RF-012**: O sistema precisa permitir que o operador desative um cliente sem apagar seus dados.
- **RF-013**: O sistema precisa recusar o acesso à área de um cliente desativado, independentemente de o provisionamento e o cadastro terem sido concluídos anteriormente.
- **RF-013A**: O sistema precisa invalidar imediatamente todos os convites ativos ou pendentes quando o cliente for desativado e impedir que eles ativem o acesso.
- **RF-014**: O sistema precisa preservar o estado e o histórico necessários para auditar o cadastro, o provisionamento, os convites, as retentativas e a desativação.
- **RF-015**: O sistema precisa registrar e comunicar falhas de forma compreensível ao operador e ao cliente, sem revelar dados de outro tenant ou informações sensíveis desnecessárias.

### Entidades Principais

- **Cliente**: Pessoa física cadastrada manualmente e titular de um tenant, com estado de atividade e disponibilidade para uso.
- **Tenant**: Limite lógico de isolamento que reúne os dados financeiros e de acesso de um cliente.
- **Usuário**: Identidade associada a um cliente, com permissão para acessar somente a área do próprio tenant quando habilitado.
- **Provisionamento**: Processo de preparação da conta e dos dados iniciais, com estado, resultado, falhas e retentativas rastreáveis.
- **Convite de acesso**: Credencial temporária enviada por e-mail para concluir o cadastro, com validade e uso controlados.
- **Categoria inicial**: Categoria Lanches, Mercado ou Pessoal disponibilizada ao cliente após provisionamento bem-sucedido.

## Critérios de Sucesso

### Resultados Mensuráveis

- **CS-001**: Em 100% dos testes de falha de provisionamento, o cliente permanece não apto ao uso até uma conclusão bem-sucedida.
- **CS-002**: Em 100% dos testes de retentativa, não há duplicação de cliente, usuário ou categorias iniciais.
- **CS-003**: Em 100% dos testes de acesso cruzado, inclusive com manipulação de identificadores, nenhum dado de outro tenant é retornado ou alterado.
- **CS-004**: Em 100% dos testes de convite expirado após 24 horas ou reutilizado, a conclusão do cadastro e o acesso são recusados.
- **CS-005**: 100% dos clientes cujo provisionamento é concluído com sucesso encontram as três categorias iniciais esperadas no primeiro acesso.
- **CS-006**: 100% dos clientes desativados têm o acesso recusado sem perda dos dados preservados.
- **CS-007**: Pelo menos 95% dos operadores conseguem concluir um cadastro manual e identificar seu estado de provisionamento na primeira tentativa, sem orientação adicional.
- **CS-008**: O operador consegue identificar o resultado de uma tentativa de provisionamento em até 1 minuto após a conclusão ou falha do processo.

## Premissas

- O público inicial é composto por pessoas físicas e cada cliente possui um único usuário inicial.
- O cadastro é realizado por um operador autorizado; não há cadastro público nesta feature.
- O convite enviado ao cliente expira 24 horas após sua emissão, e links expirados sempre serão recusados.
- A ativação do acesso ocorre somente após o provisionamento bem-sucedido e a conclusão do cadastro pelo convite.
- Retentativas de provisionamento usam o mesmo tenant e preservam dados válidos já criados; não há recriação integral nem criação de novo tenant.
- Um novo convite após a expiração invalida o anterior e preserva o mesmo cliente, tenant e usuário.
- A desativação invalida imediatamente todos os convites ativos ou pendentes e impede qualquer ativação ou acesso do cliente.
- O sistema possui um meio confiável de enviar e-mail ao destinatário informado pelo operador.
- Se o envio do e-mail falhar, o convite permanece não utilizado, o acesso permanece bloqueado
	e o operador pode iniciar uma nova tentativa sem criar cliente, tenant ou usuário duplicado.
- Para retentativas de provisionamento, “dados válidos” são o tenant já criado, o usuário já
	associado, as migrations já aplicadas e as categorias padrão já persistidas corretamente;
	esses dados não são recriados, enquanto passos ausentes ou incompletos podem ser retomados.
- A conclusão do cadastro inclui a definição segura dos dados necessários para o primeiro acesso, sem envio de senha por e-mail.
- A desativação é reversível apenas se uma regra posterior do produto defini-la; esta feature exige somente a desativação e a preservação dos dados.
- Lançamento financeiro, compra parcelada, dashboard, relatórios e cobrança de assinatura estão fora do escopo desta feature.
