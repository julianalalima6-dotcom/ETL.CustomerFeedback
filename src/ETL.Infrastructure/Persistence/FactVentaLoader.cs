using ETL.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.Persistence;

/// <summary>
/// Carga la tabla de hechos FactVentas con el patrón "clean-then-load":
/// primero se vacía la tabla por completo (TRUNCATE) y luego se insertan
/// todos los registros actuales, evitando duplicados sin necesidad de upsert.
/// </summary>
public sealed class FactVentaLoader
{
    private readonly string _connectionString;
    private readonly ILogger<FactVentaLoader> _logger;

    public FactVentaLoader(IConfiguration configuration, ILogger<FactVentaLoader> logger)
    {
        _connectionString = configuration.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión DataWarehouse.");
        _logger = logger;
    }

    public async Task LoadAsync(IReadOnlyList<FactVenta> records, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Paso de limpieza requerido: vaciar la tabla de hechos antes de recargarla.
        await using (var truncate = new SqlCommand("TRUNCATE TABLE FactVentas", connection))
        {
            await truncate.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("FactVentas: tabla limpiada (TRUNCATE) antes de la carga");
        }

        const string insert = @"
            INSERT INTO FactVentas (id_detalle, id_venta, id_cliente, id_producto, id_fecha, cantidad, subtotal)
            VALUES (@idDetalle, @idVenta, @idCliente, @idProducto, @idFecha, @cantidad, @subtotal);";

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var command = new SqlCommand(insert, connection);
            command.Parameters.AddWithValue("@idDetalle", record.IdDetalle);
            command.Parameters.AddWithValue("@idVenta", record.IdVenta);
            command.Parameters.AddWithValue("@idCliente", record.IdCliente);
            command.Parameters.AddWithValue("@idProducto", record.IdProducto);
            command.Parameters.AddWithValue("@idFecha", record.IdFecha);
            command.Parameters.AddWithValue("@cantidad", record.Cantidad);
            command.Parameters.AddWithValue("@subtotal", record.Subtotal);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("FactVentas: {Count} registros cargados (clean-then-load)", records.Count);
    }
}