# Enviar e-mail

Todo e-mail sai por `IEmailSender` (`Common/Abstractions/IEmailSender.cs`) com uma `EmailMessage` que leva texto puro e HTML. O cliente de e-mail escolhe qual mostrar; os dois dizem a mesma coisa.

Modelo real: `Feature/Auth/Emails/PasswordResetEmail.cs`, chamado por `AuthService.ForgotPasswordAsync`.

## E-mail novo

1. Crie `Feature/<Area>/Emails/<Nome>Email.cs`: classe estática com `Create(...)` que devolve `EmailMessage`. Sem dependência, sem banco, sem configuração lida lá dentro; o service passa tudo pronto, inclusive `EmailBrand.From(configuration)`.
2. Monte o HTML só com `Common/Helpers/EmailLayout.cs`: `Html` (moldura com logo ou nome, título, prévia e rodapé), `Paragraph`, `Button`, `Small`. Todos codificam o texto; nunca concatene dado do usuário em HTML cru.
3. Monte o texto com `EmailLayout.Text`, na mesma ordem do HTML. Links aparecem por extenso no texto.
4. Assunto em `<Area>Constants`. O resto do texto fica no próprio template.
5. Teste unitário do template, como em `tests/Starter.Api.Tests/Auth/PasswordResetEmailTests.cs`: destinatário, assunto, link nos dois corpos e HTML codificado.
6. Teste do fluxo pela API usando `CapturingEmailSender` (`tests/Starter.Api.Tests/Infra/CapturingEmailSender.cs`).

## Envio no service

- Envio que não decide o resultado (avisos, recuperação de senha): capture a exceção, logue sem e-mail nem conteúdo e devolva sucesso. Modelo: `AuthService.ForgotPasswordAsync`.
- Envio que precisa sair uma vez só (notificação de um evento): marque o registro como notificado antes de enviar, dentro de `ExecuteInTransactionAsync` com conferência de versão; se o envio falhar, desfaça a marca para tentar de novo depois. Assim duas requisições simultâneas não mandam o e-mail duas vezes.
- Envio em lote ou lento: tire da requisição seguindo `ai/guias/tarefa-em-segundo-plano.md`.

## Ambientes

- `Testing`: só loga. `Development` sem `SMTP_HOST`: só loga.
- `Development` com Mailpit: `docker compose up -d mailpit`, descomente o bloco de Mailpit do `.env.example` e abra `http://localhost:8025`. `SMTP_SECURITY=None` só é aceito em `Development`.
- `Staging` e `Production`: SMTP real obrigatório. Porta 465 usa TLS direto; as outras exigem STARTTLS. `SMTP_USER` e `SMTP_PASSWORD` são segredo e vêm do cofre da plataforma; host, porta e remetente podem ir em configuração comum.
- Logo: `EMAIL_LOGO_URL` com URL absoluta pública. Sem ela, a moldura mostra `SMTP_SENDER_NAME` em texto.
- O envio real é testado contra Mailpit em Testcontainers: `tests/Starter.Api.Tests/Common/SmtpEmailSenderTests.cs`.

## Cuidados

- E-mail em HTML usa tabela e estilo inline; não use CSS externo, script, fonte remota nem imagem embutida em base64.
- Nunca logue corpo, link com token ou endereço do destinatário fora de `Development`.
