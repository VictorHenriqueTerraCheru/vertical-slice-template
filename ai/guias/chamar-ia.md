# Chamar um modelo de IA

A feature fala com o modelo por `IChatModelClient` (`Common/Abstractions/IChatModelClient.cs`) e nunca conhece o provedor. O adaptador atual é `Common/Services/OpenAiChatModelClient.cs` (Responses API com streaming). Decisão em `ai/adr/0008-ia-atras-de-porta-neutra.md`.

Modelo real de uso: `Feature/Assistant/Services/AssistantService.cs`, exposto em `Modules/AssistantModule.cs`.

## Contrato

- `ChatRequest`: instruções, mensagens (`ChatRole.User` ou `ChatRole.Assistant`), ferramentas oferecidas, rodadas de ferramenta já feitas e limite de tokens de saída.
- `StreamAsync` devolve, em ordem: zero ou mais `ChatTextDelta`, e um único evento final, `ChatCompleted` (motivo de parada, chamadas de ferramenta, `ChatUsage`, estado opaco para reenvio) ou `ChatFailed` (motivo curto e `ChatUsage`).
- O adaptador nunca lança exceção por falha do provedor: HTTP de erro, timeout (`AiConstants.ModelCallTimeout`), rede e stream interrompido viram `ChatFailed`. Só o cancelamento de quem chamou atravessa.
- `ChatToolRound.ReplayState` é do adaptador. A feature só guarda e devolve.

## Feature nova que usa IA

1. Service da feature recebe `IChatModelClient`, `IAiUsageService` e `EnvironmentConfiguration`. Sem `configuration.AiChatConfigured`, devolve `AiErrors.NotConfigured` (503) antes de chamar.
2. Instruções e esquemas de ferramenta em `<Area>Constants`. Esquema com `additionalProperties: false` e todos os campos em `required` (opcional vira tipo com `null`), porque o adaptador usa modo estrito.
3. Ferramenta só lê ou escreve dado do usuário do token, chamando o service de outra área, nunca o repository. Argumento inválido, id de outro usuário, ferramenta desconhecida e exceção da própria ferramenta voltam ao modelo como `{"error":"..."}`; nada lança exceção depois que o stream começou. Modelo: `Feature/Assistant/Services/AssistantTools.cs`.
4. Limite tudo: rodadas de ferramenta, chamadas por rodada, tokens de saída, tamanho e quantidade de mensagens no validator. Na última rodada, não ofereça ferramentas. O limite de tokens de saída inclui o raciocínio do modelo; deixe folga (o exemplo usa 4.096) ou baixe `AI_CHAT_REASONING_EFFORT`.
5. Registre cada chamada ao modelo com `IAiUsageService.RecordAsync` (tokens, status, latência), inclusive a interrompida por desconexão (`AiCallStatus.Canceled`, num `finally`). O registro nunca derruba o fluxo: falha de gravação é logada e descartada. O relatório sai em `GET /api/v1/admin/ai-usage`.
6. Endpoint com `RequireRateLimiting(RateLimitPolicies.Ai)`.
7. Resposta longa vai em SSE (`TypedResults.ServerSentEvents`) com eventos nomeados; resposta curta pode ser JSON comum. Falha do modelo no meio do stream vira evento `error`, porque o status 200 já foi enviado.
8. Teste pela API com `ScriptedChatModelClient` (`tests/Starter.Api.Tests/Infra/ScriptedChatModelClient.cs`), que roteiriza as respostas e guarda os pedidos. Modelo: `tests/Starter.Api.Tests/Assistant/AssistantTests.cs`.

## Outro provedor

1. Implemente `IChatModelClient` em `Common/Services/<Provedor>ChatModelClient.cs`, com o mesmo contrato acima.
2. Acrescente o nome em `AiConstants.SupportedProviders` e o registro em `AddChatModel` de `Infrastructure/DependencyInjections/DependencyInjection.cs`.
3. Teste o adaptador com um `HttpMessageHandler` falso, como em `tests/Starter.Api.Tests/Common/OpenAiChatModelClientTests.cs`: texto, ferramenta com reenvio, truncado, erro HTTP, stream interrompido e falha de rede.
4. A feature não muda.

## Cuidados

- A chave é segredo: `AI_CHAT_API_KEY` vem do cofre da plataforma, nunca do repositório. `AI_CHAT_BASE_URL` exige https fora de `Development`, para a chave não trafegar em texto puro.
- Nunca logue prompt, resposta, argumentos de ferramenta nem conteúdo do usuário; logue só tipo de exceção e status HTTP.
- Conteúdo do usuário e saída de ferramenta são dados, não instrução. As instruções dizem isso ao modelo, e as ferramentas não dão poder além do que o próprio usuário tem.
- Erro do provedor não vai para o cliente; ele recebe `AiErrors.Unavailable`.
- O adaptador da OpenAI usa `store: false`: a conversa não fica guardada no provedor, e o raciocínio criptografado volta em cada rodada (`AI_CHAT_INCLUDE_REASONING`). Item de raciocínio sem conteúdo criptografado não é reenviado, porque o provedor não o encontraria.
- `response.incomplete` só vira resposta truncada quando o motivo é o limite de tokens; outro motivo (filtro de conteúdo) vira falha.

## Fora do exemplo

- Histórico salvo no banco: hoje o cliente manda as últimas mensagens a cada pedido. Para guardar, crie tabelas de conversa e mensagem na feature, com dono e índice, e salve a resposta ao receber `ChatCompleted`.
- Resposta que precisa ser salva mesmo se o cliente desconectar: rode o turno fora da requisição, com `Channel` e um escopo próprio (`IServiceScopeFactory`), e deixe o endpoint só ler o canal.
- Busca semântica (embeddings + pgvector): crie uma porta `IEmbeddingClient` no mesmo molde e uma tabela de trechos com vetor na feature.
