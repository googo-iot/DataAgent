using System.Globalization;

namespace DataAgent.Collectors;

/// <summary>
/// 가짜 장비. 실제 장비 없이 개발/테스트할 때 사용.
/// Settings 예: "temperature": "180..220" (범위 랜덤), "count": "counter" (호출마다 +1)
/// </summary>
public sealed class SimulatorCollector(CollectorOptions options) : ICollector
{
    private readonly Dictionary<string, long> _counters = new();

    public CollectorOptions Options { get; } = options;

    public Task ConnectAsync(CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<Reading>> ReadAsync(CancellationToken ct)
    {
        var list = new List<Reading>();
        foreach (var (tag, spec) in Options.Settings)
        {
            object value;
            if (spec.Equals("counter", StringComparison.OrdinalIgnoreCase))
            {
                value = _counters[tag] = _counters.GetValueOrDefault(tag) + 1;
            }
            else
            {
                var parts = spec.Split("..");
                var min = double.Parse(parts[0], CultureInfo.InvariantCulture);
                var max = double.Parse(parts[1], CultureInfo.InvariantCulture);
                value = Math.Round(min + Random.Shared.NextDouble() * (max - min), 2);
            }
            list.Add(Reading.Now(Options.Id, tag, value));
        }
        return Task.FromResult<IReadOnlyList<Reading>>(list);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
