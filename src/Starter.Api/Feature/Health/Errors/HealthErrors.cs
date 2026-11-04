using Starter.Api.Common;

namespace Starter.Api.Health;

public static class HealthErrors
{
    public static ApplicationError DatabaseUnavailable { get; } = ApplicationError.Rule(
        StatusCodes.Status503ServiceUnavailable, "database-unavailable", "Banco de dados indisponível.");
}
