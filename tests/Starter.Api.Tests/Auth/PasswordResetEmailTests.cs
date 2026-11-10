using Starter.Api.Auth;
using Starter.Api.Common;

namespace Starter.Api.Tests.Auth;

public sealed class PasswordResetEmailTests
{
    private const string Link = "https://app.example.com/reset-password?email=a%40b.com&token=abc";

    [Fact]
    public void Message_has_subject_recipient_and_both_bodies_with_the_link()
    {
        var message = PasswordResetEmail.Create("a@b.com", Link, new EmailBrand("Acme", null));

        message.To.Should().Be("a@b.com");
        message.Subject.Should().Be(AuthConstants.PasswordResetSubject);
        message.TextBody.Should().Contain(Link).And.EndWith("E-mail automático de Acme. Não é preciso responder.");
        message.HtmlBody.Should().Contain("href=\"https://app.example.com/reset-password?email=a%40b.com&amp;token=abc\"");
        message.HtmlBody.Should().Contain("Escolher nova senha");
    }
}
