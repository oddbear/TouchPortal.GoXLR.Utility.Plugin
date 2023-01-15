using Microsoft.Extensions.Logging;
using TouchPortal.GoXLR.Utility.Plugin.Enums;
using TouchPortal.GoXLR.Utility.Plugin.Modules;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace TouchPortal.GoXLR.Utility.Plugin.Plugin;

public class GoXlrUtilityPlugin : ITouchPortalEventHandler
{
    private readonly ILogger<GoXlrUtilityPlugin> _logger;

    private readonly ITouchPortalClient _client;

    private readonly Faders _faders;
    private readonly Channels _channels;
    private readonly Effects _effects;
    private readonly Routing _routing;
    private readonly Microphone _microphone;

    private List<ConnectorInfo> _connectors = new();

    public string PluginId => "TouchPortal.GoXLR.Utility.Plugin";

    public GoXlrUtilityPlugin(
        ITouchPortalClientFactory clientFactory,
        ILogger<GoXlrUtilityPlugin> logger,
        Faders faders,
        Channels channels,
        Effects effects,
        Routing routing,
        Microphone microphone)
    {
        _client = clientFactory.Create(this);

        _logger = logger;

        _faders = faders;
        _channels = channels;
        _effects = effects;
        _routing = routing;
        _microphone = microphone;

        _faders.VolumeUpdated += FaderVolumeUpdated;
        _faders.MuteUpdated += FaderMuteUpdated;
        _faders.ChannelUpdated += FaderChannelUpdated;

        _channels.VolumeUpdated += ChannelVolumeUpdate;

        _effects.EffectsStateUpdated += EffectsOnEffectsStateUpdated;
        _effects.EffectsPresetUpdated += EffectsOnEffectsPresetUpdated;
        _effects.EffectsEncoderAmountUpdated += EffectsOnEncoderAmountUpdated;

        _routing.RoutingUpdated += RoutingOnRoutingUpdated;

        _microphone.MuteUpdated += MicrophoneOnMuteUpdated;
    }

    private void MicrophoneOnMuteUpdated(MuteState state)
    {
        _client.StateUpdate("TouchPortal.GoXLR.Utility.Plugin.states.cough.mute", state.ToString());
    }

    private void EffectsOnEncoderAmountUpdated(EncoderName encoderName, int amount)
    {
        //TODO: I got some issue with Pitch updating all other, but the others does not update pitch. ShortId did not help.
        //var shortId = _connectors
        //    .Where(connectorInfo => connectorInfo.ConnectorId == "TouchPortal.GoXLR.Utility.Plugin.connector.effects.encoder")
        //    .Single(connectorInfo => connectorInfo.GetValue("encoderName") == encoderName.ToString())
        //    .ShortId;

        _client.ConnectorUpdate($"TouchPortal.GoXLR.Utility.Plugin.connector.effects.encoder|encoderName={encoderName}", amount);
    }

    private void RoutingOnRoutingUpdated(InputDevice input, OutputDevice output, BooleanState state)
    {
        _client.StateUpdate($"TouchPortal.GoXLR.Utility.Plugin.states.routing.{input}.{output}", state.ToString());
    }

    private void EffectsOnEffectsPresetUpdated(EffectBankPresets effectPreset, BooleanState state)
    {
        _client.StateUpdate($"TouchPortal.GoXLR.Utility.Plugin.states.effects.{effectPreset}", state.ToString());
    }

    private void EffectsOnEffectsStateUpdated(EffectsType effectsType, BooleanState state)
    {
        _client.StateUpdate($"TouchPortal.GoXLR.Utility.Plugin.states.effects.{effectsType}", state.ToString());
    }

    private void FaderChannelUpdated(FaderName faderName, ChannelName channelName)
    {
        _client.StateUpdate($"TouchPortal.GoXLR.Utility.Plugin.states.faders.{faderName}.channelname", channelName.ToString());
    }

    private void FaderVolumeUpdated(FaderName faderName, int volume)
    {
        _client.ConnectorUpdate($"TouchPortal.GoXLR.Utility.Plugin.connector.faders.volume|faderName={faderName}", volume);
        _client.StateUpdate($"TouchPortal.GoXLR.Utility.Plugin.states.faders.{faderName}.volume", volume.ToString());
    }

    private void FaderMuteUpdated(FaderName faderName, MuteState muteState)
    {
        _client.StateUpdate($"TouchPortal.GoXLR.Utility.Plugin.states.faders.{faderName}.mute", muteState.ToString());
    }

    private void ChannelVolumeUpdate(ChannelName channelName, int volume)
    {
        _client.ConnectorUpdate($"TouchPortal.GoXLR.Utility.Plugin.connector.channels.volume|channelName={channelName}", volume);
        _client.StateUpdate($"TouchPortal.GoXLR.Utility.Plugin.states.channel.{channelName}.volume", volume.ToString());
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
        if (message.ActionId.StartsWith("TouchPortal.GoXLR.Utility.Plugin.actions.routing."))
        {
            _routing.SetRouting(message);
            return;
        }

        if (message.ActionId.StartsWith("TouchPortal.GoXLR.Utility.Plugin.actions.effects."))
        {
            _effects.SetEffect(message);
            return;
        }

        switch (message.ActionId)
        {
            case "TouchPortal.GoXLR.Utility.Plugin.actions.faders.mute":
                _faders.SetMute(message);
                return;
            case "TouchPortal.GoXLR.Utility.Plugin.actions.cough.mute":
                _microphone.SetCoughMute(message);
                return;
        }
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
                _faders.SetVolume(message);
                break;
            case "TouchPortal.GoXLR.Utility.Plugin.connector.channels.volume":
                _channels.SetVolume(message);
                break;
            case "TouchPortal.GoXLR.Utility.Plugin.connector.effects.encoder":
                _effects.SetEffectAmount(message);
                break;
        }
    }

    public void OnShortConnectorIdNotificationEvent(ConnectorInfo connectorInfo)
    {
        _connectors.Add(connectorInfo);
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