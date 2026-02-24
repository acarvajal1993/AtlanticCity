namespace BulkLoad.Application.Settings;

public class RabbitMQSettings
{
    public const string SectionName = "RabbitMQ";
    
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string BulkLoadQueue { get; set; } = "carga_masiva";
    public string NotificationQueue { get; set; } = "notificaciones";
}

public class SeaweedFSSettings
{
    public const string SectionName = "SeaweedFS";
    
    public string MasterUrl { get; set; } = "http://localhost:9333";
}

public class DatabaseSettings
{
    public const string SectionName = "Database";
    
    public string ConnectionString { get; set; } = string.Empty;
}

public class ProcessingSettings
{
    public const string SectionName = "Processing";
    
    public int BatchSize { get; set; } = 100;
    public string DefaultCategoria { get; set; } = "Sin Categoría";
    public decimal DefaultCantidad { get; set; } = 0;
    public decimal DefaultPrecio { get; set; } = 0;
}
