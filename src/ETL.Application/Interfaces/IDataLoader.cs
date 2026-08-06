namespace ETL.Application.Interfaces;

/// <summary>
/// Abstracción para el componente responsable de persistir los registros
/// extraídos en el área de staging (archivos temporales o tablas staging en
/// la base de datos analítica), previo a su transformación y carga final.
/// </summary>
/// <typeparam name="T">Entidad de dominio a persistir.</typeparam>
public interface IDataLoader<T>
{
    Task SaveToStagingAsync(string stagingTableOrFileName, IReadOnlyList<T> records, CancellationToken cancellationToken);
}
