# Tests

Espelha `Feature/`. `Infra/` tem `ApiFactory`, `ApiClient` e `Scenarios`; `Architecture/` guarda os testes que protegem as regras.

Regra de negócio testa-se pela API contra PostgreSQL real em Testcontainers; regra pura, direto. `FakeTimeProvider` controla o tempo. Exige Docker.

Nomes de método em inglês, descrevendo o comportamento.

Modelos: `tests/Starter.Api.Tests/Todos/TodoCrudTests.cs`, `tests/Starter.Api.Tests/Todos/TodoTransitionsTests.cs`.
