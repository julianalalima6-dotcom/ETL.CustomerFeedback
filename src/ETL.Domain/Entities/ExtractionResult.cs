namespace ETL.Domain.Entities;

/// <summary>
/// Resultado estándar devuelto por cualquier IExtractor. Permite a la capa de
/// orquestación registrar métricas de rendimiento y manejar errores de forma
/// uniforme sin importar la fuente de datos.
/// </summary>
/// <typeparam name="T">Tipo de entidad extraída (SurveyResponse, ProductReview, CustomerComment).</typeparam>
public sealed class ExtractionResult<T>
{
    public bool Success { get; init; }
    public IReadOnlyList<T> Records { get; init; } = Array.Empty<T>();
    public int RecordCount => Records.Count;
    public TimeSpan Elapsed { get; init; }
    public string? ErrorMessage { get; init; }

    public static ExtractionResult<T> Ok(IReadOnlyList<T> records, TimeSpan elapsed) =>
        new() { Success = true, Records = records, Elapsed = elapsed };

    public static ExtractionResult<T> Fail(string errorMessage, TimeSpan elapsed) =>
        new() { Success = false, ErrorMessage = errorMessage, Elapsed = elapsed };
}
