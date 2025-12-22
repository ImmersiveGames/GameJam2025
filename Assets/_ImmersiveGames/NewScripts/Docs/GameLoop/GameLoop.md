# GameLoop (NewScripts)

## O que é o GameLoop
- FSM leve que controla estados **Boot → Playing → Paused** no baseline NewScripts.
- Autoridade primária para saber se o jogo está jogável ou pausado.
- Mantém sinais transitórios (start/pause/resume/reset) e atualiza o estado corrente.

## Como Pause funciona (sem congelar física)
- Pausa **não usa `Time.timeScale`** nem congela física/`Rigidbody`.
- Tokens de gate (`SimulationGateTokens.Pause`) continuam sendo a proteção para entradas de movimento.
- Resultado: inputs de movimentação são bloqueados, mas simulação física/gravity continuam ativas.

## Bootstrap (NEWSCRIPTS_MODE)
- O boot do NewScripts acontece via `GlobalBootstrap` (BeforeSceneLoad).
- `GlobalBootstrap` registra `ISimulationGateService`, `GamePauseGateBridge`, `NewScriptsStateDependentService` e o pipeline de câmera do NewScripts.
- O bootstrap do legado **não** inicializa esses serviços quando `NEWSCRIPTS_MODE` está ativo.

## De onde vêm os sinais (eventos → bridge → GameLoop)
- Eventos globais existentes entram via **GameLoopEventInputBridge**:
  - `GameStartEvent` → `RequestStart()`
  - `GamePauseEvent(IsPaused=true)` → `RequestPause()`
  - `GamePauseEvent(IsPaused=false)` ou `GameResumeRequestedEvent` → `RequestResume()`
  - `GameResetRequestedEvent` → `RequestReset()` (quando usado)
- O bridge apenas **ouve** eventos e sinaliza o GameLoop; não republica eventos.
- Se o EventBus não estiver disponível, o bridge loga e continua sem travar boot.

## Como o StateDependentService decide o que pode fazer
- `NewScriptsStateDependentService` consulta o **GameLoop** como fonte primária:
  - `Playing`: ações liberadas (exceto bloqueio de gate).
  - `Paused` ou `Boot`: apenas `Navigate`, `UiSubmit`, `UiCancel`, `RequestReset`, `RequestQuit`.
- Gate de pause continua vigente:
  - `SimulationGateTokens.Pause` ativo → bloqueia `ActionType.Move` e loga uma vez.
  - Não altera física/timeScale; apenas evita comandos de movimento.
- Se o GameLoop não estiver disponível (ex.: ordem de boot), o serviço faz **fallback** para o estado interno baseado em eventos legados.

## Troubleshooting
- **GameLoop não registrado**: verificará estado interno; registre via `GameLoopBootstrap.EnsureRegistered()` ou `GameLoopDriver` em cena.
- **Sem logs de integração**: o log verbose `[StateDependent] Integrado ao GameLoop...` aparece apenas na primeira resolução bem-sucedida do GameLoop.
- **Pausa não bloqueia movimento**: confirme se o token `SimulationGateTokens.Pause` está ativo (via `ISimulationGateService`) e se o bridge está ouvindo `GamePauseEvent`.
