using System.Text.RegularExpressions;
using TouchPortal.GoXLR.Utility.Plugin.Enums;
using TouchPortal.GoXLR.Utility.Plugin.Helpers;
using TouchPortalSDK.Messages.Events;

namespace TouchPortal.GoXLR.Utility.Plugin.Modules;

public class Faders
{
    private readonly GoXlrUtilityClient _client;

    private readonly Dictionary<FaderName, MuteFunction> _muteFunctions;
    private readonly Dictionary<FaderName, MuteState> _muteState;
    private readonly Dictionary<FaderName, ChannelName> _faderChannel;
    private readonly Dictionary<ChannelName, int> _channelVolume; //Volume in Percentage

    public event Action<FaderName, int>? VolumeUpdated;
    public event Action<FaderName, MuteState>? MuteUpdated;
    public event Action<FaderName, ChannelName>? ChannelUpdated;

    public Faders(GoXlrUtilityClient client)
    {
        _client = client;

        //Register update events:
        _client.PatchEvent += MuteFunctionChangeEvent;
        _client.PatchEvent += MuteStateChangeEvent;
        _client.PatchEvent += FaderChannelChangedPatchEvent;
        _client.PatchEvent += ChannelVolumeChangeEvent;

        //Fill defaults:
        _muteFunctions = EnumHelpers.CreateDictionaryWithDefaultKeys<FaderName, MuteFunction>();
        _muteState = EnumHelpers.CreateDictionaryWithDefaultKeys<FaderName, MuteState>();
        _faderChannel = EnumHelpers.CreateDictionaryWithDefaultKeys<FaderName, ChannelName>();
        _channelVolume = EnumHelpers.CreateDictionaryWithDefaultKeys<ChannelName, int>();
    }

    private void MuteFunctionChangeEvent(object sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/fader_status/(?<faderName>\w+)/mute_type");
        if (!match.Success)
            return;

        var faderName = EnumHelpers.Parse<FaderName>(match.Groups["faderName"].Value);
        var muteFunction = EnumHelpers.Parse<MuteFunction>(patch.Value.GetString()!);

        _muteFunctions[faderName] = muteFunction;
    }

    private void MuteStateChangeEvent(object sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/fader_status/(?<faderName>\w+)/mute_state");
        if (!match.Success)
            return;

        var faderName = EnumHelpers.Parse<FaderName>(match.Groups["faderName"].Value);
        var muteState = EnumHelpers.Parse<MuteState>(patch.Value.GetString()!);
        _muteState[faderName] = muteState;

        MuteUpdated?.Invoke(faderName, muteState);
    }

    private void FaderChannelChangedPatchEvent(object? sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/fader_status/(?<faderName>\w+)/channel");
        if (!match.Success)
            return;

        var faderName = EnumHelpers.Parse<FaderName>(match.Groups["faderName"].Value);
        var channelName = EnumHelpers.Parse<ChannelName>(patch.Value.GetString()!);
        var volumeInPercent = _channelVolume[channelName];

        _faderChannel[faderName] = channelName;

        //Send state update to plugin (it's now the volume of the wrong channel).
        ChannelUpdated?.Invoke(faderName, channelName);
        VolumeUpdated?.Invoke(faderName, volumeInPercent);
    }

    private void ChannelVolumeChangeEvent(object? sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/levels/volumes/(?<channelName>\w+)");
        if (!match.Success)
            return;

        var channelName = EnumHelpers.Parse<ChannelName>(match.Groups["channelName"].Value);
        var volume = patch.Value.GetInt32();
        var volumeInPercent = ValuesHelper.FromVolumeToVolumePercentage(volume);

        _channelVolume[channelName] = volumeInPercent;

        var fader = DictionaryHelper.GetKeyFromValue(_faderChannel, channelName);

        VolumeUpdated?.Invoke(fader, volumeInPercent);
    }

    public void SetVolume(ConnectorChangeEvent message)
    {

        var volume = ValuesHelper.FromVolumePercentageToVolume(message.Value);

        var faderName = EnumHelpers.Parse<FaderName>(message.GetValue("faderName"));
        var channel = _faderChannel[faderName];

        _client.SendCommand("SetVolume", channel, volume);
    }

    public void SetMute(ActionEvent message)
    {
        var faderName = EnumHelpers.Parse<FaderName>(message.GetValue("faderName"));

        //If it's muted, then unmute, same for all muteFunctions:
        if (_muteState[faderName] is not MuteState.Unmuted)
        {
            _client.SendCommand("SetFaderMuteState", faderName, MuteState.Unmuted);
            return;
        }

        //We need to use muteFunction to determine the correct mute state:
        var muteFunction = _muteFunctions[faderName];
        var muteState = muteFunction == MuteFunction.All
            ? MuteState.MutedToAll
            : MuteState.MutedToX;

        _client.SendCommand("SetFaderMuteState", faderName, muteState);
    }
}