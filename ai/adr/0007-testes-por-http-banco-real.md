# Testes por HTTP contra banco real

Regra de negócio é testada pela API com `WebApplicationFactory<Program>` e PostgreSQL em Testcontainers, mesma imagem do `docker-compose.yml`. Regra pura tem teste unitário direto, sem banco. `FakeTimeProvider` controla o tempo.

Por que: o comportamento que importa (SQL, transação, conflito, serialização, status HTTP) só aparece de ponta a ponta; banco em memória esconde diferenças reais. Testes não quebram quando a implementação interna muda.

Consequências: a suíte exige Docker; testes de arquitetura por reflexão protegem as regras de camada e de segurança de rota; nomes de método em inglês.
