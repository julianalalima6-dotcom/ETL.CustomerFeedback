namespace ETL.Application.Interfaces; 
public interface IDimensionLoader<T> { Task LoadAsync(IReadOnlyList<T> records, CancellationToken cancellationToken); }