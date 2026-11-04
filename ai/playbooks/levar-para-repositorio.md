# Levar para outro repositório

Use quando a pessoa pede para iniciar um projeto .NET em outro repositório, usando este template como referência.

## Antes de começar

- O destino precisa ser novo ou quase vazio. Se já tiver código com outro padrão, pare e pergunte; não migre código existente.
- Leia `AGENTS.md`, `ai/DIRETRIZES.md` e `CONTEXT.md` deste template.

## Passos

1. Confirme com a pessoa o nome do repositório e a licença.
2. No destino, rode `git init`.
3. Copie para o destino, sem o histórico git: `src/`, `tests/`, `bruno/`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `nuget.config`, `.editorconfig`, `.gitignore`, `.dockerignore`, `.env.example`, `dotnet-tools.json`, `Dockerfile`, `docker-compose.yml`, `Starter.slnx`, `.github/`, `docs/`, `AGENTS.md`, `CLAUDE.md`, `CONTEXT.md`, `ai/`, `.claude/`, `tools/`, `LICENSE`.
4. Commite a importação: `git add -A && git commit -m "chore: importa o template"`. Os scripts exigem árvore limpa.
5. Siga `ai/playbooks/iniciar-projeto.md` a partir do passo 1; o rename vem depois desse commit.
6. Antes de publicar o repositório, confirme que build, testes e `tools/check-docs.sh` passam.

## Pronto quando

O destino compila, testa e tem as instruções apontando para caminhos que existem.
