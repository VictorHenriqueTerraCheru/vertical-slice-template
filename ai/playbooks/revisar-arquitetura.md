# Revisar arquitetura

Confira o diff contra as regras de `AGENTS.md`. Rode `git diff <base>...HEAD` e responda cada item com o arquivo e a linha, ou "n/a".

## Camadas

- Module tem só HTTP, handlers `private static ...Async`, `switch` sobre o resultado, `Results<...>` tipado e `.Validate<T>()` onde há corpo ou query.
- Service não recebe `AppDbContext`, `UserManager`, `HttpContext` nem usa `IQueryable`.
- Repository não chama `SaveChanges`, não devolve `IQueryable`, filtra pelo dono. Exceções legítimas: coleção filha pequena e limitada, com o dono conferido no service através do pai (`TodoEventRepository.ListAsync`), e rotas de administrador, sem filtro por política (`ListAllAsync`).
- Uma feature usa outra pelo service, nunca pelo repository. Sem ciclo; `Common` não depende de feature.
- `UserManager` só em `Feature/Users/Repositories/UserRepository.cs` e `Infrastructure/Persistence`.

## Escrita e dados

- Escrita única sem conflito usa `SaveChangesAsync`; update, delete que conflita e operação com mais de um repository usam `ExecuteInTransactionAsync`.
- Listagem usa `PageRequest`, `AddPageRules`, `ApplySort` e `ToPagedAsync`; desempate por `Id`.
- Migration gerada, lida e coerente com as regras de `ai/playbooks/nova-migration.md`.

## Segurança

- Rota nova exige autenticação; pública só com `AllowAnonymous()` explícito e no teste de segurança.
- Usuário vem do token. Recurso de outro usuário responde 404.
- Nenhum log com senha, token ou dado pessoal.

## Estilo

- Zero comentários; um tipo por arquivo; `const` fora do arquivo que a usa (exceto caminho de rota).
- Código em inglês, mensagens e `WithSummary` em português; `Async` e `CancellationToken`.
- `TimeProvider`, nunca `DateTime.Now`; `record` e `sealed`.

## Verificação

- Testes cobrem o comportamento novo, com nomes em inglês.
- `dotnet build`, `dotnet test` e `tools/check-docs.sh` passam, e você viu a saída.
- Instruções e `ai/memory/project-map.md` atualizados.
