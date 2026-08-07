using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.Persistence;

public sealed class DimFechaLoader : IDimensionLoader<DimFecha>
{
    private readonly string _connectionString;
    private readonly ILogger<DimFechaLoader> _logger;

    public DimFechaLoader(IConfiguration configuration, ILogger<DimFechaLoader> logger)
    {
        _connectionString = configuration.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión DataWarehouse.");
        _logger = logger;
    }

    public async Task LoadAsync(IReadOnlyList<DimFecha> records, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string merge = @"
            MERGE INTO DimFecha AS target
            USING (VALUES (@id, @fecha, @anio, @mes, @dia))
                AS source (id_fecha, fecha, anio, mes, dia)
            ON target.id_fecha = source.id_fecha
            WHEN MATCHED THEN
                UPDATE SET fecha = source.fecha, anio = source.anio, mes = source.mes, dia = source.dia
            WHEN NOT MATCHED THEN
                INSERT (id_fecha, fecha, anio, mes, dia)
                VALUES (source.id_fecha, source.fecha, source.anio, source.mes, source.dia);";

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var command = new SqlCommand(merge, connection);
            command.Parameters.AddWithValue("@id", record.IdFecha);
            command.Parameters.AddWithValue("@fecha", record.Fecha);
            command.Parameters.AddWithValue("@anio", record.Anio);
            command.Parameters.AddWithValue("@mes", record.Mes);
            command.Parameters.AddWithValue("@dia", record.Dia);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("DimFecha: {Count} registros cargados (upsert)", records.Count);
    }
}