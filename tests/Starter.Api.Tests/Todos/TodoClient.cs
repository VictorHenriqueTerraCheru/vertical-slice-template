using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;
using Starter.Api.Todos;

namespace Starter.Api.Tests.Todos;

public sealed class TodoClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = CreateJson();

    public static async Task<TodoClient> ForNewUserAsync(ApiFactory factory)
    {
        var user = await Scenarios.RegisterAsync(factory);
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Tokens.AccessToken);
        return new TodoClient(http);
    }

    public static TodoClient For(HttpClient http, RegisteredUser user)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Tokens.AccessToken);
        return new TodoClient(http);
    }

    public static async Task<TodoClient> ForNewAdministratorAsync(ApiFactory factory)
    {
        var user = await Scenarios.RegisterAsync(factory);
        await factory.InDatabaseAsync(db => db.Set<User>()
            .Where(row => row.Id == user.UserId)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.Role, UserRole.Administrator)));
        var login = await user.Client.LoginAsync(user.Email, user.Password);
        login.EnsureSuccessStatusCode();
        var tokens = await ApiClient.ReadAsync<TokenPair>(login);
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return new TodoClient(http);
    }

    public static TodoClient Anonymous(ApiFactory factory) => new(factory.CreateClient());

    public Task<HttpResponseMessage> CreateAsync(string title, string? description = null) =>
        http.PostAsJsonAsync("/api/v1/todos", new CreateTodoRequest(title, description), Json);

    public async Task<TodoItemResponse> CreateOkAsync(string title, string? description = null)
    {
        var response = await CreateAsync(title, description);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<TodoItemResponse>(response);
    }

    public Task<HttpResponseMessage> PostRawAsync(string body) =>
        http.PostAsync("/api/v1/todos", new StringContent(body, Encoding.UTF8, "application/json"));

    public Task<HttpResponseMessage> GetAsync(Guid id) => http.GetAsync($"/api/v1/todos/{id}");

    public Task<HttpResponseMessage> GetPathAsync(string path) => http.GetAsync(path);

    public Task<HttpResponseMessage> UpdateAsync(Guid id, string title, string? description = null) =>
        http.PutAsJsonAsync($"/api/v1/todos/{id}", new UpdateTodoRequest(title, description), Json);

    public Task<HttpResponseMessage> DeleteAsync(Guid id) => http.DeleteAsync($"/api/v1/todos/{id}");

    public Task<HttpResponseMessage> TransitionAsync(Guid id, TodoStatus status) =>
        http.PostAsJsonAsync($"/api/v1/todos/{id}/transitions", new TransitionTodoRequest(status), Json);

    public Task<HttpResponseMessage> TransitionRawAsync(Guid id, string body) =>
        http.PostAsync($"/api/v1/todos/{id}/transitions", new StringContent(body, Encoding.UTF8, "application/json"));

    public Task<HttpResponseMessage> EventsAsync(Guid id) => http.GetAsync($"/api/v1/todos/{id}/events");

    public async Task<TodoEventResponse[]> EventsOkAsync(Guid id)
    {
        var response = await EventsAsync(id);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<TodoEventResponse[]>(response);
    }

    public Task<HttpResponseMessage> AdminListAsync(string query = "") =>
        http.GetAsync(string.IsNullOrEmpty(query) ? "/api/v1/admin/todos" : $"/api/v1/admin/todos?{query}");

    public async Task<PagedResponse<AdminTodoItemResponse>> AdminListOkAsync(string query = "")
    {
        var response = await AdminListAsync(query);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<PagedResponse<AdminTodoItemResponse>>(response);
    }

    public Task<HttpResponseMessage> ListAsync(string query = "") =>
        http.GetAsync(string.IsNullOrEmpty(query) ? "/api/v1/todos" : $"/api/v1/todos?{query}");

    public async Task<PagedResponse<TodoItemResponse>> ListOkAsync(string query = "")
    {
        var response = await ListAsync(query);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<PagedResponse<TodoItemResponse>>(response);
    }

    public static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json);

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions();
        JsonConfig.Configure(options);
        return options;
    }
}
