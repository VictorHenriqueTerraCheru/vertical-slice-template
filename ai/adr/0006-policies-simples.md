# Policies simples e quando adotar permissões granulares

Só existem `Authenticated` e `Administrator` (`Common/Statics/AuthorizationPolicies.cs`). O papel vem do banco para o claim do token. Dois papéis cobrem o template e mantêm o modelo de autorização legível.

Adote permissões granulares (features e perfis) quando houver mais de dois níveis reais de acesso, ou quando o cliente precisar configurar quem pode o quê sem deploy. Passo a passo em `ai/guias/permissoes-granulares.md`.

Consequência: rota de admin é um grupo com `RequireAuthorization(AuthorizationPolicies.Administrator)`, como em `Modules/TodoModule.cs`.
