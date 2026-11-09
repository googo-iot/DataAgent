using System.Text.Json;

namespace DataAgent.Senders;

/// <summary>표준출력으로 전송 (디버그용).</summary>
public sealed class ConsoleSender : ISender
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task SendAsync(IReadOnlyList<Reading> readings, CancellationToken ct)
    {
        foreach (var r in readings)
            Console.WriteLine(JsonSerializer.Serialize(r, Json));
        return Task.CompletedTask;
    }

    public void Dispose() { }
}
