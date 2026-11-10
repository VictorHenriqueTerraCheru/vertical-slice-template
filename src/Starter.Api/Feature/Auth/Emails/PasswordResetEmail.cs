using Starter.Api.Common;

namespace Starter.Api.Auth;

public static class PasswordResetEmail
{
    public static EmailMessage Create(string to, string link, EmailBrand brand)
    {
        var intro = "Recebemos um pedido para redefinir a senha da sua conta.";
        var ignore = "Se você não fez esse pedido, ignore esta mensagem. Sua senha continua a mesma.";

        var text = EmailLayout.Text(brand, intro, $"Para escolher uma nova senha, acesse:\n{link}", ignore);
        var html = EmailLayout.Html(
            brand,
            AuthConstants.PasswordResetSubject,
            "Use o link para escolher uma nova senha.",
            EmailLayout.Paragraph(intro)
            + EmailLayout.Button("Escolher nova senha", link)
            + EmailLayout.Paragraph(ignore)
            + EmailLayout.Small($"Se o botão não funcionar, copie este endereço no navegador: {link}"));

        return new EmailMessage(to, AuthConstants.PasswordResetSubject, text, html);
    }
}
