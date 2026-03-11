# SF-1.3a - SceneFlow redundancy inventory v4 (DOC-ONLY, behavior-preserving)

Date: 2026-03-06
Source of truth: local workspace files.

## Scope
- `Modules/SceneFlow/**`
- `Infrastructure/Composition/**` (callsites)
- cross-scan only: `Modules/*` for bridge/readiness references.

## Required callsite evidence (raw commands)
```text
rg -n "new NoOpTransitionCompletionGate\(|NoOpTransitionCompletionGate" Modules/SceneFlow Infrastructure -g "*.cs"
rg -n "new NoFadeAdapter\(|NoFadeAdapter" Modules/SceneFlow Infrastructure -g "*.cs"
rg -n "CompletionGateFallback|AwaitBeforeFadeOutAsync|AwaitCompletionGateAsync" Modules/SceneFlow -g "*.cs"
rg -n "LoadingHudOrchestrator|LoadingHudService|EnsureLoadedAsync|LoadingHudEnsure" Modules/SceneFlow -g "*.cs"
rg -n "SceneFlowSignatureCache|RegisterSceneFlowSignatureCache" Modules/SceneFlow Infrastructure -g "*.cs"
```

## A/B/C inventory table

| FilePath | Type(s) definidos | HasRuntimeCallsite (prova rg) | HasAssetRef (GUID scan) | DevOnly | Recommendation | Rationale |
|---|---|---|---|---|---|---|
| `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | `SceneTransitionService` | `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:114` (`new SceneTransitionService(...)`) | N/A (serviço DI) | parcial editor-only interno | A KEEP | Owner canônico da pipeline de transição e eventos Started/Completed. |
| `Modules/SceneFlow/Transition/Adapters/NoOpTransitionCompletionGate.cs` | `NoOpTransitionCompletionGate` | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:58` (`new NoOpTransitionCompletionGate()`) | guid=`33579b530a9e43b0b207ff226d25bc01` -> no match em `.unity/.prefab/.asset` | não | C RISK | É fallback canônico; simplificação possível só com prova de política/fail-fast em SF-1.3b. |
| `Modules/SceneFlow/Transition/Adapters/NoFadeAdapter.cs` | `NoFadeAdapter` | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:57` (`new NoFadeAdapter()`) | guid=`842e849dea7d4e5c96ed95b5dd206cc9` -> no match | não | C RISK | Fallback visual canônico; remover/mover sem estratégia de degraded mode pode alterar comportamento. |
| `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs` | `LoadingHudOrchestrator` | `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:376` (`new LoadingHudOrchestrator()`) | guid=`1f399630ae59f5a45b23e2a9c07671bc` -> no match | não | C RISK | Há sobreposição de ensure/dedupe com `LoadingHudService`; redução exige consolidação controlada. |
| `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs` | `LoadingHudService` | `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:362` (`new LoadingHudService(...)`) | guid=`85fddce2f6bc59e4b825d1e5aa188955` -> no match | não | A KEEP | Serviço técnico de HUD com degraded handling; canônico no DI global. |
| `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs` | `SceneFlowSignatureCache` | `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:133` (`new SceneFlowSignatureCache()`) | guid=`05dc7a12c6201d148961b266a3276a1c` -> no match | não | C RISK | Dedupe/cache cruza responsabilidades com TransitionService e bridges; candidato a racionalização posterior. |
| `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs` | `GameReadinessService` | `Infrastructure/Composition/GlobalCompositionRoot.PauseReadiness.cs:64` (`new GameReadinessService(gateService)`) | guid=`fd0c4bc3cbe29104e91c367cbb62ac1e` -> no match | não | C RISK | Consome mesmos eventos de transição que outros consumidores; possível redundância de ownership de ready/not-ready. |
| `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` (cross-scan) | `SceneFlowInputModeBridge` | `Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs:109` (`new SceneFlowInputModeBridge()`) | guid=`92e8c38ae65e56d46b8298face242b95` -> no match | não | C RISK | Consumidor cross-module de Started/Completed com dedupe próprio; inventário-only nesta etapa. |

## GUID evidence (B/C)

### NoOpTransitionCompletionGate
- meta guid: `33579b530a9e43b0b207ff226d25bc01`
- command: `rg -n "33579b530a9e43b0b207ff226d25bc01" -g "*.unity" -g "*.prefab" -g "*.asset" .`
- result: no match
- callsite evidence: `SceneTransitionService.cs:58`

### NoFadeAdapter
- meta guid: `842e849dea7d4e5c96ed95b5dd206cc9`
- command: `rg -n "842e849dea7d4e5c96ed95b5dd206cc9" -g "*.unity" -g "*.prefab" -g "*.asset" .`
- result: no match
- callsite evidence: `SceneTransitionService.cs:57`

### LoadingHudOrchestrator
- meta guid: `1f399630ae59f5a45b23e2a9c07671bc`
- command: `rg -n "1f399630ae59f5a45b23e2a9c07671bc" -g "*.unity" -g "*.prefab" -g "*.asset" .`
- result: no match
- callsite evidence: `GlobalCompositionRoot.SceneFlow.cs:376`

### SceneFlowSignatureCache
- meta guid: `05dc7a12c6201d148961b266a3276a1c`
- command: `rg -n "05dc7a12c6201d148961b266a3276a1c" -g "*.unity" -g "*.prefab" -g "*.asset" .`
- result: no match
- callsite evidence: `GlobalCompositionRoot.SceneFlow.cs:133`

### GameReadinessService
- meta guid: `fd0c4bc3cbe29104e91c367cbb62ac1e`
- command: `rg -n "fd0c4bc3cbe29104e91c367cbb62ac1e" -g "*.unity" -g "*.prefab" -g "*.asset" .`
- result: no match
- callsite evidence: `GlobalCompositionRoot.PauseReadiness.cs:64`

### SceneFlowInputModeBridge (cross-scan)
- meta guid: `92e8c38ae65e56d46b8298face242b95`
- command: `rg -n "92e8c38ae65e56d46b8298face242b95" -g "*.unity" -g "*.prefab" -g "*.asset" .`
- result: no match
- callsite evidence: `GlobalCompositionRoot.InputModes.cs:109`

## Top 5 reductions (proposta para SF-1.3b)
1. Consolidar dedupe de assinatura entre `SceneTransitionService` e `SceneFlowSignatureCache` com owner único explícito.
2. Consolidar `EnsureLoadedAsync` (orchestrator vs service) para remover redundância de guard/log e manter observabilidade central.
3. Revisar fallback adapters (`NoFadeAdapter` / `NoOpTransitionCompletionGate`) para contrato explícito de degraded mode sem duplicar lógica de fallback.
4. Delimitar ownership de eventos `SceneTransitionStarted/Completed` entre `GameReadinessService` e `SceneFlowInputModeBridge` (sem mudança de comportamento nesta etapa).
5. Isolar itens de compat/dev em trilho dedicado, mantendo runtime canônico mínimo no `GlobalCompositionRoot.SceneFlow.cs`.

## Summary
- Nenhum candidato B (LEGACY/DEAD) confirmado com segurança nesta etapa.
- Itens redundantes identificados ficaram como C (RISK) por possuírem callsites runtime ativos.
- Etapa concluída como DOC-ONLY (sem alteração de `.cs`).
