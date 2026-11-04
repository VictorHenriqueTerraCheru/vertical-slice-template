namespace Starter.Api.Common;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers[SecurityHeaders.ContentTypeOptions] = SecurityHeaders.ContentTypeOptionsValue;
        headers[SecurityHeaders.FrameOptions] = SecurityHeaders.FrameOptionsValue;
        headers[SecurityHeaders.ReferrerPolicy] = SecurityHeaders.ReferrerPolicyValue;
        return next(context);
    }
}
