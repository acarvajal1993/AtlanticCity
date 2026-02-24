namespace Notifications.Application.Settings;

public class RabbitMQSettings
{
    public const string SectionName = "RabbitMQ";
    
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string NotificationQueue { get; set; } = "notificaciones";
}

public class EmailSettings
{
    public const string SectionName = "Email";
    
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Sistema de Carga Masiva";
    public bool UseSsl { get; set; } = true;
}

public class DatabaseSettings
{
    public const string SectionName = "Database";
    
    public string ConnectionString { get; set; } = string.Empty;
}
