# DevQA Leak Sweep Audit v1 (DQ-1.4)

Data: 2026-03-07

Fonte da verdade: workspace local.

## Behavior-preserving statement
- Sem alteração de contratos públicos, assinaturas públicas, pipeline ou callsites canônicos.
- Sem move de arquivos runtime principais (GUID/meta preservados).
- Mudança aplicada foi estrutural (`partial + Dev file`) em caso classificado como B (DevQA embutido).

## Evidência estática (scan)

### A) Scan global (sem filtros)
Comando:
`rg -n "UnityEditor|MenuItem|ContextMenu|AssetDatabase|FindAssets|InitializeOnLoad|RuntimeInitializeOnLoadMethod|DidReloadScripts|ExecuteAlways" Modules -g "*.cs"`

Trechos relevantes (amostra curta):
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:231`
- `Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs:63`
- `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs:166`
- `Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs:149` (antes da extração B)
- `Modules/GameLoop/Pause/Dev/PauseOverlayController.DevQA.cs:5`

### B) Suspeitos fora de Dev/Editor/Legacy
Comando:
`rg -n "UnityEditor|MenuItem|ContextMenu|AssetDatabase|FindAssets|InitializeOnLoad|RuntimeInitializeOnLoadMethod|DidReloadScripts|ExecuteAlways" Modules -g "*.cs" --glob "!**/Dev/**" --glob "!**/Editor/**" --glob "!**/Legacy/**"`

Resultado consolidado:
- `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs:166`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:231`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:245`
- `Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs:63`
- `Modules/Navigation/Bindings/MenuQuitButtonBinder.cs:23`
- `Modules/Navigation/GameNavigationIntentCatalogAsset.cs:207`
- `Modules/Navigation/GameNavigationCatalogAsset.cs:1024`
- `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs:10,75,85`
- `Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs:7,107`
- `Modules/LevelFlow/Config/SceneBuildIndexRef.cs:6,34`
- `Modules/GameLoop/IntroStage/Runtime/IntroStageCoordinator.cs:83` (string de log, sem API editor)

## Classificação A/B/C

| File | Class | Justificativa | Ação |
|---|---|---|---|
| `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs` | A | fail-fast runtime em `#if UNITY_EDITOR` + `Application.Quit`; trilho crítico de start/sync | manter |
| `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | A | fail-fast canônico de configuração; não é tooling DevQA | manter |
| `Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs` | A | fail-fast de policy de rota | manter |
| `Modules/Navigation/Bindings/MenuQuitButtonBinder.cs` | A | comportamento esperado de botão Quit (Editor encerra play mode) | manter |
| `Modules/Navigation/GameNavigationIntentCatalogAsset.cs` | A | validação/config fail-fast canônica | manter |
| `Modules/Navigation/GameNavigationCatalogAsset.cs` | A | validação/config fail-fast canônica | manter |
| `Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs` | B | ContextMenu de validação embutido em arquivo runtime | extrair para parcial DevQA (aplicado) |
| `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs` | C | validações editor + AssetDatabase em ScriptableObject de config; alto risco sem revisão dedicada | manual confirmation |
| `Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs` | C | validação editor embutida em catálogo canônico; requer revisão de impacto | manual confirmation |
| `Modules/LevelFlow/Config/SceneBuildIndexRef.cs` | C | campo `SceneAsset`/sync editor no tipo de config serializada; extração exige validação de serialização | manual confirmation |
| `Modules/GameLoop/IntroStage/Runtime/IntroStageCoordinator.cs` | A | match é texto de log (“ContextMenu/MenuItem”), sem símbolo editor | manter |

## Correção aplicada (classe B)
- Runtime mantido no mesmo caminho: `Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs`.
- Classe convertida para `partial`.
- `ContextMenu("Validate Transition Profiles")` e implementação da validação movidos para:
  - `Modules/SceneFlow/Navigation/Dev/TransitionStyleCatalogAsset.DevQA.cs`
- Arquivo DevQA sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Arquivo runtime sem `UnityEditor`, `ContextMenu`, `MenuItem`, `AssetDatabase`.

## Hardening PA-1.1 obrigatório
Comando:
`rg -n "using UnityEditor|UnityEditor\." Modules/GameLoop/Pause -g "*.cs"`

Resultado:
- `Modules/GameLoop/Pause/Dev/PauseOverlayController.DevQA.cs:5:using UnityEditor;`
- Nenhuma ocorrência em `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`.

## Pós-check (runtime tocado)
Comando:
`rg -n "UnityEditor|AssetDatabase|FindAssets|MenuItem|ContextMenu" Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`

Resultado:
- sem matches (exit code 1).

## Before/After mínimo
- Antes (scan A): `Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs:149: [ContextMenu("Validate Transition Profiles")]`
- Depois (scan B + pós-check): ocorrência removida do runtime; agora em `Modules/SceneFlow/Navigation/Dev/TransitionStyleCatalogAsset.DevQA.cs:8`.
