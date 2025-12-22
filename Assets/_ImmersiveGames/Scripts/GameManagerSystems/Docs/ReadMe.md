# Game Manager System – Visão Geral Atual

O Game Manager centraliza o ciclo de vida da sessão em uma máquina de estados explícita
(`GameLoopStateMachine`) e fornece pontos de extensão via eventos e DI para UI, serviços
persistentes e lógicas de gameplay. Este documento resume o fluxo atual, estados, eventos e
como integrar novas funcionalidades sem acoplamento.

## Estados e ações liberadas

| Estado | Ações permitidas (`ActionType`) | Side-effects de entrada/saída |
| --- | --- | --- |
| `MenuState` | Navegação básica (`Navigate`, `UiSubmit`, `UiCancel`) e atalhos de ciclo (`RequestReset`, `RequestQuit`). | Emite `StateChangedEvent(false)` e `ActorStateChangedEvent(false)` ao entrar; restaura ambos ao sair. |
| `PlayingState` | Gameplay (`Move`, `Shoot`, `Spawn`, `Interact`). | Emite `StateChangedEvent(true)` e `ActorStateChangedEvent(true)`; não altera `timeScale`. |
| `PausedState` | Navegação + reset/quit (mesmos atalhos de menu). | Adquire `SimulationGateTokens.Pause`, emite `StateChangedEvent(false)`/`ActorStateChangedEvent(false)`; **não altera `timeScale`**. |
| `GameOverState` | Navegação + reset/quit. | Adquire `SimulationGateTokens.GameOver`, emite `StateChangedEvent(false)`; mantém `timeScale` intacto. |
| `VictoryState` | Navegação + reset/quit. | Adquire `SimulationGateTokens.Victory`, emite `StateChangedEvent(false)`; mantém `timeScale` intacto. |

O estado inicial é sempre `MenuState`. Estados que bloqueiam ações mantêm a UI operável através
do perfil de navegação compartilhado, permitindo reiniciar ou sair sem congelar física (`timeScale` permanece inalterado).

## Eventos de ciclo de vida

| Evento | Origem típica | Efeito esperado |
| --- | --- | --- |
| `GameStartRequestedEvent` | Botão de UI ou menu de contexto | `GameManager` valida que o estado atual é `MenuState` e publica `GameStartEvent`, acionando a transição para `PlayingState`. |
| `GamePauseRequestedEvent` / `GameResumeRequestedEvent` | UI ou hotkeys | Em `PlayingState` gera `GamePauseEvent(true)`; em `PausedState` gera `GamePauseEvent(false)`. |
| `GameResetRequestedEvent` | UI, hotkey ou contexto | Inicia o pipeline de reset (detalhado abaixo) a partir de qualquer estado. |
| `GameOverEvent` / `GameVictoryEvent` | `GameManager.TryTrigger*` (valida `PlayingState`) | Predicados da FSM disparam a transição para `GameOverState` ou `VictoryState`; eventos são ignorados e logados se chegarem fora de `PlayingState`. |
| `StateChangedEvent` | Estados | Sinaliza se o jogo está ativo (`true`) ou parado (`false`) para serviços genéricos. |
| `ActorStateChangedEvent` | Estados | Específico para atores/controles habilitarem ou desabilitarem atualizações. |
| `GameResetStartedEvent` / `GameResetCompletedEvent` | `GameManager.ResetGameRoutine` | Pontos de sincronização para sistemas persistentes limparem estado antes/depois do reload. |

## Pipeline de reset (robusto)

1. `GameResetRequestedEvent` é recebido e `GameManager.ResetGame()` inicia uma coroutine guardada
   para evitar resets concorrentes.
2. Publica `GameResetStartedEvent` para que pools, HUDs ou serviços persistentes limpem estado.
3. Aguarda um frame para que handlers desliguem timers/spawns, depois espera a FSM voltar ao
   `MenuState` (usa `Time.unscaledDeltaTime` para respeitar pausas).
4. Normaliza `Time.timeScale = 1` de forma defensiva (FSM não altera mais o tempo, mas mantém proteção em resets).
5. Reconstrói a FSM (`GameLoopStateMachine.Rebuild`) limpando bindings antigos.
6. Recarrega a cena ativa via `SceneLoader.ReloadCurrentSceneAsync` e reanexa a cena de UI.
7. Publica `GameResetCompletedEvent` e libera o flag de reset em andamento.

## Integração via DI

`GameManager`, `GameConfig` e `GameLoopStateMachine` são registrados como serviços globais no
`DependencyManager`. Prefira resolver `IGameManager` e `GameConfig` por DI em vez de acessar o
singleton diretamente. Isso permite substituir implementações em testes ou em fluxos de recarga
sem acoplamento.

### Como disparar transições de fim de jogo com validação

Use os helpers do `GameManager` para respeitar o estado atual e gerar logs claros:

```csharp
// Encadeia com os predicados da FSM e falha silenciosamente fora do Playing.
DependencyManager.Provider.GetGlobal<IGameManager>().TryTriggerGameOver("player died");
DependencyManager.Provider.GetGlobal<IGameManager>().TryTriggerVictory("all objectives done");
```

Em casos de UI, continue usando os eventos `*Requested` (start/pause/resume/reset) para que a FSM
controle as transições.

## Boas práticas para novos sistemas

- **Respeite `CanPerformAction`**: antes de executar ações de gameplay, consulte o estado atual
  via FSM ou serviços que encapsulem as permissões de `ActionType`.
- **Inscreva/descadastre**: serviços dependentes de estado devem implementar `IDisposable` e
  descadastrar bindings de evento quando forem removidos dos registries (global/scene/object).
- **Evite acoplamento com UI**: mantenha interações por eventos ou ações genéricas (`Navigate`,
  `UiSubmit`, `UiCancel`, `RequestReset`, `RequestQuit`) para reutilizar fluxos existentes em telas
  de pausa, game over e vitória.

Esse documento será atualizado conforme novos estados, predicados ou perfis de ação forem
adicionados ao ciclo de vida do jogo.
