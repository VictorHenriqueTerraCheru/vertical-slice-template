# Starter

API de exemplo de vertical slice em .NET: contas de usuário com autenticação e tarefas pessoais. Este glossário é trocado no início de um projeto real (`ai/playbooks/iniciar-projeto.md`).

## Linguagem

**Usuário**:
Pessoa com conta, identificada por e-mail. Tem um papel: `User` ou `Administrator`.
_Avoid_: Cliente, membro

**Administrador**:
Usuário com a policy `Administrator`, que enxerga tarefas de todos.
_Avoid_: Admin, superusuário

**Par de tokens**:
Access token de vida curta mais refresh token de vida longa, emitidos juntos.
_Avoid_: Sessão, credencial

**Refresh token**:
Segredo opaco, gravado só como hash, que troca por um novo par de tokens e é revogado ao ser usado.
_Avoid_: Token de sessão

**Tarefa**:
Item pessoal de um usuário, com título, descrição opcional e status. No código chama-se `TodoItem`, porque `Task` colide com `System.Threading.Tasks.Task`.
_Avoid_: Todo, atividade

**Status**:
Estado de uma tarefa: pendente, em andamento, concluída ou cancelada. Só certas mudanças são permitidas.
_Avoid_: Situação, fase

**Transição**:
Mudança de status aceita pela regra de `TodoTransitions`.
_Avoid_: Movimentação

**Assistente**:
Chat com IA que responde sobre as tarefas do próprio usuário, lendo-as por ferramentas. Só lê; não cria nem altera.
_Avoid_: Bot, chatbot

**Gasto de IA**:
Registro de cada chamada ao modelo, com tokens, status e latência, usado para acompanhar custo.
_Avoid_: Log de IA, consumo

**Evento de tarefa**:
Registro imutável de uma transição, com status anterior, novo status, autor e momento.
_Avoid_: Log, histórico bruto
