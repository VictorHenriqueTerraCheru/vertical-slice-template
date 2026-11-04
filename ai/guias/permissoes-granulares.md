# Permissões granulares

Use quando `Authenticated` e `Administrator` deixarem de bastar (`ai/adr/0006-policies-simples.md`). Não implemente antes.

## Desenho

- Tabelas por feature: `profiles` (perfil), `features` (capacidades, por exemplo `todos.read`) e `profile_features`; `users` ganha o perfil.
- Entidades em `Infrastructure/Persistence/Entities/`, uma feature `Permissions` seguindo `ai/playbooks/nova-feature.md`.
- A decisão "este usuário pode X" é uma regra pura em `Feature/<Area>/Rules/` (por exemplo, numa área de permissões), testada direto.

## Aplicação

1. Registre uma policy por capacidade em `Infrastructure/Configurations/AuthenticationConfiguration.cs` e os nomes em `Common/Statics/AuthorizationPolicies.cs`.
2. Emita as capacidades como claims em `Feature/Auth/Services/TokenService.cs`, ou leia-as por request se precisarem refletir mudança imediata.
3. Proteja a rota com `RequireAuthorization(<policy>)` no Module.
4. Atualize `tests/Starter.Api.Tests/Architecture/EndpointSecurityTests.cs` e escreva testes de 403.

## Cuidados

- Claim no token só muda quando o token é renovado; para revogação imediata, consulte o banco.
- O papel `Administrator` continua existindo como atalho de acesso total.
- Registre a decisão em um novo ADR.
