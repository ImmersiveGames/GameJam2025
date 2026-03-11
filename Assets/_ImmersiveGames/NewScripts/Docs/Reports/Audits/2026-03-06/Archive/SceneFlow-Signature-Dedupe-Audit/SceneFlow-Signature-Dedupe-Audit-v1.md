# SF-1.3b.2a - SceneFlow Signature/Dedupe/Cache Audit v1 (DOC-only)

Date: 2026-03-06
Source of truth: local workspace files.

## Mandatory evidence (rg)

### 1) Cache / signature store
```text
rg -n "ISceneFlowSignatureCache|SceneFlowSignatureCache|SignatureCache" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Relevant lines:
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:33` `RegisterSceneFlowSignatureCache()`
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:133` `new SceneFlowSignatureCache()`
- `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs:15` class definition
- `Modules/SceneFlow/Runtime/ISceneFlowSignatureCache.cs:6` interface definition
- `Modules/GameLoop/Runtime/Services/GameLoopService.cs:363` reads cache via `TryGetGlobal<ISceneFlowSignatureCache>`

### 2) Dedupe by signature/frame/in-flight
```text
rg -n "dedupe|Dedupe|same_frame|_inFlightSignature|IsInFlight|ShouldDedupe|frameCount|Time\.frameCount" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Relevant lines:
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:43-45` signature state fields
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:355` `ShouldDedupeSameFrame`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:372` `IsInFlightSameSignature`
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:45-46` ensure dedupe fields
- `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs:61-66` same-frame ensure dedupe
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:31-33` dedupe fields
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:62-66` started same-frame dedupe
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:121/178/225` completed dedupe checks

### 3) Signature guards in consumers
```text
rg -n "signature='r:|signature=.*to-|SceneTransitionStartedEvent|SceneTransitionCompletedEvent|ScenesReady" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Relevant lines:
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:157/171/185` publishes Started/ScenesReady/Completed
- `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs:54/103/138` consumes Started/ScenesReady/Completed
- `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs:42/47` consumes Started/Completed and caches
- `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs:103/114/123` consumes Started/ScenesReady/Completed
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs:57/80` consumes Started/Completed and applies local dedupe

### 4) Asset scan (risk only)
```text
rg -n "SceneFlowSignatureCache|ISceneFlowSignatureCache" -g "*.unity" -g "*.prefab" -g "*.asset" .
```
Result:
- no matches

---

## Inventory Table

| Component (Type/Service) | FilePath | Mechanism | Key | Writes? | Reads? | Consumers (quem lê) | Producers (quem escreve) | Ownership claim | Runtime critical? | AssetRef? | Recommendation | Risk notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `SceneTransitionService` | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | `DedupeSameFrame + DedupeInFlight` | `signature + frame + inFlight` | Y | Y | próprio serviço (controle interno), pipeline SceneFlow | próprio serviço (`_lastRequestedSignature/_inFlightSignature/_lastCompletedSignature`) | **Owner** do dedupe de request de transição | Y | N | A KEEP | Camada canônica; mexer aqui sem plano altera semântica de concorrência. |
| `SceneFlowSignatureCache` | `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs` | `Cache` | `last signature + profile + targetScene` | Y | Y | `GameLoopService`, `SceneFlowDevContextMenu` | eventos Started/Completed do SceneFlow | **Owner** de cache de última assinatura (read-model) | N | N | C CONSOLIDATE | Redundância potencial com dedupe interno do TransitionService; manter separado só se consumo externo justificar. |
| `ISceneFlowSignatureCache` | `Modules/SceneFlow/Runtime/ISceneFlowSignatureCache.cs` | `Other (contract)` | `TryGetLast(...)` | N | Y | consumidores DI | `SceneFlowSignatureCache` | contrato de leitura | Y | N | A KEEP | Interface pública em uso por GameLoop/Dev; não alterar nesta fase. |
| `LoadingHudService` | `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs` | `DedupeSameFrame` | `signature + Time.frameCount` | Y | Y | `LoadingHudOrchestrator` | próprio serviço (`_lastEnsureLogSignature/_lastEnsureLogFrame`) | owner técnico de HUD load/show/hide | Y | N | C CONSOLIDATE | Dedupe local protege log/ensure; pode ser simplificado com ownership explícito com orquestrador. |
| `LoadingHudOrchestrator` | `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs` | `Other (signature guard/state)` | `activeSignature + pendingSignature + visibleSignature` | Y | Y | próprio orquestrador | eventos Started/FadeInCompleted/ScenesReady/Completed | consumer auxiliar de transição | Y | N | C CONSOLIDATE | Há lógica de guarda de assinatura paralela ao dedupe do serviço/hub de transição. |
| `SceneFlowInputModeBridge` | `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` | `DedupeSameFrame + Other` | `started(signature+frame), completed(profile|signature)` | Y | Y | próprio bridge (InputMode/GameLoop sync) | eventos Started/Completed | consumer auxiliar (não-owner do fluxo) | Y | N | C CONSOLIDATE | Dedupe próprio pode divergir do owner central; precisa alinhamento de ownership. |
| `GameReadinessService` | `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs` | `Other (snapshot dedupe)` | `ReadinessSnapshot fields` | Y | Y | consumidores de `ReadinessChangedEvent` | eventos Started/ScenesReady/Completed + gate events | owner de readiness (não de signature dedupe) | Y | N | A KEEP | Dedupe é de snapshot/readiness, não de signature request. |
| `GameLoopService` (signature read) | `Modules/GameLoop/Runtime/Services/GameLoopService.cs` | `Other (cache read)` | `TryGetLast(signature/profile/scene)` | N | Y | n/a | `SceneFlowSignatureCache` | consumer de read-model | N | N | A KEEP | Leitura de assinatura para observabilidade/postgame; não dedupe request. |
| `GameNavigationService` (signature compute) | `Modules/Navigation/GameNavigationService.cs` | `Other (compute only)` | `SceneTransitionSignature.Compute(BuildContext(request))` | N | Y | logs/observabilidade de navegação | n/a | consumer de cálculo de assinatura | N | N | A KEEP | Não mantém cache/dedupe; apenas computa assinatura para request/log. |

---

## Proposed Plan: SF-1.3b.2 (CODE) [proposal only]

### Single source of truth
- Manter `SceneTransitionService` como **single source of truth** para dedupe de request de transição (`same-frame` + `in-flight`).
- Manter `SceneFlowSignatureCache` apenas como **read-model** da última assinatura observada (sem regras de aceitação/rejeição).
- Tratar `LoadingHudOrchestrator`, `LoadingHudService` e `SceneFlowInputModeBridge` como **consumers idempotentes**.

### Candidate consolidations (next phase, no changes now)
1. Clarificar fronteira entre `LoadingHudOrchestrator` e `LoadingHudService`: um decide fase/visibilidade, o outro executa técnico; remover dedupe duplicado quando seguro.
2. Harmonizar dedupe no `SceneFlowInputModeBridge` com owner central (SceneTransitionService) para evitar regras paralelas não documentadas.
3. Confirmar necessidade de `SceneFlowSignatureCache` separado vs leitura derivada; manter se consumidores externos reais dependem de `TryGetLast`.

### Invariants to preserve
- Anchor logs must remain: `TransitionStarted`, `ScenesReady`, `CompletionGateFallback`, `TransitionCompleted`.
- Policy must remain: rejeitar/coalescer concorrência em in-flight conforme contrato atual do owner canônico.
- No public interface/contract change in this phase (`ISceneFlowSignatureCache`, events, payloads).

## Note
- Esta etapa foi **DOC-only** (nenhuma alteração em `.cs`).
