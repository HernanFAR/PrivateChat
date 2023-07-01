using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace ChatHubWebApi;

public partial class ChatHubWebApiConnection
{
    public static readonly Uri ServiceUrl = new("https://privatechat-production.up.railway.app");
    private readonly ILogger<ChatHubWebApiConnection> _logger;

    public ChatHubWebApiConnection(HttpClient httpClient,
        ILogger<ChatHubWebApiConnection> logger)
    {
        BaseUrl = ServiceUrl.ToString();

        _logger = logger;
        _httpClient = httpClient;
        _settings = new Lazy<JsonSerializerSettings>(CreateSerializerSettings);
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        _logger.LogInformation("Enviando solicitud {Method} a la ruta: {Url}.", request.Method.Method, url);
    }
}
