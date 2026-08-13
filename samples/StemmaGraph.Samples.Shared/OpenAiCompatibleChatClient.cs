using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StemmaGraph.Samples.Shared;

/// <summary>
///     OpenAI-compatible Chat Completions client.
/// </summary>
/// <remarks>
///     Env: STEMMA_CHAT_ENDPOINT, STEMMA_CHAT_API_KEY, STEMMA_CHAT_MODEL.
/// </remarks>
public sealed class OpenAiCompatibleChatClient(HttpClient http, string model, bool ownsHttp = false) : IChatCompletionClient, IDisposable
{
    private readonly HttpClient http = http;
    private readonly string model = model;
    private readonly bool ownsHttp = ownsHttp;

    public static bool TryCreateFromEnvironment(out OpenAiCompatibleChatClient? client, out string? error)
    {
        var endpoint = Environment.GetEnvironmentVariable("STEMMA_CHAT_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("STEMMA_CHAT_API_KEY");
        var model = Environment.GetEnvironmentVariable("STEMMA_CHAT_MODEL") ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            client = null;
            error =
                "Set STEMMA_CHAT_ENDPOINT and STEMMA_CHAT_API_KEY (optional STEMMA_CHAT_MODEL), or use --offline.";
            return false;
        }

        var http = new HttpClient
        {
            BaseAddress = new Uri(endpoint.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(2),
        };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        client = new OpenAiCompatibleChatClient(http, model, ownsHttp: true);
        error = null;
        return true;
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var body = new ChatRequest(
            model,
            [
                new ChatMessageDto("system", systemPrompt),
                new ChatMessageDto("user", userPrompt),
            ]);

        using var response = await http.PostAsJsonAsync(
            "chat/completions",
            body,
            ChatJsonContext.Default.ChatRequest,
            cancellationToken);

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Chat API {(int)response.StatusCode}: {raw}");
        }

        var parsed = JsonSerializer.Deserialize(raw, ChatJsonContext.Default.ChatResponse);
        var content = parsed?.Choices is { Count: > 0 } choices
            ? choices[0].Message?.Content
            : null;

        return string.IsNullOrWhiteSpace(content)
            ? throw new InvalidOperationException("Chat API returned empty content.")
            : content.Trim();
    }

    public void Dispose()
    {
        if (ownsHttp)
        {
            http.Dispose();
        }
    }
}

internal sealed record ChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessageDto> Messages);

internal sealed record ChatMessageDto(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record ChatResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<ChatChoiceDto>? Choices);

internal sealed record ChatChoiceDto(
    [property: JsonPropertyName("message")] ChatMessageDto? Message);

[JsonSerializable(typeof(ChatRequest))]
[JsonSerializable(typeof(ChatResponse))]
internal sealed partial class ChatJsonContext : JsonSerializerContext;
