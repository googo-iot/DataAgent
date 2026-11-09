# DataAgent

생산장비 데이터 자동수집·전송 Windows 서비스 (C# / .NET 10, store-and-forward).

```bash
dotnet test DataAgent.sln
dotnet run --project src/DataAgent      # 시뮬레이터로 실행
```

현장 PC 서비스 등록 (관리자 PowerShell):
```powershell
.\scripts\install-service.ps1 -InstallDir C:\DataAgent
```

- 요구사항: [docs/requirements.md](docs/requirements.md)
- 구조: [docs/architecture.md](docs/architecture.md)

개발 환경: Windows + .NET 10 SDK (`winget install Microsoft.DotNet.SDK.10`)
