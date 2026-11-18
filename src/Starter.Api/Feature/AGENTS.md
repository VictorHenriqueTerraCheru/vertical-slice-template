# Feature

Uma pasta por área de negócio. Layout fixo: `Abstractions/Repositories`, `Abstractions/Services`, `Errors`, `Models`, `Persistence`, `Repositories`, `Rules`, `Services`, `Validators`, `<Area>Constants.cs`. `Emails` e `Workers` quando existirem.

Mora aqui: service, repository, validator, requests e responses, erros e constantes da área, e as entidades da área com o `IEntityTypeConfiguration` delas em `Persistence/`. O `AppDbContext` acha as configurações sozinho pelo assembly.

Não mora aqui: HTTP (vai em `Modules`), `AppDbContext`, migrations e o `User` do Identity (ficam em `Infrastructure/Persistence`), código compartilhado (vai para `Common`).

Decisão de domínio do tipo "isto pode acontecer dado este estado" vai para `Rules/`, mesmo com uma linha, com teste unitário sem banco; o service só orquestra e devolve o erro. Modelo: `Feature/Todos/Rules/TodoTransitions.cs`.

Uma feature usa outra pelo service, nunca pelo repository nem pela entidade. Sem ciclos. Chave estrangeira para o usuário aponta para `User` de `Infrastructure/Persistence`.

Modelo completo: `Feature/Todos/`. Passo a passo: `ai/playbooks/nova-feature.md`.
