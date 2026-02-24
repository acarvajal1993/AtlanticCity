namespace Control.Application.Settings;

public class FileSettings
{
    public const string SectionName = "FileSettings";
    
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default
    public string[] AllowedExtensions { get; set; } = [".xlsx", ".xls"];
}

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
    public string VolumeUrl { get; set; } = "http://localhost:8080";
}

public class DatabaseSettings
{
    public const string SectionName = "Database";
    
    public string ConnectionString { get; set; } = string.Empty;
}
