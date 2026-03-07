# LevelFlow Cleanup Audit v2 (LF-1.2 monotonic selectionVersion)

Date: 2026-03-07
Scope: `Modules/LevelFlow/**`, `Modules/Navigation/**` (read-only for callsites).
Source of truth: workspace local.

## Objetivo
- Tornar `selectionVersion` monotônico após `MacroRestart/RestartContext.Clear`.
- Preservar contratos públicos, payloads e trilho canônico.
- Mudança behavior-preserving, exceto remoção do rewind para `v=1` após clear.

## Arquivos alterados
- `Modules/LevelFlow/Runtime/RestartContextService.cs`
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs`
- `Docs/Modules/LevelFlow.md`
- `Docs/Reports/Audits/2026-03-06/Modules/LevelFlow-Cleanup-Audit-v2.md`

## O que mudou

### 1) RestartContextService.Clear(reason)
- Antes: `Clear` zerava o único snapshot (`_current`), perdendo referência para derivar versão.
- Agora: mantém `last snapshot` + contador monotônico; limpa apenas `_current`.
- Log adicionado:
  - `[OBS][Navigation] RestartContextCleared keepLast='true' lastSelectionV='{v}' reason='{reason}'.`

### 2) LevelMacroPrepareService.PrepareGameplayAsync(...)
- Antes: `selectionVersion` dependia de `TryGetCurrent(out snapshot)`; após clear, caía em `1`.
- Agora: versão deriva de `TryGetLastGameplayStartSnapshot(out lastSnapshot)`:
  - `next = last.SelectionVersion + 1` quando last existe;
  - `1` apenas no primeiro caso sem last válido.
- `useSnapshot` para escolha de `LevelRef` continua baseado em `current` (mesma semântica de seleção).
- Log adicionado quando `current` está vazio e `last` existe:
  - `[OBS][LevelFlow] SelectionVersionSource source='last_snapshot' prev='{prev}' next='{next}' reason='{reason}'.`

### 3) LevelSwapLocalService
- Mantido sem alteração funcional: já usa `TryGetLastGameplayStartSnapshot(...)` para versão monotônica.

### 4) LevelStageOrchestrator
- Fallback `selection_version_rewind` mantido como proteção, sem mudança de dedupe.

## Evidência estática obrigatória

### A) selectionVersion (+1 / =1)
```text
rg -n "SelectionVersion\s*\+\s*1|selectionVersion\s*=\s*1" Modules/LevelFlow Modules/Navigation -g "*.cs"
Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs:101:                ? Math.Max(lastSnapshot.SelectionVersion + 1, 1)
Modules/LevelFlow/Runtime/LevelSwapLocalService.cs:67:            int nextSelectionVersion = Math.Max(currentSnapshot.SelectionVersion + 1, 1);
```

### B) current vs last snapshot callsites
```text
rg -n "TryGetCurrent\(out GameplayStartSnapshot|TryGetLastGameplayStartSnapshot" Modules/LevelFlow/Runtime -g "*.cs"
Modules/LevelFlow/Runtime/RestartContextService.cs:56:        public bool TryGetCurrent(out GameplayStartSnapshot snapshot)
Modules/LevelFlow/Runtime/RestartContextService.cs:65:        public bool TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot)
Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs:70:            bool hasLastSnapshot = _restartContextService.TryGetLastGameplayStartSnapshot(out lastSnapshot) &&
Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs:161:            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
Modules/LevelFlow/Runtime/LevelSwapLocalService.cs:45:            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot currentSnapshot) ||
Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs:55:                || !restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot)
Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs:66:                _restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) &&
```

### C) rewind fallback ainda presente
```text
rg -n "selection_version_rewind|LevelStageDedupeReset" Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs
89:                        "[OBS][LevelFlow] LevelStageDedupeReset reason='selection_version_rewind' prev='{previousVersion}' next='{nextVersion}' routeId='{snapshot.RouteId}'.",
```

## Contratos e comportamento
- Nenhuma assinatura pública alterada (`IRestartContextService`, eventos/payloads).
- Nenhuma mudança no pipeline global/callsites.
- Logs âncora preservados.
