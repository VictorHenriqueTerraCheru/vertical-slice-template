# Iniciar projeto

Use quando a pessoa descreve um projeto novo e quer começar a partir deste template. Não pergunte onde as coisas moram: `AGENTS.md` responde.

## Passos

1. Leia a descrição do projeto. Se houver `ai/templates/brief.md` preenchido, use-o.
2. Pergunte só o que falta e muda o resultado: nome do projeto, features, quem usa, regras que não são óbvias. Uma rodada, perguntas numeradas, com sua sugestão de resposta.
3. Confirme que a árvore git está limpa e rode `tools/rename-project.sh <Nome>`, com `<Nome>` em PascalCase.
4. Rode `tools/archive-example.sh`. Ele move `Todos` para `ai/moldes/Todos/`, remove registros e migration inicial e ajusta os ponteiros das instruções. Daqui em diante `Todos` é só molde: consulte-o para regra pura, transação com dois repositories e paginação.
5. Gere de novo a migration inicial conforme `ai/playbooks/nova-migration.md`.
6. Reescreva `CONTEXT.md` com o glossário do domínio real, `README.md` e `ai/memory/project-map.md`.
7. Registre cada decisão difícil de reverter em `ai/adr/`, com o próximo número livre.
8. Escreva `ai/PLANO.md`: features em ordem de implementação, cada uma com critérios de aceite verificáveis.
9. Rode `dotnet build`, `dotnet test` e `tools/check-docs.sh`. Tudo precisa passar antes de seguir.
10. Implemente a primeira feature com `ai/playbooks/nova-feature.md`.

## Pronto quando

- Nenhum `Starter` sobra no código, e `Todos` só existe em `ai/moldes/`.
- `CONTEXT.md`, `README.md` e `ai/memory/project-map.md` descrevem o projeto real.
- `ai/PLANO.md` existe e build, testes e `tools/check-docs.sh` passam.
