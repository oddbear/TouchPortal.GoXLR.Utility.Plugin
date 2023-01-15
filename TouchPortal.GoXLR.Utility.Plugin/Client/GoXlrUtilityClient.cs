using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TouchPortal.GoXLR.Utility.Plugin.Enums;
using WebSocketSharp;

namespace TouchPortal.GoXLR.Utility.Plugin.Client;

public class GoXlrUtilityClient : IDisposable
{
    private readonly ILogger<GoXlrUtilityClient> _logger;
    private readonly Thread _thread;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;


    private WebSocket? _client;

    private int _commandIndex = 0;

    /// <summary>Serial numbers of devices.</summary>
    public string[]? Devices;

    public Dictionary<string, JsonElement> States = new();

    /// <summary>Event handler for patches.</summary>
    public event EventHandler<Patch>? PatchEvent;
    //public event EventHandler<(PluginStatus status, string message)> PluginStatusEvent;

    public GoXlrUtilityClient(
        ILogger<GoXlrUtilityClient> logger)
    {
        _logger = logger;
        _thread = new Thread(Reconnect);
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    }

    private void Reconnect()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            bool Connected() => _client?.ReadyState == WebSocketState.Open;

            try
            {
                if (Connected())
                    continue;

                _client ??= CreateClient();

                _client.Connect();
            }
            catch (Exception exception)
            {
                if (exception.Message != "A series of reconnecting has failed.")
                {
                    _logger.LogError(exception, "A series of reconnecting has failed.");
                }

                IDisposable? oldClient = _client;
                _client = CreateClient();
                oldClient?.Dispose();
            }
            finally
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }

    private WebSocket CreateClient()
    {
        var client = new WebSocket("ws://127.0.0.1:14564/api/websocket");
        client.OnOpen += ClientOnOpen;
        client.OnClose += ClientOnClose;
        client.OnMessage += ClientOnMessage;

        return client;
    }

    public void Start()
    {
        _thread.Start();
    }

    public void SendCommand(string commandName, params object[] parameters)
    {
        if (parameters.Length < 1)
            return;

        var commandParameters = parameters.Length == 1
            ? parameters[0]
            : parameters;

        SendCommand(new Dictionary<string, object>
        {
            [commandName] = commandParameters
        });
    }

    private void SendCommand(object command)
    {
        var serial = Devices?.FirstOrDefault();

        if (serial is null)
            return;

        var id = _commandIndex++;
        var finalRequest = new
        {
            id,
            data = new
            {
                Command = new[] {
                    serial,
                    command
                }
            }
        };

        var json = JsonSerializer.Serialize(finalRequest, _jsonSerializerOptions);
        _client?.Send(json);
    }

    private void ClientOnOpen(object? sender, EventArgs eventArgs)
    {
        _client?.Send($"{{\"id\":{_commandIndex++},\"data\":\"GetStatus\"}}");
    }

    private void ClientOnClose(object? sender, CloseEventArgs closeEventArgs)
    {
        //closeEventArgs.Dump();
    }

    private void ClientOnMessage(object? sender, MessageEventArgs message)
    {
        if (message.Data is null)
            return;

        try
        {
            if (!message.IsText)
                return;

            var jsonElement = JsonSerializer.Deserialize<JsonElement>(message.Data, _jsonSerializerOptions);

            if (!jsonElement.TryGetProperty("data", out var data))
                return;

            if (data.ValueKind is not JsonValueKind.Object)
                return; //Ex. string if "data" is "Ok"

            if (data.TryGetProperty("Status", out var status))
            {
                if (status.TryGetProperty("mixers", out var mixers))
                {
                    Devices = mixers
                        .EnumerateObject()
                        .Select(property => property.Name)
                        .ToArray();
                }

                TraverseObject(status);
            }

            if (data.TryGetProperty("Patch", out var patches))
            {
                foreach (var patch in patches.Deserialize<Patch[]>(_jsonSerializerOptions)!)
                {
                    InvokePatch(patch);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Parsing GoXLR Utility message failed");
        }
    }

    private void TraverseObject(JsonElement jObject, string path = "")
    {
        foreach (var property in jObject.EnumerateObject())
        {
            var currentPath = $"{path}/{property.Name}";
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    TraverseObject(property.Value, currentPath);
                    break;

                default:
                    InvokePatch(new Patch { Op = OpPatchEnum.Replace, Path = currentPath, Value = property.Value });
                    break;
            }
        }
    }

    private void InvokePatch(Patch patch)
    {
        try
        {
            States[patch.Path] = patch.Value;
            PatchEvent?.Invoke(this, patch);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Invoke Patch failed");
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        ((IDisposable?)_client)?.Dispose();
    }
}
