# Iniciar projeto

Use quando a pessoa descreve um projeto novo e quer começar a partir deste template. Não pergunte onde as coisas moram: `AGENTS.md` responde.

## Passos

1. Leia a descrição do projeto. Se houver `ai/templates/brief.md` preenchido, use-o.
2. Pergunte só o que falta e muda o resultado: nome do projeto, features, quem usa, regras que não são óbvias. Uma rodada, perguntas numeradas, com sua sugestão de resposta.
3. Confirme que a árvore git está limpa e rode `tools/rename-project.sh <Nome>`, com `<Nome>` em PascalCase.
4. Commite o rename: `git add -A && git commit -m "chore: renomeia o projeto"`. O script não comita e o `archive` exige árvore limpa.
5. Rode `tools/archive-example.sh`. Ele move `Todos` e `bruno/Todos` para `ai/moldes/Todos/`, remove registros, ajusta os ponteiros das instruções, regenera a migration `Initial` e remove a si mesmo, `tools/rename-project.sh`, `tools/test-template-scripts.sh` e `.github/workflows/template-check.yml`. Não gere a migration à mão. Daqui em diante `Todos` é só molde: consulte-o para regra pura, transação com dois repositories e paginação.
6. Reescreva `CONTEXT.md` com o glossário do domínio real e `ai/memory/project-map.md`.
7. Reescreva `README.md` e `docs/API.md` para o projeto real, removendo toda menção ao template, às duas formas de usar o template, aos scripts de template e ao exemplo `Todos`.
8. Registre cada decisão difícil de reverter em `ai/adr/`, com o próximo número livre.
9. Escreva `ai/PLANO.md`: features em ordem de implementação, cada uma com critérios de aceite verificáveis.
10. Rode `dotnet build`, `dotnet test` e `tools/check-docs.sh`. Tudo precisa passar antes de seguir; então commite o arquivamento.
11. Implemente a primeira feature com `ai/playbooks/nova-feature.md`.
12. Ao publicar o repositório, siga `ai/playbooks/configurar-ci-cd.md`.

## Pronto quando

- Nenhum `Starter` sobra no código, e `Todos` só existe em `ai/moldes/`.
- `CONTEXT.md`, `README.md`, `docs/API.md` e `ai/memory/project-map.md` descrevem o projeto real, sem menção ao template nem aos scripts.
- `ai/PLANO.md` existe e build, testes e `tools/check-docs.sh` passam.
