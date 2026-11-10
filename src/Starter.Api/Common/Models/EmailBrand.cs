namespace Starter.Api.Common;

public sealed record EmailBrand(string Name, Uri? LogoUrl)
{
    public static EmailBrand From(EnvironmentConfiguration configuration) =>
        new(string.IsNullOrWhiteSpace(configuration.SmtpSenderName) ? EmailConstants.DefaultBrandName : configuration.SmtpSenderName,
            configuration.EmailLogoUrl);
}
