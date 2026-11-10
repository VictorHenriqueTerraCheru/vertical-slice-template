using Starter.Api.Common;

namespace Starter.Api.Tests.Common;

public sealed class EmailLayoutTests
{
    private static readonly EmailBrand Brand = new("Acme", null);

    [Fact]
    public void Html_encodes_title_preheader_and_text_helpers()
    {
        var html = EmailLayout.Html(Brand, "<b>title</b>", "<i>preview</i>", EmailLayout.Paragraph("<script>x</script>"));

        html.Should().Contain("&lt;b&gt;title&lt;/b&gt;")
            .And.Contain("&lt;i&gt;preview&lt;/i&gt;")
            .And.Contain("&lt;script&gt;x&lt;/script&gt;")
            .And.NotContain("<script>");
    }

    [Fact]
    public void Header_shows_the_brand_name_without_a_logo()
    {
        EmailLayout.Html(Brand, "t", "p", string.Empty).Should().Contain(">Acme</span>").And.NotContain("<img");
    }

    [Fact]
    public void Header_shows_the_logo_when_configured()
    {
        var brand = new EmailBrand("Acme", new Uri("https://cdn.example.com/logo.png?a=1&b=2"));

        EmailLayout.Html(brand, "t", "p", string.Empty)
            .Should().Contain("""<img src="https://cdn.example.com/logo.png?a=1&amp;b=2" """)
            .And.Contain("""alt="Acme" """);
    }

    [Fact]
    public void Button_encodes_the_link()
    {
        EmailLayout.Button("Abrir", "https://app.example.com/?a=1&b=\"x\"")
            .Should().Contain("""href="https://app.example.com/?a=1&amp;b=&quot;x&quot;" """);
    }

    [Fact]
    public void Text_joins_paragraphs_and_ends_with_the_footer()
    {
        EmailLayout.Text(Brand, "first", "second")
            .Should().Be("first\n\nsecond\n\nE-mail automático de Acme. Não é preciso responder.");
    }
}
