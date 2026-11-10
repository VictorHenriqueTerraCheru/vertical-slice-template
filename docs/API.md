# Contrato HTTP

Base: `/api/v1`. JSON em camelCase. Datas em UTC, ISO 8601. Mensagens de erro em português. Em `Development`, a documentação interativa (Scalar) está em `/scalar`; ela não existe em `Production`.

## Enums

Valores de enum viajam como string em camelCase, no corpo e na query string (`inProgress`, não `1`). Número é recusado com 400. Na query string a comparação ignora maiúsculas.

| Enum | Valores |
|---|---|
| `status` da tarefa | `pending`, `inProgress`, `done`, `canceled` |
| `role` do usuário | `user`, `administrator` |
| `sortDirection` | `asc`, `desc` |
| `role` da mensagem ao assistente | `user`, `assistant` |

## Autenticação

Autenticação por bearer token: `Authorization: Bearer <accessToken>`. Toda rota exige token, exceto as marcadas como públicas.

| Rota | Acesso | Corpo |
|---|---|---|
| `POST /auth/register` | público, com rate limit | `name`, `email`, `password` |
| `POST /auth/login` | público, com rate limit | `email`, `password` |
| `POST /auth/refresh` | público | `refreshToken` |
| `POST /auth/logout` | autenticado | nenhum |
| `POST /auth/forgot-password` | público, com rate limit | `email` |
| `POST /auth/reset-password` | público | `email`, `token`, `newPassword` |
| `POST /auth/change-password` | autenticado | `currentPassword`, `newPassword` |
| `GET`, `PUT /me` | autenticado | `PUT`: `name` |

`register`, `login` e `refresh` devolvem o par de tokens (`201` no registro, `200` nos demais):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "accessTokenExpiresAt": "2026-11-04T12:15:00Z",
  "refreshToken": "q3Zr0uYk2m8...",
  "refreshTokenExpiresAt": "2026-12-04T12:00:00Z"
}
```

- Access token: JWT HS256, vida de 15 minutos (`ACCESS_TOKEN_MINUTES`).
- Refresh token: vida de 30 dias (`REFRESH_TOKEN_DAYS`). Opaco, guardado no servidor só como hash.
- Rotação: `refresh` revoga o refresh token enviado e devolve um par novo. Reusar um refresh token já usado ou expirado responde 401.
- `logout`, `change-password` e `reset-password` revogam todos os refresh tokens da conta. O access token já emitido continua válido até expirar.
- `forgot-password` responde sempre 204, exista a conta ou não. O token de recuperação chega por e-mail, no link de `PASSWORD_RESET_URL`.
- Cinco senhas erradas seguidas bloqueiam a conta por 15 minutos (`LOCKOUT_MAX_FAILURES`, `LOCKOUT_MINUTES`).
- Rate limit de `register`, `login` e `forgot-password`: 10 requisições por minuto por IP (`RATE_LIMIT_AUTH_PER_MINUTE`); excedido responde 429 com `Retry-After`.
- Senha: 8 a 128 caracteres.

## Erros

Toda resposta de erro é ProblemDetails (`application/problem+json`).

Validação, 400, todos os campos juntos, chaves em camelCase:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "pageSize": ["O tamanho da página deve estar entre 1 e 100."],
    "sortBy": ["A ordenação deve ser um dos campos: createdAt, title."]
  }
}
```

Regra de negócio, com `type` estável para o cliente tratar:

```json
{
  "type": "invalid-transition",
  "title": "Essa mudança de status não é permitida para a tarefa.",
  "status": 409
}
```

| Status | Quando |
|---|---|
| 400 | validação, JSON ou parâmetro malformado, enum inválido |
| 401 | sem token, token inválido ou expirado, credenciais inválidas, refresh inválido |
| 403 | autenticado sem a policy exigida |
| 404 | recurso inexistente ou de outro usuário (sem corpo) |
| 409 | conflito de regra (`email-already-in-use`, `invalid-transition`, `todo-not-editable`) ou de concorrência (`concurrent-change`) |
| 429 | rate limit |
| 503 | integração de IA sem chave (`ai-not-configured`) |
| 500 | falha inesperada, sem detalhe interno |

`type` de conflito de concorrência: `concurrent-change`; o cliente pode reler e tentar de novo.

## Paginação

Toda listagem aceita:

| Parâmetro | Regra |
|---|---|
| `page` | 1 a 10.000, padrão 1 |
| `pageSize` | 1 a 100, padrão 20 |
| `sortBy` | só os campos que a rota declara; outro valor responde 400 |
| `sortDirection` | `asc` ou `desc`; o padrão depende da rota |

Valor fora do limite responde 400; nunca é ajustado. A ordem é estável: empates se resolvem pelo `id`, então nenhum item repete nem some entre páginas. Página além do fim responde 200 com `items` vazio e totais corretos.

Requisição:

```http
GET /api/v1/todos?status=pending&sortBy=title&sortDirection=asc&page=2&pageSize=2
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

Resposta:

```json
{
  "items": [
    {
      "id": "0193c2a4-7f10-7b2e-9a51-3c0d2f6e8a14",
      "title": "Comprar leite",
      "description": null,
      "status": "pending",
      "createdAt": "2026-11-04T11:02:10Z",
      "updatedAt": "2026-11-04T11:02:10Z"
    },
    {
      "id": "0193c2a4-9d3c-7a40-8c11-6e5b7d9f0b27",
      "title": "Enviar relatório",
      "description": "Versão final",
      "status": "pending",
      "createdAt": "2026-11-04T11:05:42Z",
      "updatedAt": "2026-11-04T11:05:42Z"
    }
  ],
  "page": 2,
  "pageSize": 2,
  "totalCount": 5,
  "totalPages": 3
}
```

Listagens: `GET /todos` (tarefas do usuário, filtro `status`; ordena por `createdAt` desc por padrão) e `GET /admin/todos` (todas, só `Administrator`). `GET /todos/{id}/events` não é paginada.

## Assistente (SSE)

`POST /assistant/messages`, autenticado, com rate limit de 10 requisições por minuto por usuário (`RATE_LIMIT_AI_PER_MINUTE`). O assistente só lê as tarefas de quem chamou. O servidor não guarda a conversa: o cliente manda o histórico a cada pedido.

```json
{
  "messages": [
    { "role": "user", "content": "Quais tarefas estão pendentes?" },
    { "role": "assistant", "content": "Você tem duas: comprar leite e enviar relatório." },
    { "role": "user", "content": "E qual é a mais antiga?" }
  ]
}
```

- De 1 a 20 mensagens, cada uma com 1 a 4.000 caracteres; a última precisa ser `user`. Fora disso, 400 antes de chamar a IA.
- Sem `AI_CHAT_API_KEY`: 503 com `type` `ai-not-configured`.
- Sucesso: 200 com `Content-Type: text/event-stream`. Eventos, em ordem:

```text
event: delta
data: {"text":"Você tem "}

event: delta
data: {"text":"duas tarefas pendentes."}

event: done
data: {"truncated":false}
```

| Evento | Dados | Quando |
|---|---|---|
| `delta` | `text` | trecho da resposta; concatene na ordem |
| `done` | `truncated` | fim; `true` quando a resposta foi cortada pelo limite de tokens de saída |
| `error` | `type`, `title` | a IA falhou depois do 200 (`ai-unavailable`); descarte ou marque a resposta parcial |

Salvo queda da conexão, o stream termina em `done` ou `error`; falha de ferramenta ou do registro de gasto não interrompe a resposta. Para ler no navegador, use `fetch` com o corpo em stream; `EventSource` não envia `POST` nem cabeçalho de autorização.

## Gasto de IA

`GET /admin/ai-usage?from=2026-11-01T00:00:00Z&to=2026-12-01T00:00:00Z`, só `Administrator`. `from` e `to` são obrigatórios, `to` depois de `from`, no máximo 366 dias. O período inclui `from` e exclui `to`.

```json
{
  "from": "2026-11-01T00:00:00Z",
  "to": "2026-12-01T00:00:00Z",
  "models": [
    {
      "provider": "openai",
      "model": "nome-do-modelo",
      "calls": 42,
      "failedCalls": 1,
      "inputTokens": 51200,
      "cachedInputTokens": 30100,
      "outputTokens": 6400
    }
  ]
}
```

`calls` conta cada chamada ao modelo, inclusive as interrompidas porque o cliente desconectou (status `canceled`, sem tokens conhecidos) (uma pergunta com ferramenta gera mais de uma). `inputTokens` inclui os `cachedInputTokens`.
