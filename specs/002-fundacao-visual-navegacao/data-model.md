# Modelo de Dados da Interface

**Feature**: [Fundação visual e navegação](spec.md)

## Persistência

Esta feature não cria entidades, tabelas, colunas, migrations ou alterações de domínio. Cliente, usuário, tenant, convite, provisionamento e autorização continuam sendo governados pela Feature 1 e pelo estado atual da aplicação.

## Conceitos efêmeros da interface

| Conceito | Informação representada | Regras e origem | Persistência |
|---|---|---|---|
| Destino de navegação | Rota existente, rótulo, grupo visual e política/perfil habilitado | Deve corresponder a uma página existente; destinos administrativos são filtrados usando as políticas existentes. A autorização do servidor segue obrigatória. | Nenhuma |
| Página atual | Destino que corresponde à rota visitada | Deve ser destacado visualmente e semanticamente; mudança de rota atualiza o destaque e o título da página. | Nenhuma |
| Estado da operação | `Ocioso`, `Carregando`, `Sucesso`, `Erro` ou `Validação` | Derivado da operação existente. Durante `Carregando`, evitar submissões duplicadas; o resultado não altera a regra de negócio nem o resultado do caso de uso. | Nenhuma |
| Mensagem de feedback | Texto associado a resultado, falha ou correção necessária | Clara, sem dados sensíveis desnecessários, anunciada de modo acessível e não comunicada apenas por cor/ícone. | Nenhuma |
| Identidade visual | Tokens semânticos de cor, tipografia, espaçamento, foco e superfícies | Centralizados no tema visual comum; cores de estado têm significado consistente e contraste suficiente. | Configuração de apresentação, sem dados de usuário |

## Transições de feedback

- Ação iniciada: `Ocioso` → `Carregando`.
- Operação concluída conforme o resultado funcional existente: `Carregando` → `Sucesso`.
- Falha operacional: `Carregando` → `Erro`.
- Dados rejeitados por validação: `Ocioso` ou `Carregando` → `Validação`.
- Nova tentativa: `Erro`, `Validação` ou `Sucesso` → `Carregando` quando a ação for reexecutada.

O estado efêmero não é evidência de persistência ou sucesso do caso de uso. O componente deve comunicar somente o resultado efetivamente retornado pelo fluxo atual.

## Perfis e destinos existentes

- **Administrador da plataforma**: destinos de cadastro, provisionamento, desativação e convite, conforme as páginas e políticas atuais.
- **Operador somente leitura**: consulta de provisionamento; não recebe ações de mutação.
- **Pessoa convidada**: fluxo existente de conclusão de cadastro, sem navegação administrativa.
- **Cliente autenticado**: não há uma área financeira autenticada no código observado; nenhuma entrada de navegação ou estado de dashboard deve ser inventado.
