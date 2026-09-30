# Contratos de Fluxo

Este artefato descreve contratos de comportamento entre a UI, os casos de uso e os
adaptadores. Não define endpoints, classes ou detalhes de implementação.

## Cadastro manual do cliente

**Entrada**: operador autenticado, dados da pessoa cliente e e-mail destinatário.

**Garantias**:

- Cria ou localiza de forma idempotente o cliente e seu tenant inicial.
- O tenant começa não apto ao uso.
- Registra o início do provisionamento e o ator responsável.
- Não aceita tenant ou banco escolhido pelo formulário.

**Falhas observáveis**: dados inválidos, operador sem permissão ou falha de persistência; em
qualquer falha o cliente não é apresentado como pronto.

## Consulta de provisionamento

**Entrada**: operador autenticado e cliente autorizado na plataforma.

**Saída**: estado atual, última tentativa, resultado seguro e indicação de retentativa possível.

**Garantias**: não expõe credenciais, tokens, strings de conexão ou dados de outro tenant.

## Retentativa de provisionamento

**Entrada**: operador autorizado e tenant em estado falho ou incompleto.

**Garantias**:

- Usa o mesmo tenant.
- Preserva dados válidos já criados.
- Executa apenas passos pendentes ou incompletos.
- Não duplica banco lógico, categorias, usuário ou cliente.
- Só marca o tenant como pronto quando todas as invariantes forem satisfeitas.
- Registra cada tentativa e falha de modo auditável.

## Convite e primeiro acesso

**Emissão**: somente para cliente elegível; invalida convite anterior e envia link sem senha.

**Conclusão**: aceita somente convite ativo, dentro de 24 horas, não utilizado, associado ao
cliente correto e com provisionamento concluído.

**Sucesso**: marca o convite como utilizado, conclui o cadastro e habilita o acesso.

**Recusa**: convite expirado, utilizado, invalidado, associado a cliente desativado ou
provisionamento incompleto não pode criar sessão nem habilitar acesso.

## Resolução do tenant para acesso Finance

**Entrada**: identidade autenticada e autorizada.

**Garantias**:

- Obtém o vínculo usuário-tenant no banco da plataforma.
- Verifica cliente e tenant ativos.
- Resolve a conexão pelo catálogo confiável do servidor.
- Nunca usa identificador de tenant fornecido pela UI como fonte de conexão.
- Recusa acesso cruzado, inclusive com identificadores manipulados.

## Desativação

**Entrada**: operador autorizado e cliente ativo.

**Garantias**:

- Preserva dados do cliente e do tenant.
- Bloqueia o usuário.
- Invalida imediatamente convites ativos ou pendentes.
- Impede ativação ou acesso futuro enquanto desativado.
- Registra ator, data, motivo e resultado.

## Serviço de e-mail

**Entrada**: destinatário, link de conclusão e dados mínimos de mensagem.

**Garantias**:

- Nunca recebe ou envia senha.
- Permite capturar a mensagem no Mailpit em desenvolvimento.
- Falha no envio não marca o convite como utilizado nem habilita acesso.
- O adaptador poderá ser substituído por provedor real sem alterar os casos de uso.
