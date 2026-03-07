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

## DQ-1.4.1 update — SceneRouteDefinitionAsset

Alvo: `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs`.

Before (evidência):
- `10:using UnityEditor;`
- `75:string assetPath = AssetDatabase.GetAssetPath(this);`
- `85:string assetPath = AssetDatabase.GetAssetPath(this);`

Ação aplicada (behavior-preserving):
- classe runtime convertida para `partial` (arquivo original preservado no mesmo caminho);
- bloco editor/validation extraído para `Modules/SceneFlow/Navigation/Dev/SceneRouteDefinitionAsset.DevQA.cs`;
- arquivo DevQA inteiro sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`;
- `using UnityEditor;` e chamadas `AssetDatabase` mantidas sob `#if UNITY_EDITOR` no arquivo DevQA.

After (pós-check runtime):
- `rg -n "UnityEditor|ContextMenu|MenuItem|AssetDatabase|FindAssets" Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs`
- resultado: sem matches (exit code 1).

## DQ-1.4.2 update — GameNavigationCatalogAsset
- O que vazava: referência runtime a `UnityEditor.EditorApplication.isPlaying` em `Modules/Navigation/GameNavigationCatalogAsset.cs:1024`.
- Para onde foi: hook editor-only extraído para `Modules/Navigation/Dev/GameNavigationCatalogAsset.DevQA.cs` (classe parcial, guard de arquivo `UNITY_EDITOR || DEVELOPMENT_BUILD`).
- Prova pós-check: `rg -n "UnityEditor|ContextMenu|MenuItem|AssetDatabase|FindAssets" Modules/Navigation/GameNavigationCatalogAsset.cs` => sem matches.

## DQ-1.4.3 update — SceneRouteResetPolicy
- O que vazava: `UnityEditor.EditorApplication` no runtime (`Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs:63`).
- Para onde foi: hook editor-only movido para `Modules/WorldLifecycle/Dev/SceneRouteResetPolicy.DevQA.cs` (classe partial + guard de arquivo `UNITY_EDITOR || DEVELOPMENT_BUILD`).
- Prova pós-check: `rg -n "UnityEditor|ContextMenu|MenuItem|AssetDatabase|FindAssets|EditorApplication" Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs` => sem matches.

## DQ-1.4.4+ update (batch)
- Behavior-preserving: apenas isolamento DevQA/Editor; sem mudanÃ§a de contratos pÃºblicos/pipeline/callsites.
- Runtime files limpos:
  - `Modules/Navigation/Bindings/MenuQuitButtonBinder.cs`
  - `Modules/Navigation/GameNavigationIntentCatalogAsset.cs`
  - `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`
  - `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - `Modules/LevelFlow/Config/SceneBuildIndexRef.cs`
  - `Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs`
- DevQA files criados:
  - `Modules/Navigation/Dev/MenuQuitButtonBinder.DevQA.cs`
  - `Modules/Navigation/Dev/GameNavigationIntentCatalogAsset.DevQA.cs`
  - `Modules/GameLoop/Dev/GameLoopSceneFlowCoordinator.DevQA.cs`
  - `Modules/SceneFlow/Transition/Dev/SceneTransitionService.DevQA.cs`
  - `Modules/LevelFlow/Config/Dev/SceneBuildIndexRef.DevQA.cs`
  - `Modules/SceneFlow/Navigation/Dev/SceneRouteCatalogAsset.DevQA.cs`

### EvidÃªncia (before)
Comando global (fora Dev/Editor/Legacy):
`rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|ContextMenu|MenuItem|InitializeOnLoad|RuntimeInitializeOnLoadMethod" C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**"`

Trechos relevantes:
- `Modules/Navigation/Bindings/MenuQuitButtonBinder.cs:23`
- `Modules/Navigation/GameNavigationIntentCatalogAsset.cs:207`
- `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs:166`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs:231`
- `Modules/LevelFlow/Config/SceneBuildIndexRef.cs:6,34`
- `Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs:7,107`

### EvidÃªncia (after)
- Runtime limpo por arquivo (6 comandos `rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|ContextMenu|MenuItem" <runtime_file>`): sem matches.
- Guards dos DevQA files presentes (`#if UNITY_EDITOR || DEVELOPMENT_BUILD` + `#if UNITY_EDITOR` para APIs editor): confirmado.
- Prova final: global sweep outside Dev/Editor/Legacy = 0 matches.
