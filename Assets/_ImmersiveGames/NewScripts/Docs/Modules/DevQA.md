# DevQA (Baseline 3.1)

## O que existe
- Pipeline can�nico de instala��o DevQA:
  - `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs` -> `CompositionInstallStage.DevQA` -> `InstallDevQaServices()`.
  - `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs` registra:
    - `IntroStageDevInstaller`
    - `ContentSwapDevInstaller`
    - `SceneFlowDevInstaller`
    - `LevelFlowDevInstaller`
    - `IntroStageRuntimeDebugGui` (guardado por `UNITY_EDITOR || DEVELOPMENT_BUILD`).
- Features principais:
  - IntroStage QA (`Modules/GameLoop/IntroStage/Dev/**`)
  - ContentSwap QA (`Modules/ContentSwap/Dev/**`)
  - SceneFlow QA (`Modules/SceneFlow/Dev/**`)
  - LevelFlow QA (`Modules/LevelFlow/Dev/**`)
  - WorldLifecycle Dev hotkey/hook (`Modules/WorldLifecycle/Dev/**`)
  - Editor tooling em `Modules/**/Editor/**` (drawers/validators/menu items).

## Como usar
- ContextMenu:
  - selecione GOs `QA_IntroStage`, `QA_ContentSwap`, `QA_SceneFlow`, `QA_LevelFlow` no Hierarchy (DontDestroyOnLoad).
- MenuItem (Editor):
  - caminhos `Tools/NewScripts/QA/...` e `ImmersiveGames/NewScripts/...` para a��es dev/editor.
- RuntimeDebugGui:
  - `IntroStageRuntimeDebugGui` aparece em runtime de dev para concluir IntroStage.
- QA GOs criados em runtime:
  - instaladores criam/reaproveitam GOs `QA_*` e marcam `DontDestroyOnLoad`.

## Pol�tica can�nica
- N�o deve rodar em produ��o:
  - instala��o central via `InstallDevQaServices()` est� sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Ferramentas editor-only:
  - classes em `Editor/**` e blocos `#if UNITY_EDITOR`.
- Runtime debug controlado:
  - bootstrappers/hotkeys com guards de build (`UNITY_EDITOR`, `DEVELOPMENT_BUILD`, `NEWSCRIPTS_DEV`, `NEWSCRIPTS_QA`).

## Manual confirmation required
- `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`: poss�vel sobreposi��o com installer central DevQA.
- `Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyDevBootstrap.cs`: bootstrap runtime paralelo ao trilho de composi��o DevQA.
- `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs`: classe dev sem guard expl�cito de compila��o.
- ContextMenus QA em arquivos de runtime (`PauseOverlayController`, `PostGameOverlayController`): manter at� validar wiring por inspector/prefab.
## Status DQ-1.2 (2026-03-06)
- Trilho canônico reforçado: instalação DevQA centralizada em `GlobalCompositionRoot.Pipeline.cs` (`CompositionInstallStage.DevQA`) + `GlobalCompositionRoot.DevQA.cs`.
- `ContentSwapDevBootstrapper` foi desativado como caminho paralelo (mantido como LEGACY no-op com log `[OBS][LEGACY][DevQA]`).
- Hotkey DEV de WorldLifecycle foi centralizado no installer DevQA (`RegisterWorldLifecycleQaInstaller` -> `WorldResetRequestHotkeyDevBootstrap.EnsureInstalled()`).
- Guards consolidados nos arquivos alterados para `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.