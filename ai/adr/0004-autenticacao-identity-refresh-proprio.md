# Autenticação: Identity + JWT HS256 + refresh próprio

ASP.NET Core Identity cuida de hash de senha, bloqueio por tentativas e token de recuperação. O access token é um JWT HS256 de vida curta (`ACCESS_TOKEN_MINUTES`, padrão 15). O refresh token é um segredo aleatório gravado só como hash, com expiração (`REFRESH_TOKEN_DAYS`, padrão 30) e `revoked_at`; cada refresh revoga o token usado e emite um par novo (rotação). Logout, troca e redefinição de senha revogam todos os refresh tokens da conta. Detalhes do contrato em `docs/API.md`.

Por que: segurança de senha é do Identity, não escrita à mão; o refresh em tabela própria permite revogar e rotacionar, o que o JWT sozinho não permite.

## Limites conhecidos

- O token de recuperação depende das chaves do Data Protection, que não são persistidas. Ele expira a cada reinício da API, e várias instâncias precisam de chaves persistidas e compartilhadas.
- `login` e `forgot-password` têm diferenças de tempo de resposta que podem sugerir se a conta existe. O rate limit por IP mitiga, não elimina.
- Logins falhos simultâneos podem contar menos falhas do que ocorreram, e o bloqueio demorar a disparar.
- Um refresh em andamento pode sobreviver a um logout até expirar.
- O access token continua válido até expirar depois do logout.

Quando sair de HS256: `0005-hs256-para-rs256.md`.
