# Nova migration

Migrations do EF ficam em `src/Starter.Api/Infrastructure/Persistence/Migrations/`. A ferramenta `dotnet-ef` vem de `dotnet-tools.json`.

## Comandos

```bash
dotnet tool restore
dotnet ef migrations add <Nome> --project src/Starter.Api --output-dir Infrastructure/Persistence/Migrations
dotnet ef migrations remove --project src/Starter.Api
```

Aplique em desenvolvimento subindo a API (`docker compose up -d db`, `dotnet run --project src/Starter.Api`) ou com `dotnet ef database update --project src/Starter.Api` e `--connection` apontando para o banco local.

## Regras de schema

- Tabelas e colunas em snake_case, plural em inglês (`todo_events`). A convenção vem de `UseSnakeCaseNamingConvention`.
- Chave primária `Guid`, gerada no código com `Guid.CreateVersion7`.
- Enum gravado como texto com `HasConversion<string>()` e `HasMaxLength`.
- Índices e chaves estrangeiras com nome explícito, como em `Infrastructure/Persistence/Configurations/TodoEventConfiguration.cs`.
- Entidade editável concorrentemente leva `Version` com `IsRowVersion()`.
- Tabela por feature; não crie tabela polimórfica compartilhada entre áreas.
- Todo dado de usuário leva a coluna do dono e índice por ela.
- Zero comentários no SQL e nos arquivos de migration que você escreve.

## Conferir

1. Leia a migration gerada inteira antes de commitar; confira que não removeu nada sem querer.
2. `dotnet build` e `dotnet test`.
3. Nunca edite migration já aplicada em ambiente compartilhado; crie outra.
