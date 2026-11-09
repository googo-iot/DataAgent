using DataAgent.Buffering;
using DataAgent.Collectors;
using DataAgent.Senders;
using Microsoft.Extensions.Logging;

namespace DataAgent;

/// <summary>수집 1회 / 전송 1배치 단위 동작. Worker와 테스트에서 공용.</summary>
public static class Pipeline
{
    /// <summary>장비 1대 1회 수집 → 버퍼 저장. 실패해도 예외를 밖으로 던지지 않음 (다른 장비에 영향 X).</summary>
    public static async Task<int> CollectOnceAsync(
        ICollector collector, SqliteReadingBuffer buffer, ILogger logger, CancellationToken ct)
    {
        try
        {
            var readings = await collector.ReadAsync(ct);
            buffer.Put(readings.ToList());
            return readings.Count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[{Equipment}] 수집 실패", collector.Options.Id);
            return 0;
        }
    }

    /// <summary>버퍼에서 1배치 전송. 성공 시 Ack 후 건수 반환, 실패 시 예외 (데이터는 버퍼에 남음).</summary>
    public static async Task<int> FlushOnceAsync(
        ISender sender, SqliteReadingBuffer buffer, int batchSize, CancellationToken ct)
    {
        var batch = buffer.Peek(batchSize);
        if (batch.Count == 0) return 0;
        await sender.SendAsync(batch.Select(b => b.Reading).ToList(), ct);
        buffer.Ack(batch.Select(b => b.Id).ToList());
        return batch.Count;
    }
}
