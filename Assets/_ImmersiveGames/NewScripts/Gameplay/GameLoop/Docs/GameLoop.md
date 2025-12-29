# GameLoop (NewScripts)

## Escopo e responsabilidade
O **GameLoop** define o **estado macro da simulação** (ativo/inativo/pausado) e expõe isso como uma **fonte de verdade operacional** para sistemas que precisam saber se a simulação está jogável.

Ele **não** representa navegação de app, telas de frontend, menus de UI ou carregamento de cenas. Esses temas pertencem ao **SceneFlow / App Frontend**.

### O que o GameLoop controla
- Estado macro: **Boot / Ready / Playing / Paused / PostPlay**
- “Game activity” (simulação ativa ou não)
- Sinalização do estado para observadores (ex.: UI reativa, HUD, debug)

### O que o GameLoop NÃO faz
- Carregar/descarregar cenas
- Executar spawn/despawn / reset de mundo
- Controlar Fade/Loading
- Orquestrar readiness/gates (isso é **infra externa**)

---

## Estados do GameLoop (semântica corrigida)
Os estados **não representam telas**, mas **fases da simulação**:

| Estado    | Significado |
|----------|-------------|
| **Boot** | Simulação ainda não iniciada (pré-start). |
| **Ready** | Simulação em modo “idle” (não ativa). **Substitui o legado “Menu”.** |
| **Playing** | Simulação ativa (jogável). |
| **Paused** | Simulação pausada (jogável, mas ações bloqueadas via gate/pause). |
| **PostPlay** | Estado pós-gameplay após o fim da run, antes de reiniciar ou sair para o menu. |

**PostPlay** é alcançado quando uma run termina e o GameLoop recebe o sinal de término
(`GameRunEndedEvent` → `RequestEnd()` → `EndRequested`). Esse estado representa o “pós-game”
enquanto a UI exibe o resultado e o jogador decide reiniciar ou retornar ao menu.

> Nota: **Ready não é UI/Menu**. O nome “Menu” era legado conceitual e deve ser removido do macro estado.

---

## Arquitetura (papéis)
- **GameLoopService**: façade do GameLoop (API de start/pause/resume/reset) + logs + observer.
- **GameLoopStateMachine**: FSM pura, determinística, sem MonoBehaviour.
- **GameLoopSceneFlowCoordinator**: coordena **REQUEST de start** com SceneFlow + WorldLifecycle e só então chama `GameLoopService.RequestStart()`.
- **Bridges**:
    - *Entrada de eventos definitivos* (pause/resume/reset) → `IGameLoopService`
    - *Pause → SimulationGate* (opcional) via `GamePauseGateBridge`

---

## Eventos (REQUEST vs COMMAND)
### Start
- **`GameStartRequestedEvent` (REQUEST)**: intenção de iniciar.
- Start efetivo: **Coordinator** → SceneFlow → WorldLifecycle → `IGameLoopService.RequestStart()`.

> Regra: sistemas não devem “dar start” no GameLoop diretamente ao receber REQUEST.

### Pause/Resume/Reset (definitivos)
- `GamePauseCommandEvent` (COMMAND)
- `GameResumeRequestedEvent` (REQUEST simples, tratado como comando no bridge)
- `GameResetRequestedEvent` (REQUEST, vira `RequestReset()` no service)

---

## Ciclo de run (start / end / pós-game)
- **Boot → Ready → Playing**:
  coordenado pelo `GameLoopSceneFlowCoordinator`, que chama `IGameLoopService.RequestStart()` somente
  quando SceneFlow + WorldLifecycle estão prontos.
- **Início de run**:
  ao entrar em `Playing`, o `GameLoopService` publica `GameRunStartedEvent`.
  O `GameRunStatusService` recebe esse evento e chama `Clear()`, limpando o resultado anterior.
- **Fim de run**:
  um sistema de gameplay emite `GameRunEndedEvent` com `GameRunOutcome` e `Reason`.
  O `GameLoopRunEndEventBridge` converte o evento em `IGameLoopService.RequestEnd()`.
  A `GameLoopStateMachine` faz `Playing → PostPlay` quando `EndRequested` está `true`.
- **Pós-game**:
  em `PostPlay`, o loop permanece estável enquanto a UI decide entre:
  - **Reiniciar** a run (reset + novo start), ou
  - **Voltar ao menu** (`GameExitToMenuRequestedEvent` → `ExitToMenuNavigationBridge` / `IGameNavigationService`).

### Fluxo de pós-game (UI + reset)
- `GameRunEndedEvent` → `GameRunStatusService` armazena resultado.
- `PostGameOverlayController` consulta `IGameRunStatusService` e exibe Victory/Defeat/Match Ended.
- Botão **Restart**:
  - `GameResetRequestedEvent` → `RestartNavigationBridge` → `IGameNavigationService.RequestToGameplay(...)`.
  - SceneFlow emite `SceneTransitionScenesReadyEvent` (profile gameplay) → `WorldLifecycleRuntimeCoordinator` → reset determinístico.
- Botão **Exit to Menu**:
  - `GameExitToMenuRequestedEvent` → `ExitToMenuNavigationBridge` → `IGameNavigationService.RequestToMenu(...)`.

---

## Fluxo de produção (startup vs gameplay)
### Startup / Frontend (Menu)
- `GameLoopService` é registrado no escopo global e inicializado no primeiro `RequestStart()`.
- A transição `startup` (MenuScene) conclui `SceneTransitionCompletedEvent` e recebe
  `WorldLifecycleResetCompletedEvent` mesmo quando o reset é **SKIPPED**.
- O `GameLoopSceneFlowCoordinator` considera **ready** quando recebe `TransitionCompleted + WorldLifecycleResetCompleted`
  e chama `GameLoop.RequestStart()`, levando **Boot → Ready** (sem entrar em `Playing`).
- `InputModeSceneFlowBridge` aplica `FrontendMenu` ao receber `SceneTransitionCompleted(profile='startup'/'frontend')`.

### Gameplay
- `SceneTransitionCompleted(profile='gameplay')` aciona `InputModeSceneFlowBridge`:
  - aplica `InputMode = Gameplay`,
  - chama `GameLoop.RequestStart()` após a transição.
- Com a transição completa e o reset confirmado, o GameLoop avança **Ready → Playing**.
- `GameRunStatusService` é alimentado normalmente pelo fluxo `GameRunStartedEvent` / `GameRunEndedEvent`.

---

## QA (hotkeys de pós-game)
- `PostGameQaHotkeys` (F6/F7) pode disparar `GameRunEndedEvent` diretamente:
  - F6 → `Outcome=Defeat`, `Reason='QA_ForcedDefeat'`
  - F7 → `Outcome=Victory`, `Reason='QA_ForcedVictory'`
- Nessa situação, o `GameRunStatusService` pode emitir warnings como:
  - `[WARNING] [GameRunStatusService] [GameLoop] GameLoopService indisponível ao processar GameRunEndedEvent. RequestEnd() não foi chamado.`
- Esses warnings são **esperados em contexto QA** (evento forçado sem `GameLoopService.RequestEnd()`) e não indicam erro funcional do ciclo de jogo.

---

## Telemetria de atividade do GameLoop
O `GameLoopService.OnGameActivityChanged` publica `GameLoopActivityChangedEvent` com:
- `CurrentStateId` (estado atual da `GameLoopStateMachine`),
- `IsActive` (true quando o loop está em estado “ativo”, hoje alinhado ao `Playing`).

Uso esperado:
- UI/QA/telemetria podem observar o evento para saber quando o gameplay ficou ativo/inativo,
  sem acoplar diretamente à máquina de estados.
- A autorização final de ações permanece no `IStateDependentService` (gate-aware), não no evento.

---

## Serviço de status da run (IGameRunStatusService)
O `IGameRunStatusService` mantém o resultado da última run:
- `HasResult`
- `Outcome` (`GameRunOutcome`)
- `Reason` (string livre, ex.: "AllPlanetsDestroyed", "BossDefeated", "QA_ForcedEnd")

Integração com eventos:
- `GameRunStartedEvent` → `Clear()` → limpa o resultado anterior.
- `GameRunEndedEvent` → preenche `HasResult`/`Outcome`/`Reason`.

Uso recomendado:
- UI de pós-game (Game Over / Victory) deve consultar `IGameRunStatusService` como fonte de verdade,
  evitando acoplamento direto a sistemas específicos de gameplay.

---

## Diretrizes SOLID
- **SRP**: FSM só faz macro estado; Coordinator faz sincronia com SceneFlow; Bridges só traduzem eventos.
- **DIP**: dependências via DI (ex.: resolver `IGameLoopService` no bridge).
- **Determinismo**: FSM processa sinais transitórios por tick e limita transições por tick.
- **Semântica limpa**: Ready substitui “Menu” e evita poluição conceitual de UI no GameLoop.

---

## Nota sobre StateDependent (movimento)
- Os controladores de movimento de **Player** e **Eater** usam `IStateDependentService` para a ação `Move`.
- Em `Boot`, durante transições, ou quando pausado, `CanExecuteAction(ActionType.Move)` retorna **false** e
  bloqueia movimento.
- Em `Playing` com gate aberto e gameplay pronta, `Move` é liberado.

---

## Atualização (2025-12-25)
- Padronizado: **GameLoopStateId.Ready** (removendo “Menu” como estado macro).
- `GameStartRequestedEvent` definido como REQUEST canônico; `GameStartCommandEvent` mantido apenas como legado (obsoleto).
- Coordinator trata start como REQUEST e executa start efetivo somente após readiness + reset/skip.
