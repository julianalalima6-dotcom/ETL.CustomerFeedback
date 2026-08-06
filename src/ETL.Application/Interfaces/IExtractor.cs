using ETL.Domain.Entities;

namespace ETL.Application.Interfaces;

/// <summary>
/// Abstracción común para cualquier fuente de datos del proceso de extracción.
/// Aplica el principio de Inversión de Dependencias (SOLID: D) para que la capa
/// de orquestación no dependa de implementaciones concretas (CSV, BD, API),
/// sino de este contrato. Agregar una nueva fuente = crear una nueva clase que
/// implemente IExtractor y registrarla en DI, sin tocar el resto del sistema
/// (principio Open/Closed).
/// </summary>
/// <typeparam name="T">Entidad de dominio que produce esta fuente.</typeparam>
public interface IExtractor<T>
{
    /// <summary>
    /// Nombre único de la fuente, usado en logs y métricas (ej. "CSV.Encuestas").
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Ejecuta la extracción de forma asíncrona para no bloquear el hilo del
    /// Worker Service y permitir que múltiples extractores corran en paralelo.
    /// </summary>
    Task<ExtractionResult<T>> ExtractAsync(CancellationToken cancellationToken);
}
