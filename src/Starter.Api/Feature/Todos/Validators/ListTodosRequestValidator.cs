using FluentValidation;

using Starter.Api.Common;

namespace Starter.Api.Todos;

public sealed class ListTodosRequestValidator : AbstractValidator<ListTodosRequest>
{
    public ListTodosRequestValidator() => this.AddPageRules(TodosConstants.SortableFields);
}
