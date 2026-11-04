namespace Starter.Api.Common;

public static class ConfigurationKeys
{
    public const string DatabaseConnection = "DB_CONNECTION";
    public const string JwtKey = "JWT_KEY";
    public const string JwtIssuer = "JWT_ISSUER";
    public const string JwtAudience = "JWT_AUDIENCE";
    public const string AccessTokenMinutes = "ACCESS_TOKEN_MINUTES";
    public const string RefreshTokenDays = "REFRESH_TOKEN_DAYS";
    public const string LockoutMaxFailures = "LOCKOUT_MAX_FAILURES";
    public const string LockoutMinutes = "LOCKOUT_MINUTES";
    public const string RateLimitAuthPerMinute = "RATE_LIMIT_AUTH_PER_MINUTE";
    public const string CorsOrigins = "CORS_ORIGINS";
    public const string SmtpHost = "SMTP_HOST";
    public const string SmtpPort = "SMTP_PORT";
    public const string SmtpUser = "SMTP_USER";
    public const string SmtpPassword = "SMTP_PASSWORD";
    public const string SmtpSenderName = "SMTP_SENDER_NAME";
    public const string SmtpSenderEmail = "SMTP_SENDER_EMAIL";
    public const string PasswordResetUrl = "PASSWORD_RESET_URL";
    public const string InitialAdminEmail = "INITIAL_ADMIN_EMAIL";
    public const string InitialAdminPassword = "INITIAL_ADMIN_PASSWORD";
}
