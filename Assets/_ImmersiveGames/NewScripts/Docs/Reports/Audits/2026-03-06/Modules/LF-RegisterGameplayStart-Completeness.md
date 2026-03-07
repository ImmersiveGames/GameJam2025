# LF RegisterGameplayStart Completeness Audit

Data da auditoria: 2026-03-07

Nota: workspace local e a fonte da verdade para esta auditoria.

## Writers encontrados

Comando:
`rg -n "RegisterGameplayStart\(|UpdateGameplayStartSnapshot\(" Modules -g "*.cs"`

Resultados relevantes:
- `Modules/LevelFlow/Runtime/RestartContextService.cs:24` - `RegisterGameplayStart(...)` (wrapper que delega para update canonico).
- `Modules/LevelFlow/Runtime/RestartContextService.cs:29` - `UpdateGameplayStartSnapshot(...)` (writer canonico de estado em runtime).
- `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:57` - chamada de escrita para `restartContext.UpdateGameplayStartSnapshot(snapshot)`.
- `Modules/LevelFlow/Runtime/IRestartContextService.cs:6` e `:8` - declaracoes de contrato (nao sao writer de estado).

Conclusao de completude: o writer canonico de estado permanece `RestartContextService.UpdateGameplayStartSnapshot(...)`, com entrada principal via `LevelSelectedRestartSnapshotBridge`.

## Consumer do LevelSelectedEvent

Comando:
`rg -n "LevelSelectedEventConsumed|EventBus<LevelSelectedEvent>\.Register" Modules -g "*.cs"`

Resultados relevantes:
- `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:18` - `EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding)`.
- `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:41` - log `LevelSelectedEventConsumed ...`.

## Leitores usados por LevelFlow/orchestrators

Comando:
`rg -n "TryGetCurrent\(out GameplayStartSnapshot|TryGetLastGameplayStartSnapshot\(" Modules/LevelFlow -g "*.cs"`

Resultados relevantes:
- `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs:66`
- `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs:55`
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs:70`
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs:161`
- `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs:45`
- `Modules/LevelFlow/Runtime/PostLevelActionsService.cs:66`
- `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs:77`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:69` e `:78` (implementacao dos getters)
- `Modules/LevelFlow/Runtime/IRestartContextService.cs:7` e `:9` (contrato)

## Hardening aplicado

Arquivo alterado: `Modules/LevelFlow/Runtime/RestartContextService.cs`

Diff narrativo:
- Inserida validacao no inicio de `UpdateGameplayStartSnapshot(snapshot)`:
  - se `!snapshot.HasLevelRef` ou `!snapshot.RouteId.IsValid`, a atualizacao e ignorada.
- Em caso invalido:
  - NAO atualiza `_current`.
  - NAO atualiza `_lastGameplayStartSnapshot`.
  - NAO atualiza `_selectionVersionCounter`.
  - emite log de warning:
    - `[WARN][LevelFlow] Ignored invalid GameplayStartSnapshot. levelRef='<null|name>' routeId='...' reason='...'`
  - retorna `_current` (preserva estado valido anterior).
- Regra de monotonicidade foi mantida sem alteracao:
  - `persistedVersion = max(candidateVersion, _selectionVersionCounter + 1)`.

## Checagem final obrigatoria

Comando:
`rg -n "Ignored invalid GameplayStartSnapshot" Modules -g "*.cs"`

Resultado:
- `Modules/LevelFlow/Runtime/RestartContextService.cs:40`.

Comando:
`rg -n "UpdateGameplayStartSnapshot\(" Modules -g "*.cs"`

Resultado:
- `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:57` - call site.
- `Modules/LevelFlow/Runtime/RestartContextService.cs:26` - delegacao interna.
- `Modules/LevelFlow/Runtime/RestartContextService.cs:29` - implementacao canonica.
- `Modules/LevelFlow/Runtime/IRestartContextService.cs:6` - contrato.

## Comandos executados e resultados relevantes

1. `rg -n "RegisterGameplayStart\(|UpdateGameplayStartSnapshot\(" Modules -g "*.cs"`
- `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:57`
- `Modules/LevelFlow/Runtime/IRestartContextService.cs:6`
- `Modules/LevelFlow/Runtime/IRestartContextService.cs:8`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:24`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:26`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:29`

2. `rg -n "LevelSelectedEventConsumed|EventBus<LevelSelectedEvent>\.Register" Modules -g "*.cs"`
- `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:18`
- `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:41`

3. `rg -n "TryGetCurrent\(out GameplayStartSnapshot|TryGetLastGameplayStartSnapshot\(" Modules/LevelFlow -g "*.cs"`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:69`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:78`
- `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs:66`
- `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs:77`
- `Modules/LevelFlow/Runtime/IRestartContextService.cs:7`
- `Modules/LevelFlow/Runtime/IRestartContextService.cs:9`
- `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs:55`
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs:70`
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs:161`
- `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs:45`
- `Modules/LevelFlow/Runtime/PostLevelActionsService.cs:66`

4. `rg -n "Ignored invalid GameplayStartSnapshot" Modules -g "*.cs"`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:40`

5. `rg -n "UpdateGameplayStartSnapshot\(" Modules -g "*.cs"`
- `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs:57`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:26`
- `Modules/LevelFlow/Runtime/RestartContextService.cs:29`
- `Modules/LevelFlow/Runtime/IRestartContextService.cs:6`
