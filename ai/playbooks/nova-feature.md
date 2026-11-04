# Nova feature

Use para criar uma área nova ou acrescentar funcionalidade. Modelo completo: `Feature/Todos/`. Se `Todos` já foi arquivado, o modelo está em `ai/moldes/Todos/`.

## Ordem

1. **Glossário**: registre os termos novos em `CONTEXT.md`.
2. **Entidade**: `Infrastructure/Persistence/Entities/` e `DbSet` em `Infrastructure/Persistence/AppDbContext.cs`. Modelo: `Infrastructure/Persistence/Entities/TodoItem.cs`.
3. **Configuração de entidade**: `Infrastructure/Persistence/Configurations/` com snake_case, enums como texto e nomes de índice e chave explícitos. Modelo: `Infrastructure/Persistence/Configurations/TodoItemConfiguration.cs`.
4. **Migration**: siga `ai/playbooks/nova-migration.md`.
5. **Repository**: interface em `Feature/<Area>/Abstractions/Repositories`, implementação em `Feature/<Area>/Repositories`. Escrita devolve entidade, leitura devolve projeção, sem `SaveChanges`, sem `IQueryable`, sempre filtrando pelo dono. Listagem usa `ApplySort` e `ToPagedAsync`.
6. **Regra pura**: se a decisão é pesada, `Feature/<Area>/Rules/`, com teste unitário. Modelo: `Feature/Todos/Rules/TodoTransitions.cs`.
7. **Service**: interface em `Abstractions/Services`, implementação em `Services`. Devolve `Result<T>`. Escolha `SaveChangesAsync` ou `ExecuteInTransactionAsync` conforme `AGENTS.md`. Erros em `Errors/<Area>Errors.cs`, constantes em `<Area>Constants.cs`.
8. **Validator**: um por request em `Validators`. Listagem usa `AddPageRules`. Modelo: `Feature/Todos/Validators/ListTodosRequestValidator.cs`.
9. **Module**: `Modules/<Area>Module.cs`, só HTTP, `Results<...>` tipado, `WithSummary` em português, `.Validate<T>()`. Modelo: `Modules/TodoModule.cs`.
10. **Registro em DI**: `AddRepositories` e `AddServices` em `Infrastructure/DependencyInjections/DependencyInjection.cs`.
11. **Testes**: pasta espelhando a feature. Isolamento entre usuários, validação, regra de negócio, conflito de concorrência quando houver. Nomes em inglês. Modelo: `tests/Starter.Api.Tests/Todos/TodoOwnershipTests.cs`.
12. **Mapa**: atualize `ai/memory/project-map.md` e `docs/API.md` se o contrato mudou.

## Checklist final

- Build sem warning e testes passando.
- `switch` em todo handler, nenhum `Result` ignorado.
- Nenhuma rota nova pública sem `AllowAnonymous()` deliberado.
- `tools/check-docs.sh` passa.
- Rode `ai/playbooks/revisar-arquitetura.md` sobre o diff.
