using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Starter.Api.Common;
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

    public Task<HttpResponseMessage> ListAsync(string query = "") =>
        http.GetAsync(string.IsNullOrEmpty(query) ? "/api/v1/todos" : $"/api/v1/todos?{query}");

    public async Task<PagedResponse<TodoItemResponse>> ListOkAsync(string query = "")
    {
        var response = await ListAsync(query);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<PagedResponse<TodoItemResponse>>(response);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions();
        JsonConfig.Configure(options);
        return options;
    }
}
