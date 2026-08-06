using ETL.Application.Configuration;
using ETL.Application.Services;
using Microsoft.Extensions.Options;

namespace ETL.Worker;

/// <summary>
/// BackgroundService que ejecuta el proceso de extracción de forma periódica.
/// Cada ciclo crea su propio scope de DI (los extractores son Scoped) y
/// delega la lógica real al ExtractionOrchestrator, manteniendo esta clase
/// enfocada solo en el "cuándo" ejecutar (separación de responsabilidades).
/// </summary>
public sealed class EtlWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EtlWorker> _logger;
    private readonly EtlOptions _options;

    public EtlWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EtlOptions> options,
        ILogger<EtlWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "EtlWorker iniciado. Intervalo de ejecución: {Minutes} minutos.",
            _options.ExecutionIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<ExtractionOrchestrator>();

            try
            {
                await orchestrator.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fallo crítico en el ciclo ETL. Se reintentará en el próximo intervalo.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.ExecutionIntervalMinutes), stoppingToken);
        }
    }
}
