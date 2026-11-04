using System.Linq.Expressions;

using FluentValidation;

using Starter.Api.Common;

namespace Starter.Api.Todos;

public static class TodoRuleExtensions
{
    public static void AddTodoTextRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string>> title,
        Expression<Func<T, string>> description)
    {
        validator.RuleFor(title).NotEmpty().MaximumLength(TodosConstants.TitleMaxLength).NoNullCharacter();
        validator.RuleFor(description).MaximumLength(TodosConstants.DescriptionMaxLength).NoNullCharacter();
    }
}
