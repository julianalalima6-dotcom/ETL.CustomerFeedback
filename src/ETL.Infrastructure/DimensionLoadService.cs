using ETL.Application.Interfaces;
using ETL.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETL.Infrastructure.Persistence;

public sealed class DimensionLoadService
{
    private readonly string _connectionString;
    private readonly IDimensionLoader<DimCliente> _clienteLoader;
    private readonly IDimensionLoader<DimCategoria> _categoriaLoader;
    private readonly IDimensionLoader<DimProducto> _productoLoader;
    private readonly ILogger<DimensionLoadService> _logger;

    public DimensionLoadService(
        IConfiguration configuration,
        IDimensionLoader<DimCliente> clienteLoader,
        IDimensionLoader<DimCategoria> categoriaLoader,
        IDimensionLoader<DimProducto> productoLoader,
        ILogger<DimensionLoadService> logger)
    {
        _connectionString = configuration.GetConnectionString("DataWarehouse")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión DataWarehouse.");
        _clienteLoader = clienteLoader;
        _categoriaLoader = categoriaLoader;
        _productoLoader = productoLoader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Inicio de carga de dimensiones ===");

        var categorias = await ReadCategoriasAsync(cancellationToken);
        await _categoriaLoader.LoadAsync(categorias, cancellationToken);

        var productos = await ReadProductosAsync(cancellationToken);
        await _productoLoader.LoadAsync(productos, cancellationToken);

        var clientes = await ReadClientesAsync(cancellationToken);
        await _clienteLoader.LoadAsync(clientes, cancellationToken);

        _logger.LogInformation("=== Fin de carga de dimensiones ===");
    }

    private async Task<List<DimCategoria>> ReadCategoriasAsync(CancellationToken cancellationToken)
    {
        var result = new List<DimCategoria>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT id_categoria, nombre_categoria FROM Categorias", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DimCategoria
            {
                IdCategoria = reader.GetInt32(reader.GetOrdinal("id_categoria")),
                NombreCategoria = reader.IsDBNull(reader.GetOrdinal("nombre_categoria"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("nombre_categoria"))
            });
        }

        return result;
    }

    private async Task<List<DimProducto>> ReadProductosAsync(CancellationToken cancellationToken)
    {
        var result = new List<DimProducto>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT id_producto, nombre_producto, precio, id_categoria FROM Productos", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DimProducto
            {
                IdProducto = reader.GetInt32(reader.GetOrdinal("id_producto")),
                NombreProducto = reader.IsDBNull(reader.GetOrdinal("nombre_producto"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("nombre_producto")),
                Precio = reader.IsDBNull(reader.GetOrdinal("precio")) ? 0 : reader.GetDecimal(reader.GetOrdinal("precio")),
                IdCategoria = reader.IsDBNull(reader.GetOrdinal("id_categoria")) ? 0 : reader.GetInt32(reader.GetOrdinal("id_categoria"))
            });
        }

        return result;
    }

    private async Task<List<DimCliente>> ReadClientesAsync(CancellationToken cancellationToken)
    {
        var result = new List<DimCliente>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT id_cliente, nombre, apellido, correo, telefono FROM Clientes", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DimCliente
            {
                IdCliente = reader.GetInt32(reader.GetOrdinal("id_cliente")),
                Nombre = reader.IsDBNull(reader.GetOrdinal("nombre")) ? string.Empty : reader.GetString(reader.GetOrdinal("nombre")),
                Apellido = reader.IsDBNull(reader.GetOrdinal("apellido")) ? string.Empty : reader.GetString(reader.GetOrdinal("apellido")),
                Correo = reader.IsDBNull(reader.GetOrdinal("correo")) ? string.Empty : reader.GetString(reader.GetOrdinal("correo")),
                Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? string.Empty : reader.GetString(reader.GetOrdinal("telefono"))
            });
        }

        return result;
    }
}