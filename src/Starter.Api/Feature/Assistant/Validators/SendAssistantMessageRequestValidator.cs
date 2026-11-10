using FluentValidation;

using Starter.Api.Common;

namespace Starter.Api.Assistant;

public sealed class SendAssistantMessageRequestValidator : AbstractValidator<SendAssistantMessageRequest>
{
    public SendAssistantMessageRequestValidator()
    {
        RuleFor(request => request.Messages)
            .NotEmpty()
            .Must(messages => messages.Count <= AssistantConstants.MaxMessages)
            .WithMessage($"Envie no máximo {AssistantConstants.MaxMessages} mensagens.")
            .Must(messages => messages[^1]?.Role == ChatRole.User)
            .WithMessage("A última mensagem precisa ser do usuário.");

        RuleForEach(request => request.Messages)
            .NotNull()
            .ChildRules(message =>
            {
                message.RuleFor(item => item.Role).IsInEnum();
                message.RuleFor(item => item.Content)
                    .NotEmpty()
                    .MaximumLength(AssistantConstants.MessageMaxLength)
                    .NoNullCharacter();
            });
    }
}
