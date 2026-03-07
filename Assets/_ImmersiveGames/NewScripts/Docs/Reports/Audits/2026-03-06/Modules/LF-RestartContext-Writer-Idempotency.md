# LF RestartContext Writer Idempotency

Data: 2026-03-07

Nota: workspace local e a fonte da verdade.

## Writers encontrados (paths/linhas)

Comando (A1):
`rg -n "RegisterGameplayStart\(|UpdateGameplayStartSnapshot\(" . -g *.cs`

Resultado:
- `./Modules/LevelFlow/Runtime/RestartContextService.cs:24` - `RegisterGameplayStart(...)`
- `./Modules/LevelFlow/Runtime/RestartContextService.cs:26` - delegacao para `UpdateGameplayStartSnapshot(...)`
- `./Modules/LevelFlow/Runtime/RestartContextService.cs:29` - `UpdateGameplayStartSnapshot(...)`
- `./Modules/LevelFlow/Runtime/IRestartContextService.cs:6` - contrato de `UpdateGameplayStartSnapshot(...)`
- `./Modules/LevelFlow/Runtime/IRestartContextService.cs:8` - contrato de `RegisterGameplayStart(...)`
- `./Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:57` - callsite de escrita via `restartContext.UpdateGameplayStartSnapshot(snapshot)`

## Writer canônico identificado

- Origem canônica de entrada: `LevelSelectedRestartSnapshotBridge` (consumer de `LevelSelectedEvent`) em `./Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:57`.
- Writer canônico de estado: `RestartContextService.UpdateGameplayStartSnapshot(...)` em `./Modules/LevelFlow/Runtime/RestartContextService.cs:29`.

## Confirmacao da origem (LevelSelectedEvent)

Comando (B1):
`rg -n "EventBus<LevelSelectedEvent>\.Register|LevelSelectedEventConsumed" . -g *.cs`

Resultado:
- `./Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:18` - `EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding)`
- `./Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:41` - log `LevelSelectedEventConsumed ...`

Confirmacao:
- O consumer registrado chama `restartContext.UpdateGameplayStartSnapshot(snapshot)` em `./Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:57`.

## Regra antiga vs regra nova

- Regra antiga: calculava `persistedVersion = Math.Max(candidate, _selectionVersionCounter + 1)`, causando salto quando havia rewrite com `SelectionVersion == counter`.
- Regra nova: usa `incoming/next` com equality idempotente:
  - `incoming <= 0` => `next = counter + 1`
  - `incoming < counter` => `next = counter + 1`
  - `incoming >= counter` => `next = incoming` (inclui equality sem salto)
- Counter persistido: `_selectionVersionCounter = Math.Max(_selectionVersionCounter, next)`.
- Log adicional de dedupe (sem remover logs existentes):
  - `[OBS][LevelFlow] GameplayStartSnapshotWrite dedupe reason='same_selection_version' ...`

## Evidencia estatica pos-change

Comando (D1):
`rg -n "counter\s*\+\s*1\)|Math\.Max\(.*_selectionVersionCounter\s*\+\s*1" Modules/LevelFlow/Runtime/RestartContextService.cs`

Resultado:
- Sem matches (exit code 1), indicando ausencia do padrao antigo de `Math.Max(..., counter+1)` para o caminho `incoming > 0`.

Comando (D2):
`rg -n "RegisterGameplayStart\(|UpdateGameplayStartSnapshot\(" . -g *.cs`

Resultado:
- `./Modules/LevelFlow/Runtime/RestartContextService.cs:24`
- `./Modules/LevelFlow/Runtime/RestartContextService.cs:26`
- `./Modules/LevelFlow/Runtime/RestartContextService.cs:29`
- `./Modules/LevelFlow/Runtime/IRestartContextService.cs:6`
- `./Modules/LevelFlow/Runtime/IRestartContextService.cs:8`
- `./Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:57`

(sem novos writers criados)
