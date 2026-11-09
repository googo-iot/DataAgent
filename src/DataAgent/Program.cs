using DataAgent;
using DataAgent.Buffering;
using DataAgent.Workers;
using Microsoft.Extensions.Options;

// 한글 로그가 Windows 콘솔(CP949)에서 깨지지 않도록
Console.OutputEncoding = System.Text.Encoding.UTF8;

// 서비스로 실행되면 작업 디렉터리가 System32가 되므로 실행 파일 위치 기준으로 고정
// (appsettings.json, data/buffer.db 모두 실행 파일 옆에 위치)
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(args);

// Windows 서비스로 실행 시 서비스 수명주기 연동 (콘솔 실행 시에는 영향 없음)
builder.Services.AddWindowsService(o => o.ServiceName = "DataAgent");

builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));
builder.Services.AddSingleton(sp =>
    new SqliteReadingBuffer(sp.GetRequiredService<IOptions<AgentOptions>>().Value.BufferPath));
builder.Services.AddHostedService<CollectorWorker>();
builder.Services.AddHostedService<SenderWorker>();

builder.Build().Run();
