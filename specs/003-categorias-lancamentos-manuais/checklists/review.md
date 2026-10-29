# Revisão de Requisitos Checklist: Categorias e lançamentos manuais

**Purpose**: Avaliar clareza, completude, consistência e testabilidade dos requisitos antes da revisão de PR
**Created**: 2026-10-27
**Feature**: [spec.md](../spec.md)

**Note**: Checklist personalizado para revisão da qualidade dos requisitos; não avalia a implementação.
**Review Ownership**: Esta checklist é um artefato de revisão de requisitos. Marque `[x]` somente quando o revisor considerar que o critério de qualidade do requisito está satisfeito.
**Marker Semantics**: `[x]` significa que o critério foi revisado e aprovado quanto à qualidade dos requisitos. Não significa que a funcionalidade foi implementada.

## Completude dos requisitos

- [ ] CHK001 - Os requisitos identificam com clareza quais operações o cliente autenticado pode realizar em categorias, lançamentos e históricos? [Completude, Spec §Requisitos funcionais, RF-001–RF-013]
- [ ] CHK002 - A regra de valor BRL especifica a precisão permitida e o tratamento de valores com frações menores que um centavo? [Gap, Spec §Requisitos funcionais, RF-006]
- [ ] CHK003 - Os requisitos esclarecem se editar uma categoria já associada altera o nome apresentado em lançamentos históricos? [Gap, Spec §Requisitos funcionais, RF-003–RF-005]
- [ ] CHK004 - O requisito de categorias iniciais define como a disponibilização e a reconciliação se aplicam a tenants existentes, além de novos tenants? [Completude, Spec §Requisitos funcionais, RF-002]

## Clareza e consistência dos requisitos

- [ ] CHK005 - A comparação de nomes de categoria define suficientemente o que significa ignorar diferenças de maiúsculas/minúsculas para todos os nomes aceitos, inclusive na reconciliação inicial? [Clareza, Spec §Requisitos funcionais, RF-003]
- [ ] CHK006 - Os requisitos enumeram diretamente na especificação todos os campos corrigíveis e deixam claro quais campos ficam imutáveis depois da realização? [Clareza, Spec §Requisitos funcionais, RF-009]
- [ ] CHK007 - A definição de histórico esclarece se valores anteriores e novos abrangem todos os campos alterados, inclusive descrição, tipo, categoria e datas? [Clareza, Spec §Requisitos funcionais, RF-009]
- [ ] CHK008 - O uso de “totais futuros” está consistente com a afirmação de que relatórios agregados e dashboard estão fora do escopo? [Conflito, Spec §Requisitos funcionais, RF-010 e §Fora de escopo]
- [ ] CHK009 - Os requisitos distinguem com precisão a data usada para filtrar lançamentos previstos, realizados e cancelados em todas as transições possíveis? [Consistência, Spec §Requisitos funcionais, RF-011]
- [ ] CHK010 - Os termos “cancelar”, “excluir”, “realizado” e “data efetiva” são usados de forma consistente entre requisitos, cenários e entidades? [Consistência, Spec §Cenários de aceite, §Requisitos funcionais e §Entidades principais]

## Cenários e casos extremos

- [ ] CHK011 - Os cenários de aceite cobrem a alteração do tipo de entrada para despesa (e vice-versa) durante a correção de um lançamento realizado? [Cobertura, Spec §História de usuário 3, RF-009]
- [ ] CHK012 - Os requisitos definem se e como uma correção pode alterar a data prevista de um lançamento realizado sem alterar sua data efetiva? [Clareza, Spec §História de usuário 3, RF-009]
- [ ] CHK013 - Os cenários especificam se uma realização pode ser revertida ou corrigida após erro na data efetiva, ou se o estado realizado é final até o cancelamento? [Gap, Spec §Histórias de usuário 1 e 3, RF-008–RF-009]
- [ ] CHK014 - As regras para conflitos simultâneos cobrem correção, realização e cancelamento baseados em versão obsoleta? [Cobertura, Spec §Histórias de usuário 1, 3 e 4, RF-015]
- [ ] CHK015 - Os requisitos definem o comportamento quando a criação ou renomeação de categorias concorrentes tenta usar o mesmo nome normalizado? [Cobertura, Spec §Requisitos funcionais, RF-003]
- [ ] CHK016 - Os fluxos de falha esclarecem o resultado esperado quando a gravação falha após o cliente iniciar correção, realização ou cancelamento? [Cobertura, Spec §Requisitos funcionais, RF-008–RF-010]

## Qualidade dos critérios de aceite

- [ ] CHK017 - Os critérios de sucesso permitem avaliar objetivamente precisão monetária, filtros combinados, histórico e isolamento entre tenants, sem depender de comportamento não definido? [Mensurabilidade, Spec §Critérios de sucesso, CS-001–CS-007]
- [ ] CHK018 - A meta de dois minutos define condições suficientes para comparação entre avaliações, como quantidade de lançamentos e ponto inicial/final da medição? [Clareza, Spec §Critérios de sucesso, CS-005]
- [ ] CHK019 - O critério de 90% define como selecionar participantes e contabilizar tarefas concluídas, além do número mínimo de participantes? [Mensurabilidade, Spec §Critérios de sucesso, CS-006]

## Segurança, acessibilidade e requisitos não funcionais

- [ ] CHK020 - Os requisitos tornam explícito que consultas, filtros, históricos e mutações aplicam autorização do tenant no servidor, inclusive para acesso por identificadores manipulados? [Cobertura, Spec §Requisitos funcionais, RF-012–RF-013]
- [ ] CHK021 - Os requisitos definem o que a interface informa sem revelar se um identificador inválido pertence a outro tenant? [Gap, Spec §Requisitos funcionais, RF-012–RF-013]
- [ ] CHK022 - As exigências de acessibilidade e responsividade da Feature 2 estão referenciadas como critérios de aceite desta nova interface, em vez de permanecerem apenas como premissa visual? [Dependência, Spec §Premissas, RF-001]
- [ ] CHK023 - A especificação define se existem limites de latência, volume de lançamentos ou disponibilidade necessários para aprovar os fluxos de consulta e gravação? [Gap, Spec §Critérios de sucesso, §Premissas]

## Dependências, rollout e recuperação

- [ ] CHK024 - Os requisitos e premissas definem quem atualiza tenants já provisionados e como confirmar que todos estão aptos antes de liberar a funcionalidade? [Dependência, Spec §Requisitos funcionais, RF-002; Gap]
- [ ] CHK025 - Há requisitos para recuperação ou retomada segura se a atualização das bases de vários tenants falhar parcialmente? [Gap, Exception Flow]
- [ ] CHK026 - Os requisitos definem como resolver nomes de categorias legados duplicados sem perda de dados e sem disponibilizar registros inconsistentes? [Cobertura, Spec §Requisitos funcionais, RF-002–RF-003; Gap]
- [ ] CHK027 - A retenção e a consulta de eventos de auditoria estão definidas de forma compatível com as necessidades de privacidade e conservação de dados financeiros? [Gap, Spec §Entidades principais, §Premissas]
- [ ] CHK028 - Os requisitos distinguem claramente auditoria de correções/cancelamentos dos registros operacionais de segurança e falha exigidos pela governança do projeto? [Consistência, Spec §Requisitos funcionais, RF-009–RF-010; Gap]

## Notas

- Marque itens `[x]` somente após revisão da qualidade do requisito correspondente; os itens não verificam implementação.
- Itens pendentes devem gerar esclarecimentos ou ajustes na especificação antes de serem tratados como requisitos prontos.
- A checklist personalizada é independente de `checklists/requirements.md`, cujo ciclo de vida é mantido por `/speckit-specify` e `/speckit-clarify`.
- `/speckit-implement` lê o estado das checklists como gate e não deve modificar seus marcadores.
- IDs seguem sequência para facilitar referências durante revisão de PR.
