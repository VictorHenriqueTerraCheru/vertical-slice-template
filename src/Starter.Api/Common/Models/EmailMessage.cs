namespace Starter.Api.Common;

public sealed record EmailMessage(string To, string Subject, string Body);
