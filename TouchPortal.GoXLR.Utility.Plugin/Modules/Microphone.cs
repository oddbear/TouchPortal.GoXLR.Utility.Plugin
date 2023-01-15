using System.Text.RegularExpressions;
using TouchPortal.GoXLR.Utility.Plugin.Client;
using TouchPortal.GoXLR.Utility.Plugin.Enums;
using TouchPortal.GoXLR.Utility.Plugin.Helpers;
using TouchPortalSDK.Messages.Events;

namespace TouchPortal.GoXLR.Utility.Plugin.Modules;

public class Microphone
{
    private readonly GoXlrUtilityClient _client;

    private MuteState _muteState;
    private MuteFunction _muteType;

    public event Action<MuteState>? MuteUpdated;

    public Microphone(GoXlrUtilityClient client)
    {
        _client = client;

        _client.PatchEvent += CoughMuteTypeChangEvent;
        _client.PatchEvent += CoughStateChangeEvent;
    }
    
    private void CoughMuteTypeChangEvent(object sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/cough_button/mute_type");
        if (!match.Success)
            return;
        
        _muteType = EnumHelpers.Parse<MuteFunction>(patch.Value.GetString()!);
    }

    private void CoughStateChangeEvent(object sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/cough_button/state");
        if (!match.Success)
            return;
        
        _muteState = EnumHelpers.Parse<MuteState>(patch.Value.GetString()!);

        MuteUpdated?.Invoke(_muteState);
    }

    //TODO: Why does this not work? Also Set sampler bank does not work.
    public void SetCoughMute(ActionEvent message)
    {
        //If it's muted, then unmute, same for all muteFunctions:
        if (_muteState is not MuteState.Unmuted)
        {
            _client.SendCommand("SetCoughMuteState", MuteState.Unmuted);
            return;
        }

        //Or mute by rule (is this correct to assume?):
        var muteState = _muteType == MuteFunction.All
            ? MuteState.MutedToAll
            : MuteState.MutedToX;

        _client.SendCommand("SetCoughMuteState", muteState);
    }
}
