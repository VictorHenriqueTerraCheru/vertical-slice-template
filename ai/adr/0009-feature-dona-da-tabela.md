# Feature dona da própria tabela, Common só com o que feature usa

A entidade de uma área e o `IEntityTypeConfiguration` dela moram em `Feature/<Area>/Persistence/`, no namespace da área. Continuam em `Infrastructure/Persistence` só o que é de todos: `AppDbContext`, `UnitOfWork`, o `User` do Identity, migrations e seeds. `Common` guarda só o que as features enxergam; implementação de porta vai para `Infrastructure/Integrations`, e middleware, seed e constante que só a montagem usa vão para `Infrastructure`.

Por que: uma área inteira cabe numa pasta, e arquivar ou apagar uma área é mover essa pasta, sem caçar entidade e configuração em outro lugar. `Common` deixa de depender de SMTP, HTTP de terceiro e detalhes de montagem, e a regra "quem usa decide onde mora" fica verificável.

Consequências: revisa a frase do ADR 0001 que punha todas as entidades em `Infrastructure/Persistence`. `AppDbContext` continua achando as configurações pelo assembly, sem `DbSet` nem registro. `User` fica central porque é a base do `IdentityUserContext` e várias áreas apontam chave estrangeira para ele. A migration não muda: a tabela é a mesma, só o tipo trocou de namespace.

Alternativa rejeitada: manter tudo de banco em `Infrastructure/Persistence`. Uma feature nova tocava cinco pastas e o script de arquivamento precisava listar cada entidade.
