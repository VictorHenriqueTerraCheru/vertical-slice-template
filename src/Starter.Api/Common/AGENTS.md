# Common

O que duas ou mais features usam. Não depende de nenhuma feature nem de `Infrastructure`.

Pastas: `Abstractions` (portas e `IUnitOfWork`), `Enums`, `Errors`, `Extensions`, `Helpers`, `Models` (`Result<T>`, `PageRequest`, `EnvironmentConfiguration`), `Statics` (constantes e nomes).

Não mora aqui: regra de uma área só, entidade, repository, implementação de porta (vai em `Infrastructure/Integrations`), middleware, seed e constante que só a montagem usa (vão para `Infrastructure`).

Constante compartilhada com feature vai em `Common/Statics`. Paginação: `Common/Models/PageRequest.cs`.

Regras completas: `AGENTS.md`.
