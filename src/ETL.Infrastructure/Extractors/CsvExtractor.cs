using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ETL.Application.Configuration;
using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETL.Infrastructure.Extractors;

/// <summary>
/// Extrae respuestas de la encuesta interna de satisfacción desde archivos
/// CSV ubicados en una carpeta configurable. Implementa IExtractor
/// para poder ser sustituido o testeado sin afectar al resto del sistema.
/// </summary>
public sealed class CsvExtractor : IExtractor<SurveyResponse>
{
    private readonly CsvSourceOptions _options;
    private readonly ILogger<CsvExtractor> _logger;

    public string SourceName => "CSV.EncuestasSatisfaccion";

    public CsvExtractor(IOptions<EtlOptions> options, ILogger<CsvExtractor> logger)
    {
        _options = options.Value.Csv;
        _logger = logger;
    }

    public async Task<ExtractionResult<SurveyResponse>> ExtractAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var records = new List<SurveyResponse>();

        try
        {
            if (!Directory.Exists(_options.FolderPath))
            {
                _logger.LogWarning("Carpeta CSV no existe: {Path}. Se omite la fuente.", _options.FolderPath);
                return ExtractionResult<SurveyResponse>.Ok(records, stopwatch.Elapsed);
            }

            var files = Directory.GetFiles(_options.FolderPath, _options.FilePattern);

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,   // Tolerante a columnas opcionales
                    BadDataFound = context => _logger.LogWarning(
                        "Fila inválida en {File}: {RawRecord}", file, context.RawRecord)
                };

                using var reader = new StreamReader(file);
                using var csv = new CsvReader(reader, config);

                await foreach (var record in csv.GetRecordsAsync<SurveyCsvRow>(cancellationToken))
                {
                    records.Add(new SurveyResponse
                    {
                        SurveyId = record.SurveyId,
                        CustomerId = record.CustomerId,
                        SatisfactionScore = record.SatisfactionScore,
                        Comments = record.Comments,
                        ResponseDate = record.ResponseDate,
                        SourceFile = Path.GetFileName(file)
                    });
                }
            }

            stopwatch.Stop();
            return ExtractionResult<SurveyResponse>.Ok(records, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error leyendo archivos CSV de encuestas");
            return ExtractionResult<SurveyResponse>.Fail(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>Fila cruda del CSV, mapeada 1:1 con las columnas del archivo.</summary>
    private sealed class SurveyCsvRow
    {
        public string SurveyId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public int SatisfactionScore { get; set; }
        public string? Comments { get; set; }
        public DateTime ResponseDate { get; set; }
    }
}
