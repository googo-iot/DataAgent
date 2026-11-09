namespace DataAgent;

public static class Quality
{
    public const string Good = "good";
    public const string Bad = "bad";
    public const string Uncertain = "uncertain";
}

/// <summary>장비에서 수집한 측정값 1건.</summary>
/// <param name="Value">숫자/문자열/불리언. 버퍼에서 다시 읽으면 JsonElement로 복원된다.</param>
/// <param name="Timestamp">수집 시각 (UTC).</param>
public sealed record Reading(
    string EquipmentId,
    string Tag,
    object? Value,
    DateTimeOffset Timestamp,
    string Quality = DataAgent.Quality.Good)
{
    public static Reading Now(string equipmentId, string tag, object? value, string quality = DataAgent.Quality.Good)
        => new(equipmentId, tag, value, DateTimeOffset.UtcNow, quality);
}
