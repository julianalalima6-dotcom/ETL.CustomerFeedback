namespace ETL.Application.Configuration;

/// <summary>
/// Opciones raíz que agrupan la configuración de todas las fuentes.
/// Se enlaza desde la sección "Etl" de appsettings.json mediante el patrón
/// Options de .NET, lo que centraliza la configuración (fuentes, rutas,
/// credenciales) tal como exige la guía de la práctica.
/// </summary>
public sealed class EtlOptions
{
    public const string SectionName = "Etl";

    public CsvSourceOptions Csv { get; set; } = new();
    public DatabaseSourceOptions Database { get; set; } = new();
    public ApiSourceOptions Api { get; set; } = new();
    public StagingOptions Staging { get; set; } = new();

    /// <summary>
    /// Intervalo (en minutos) entre ejecuciones del proceso ETL completo.
    /// </summary>
    public int ExecutionIntervalMinutes { get; set; } = 60;
}

public sealed class CsvSourceOptions
{
    public bool Enabled { get; set; } = true;
    public string FolderPath { get; set; } = "Data/Surveys";
    public string FilePattern { get; set; } = "*.csv";
}

public sealed class DatabaseSourceOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Nombre lógico de la cadena de conexión (definida en ConnectionStrings).
    /// Nunca se guarda la contraseña en texto plano en este archivo: en
    /// desarrollo se usa dotnet user-secrets y en producción variables de
    /// entorno o un vault (Azure Key Vault / AWS Secrets Manager).
    /// </summary>
    public string ConnectionStringName { get; set; } = "ReviewsDatabase";
    public string ReviewsQuery { get; set; } =
        "SELECT ReviewId, ProductId, CustomerId, Rating, ReviewText, CreatedAt FROM dbo.ProductReviews WHERE CreatedAt >= @since";
    public int CommandTimeoutSeconds { get; set; } = 30;
}

public sealed class ApiSourceOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://api.support-provider.example.com/";
    public string CommentsEndpoint { get; set; } = "v1/comments";

    /// <summary>
    /// Nombre de la variable de entorno que contiene el API Key. El valor
    /// nunca se guarda en appsettings.json (ver sección de seguridad).
    /// </summary>
    public string ApiKeyEnvironmentVariable { get; set; } = "SUPPORT_API_KEY";
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxRetryAttempts { get; set; } = 3;
}

public sealed class StagingOptions
{
    public string OutputFolderPath { get; set; } = "Data/Staging";
}
