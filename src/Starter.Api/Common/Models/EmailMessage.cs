namespace Starter.Api.Common;

public sealed record EmailMessage(string To, string Subject, string TextBody, string HtmlBody);
