# 현장 PC에서 관리자 권한 PowerShell로 실행: Windows 서비스로 등록
#   .\scripts\install-service.ps1 -InstallDir C:\DataAgent
param(
    [string]$InstallDir = "C:\DataAgent",
    [string]$ServiceName = "DataAgent"
)
$ErrorActionPreference = "Stop"

dotnet publish src/DataAgent/DataAgent.csproj -c Release -r win-x64 --self-contained true -o $InstallDir

if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Stop-Service $ServiceName
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

New-Service -Name $ServiceName -BinaryPathName "`"$InstallDir\DataAgent.exe`"" `
    -DisplayName "DataAgent (생산장비 데이터 전송)" -StartupType Automatic
# 비정상 종료 시 자동 재시작 (1분 후)
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
Start-Service $ServiceName
Get-Service $ServiceName
