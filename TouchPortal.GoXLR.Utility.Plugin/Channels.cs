using System.Text.RegularExpressions;
using System.Threading.Channels;
using TouchPortal.GoXLR.Utility.Plugin.Enums;
using TouchPortal.GoXLR.Utility.Plugin.Helpers;
using TouchPortalSDK.Messages.Events;

namespace TouchPortal.GoXLR.Utility.Plugin
{
    public class Channels
    {
        private readonly GoXlrUtilityClient _client;
        
        public event EventHandler<(ChannelName channel, int volume)>? VolumeUpdated;

        public Channels(GoXlrUtilityClient client)
        {
            _client = client;
            
            _client.PatchEvent += ChannelVolumeChangeEvent;
        }

        private void ChannelVolumeChangeEvent(object? sender, Patch patch)
        {
            var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/levels/volumes/(?<channelName>\w+)");
            if (!match.Success)
                return;

            var channelName = Enum.Parse<ChannelName>(match.Groups["channelName"].Value);
            var volume = patch.Value.GetInt32();
            var volumePercentage = ValuesHelper.FromVolumeToVolumePercentage(volume);

            RaiseUpdateFaderState(channelName, volumePercentage);
        }

        private void RaiseUpdateFaderState(ChannelName channel, int volume)
        {
            VolumeUpdated?.Invoke(this, (channel, volume));
        }

        public void SetVolume(ConnectorChangeEvent message)
        {
            var volume = ValuesHelper.FromVolumePercentageToVolume(message.Value);
            var channel = Enum.Parse<ChannelName>(message.GetValue("channelName"));

            _client.SendCommand("SetVolume", channel, volume);
        }
    }
}
