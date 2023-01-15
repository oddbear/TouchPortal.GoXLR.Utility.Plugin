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

    public static Dictionary<(TKeyA, TKeyB), TValue> CreateDictionaryWithDefaultKeys<TKeyA, TKeyB, TValue>()
        => CompositeKeyGetValues<TKeyA, TKeyB>()
            .ToDictionary(key => key, _ => default(TValue));

    private static IEnumerable<(TKeyA, TKeyB)> CompositeKeyGetValues<TKeyA, TKeyB>()
        =>
            from keyA in GetValues<TKeyA>()
            from keyB in GetValues<TKeyB>()
            select (keyA, keyB);
}
