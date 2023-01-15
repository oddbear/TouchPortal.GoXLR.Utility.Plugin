using System.Text.RegularExpressions;
using TouchPortal.GoXLR.Utility.Plugin.Client;
using TouchPortal.GoXLR.Utility.Plugin.Enums;
using TouchPortal.GoXLR.Utility.Plugin.Helpers;
using TouchPortalSDK.Messages.Events;

namespace TouchPortal.GoXLR.Utility.Plugin.Modules;

public class Effects
{
    private readonly GoXlrUtilityClient _client;

    private readonly Dictionary<EffectsType, BooleanState> _currentEffectState;
    private readonly Dictionary<EffectBankPresets, BooleanState> _currentPresetState;

    public event Action<EffectsType, BooleanState>? EffectsStateUpdated;
    public event Action<EffectBankPresets, BooleanState>? EffectsPresetUpdated;
    public event Action<EncoderName, int>? EffectsEncoderAmountUpdated;

    public Effects(GoXlrUtilityClient client)
    {
        _client = client;

        _client.PatchEvent += ActivePresetChangeEvent;
        _client.PatchEvent += EffectAmountChangeEvent;
        _client.PatchEvent += ChannelVolumeChangeEvent;

        _currentEffectState = EnumHelpers.CreateDictionaryWithDefaultKeys<EffectsType, BooleanState>();
        _currentPresetState = EnumHelpers.CreateDictionaryWithDefaultKeys<EffectBankPresets, BooleanState>();
    }

    private void ActivePresetChangeEvent(object sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/effects/active_preset");
        if (!match.Success)
            return;

        //TODO: There might be some better way here, ex. instead of On/Off, use Preset -> choice [ Preset1, Preset2, .. ], but how to get Events working best then?
        var activePreset = EnumHelpers.Parse<EffectBankPresets>(patch.Value.GetString()!);
        foreach (var preset in EnumHelpers.GetValues<EffectBankPresets>())
        {
            var booleanState = preset == activePreset
                ? BooleanState.On
                : BooleanState.Off;

            _currentPresetState[preset] = booleanState;
            EffectsPresetUpdated?.Invoke(preset, booleanState);
        }
    }
    
    private void EffectAmountChangeEvent(object sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/effects/(?<effectPath>.+)");
        if (!match.Success)
            return;

        var effectPath = match.Groups["effectPath"].Value;

        EncoderName? encoderName = effectPath switch
        {
            "current/reverb/amount" => EncoderName.Reverb,
            "current/pitch/amount" => EncoderName.Pitch,
            "current/echo/amount" => EncoderName.Echo,
            "current/gender/amount" => EncoderName.Gender,
            _ => null
        };

        if (encoderName is null)
            return;

        var amount = patch.Value.GetSByte();

        var percentage = encoderName switch
        {
            EncoderName.Reverb => ValuesHelper.FromValueToPercentage(amount, 0, 100),
            EncoderName.Pitch => ValuesHelper.FromValueToPercentage(amount, -24, 24),
            EncoderName.Echo => ValuesHelper.FromValueToPercentage(amount, 0, 100),
            EncoderName.Gender => ValuesHelper.FromValueToPercentage(amount, -12, 12),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        EffectsEncoderAmountUpdated?.Invoke(encoderName.Value, percentage);
    }

    private void ChannelVolumeChangeEvent(object sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/effects/(?<effectPath>.+)");
        if (!match.Success)
            return;

        var effectPath = match.Groups["effectPath"].Value;
        
        EffectsType? effectType = effectPath switch
        {
            "is_enabled" => EffectsType.Fx,
            "current/megaphone/is_enabled" => EffectsType.Megaphone,
            "current/robot/is_enabled" => EffectsType.Robot,
            "current/hard_tune/is_enabled" => EffectsType.Hardtune,
            _ => null
        };

        if (effectType is null)
            return;

        var booleanState = patch.Value.GetBoolean()
            ? BooleanState.On
            : BooleanState.Off;

        _currentEffectState[effectType.Value] = booleanState;
        EffectsStateUpdated?.Invoke(effectType.Value, booleanState);
    }

    public void SetEffect(ActionEvent message)
    {
        var match = Regex.Match(message.ActionId, @"TouchPortal.GoXLR.Utility.Plugin.actions.effects.(?<effectType>\w+)");
        if (!match.Success)
            return;

        var effectType = match.Groups["effectType"].Value;

        switch (effectType)
        {
            case "Preset":
                SetEffectPreset(message);
                break;
            case "Fx":
            case "Megaphone":
            case "Robot":
            case "Hardtune":
                SetEffectTypeEnabled(message);
                break;
        }
    }

    public void SetEffectAmount(ConnectorChangeEvent message)
    {
        var encoderName = EnumHelpers.Parse<EncoderName>(message.GetValue("encoderName"));

        var command = GetCommand(encoderName);

        var percentage = message.Value;
        var amount = encoderName switch
        {
            EncoderName.Reverb => ValuesHelper.FromPercentageToValue(percentage, 0, 100),
            EncoderName.Pitch => ValuesHelper.FromPercentageToValue(percentage, -24, 24),
            EncoderName.Echo => ValuesHelper.FromPercentageToValue(percentage, 0, 100),
            EncoderName.Gender => ValuesHelper.FromPercentageToValue(percentage, -12, 12),
            _ => throw new ArgumentOutOfRangeException()
        };

        _client.SendCommand(command, amount);
    }

    private void SetEffectPreset(ActionEvent message)
    {
        var bankPresets = EnumHelpers.Parse<EffectBankPresets>(message.GetValue("presetName"));
        _client.SendCommand("SetActiveEffectPreset", bankPresets);
    }

    private void SetEffectTypeEnabled(ActionEvent message)
    {
        var effectsType = EnumHelpers.Parse<EffectsType>(message.ActionId.Split('.')[^1]);
        var actionType = EnumHelpers.Parse<ActionType>(message.GetValue("actionType"));

        var command = GetCommand(effectsType);
        var enable = GetNewEffectsState(effectsType, actionType);

        _client.SendCommand(command, enable);
    }

    private string GetCommand(EncoderName encoderName)
        => encoderName switch
        {
            EncoderName.Echo => "SetEchoAmount",
            EncoderName.Reverb => "SetReverbAmount",
            EncoderName.Pitch => "SetPitchAmount",
            EncoderName.Gender => "SetGenderAmount",
            _ => throw new ArgumentOutOfRangeException()
        };

    private string GetCommand(EffectsType effectsType)
        => effectsType switch
        {
            EffectsType.Fx => "SetFXEnabled",
            EffectsType.Megaphone => "SetMegaphoneEnabled",
            EffectsType.Robot => "SetRobotEnabled",
            EffectsType.Hardtune => "SetHardTuneEnabled",
            _ => throw new ArgumentOutOfRangeException()
        };

    private bool GetNewEffectsState(EffectsType effectsType, ActionType actionType)
    {
        var currentState = _currentEffectState[effectsType];
        return actionType switch
        {
            ActionType.Toggle => currentState is not BooleanState.On,
            ActionType.On => true,
            ActionType.Off => false,
            _ => false
        };
    }
}