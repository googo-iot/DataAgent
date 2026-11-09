# 아키텍처

```
 [장비 A] ─┐                                         ┌─> [MES / REST API]
 [장비 B] ─┼─> CollectorWorker ─> SQLite 버퍼 ─> SenderWorker ┼─> [MQTT 브로커]
 [장비 C] ─┘   (장비별 주기 수집)   (store-and-forward)          └─> [DB]
```

.NET Worker Service 하나에 BackgroundService 2개가 돈다. 콘솔로도, Windows 서비스로도 실행된다.

## 흐름
1. **CollectorWorker**: 장비마다 독립된 루프(`PeriodicTimer`)로 `ICollector.ReadAsync` 호출.
   연결 실패 시 5초 간격 재시도, 읽기 실패는 로그만 남기고 다음 주기로.
2. **버퍼 저장**: 모든 `Reading`은 먼저 로컬 SQLite(`data/buffer.db`)에 기록.
3. **SenderWorker**: 버퍼에서 오래된 순으로 배치를 꺼내 `ISender.SendAsync`, 성공 시에만 `Ack`(삭제).
4. 전송 실패 시 지수 백오프(최대 5분)로 재시도. 그동안 수집은 계속되고 버퍼에 쌓인다.

## Reading 모델
| 필드 | 설명 |
|---|---|
| `EquipmentId` | 장비 식별자 (설정의 `Id`) |
| `Tag` | 항목명 (예: `temperature`) |
| `Value` | 값 (숫자/문자열/불리언) |
| `Timestamp` | 수집 시각 (UTC) |
| `Quality` | `good` / `bad` / `uncertain` |

## 설정 (appsettings.json)
```json
"Agent": {
  "BufferPath": "data/buffer.db",
  "Collectors": [ { "Id": "...", "Type": "simulator", "IntervalSeconds": 2, "Settings": { ... } } ],
  "Sender": { "Type": "http", "IntervalSeconds": 5, "BatchSize": 500, "Settings": { "Url": "...", "ApiKey": "..." } }
}
```
`Settings`는 수집기/전송기별 자유 형식 key-value. 비밀값은 `appsettings.Production.json`(git 제외) 또는 환경변수로 덮어쓴다.

## 설계 원칙
- 수집과 전송 분리 → 전송 대상 장애가 수집을 막지 않는다.
- 장비 1대 실패가 다른 장비 수집을 막지 않는다.
- 프로토콜/전송 대상은 Factory Registry로 확장.

## 향후 과제 (요구사항 확정 후)
- 실제 프로토콜 수집기 (OPC UA / Modbus / 파일 감시 / 시리얼)
- 실제 전송기 (MES API / MQTT / DB)
- 파일 로그 (Serilog 등), Windows 이벤트 로그
- 상태 모니터링(heartbeat), 버퍼 용량 상한
