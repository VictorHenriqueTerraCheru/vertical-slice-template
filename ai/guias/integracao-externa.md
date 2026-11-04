# Integração externa

Todo serviço de fora (e-mail, armazenamento, pagamento, IA) entra atrás de uma interface, para o service não conhecer o fornecedor e os testes usarem um substituto.

Modelo real: `Common/Abstractions/IEmailSender.cs`, `Common/Services/LoggingEmailSender.cs`, `Common/Services/SmtpEmailSender.cs`, registro em `AddEmailSender` de `Infrastructure/DependencyInjections/DependencyInjection.cs`.

## Passos

1. Interface em `Common/Abstractions/I<Nome>.cs`, com `CancellationToken` e tipos do domínio, nunca do fornecedor.
2. Implementação real em `Common/Services/`, e uma falsa de desenvolvimento quando fizer sentido (log, memória).
3. Variáveis de ambiente: chaves em `Common/Statics/ConfigurationKeys.cs`, propriedades em `Common/Models/EnvironmentConfiguration.cs`, documentação em `.env.example`. Obrigatórias nos ambientes que usam a implementação real.
4. Registro condicional por `configuration.Environment == EnvironmentNames.X` em `DependencyInjection.cs`.
5. Constantes da integração em `Common/Statics/` (modelo: `SmtpConstants.cs`).
6. Teste com um substituto em `tests/Starter.Api.Tests/Infra/` (modelo: `CapturingEmailSender.cs`) e teste do registro por ambiente (modelo: `tests/Starter.Api.Tests/Common/EmailSenderRegistrationTests.cs`).

## Cuidados

- Falha da integração não pode derrubar o fluxo quando o resultado não depende dela; capture, logue sem dado pessoal e siga.
- Não registre segredo, token nem corpo da mensagem em log.
