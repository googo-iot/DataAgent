namespace DataAgent;

/// <summary>appsettings.json의 "Agent" 섹션.</summary>
public sealed class AgentOptions
{
    public string BufferPath { get; set; } = "data/buffer.db";
    public List<CollectorOptions> Collectors { get; set; } = [];
    public SenderOptions Sender { get; set; } = new();
}

public sealed class CollectorOptions
{
    /// <summary>장비 식별자 (중복 불가).</summary>
    public string Id { get; set; } = "";
    /// <summary>CollectorFactory 등록 키 (예: simulator, opcua, modbus).</summary>
    public string Type { get; set; } = "";
    public double IntervalSeconds { get; set; } = 1;
    /// <summary>수집기별 설정 (접속 주소, 태그 맵 등).</summary>
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SenderOptions
{
    /// <summary>SenderFactory 등록 키 (예: console, http).</summary>
    public string Type { get; set; } = "console";
    public double IntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 500;
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
