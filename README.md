# ETL.CustomerFeedback

Worker Service en **.NET 8** que implementa el proceso de **Extracción (E)** de
un pipeline ETL para análisis de satisfacción del cliente, integrando tres
fuentes heterogéneas:

| Fuente | Contenido | Tecnología |
|---|---|---|
| CSV | Encuestas internas de satisfacción post-compra | CsvHelper |
| Base de datos relacional (SQL Server) | Reseñas de productos | ADO.NET (Microsoft.Data.SqlClient) |
| API REST | Comentarios de soporte / redes sociales | HttpClient + IHttpClientFactory + Polly |

## Arquitectura

Solución organizada en 4 proyectos siguiendo **Clean Architecture**:

```
ETL.Domain          → Entidades puras, sin dependencias externas
ETL.Application     → Interfaces (IExtractor, IDataLoader), opciones y el orquestador
ETL.Infrastructure   → Implementaciones concretas: CsvExtractor, DatabaseExtractor, ApiExtractor, StagingDataLoader
ETL.Worker           → Host, BackgroundService (EtlWorker), Program.cs, appsettings.json
```

La regla de dependencias apunta siempre hacia el centro (Domain): `Worker → Infrastructure → Application → Domain`.

## Cómo ejecutar

```bash
cd src/ETL.Worker
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ReviewsDatabase" "Server=...;Database=...;User Id=...;Password=...;"
export SUPPORT_API_KEY="tu-api-key"
dotnet run
```

## Configuración

Toda la configuración de fuentes (rutas, endpoints, timeouts) está
centralizada en `appsettings.json`, sección `Etl`. Las credenciales
(cadena de conexión y API key) **nunca** se guardan en el repositorio: se
inyectan vía `dotnet user-secrets` en desarrollo o variables de entorno en
producción.

## Cómo agregar una nueva fuente de datos

1. Crear una clase en `ETL.Infrastructure/Extractors` que implemente `IExtractor<TEntidad>`.
2. Registrarla en `DependencyInjection.cs` con `services.AddScoped<IExtractor<TEntidad>, NuevoExtractor>();`.
3. Agregar su sección de configuración en `EtlOptions` y `appsettings.json`.

No es necesario modificar el orquestador ni el Worker (principio Open/Closed).

## Logs

Se usa Serilog (sobre `ILogger`) con salida a consola y a archivo con
rotación diaria en `logs/etl-YYYYMMDD.log`, registrando inicio/fin de cada
extractor, cantidad de registros y duración (para validar rendimiento con
Stopwatch/logging).
