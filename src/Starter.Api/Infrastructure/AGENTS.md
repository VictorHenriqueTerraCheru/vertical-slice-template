# Infrastructure

Montagem da aplicação.

Mora aqui: `Configurations/` (uma classe por preocupação: autenticação, CORS, rate limit, OpenAPI, JSON, erro, proxy, security headers, `.env`), `DependencyInjections/DependencyInjection.cs` (`AddRepositories`, `AddServices`), `Integrations/` (implementação das portas de `Common/Abstractions`), `Middlewares/`, `Statics/` (constantes que só a montagem usa) e `Persistence/`.

Não mora aqui: regra de negócio, request ou response de feature, entidade de feature.

Serviço ou repository novo precisa de linha em `Infrastructure/DependencyInjections/DependencyInjection.cs`.
