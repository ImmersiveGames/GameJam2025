# Baseline 2.1 — Evidências consolidadas (2026-01-16)
## Objetivo
Consolidar, em um único arquivo, a evidência mínima (com referências de linha) para suportar ADRs e validações de regressão.
## Artefatos desta execução
- Log bruto (imutável): `./Logs/Baseline-2.1-Smoke-2026-01-16.log`
- Verificação contract-driven (referência): `./Verifications/Baseline-2.1-ContractVerification-2026-01-16.md`
- Total de linhas no log: **1330**
- Capture start: L1 — `[Baseline21Smoke] CAPTURE STARTED. utc=2026-01-16T18:54:09.2339900Z captureId=f2b79f1ccc6849f9b3360a3866f841af`
- Capture stop: L1285 — `[Baseline21Smoke] CAPTURE STOPPED. utc=2026-01-16T18:54:58.4565695Z duration=49,23s reason=ExitingPlayMode`

## Matrix de evidências (contrato mínimo)
| Domínio | Item | Status | Linhas |
|---|---|---|---|
| SceneFlow | SceneFlow/Started | OK | L181, L286, L584 |
| SceneFlow | SceneFlow/ScenesReady | OK | L240, L251, L350 |
| SceneFlow | SceneFlow/Completed | OK | L269, L483, L748 |
| WorldLifecycle | WorldLifecycle/ResetRequested | OK | L244, L354, L606 |
| WorldLifecycle | WorldLifecycle/ResetCompletedEvent | OK | L246, L247, L445 |
| GameLoop | IntroStage/UIConfirm | OK | L491, L492, L759 |
| GameLoop | IntroStage/NoContent | N/A | — |
| WorldLifecycle | Skipped_StartupOrFrontend | OK | L243, L244, L245 |
| WorldLifecycle | Failed_NoController | N/A | — |

### Notas de leitura do status
- **OK**: evidência encontrada no log desta execução.
- **WARN**: evidência esperada para o cenário, mas não localizada (validar se o cenário foi exercitado).
- **N/A**: não aplicável / não exercitado nesta execução (ex.: cenários de falha).

## Invariantes
| Domínio | Invariante | Status | Evidência |
|---|---|---|---|
| SceneFlow | ScenesReady ocorre antes de Completed | OK | ScenesReady=L240 ; Completed=L269 |
| WorldLifecycle | ResetCompletedEvent é emitido | OK | L246 |

## Snippets canônicos (curados)

### SceneFlow — transições (Started / ScenesReady / Completed)
```
L181: [VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup' (@ 3,57s)
L286: [VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 9,89s)
L584: [VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 21,03s)
L809: [VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay' (@ 30,99s)
L1250: [VERBOSE] [GameReadinessService] [Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene', UseFade=True, Profile='frontend' (@ 37,74s)
```

### WorldLifecycle — reset (REQUESTED / COMPLETED)
```
L244: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene', signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', profile='startup'.</color>
L354: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. reason='ScenesReady/GameplayScene', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', profile='gameplay'.</color>
L606: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. reason='ScenesReady/GameplayScene', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', profile='gameplay'.</color>
L831: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. reason='ScenesReady/GameplayScene', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', profile='gameplay'.</color>
L984: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. signature='directReset:scene=GameplayScene;src=Gameplay/HotkeyR;seq=1;salt=a3c26649', reason='ProductionTrigger/Gameplay/HotkeyR', source='Gameplay/HotkeyR', scene='GameplayScene'.</color>
L1116: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. signature='directReset:scene=GameplayScene;src=Gameplay/HotkeyR;seq=2;salt=a3c26649', reason='ProductionTrigger/Gameplay/HotkeyR', source='Gameplay/HotkeyR', scene='GameplayScene'.</color>
L1301: <color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset REQUESTED. reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene', signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', profile='frontend'.</color>
```

### IntroStage — evidências
```
L473: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageStarted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
L476: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
L492: <color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' skip=false signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
L494: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' result='completed' profile='gameplay' target='GameplayScene'.</color>
L500: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
L738: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageStarted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
L741: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
L760: <color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' skip=false signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
L762: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' result='completed' profile='gameplay' target='GameplayScene'.</color>
L768: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
L963: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageStarted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
L966: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/Completed'.</color>
L1091: <color=#A8DEED>[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' skip=false signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene'.</color>
L1093: <color=#A8DEED>[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' result='completed' profile='gameplay' target='GameplayScene'.</color>
... (+1 ocorrências)
```

### InputMode — mudanças
```
L99: <color=#A8DEED>[VERBOSE] [InputModeSceneFlowBridge] [InputMode] InputModeSceneFlowBridge registrado nos eventos de SceneTransitionStartedEvent e SceneTransitionCompletedEvent. (@ 3,24s)</color>
L102: <color=#A8DEED>[VERBOSE] [GlobalBootstrap] [InputMode] InputModeSceneFlowBridge registrado no DI global. (@ 3,24s)</color>
L224: <color=#4CAF50>[INFO] [InputModeBootstrap] [InputMode] IInputModeService registrado no DI global.</color>
L264: <color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'FrontendMenu' (SceneFlow/Completed:Frontend).</color>
L265: <color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Nenhum PlayerInput ativo encontrado ao aplicar modo 'FrontendMenu'. Isto é esperado em Menu/Frontend. Em Gameplay, verifique se o Player foi spawnado. (@ 5,92s)</color>
L463: <color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (SceneFlow/Completed:Gameplay).</color>
L464: <color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (SceneFlow/Completed:Gameplay). (@ 10,98s)</color>
L479: <color=#A8DEED>[INFO] [ConfirmToStartIntroStageStep] [OBS][InputMode] Apply mode='FrontendMenu' map='UI' phase='IntroStage' reason='IntroStage/ConfirmToStart' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay'.</color>
L480: <color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'FrontendMenu' (IntroStage/ConfirmToStart).</color>
L481: <color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'UI' em 1/1 PlayerInput(s) (IntroStage/ConfirmToStart). (@ 10,98s)</color>
L505: <color=#A8DEED>[INFO] [GameLoopService] [OBS][InputMode] Apply mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing' signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' scene='GameplayScene' profile='gameplay' frame=1517.</color>
L506: <color=#A8DEED>[INFO] [InputModeService] [InputMode] Modo alterado para 'Gameplay' (GameLoop/Playing).</color>
L507: <color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Applied map 'Player' em 1/1 PlayerInput(s) (GameLoop/Playing). (@ 12,40s)</color>
L519: <color=#A8DEED>[VERBOSE] [InputModeService] [InputMode] Modo 'Gameplay' ja ativo. Reaplicando (PostGame/RunStarted). (@ 12,40s)</color>
... (+26 ocorrências)
```

## Mapa rápido para ADRs
- **ADR-0013 (Ciclo de vida)**: use seções *WorldLifecycle* + invariantes.
- **ADR-0010 (Loading/SceneFlow)** e **ADR-0009 (Fade)**: use seções *SceneFlow* + invariantes + snippets de Fade/Loading se necessário.
- **ADR-0016/0017 (ContentSwap)**: este run não demonstra ContentSwap explicitamente; se precisar, rodar cenário de ContentSwap/InPlace e/ou ContentSwap/WithTransition e regenerar evidências.

## Observações
- Caso o verificador 2.1 esteja marcando FAIL por divergência de tokens genéricos (placeholders), a fonte de verdade para ADR continua sendo o *log bruto* e os *snippets curados* acima.
