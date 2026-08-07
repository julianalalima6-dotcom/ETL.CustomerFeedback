using ETL.Application.Configuration;
using ETL.Application.Interfaces;
using ETL.Application.Services;
using ETL.Domain.Entities;
using ETL.Infrastructure.Extractors;
using ETL.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;



namespace ETL.Infrastructure;

/// <summary>
/// Punto único de registro de dependencias para la capa de infraestructura.
/// Mantener todo el "cableado" aquí facilita la mantenibilidad: para agregar
/// una fuente nueva solo se añade una línea AddScoped&lt;IExtractor&lt;T&gt;, NuevoExtractor&gt;()
/// (escalabilidad de configuración modular de fuentes).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEtlInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EtlOptions>(configuration.GetSection(EtlOptions.SectionName));

        // Extractores concretos, uno por fuente de datos.
        services.AddScoped<IExtractor<SurveyResponse>, CsvExtractor>();
        services.AddScoped<IExtractor<ProductReview>, DatabaseExtractor>();
        services.AddScoped<IExtractor<CustomerComment>, ApiExtractor>();

        // Loader genérico de staging, reutilizado para las tres entidades.
        services.AddScoped<IDataLoader<SurveyResponse>, StagingDataLoader<SurveyResponse>>();
        services.AddScoped<IDataLoader<ProductReview>, StagingDataLoader<ProductReview>>();
        services.AddScoped<IDataLoader<CustomerComment>, StagingDataLoader<CustomerComment>>();

        services.AddScoped<IDimensionLoader<DimCliente>, DimClienteLoader>();
        services.AddScoped<IDimensionLoader<DimCategoria>, DimCategoriaLoader>();
        services.AddScoped<IDimensionLoader<DimProducto>, DimProductoLoader>();
        services.AddScoped<DimensionLoadService>();

        services.AddScoped<ExtractionOrchestrator>();

        // HttpClient nombrado para el ApiExtractor, con política de reintentos
        // (Polly) con backoff exponencial: 3 intentos ante errores 5xx, 408 o
        // fallas de red, lo que aporta resiliencia sin acoplar el extractor a
        // la librería de reintentos.
        services.AddHttpClient(nameof(ApiExtractor), (sp, client) =>
        {
            var options = configuration.GetSection(EtlOptions.SectionName).Get<EtlOptions>() ?? new EtlOptions();
            client.BaseAddress = new Uri(options.Api.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.Api.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx y 408
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
