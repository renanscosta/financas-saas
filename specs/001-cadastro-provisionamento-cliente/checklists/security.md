# Checklist de Qualidade dos Requisitos de Segurança e Provisionamento: Cadastro e Provisionamento de Cliente

**Objetivo**: Revisar a completude, clareza e consistência dos requisitos de segurança,
isolamento entre tenants, provisionamento e primeiro acesso.
**Criado em**: 2026-09-29
**Feature**: [spec.md](../spec.md)

**Nota**: Este checklist é gerado pelo comando `/speckit-checklist` com base no contexto e nos requisitos da feature.
**Responsabilidade da revisão**: Este é um artefato de revisão de qualidade dos requisitos, sob responsabilidade do revisor. Marque `[x]` somente quando o critério de qualidade dos requisitos estiver satisfeito.
**Semântica dos marcadores**: `[x]` significa que o critério foi revisado e satisfeito quanto à qualidade dos requisitos. Isso não significa que a implementação foi concluída.

## Completude dos Requisitos

- [x] CHK001 - Os requisitos distinguem claramente as responsabilidades do operador, do cliente convidado e do usuário autenticado? [Completeness, Spec §Cenários de Usuário e Testes]
- [x] CHK002 - O ciclo completo do tenant está documentado desde o cadastro manual até a desativação, incluindo os estados intermediários? [Completeness, Spec §RF-001 a RF-014]
- [x] CHK003 - Os requisitos definem o que permanece preservado quando o cliente é desativado, incluindo dados, histórico, tenant e usuário? [Completeness, Spec §História de Usuário 3, RF-012 a RF-014]
- [x] CHK004 - As regras de falha parcial, retentativa, convite, ativação e desativação cobrem tanto os fluxos principais quanto os alternativos? [Coverage, Spec §Casos de Erro e Limite]
- [x] CHK005 - O requisito de envio de e-mail define o comportamento esperado quando o serviço de e-mail estiver indisponível ou falhar? [Gap, Spec §RF-006, RF-015]

## Clareza e Determinismo

- [x] CHK006 - Termos como “pronto para uso”, “provisionado”, “apto ao acesso”, “ativo” e “desativado” têm estados distintos e não sobrepostos? [Clarity, Spec §RF-003, RF-005A, RF-013]
- [x] CHK007 - A especificação deixa inequívoco que a ativação exige simultaneamente provisionamento concluído e cadastro concluído pelo convite? [Clarity, Spec §RF-005A]
- [x] CHK008 - A regra de expiração está quantificada como 24 horas após a emissão e é consistente entre requisitos, cenários e critérios de sucesso? [Clarity, Spec §RF-007, CS-004]
- [x] CHK009 - O significado de “dados válidos” preservados durante a retentativa está definido de forma suficiente para evitar interpretações diferentes? [Ambiguity, Spec §RF-009]
- [x] CHK010 - A especificação define quais resultados e falhas o operador pode consultar sem expor segredos, tokens ou dados de outro tenant? [Clarity, Spec §RF-004, RF-015]

## Consistência entre Requisitos e Design

- [x] CHK011 - Os requisitos funcionais, o modelo de dados e os contratos usam o mesmo ciclo de estados para provisionamento, convite e acesso? [Consistency, Spec §RF-003 a RF-013, data-model.md §Transições de estado]
- [x] CHK012 - A exigência de um banco PostgreSQL por tenant está claramente separada do banco da plataforma e não contradiz a regra de isolamento? [Consistency, plan.md §Contexto Técnico, data-model.md §Limites de persistência]
- [x] CHK013 - O design mantém a resolução da conexão do tenant no servidor e impede que identificadores da UI determinem o banco acessado? [Consistency, Spec §RF-011, contracts/fluxos.md §Resolução do tenant para acesso Finance]
- [x] CHK014 - O uso de migrations separadas para plataforma e tenants está refletido como requisito de evolução e não apenas como detalhe técnico do plano? [Traceability, plan.md §Restrições, research.md §Entity Framework Core e migrations]
- [x] CHK015 - As decisões de não usar microserviços, mensageria ou transação distribuída estão alinhadas com os requisitos de retentativa e auditabilidade? [Consistency, plan.md §Verificação da Constituição, research.md §Provisionamento idempotente]

## Qualidade dos Cenários e Recuperação

- [x] CHK016 - Há critérios claros para diferenciar uma falha de provisionamento de um provisionamento concluído com sucesso? [Clarity, Spec §História de Usuário 1, RF-010]
- [x] CHK017 - A retentativa especifica o comportamento quando apenas parte dos passos anteriores foi concluída, sem exigir recriação integral? [Recovery, Spec §RF-009, contracts/fluxos.md §Retentativa de provisionamento]
- [x] CHK018 - Os requisitos definem o comportamento para tentativas concorrentes de provisionamento ou de emissão de convite? [Gap, Spec §RF-009, RF-007A]
- [x] CHK019 - Os requisitos distinguem convite expirado, utilizado, invalidado por substituição e invalidado por desativação? [Completeness, Spec §RF-007, RF-007A, RF-013A]
- [x] CHK020 - O fluxo de recuperação de convite expirado preserva explicitamente o mesmo cliente, tenant e usuário, sem duplicação? [Recovery, Spec §RF-007A]

## Isolamento, Autorização e Privacidade

- [x] CHK021 - O requisito de isolamento cobre consulta, alteração e tentativa de acesso por identificadores manipulados, e não apenas visualização? [Coverage, Spec §RF-011, História de Usuário 3]
- [x] CHK022 - Está explícito que a autorização é verificada antes da resolução da conexão do tenant e antes de qualquer acesso aos dados? [Security, plan.md §Restrições, contracts/fluxos.md §Resolução do tenant para acesso Finance]
- [x] CHK023 - Os requisitos definem o comportamento quando o usuário autenticado não possui tenant, possui vínculo inválido ou tenta acessar um tenant desativado? [Gap, Spec §RF-011, RF-013]
- [x] CHK024 - A especificação exige que mensagens de erro sejam úteis para o ator correto sem revelar existência, identificadores ou dados de outro tenant? [Privacy, Spec §RF-015]
- [x] CHK025 - A regra de não enviar senha por e-mail está aplicada a emissão, reenvio, falha e recuperação de convite? [Completeness, Spec §RF-008, Casos de Erro e Limite]

## Auditabilidade e Critérios de Aceitação

- [x] CHK026 - Cada mudança relevante de estado identifica o ator, momento, tenant afetado, resultado e motivo quando aplicável? [Completeness, Spec §RF-014, data-model.md §Registro de auditoria]
- [x] CHK027 - Os critérios de sucesso permitem verificar a idempotência sem confundir ausência de duplicação com simples conclusão do fluxo? [Measurability, Spec §CS-002]
- [x] CHK028 - Os critérios de sucesso cobrem simultaneamente falha de provisionamento, retentativa, convite, desativação e acesso cruzado? [Coverage, Spec §CS-001 a CS-006]
- [x] CHK029 - O limite de até 1 minuto para identificação do resultado do provisionamento está relacionado a um estado observável e não a um detalhe de implementação? [Clarity, Spec §CS-008, plan.md §Metas de Desempenho]
- [x] CHK030 - As premissas e dependências sobre PostgreSQL, migrations e captura de e-mails estão identificadas sem transformar decisões de implementação em regras de negócio? [Dependencies, Spec §Premissas, plan.md §Dependências Principais]

## Notas

- Todos os itens são testes da qualidade dos requisitos, não testes de execução da implementação.
- O checklist deve ser revisado por um responsável antes de marcar qualquer item como `[x]`.
- O arquivo `checklists/requirements.md` possui ciclo de vida próprio mantido por `/speckit-specify` e `/speckit-clarify`.
- `/speckit-implement` lê o estado dos checkboxes, mas não deve modificar seus marcadores.

## Segurança da Fundação

- [x] CHK031 - Os requisitos distinguem claramente a identidade do usuário na plataforma da identidade e dos dados financeiros do tenant? [Clarity, Spec §RF-002, plan.md §Contexto Técnico]
- [x] CHK032 - A exigência de migrations separadas define o que acontece quando a migration da plataforma ou do tenant falha parcialmente? [Recovery, Spec §RF-009, plan.md §Restrições]
- [x] CHK033 - A resolução server-side especifica quais verificações precedem a obtenção da conexão do tenant? [Completeness, Spec §RF-011, contracts/fluxos.md §Resolução do tenant para acesso Finance]
- [x] CHK034 - Os requisitos de auditoria definem quais dados são proibidos nos registros, incluindo credenciais, tokens e strings de conexão? [Privacy, Spec §RF-014/RF-015, data-model.md §Registro de auditoria]
- [x] CHK035 - Os requisitos deixam explícito como um tenant sem configuração de conexão, com conexão inválida ou indisponível deve ser apresentado ao operador? [Gap, Spec §RF-004/RF-015]
- [x] CHK036 - As responsabilidades de autenticação, autorização, resolução de tenant e acesso financeiro estão separadas sem permitir que uma camada de interface escolha o banco? [Consistency, plan.md §Verificação da Constituição, contracts/fluxos.md]
