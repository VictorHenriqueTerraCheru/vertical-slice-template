# Diretrizes de código

Valem para qualquer alteração. As regras específicas deste repositório estão em `AGENTS.md`.

1. **Simples antes de esperto.** Escolha a solução mais direta que atende ao pedido. Abstração só entra quando duas necessidades reais a exigem.
2. **Faça só o pedido.** Não acrescente recurso, parâmetro ou configuração que ninguém pediu. O que estiver fora do escopo você relata.
3. **Leia antes de escrever.** Entenda o fluxo existente e copie o padrão dele. Dois jeitos de fazer a mesma coisa são um defeito.
4. **Nome diz o que é.** Se o código precisa de comentário para ser entendido, renomeie ou divida. Não deixe comentário.
5. **Funções pequenas, uma responsabilidade.** Saia cedo com guard clauses. Evite aninhamento fundo.
6. **Falhe de forma explícita.** Trate o erro esperado como resultado, não como exceção. Valide na borda e nunca engula erro em silêncio.
7. **Prove que funciona.** Escreva o teste que falha, faça passar, rode build e testes. Só diga que está pronto com a saída na mão.
8. **Segurança e dados primeiro.** Nunca confie em entrada do cliente, nunca exponha segredo ou dado pessoal em log, e toda consulta respeita o dono do dado.
