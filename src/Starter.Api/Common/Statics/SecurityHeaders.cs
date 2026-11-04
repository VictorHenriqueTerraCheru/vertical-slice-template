namespace Starter.Api.Common;

public static class SecurityHeaders
{
    public const string ContentTypeOptions = "X-Content-Type-Options";
    public const string ContentTypeOptionsValue = "nosniff";
    public const string FrameOptions = "X-Frame-Options";
    public const string FrameOptionsValue = "DENY";
    public const string ReferrerPolicy = "Referrer-Policy";
    public const string ReferrerPolicyValue = "no-referrer";
}
