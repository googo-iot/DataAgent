using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DataAgent.Senders;

/// <summary>
/// JSON 배치를 HTTP POST로 전송.
/// Settings: Url (필수), ApiKey (Bearer 토큰, 선택), TimeoutSeconds
/// 요청 본문: { "readings": [ ... ] } — 실제 API 스키마 확정 후 수정.
/// </summary>
public sealed class HttpSender : ISender
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;
    private readonly Uri _url;

    public HttpSender(SenderOptions options, HttpMessageHandler? handler = null)
    {
        var url = options.Settings.GetValueOrDefault("Url");
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("http sender: Settings.Url이 필요합니다");
        _url = new Uri(url);

        _client = handler is null ? new HttpClient() : new HttpClient(handler);
        _client.Timeout = TimeSpan.FromSeconds(
            double.Parse(options.Settings.GetValueOrDefault("TimeoutSeconds", "10"), CultureInfo.InvariantCulture));

        var apiKey = options.Settings.GetValueOrDefault("ApiKey");
        if (!string.IsNullOrEmpty(apiKey))
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task SendAsync(IReadOnlyList<Reading> readings, CancellationToken ct)
    {
        using var resp = await _client.PostAsJsonAsync(_url, new { readings }, Json, ct);
        resp.EnsureSuccessStatusCode();
    }

    public void Dispose() => _client.Dispose();
}
