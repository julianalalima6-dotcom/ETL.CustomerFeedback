namespace ETL.Domain.Entities; 
public sealed class DimProducto { 
    public int IdProducto { get; set; } 
    public string NombreProducto { get; set; } = string.Empty;
    public decimal Precio { get; set; } 
    public int IdCategoria { get; set; } }

