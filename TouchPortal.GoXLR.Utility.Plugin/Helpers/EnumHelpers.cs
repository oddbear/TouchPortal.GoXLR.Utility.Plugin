namespace TouchPortal.GoXLR.Utility.Plugin.Helpers;

public static class EnumHelpers
{
    public static T Parse<T>(string value)
        => (T)Enum.Parse(typeof(T), value);

    public static T[] GetValues<T>()
        => Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToArray();

    public static Dictionary<TKey, TValue> CreateDictionaryWithDefaultKeys<TKey, TValue>()
        => GetValues<TKey>()
            .ToDictionary(key => key, _ => default(TValue));
}
