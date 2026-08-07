using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.Persistence;

public sealed class DimCategoriaLoader : IDimensionLoader<DimCategoria>
{
    private readonly string _connectionString;
    private readonly ILogger<DimCategoriaLoader> _logger;

    public DimCategoriaLoader(IConfiguration configuration, ILogger<DimCategoriaLoader> logger)
    {
        _connectionString = configuration.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión DataWarehouse.");
        _logger = logger;
    }

    public async Task LoadAsync(IReadOnlyList<DimCategoria> records, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string merge = @"
            MERGE INTO DimCategoria AS target
            USING (VALUES (@id, @nombre)) AS source (id_categoria, nombre_categoria)
            ON target.id_categoria = source.id_categoria
            WHEN MATCHED THEN
                UPDATE SET nombre_categoria = source.nombre_categoria
            WHEN NOT MATCHED THEN
                INSERT (id_categoria, nombre_categoria)
                VALUES (source.id_categoria, source.nombre_categoria);";

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var command = new SqlCommand(merge, connection);
            command.Parameters.AddWithValue("@id", record.IdCategoria);
            command.Parameters.AddWithValue("@nombre", record.NombreCategoria);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("DimCategoria: {Count} registros cargados (upsert)", records.Count);
    }
}