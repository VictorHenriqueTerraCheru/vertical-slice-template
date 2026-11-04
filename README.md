# Starter

Template privado de API .NET 10 em vertical slice, projeto único, com PostgreSQL. Traz autenticação (Identity + JWT com refresh token rotativo), exemplo completo de feature (`Todos`), testes por HTTP contra banco real e um pacote de instruções para IA (`AGENTS.md`, `ai/`).

## Pré-requisitos

- .NET SDK 10
- Docker (banco local e testes com Testcontainers)

## Como subir

```bash
docker compose up -d db
cp .env.example .env
dotnet run --project src/Starter.Api
```

- A API sobe em `http://localhost:5080`; a documentação interativa (Scalar) fica em `/scalar`.
- Em `Development`, duas contas de teste são criadas: `admin@starter.local` e `user@starter.local`, senha `starter12345`.
- Porta 5432 ocupada: ajuste `DB_PORT` e a porta de `DB_CONNECTION` no `.env`.

## Comandos

```bash
dotnet build
dotnet test
tools/check-docs.sh
```

## Contrato e requests

- Contrato HTTP (autenticação, erros, paginação, enums): `docs/API.md`.
- Coleção de requests para o Bruno: pasta `bruno/`, ambiente `local`. Com a API no ar: `npx @usebruno/cli run -r --env local` dentro de `bruno/`.

## Duas formas de usar o template

### 1. Clonar e começar o projeto

Clone o repositório, abra com a IA e cole:

> Leia o AGENTS.md e siga ai/playbooks/iniciar-projeto.md para começar um projeto novo chamado <Nome>. O projeto é: <descrição>.

A IA renomeia o template, arquiva o exemplo `Todos` em `ai/moldes/Todos/`, gera a migration inicial e escreve o plano do projeto.

### 2. Levar para outro repositório

Na pasta do repositório de destino (novo ou quase vazio), com este template clonado ao lado, cole:

> Leia o AGENTS.md do template em <caminho do template> e siga ai/playbooks/levar-para-repositorio.md para iniciar neste repositório um projeto .NET chamado <Nome>. O projeto é: <descrição>.

## Scripts

- `tools/rename-project.sh <Nome>`: troca `Starter` pelo nome do projeto (PascalCase). Exige árvore git limpa e não comita.
- `tools/archive-example.sh`: move o exemplo `Todos` para `ai/moldes/Todos/`, remove seus registros, refaz a migration inicial e apaga os scripts do template. Exige árvore git limpa e não comita.
- `tools/test-template-scripts.sh`: testa os dois scripts em clones temporários.

## Licença

MIT. Veja `LICENSE`.
