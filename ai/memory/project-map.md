# Mapa do projeto

## Features

| Área | Pasta | Module | O que faz |
|---|---|---|---|
| Auth | `src/Starter.Api/Feature/Auth` | `src/Starter.Api/Modules/AuthModule.cs` | registro, login, refresh com rotação, logout, recuperação e troca de senha |
| Users | `src/Starter.Api/Feature/Users` | `src/Starter.Api/Modules/UserModule.cs` | dados do próprio usuário; único uso de `UserManager` |
| Todos | `src/Starter.Api/Feature/Todos` | `src/Starter.Api/Modules/TodoModule.cs` | CRUD, transições de status com histórico, listagem paginada, rota de admin; molde do template |
| Health | `src/Starter.Api/Feature/Health` | `src/Starter.Api/Modules/HealthModule.cs` | saúde da API e do banco |

## Tabelas

| Tabela | Entidade | Feature |
|---|---|---|
| tabelas do Identity (`users`, `user_claims`, `user_logins`, `user_tokens`) | `src/Starter.Api/Infrastructure/Persistence/Entities/User.cs` | Users |
| `refresh_tokens` | `src/Starter.Api/Infrastructure/Persistence/Entities/RefreshToken.cs` | Auth |
| `todo_items` | `src/Starter.Api/Infrastructure/Persistence/Entities/TodoItem.cs` | Todos |
| `todo_events` | `src/Starter.Api/Infrastructure/Persistence/Entities/TodoEvent.cs` | Todos |

## Rotas

| Rota | Acesso |
|---|---|
| `POST /api/v1/auth/register`, `login`, `refresh`, `forgot-password`, `reset-password` | público |
| `POST /api/v1/auth/logout`, `change-password` | autenticado |
| `GET`, `PUT /api/v1/me` | autenticado |
| `POST`, `GET /api/v1/todos`; `GET`, `PUT`, `DELETE /api/v1/todos/{id}`; `POST /api/v1/todos/{id}/transitions`; `GET /api/v1/todos/{id}/events` | autenticado, dono |
| `GET /api/v1/admin/todos` | `Administrator` |
| `GET /api/v1/health`, `GET /api/v1/health/database` | público |

Contrato HTTP: `docs/API.md`.

## Feature mais recente

Nenhuma além dos exemplos. Depois de `iniciar-projeto`, registre aqui a feature real mais recente.
