using System.Globalization;
using System.Text.Json;

using FluentValidation;

namespace Starter.Api.Common;

public static class ValidationExtensions
{
    public static void ConfigureGlobal()
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        ValidatorOptions.Global.PropertyNameResolver = (_, member, _) =>
            member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name);
    }

    public static RouteHandlerBuilder Validate<T>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<T>>().ProducesValidationProblem();
}
