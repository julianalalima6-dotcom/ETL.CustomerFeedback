namespace ETL.Domain.Entities; 
public sealed class FactVenta { 
    public int IdDetalle { get; set; }
    public int IdVenta { get; set; } 
    public int IdCliente { get; set; }
    public int IdProducto { get; set; } 
    public int IdFecha { get; set; } 
    public int Cantidad { get; set; } 
    public decimal Subtotal { get; set; } }