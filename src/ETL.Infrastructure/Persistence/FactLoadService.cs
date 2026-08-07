using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.Persistence;

public sealed class FactLoadService
{
    private readonly string _connectionString;
    private readonly IDimensionLoader<DimFecha> _fechaLoader;
    private readonly FactVentaLoader _factVentaLoader;
    private readonly ILogger<FactLoadService> _logger;

    public FactLoadService(
        IConfiguration configuration,
        IDimensionLoader<DimFecha> fechaLoader,
        FactVentaLoader factVentaLoader,
        ILogger<FactLoadService> logger)
    {
        _connectionString = configuration.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión DataWarehouse.");
        _fechaLoader = fechaLoader;
        _factVentaLoader = factVentaLoader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Inicio de carga de hechos (FactVentas) ===");

        var (fechas, hechos) = await ReadVentasConDetalleAsync(cancellationToken);

        // Las fechas se cargan primero (upsert), porque FactVentas depende de ellas (llave foránea).
        await _fechaLoader.LoadAsync(fechas, cancellationToken);

        // FactVentas usa "clean-then-load": limpia la tabla y la recarga completa.
        await _factVentaLoader.LoadAsync(hechos, cancellationToken);

        _logger.LogInformation("=== Fin de carga de hechos ===");
    }

    private async Task<(List<DimFecha> fechas, List<FactVenta> hechos)> ReadVentasConDetalleAsync(
        CancellationToken cancellationToken)
    {
        var fechasPorId = new Dictionary<int, DimFecha>();
        var hechos = new List<FactVenta>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string query = @"
            SELECT dv.id_detalle, dv.id_venta, v.id_cliente, dv.id_producto,
                   v.fecha_venta, dv.cantidad, dv.subtotal
            FROM Detalle_Ventas dv
            INNER JOIN Ventas v ON dv.id_venta = v.id_venta";

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var fechaVenta = reader.GetDateTime(reader.GetOrdinal("fecha_venta"));

            // id_fecha con formato AAAAMMDD, convención estándar de superficie de fecha en DW.
            var idFecha = fechaVenta.Year * 10000 + fechaVenta.Month * 100 + fechaVenta.Day;

            if (!fechasPorId.ContainsKey(idFecha))
            {
                fechasPorId[idFecha] = new DimFecha
                {
                    IdFecha = idFecha,
                    Fecha = fechaVenta.Date,
                    Anio = fechaVenta.Year,
                    Mes = fechaVenta.Month,
                    Dia = fechaVenta.Day
                };
            }

            hechos.Add(new FactVenta
            {
                IdDetalle = reader.GetInt32(reader.GetOrdinal("id_detalle")),
                IdVenta = reader.GetInt32(reader.GetOrdinal("id_venta")),
                IdCliente = reader.GetInt32(reader.GetOrdinal("id_cliente")),
                IdProducto = reader.GetInt32(reader.GetOrdinal("id_producto")),
                IdFecha = idFecha,
                Cantidad = reader.GetInt32(reader.GetOrdinal("cantidad")),
                Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal"))
            });
        }

        return (fechasPorId.Values.ToList(), hechos);
    }
}