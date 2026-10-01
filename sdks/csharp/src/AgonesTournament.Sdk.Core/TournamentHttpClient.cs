using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgonesTournament.Sdk.Core;

/// <summary>Shared HTTP transport. The caller supplies the service root URL and owns the HttpClient.</summary>
public sealed class TournamentHttpClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient http;
    private readonly Uri baseUrl;

    /// <summary>Creates a transport for a service root URL, preserving any deployment prefix.</summary>
    public TournamentHttpClient(HttpClient http, Uri baseUrl)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(baseUrl);
        if (!baseUrl.IsAbsoluteUri || (baseUrl.Scheme != "https" && baseUrl.Scheme != "http")
            || baseUrl.Query.Length != 0 || baseUrl.Fragment.Length != 0)
            throw new ArgumentException("Supply an absolute HTTP(S) service root URL without a query or fragment.", nameof(baseUrl));
        this.http = http;
        this.baseUrl = new Uri(baseUrl.AbsoluteUri.TrimEnd('/') + "/");
    }

    /// <summary>Reads a JSON response. API failures throw an ApiException carrying the problem code.</summary>
    public async Task<T> ReadAsync<T>(string path, string token, CancellationToken cancellationToken = default)
    {
        using var request = Request(HttpMethod.Get, path, token, null);
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException("The API returned a null response.");
    }

    /// <summary>Sends a mutation with an optional JSON body.</summary>
    public async Task SendAsync(HttpMethod method, string path, string token, object? body = null,
        CancellationToken cancellationToken = default)
    {
        using var request = Request(method, path, token, body);
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;
        ProblemDetails? problem;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // A proxy may return HTML or an empty body instead of API problem details.
            problem = null;
        }
        problem ??= new ProblemDetails
        {
            Title = response.ReasonPhrase ?? "HTTP error",
            Status = (int)response.StatusCode,
            Code = "http-error",
            Detail = "The server returned an error without valid problem details."
        };
        throw ApiException.From(response.StatusCode, problem);
    }

    private HttpRequestMessage Request(HttpMethod method, string path, string token, object? body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var uri = new Uri(baseUrl, path.TrimStart('/'));
        if (path.StartsWith("//", StringComparison.Ordinal) || !baseUrl.IsBaseOf(uri))
            throw new ArgumentException("The API path must stay within the configured service root.", nameof(path));
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        return request;
    }
}
