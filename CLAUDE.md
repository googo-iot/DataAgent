# DataAgent — 생산장비 데이터 자동전송 (C# / .NET 10)

생산장비(PLC·설비 PC·계측기)에서 데이터를 주기적으로 수집해 상위 시스템(MES·DB·클라우드 API 등)으로
자동 전송하는 Windows 서비스. 네트워크가 끊겨도 데이터를 잃지 않는 **store-and-forward** 구조가 핵심.

## 먼저 읽을 문서
- [docs/requirements.md](docs/requirements.md) — 장비 목록, 프로토콜, 전송 대상, 주기 (사용자가 채우는 문서. `TBD` 항목은 추측하지 말고 사용자에게 질문)
- [docs/architecture.md](docs/architecture.md) — 구조와 설계 원칙

## 개발 환경
- 로컬 Windows PC에서 Claude Code(VS Code)로 개발. .NET 10 SDK 필요 (`winget install Microsoft.DotNet.SDK.10`).

## 구조
```
DataAgent.sln
src/DataAgent/                  # Worker Service (콘솔 실행 + Windows 서비스 겸용)
  Program.cs                    # 호스트 구성, DI 등록
  AgentOptions.cs               # appsettings.json "Agent" 섹션 바인딩
  Reading.cs                    # 측정값 모델
  Pipeline.cs                   # 수집 1회 / 전송 1배치 (테스트 가능한 핵심 로직)
  Buffering/SqliteReadingBuffer.cs  # SQLite 로컬 버퍼
  Collectors/                   # ICollector 구현 + CollectorFactory 등록
    SimulatorCollector.cs       # 가짜 장비 — 실장비 없이 개발/테스트용
  Senders/                      # ISender 구현 + SenderFactory 등록
    ConsoleSender.cs, HttpSender.cs
  Workers/                      # CollectorWorker(장비별 주기 수집), SenderWorker(백오프 재전송)
  appsettings.json
tests/DataAgent.Tests/          # xUnit
scripts/install-service.ps1     # 현장 PC 서비스 등록 (관리자 권한)
```

## 서브에이전트 (.claude/agents/)
| 이름 | 용도 | 코드 수정 |
|---|---|---|
| `collector-dev` | 장비 수집기(ICollector) 구현 + 모의 서버 테스트 | O |
| `sender-dev` | 전송기(ISender) 구현 + 실패 경로 테스트 | O |
| `build-tester` | dotnet build/test 실행, 오류만 요약 (haiku) | X |
| `code-reviewer` | 데이터 유실·장애 격리·재연결·보안 관점 리뷰 | X |

기본 흐름: `collector-dev`/`sender-dev` 구현 → `build-tester` 검증 → `code-reviewer` 리뷰 → 지적 사항 수정.

## 명령어
```bash
dotnet build DataAgent.sln
dotnet test DataAgent.sln
dotnet run --project src/DataAgent          # 시뮬레이터로 실행 (Ctrl+C 종료)
```

## 개발 규칙
- 새 수집기는 먼저 시뮬레이터/모의 서버(mock)로 테스트를 작성한다.
  **실제 장비에 접속하거나 값을 쓰는(write) 동작은 반드시 사용자 확인 후** 실행한다 (가동 중인 설비 보호).
- 새 장비 프로토콜 추가: `Collectors/`에 `ICollector` 구현 → `CollectorFactory.Registry`에 등록 →
  `appsettings.json` 예시와 테스트 추가. 프로토콜 라이브러리 후보:
  OPC UA `OPCFoundation.NetStandard.Opc.Ua.Client`, Modbus `NModbus`, MQTT `MQTTnet`, 시리얼 `System.IO.Ports`.
- 새 전송 대상 추가: `Senders/`에 `ISender` 구현 → `SenderFactory.Registry` 등록.
- 전송 실패 시 데이터를 버리지 않는다. 버퍼에서 전송 성공 후에만 `Ack`(삭제).
- 장비 1대의 예외가 다른 장비 수집을 멈추면 안 된다 (`Pipeline.CollectOnceAsync`에서 격리).
- 시간은 `DateTimeOffset` UTC로 저장/전송.
- 비밀값(API 키, DB 비밀번호)은 `appsettings.json`에 넣지 않는다. 현장 PC에서는
  `appsettings.Production.json`(git 제외) 또는 환경변수(`Agent__Sender__Settings__ApiKey`)로.
- 실행 대상은 현장 Windows PC.
- 문서와 주석은 한국어, 코드 식별자는 영어.
