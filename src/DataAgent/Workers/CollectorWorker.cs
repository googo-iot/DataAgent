using DataAgent.Buffering;
using DataAgent.Collectors;
using Microsoft.Extensions.Options;

namespace DataAgent.Workers;

/// <summary>설정된 장비마다 독립 루프로 주기 수집.</summary>
public sealed class CollectorWorker(
    IOptions<AgentOptions> options,
    SqliteReadingBuffer buffer,
    ILogger<CollectorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var configs = options.Value.Collectors;
        var dup = configs.GroupBy(c => c.Id).FirstOrDefault(g => g.Count() > 1);
        if (dup is not null) throw new InvalidOperationException($"중복된 collector Id: {dup.Key}");

        logger.LogInformation("수집기 {Count}개 시작", configs.Count);
        await Task.WhenAll(configs.Select(c => RunCollectorAsync(c, ct)));
    }

    private async Task RunCollectorAsync(CollectorOptions config, CancellationToken ct)
    {
        await using var collector = CollectorFactory.Create(config);
        var retry = TimeSpan.FromSeconds(5);

        // 연결 실패 시 재시도 (장비 전원 off 등)
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await collector.ConnectAsync(ct);
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "[{Equipment}] 연결 실패, {Sec}초 후 재시도", config.Id, retry.TotalSeconds);
                await Task.Delay(retry, ct);
            }
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(config.IntervalSeconds));
        do
        {
            await Pipeline.CollectOnceAsync(collector, buffer, logger, ct);
        } while (await timer.WaitForNextTickAsync(ct));
    }
}
