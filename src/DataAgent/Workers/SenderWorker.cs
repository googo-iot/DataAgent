using DataAgent.Buffering;
using DataAgent.Senders;
using Microsoft.Extensions.Options;

namespace DataAgent.Workers;

/// <summary>버퍼 데이터를 주기적으로 전송. 실패 시 지수 백오프로 재시도.</summary>
public sealed class SenderWorker(
    IOptions<AgentOptions> options,
    SqliteReadingBuffer buffer,
    ILogger<SenderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var cfg = options.Value.Sender;
        using var sender = SenderFactory.Create(cfg);
        var interval = TimeSpan.FromSeconds(cfg.IntervalSeconds);
        var backoff = interval;

        logger.LogInformation("전송기 시작: {Type}", cfg.Type);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 밀린 데이터가 있으면 배치를 연속으로 비움
                while (await Pipeline.FlushOnceAsync(sender, buffer, cfg.BatchSize, ct) == cfg.BatchSize) { }
                backoff = interval;
                await Task.Delay(interval, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "전송 실패 — {Sec}초 후 재시도 (버퍼 {Count}건)", backoff.TotalSeconds, buffer.Count());
                await Task.Delay(backoff, ct);
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
            }
        }
    }
}
