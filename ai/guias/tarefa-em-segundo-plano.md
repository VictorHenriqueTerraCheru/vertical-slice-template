# Tarefa em segundo plano

O template não traz fila nem agendador. Use quando um trabalho não deve segurar a requisição (envio em lote, limpeza, importação).

## Opção simples: BackgroundService

1. Crie `Common/Services/<Nome>Worker.cs` herdando `BackgroundService`.
2. Dentro do loop, crie um escopo com `IServiceScopeFactory`; services e repositories são scoped e não podem ser injetados direto.
3. Use `TimeProvider` e `PeriodicTimer` para o intervalo, e respeite o `CancellationToken` de `ExecuteAsync`.
4. Registre com `services.AddHostedService<...>()` em `Infrastructure/DependencyInjections/DependencyInjection.cs`.
5. Capture exceção por iteração, logue sem dado pessoal e continue.

## Regras

- O worker chama um service como qualquer Module; a regra continua no service e a transação no `IUnitOfWork`.
- Com mais de uma instância, o worker roda em todas. Trabalho que não pode duplicar precisa de trava no banco ou de fila externa; registre a escolha em ADR.
- Teste o service pela API ou direto; o worker só dispara.

## Quando migrar

Trabalho que precisa de retentativa, persistência do que foi pedido ou execução fora do processo pede uma fila ou biblioteca de jobs. Isso é decisão de ADR, atrás de uma interface em `Common/Abstractions`.
