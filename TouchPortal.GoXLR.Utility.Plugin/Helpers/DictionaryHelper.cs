namespace TouchPortal.GoXLR.Utility.Plugin.Helpers;

internal static class DictionaryHelper
{
    internal static TKey GetKeyFromValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TValue value)
        => dictionary
            .SingleOrDefault(pair => pair.Value?.Equals(value) ?? false)
            .Key;
}
