# Modules

Só HTTP. Um `<Area>Module` por área, implementando `IEndpointModule`; é descoberto sozinho, sem registro.

Mora aqui: rotas em `Map`, handlers `private static` terminados em `Async`, `.Validate<T>()`, `WithSummary` em português, tradução de `Result<T>` para `Results<...>` com `switch`.

Não mora aqui: regra de negócio, acesso a dados, repository, `AppDbContext`, LINQ.

Rota nova exige autenticação; pública só com `AllowAnonymous()` e entrada em `tests/Starter.Api.Tests/Architecture/EndpointSecurityTests.cs`.

Modelo: `Modules/TodoModule.cs`. Regras completas: `AGENTS.md`.
