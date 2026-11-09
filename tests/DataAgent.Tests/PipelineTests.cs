using DataAgent.Buffering;
using DataAgent.Collectors;
using DataAgent.Senders;
using Microsoft.Extensions.Logging.Abstractions;

namespace DataAgent.Tests;

public class PipelineTests
{
    private sealed class RecordingSender(bool fail = false) : ISender
    {
        public List<Reading> Sent { get; } = [];

        public Task SendAsync(IReadOnlyList<Reading> readings, CancellationToken ct)
        {
            if (fail) throw new HttpRequestException("network down");
            Sent.AddRange(readings);
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }

    private static ICollector MakeSim() => CollectorFactory.Create(new CollectorOptions
    {
        Id = "sim-1",
        Type = "simulator",
        Settings = new() { ["temp"] = "10..20", ["count"] = "counter" },
    });

    [Fact]
    public async Task Simulator_reads_values_in_range()
    {
        var readings = await MakeSim().ReadAsync(CancellationToken.None);
        var temp = Convert.ToDouble(readings.Single(r => r.Tag == "temp").Value);
        Assert.InRange(temp, 10, 20);
        Assert.Equal(1L, readings.Single(r => r.Tag == "count").Value);
    }

    [Fact]
    public async Task Collect_then_flush_sends_and_empties_buffer()
    {
        using var buffer = new SqliteReadingBuffer(":memory:");
        await Pipeline.CollectOnceAsync(MakeSim(), buffer, NullLogger.Instance, CancellationToken.None);

        var sender = new RecordingSender();
        Assert.Equal(2, await Pipeline.FlushOnceAsync(sender, buffer, 100, CancellationToken.None));
        Assert.Equal(0, buffer.Count());
        Assert.Equal(new[] { "count", "temp" }, sender.Sent.Select(r => r.Tag).Order().ToArray());
    }

    [Fact]
    public async Task Failed_send_keeps_data_in_buffer()
    {
        using var buffer = new SqliteReadingBuffer(":memory:");
        await Pipeline.CollectOnceAsync(MakeSim(), buffer, NullLogger.Instance, CancellationToken.None);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => Pipeline.FlushOnceAsync(new RecordingSender(fail: true), buffer, 100, CancellationToken.None));
        Assert.Equal(2, buffer.Count()); // 유실 없음
    }

    [Fact]
    public void Unknown_collector_type_throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => CollectorFactory.Create(new CollectorOptions { Id = "x", Type = "nope" }));
    }
}
