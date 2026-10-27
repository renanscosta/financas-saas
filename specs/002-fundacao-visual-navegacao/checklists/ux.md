# UX Checklist: Fundação visual e navegação

**Purpose**: Avaliar completude, clareza, consistência e verificabilidade dos requisitos de experiência visual, navegação, feedback e acessibilidade.
**Created**: 2026-09-30
**Feature**: [spec.md](../spec.md)

**Review Ownership**: Checklist de qualidade dos requisitos, pertencente ao revisor. Marque `[x]` somente após avaliar os requisitos escritos.
**Marker Semantics**: `[x]` significa que o critério de qualidade dos requisitos foi revisado e satisfeito; não significa que a implementação foi concluída.

## Completude dos requisitos

- [x] CHK001 - Os requisitos definem quais superfícies existentes recebem a identidade visual comum e quais mantêm apresentação própria? [Completude, Spec §Premissas e Dependências]
- [x] CHK002 - Os requisitos deixam claro se a navegação própria do cliente será entregue agora ou depende de uma tela autenticada existente, sem criar destinos fictícios? [Dependência, Spec §História de Usuário 4, RF-014]
- [x] CHK003 - Os requisitos definem a saída da conta como parte da experiência autenticada e distinguem esse fluxo do primeiro acesso por convite? [Completude, Spec §RF-005, RF-015]
- [x] CHK004 - Os requisitos especificam os estados de carregamento, sucesso, erro e validação para todas as operações existentes abrangidas? [Completude, Spec §RF-008, História de Usuário 3]
- [x] CHK005 - Os requisitos cobrem identificação da página atual e descoberta das páginas autorizadas tanto em desktop quanto em celular? [Cobertura, Spec §RF-003–RF-006, História de Usuário 1]

## Clareza e mensurabilidade

- [x] CHK006 - “Identidade visual consistente” está suficientemente caracterizada quanto à hierarquia, tipografia, cores, espaçamento e componentes para orientar decisões sem impor uma solução de implementação? [Ambiguidade, Spec §RF-001]
- [x] CHK007 - “Acesso fácil” à saída da conta e às opções de navegação tem critério observável ou permanece subjetivo? [Ambiguidade, Spec §RF-005, História de Usuário 1]
- [x] CHK008 - Os limites de viewport distinguem sem lacunas os tamanhos desktop e celular previstos, inclusive larguras entre 391 px e 1023 px? [Clareza, Spec §RF-003, CS-002]
- [x] CHK009 - “Mensagem compreensível” define expectativa suficiente sobre conteúdo, associação ao campo e próximo passo para erros de validação ou operação? [Clareza, Spec §RF-010, História de Usuário 2–3]
- [x] CHK010 - Os critérios de sucesso identificam como medir legibilidade por tecnologia assistiva, foco visível, contraste e ausência de dependência exclusiva de cor? [Mensurabilidade, Spec §RF-009–RF-011, CS-003–CS-004]

## Consistência e rastreabilidade

- [x] CHK011 - Os critérios de desktop/celular estão alinhados entre requisitos, cenários de aceitação e critérios de sucesso? [Consistência, Spec §RF-003, Histórias 1–2, CS-001–CS-002]
- [x] CHK012 - A exigência de mostrar somente ações autorizadas está claramente separada da obrigação de autorização no servidor? [Consistência, Spec §RF-006–RF-007, História de Usuário 1]
- [x] CHK013 - Os requisitos de identidade visual e feedback preservam explicitamente resultados, validações e regras de autorização da Feature 1? [Rastreabilidade, Spec §RF-012, História de Usuário 3, CS-006]

## Cobertura de cenários e limites

- [x] CHK014 - Os requisitos especificam a resposta esperada para destino inexistente ou indisponível sem apresentar funcionalidades como existentes? [Cobertura, Spec §Casos de Erro e Limite, RF-013–RF-014]
- [x] CHK015 - Os requisitos distinguem convite inválido/expirado, falha de validação e falha operacional, sem confundir recusa com sucesso? [Cobertura, Exceção, Spec §História de Usuário 2, História de Usuário 3]
- [x] CHK016 - A dependência da área de cliente cobre o caso em que nenhuma tela autenticada desse perfil existe e evita páginas ou dados de demonstração? [Limite de escopo, Spec §História de Usuário 4, RF-013–RF-014]
- [x] CHK017 - Os requisitos definem se mudanças de largura, sessão encerrada ou perda de conexão afetam navegação e comunicação de estado sem alterar o resultado da operação? [Cobertura, Recuperação, Spec §Casos de Erro e Limite, RF-008, RF-012]

## Notas

- Itens desta checklist avaliam a qualidade do que está especificado, não a execução da interface nem a conclusão da implementação.
- A área financeira autenticada do cliente não foi encontrada no código; a especificação registra essa dependência, que deve permanecer explícita durante a revisão.
- O nível adotado é padrão, para revisão de requisitos da Feature 2. Os itens permanecem desmarcados para avaliação do revisor.
- O arquivo `tasks.md` ainda não existe; itens relativos à rastreabilidade de execução podem ser reavaliados após a geração das tarefas.
- `/speckit-implement` lê o estado das checklists, mas não deve alterar os marcadores.
