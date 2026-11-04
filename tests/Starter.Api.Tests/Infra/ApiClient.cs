using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Users;

namespace Starter.Api.Tests.Infra;

public sealed class ApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = CreateJson();

    public Task<HttpResponseMessage> RegisterAsync(string email, string password, string name = "Test User") =>
        http.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(name, email, password), Json);

    public Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        http.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password), Json);

    public Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        http.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(refreshToken), Json);

    public Task<HttpResponseMessage> LogoutAsync() => http.PostAsync("/api/v1/auth/logout", null);

    public Task<HttpResponseMessage> ForgotPasswordAsync(string email) =>
        http.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email), Json);

    public Task<HttpResponseMessage> ResetPasswordAsync(string email, string token, string newPassword) =>
        http.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(email, token, newPassword), Json);

    public Task<HttpResponseMessage> ChangePasswordAsync(string currentPassword, string newPassword) =>
        http.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequest(currentPassword, newPassword), Json);

    public Task<HttpResponseMessage> GetMeAsync() => http.GetAsync("/api/v1/me");

    public Task<HttpResponseMessage> UpdateMeAsync(string name) =>
        http.PutAsJsonAsync("/api/v1/me", new UpdateMeRequest(name), Json);

    public void Authenticate(string accessToken) =>
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Json))!;

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions();
        JsonConfig.Configure(options);
        return options;
    }
}
