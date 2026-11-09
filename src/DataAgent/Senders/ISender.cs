namespace DataAgent.Senders;

/// <summary>전송기. 전송 대상별로 구현하고 SenderFactory에 등록한다.</summary>
public interface ISender : IDisposable
{
    /// <summary>배치 전송. 실패 시 예외를 던진다 (예외가 없으면 성공으로 보고 버퍼에서 삭제).</summary>
    Task SendAsync(IReadOnlyList<Reading> readings, CancellationToken ct);
}
