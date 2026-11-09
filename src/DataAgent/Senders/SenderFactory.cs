namespace DataAgent.Senders;

public static class SenderFactory
{
    /// <summary>새 전송기는 여기에 등록 (설정의 Type 값 -> 생성 함수).</summary>
    private static readonly Dictionary<string, Func<SenderOptions, ISender>> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["console"] = _ => new ConsoleSender(),
            ["http"] = o => new HttpSender(o),
        };

    public static ISender Create(SenderOptions options) =>
        Registry.TryGetValue(options.Type, out var create)
            ? create(options)
            : throw new InvalidOperationException(
                $"알 수 없는 sender type: '{options.Type}' (가능: {string.Join(", ", Registry.Keys)})");
}
