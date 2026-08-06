namespace ETL.Domain.Entities;

/// <summary>
/// Representa un comentario de soporte/redes sociales recuperado desde
/// el API REST del proveedor de atención al cliente.
/// </summary>
public sealed class CustomerComment
{
    public string CommentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty; // Twitter, Chat, Email, etc.
    public string Text { get; set; } = string.Empty;
    public string? Sentiment { get; set; }
    public DateTime PostedAt { get; set; }
}
