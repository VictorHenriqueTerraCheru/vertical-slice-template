# Skills

Skills do Claude Code ficam em `.claude/skills/`. Os roteiros em `ai/playbooks/` funcionam em qualquer IA; as skills finas só apontam para eles.

## Do template

| Skill | Quando usar | Roteiro |
|---|---|---|
| `iniciar-projeto` | começar um projeto a partir de uma descrição | `ai/playbooks/iniciar-projeto.md` |
| `levar-para-repositorio` | iniciar um projeto em outro repositório usando este template como base | `ai/playbooks/levar-para-repositorio.md` |
| `nova-feature` | criar uma área ou funcionalidade | `ai/playbooks/nova-feature.md` |
| `nova-migration` | alterar o schema | `ai/playbooks/nova-migration.md` |
| `revisar-arquitetura` | conferir um diff contra as regras | `ai/playbooks/revisar-arquitetura.md` |

## De terceiros

Copiadas de `mattpocock/skills` (licença MIT); origem e commit em `.claude/skills/THIRD_PARTY_NOTICES.md`. Leem `ai/agents/domain.md` e `ai/agents/issue-tracker.md`.

| Skill | Quando usar |
|---|---|
| `grilling` | testar um plano ou decisão com perguntas até haver entendimento comum |
| `grill-me` | atalho para `grilling` |
| `grill-with-docs` | `grilling` mais atualização de `CONTEXT.md` e ADRs |
| `domain-modeling` | afinar o glossário e registrar ADR |
| `codebase-design` | vocabulário e princípios de módulos profundos |
| `improve-codebase-architecture` | procurar oportunidades de aprofundar módulos |
| `code-review` | revisar mudanças contra os padrões e a especificação |

## Sem estas skills

Os roteiros bastam. Para instalar as de terceiros em outra máquina, copie as pastas de `.claude/skills/` ou baixe-as do repositório de origem no commit registrado, e audite antes (`AGENTS.md`, "Forma de trabalhar").
