# Persistence

Banco compartilhado: `AppDbContext`, `UnitOfWork`, `User` do Identity (`Entities/` e `Configurations/`), `Migrations/`, seeds, `PostgresClock` e as extensões de paginação.

Não mora aqui: regra de negócio, consulta de feature (vai no repository), entidade de uma área só (vai em `Feature/<Area>/Persistence/`).

Convenções de schema: snake_case, enums como texto, chaves com `Guid.CreateVersion7`, nomes de índice e chave estrangeira explícitos. Modelo: `Feature/Todos/Persistence/TodoEventConfiguration.cs`.

Migration: `ai/playbooks/nova-migration.md`. `UserManager` só é usado aqui e em `Feature/Users/Repositories/UserRepository.cs`.
