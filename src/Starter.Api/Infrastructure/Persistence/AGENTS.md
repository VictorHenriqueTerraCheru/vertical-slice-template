# Persistence

Tudo de banco: `AppDbContext`, `UnitOfWork`, `Entities/`, `Configurations/` (uma por entidade), `Migrations/`, seeds.

Não mora aqui: regra de negócio, consulta de feature (vai no repository).

Convenções de schema: snake_case, enums como texto, chaves com `Guid.CreateVersion7`, nomes de índice e chave estrangeira explícitos. Modelo: `Infrastructure/Persistence/Configurations/TodoEventConfiguration.cs`.

Migration: `ai/playbooks/nova-migration.md`. `UserManager` só é usado aqui e em `Feature/Users/Repositories/UserRepository.cs`.
