# Feature

Uma pasta por área de negócio. Layout fixo: `Abstractions/Repositories`, `Abstractions/Services`, `Errors`, `Models`, `Repositories`, `Rules`, `Services`, `Validators`, `<Area>Constants.cs`.

Mora aqui: service, repository, validator, requests e responses, erros e constantes da área.

Não mora aqui: HTTP, entidades e `DbContext` (ficam em `Infrastructure/Persistence`), código compartilhado (vai para `Common`).

Uma feature usa outra pelo service, nunca pelo repository. Sem ciclos.

Modelo completo: `Feature/Todos/`. Passo a passo: `ai/playbooks/nova-feature.md`.
