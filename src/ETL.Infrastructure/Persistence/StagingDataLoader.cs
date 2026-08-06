using System.Text.Json;
using ETL.Application.Configuration;
using ETL.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETL.Infrastructure.Persistence;

/// <summary>
/// Implementación genérica de IDataLoader que persiste los registros
/// extraídos como JSON en el área de staging (carpeta local o, en un entorno
/// productivo, un contenedor de Blob Storage / tabla staging en la BD
/// analítica). Al ser genérica sobre T, sirve para las tres entidades sin
/// duplicar código (principio DRY).
/// </summary>
/// <typeparam name="T">Tipo de entidad a persistir.</typeparam>
public sealed class StagingDataLoader<T> : IDataLoader<T>
{
    private readonly StagingOptions _options;
    private readonly ILogger<StagingDataLoader<T>> _logger;

    public StagingDataLoader(IOptions<EtlOptions> options, ILogger<StagingDataLoader<T>> logger)
    {
        _options = options.Value.Staging;
        _logger = logger;
    }

    public async Task SaveToStagingAsync(
        string stagingTableOrFileName,
        IReadOnlyList<T> records,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.OutputFolderPath);

        var fileName = Path.GetFileNameWithoutExtension(stagingTableOrFileName) +
            $"_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        var fullPath = Path.Combine(_options.OutputFolderPath, fileName);

        await using var stream = File.Create(fullPath);
        await JsonSerializer.SerializeAsync(stream, records, new JsonSerializerOptions
        {
            WriteIndented = true
        }, cancellationToken);

        _logger.LogInformation(
            "Staging: {Count} registros de {Type} guardados en {Path}",
            records.Count, typeof(T).Name, fullPath);
    }
}
