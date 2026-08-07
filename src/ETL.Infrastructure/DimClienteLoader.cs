using ETL.Application.Configuration;
using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.Persistence;

public sealed class DimClienteLoader : IDimensionLoader<DimCliente>
{
    private readonly string _connectionString;
    private readonly ILogger<DimClienteLoader> _logger;

    public DimClienteLoader(IConfiguration configuration, ILogger<DimClienteLoader> logger)
    {
        _connectionString = configuration.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión DataWarehouse.");
        _logger = logger;
    }

    public async Task LoadAsync(IReadOnlyList<DimCliente> records, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string merge = @"
            MERGE INTO DimCliente AS target
            USING (VALUES (@id, @nombre, @apellido, @correo, @telefono))
                AS source (id_cliente, nombre, apellido, correo, telefono)
            ON target.id_cliente = source.id_cliente
            WHEN MATCHED THEN
                UPDATE SET nombre = source.nombre, apellido = source.apellido,
                           correo = source.correo, telefono = source.telefono
            WHEN NOT MATCHED THEN
                INSERT (id_cliente, nombre, apellido, correo, telefono)
                VALUES (source.id_cliente, source.nombre, source.apellido, source.correo, source.telefono);";

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var command = new SqlCommand(merge, connection);
            command.Parameters.AddWithValue("@id", record.IdCliente);
            command.Parameters.AddWithValue("@nombre", record.Nombre);
            command.Parameters.AddWithValue("@apellido", record.Apellido);
            command.Parameters.AddWithValue("@correo", record.Correo);
            command.Parameters.AddWithValue("@telefono", record.Telefono);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("DimCliente: {Count} registros cargados (upsert) en el Data Warehouse", records.Count);
    }
}