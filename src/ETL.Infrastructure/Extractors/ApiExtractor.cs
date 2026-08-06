using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ETL.Application.Configuration;
using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETL.Infrastructure.Extractors;

/// <summary>
/// Consume el API REST público JSONPlaceholder (https://jsonplaceholder.typicode.com/comments)
/// como fuente real de comentarios para esta demostración. Usa IHttpClientFactory
/// (evita el agotamiento de sockets) y la política de reintentos con backoff
/// exponencial registrada en DependencyInjection.cs vía Polly.
/// </summary>
public sealed class ApiExtractor : IExtractor<CustomerComment>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiSourceOptions _options;
    private readonly ILogger<ApiExtractor> _logger;

    public string SourceName => "API.ComentariosSoporte";

    public ApiExtractor(
        IHttpClientFactory httpClientFactory,
        IOptions<EtlOptions> options,
        ILogger<ApiExtractor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value.Api;
        _logger = logger;
    }

    public async Task<ExtractionResult<CustomerComment>> ExtractAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(ApiExtractor));

            // Si el API real requiriera autenticación, el token se leería aquí de
            // una variable de entorno (nunca de appsettings.json). JSONPlaceholder
            // es público y no la exige, así que esto queda como no-op si falta.
            var apiKey = Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var allComments = new List<CustomerComment>();
            var page = 1;
            const int pageSize = 100;

            // JSONPlaceholder pagina con _page y _limit, y devuelve un arreglo
            // simple (no un objeto envoltorio con Items/HasMore).
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var url = $"{_options.CommentsEndpoint}?_page={page}&_limit={pageSize}";
                var pageItems = await client.GetFromJsonAsync<List<CommentApiItem>>(url, cancellationToken);

                if (pageItems is null || pageItems.Count == 0)
                {
                    break;
                }

                allComments.AddRange(pageItems.Select(item => new CustomerComment
                {
                    CommentId = item.Id.ToString(),
                    CustomerId = item.Email,
                    Channel = "Email",
                    Text = item.Body,
                    Sentiment = null,
                    PostedAt = DateTime.UtcNow
                }));

                // Si la página vino incompleta, ya no hay más datos.
                if (pageItems.Count < pageSize)
                {
                    break;
                }

                page++;
            }

            stopwatch.Stop();
            return ExtractionResult<CustomerComment>.Ok(allComments, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error consumiendo el API de comentarios de soporte");
            return ExtractionResult<CustomerComment>.Fail(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>DTO que refleja el contrato real de JSONPlaceholder: postId, id, name, email, body.</summary>
    private sealed class CommentApiItem
    {
        public int PostId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}