# CS-1.2 - ContentSwap ownership/publish consolidation audit v2 (behavior-preserving)

Date: 2026-03-06
Source of truth: local workspace files.

## Scope
- `Modules/ContentSwap/Runtime/*`
- callsites in `Infrastructure/Composition/*`
- consumers in `Modules/LevelFlow/*`, `Modules/WorldLifecycle/*`, `Modules/SceneFlow/*`, `Modules/Navigation/*`

## PASSO 0 - Inventário obrigatório (evidência)

### A) Publishers de eventos ContentSwap
Command:
```text
rg -n "EventBus<.*ContentSwap.*>\.Raise|Raise\(new .*ContentSwap" Modules/ContentSwap Modules/LevelFlow Modules/WorldLifecycle Modules/SceneFlow Infrastructure -g "*.cs"
```
Relevant result:
- `Modules/ContentSwap/Runtime/ContentSwapContextService.cs:72` `ContentSwapPendingSetEvent`
- `Modules/ContentSwap/Runtime/ContentSwapContextService.cs:85` `ContentSwapCommittedEvent`
- `Modules/ContentSwap/Runtime/ContentSwapContextService.cs:98` `ContentSwapPendingClearedEvent`

### B) Consumers/registrations
Command:
```text
rg -n "EventBus<.*ContentSwap.*>\.Register|new EventBinding<.*ContentSwap" Modules Infrastructure -g "*.cs"
```
Relevant result:
- `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs:15-16` registers `ContentSwapCommittedEvent` (legacy bridge).

### C) Entry points canônicos
Command:
```text
rg -n "IContentSwapChangeService|RequestContentSwapInPlaceAsync|TryCommitPending|SetPending|Commit" Modules Infrastructure -g "*.cs"
```
Relevant result:
- DI/register: `Infrastructure/Composition/GlobalCompositionRoot.ContentLevels.cs`
- Executor canônico: `Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs`
- State/context: `Modules/ContentSwap/Runtime/ContentSwapContextService.cs`
- Canonical consumer: `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs` (`RequestContentSwapInPlaceAsync`)

### D) Asset scan (safety)
Command:
```text
rg -n "ContentSwap" -g "*.unity" -g "*.prefab" -g "*.asset" .
```
Result:
- no matches in scanned assets for this run.

## PASSO 1/2 - Ownership e centralização aplicada
- Ownership explicitado em código:
  - `ContentSwapContextService`: owner de estado (`Current/Pending`) + publish de `PendingSet/PendingCleared/Committed`.
  - `InPlaceContentSwapService`: executor canônico de request/validação/gate/commit; delega publish de estado ao context service.
- Centralização de publisher confirmada:
  - `ContentSwapPendingSetEvent`, `ContentSwapPendingClearedEvent`, `ContentSwapCommittedEvent` continuam sendo publicados em um único lugar (`ContentSwapContextService`).
- Nenhum contrato público/payload alterado.

## PASSO 3 - Dedupe/idempotência adicional
- Não aplicado.
- Motivo: inventário não mostrou evidência de múltiplos publishers/commits same-frame no trilho canônico de ContentSwap.

## PASSO 4 - Limpeza segura (Dev/Legacy)
- `ContentSwapDevBootstrapper` já estava desativado como caminho paralelo com log `[OBS][LEGACY][DevQA]`.
- Nenhum move/remove de código nesta etapa (sem evidência adicional de dead code no escopo pedido).

## PASSO 5 - Evidência pós-change (unicidade)

Command:
```text
rg -n "EventBus<ContentSwapPendingSetEvent>\.Raise|EventBus<ContentSwapCommittedEvent>\.Raise|EventBus<ContentSwapPendingClearedEvent>\.Raise" Modules Infrastructure -g "*.cs"
```
Result:
- `Modules/ContentSwap/Runtime/ContentSwapContextService.cs:72`
- `Modules/ContentSwap/Runtime/ContentSwapContextService.cs:85`
- `Modules/ContentSwap/Runtime/ContentSwapContextService.cs:98`

Command:
```text
rg -n "EventBus<.*ContentSwap.*>\.Raise|Raise\(new .*ContentSwap" Modules/ContentSwap Modules/LevelFlow Modules/WorldLifecycle Modules/SceneFlow Infrastructure -g "*.cs"
```
Result:
- only `ContentSwapContextService` raises ContentSwap state events.

## Publishers/Consumers/Owners (final)

| Event | Publisher | Consumers | Owner |
|---|---|---|---|
| `ContentSwapPendingSetEvent` | `ContentSwapContextService` | none in scanned scope | `ContentSwapContextService` |
| `ContentSwapPendingClearedEvent` | `ContentSwapContextService` | none in scanned scope | `ContentSwapContextService` |
| `ContentSwapCommittedEvent` | `ContentSwapContextService` | `Navigation/Legacy/RestartSnapshotContentSwapBridge` | `ContentSwapContextService` |

## Risks residuais
- Legacy consumer externo permanece ativo: `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs`.
- `rg` de assets não encontrou referências textuais de ContentSwap neste run; isso não substitui validação funcional em Editor.

## Manual checklist (Editor/Dev Build)
- Reexecutar smoke canônico + fluxo de reset de nível que usa `WorldResetCommands.ResetLevelAsync`.
- Confirmar timeline em log: `Request -> PendingSet -> Committed` (e `PendingCleared` em falha).
