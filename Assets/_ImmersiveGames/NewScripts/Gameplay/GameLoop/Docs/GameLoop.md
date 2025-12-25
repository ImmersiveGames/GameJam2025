# GameLoop (NewScripts)

## Escopo e responsabilidade
O **GameLoop** define o **estado macro da simulação** (ativo/inativo/pausado) e expõe isso como uma **fonte de verdade operacional** para sistemas que precisam saber se a simulação está jogável.

Ele **não** representa navegação de app, telas de frontend, menus de UI ou carregamento de cenas. Esses temas pertencem ao **SceneFlow / App Frontend**.

### O que o GameLoop controla
- Estado macro: **Boot / Ready / Playing / Paused**
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

## Diretrizes SOLID
- **SRP**: FSM só faz macro estado; Coordinator faz sincronia com SceneFlow; Bridges só traduzem eventos.
- **DIP**: dependências via DI (ex.: resolver `IGameLoopService` no bridge).
- **Determinismo**: FSM processa sinais transitórios por tick e limita transições por tick.
- **Semântica limpa**: Ready substitui “Menu” e evita poluição conceitual de UI no GameLoop.

---

## Atualização (2025-12-25)
- Padronizado: **GameLoopStateId.Ready** (removendo “Menu” como estado macro).
- `GameStartRequestedEvent` definido como REQUEST canônico; `GameStartCommandEvent` mantido apenas como legado (obsoleto).
- Coordinator trata start como REQUEST e executa start efetivo somente após readiness + reset/skip.
