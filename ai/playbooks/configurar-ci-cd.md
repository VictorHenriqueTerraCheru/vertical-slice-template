# Configurar CI e CD

Vale para qualquer IA ou pessoa. Use ao criar o repositório no GitHub, ao abrir o primeiro PR ou ao preparar o primeiro deploy.

## Quando cada coisa roda

| Evento | Workflow | O que faz |
|---|---|---|
| PR aberto ou atualizado | `.github/workflows/ci.yml` | Confere documentos, formatação, build e testes do PR |
| Merge ou push na `main` | `.github/workflows/ci.yml` | Repete tudo no resultado do merge |
| Tag `vX.Y.Z` | `.github/workflows/cd.yml` | Roda o CI e, se passar, publica a imagem Docker no GHCR |

O CI só bloqueia o merge depois do passo 2 abaixo. O CD publica a imagem, mas não faz deploy: cada projeto escolhe onde hospedar.

## Fluxo de entrega

1. Crie uma branch a partir da `main`; não commite direto nela.
2. Antes de abrir o PR, rode localmente: `tools/check-docs.sh`, `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test` (exige Docker).
3. Abra o PR e acompanhe: `gh pr checks --watch`.
4. Com falha: leia o log com `gh run view <id> --log-failed`, corrija a causa e envie novo commit. Não desative verificação nem teste para passar.
5. Só faça merge com o CI verde e, em seguida, confira a execução na `main`: `gh run list --branch main --limit 1`.
6. Para publicar uma versão: `git tag vX.Y.Z && git push origin vX.Y.Z`.

Se a pessoa não pediu merge ou tag, pare antes e pergunte. São ações visíveis para outras pessoas.

## Configuração inicial

1. Confirme que os workflows existem no repositório e rodam em um PR de teste.
2. Exija o CI antes do merge. O nome da verificação é o do job, `build-test`:

   ```bash
   gh api -X PUT repos/<dono>/<repositorio>/branches/main/protection --input - <<'JSON'
   {
     "required_status_checks": { "strict": true, "contexts": ["build-test"] },
     "enforce_admins": false,
     "required_pull_request_reviews": null,
     "restrictions": null
   }
   JSON
   ```

   Repositório privado em conta gratuita não tem essa proteção: o comando responde 403. Nesse caso, avise a pessoa e siga o fluxo acima por disciplina.
3. A imagem usa o `GITHUB_TOKEN` do próprio workflow, sem segredo novo. Para publicar em outro registro, troque `docker/login-action` e `images` em `cd.yml`, com credenciais em GitHub Secrets.

## Adicionar deploy

Pergunte à pessoa onde o projeto vai rodar. Depois:

1. Adicione ao `cd.yml` um job `deploy` com `needs: publish-image`.
2. Guarde credenciais em GitHub Secrets; nunca no repositório.
3. Forneça em produção as variáveis obrigatórias de `.env.example`. Sem elas, a API não sobe e lista as que faltam.
4. As migrations rodam na subida da API em `Staging` e `Production`. Com várias instâncias, garanta que só uma aplique migrations.
5. Registre a escolha de hospedagem em `ai/adr/`.

## Outro provedor de CI

Em GitLab, Azure DevOps ou outro, mantenha a mesma ordem: documentos, restore, formatação, build, testes e, só em tag, publicar a imagem. Os testes precisam de Docker disponível no executor, porque sobem o PostgreSQL com Testcontainers.
