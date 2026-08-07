using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.Persistence;

public sealed class DimProductoLoader : IDimensionLoader<DimProducto>
{
    private readonly string _connectionString;
    private readonly ILogger<DimProductoLoader> _logger;

    public DimProductoLoader(IConfiguration configuration, ILogger<DimProductoLoader> logger)
    {
        _connectionString = configuration.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión DataWarehouse.");
        _logger = logger;
    }

    public async Task LoadAsync(IReadOnlyList<DimProducto> records, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string merge = @"
            MERGE INTO DimProducto AS target
            USING (VALUES (@id, @nombre, @precio, @idCategoria))
                AS source (id_producto, nombre_producto, precio, id_categoria)
            ON target.id_producto = source.id_producto
            WHEN MATCHED THEN
                UPDATE SET nombre_producto = source.nombre_producto,
                           precio = source.precio, id_categoria = source.id_categoria
            WHEN NOT MATCHED THEN
                INSERT (id_producto, nombre_producto, precio, id_categoria)
                VALUES (source.id_producto, source.nombre_producto, source.precio, source.id_categoria);";

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var command = new SqlCommand(merge, connection);
            command.Parameters.AddWithValue("@id", record.IdProducto);
            command.Parameters.AddWithValue("@nombre", record.NombreProducto);
            command.Parameters.AddWithValue("@precio", record.Precio);
            command.Parameters.AddWithValue("@idCategoria", record.IdCategoria);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("DimProducto: {Count} registros cargados (upsert)", records.Count);
    }
}