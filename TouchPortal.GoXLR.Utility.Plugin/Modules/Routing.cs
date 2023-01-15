using System.Text.RegularExpressions;
using TouchPortal.GoXLR.Utility.Plugin.Client;
using TouchPortal.GoXLR.Utility.Plugin.Enums;
using TouchPortal.GoXLR.Utility.Plugin.Helpers;
using TouchPortalSDK.Messages.Events;

namespace TouchPortal.GoXLR.Utility.Plugin.Modules;

public class Routing
{
    private readonly GoXlrUtilityClient _client;

    private readonly Dictionary<(InputDevice, OutputDevice), BooleanState> _currentRouting;

    public event Action<InputDevice, OutputDevice, BooleanState>? RoutingUpdated;

    public Routing(GoXlrUtilityClient client)
    {
        _client = client;

        _client.PatchEvent += RoutingChangeEvent;

        _currentRouting = EnumHelpers.CreateDictionaryWithDefaultKeys<InputDevice, OutputDevice, BooleanState>();
    }

    private void RoutingChangeEvent(object sender, Patch patch)
    {
        var match = Regex.Match(patch.Path, @"/mixers/(?<serial>\w+)/router/(?<input>\w+)/(?<output>\w+)");
        if (!match.Success)
            return;
        
        var input = EnumHelpers.Parse<InputDevice>(match.Groups["input"].Value);
        var output = EnumHelpers.Parse<OutputDevice>(match.Groups["output"].Value);

        var booleanState = patch.Value.GetBoolean()
            ? BooleanState.On
            : BooleanState.Off;

        _currentRouting[(input, output)] = booleanState;
        RoutingUpdated?.Invoke(input, output, booleanState);
    }

    public void SetRouting(ActionEvent message)
    {
        var match = Regex.Match(message.ActionId, @"TouchPortal.GoXLR.Utility.Plugin.actions.routing.(?<input>\w+).(?<output>\w+)");
        if (!match.Success)
            return;

        var input = EnumHelpers.Parse<InputDevice>(match.Groups["input"].Value);
        var output = EnumHelpers.Parse<OutputDevice>(match.Groups["output"].Value);

        var actionType = EnumHelpers.Parse<ActionType>(message.GetValue("actionType"));
        
        var state = GetNewRoutingState((input, output), actionType);

        _client.SendCommand("SetRouter", input, output, state);
    }

    private bool GetNewRoutingState((InputDevice, OutputDevice) key, ActionType actionType)
    {
        var currentState = _currentRouting[key];
        return actionType switch
        {
            ActionType.Toggle => currentState is not BooleanState.On,
            ActionType.On => true,
            ActionType.Off => false,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
