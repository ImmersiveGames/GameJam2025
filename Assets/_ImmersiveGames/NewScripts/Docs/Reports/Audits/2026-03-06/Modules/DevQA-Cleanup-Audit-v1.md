# DQ-1.1 - DevQA Cleanup Audit v1 (behavior-preserving)

Date: 2026-03-06
Scope:
- `Modules/**/Dev/**`
- `Modules/**/QA/**`
- `Modules/**/Bindings/**` (somente itens DEV/QA)
- `Infrastructure/Composition/**` (pipeline + installers)

## A) Owners (registro e política)
- Owner canônico de instalação DevQA no pipeline:
  - `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - stage `CompositionInstallStage.DevQA` -> `InstallDevQaServices()`.
- Gate de instalação canônico:
  - `InstallDevQaServices()` executa sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Módulo de composição DevQA:
  - `Infrastructure/Composition/Modules/DevQaCompositionModule.cs` (`_installed` + stage check).
- Registradores centrais de DevQA:
  - `GlobalCompositionRoot.DevQA.cs`:
    - `RegisterIntroStageQaInstaller()` -> `IntroStageDevInstaller.EnsureInstalled()`
    - `RegisterContentSwapQaInstaller()` -> `ContentSwapDevInstaller.EnsureInstalled()`
    - `RegisterSceneFlowQaInstaller()` -> `SceneFlowDevInstaller.EnsureInstalled()` + `LevelFlowDevInstaller.EnsureInstalled()`
    - `RegisterIntroStageRuntimeDebugGui()` (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`).

## B) Inventário por feature

### IntroStage QA
- `Modules/GameLoop/IntroStage/Dev/IntroStageDevInstaller.cs`
- `Modules/GameLoop/IntroStage/Dev/IntroStageDevContextMenu.cs`
- `Modules/GameLoop/IntroStage/Dev/IntroStageDevTester.cs`
- `Modules/GameLoop/IntroStage/Dev/IntroStageRuntimeDebugGui.cs`
- `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevMenuItems.cs`
- `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs`

### ContentSwap QA
- `Modules/ContentSwap/Dev/Runtime/ContentSwapDevInstaller.cs`
- `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`
- `Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs`

### SceneFlow QA
- `Modules/SceneFlow/Dev/SceneFlowDevInstaller.cs`
- `Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs`

### LevelFlow QA
- `Modules/LevelFlow/Dev/LevelFlowDevInstaller.cs`
- `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs`

### WorldLifecycle QA/Dev
- `Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyDevBootstrap.cs`
- `Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyBridge.cs`
- `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs`

### Outros Editor/QA tools relevantes (fora do installer central)
- `Modules/Gameplay/Editor/RunRearm/RunRearmRequestDevDriver.cs`
- `Modules/Gameplay/Editor/RunRearm/RunRearmKindDevEaterActor.cs`
- `Modules/Gameplay/Editor/RunRearm/RunRearmDevStepLogger.cs`
- `Modules/Navigation/Dev/Editor/GameNavigationCatalogNormalizer.cs`
- `Modules/SceneFlow/Editor/Validation/*.cs` e `Modules/SceneFlow/Editor/Drawers/*.cs`

## C) Callsites no pipeline
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - `CompositionInstallStage.DevQA`
  - `InstallDevQaServices()`
- `Infrastructure/Composition/Modules/DevQaCompositionModule.cs`
  - `context.InstallDevQa?.Invoke()`
- `Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs`
  - centraliza os installers principais de IntroStage/ContentSwap/SceneFlow/LevelFlow e RuntimeDebugGui.

## Tabela (installers / bootstrappers / context menus / runtime debug gui)
| FilePath | Kind | Compile guard | Runtime class | Classification | Notes |
|---|---|---|---|---|---|
| `Modules/GameLoop/IntroStage/Dev/IntroStageDevInstaller.cs` | Installer | none (called only by guarded pipeline) | PlayMode (DontDestroyOnLoad) | CANON_DEV_BUILD_ONLY | Installer principal de IntroStage QA. |
| `Modules/GameLoop/IntroStage/Dev/IntroStageRuntimeDebugGui.cs` | RuntimeDebugGui | `#if UNITY_EDITOR || DEVELOPMENT_BUILD` | Dev runtime GUI | RUNTIME_DEBUG | Instalado por `RegisterIntroStageRuntimeDebugGui`. |
| `Modules/GameLoop/IntroStage/Dev/IntroStageDevContextMenu.cs` | ContextMenu | none | PlayMode helper | CANON_DEV_BUILD_ONLY | Acoplado ao GO `QA_IntroStage`. |
| `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevMenuItems.cs` | MenuItem | Editor file | Editor tool | EDITOR_ONLY | MenuItem explícito. |
| `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs` | MenuItem | Editor file | Editor tool | EDITOR_ONLY | Seleção do `QA_IntroStage`. |
| `Modules/ContentSwap/Dev/Runtime/ContentSwapDevInstaller.cs` | Installer | none (called only by guarded pipeline) | PlayMode (DontDestroyOnLoad) | CANON_DEV_BUILD_ONLY | Installer principal de ContentSwap QA. |
| `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs` | Bootstrapper | `#if UNITY_EDITOR || DEVELOPMENT_BUILD` | RuntimeInitializeOnLoad | RUNTIME_DEBUG | Sobreposição potencial com installer via composition. |
| `Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs` | ContextMenu + MenuItem | `#if UNITY_EDITOR` | Editor/PlayMode helper | EDITOR_ONLY | Inclui `MenuItem` e ações QA. |
| `Modules/SceneFlow/Dev/SceneFlowDevInstaller.cs` | Installer | none (called only by guarded pipeline) | PlayMode (DontDestroyOnLoad) | CANON_DEV_BUILD_ONLY | Installer principal de SceneFlow QA. |
| `Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs` | ContextMenu | partial editor blocks | PlayMode helper | CANON_DEV_BUILD_ONLY | Ações QA de SceneFlow/WorldLifecycle/LevelFlow. |
| `Modules/LevelFlow/Dev/LevelFlowDevInstaller.cs` | Installer | none (called only by guarded pipeline) | PlayMode (DontDestroyOnLoad) | CANON_DEV_BUILD_ONLY | Installer principal de LevelFlow QA. |
| `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs` | ContextMenu | none | PlayMode helper | CANON_DEV_BUILD_ONLY | NextLevel/RestartLocal QA. |
| `Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyDevBootstrap.cs` | Bootstrapper | `#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_DEV` | RuntimeInitializeOnLoad | RUNTIME_DEBUG | Fora do installer central DevQA. |
| `Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyBridge.cs` | Hotkey bridge | `#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_DEV` | Dev runtime input bridge | RUNTIME_DEBUG | Shift+R reset request. |
| `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs` | Dev hook | none | Runtime hook | MANUAL_CONFIRMATION_REQUIRED | Classe Dev sem guard de compilação explícito. |
| `Modules/Gameplay/Editor/RunRearm/RunRearmRequestDevDriver.cs` | ContextMenu QA | `#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA` | Editor/Dev tool | EDITOR_ONLY | Fora do pipeline central DevQA. |

## Classificação consolidada
- `CANON_DEV_BUILD_ONLY`: installers/context menus instalados pelo trilho DevQA de composition (IntroStage, ContentSwap, SceneFlow, LevelFlow).
- `EDITOR_ONLY`: menu items/drawers/validators sob `Editor/**` ou `#if UNITY_EDITOR`.
- `RUNTIME_DEBUG`: bootstrappers runtime sob guards de dev/build (`RuntimeInitializeOnLoadMethod`, hotkeys, debug GUI).
- `LEGACY_COMPAT`: nenhum item conclusivo dentro de `Modules/**/Dev` nesta etapa.
- `ORPHAN_CANDIDATE`: nenhum move seguro nesta etapa; itens fora do installer central foram marcados para confirmação manual.

## Manual confirmation required
- `Modules/ContentSwap/Dev/Runtime/ContentSwapDevBootstrapper.cs`: coexistência com `GlobalCompositionRoot.DevQA` pode gerar instalação redundante.
- `Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyDevBootstrap.cs`: bootstrap runtime paralelo ao pipeline DevQA.
- `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs`: arquivo Dev sem guard de compilação explícito.
- `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs` e `Modules/PostGame/Bindings/PostGameOverlayController.cs` têm context menus QA em arquivos de runtime (não mover sem validação de prefab/inspector).

## Comandos rg usados (evidência)
```text
rg -n "DevInstaller|DevBootstrap|ContextMenu|RuntimeDebugGui|MenuItem" Modules Infrastructure
rg -n "#if\s+UNITY_EDITOR|#if\s+DEVELOPMENT_BUILD|UNITY_EDITOR\s*\|\|\s*DEVELOPMENT_BUILD" Modules
rg -n "InstallDevQaServices|CompositionInstallStage\.DevQA|DevQaCompositionModule" Infrastructure/Composition
rg -n "QA_" Modules
rg -n "Register.*Qa|Register.*Dev|Install.*Qa|Install.*Dev" Infrastructure/Composition
```

## Resultado DQ-1.1
- Nenhuma alteração de `.cs`.
- Nenhum move realizado (behavior-preserving + evitar mover no escuro).