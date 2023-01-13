using Microsoft.Extensions.Logging;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace TouchPortal.GoXLR.Utility.Plugin;

public class GoXlrUtilityPlugin : ITouchPortalEventHandler
{
    private readonly ILogger<GoXlrUtilityPlugin> _logger;

    private readonly ITouchPortalClient _client;

    private readonly Faders _faders;
    private readonly Channels _channels;

    public string PluginId => "TouchPortal.GoXLR.Utility.Plugin";

    public GoXlrUtilityPlugin(
        ITouchPortalClientFactory clientFactory,
        ILogger<GoXlrUtilityPlugin> logger,
        Faders faders,
        Channels channels)
    {
        _client = clientFactory.Create(this);
        
        _logger = logger;

        _faders = faders;
        _channels = channels;

        _faders.VolumeUpdated += (_, tuple) => _client.ConnectorUpdate($"TouchPortal.GoXLR.Utility.Plugin.connector.faders.volume|faderName={tuple.fader}", tuple.volume);
        _channels.VolumeUpdated += (_, tuple) => _client.ConnectorUpdate($"TouchPortal.GoXLR.Utility.Plugin.connector.channels.volume|channelName={tuple.channel}", tuple.volume);
    }

    public void Run()
    {
        //Connect to Touch Portal:
        _client.Connect();
    }

    public void OnInfoEvent(InfoEvent message)
    {
        //
    }

    public void OnListChangedEvent(ListChangeEvent message)
    {
        //
    }

    public void OnBroadcastEvent(BroadcastEvent message)
    {
        //
    }

    public void OnSettingsEvent(SettingsEvent message)
    {
        //
    }

    public void OnActionEvent(ActionEvent message)
    {
        //
    }

    public void OnNotificationOptionClickedEvent(NotificationOptionClickedEvent message)
    {
        //
    }

    public void OnConnecterChangeEvent(ConnectorChangeEvent message)
    {
        switch (message.ConnectorId)
        {
            case "TouchPortal.GoXLR.Utility.Plugin.connector.faders.volume":
                _faders.SetVolumeUtility(message);
                break;
            case "TouchPortal.GoXLR.Utility.Plugin.connector.channels.volume":
                _channels.SetVolume(message);
                break;
        }
    }
    
    public void OnShortConnectorIdNotificationEvent(ConnectorInfo connectorInfo)
    {
        //TODO:
    }

    public void OnClosedEvent(string message)
    {
        Environment.Exit(0);
    }

    public void OnUnhandledEvent(string jsonMessage)
    {
        //
    }
}