using System.Diagnostics;
using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ETL.Application.Services;

/// <summary>
/// Orquesta la ejecución del proceso de extracción para las tres fuentes del
/// proyecto (CSV, base de datos relacional y API REST).
///
/// Atributo de calidad - Rendimiento: las tres extracciones se lanzan en
/// paralelo con Task.WhenAll en lugar de secuencialmente, y cada extractor
/// usa I/O asíncrono (async/await) para no bloquear hilos mientras espera
/// disco, red o la base de datos.
///
/// Atributo de calidad - Mantenibilidad: esta clase no conoce ninguna
/// implementación concreta, solo las interfaces IExtractor&lt;T&gt; e
/// IDataLoader&lt;T&gt;, siguiendo Clean Architecture (la capa Application no
/// depende de Infrastructure).
/// </summary>
public sealed class ExtractionOrchestrator
{
    private readonly IExtractor<SurveyResponse> _csvExtractor;
    private readonly IExtractor<ProductReview> _dbExtractor;
    private readonly IExtractor<CustomerComment> _apiExtractor;
    private readonly IDataLoader<SurveyResponse> _surveyLoader;
    private readonly IDataLoader<ProductReview> _reviewLoader;
    private readonly IDataLoader<CustomerComment> _commentLoader;
    private readonly ILogger<ExtractionOrchestrator> _logger;

    public ExtractionOrchestrator(
        IExtractor<SurveyResponse> csvExtractor,
        IExtractor<ProductReview> dbExtractor,
        IExtractor<CustomerComment> apiExtractor,
        IDataLoader<SurveyResponse> surveyLoader,
        IDataLoader<ProductReview> reviewLoader,
        IDataLoader<CustomerComment> commentLoader,
        ILogger<ExtractionOrchestrator> logger)
    {
        _csvExtractor = csvExtractor;
        _dbExtractor = dbExtractor;
        _apiExtractor = apiExtractor;
        _surveyLoader = surveyLoader;
        _reviewLoader = reviewLoader;
        _commentLoader = commentLoader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Inicio de ciclo ETL: {Timestamp} ===", DateTimeOffset.UtcNow);

        // Rendimiento: las 3 fuentes se extraen en paralelo, no una tras otra.
        var surveyTask = ExtractAndStageAsync(_csvExtractor, _surveyLoader, "surveys_staging.csv", cancellationToken);
        var reviewTask = ExtractAndStageAsync(_dbExtractor, _reviewLoader, "reviews_staging", cancellationToken);
        var commentTask = ExtractAndStageAsync(_apiExtractor, _commentLoader, "comments_staging.json", cancellationToken);

        await Task.WhenAll(surveyTask, reviewTask, commentTask);

        stopwatch.Stop();
        _logger.LogInformation(
            "=== Fin de ciclo ETL. Duración total: {ElapsedMs} ms ===",
            stopwatch.ElapsedMilliseconds);
    }

    private async Task ExtractAndStageAsync<T>(
        IExtractor<T> extractor,
        IDataLoader<T> loader,
        string stagingTarget,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await extractor.ExtractAsync(cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Fuente {Source} falló tras {ElapsedMs} ms: {Error}",
                    extractor.SourceName, result.Elapsed.TotalMilliseconds, result.ErrorMessage);
                return;
            }

            _logger.LogInformation(
                "Fuente {Source} extrajo {Count} registros en {ElapsedMs} ms",
                extractor.SourceName, result.RecordCount, result.Elapsed.TotalMilliseconds);

            await loader.SaveToStagingAsync(stagingTarget, result.Records, cancellationToken);
        }
        catch (Exception ex)
        {
            // Un fallo en una fuente no debe detener a las demás (aislamiento de errores).
            _logger.LogError(ex, "Excepción no controlada extrayendo la fuente {Source}", extractor.SourceName);
        }
    }
}
