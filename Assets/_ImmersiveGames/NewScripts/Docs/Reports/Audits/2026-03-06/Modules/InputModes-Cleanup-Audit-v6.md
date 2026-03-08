# InputModes Cleanup Audit v6 (IM-1.2e Freeze)
Date: 2026-03-10
Scope: InputModes + SceneFlowInputModeBridge + PauseOverlayController + PostGameOwnershipService + PostGameOverlayController + GameLoopService + ConfirmToStartIntroStageStep
Source of truth: workspace local (`C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts`)

## Objetivo
Congelar em documentacao canonica o estado Baseline 3.1 do trilho de InputModes apos IM-1.2d e IM-1.2d.1: single-writer, request rail, gating e comportamento degraded/fail-soft.

## Criteria OK/FAIL
- OK1 single-writer: `rg A` retorna apenas `Modules/InputModes/Runtime/InputModeCoordinator.cs` como callsite real de write.
- OK2 rail register unico: `rg C` retorna exatamente 1 `Register` em `InputModeCoordinator.cs`.
- OK3 leak sweep 0: `rg F` retorna 0 matches.
- OK4 composition gating presente: `rg E` mostra `InputModeCoordinator`, `SceneFlowInputModeBridge`, `RegisterInputModesFromRuntimeConfig` e o anchor `input_modes_disabled_or_not_registered` em `GlobalCompositionRoot.InputModes.cs`.

## Evidencias rg
### A) Single-writer (callsites reais; exclui contrato/impl)
Command:
```text
rg -n "SetFrontendMenu\(|SetGameplay\(|SetPauseOverlay\(" Assets/_ImmersiveGames/NewScripts/Modules -g "*.cs" -g "!**/IInputModeService.cs" -g "!**/InputModeService.cs"
```
Result:
```text
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\InputModes\Runtime\InputModeCoordinator.cs:72:                    service.SetFrontendMenu(evt.Reason);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\InputModes\Runtime\InputModeCoordinator.cs:75:                    service.SetGameplay(evt.Reason);
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\InputModes\Runtime\InputModeCoordinator.cs:78:                    service.SetPauseOverlay(evt.Reason);
```

### B) Request rail - raises
Command:
```text
rg -n "EventBus<InputModeRequestEvent>\.Raise\(" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Result:
```text
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\InputModes\Interop\SceneFlowInputModeBridge.cs:90:                EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\InputModes\Interop\SceneFlowInputModeBridge.cs:174:                EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Runtime\Services\GameLoopService.cs:316:            EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\IntroStage\Runtime\ConfirmToStartIntroStageStep.cs:85:            EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\PostGame\PostGameOwnershipService.cs:86:            EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\PostGame\PostGameOwnershipService.cs:101:            EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\PostGame\Bindings\PostGameOverlayController.cs:359:            EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\PostGame\Bindings\PostGameOverlayController.cs:366:            EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:204:            EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:323:            EventBus<InputModeRequestEvent>.Raise(
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\GameLoop\Pause\Bindings\PauseOverlayController.cs:329:            EventBus<InputModeRequestEvent>.Raise(
```

### C) Request rail - register (deve ser 1)
Command:
```text
rg -n "EventBus<InputModeRequestEvent>\.Register\(" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Result:
```text
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Modules\InputModes\Runtime\InputModeCoordinator.cs:23:            EventBus<InputModeRequestEvent>.Register(_requestBinding);
```

### D) Registro do servico (deve ser 1)
Command:
```text
rg -n "RegisterGlobal<IInputModeService>" Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition -g "*.cs"
```
Result:
```text
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:78:                provider.RegisterGlobal<IInputModeService>(new InputModeService(playerMapName, menuMapName));
```

### E) Registro do coordinator/bridge + gating
Command:
```text
rg -n "InputModeCoordinator|SceneFlowInputModeBridge|input_modes_disabled_or_not_registered|RegisterInputModesFromRuntimeConfig" Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition -g "*.cs"
```
Result:
```text
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.Pipeline.cs:38:            RegisterInputModesFromRuntimeConfig();
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:17:        private static void RegisterInputModesFromRuntimeConfig()
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:110:        private static void RegisterInputModeCoordinator()
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:113:                () => new InputModeCoordinator(),
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:114:                "[InputMode] InputModeCoordinator ja registrado no DI global.",
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:115:                "[InputMode] InputModeCoordinator registrado no DI global."
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:126:            RegisterInputModeCoordinator();
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:128:                () => new SceneFlowInputModeBridge(),
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:129:                "[InputMode] SceneFlowInputModeBridge ja registrado no DI global.",
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:130:                "[InputMode] SceneFlowInputModeBridge registrado no DI global."
C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Infrastructure\Composition\GlobalCompositionRoot.InputModes.cs:166:                "[OBS][InputMode] InputModeCoordinator/Bridge skipped reason='input_modes_disabled_or_not_registered'.",
```

### F) Leak sweep (runtime)
Command:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
```
Result:
```text
0 matches
```

## Freeze summary
- Single-writer ativo: `InputModeCoordinator`.
- Request rail ativo: `InputModeRequestEvent` + requestors listados em `rg B`.
- Executor preservado: `InputModeService`.
- Registro/gating preservado: `GlobalCompositionRoot.InputModes.cs`.
- Hardening ativo: composition gating + warn-once no coordinator quando o servico nao existe.
- Dedupe same-frame endurecido: a key inclui `kind|source|reason|contextSignature`.

## Checklist manual (Editor/DevBuild)
- Gameplay -> Pause -> Resume -> PostGame -> Menu
- Confirmar logs:
  - `InputModeRequested`
  - `InputModeApplied`
  - `InputModeRequestDeduped` apenas quando houver request identica no mesmo frame
- Confirmar ausencia de regressao em pause/menu/postgame

## Notas
- Esta etapa e DOC-only.
- Workspace local e a fonte da verdade.
- Nenhum `.cs` foi alterado nesta tarefa.
