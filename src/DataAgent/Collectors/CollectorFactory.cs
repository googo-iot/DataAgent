namespace DataAgent.Collectors;

public static class CollectorFactory
{
    /// <summary>새 수집기는 여기에 등록 (설정의 Type 값 -> 생성 함수).</summary>
    private static readonly Dictionary<string, Func<CollectorOptions, ICollector>> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["simulator"] = o => new SimulatorCollector(o),
        };

    public static ICollector Create(CollectorOptions options) =>
        Registry.TryGetValue(options.Type, out var create)
            ? create(options)
            : throw new InvalidOperationException(
                $"알 수 없는 collector type: '{options.Type}' (가능: {string.Join(", ", Registry.Keys)})");
}
