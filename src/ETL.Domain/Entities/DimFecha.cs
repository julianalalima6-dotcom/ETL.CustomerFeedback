namespace ETL.Domain.Entities;
public sealed class DimFecha { 
    public int IdFecha { get; set; } 
    public DateTime Fecha { get; set; } 
    public int Anio { get; set; } 
    public int Mes { get; set; }
    public int Dia { get; set; } }