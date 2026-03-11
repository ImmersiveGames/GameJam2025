# IM-1.1 - InputModes canonical audit (DOC-only)

Date: 2026-03-08
Status: DOC-only

## Objetivo
- registrar o snapshot canônico do módulo `InputModes` no workspace local
- separar owners, consumers e pontos de integração com SceneFlow / Pause / PostGame
- listar candidatos de simplificação para `IM-1.2` sem alterar runtime nesta etapa

## Inventário A / B / C / D
| Component | Bucket | Rationale curto | Evidência |
|---|---|---|---|
| `Modules/InputModes/IInputModeService.cs` | A canonical | contrato único de alternância de input modes | interface consumida por Pause/PostGame/GameLoop/IntroStage |
| `Modules/InputModes/InputModeService.cs` | A canonical | implementação concreta de switch de action maps | criado por composição global; múltiplos consumers via interface |
| `Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs` | A canonical | owner de registro do serviço e bridge no DI global | chamado por `GlobalCompositionRoot.Pipeline.cs` |
| `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` | A canonical | owner do sync por evento de SceneFlow | registrado pela composição e consome `SceneTransition*` |
| `Infrastructure/RuntimeMode/RuntimeModeConfig.cs` (`InputModesSettings`) | A canonical | source of truth de enable/map names/logVerbose | lido por composição global |
| `Modules/InputModes/Bindings/InputModeBootstrap.cs` | C reserve | bootstrap alternativo por componente; duplica registro do serviço no escopo local auditado | sem callsites em `.cs`; sem refs em `.unity/.prefab/.asset` dentro de `NewScripts` |
| `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs` | D legacy-ish wrapper | parcial wrapper vazia, sem lógica runtime própria | arquivo contém apenas comentário de wrapper legado |

## Outputs rg (resumo)
### 1) Inventário de tipos / entry points
Comando:
```text
rg -n "IInputModeService|InputModeService|InputModes|ActionMap|playerActionMapName|menuActionMapName" Modules Infrastructure -g "*.cs"
```
Resumo curto:
```text
Modules/InputModes/InputModeService.cs: implementa IInputModeService e alterna Player/UI maps.
Modules/InputModes/Bindings/InputModeBootstrap.cs: registra IInputModeService com map names configuráveis.
Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs: registra IInputModeService no DI e cria SceneFlowInputModeBridge.
Infrastructure/RuntimeMode/RuntimeModeConfig.cs: define InputModesSettings com enableInputModes/playerActionMapName/menuActionMapName/logVerbose.
Consumers diretos de IInputModeService: PauseOverlayController, PostGameOwnershipService, PostGameOverlayController, GameLoopService, ConfirmToStartIntroStageStep.
```

### 2) Bridge / consumo de SceneFlow
Comando:
```text
rg -n "SceneFlowInputModeBridge|SceneTransitionStartedEvent|SceneTransitionCompletedEvent" Modules Infrastructure -g "*.cs"
```
Resumo curto:
```text
Modules/InputModes/Interop/SceneFlowInputModeBridge.cs: consome SceneTransitionStarted/Completed e sincroniza InputMode + GameLoop.
Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs: registra SceneFlowInputModeBridge no DI global.
Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs: chama RegisterInputModeSceneFlowBridge().
SceneTransitionStarted/Completed também são consumidos por GameLoopSceneFlowCoordinator, LoadingHudOrchestrator, SceneFlowSignatureCache, GameReadinessService e LevelStageOrchestrator.
```

### 3) Pausa / overlay e interação com InputModes
Comando:
```text
rg -n "GamePauseCommandEvent|GameResumeRequestedEvent|state\.pause|PauseOverlay" Modules -g "*.cs"
```
Resumo curto:
```text
Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs: owner da UI de pausa; consome IInputModeService e eventos GamePauseCommandEvent/GameResumeRequestedEvent.
Modules/Gates/Interop/GamePauseGateBridge.cs: converte pause/resume em gate token state.pause.
Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs: também observa pause/resume para sincronizar estado.
Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs: reage aos mesmos eventos no trilho GameLoop.
```

### 4) Leak sweep fora de Dev/Editor/Legacy/QA
Comando:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod|RuntimeInitializeOnLoadMethod" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
```
Resultado observado:
```text
.Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61: [RuntimeInitializeOnLoadMethod(...)]
.Core/Logging/DebugUtility.cs:62: [RuntimeInitializeOnLoadMethod(...)]
```
Leitura:
- nao apareceu leak de `UnityEditor`, `EditorApplication`, `AssetDatabase`, `FindAssets`, `MenuItem` ou `ContextMenu` no trilho runtime auditado
- os 2 hits sao a allowlist runtime-init já conhecida, fora do escopo específico de InputModes

## Owners canônicos
- Registro do serviço / bridge: `Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs`
- Contrato público: `Modules/InputModes/IInputModeService.cs`
- Alternância concreta de action maps: `Modules/InputModes/InputModeService.cs`
- Sync orientado por SceneFlow: `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
- Configuração: `Infrastructure/RuntimeMode/RuntimeModeConfig.cs` (`InputModesSettings`)

## Top 5 candidatos IM-1.2
1. `InputModeBootstrap` vs `GlobalCompositionRoot.InputModes`.
Risco: `Med-High`.
Evidência: ambos constroem `InputModeService`, resolvem/fallback de map names e registram `IInputModeService`; no escopo local não há callsites `.cs` nem refs `.unity/.prefab/.asset` para `InputModeBootstrap`.

2. Fallback/default de map names distribuído.
Risco: `Med`.
Evidência: `RuntimeModeConfig.InputModesSettings`, `GlobalCompositionRoot.InputModes.Resolve default`, `InputModeBootstrap.ResolveMapName` e `InputModeService` repetem a mesma convenção `Player/UI`.

3. Muitos callers diretos de `IInputModeService` fora do bridge central.
Risco: `High`.
Evidência: `PauseOverlayController`, `PostGameOwnershipService`, `PostGameOverlayController`, `GameLoopService`, `ConfirmToStartIntroStageStep` pedem modos diretamente, aumentando risco de drift de razões/ownership.

4. `SceneFlowInputModeBridge` mistura duas responsabilidades.
Risco: `Med-High`.
Evidência: além de aplicar `FrontendMenu`/`Gameplay`, ele também sincroniza `IGameLoopService` (`RequestResume`, `RequestReady`, etc.) na mesma classe/event flow.

5. Trilha de pause espalhada em múltiplos bridges/consumers.
Risco: `Med`.
Evidência: `PauseOverlayController`, `GamePauseGateBridge`, `StateDependentService` e `GameLoopCommandEventBridge` reagem ao mesmo par `GamePauseCommandEvent` / `GameResumeRequestedEvent`, com impacto indireto em input mode/pause ownership.

## Arquivos tocados
- `Docs/Modules/InputModes.md`
- `Docs/Reports/Audits/2026-03-06/Modules/InputModes-Cleanup-Audit-v6.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`

