namespace Starter.Api.Common;

public static class AiErrors
{
    public static ApplicationError NotConfigured { get; } = ApplicationError.Rule(
        StatusCodes.Status503ServiceUnavailable, "ai-not-configured", "A integração com IA não está configurada.");

    public static ApplicationError Unavailable { get; } = ApplicationError.Rule(
        StatusCodes.Status502BadGateway, "ai-unavailable", "Não foi possível obter resposta da IA. Tente novamente.");
}
