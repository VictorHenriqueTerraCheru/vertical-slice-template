namespace Starter.Api.Common;

public static class CommonErrors
{
    public static ApplicationError ConcurrentChange { get; } = ApplicationError.Rule(
        StatusCodes.Status409Conflict, "concurrent-change", "O registro foi alterado por outra requisição. Tente novamente.");
}
