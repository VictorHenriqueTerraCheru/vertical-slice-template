# Feature

Uma pasta por área de negócio. Layout fixo: `Abstractions/Repositories`, `Abstractions/Services`, `Errors`, `Models`, `Repositories`, `Rules`, `Services`, `Validators`, `<Area>Constants.cs`.

Mora aqui: service, repository, validator, requests e responses, erros e constantes da área.

Não mora aqui: HTTP, entidades e `DbContext` (ficam em `Infrastructure/Persistence`), código compartilhado (vai para `Common`).

Decisão de domínio do tipo "isto pode acontecer dado este estado" vai para `Rules/`, mesmo com uma linha, com teste unitário sem banco; o service só orquestra e devolve o erro. Modelo: `Feature/Todos/Rules/TodoTransitions.cs`.

Uma feature usa outra pelo service, nunca pelo repository. Sem ciclos.

Modelo completo: `Feature/Todos/`. Passo a passo: `ai/playbooks/nova-feature.md`.
