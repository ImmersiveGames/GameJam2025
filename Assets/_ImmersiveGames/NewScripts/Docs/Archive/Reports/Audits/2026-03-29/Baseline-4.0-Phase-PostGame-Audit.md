# Baseline 4.0 - Phase PostGame Audit

## 1. Canonical Target

`PostGame` e o owner canonico de:
- ownership do pos-run
- gate e elegibilidade contextual do pos-run
- publicacao de `PostGameEnteredEvent` / `PostGameExitedEvent`
- coordenacao do `PostStage` como handoff transitivo apos `RunEnd`
- consumo do resultado consolidado para apresentacao

`PostGame` nao pode possuir:
- gameplay state machine
- primary navigation policy
- route dispatch
- audio precedence/context ownership
- ownership de overlay visual
- execução de `Restart` ou `ExitToMenu` como rota
- fallback silencioso para compensar snapshot ausente

### Terminologia preferencial desta fase

- `RunEnd`: fronteira de entrada vinda do `GameLoop`.
- `PostGame`: owner do pos-run.
- `PostStage`: ponte transitoria entre `RunEnd` e o owner do pos-run.
- `PostPlay`: termo legado/ambíguo do `GameLoop`; nao deve ser usado para nomear ownership de `PostGame`.
- `Restart`: intent derivada; `Frontend/UI` emite como input boundary, `PostGameOwnershipService` valida elegibilidade contextual do pos-run, `LevelFlow` executa downstream.
- `ExitToMenu`: intent derivada; `Frontend/UI` emite como input boundary, `PostGameOwnershipService` valida elegibilidade contextual do pos-run, `LevelFlow` e `Navigation` executam downstream.

Termos concorrentes sem autoridade nesta fase:
- `PostPlay` como nome do owner pos-run
- `PostStage` como substituto de ownership pos-run
- `Exit` como nome canonico de saida

### Owners canônicos declarados

- Owner do pós-run: `PostGameOwnershipService`
- Owner da projeção do resultado: `PostGameResultService`
- Owner do overlay pós-run: `Frontend/UI` via `PostGameOverlayController` movido para a camada visual
- Owner da decisão de `Restart`: `Frontend/UI` como input boundary e intent emitter; `PostGameOwnershipService` como owner da elegibilidade contextual; `LevelFlow` como execução downstream
- Owner da decisão de `ExitToMenu`: `Frontend/UI` como input boundary e intent emitter; `PostGameOwnershipService` como owner da elegibilidade contextual; `LevelFlow` e `Navigation` como execução downstream

## 2. Inventory Decision Matrix

| Item | Decision | Justificativa curta |
|---|---|---|
| `PostGameOwnershipService` | Keep with reshape | Estado atual resumido: owns gate/input e publica `PostGameEnteredEvent` / `PostGameExitedEvent`. Forma canonica alvo: owner estrito do pos-run sem projeção visual nem dispatch. Restricao explicita: nao decidir rota, nao decidir audio, nao decidir UI. Condicao de aceite: manter apenas ownership do pos-run e handoff de entrada/saida. |
| `IPostGameOwnershipService` | Keep | Contrato canonico do owner de pos-run. |
| `PostGameResultService` | Keep with reshape | Estado atual resumido: projeta `Victory`/`Defeat`/`Exit` a partir de eventos da run e limpa no start. Forma canonica alvo: read-model do resultado para PostGame, sem decidir UI ou navigation. Restricao explicita: nao publicar eventos de run, nao decidir overlay, nao executar intents. Condicao de aceite: ser a unica fonte de leitura de resultado para o pos-run. |
| `IPostGameResultService` | Keep | Contrato canonico da projeção do resultado. |
| `PostStageCoordinator` | Keep | Owner canonico da orquestracao do `PostStage` como ponte transitiva entre `RunEnd` e o pos-run. |
| `PostStageControlService` | Keep | Estado de controle do stage e do sinal de conclusao. |
| `PostStagePresenterRegistry` | Keep with reshape | Estado atual resumido: adota presenter e falha fast em multiplos candidatos. Forma canonica alvo: registro estrito do presenter do `PostStage`, sem decidir ownership visual permanente. Restricao explicita: nao virar roteador de UI nem fallback de binding. Condicao de aceite: adocao atomica de um presenter valido ou skip fail-fast. |
| `PostStagePresenterScopeResolver` | Keep | Resolvedor de escopo/candidatos do presenter do `PostStage`. |
| `PostStageContext` / `PostStageCompletionResult` / `PostStageStartRequestedEvent` / `PostStageStartedEvent` / `PostStageCompletedEvent` | Keep | Contratos canonicos do stage transitivo. |
| `GameRunEndedEventBridge` | Keep with reshape | Estado atual resumido: faz handoff de `GameRunEndedEvent` para `PostStage` e chama o loop novamente ao final. Forma canonica alvo: ponte transitiva unidirecional entre `RunEnd` e `PostGame`. Restricao explicita: nao decidir PostGame, nao decidir overlay, nao decidir menu. Condicao de aceite: removivel quando o handoff estiver absorvido por owner downstream unico. |
| `PostGameOverlayController` | Move | Estado atual resumido: mistura visibilidade, input mode, gate, leitura de snapshot e emissão de restart/exit. Forma canonica alvo: camada visual de `Frontend/UI` com emissão de intents בלבד. Restricao explicita: nao possuir gate, nao possuir ownership de pos-run, nao possuir fallback de resultado. Condicao de aceite: operar como presenter/intent emitter, sem ownership de fluxo. |
| `GameRunResultSnapshotService` | Replace | Estado atual resumido: snapshot de resultado no `GameLoop` usado como fonte de texto do overlay. Forma canonica alvo: projeção canonica no `PostGameResultService`. Restricao explicita: nao permanecer como fonte paralela de resultado para UI. Condicao de aceite: deixar de ser a leitura principal do pos-run visual. |
| `PostLevelActionsService` | Keep with reshape | Estado atual resumido: executa restart/next-level/exit-to-menu a partir de intents da camada visual. Forma canonica alvo: executor de nivel/saida em `LevelFlow`, nao owner de `PostGame`. Restricao explicita: nao decidir visual, nao absorver route policy. Condicao de aceite: receber intent e executar o caminho downstream sem assumir ownership do overlay. |
| `IGameNavigationService` / `GameNavigationService` | Keep | Owner canonico do dispatch primario de rota e saida para menu. |
| `GameLoopCommands` | Keep with reshape | Estado atual resumido: converte intents de restart/exit/end em comandos do loop. Forma canonica alvo: superficie de comando do `GameLoop`, nao do `PostGame`. Restricao explicita: nao ser usado como atalho de UI nem como dono de decisão de overlay. Condicao de aceite: continuar emitindo intents do loop sem absorver ownership do pos-run. |
| `ExitToMenuCoordinator` | Forbid adapter | Estado atual resumido: mistura liberacao de gate, navigation dispatch e marcacao de resultado de `PostGame`. Forma canonica alvo: nenhuma. Restricao explicita: nao deve permanecer como ponte de conveniencia. Condicao de aceite: aposentado quando o caminho de exit estiver repartido entre `PostGame` (intent), `LevelFlow` (execucao) e `Navigation` (dispatch). |
| `LevelPostStageMockPresenter` | Delete | Estado atual resumido: presenter de QA/prototipagem. Forma canonica alvo: nenhum papel em runtime canonico. Restricao explicita: nao pode ser confundido com presenter de producao. Condicao de aceite: ficar restrito a QA/documentacao ou sair do fluxo de producao. |

## 3. Canonical Runtime Rail

Trilho runtime canônico sustentado por `PostGame`:

1. `GameRunEndedEvent` chega do `GameLoop` como `RunEnd`.
2. `GameRunEndedEventBridge` cria `PostStageContext`.
3. `PostStageCoordinator` inicia o stage transitivo.
4. `PostStageControlService` controla inicio, conclusao ou skip.
5. `PostGameOwnershipService` assume o pos-run e publica `PostGameEnteredEvent`.
6. `PostGameResultService` expõe a projeção consolidada do resultado.
7. `PostGameOverlayController`, já na camada visual, mostra o estado e emite intents.
8. `Restart` sai como intent visual e segue para `LevelFlow.PostLevelActionsService`.
9. `ExitToMenu` sai como intent visual e segue para `LevelFlow.PostLevelActionsService`.
10. `LevelFlow` executa restart/exit; `Navigation` apenas despacha rota quando a saida exigir menu.
11. `PostGameExitedEvent` fecha o pos-run.

`PostStage` e a ponte transitiva. `PostGame` e o owner do pos-run. `PostPlay` nao e o nome do owner desta fase. `RunEnd` e a fronteira de entrada.

## 4. Parallel Rails to Eliminate

- `GameLoop` expondo `PostPlay` como rótulo de estado e competindo com `PostGame`.
- `GameRunResultSnapshotService` no `GameLoop` competindo com `PostGameResultService` como fonte de resultado.
- `PostGameOverlayController` decidindo visual, gate, input mode e execucao downstream ao mesmo tempo.
- `ExitToMenuCoordinator` misturando navigation, gate release e result marking.
- `PostGameOverlayController` chamando `PostLevelActionsService` como workaround permanente sem intent boundary explícito.
- `PostLevelActionsService` sendo lido como ownership de `PostGame` quando seu papel real é executor de `LevelFlow`.
- `GameRunEndedEventBridge` sendo usado como owner de post-run em vez de ponte transitiva.
- logs de fallback ou polling para compensar ausencia de `PostGameResultService` como fonte canônica.

## 5. Phase Scope

### Pertence a esta fase

- `PostGameOwnershipService`
- `IPostGameOwnershipService`
- `PostGameResultService`
- `IPostGameResultService`
- `PostStageCoordinator`
- `PostStageControlService`
- `PostStagePresenterRegistry`
- `PostStagePresenterScopeResolver`
- `PostStageContext` / `PostStageCompletionResult`
- `PostStageStartRequestedEvent` / `PostStageStartedEvent` / `PostStageCompletedEvent`
- `GameRunEndedEventBridge`
- `PostGameOverlayController`
- `GameRunResultSnapshotService`
- `PostLevelActionsService`
- `GameNavigationService`
- `GameLoopCommands`
- `ExitToMenuCoordinator`
- `LevelPostStageMockPresenter`

### Fronteiras fechadas nesta fase

- `PostGameOverlayController` pertence à camada visual e nao pode ser owner de gate ou resultado.
- `PostGame` nao executa route dispatch.
- `PostGame` nao executa audio precedence.
- `PostGame` nao assume state machine do `GameLoop`.
- `Restart` e `ExitToMenu` sao intents emitidas no contexto visual, nao rotas.
- `Navigation` permanece dono do dispatch de rota.
- `LevelFlow` permanece dono da execucao de restart/exit.

### Nao pertence a esta fase

- alteracao de `.cs`
- alteração de assets
- reestruturação de `Navigation`
- reestruturação de `Audio`
- reestruturação de `SceneFlow`
- implementação de novos bridges

## 6. Explicit Prohibitions

- mover ownership para camada visual
- usar adapter/bridge para esconder fronteira errada
- adicionar fallback silencioso
- corrigir sintoma local sem declarar owner canonico
- usar polling/log spam como sustentacao de contrato frágil
- usar log por frame em caminho estavel
- usar polling para sustentar contrato de pos-run, restart ou exit
- usar dedupe em bridge como correção estrutural permanente
- tratar overlay visual como owner de gate, resultado ou route
- tratar compile/runtime como suficiente para aceite arquitetural
- manter `ExitToMenuCoordinator` como adaptador de conveniencia entre navegacao, pause e post-game

## 7. Acceptance Gates

- `PostGameOwnershipService` e a unica superficie que publica `PostGameEnteredEvent` / `PostGameExitedEvent` e controla o gate do pos-run.
- `PostGameResultService` e a unica fonte de projeção do resultado pos-run para leitura arquitetural.
- `PostGameOverlayController` nao possui gate, nao possui ownership de pos-run e nao executa route dispatch.
- `Frontend/UI` nao decide semanticamente `Restart` ou `ExitToMenu`; apenas emite intent.
- A elegibilidade contextual de `Restart` e `ExitToMenu` vem do owner canonico `PostGameOwnershipService`.
- A execução de `Restart` e `ExitToMenu` acontece downstream em `LevelFlow` e, quando aplicavel, em `Navigation`.
- `Restart` e `ExitToMenu` aparecem como intents visuais e sao executados downstream por `LevelFlow`/`Navigation`, nao por um atalho em `PostGame`.
- `GameRunEndedEventBridge` permanece ponte transitiva e tem condição escrita de aposentadoria.
- nenhum caminho depende de fallback silencioso, polling ou log por frame para sustentar contrato.
- nenhum adapter permanente mascara `PostGame -> GameLoop`, `PostGame -> Navigation` ou `PostGame -> LevelFlow`.
- a leitura arquitetural mostra owner unico por fronteira, sem trilho paralelo para resultado, overlay ou saída.

## 8. Evidence Required

Na futura implementacao, a fase deve anexar:

- mapa de ownership por arquivo e por contrato
- trace de `GameRunEndedEvent -> PostStage -> PostGameEnteredEvent -> PostGameExitedEvent`
- evidência de que `PostGameOverlayController` ficou limitado a visual/intent
- evidência de que `PostGameResultService` substituiu qualquer projeção paralela do `GameLoop`
- evidência de que `Restart` e `ExitToMenu` saem como intents e chegam aos executores certos
- evidência de que `Navigation` executa apenas dispatch de rota e nao decisão pos-run
- evidência de que `LevelFlow` executa restart/exit e `PostGame` nao absorve essa responsabilidade
- logs/event trace que mostrem ausência de polling/log spam e ausência de fallback silencioso
- prova de aposentadoria para `ExitToMenuCoordinator` quando o trilho for repartido por owners canonicos
