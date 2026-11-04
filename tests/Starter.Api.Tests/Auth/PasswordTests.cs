using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.WebUtilities;

using Starter.Api.Auth;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Auth;

[Collection(nameof(ApiCollection))]
public sealed partial class PasswordTests(ApiFactory factory)
{
    private ApiClient NewClient() => new(factory.CreateClient());

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex LinkPattern();

    private string ReadResetToken(string email)
    {
        var body = factory.Emails.SentTo(email).Should().ContainSingle().Subject.Body;
        var link = new Uri(LinkPattern().Match(body).Value);
        var query = QueryHelpers.ParseQuery(link.Query);
        query["email"].ToString().Should().Be(email);
        return query["token"].ToString();
    }

    private async Task<string> RequestResetTokenAsync(string email)
    {
        (await NewClient().ForgotPasswordAsync(email)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        return ReadResetToken(email);
    }

    [Fact]
    public async Task Forgot_with_existing_account_sends_email_and_returns_204()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await NewClient().ForgotPasswordAsync(user.Email);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var message = factory.Emails.SentTo(user.Email).Should().ContainSingle().Subject;
        message.Body.Should().Contain("token=");
        ReadResetToken(user.Email).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Forgot_with_different_case_sends_to_the_stored_address()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await NewClient().ForgotPasswordAsync(user.Email.ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ReadResetToken(user.Email).Should().NotBeNullOrWhiteSpace();
        factory.Emails.SentTo(user.Email.ToUpperInvariant()).Should().BeEmpty();
    }

    [Fact]
    public async Task Forgot_with_unknown_email_returns_204_without_sending()
    {
        var email = Scenarios.NewEmail();

        var response = await NewClient().ForgotPasswordAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.Emails.SentTo(email).Should().BeEmpty();
    }

    [Fact]
    public async Task Forgot_with_delivery_failure_still_returns_204()
    {
        var user = await Scenarios.RegisterAsync(factory);
        factory.Emails.FailOnSend = true;

        try
        {
            var response = await NewClient().ForgotPasswordAsync(user.Email);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        finally
        {
            factory.Emails.FailOnSend = false;
        }

        factory.Emails.SentTo(user.Email).Should().BeEmpty();
    }

    [Fact]
    public async Task Forgot_with_invalid_email_returns_400()
    {
        var response = await NewClient().ForgotPasswordAsync("not-an-email");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reset_with_valid_token_changes_the_password_and_revokes_refresh()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var token = await RequestResetTokenAsync(user.Email);

        var response = await NewClient().ResetPasswordAsync(user.Email, token, Scenarios.NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await NewClient().LoginAsync(user.Email, Scenarios.NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await NewClient().LoginAsync(user.Email, user.Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await NewClient().RefreshAsync(user.Tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reset_with_invalid_token_returns_400()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await NewClient().ResetPasswordAsync(user.Email, "invalid-token", Scenarios.NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await NewClient().LoginAsync(user.Email, user.Password)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await NewClient().RefreshAsync(user.Tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_with_unknown_email_returns_the_same_400_as_an_invalid_token()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var token = await RequestResetTokenAsync(user.Email);

        var unknown = await NewClient().ResetPasswordAsync(Scenarios.NewEmail(), token, Scenarios.NewPassword);
        var invalid = await NewClient().ResetPasswordAsync(user.Email, "invalid-token", Scenarios.NewPassword);

        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unknownProblem = await ApiClient.ReadJsonAsync(unknown);
        var invalidProblem = await ApiClient.ReadJsonAsync(invalid);
        unknownProblem.GetProperty("type").GetString().Should().Be(invalidProblem.GetProperty("type").GetString());
        unknownProblem.GetProperty("title").GetString().Should().Be(invalidProblem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Reset_reusing_the_same_token_returns_400()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var token = await RequestResetTokenAsync(user.Email);
        (await NewClient().ResetPasswordAsync(user.Email, token, Scenarios.NewPassword)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await NewClient().ResetPasswordAsync(user.Email, token, "yet-another-password");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await NewClient().LoginAsync(user.Email, Scenarios.NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_with_empty_token_or_short_password_returns_400()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var token = await RequestResetTokenAsync(user.Email);

        (await NewClient().ResetPasswordAsync(user.Email, string.Empty, Scenarios.NewPassword)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await NewClient().ResetPasswordAsync(user.Email, token, "short")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await NewClient().ResetPasswordAsync(user.Email, token, Scenarios.NewPassword)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Change_with_wrong_current_password_returns_400()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await user.Client.ChangePasswordAsync("wrong-current-password", Scenarios.NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("currentPassword", out _).Should().BeTrue();
        (await NewClient().LoginAsync(user.Email, user.Password)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await NewClient().RefreshAsync(user.Tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Change_with_valid_current_password_changes_it_and_revokes_refresh()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await user.Client.ChangePasswordAsync(user.Password, Scenarios.NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await NewClient().LoginAsync(user.Email, Scenarios.NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await NewClient().LoginAsync(user.Email, user.Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await NewClient().RefreshAsync(user.Tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_without_token_returns_401()
    {
        var response = await NewClient().ChangePasswordAsync("any-password", Scenarios.NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
