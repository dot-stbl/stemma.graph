using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Voluta.UI.Studio;

/// <summary>
///     Optional shared-secret gate for Studio API routes (single-tenant default: off).
/// </summary>
public static class StudioApiKeyMiddleware
{
    /// <summary>
    ///     Header name for the API key.
    /// </summary>
    public const string HeaderName = "X-Api-Key";

    /// <summary>
    ///     Returns true when <paramref name="requiredApiKey"/> is null/empty (auth off)
    ///     or the request presents a matching key.
    /// </summary>
    /// <param name="request">Incoming HTTP request.</param>
    /// <param name="requiredApiKey">Configured key; null/empty disables the check.</param>
    /// <returns>Whether the request is authorized.</returns>
    public static bool IsAuthorized(HttpRequest request, string? requiredApiKey)
    {
        if (string.IsNullOrEmpty(requiredApiKey))
        {
            return true;
        }

        if (request.Headers.TryGetValue(HeaderName, out var headerValues)
            && FixedTimeEquals(headerValues.ToString(), requiredApiKey))
        {
            return true;
        }

        var authorization = request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";
        if (authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization[bearerPrefix.Length..].Trim();
            return FixedTimeEquals(token, requiredApiKey);
        }

        return false;
    }

    /// <summary>
    ///     Writes a 401 JSON body when unauthorized.
    /// </summary>
    /// <param name="response">HTTP response.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    public static async Task WriteUnauthorizedAsync(
        HttpResponse response,
        CancellationToken cancellationToken = default)
    {
        response.StatusCode = StatusCodes.Status401Unauthorized;
        response.ContentType = "application/json; charset=utf-8";
        await response.WriteAsJsonAsync(
            new { error = "unauthorized", code = "studio.unauthorized" },
            cancellationToken);
    }

    private static bool FixedTimeEquals(string? left, string right)
    {
        if (string.IsNullOrEmpty(left))
        {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
