using Starter.Api.Common;

namespace Starter.Api.Infrastructure;

public static class SecurityHeadersConfiguration
{
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
