namespace ETL.Domain.Entities;

/// <summary>
/// Representa una reseña de producto almacenada en la base de datos
/// transaccional (origen: base de datos relacional).
/// </summary>
public sealed class ProductReview
{
    public int ReviewId { get; set; }
    public int ProductId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
    public DateTime CreatedAt { get; set; }
}
