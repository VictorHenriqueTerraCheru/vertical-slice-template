using System.Globalization;
using System.Net;

namespace Starter.Api.Common;

public static class EmailLayout
{
    public static string Html(EmailBrand brand, string title, string preheader, string contentHtml) => $"""
        <!doctype html>
        <html lang="{EmailConstants.Language}">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <meta name="color-scheme" content="light">
        <title>{Encode(title)}</title>
        </head>
        <body style="margin:0;padding:0;background:#f4f4f5;">
        <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{Encode(preheader)}</div>
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f4f4f5;">
          <tr><td align="center" style="padding:32px 16px;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;background:#ffffff;border:1px solid #e4e4e7;border-radius:12px;">
              <tr><td style="padding:24px 32px;border-bottom:1px solid #e4e4e7;">{Header(brand)}</td></tr>
              <tr><td style="padding:32px;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:1.6;color:#27272a;">
                <h1 style="margin:0 0 16px;font-size:22px;line-height:1.3;color:#18181b;">{Encode(title)}</h1>
                {contentHtml}
              </td></tr>
            </table>
            <p style="margin:16px 0 0;font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#71717a;">{Encode(Footer(brand))}</p>
          </td></tr>
        </table>
        </body>
        </html>
        """;

    public static string Text(EmailBrand brand, params string[] paragraphs) =>
        string.Join("\n\n", paragraphs.Append(Footer(brand)));

    public static string Paragraph(string text) =>
        $"""<p style="margin:0 0 16px;">{Encode(text)}</p>""";

    public static string Button(string label, string url) =>
        $"""<p style="margin:24px 0;"><a href="{WebUtility.HtmlEncode(url)}" style="display:inline-block;padding:12px 24px;background:#18181b;color:#ffffff;text-decoration:none;border-radius:8px;font-weight:bold;">{Encode(label)}</a></p>""";

    public static string Small(string text) =>
        $"""<p style="margin:0;font-size:13px;color:#71717a;word-break:break-all;">{Encode(text)}</p>""";

    public static string Encode(string value) =>
        WebUtility.HtmlEncode(value).Replace("\r\n", "<br>").Replace("\n", "<br>");

    private static string Header(EmailBrand brand) => brand.LogoUrl is null
        ? $"""<span style="font-family:Arial,Helvetica,sans-serif;font-size:18px;font-weight:bold;color:#18181b;">{Encode(brand.Name)}</span>"""
        : $"""<img src="{WebUtility.HtmlEncode(brand.LogoUrl.AbsoluteUri)}" height="{EmailConstants.LogoHeight.ToString(CultureInfo.InvariantCulture)}" alt="{WebUtility.HtmlEncode(brand.Name)}" style="display:block;border:0;">""";

    private static string Footer(EmailBrand brand) =>
        $"E-mail automático de {brand.Name}. Não é preciso responder.";
}
