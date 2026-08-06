using ETL.Infrastructure;
using ETL.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

// LoggerService (requisito de la guía): se usa Serilog como implementación
// concreta de ILogger, escribiendo a consola y a archivo con rotación diaria,
// lo que da trazabilidad y monitoreo del proceso ETL.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/etl-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

// Registro modular de la infraestructura (extractores, loaders, HttpClient + Polly).
builder.Services.AddEtlInfrastructure(builder.Configuration);

// BackgroundService que dispara el ciclo ETL en el intervalo configurado.
builder.Services.AddHostedService<EtlWorker>();

var host = builder.Build();
host.Run();