# Contrato de Interface: Fundação visual e navegação

**Feature**: [Fundação visual e navegação](../spec.md)

Este contrato descreve o comportamento visível de navegação e feedback. Ele não cria endpoints financeiros, permissões ou capacidades de negócio.

## Áreas e rotas existentes

| Área | Rota existente | Acesso/visibilidade esperados |
|---|---|---|
| Entrada | `/` | Não anunciar funções ou dados financeiros que não existem. Para visitante, oferecer acesso à entrada autenticada disponível; para operador autenticado, mostrar apenas destinos autorizados existentes. |
| Administração | `/platform/customers/register` | Somente operador com a política administrativa de mutação existente. |
| Administração | `/platform/customers/provisioning` | Operador de leitura ou administrador, segundo política existente; ação de retentativa somente para administrador. |
| Administração | `/platform/customers/deactivate` | Somente operador com a política administrativa de mutação existente. |
| Administração | `/platform/customers/invite` | Somente operador com a política administrativa de mutação existente. |
| Primeiro acesso | `/concluir-cadastro?token=…` | Fluxo existente por convite; não apresentar como destino administrativo nem inferir estado do convite apenas pela URL. |
| Identidade | `/Account/Login` | Razor Page HTTP existente; alinhar sua apresentação visual ao produto e preservar cookie, validações, antifalsificação e retorno local. |
| Identidade | POST `/Account/Logout` | Contrato a introduzir para saída explícita; encerra a sessão pelo mecanismo Identity atual, exige antifalsificação e retorna à entrada/login. Não aceitar saída por GET. |

As rotas `/counter` e `/weather` são páginas de demonstração do template e não devem ser apresentadas como funcionalidades financeiras. Não criar rotas de dashboard, lançamentos, categorias, relatórios ou área do cliente sem páginas reais no escopo.

## Navegação e autorização

- **Plataforma**: rotas administrativas existentes usam o layout da plataforma, com navegação própria condicionada às políticas atuais.
- **Primeiro acesso**: `/concluir-cadastro` usa layout público enxuto, sem menu da plataforma.
- **Cliente autenticado**: o layout de cliente é um contrato visual separado, sem ações administrativas. Não existe rota autenticada de cliente no código atual; não criar nem ativar esse layout até uma página real estar definida e autorizada.

- A navegação administrativa tem identidade própria e destaca a rota atual por texto/semântica, além do tratamento visual.
- Links de mutação são exibidos apenas a administradores; link de consulta do provisionamento é exibido também a operadores somente leitura.
- A pessoa convidada vê apenas o fluxo de conclusão, sem navegação administrativa.
- Navegação mobile oferece os mesmos destinos autorizados da desktop e controles operáveis por teclado.
- Ocultar destinos é somente apresentação; requisições diretas, roteamento interativo e handlers continuam sujeitos às políticas/autorização existentes no servidor.
- Operações de login e logout permanecem dentro do fluxo HTTP de Identity, não em manipuladores de componentes interativos.

## Apresentação e acessibilidade

- Usar um layout administrativo consistente e distinto do layout enxuto de conclusão do convite. Um layout de cliente só se aplica quando houver telas autenticadas reais desse perfil.
- Componentes têm estados visuais e textuais para carregamento, sucesso, erro e validação; sucesso e erro são anunciados a leitores de tela com semântica apropriada.
- Campos têm rótulos programáticos visíveis; erros são associados ao campo e explicam correção possível.
- Todos os itens interativos têm ordem de foco lógica e foco visível. Contraste e estado não dependem somente de cor.
- A revisão de contraste segue como referência o nível AA das WCAG 2.1 para texto e controles, sem substituir revisão de teclado e tecnologias assistivas.
- Nas larguras de 360 px (celular), 768 px (intermediária/tablet) e 1280 px (desktop), não há rolagem horizontal e ações essenciais continuam alcançáveis; as larguras intermediárias entre 391 px e 1023 px também não devem perder conteúdo.

## Preservação funcional

A troca de componentes altera somente a apresentação e o estado temporário da UI. Entradas, validações, políticas, comandos, resultados, sequência de provisionamento/convite/desativação e efeitos persistidos da Feature 1 permanecem idênticos.
