# DQ-1.3 - DevQA hardening/isolation audit v3 (behavior-preserving)

Date: 2026-03-06
Source of truth: local workspace files.

## Scope
- `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs`
- `Modules/Gameplay/Editor/RunRearm/**`
- `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`
- `Modules/PostGame/Bindings/PostGameOverlayController.cs`
- `Infrastructure/Composition/**` (callsites only)

## Static inventory (commands and relevant evidence)

### Commands executed
```text
rg -n "WorldLifecycleHookLoggerA|RunRearm|PauseOverlayController|PostGameOverlayController" Modules Infrastructure
rg -n "RuntimeInitializeOnLoadMethod|MenuItem\(|ContextMenu\(" Modules
rg -n "#if\s+UNITY_EDITOR|#if\s+DEVELOPMENT_BUILD|UNITY_EDITOR\s*\|\|\s*DEVELOPMENT_BUILD|NEWSCRIPTS_QA|NEWSCRIPTS_DEV" Modules
rg -n "WorldLifecycleHookLoggerA" -g "*.prefab" -g "*.unity" -g "*.asset" .
rg -n "RunRearm" -g "*.prefab" -g "*.unity" -g "*.asset" .
```

### Relevant results
- `Infrastructure/Composition/SceneScopeCompositionRoot.cs:316` -> callsite para `WorldLifecycleHookLoggerA` (agora dentro de `#if UNITY_EDITOR || DEVELOPMENT_BUILD`).
- `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs:1` -> arquivo inteiro guardado por `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `Modules/Gameplay/Editor/RunRearm/RunRearmRequestDevDriver.cs:1` -> `#if UNITY_EDITOR`.
- `Modules/Gameplay/Editor/RunRearm/RunRearmKindDevEaterActor.cs:1` -> `#if UNITY_EDITOR`.
- `Modules/Gameplay/Editor/RunRearm/RunRearmDevStepLogger.cs:1` -> `#if UNITY_EDITOR`.
- `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs:237` -> ContextMenu QA em `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `Modules/PostGame/Bindings/PostGameOverlayController.cs:552` -> ContextMenu QA em `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `rg -n "WorldLifecycleHookLoggerA" -g "*.prefab" -g "*.unity" -g "*.asset" .` -> sem matches.
- `rg -n "RunRearm" -g "*.prefab" -g "*.unity" -g "*.asset" .` -> sem matches.

## Applied changes

| Item | Before | After | Callsite status | Asset/Serialize refs | Residual risk |
|---|---|---|---|---|---|
| `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs` | sem guard explicito de arquivo | arquivo inteiro sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD` | callsite em `SceneScopeCompositionRoot` tambem guardado | nenhum encontrado por `rg` | baixo |
| `Infrastructure/Composition/SceneScopeCompositionRoot.cs` | registro de `WorldLifecycleHookLoggerA` sem guard local | bloco de registro guardado por `#if UNITY_EDITOR || DEVELOPMENT_BUILD` | canonic callsite mantido, apenas condicionado a DevQA | n/a | baixo |
| `Modules/Gameplay/Editor/RunRearm/**` | `#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA` | normalizado para `#if UNITY_EDITOR` | sem callsite canonico em composition | nenhum encontrado por `rg` | baixo |
| `PauseOverlayController` QA ContextMenu | ja guardado | sem mudanca funcional | n/a | n/a | nenhum |
| `PostGameOverlayController` QA ContextMenu/helpers | ContextMenu guardado, helper/flag QA em runtime surface | helper/flag QA movidos para `#if UNITY_EDITOR || DEVELOPMENT_BUILD` | n/a | n/a | baixo |

## Behavior-preserving statement
- Nenhuma mudanca de contratos publicos.
- Nenhuma mudanca de ordem de pipeline.
- Nenhuma mudanca intencional de comportamento de producao.
- Mudancas limitadas a isolamento/guards de superficies DevQA.

## Files changed (code)
- `Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs`
- `Infrastructure/Composition/SceneScopeCompositionRoot.cs`
- `Modules/Gameplay/Editor/RunRearm/RunRearmRequestDevDriver.cs`
- `Modules/Gameplay/Editor/RunRearm/RunRearmKindDevEaterActor.cs`
- `Modules/Gameplay/Editor/RunRearm/RunRearmDevStepLogger.cs`
- `Modules/PostGame/Bindings/PostGameOverlayController.cs`
