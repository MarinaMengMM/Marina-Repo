// SharedModels/RabbitMqService.cs
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace SharedModels;

public abstract class RabbitMqServiceBase : IAsyncDisposable
{
    protected readonly IConnection Connection;
    protected readonly IChannel Channel;
    protected readonly string QueueName;
    protected bool Disposed = false;

    protected RabbitMqServiceBase(string queueName)
    {
        var settings = LoadSettings();
        QueueName = string.IsNullOrEmpty(queueName) ? settings.QueueName ?? throw new ArgumentException("Queue name must be provided.") : queueName;

        var factory = new ConnectionFactory
        {
            HostName = settings.HostName ?? "localhost",
            Port = settings.Port ?? 5672,
            UserName = settings.UserName ?? "guest",
            Password = settings.Password ?? "guest",
            VirtualHost = settings.VirtualHost ?? "/"
        };

        // 同步方法获取异步结果（用于构造函数）
        Connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        Channel = Connection.CreateChannelAsync().GetAwaiter().GetResult();

        // 声明队列
        Channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false)
            .GetAwaiter().GetResult();
    }

    private static RabbitMqSettings LoadSettings()
    {
        var settings = new RabbitMqSettings();
        var env = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RABBITMQ_HOST"] = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
            ["RABBITMQ_PORT"] = Environment.GetEnvironmentVariable("RABBITMQ_PORT"),
            ["RABBITMQ_USER"] = Environment.GetEnvironmentVariable("RABBITMQ_USER"),
            ["RABBITMQ_PASSWORD"] = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD"),
            ["RABBITMQ_VHOST"] = Environment.GetEnvironmentVariable("RABBITMQ_VHOST"),
            ["RABBITMQ_QUEUE"] = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE")
        };

        foreach (var kvp in env)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                settings.Apply(kvp.Key, kvp.Value!);
            }
        }

        var dotEnvValues = LoadDotEnvFile();
        foreach (var kvp in dotEnvValues)
        {
            if (string.IsNullOrEmpty(settings.GetValue(kvp.Key)))
            {
                settings.Apply(kvp.Key, kvp.Value);
            }
        }

        var appSettings = LoadAppSettings();
        foreach (var kvp in appSettings)
        {
            if (string.IsNullOrEmpty(settings.GetValue(kvp.Key)))
            {
                settings.Apply(kvp.Key, kvp.Value);
            }
        }

        return settings;
    }

    private static IReadOnlyDictionary<string, string> LoadDotEnvFile()
    {
        var path = FindFileUpwards(".env");
        if (path is null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || !line.Contains('='))
                continue;

            var index = line.IndexOf('=');
            var key = line.Substring(0, index).Trim();
            var value = line.Substring(index + 1).Trim().Trim('"');
            if (!string.IsNullOrEmpty(key))
            {
                data[key] = value;
            }
        }

        return data;
    }

    private static IReadOnlyDictionary<string, string> LoadAppSettings()
    {
        var path = FindFileUpwards("appsettings.json");
        if (path is null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("RabbitMQ", out var rabbitSection))
            {
                if (rabbitSection.TryGetProperty("Host", out var host)) values["RABBITMQ_HOST"] = host.GetString() ?? string.Empty;
                if (rabbitSection.TryGetProperty("Port", out var port)) values["RABBITMQ_PORT"] = port.GetRawText();
                if (rabbitSection.TryGetProperty("User", out var user)) values["RABBITMQ_USER"] = user.GetString() ?? string.Empty;
                if (rabbitSection.TryGetProperty("Password", out var password)) values["RABBITMQ_PASSWORD"] = password.GetString() ?? string.Empty;
                if (rabbitSection.TryGetProperty("VirtualHost", out var vhost)) values["RABBITMQ_VHOST"] = vhost.GetString() ?? string.Empty;
                if (rabbitSection.TryGetProperty("Queue", out var queue)) values["RABBITMQ_QUEUE"] = queue.GetString() ?? string.Empty;
            }
            return values;
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? FindFileUpwards(string fileName)
    {
        var searchPaths = new[] { Environment.CurrentDirectory, AppContext.BaseDirectory };
        foreach (var startPath in searchPaths)
        {
            var directory = new DirectoryInfo(startPath);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, fileName);
                if (File.Exists(candidate))
                    return candidate;
                directory = directory.Parent;
            }
        }

        return null;
    }

    protected byte[] SerializeMessage<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(json);
    }

    protected T DeserializeMessage<T>(byte[] body)
    {
        var json = Encoding.UTF8.GetString(body);
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize message");
    }

// 实现 IAsyncDisposable
    public async ValueTask DisposeAsync()
    {
        if (!Disposed)
        {
            // ✅ 使用异步方法关闭
            if (Channel != null)
            {
                await Channel.CloseAsync();
                Channel.Dispose();
            }
            
            if (Connection != null)
            {
                await Connection.CloseAsync();
                Connection.Dispose();
            }
            
            Disposed = true;
        }
    }

    private sealed class RabbitMqSettings
    {
        public string? HostName { get; set; }
        public int? Port { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? VirtualHost { get; set; }
        public string? QueueName { get; set; }

        public void Apply(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            switch (key.ToUpperInvariant())
            {
                case "RABBITMQ_HOST":
                    HostName = value;
                    break;
                case "RABBITMQ_PORT":
                    if (int.TryParse(value, out var parsedPort))
                        Port = parsedPort;
                    break;
                case "RABBITMQ_USER":
                    UserName = value;
                    break;
                case "RABBITMQ_PASSWORD":
                    Password = value;
                    break;
                case "RABBITMQ_VHOST":
                    VirtualHost = value;
                    break;
                case "RABBITMQ_QUEUE":
                    QueueName = value;
                    break;
            }
        }

        public string? GetValue(string key) => key.ToUpperInvariant() switch
        {
            "RABBITMQ_HOST" => HostName,
            "RABBITMQ_PORT" => Port.ToString(),
            "RABBITMQ_USER" => UserName,
            "RABBITMQ_PASSWORD" => Password,
            "RABBITMQ_VHOST" => VirtualHost,
            "RABBITMQ_QUEUE" => QueueName,
            _ => null
        };
    }
}