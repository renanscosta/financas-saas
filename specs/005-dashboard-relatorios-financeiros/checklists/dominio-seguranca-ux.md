# Checklist de qualidade de requisitos: Domínio, segurança e UX

**Purpose**: Validar a clareza, a completude e a consistência dos requisitos de cálculo e conciliação, isolamento e segurança, e experiência do usuário, antes de `/speckit-tasks`
**Created**: 2026-10-30
**Feature**: [spec.md](../spec.md)

**Note**: Este checklist customizado foi gerado por `/speckit-checklist` com base nos requisitos da feature. Profundidade padrão, para revisão por pares antes da implementação.
**Review Ownership**: Este checklist é um artefato de revisão da qualidade dos requisitos, de propriedade do revisor. Marque um item como `[x]` somente quando o revisor concluir que o critério de qualidade dos requisitos foi atendido.
**Marker Semantics**: `[x]` significa que o critério foi revisado e atendido quanto à qualidade dos requisitos. Não significa que o trabalho de implementação esteja concluído.

## Regras de cálculo e conciliação

- [x] CHK001 Os termos "previsto", "realizado" e "ainda a realizar" têm uma definição única e são usados de forma consistente entre RF-002, RF-003, RF-016 e o contrato de interface? [Consistency, Spec §RF-003]
- [x] CHK002 A data de referência por critério está definida para todo estado considerado (previsto e realizado) e a exclusão dos cancelados está explícita em todos os pontos onde totais, agrupamentos e listas aparecem? [Completeness, Spec §RF-010, RF-012, RF-013]
- [x] CHK003 O que acontece com um lançamento realizado cujo mês da data efetiva difere do mês da competência ou da data prevista está descrito para os dois critérios e para totais, agrupamentos e listas? [Coverage, Spec §História 3 cenário 5]
- [x] CHK004 O "período mensal" do dashboard está definido em relação à data de referência de cada critério, sem depender de interpretação? [Clarity, Spec §RF-002]
- [x] CHK005 A conciliação "exata em centavos" é mensurável, com unidade, tratamento de arredondamento e comportamento esperado quando uma soma não puder ser representada? [Measurability, Spec §RF-009, Casos extremos]
- [x] CHK006 O saldo "ainda a realizar" está especificado para meses sem lançamentos previstos, ou só com realizados, incluindo valor, rótulo e texto de apoio? [Edge Case, Spec §RF-003, Clarificações]
- [x] CHK007 Os requisitos sobre mudança de estado de parcela (RF-017, História 3 cenário 4, CS-005) são consistentes com a clarificação de que a previsão original não é preservada, sem texto residual que sugira o contrário? [Consistency, Spec §RF-017]
- [x] CHK008 Está claro que o agrupamento por categoria considera somente despesas e que entradas aparecem apenas nos totais do dashboard, sem composição por categoria? [Clarity, Spec §RF-005, RF-006, Premissas]
- [x] CHK009 "Cada total exibido" (RF-008) enumera quais totais têm composição, inclusive se os saldos realizado e previsto também a têm? [Ambiguity, Spec §RF-008]
- [x] CHK010 Os campos exigidos de cada item de composição, em especial a identificação da compra e o número da parcela, estão completos e coincidem entre RF-008, História 3 e o contrato de interface? [Consistency, Spec §RF-008]
- [x] CHK011 O limite de 24 meses está definido de forma inequívoca (contagem de meses de calendário, inclusão do mês inicial e final) e coincide entre RF-007, História 2 e CS-012? [Consistency, Spec §RF-007, CS-012]

## Isolamento entre tenants e segurança

- [x] CHK012 Os requisitos de isolamento cobrem de forma consistente consultas, listas, filtros, parâmetros de URL e preferência de critério, sem lacunas entre RF-019 e a História 5? [Consistency, Spec §RF-019]
- [x] CHK013 Está especificado que a resposta a uma categoria de outro tenant é indistinguível da resposta a uma categoria inexistente? [Clarity, Spec §História 5 cenário 2]
- [x] CHK014 Estão definidos os requisitos de acesso para usuário autenticado sem acesso ativo, ou com tenant ainda não pronto, e para sessão aberta de um cliente que venha a ser desativado? [Coverage, Spec §História 5 cenário 4]
- [x] CHK015 O que deve e o que não deve constar nos registros de falha e de alteração de preferência (valores financeiros, identificadores, dados pessoais) está definido de forma verificável? [Clarity, Spec §RF-023, RF-024]
- [x] CHK016 Há requisito sobre o conteúdo de mensagens de erro e de parâmetros de URL quanto à exposição de valores ou identificadores? [Gap, Spec §RF-024]
- [x] CHK017 O comportamento da preferência de critério quando salva em duas sessões ao mesmo tempo está especificado? [Gap, Spec §RF-015]
- [x] CHK018 O requisito de somente leitura (RF-020) delimita com clareza o que são "ações dirigidas às Features 3 e 4" na composição, sem criar mutações a partir desta feature? [Clarity, Spec §RF-020, História 3 cenário 9]

## Experiência do usuário e acessibilidade

- [x] CHK019 Os estados de carregamento, ausência de dados, erro e sucesso estão definidos separadamente para dashboard, relatório, composição e salvamento de preferência? [Completeness, Spec §RF-018]
- [x] CHK020 O estado vazio do dashboard cobre tanto mês sem lançamentos quanto mês só com cancelados, e o relatório informa que filtros podem restringir o resultado e permite limpá-los? [Clarity, Spec §História 1 cenário 7, História 2 cenário 7]
- [x] CHK021 Está definido o comportamento de falha parcial (por exemplo, totais carregados e composição indisponível), inclusive qual parte da tela indica o erro? [Coverage, Spec §Casos extremos]
- [x] CHK022 O texto "visível e compreensível" da regra de critério e do aviso "não é saldo bancário" é mensurável, com conteúdo ou critérios de aceitação definidos? [Measurability, Spec §RF-004, RF-014]
- [x] CHK023 Os requisitos de saldo negativo (sinal e texto) valem para os saldos realizado e ainda a realizar, considerando que os valores individuais de despesas são positivos? [Consistency, Spec §RF-003, Feature 3 §RF-006]
- [x] CHK024 O comportamento responsivo de tabelas largas em 360 px e 768 px está definido nos requisitos além da ausência de rolagem horizontal? [Clarity, Spec §RF-022]
- [x] CHK025 Os requisitos de acessibilidade cobrem anúncio de mudanças de resultado, ordem de foco após abrir e fechar a composição e associação de erros de filtro aos campos? [Gap, Spec §RF-022]
- [x] CHK026 O formato de exibição de valores negativos, zero, valores muito grandes e meses por extenso está definido, além de "formato brasileiro"? [Clarity, Spec §RF-021]
- [x] CHK027 O comportamento dos filtros e do critério ao navegar entre dashboard e relatórios, e ao trocar o critério com filtros ativos, está especificado por completo? [Gap, Spec §Casos extremos]
- [x] CHK028 Está definido como o cliente acessa o dashboard e os relatórios (destinos de navegação e destino inicial da área do cliente)? [Gap, Spec §RF-022]
- [x] CHK029 Os critérios de sucesso de usabilidade (CS-008, CS-009) definem perfil dos participantes, tarefas e como se mede "sem orientação"? [Measurability, Spec §CS-008, CS-009; Quickstart §Protocolo moderado de usabilidade]

## Recuperação, dependências e premissas

- [x] CHK030 Os requisitos de recuperação após falha de consulta ou de gravação de preferência definem o que o cliente pode refazer e o estado preservado? [Coverage, Spec §RF-018, História 4 cenário 8]
- [x] CHK031 A premissa de critério inicial (competência) é consistente com a História 4 cenário 1 e com RF-015, e está marcada como assumida e não como decisão confirmada? [Assumption, Spec §Premissas]
- [x] CHK032 A dependência das regras das Features 3 e 4 está documentada com as divergências D-001 a D-007, e há requisito que impeça qualquer alteração dessas regras? [Dependency, Spec §RF-001, Reutilização e divergências]

## Lacunas e ambiguidades conhecidas

- [x] CHK033 O filtro de período por competência em meses de calendário (com data exata no pagamento/recebimento) está escrito na especificação, ou apenas no plano, em tensão com "inclui ambos os limites" de RF-007? [Ambiguity, Conflict, Spec §RF-007]
- [x] CHK034 A "data corrente" que define o mês inicial do dashboard e o intervalo inicial dos relatórios está definida (servidor ou fuso do cliente)? [Gap, Spec §RF-002]
- [x] CHK035 O requisito de "lista dos lançamentos que compõem cada total" define se a lista é sempre completa ou admite paginação, e o volume esperado de itens? [Gap, Spec §RF-008, RF-009]

## Notes

- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied
- Leave items unchecked when they still require clarification, correction, or reviewer evaluation
- `/speckit-implement` reads checklist checkbox state as a gate and must not modify markers
- `checklists/requirements.md` has a separate built-in lifecycle maintained by `/speckit-specify` and `/speckit-clarify`
- CHK033 a CHK035 registram pontos já conhecidos e anotados em [research.md](../research.md); não bloqueiam o avanço por decisão do responsável
- CHK029 avalia a definição do protocolo, não o resultado do estudo; a execução permanece prevista em T059 e os critérios de sucesso não são considerados alcançados antes da avaliação.
- Items are numbered sequentially for easy reference
