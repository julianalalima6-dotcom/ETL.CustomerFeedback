using System.Diagnostics;
using ETL.Application.Configuration;
using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETL.Infrastructure.Extractors;

/// <summary>
/// Extrae reseñas de productos desde la base de datos transaccional
/// (SQL Server) mediante ADO.NET puro. Se usa ADO.NET en vez de EF Core
/// porque la consulta es de solo lectura, acotada y se beneficia de
/// SqlDataReader en modo streaming para volúmenes grandes (rendimiento).
/// </summary>
public sealed class DatabaseExtractor : IExtractor<ProductReview>
{
    private readonly DatabaseSourceOptions _options;
    private readonly string _connectionString;
    private readonly ILogger<DatabaseExtractor> _logger;

    public string SourceName => "SQL.ResenasProducto";

    public DatabaseExtractor(
        IOptions<EtlOptions> options,
        IConfiguration configuration,
        ILogger<DatabaseExtractor> logger)
    {
        _options = options.Value.Database;
        _logger = logger;

        // Seguridad: la cadena de conexión se resuelve desde ConnectionStrings,
        // que en desarrollo vive en User Secrets y en producción en variables
        // de entorno / Key Vault. Nunca se escribe la contraseña en el código.
        _connectionString = configuration.GetConnectionString(_options.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"No se encontró la cadena de conexión '{_options.ConnectionStringName}'.");
    }

    public async Task<ExtractionResult<ProductReview>> ExtractAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var records = new List<ProductReview>();

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(_options.ReviewsQuery, connection)
            {
                CommandTimeout = _options.CommandTimeoutSeconds
            };
            // Parámetro para extracción incremental: solo reseñas nuevas desde la última corrida.
            command.Parameters.AddWithValue("@since", DateTime.UtcNow.AddDays(-1));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(new ProductReview
                {
                    ReviewId = reader.GetInt32(reader.GetOrdinal("ReviewId")),
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    CustomerId = reader.GetString(reader.GetOrdinal("CustomerId")),
                    Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                    ReviewText = reader.IsDBNull(reader.GetOrdinal("ReviewText"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("ReviewText")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            stopwatch.Stop();
            return ExtractionResult<ProductReview>.Ok(records, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error consultando reseñas en la base de datos");
            return ExtractionResult<ProductReview>.Fail(ex.Message, stopwatch.Elapsed);
        }
    }
}
