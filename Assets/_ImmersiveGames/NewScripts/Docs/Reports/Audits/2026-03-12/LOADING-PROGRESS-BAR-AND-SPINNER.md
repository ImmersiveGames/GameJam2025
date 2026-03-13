# Loading Progress Bar and Spinner

## Modelo de progresso adotado
- `LoadingProgressSnapshot` passou a ser o snapshot canonico da HUD.
- Campos:
  - `NormalizedProgress` (`0..1`)
  - `Percentage` (`0..100`)
  - `StepLabel`
  - `Reason`
- A HUD continua somente apresentando o snapshot recebido.

## Estrategia de calculo
- O progresso ficou hibrido:
  - real durante operacoes assincronas de load/unload de cena
  - por marcos ponderados para reset, prepare e finalizacao
- Etapas e pesos:
  - gameplay:
    - route scene ops: `0.55`
    - level prepare: `0.15`
    - world reset: `0.20`
    - finalizing: `0.05`
    - completed: fecha em `1.00`
  - frontend/startup:
    - route scene ops: `0.80`
    - finalizing: `0.15`
    - completed: fecha em `1.00`
- O `completed` sempre fecha a HUD em `100%`.

## Como a porcentagem e calculada
- Durante `SceneFlow`:
  - `SceneTransitionService` calcula progresso real por operacao de cena usando `AsyncOperation.progress`
  - esse progresso vira `SceneFlowRouteLoadingProgressEvent`
- Durante `WorldLifecycle` e `LevelFlow`:
  - `LoadingProgressOrchestrator` consome `LevelSelectedEvent`, `WorldLifecycleResetStartedEvent` e `WorldLifecycleResetCompletedEvent`
  - esses marcos completam os segmentos ponderados restantes

## Spinner criado
- Asset criado: `LoadingSpinner.png`
- Tipo: sprite UI simples em PNG
- Visual: spinner circular leve com 12 segmentos
- Local salvo:
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Assets/LoadingSpinner.png`

## Como o spinner foi ligado ao Loading HUD
- `LoadingHudScene` agora referencia o sprite diretamente no `Image` do `LoadingSpinner`.
- `LoadingHudController` exige:
  - `spinnerVisual`
  - `spinnerTransform`
- A rotacao acontece no proprio controller enquanto a HUD estiver visivel.

## Elementos visuais exigidos pela HUD
- `Canvas`
- `CanvasGroup`
- `TMP_Text` para etapa atual
- `TMP_Text` para porcentagem
- `Image` para fill da barra
- `GameObject` do spinner
- `RectTransform` do spinner

## Arquivos modificados
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Bindings/LoadingHudController.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/ILoadingPresentationService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/ISceneFlowLoaderAdapter.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Adapters/SceneManagerLoaderAdapter.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- `Assets/_ImmersiveGames/Scenes/LoadingHudScene.unity`

## Arquivos criados
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingProgressSnapshot.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/SceneFlowRouteLoadingProgressEvent.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingProgressOrchestrator.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Assets/LoadingSpinner.png`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Assets/LoadingSpinner.png.meta`
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/LOADING-PROGRESS-BAR-AND-SPINNER.md`

## Pendencias
- A validacao final de import do sprite e do layout da cena depende de abrir o projeto no Unity.
- Nao foi adicionado progresso para `swap local` nesta rodada.
