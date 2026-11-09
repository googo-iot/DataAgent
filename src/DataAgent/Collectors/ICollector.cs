namespace DataAgent.Collectors;

/// <summary>장비 수집기. 프로토콜별로 구현하고 CollectorFactory에 등록한다.</summary>
public interface ICollector : IAsyncDisposable
{
    CollectorOptions Options { get; }

    /// <summary>장비 연결. 필요 없으면 빈 구현.</summary>
    Task ConnectAsync(CancellationToken ct);

    /// <summary>현재 값을 읽어 반환. 실패 시 예외를 던진다 (호출 측에서 격리).</summary>
    Task<IReadOnlyList<Reading>> ReadAsync(CancellationToken ct);
}
