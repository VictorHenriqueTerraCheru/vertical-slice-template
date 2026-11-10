# IA atrás de uma porta neutra

Features falam com modelos de linguagem por `IChatModelClient`, com tipos próprios (`ChatRequest`, eventos de stream, `ChatUsage`). Cada provedor é um adaptador em `Common/Services`, escolhido por `AI_CHAT_PROVIDER`. O primeiro é a OpenAI, pela Responses API, com `HttpClient` tipado e sem SDK.

Por que: trocar de provedor ou de modelo não toca a feature; o teste da feature usa um cliente roteirizado, sem rede; sem SDK, o contrato HTTP fica visível e a dependência não arrasta versões.

Consequências: o adaptador mantém o parser de SSE e o formato de reenvio de ferramentas; cada adaptador novo precisa dos mesmos testes do primeiro; recursos específicos de um provedor só entram se couberem na porta.
