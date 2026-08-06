namespace ETL.Domain.Entities;

/// <summary>
/// Representa una respuesta de la encuesta interna de satisfacción post-compra,
/// origen: archivo CSV.
/// </summary>
public sealed class SurveyResponse
{
    public string SurveyId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public int SatisfactionScore { get; set; }
    public string? Comments { get; set; }
    public DateTime ResponseDate { get; set; }
    public string SourceFile { get; set; } = string.Empty;
}
