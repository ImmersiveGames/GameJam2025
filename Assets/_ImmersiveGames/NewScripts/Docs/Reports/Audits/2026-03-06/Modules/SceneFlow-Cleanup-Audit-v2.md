# SceneFlow Cleanup Audit v2 (SF-1.2a inventory only)

Date: 2026-03-06  
Scope: `Modules/SceneFlow/**` + `Infrastructure/Composition/**` (callsites only)  
Rule: inventory-only, no code/runtime changes.

## Candidate Inventory
| Name | FilePath | HasRuntimeCallsite | HasAssetOrSerializeRef | DevOnly | Recommendation |
|---|---|---|---|---|---|
| NoOpTransitionCompletionGate | `Modules/SceneFlow/Transition/Adapters/NoOpTransitionCompletionGate.cs` | Yes (`SceneTransitionService` ctor fallback) | No (no matches in `.asset/.prefab/.unity`; no `SerializeField` refs found) | No | A) KEEP (required) |
| NoFadeAdapter | `Modules/SceneFlow/Transition/Adapters/NoFadeAdapter.cs` | Yes (`SceneTransitionService` ctor fallback) | No (no matches in `.asset/.prefab/.unity`; no `SerializeField` refs found) | No | A) KEEP (required) |
| Completion gate "prossegue com falha" fallback | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` (`AwaitCompletionGateAsync`) | Yes (called inside canonical transition pipeline) | No (code path only; no asset/serialize hook) | No | C) NEED MANUAL CONFIRMATION |
| SceneFlowSignatureCache | `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs` | Yes (registered in composition + consumed by runtime/dev callers) | No (no matches in `.asset/.prefab/.unity`; no `SerializeField` refs found) | No | A) KEEP (required) |
| Signature dedupe overlap (`DuplicateSignatureWindowMs`) | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | Yes (core transition dedupe) | No (code path only; no asset/serialize hook) | No | C) NEED MANUAL CONFIRMATION |
| Signature guard overlap (`_activeSignature/_pendingSignature`) | `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs` | Yes (registered by `RegisterSceneFlowLoadingIfAvailable`) | No (code path only; no asset/serialize hook) | No | C) NEED MANUAL CONFIRMATION |

## Required Static Evidence

### 1) Candidate scan
Command:
```powershell
rg -n "class\s+NoOpTransitionCompletionGate|NoFadeAdapter|SignatureCache|CompletionGate|AwaitBeforeFadeOutAsync" Modules/SceneFlow Infrastructure/Composition
```
Relevant results:
- `Modules/SceneFlow/Transition/Adapters/NoOpTransitionCompletionGate.cs:12`
- `Modules/SceneFlow/Transition/Adapters/NoFadeAdapter.cs:9`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:62-63` (fallback ctor)
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:389` (`AwaitCompletionGateAsync`)
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:412` ("Completion gate falhou/abortou. Prosseguindo...")
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:122-136` (`RegisterSceneFlowSignatureCache`)

### 2) Composition/callsites scan
Command:
```powershell
rg -n "RegisterIfMissing|InstallSceneFlowServices|RegisterSceneFlowNative|RegisterSceneFlowFadeModule|RegisterSceneFlowLoadingIfAvailable" Infrastructure/Composition
```
Relevant results:
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs:92` (`installSceneFlow: InstallSceneFlowServices`)
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:25` (`InstallSceneFlowServices`)
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:28` (`RegisterSceneFlowFadeModule`)
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:32` (`RegisterSceneFlowNative`)
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs:38` (`RegisterSceneFlowLoadingIfAvailable`)

### 3) Serialize/asset/editor search
Command:
```powershell
rg -n "SerializeField.*NoOp|SerializeField.*Fade|Resources\.Load|AssetDatabase|FindAssets" Modules/SceneFlow
```
Relevant results:
- `AssetDatabase/FindAssets` aparecem apenas em tooling/editor e validação:
  - `Modules/SceneFlow/Editor/Validation/*`
  - `Modules/SceneFlow/Editor/IdSources/*`
- Sem `SerializeField.*NoOp` e sem `SerializeField.*Fade` para os candidatos do inventário.

### 4) Asset/prefab/scene reference check by class name
Command:
```powershell
rg -n "NoOpTransitionCompletionGate|NoFadeAdapter|SceneFlowSignatureCache" -g "*.asset" -g "*.prefab" -g "*.unity" .
```
Result:
- `(no matches in .asset/.prefab/.unity)`

## Notes
- `NoOpTransitionCompletionGate` e `NoFadeAdapter` não têm callsite canônico de composição, mas são usados no fallback construtivo de `SceneTransitionService`; por isso não são candidatos a `Legacy/` nesta etapa.
- Existe sobreposição de semântica de assinatura entre `SceneTransitionService`, `SceneFlowSignatureCache` e `LoadingHudOrchestrator`; recomenda-se confirmação manual antes de qualquer redução.
- This report is inventory-only (SF-1.2a). No runtime code moved/removed in this step.
