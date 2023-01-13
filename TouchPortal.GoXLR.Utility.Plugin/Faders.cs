using System.Text.RegularExpressions;
using TouchPortal.GoXLR.Utility.Plugin.Enums;
using TouchPortal.GoXLR.Utility.Plugin.Helpers;
using TouchPortalSDK.Messages.Events;

namespace TouchPortal.GoXLR.Utility.Plugin;

public class Faders
{
    private readonly GoXlrUtilityClient _client;

    private readonly Dictionary<ChannelName, int> _channelVolume; //Volume in Percentage
    
    private readonly Dictionary<FaderName, ChannelName> _faderChannel;

    public event EventHandler<(FaderName fader, int volume)>? VolumeUpdated;

    public Faders(GoXlrUtilityClient client)
    {
        _client = client;

        _client.PatchEvent += FaderChannelChangedPatchEvent;
        _client.PatchEvent += ChannelVolumeChangeEvent;

        _channelVolume = Enum.GetValues<ChannelName>()
            .ToDictionary(channelName => channelName, _ => default(int));

        _faderChannel = Enum.GetValues<FaderName>()
            .ToDictionary(faderName => faderName, _ => default(ChannelName));
    }

    private void FaderChannelChangedPatchEvent(object? sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/fader_status/(?<faderName>\w+)/channel");
        if (!match.Success)
            return;

        var faderName = Enum.Parse<FaderName>(match.Groups["faderName"].Value);
        _faderChannel[faderName] = Enum.Parse<ChannelName>(patch.Value.GetString()!);

        //Send state update to plugin (it's now the volume of the wrong channel).
        RaiseUpdateFaderState(faderName);
    }

    private void ChannelVolumeChangeEvent(object? sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/levels/volumes/(?<channelName>\w+)");
        if (!match.Success)
            return;

        var channelName = Enum.Parse<ChannelName>(match.Groups["channelName"].Value);
        var volume = patch.Value.GetInt32();
        _channelVolume[channelName] = ValuesHelper.FromVolumeToVolumePercentage(volume);

        var fader = _faderChannel
            .SingleOrDefault(pair => pair.Value == channelName)
            .Key;

        RaiseUpdateFaderState(fader);
    }

    private void RaiseUpdateFaderState(FaderName fader)
    {
        var channel = _faderChannel[fader];
        var volume = _channelVolume[channel];
        VolumeUpdated?.Invoke(this, (fader, volume));
    }

    public void SetVolumeUtility(ConnectorChangeEvent message)
    {

        var volume = ValuesHelper.FromVolumePercentageToVolume(message.Value);
        
        var faderName = Enum.Parse<FaderName>(message.GetValue("faderName"));
        var channel = _faderChannel[faderName];

        _client.SendCommand("SetVolume", channel, volume);
    }
}