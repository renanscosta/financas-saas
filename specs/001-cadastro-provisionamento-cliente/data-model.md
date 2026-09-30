# Modelo de Dados

## Limites de persistência

### Banco da plataforma

Contém identidade, autorização, catálogo de tenants, convites, provisionamento e auditoria
do ciclo de vida. Não contém lançamentos financeiros do cliente.

### Banco de cada tenant

Contém os dados do domínio Finance daquele cliente. Nesta feature, contém as categorias
iniciais Lanches, Mercado e Pessoal. O banco deve ser criado e atualizado pelo fluxo de
provisionamento antes de o tenant ser considerado pronto.

## Entidades da plataforma

### Cliente

Representa a pessoa física cadastrada pelo operador.

- Identidade estável da pessoa cliente.
- E-mail destinado ao convite.
- Estado de atividade: ativo ou desativado.
- Referência única ao tenant e ao usuário inicial.
- Datas de criação, atualização e desativação.

Regras: desativar preserva dados e bloqueia acesso; o cliente desativado não pode ser
reativado por convite.

### Tenant

Representa o limite de isolamento e o catálogo do banco do cliente.

- Identidade estável do tenant.
- Referência ao cliente proprietário.
- Nome lógico do banco e metadados de conexão protegidos.
- Estado de disponibilidade: provisionando, pronto ou falho, além do estado de atividade.
- Datas de criação e atualização.

Regras: um cliente possui um tenant inicial; o usuário nunca escolhe livremente o banco;
a conexão só é liberada após autorização server-side e tenant ativo.

### Usuário

Representa a identidade de acesso associada ao cliente.

- Identidade de autenticação.
- E-mail normalizado e único conforme as regras da plataforma.
- Referência ao tenant autorizado.
- Estado de cadastro: convite pendente ou cadastro concluído.
- Estado de acesso: bloqueado ou habilitado.

Regras: o acesso habilita somente após provisionamento concluído e cadastro concluído; cliente
desativado permanece bloqueado.

### Convite de acesso

Representa o link temporário para concluir o primeiro cadastro.

- Referência ao cliente, tenant e usuário previstos.
- Token armazenado de forma não reversível, nunca em texto puro para consulta posterior.
- Emitido em uma data e expirado 24 horas depois.
- Estado: ativo, utilizado, expirado ou invalidado.
- Data de uso/invalidação e motivo quando aplicável.

Regras: uso único; convite expirado, utilizado, substituído ou associado a cliente desativado
é recusado; um novo convite invalida o anterior.

### Provisionamento

Representa o estado durável da preparação do tenant.

- Referência única ao tenant.
- Estado: pendente, em andamento, pronto ou falho.
- Número da tentativa atual e resumo seguro da última falha.
- Datas de início, conclusão e última tentativa.
- Resultado de cada passo idempotente.

Regras: falha nunca produz estado pronto; retentativa usa o mesmo tenant e retoma somente
passos pendentes ou incompletos. Dados válidos são tenant criado, usuário associado, migrations
aplicadas e categorias padrão persistidas corretamente; esses dados não são recriados. Cada
tentativa é auditável.

### Registro de auditoria do ciclo de vida

Registra ações relevantes sem expor segredos ou tokens.

- Evento: cadastro, início/fim/falha/retentativa de provisionamento, emissão/uso/invalidação
de convite ou desativação.
- Ator, data e hora, tenant afetado e resultado.
- Motivo e metadados mínimos necessários.

## Entidades do tenant

### Categoria

Categoria padrão disponível no banco do tenant.

- Nome: Lanches, Mercado ou Pessoal.
- Estado ativo.
- Identidade estável no banco do tenant.
- Restrição de unicidade por nome para impedir duplicação em retentativas.

## Relações e invariantes

- Cliente 1:1 Tenant.
- Cliente 1:1 Usuário inicial.
- Tenant 1:1 Provisionamento corrente.
- Cliente 1:N Convite de acesso histórico; no máximo um convite ativo por cliente.
- Tenant 1:N Registro de auditoria.
- Banco de cada Tenant 1:N Categoria.
- Nenhuma entidade do banco de um tenant referencia diretamente entidade de outro banco.
- A aptidão de acesso é derivada de: cliente ativo, tenant provisionado, usuário cadastrado e
usuário habilitado.
- A conexão do tenant é obtida somente pelo catálogo confiável da plataforma após autorização;
nenhum identificador fornecido pela UI pode substituir essa resolução.

## Transições de estado

### Provisionamento

```text
Pendente -> Em andamento -> Pronto
                     \-> Falho -> Em andamento
```

A transição para `Pronto` exige banco acessível, migrations aplicadas e categorias iniciais
presentes sem duplicação. `Falho` não concede acesso.

### Convite

```text
Ativo -> Utilizado
  |----> Expirado
  |----> Invalidado
```

Um novo convite leva o convite anterior a `Invalidado`. Desativação invalida todos os convites
ativos ou pendentes.

### Acesso do usuário

```text
Bloqueado -> Habilitado
Habilitado -> Bloqueado
```

`Bloqueado -> Habilitado` exige provisionamento `Pronto`, cliente ativo e cadastro concluído.
A desativação sempre leva o acesso a `Bloqueado`.
