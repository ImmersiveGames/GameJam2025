# Baseline 4.0 - Phase GameLoop Audit

## 1. Canonical Target

`GameLoop` e o owner canonico de:
- flow state machine
- run lifecycle
- pause/resume como estado transversal
- run start/end signaling
- activity telemetry

`GameLoop` nao pode possuir:
- post-run ownership
- post-run visual/UI ownership
- route dispatch
- navigation policy
- audio precedence/context ownership
- overlay visual como owner de pause
- fallback silencioso para sustentar contrato fraco

O contrato canônico do domínio é: `GameLoop` governa o estado e o lifecycle da run; os demais dominios consomem sinais, intents e handoffs.

### 1.1 Terminologia preferencial desta fase

- `Playing`: estado ativo canônico do `GameLoop`.
- `RunEnd`: fronteira canônica de termino de run; aqui é o sinal/evento que encerra a execução da run.
- `PostPlay`: estado terminal do `GameLoop` apos `RunEnd`; termo preferencial nesta fase para o dominio de loop.
- `PostGame`: termo legado/ambíguo quando usado para nomear estado do `GameLoop`; nesta fase ele so vale como dominio downstream de ownership pos-run fora do loop.
- `PostStage`: ponte/estagio de handoff pos-`RunEnd` entre `GameLoop` e `PostGame`; nao e estado do `GameLoop`.
- `Restart`: intent derivada para reiniciar a run/macro.
- `ExitToMenu`: intent derivada para voltar ao menu.

Termos concorrentes sem autoridade nesta fase:
- `PostGame` como rótulo de estado do `GameLoop`
- `PostStage` como substituto de `RunEnd`
- `Exit` como nome de intent canônica

## 2. Inventory Decision Matrix

| Item | Decision | Justificativa curta |
|---|---|---|
| `GameLoopStateMachine` | Keep | E a maquina pura de estados do dominio. |
| `GameLoopService` | Keep with reshape | Estado atual resumido: coordena lifecycle, pause e handoff; ainda carrega leitura de `PostPlay` e sincronizacao com SceneFlow. Forma canonica alvo: owner estrito de estado/lifecycle/telemetry, sem semantica de rota ou UI. Restricao explicita: nao decide `PostGame`, `Navigation` ou audio. Condicao de aceite: manter apenas sinais canonicos e observabilidade de estado, sem ownership paralelo. |
| `GameRunOutcomeService` | Keep | E o owner terminal do fim de run em `Playing`, com idempotencia e publicacao do evento. |
| `GameRunResultSnapshotService` | Keep with reshape | Estado atual resumido: projeta o ultimo resultado observado e hoje ainda convive com uso em overlay. Forma canonica alvo: read-model estrito, sem decisao de UI, sem semantica de pause e sem ownership de post-run. Restricao explicita: nao publica eventos, nao valida run e nao decide visibilidade. Condicao de aceite: somente consulta e reset por nova run. |
| `GameLoopCommands` | Keep with reshape | Estado atual resumido: facade unica de comandos para pause, resume, victory, defeat, restart e exit. Forma canonica alvo: comando primario do loop com intents separadas por responsabilidade. Restricao explicita: nao concentrar UI, navigation ou post-run em uma unica superficie. Condicao de aceite: comandos canonicos continuam emitindo intents, mas sem absorver ownership de overlay ou menu. |
| `GameLoopInputCommandBridge` | Keep with reshape | Estado atual resumido: traduz eventos definitivos para chamadas do loop e faz dedupe por frame. Forma canonica alvo: bridge fina EventBus -> IGameLoopService sem logica de dominio. Restricao explicita: nao usar dedupe como correcao estrutural permanente. Condicao de aceite: apenas encaminhar sinais canonicos, sem semantica propria e sem polling. |
| `GameRunOutcomeRequestBridge` | Keep | Ponte aceitavel de request de fim de run para o owner terminal. |
| `GameRunEndedEventBridge` | Keep with reshape | Estado atual resumido: faz handoff do fim de run para `PostStage` e depois volta ao loop. Forma canonica alvo: ponte transitiva entre `RunEnd` e `PostGame` downstream, sem ownership de UI. Restricao explicita: nao decidir `PostStage`, `PostGame` ou menu. Condicao de aceite: ser removivel quando o handoff canonico estiver absorvido por owner downstream unico. |
| `GamePauseGateBridge` | Keep with reshape | Estado atual resumido: reflete pause canonico no `SimulationGate`. Forma canonica alvo: ponte transitiva de infraestrutura, sem semantica de pause. Restricao explicita: nao decidir estado de pause, apenas refletir. Condicao de aceite: removivel quando o gate consumir pause por contrato direto do owner canonico. |
| `GameLoopSceneFlowSyncCoordinator` | Keep with reshape | Estado atual resumido: sincroniza readiness/start com SceneFlow e reinicializa o loop. Forma canonica alvo: sincronizacao tecnica minima entre SceneFlow e GameLoop. Restricao explicita: nao resolver rota, navegacao ou semantica de estado. Condicao de aceite: manter apenas handoff tecnico de readiness/start sem polling. |
| `MacroRestartCoordinator` | Keep with reshape | Estado atual resumido: executa restart macro a partir de intent canonica. Forma canonica alvo: executor fino de restart, sem decidir fluxo de UI ou menu. Restricao explicita: nao absorver logica de level, overlay ou navigation. Condicao de aceite: permanecer como ponte ate o owner final de restart estar consolidado fora do overlay. |
| `GamePauseOverlayController` | Move | Overlay visual e hotkey/input pertencem a UI, nao ao `GameLoop`. |
| `ExitToMenuCoordinator` | Forbid adapter | Mistura dispatch de navegacao, liberacao de gate e marcacao de resultado de post-game para mascarar fronteira errada. |
| `GamePauseCommandEvent` / `GameResumeRequestedEvent` / `PauseStateChangedEvent` / `PauseWillEnterEvent` / `PauseWillExitEvent` | Keep | Sao sinais canonicos de pause; nao sao ownership de overlay. |
| `GameRunStartedEvent` / `GameRunEndedEvent` / `GameRunEndRequestedEvent` | Keep | Sao sinais canonicos do lifecycle de run. |
| `GameResetRequestedEvent` / `GameExitToMenuRequestedEvent` | Keep | Sao intents canonicas derivadas, nao estados nem rotas. |

## 3. Canonical Runtime Rail

Trilho runtime canônico a ser sustentado por `GameLoop`:

1. SceneFlow conclui readiness do macro-flow.
2. `GameLoopService` entra em `Ready` e aguarda `Start`.
3. `GameLoopStateMachine` transita para `Playing`.
4. `GameRunOutcomeService` publica `GameRunEndedEvent` apenas em `Playing`.
5. `GameLoopService` transita para `PostPlay` apenas como estado terminal de run.
6. `GameRunEndedEventBridge` entrega o handoff para `PostStage` sem assumir ownership do `PostGame`.
7. `GameLoopService` consome o handoff final e nao decide UI.
8. `Pause` altera o estado transversal apenas via sinais canonicos.
9. `Restart` e `ExitToMenu` saem como intents, nao como atalho visual.
10. `ActivityTelemetry` reflete o estado real, sem polling para compensar fronteira fraca.

`RunEnd` e a fronteira de termino; `PostStage` e a ponte transitiva de handoff; `PostPlay` e o estado terminal do `GameLoop`; `PostGame` e o owner downstream do pos-run. Estes termos nao sao intercambiaveis.

## 4. Parallel Rails to Eliminate

- Overlay-driven pause ownership em `GamePauseOverlayController`.
- Hotkey/polling de `Escape` dentro do overlay visual como sustentacao de contrato.
- Dedupe por frame e log spam em `GameLoopInputCommandBridge` para compensar eventos repetidos.
- Sincronizacao paralela de restart em `MacroRestartCoordinator` e `LevelFlow.PostLevelActionsService`.
- Saida para menu via `GameLoop` + `ExitToMenuCoordinator` + `Navigation.GoToMenuAsync`, com marcacao adicional de resultado em `PostGame`.
- Path paralelo de fim de run entre `GameRunOutcomeRequestBridge` e `GameRunEndedEventBridge` quando a semantica de termino mistura request, publicacao e handoff.
- SceneFlow completion sync usado para dirigir o `GameLoop` por fora do owner de estado.
- Logs repetitivos de sincronizacao e polling para sustentar comportamento frágil em vez de contrato claro.

## 5. Phase Scope

### Pertence a esta fase

- `GameLoopService`
- `GameLoopStateMachine`
- `GameRunOutcomeService`
- `GameRunResultSnapshotService`
- `GameLoopCommands`
- `GameLoopInputCommandBridge`
- `GameRunOutcomeRequestBridge`
- `GameRunEndedEventBridge`
- `GamePauseGateBridge`
- `GameLoopSceneFlowSyncCoordinator`
- `MacroRestartCoordinator`
- `ExitToMenuCoordinator`
- `GamePauseOverlayController` como evidencia de ownership visual errada
- eventos e intents de pause, restart, exit e run end

### Exclusoes e fronteiras fechadas nesta fase

- `GamePauseOverlayController` fica classificado como visual-only e nao pode ser reclassificado como owner de pause.
- Input boundary de pause e command dispatch pertencem a `GameLoopCommands` e `GameLoopInputCommandBridge`, nao ao overlay.
- Decisao canonica de pause pertence ao `GameLoopService` via `RequestPause`/`RequestResume`.
- Estado canonico de pause pertence ao `GameLoopService` e sua maquina de estados.
- O overlay esta proibido de concentrar decisao de pause, estado de run, rota, navigation policy, post-run ou audio.

### Nao pertence a esta fase

- ownership de `PostGame`
- ownership de `Navigation`
- redesign de `Frontend/UI` fora da extracao do overlay de pause
- policy de audio ou precedencia BGM
- reestruturacao de `SceneFlow` como dominio principal
- alteracao de assets ou `.cs`

## 6. Explicit Prohibitions

- mover ownership para camada visual
- usar adapter/bridge para esconder fronteira errada
- adicionar fallback silencioso
- corrigir sintoma local sem declarar owner canonico
- usar polling/log spam como sustentacao de contrato frágil
- usar log por frame em caminho estavel
- usar polling para sustentar contrato de pause, run ou end
- usar dedupe em bridge como correção estrutural permanente
- tratar overlay visual como owner de pause
- tratar compile/runtime como suficiente para aceite arquitetural
- manter `ExitToMenuCoordinator` como adaptador de conveniencia entre navegacao, pause e post-game

## 7. Acceptance Gates

- `GameLoopStateMachine` permanece puramente deterministica e nao consulta UI, Navigation ou Audio.
- `GameLoopService` governa somente estado, lifecycle e telemetria; isso deve ser verificavel pela ausencia de chamadas diretas a UI, Navigation ou Audio neste owner.
- `GameRunOutcomeService` publica `GameRunEndedEvent` somente quando `Playing` estiver ativo; isso deve ser verificavel por leitura do contrato e por trace de runtime.
- `GamePauseOverlayController` fica fora da fronteira de ownership de pause; isso deve ser verificavel pela separacao entre visual-only, input boundary e state owner.
- nenhum caminho de restart, exit ou pause depende de fallback silencioso, polling ou dedupe em bridge como solucao estrutural.
- nenhum adapter permanente mascara `GameLoop -> PostGame` ou `GameLoop -> Navigation`; os bridges transitórios devem ter clausula de aposentadoria escrita.
- os bridges restantes sao finos, unidirecionais e removiveis quando o owner canonico downstream absorver a fronteira.
- a evidencia arquitetural mostra owner unico por fronteira e trilho runtime unico, sem trilhos paralelos ativos.

## 8. Evidence Required

Na futura implementacao, a fase deve anexar:

- mapa de ownership por arquivo e por contrato
- trilho runtime validado para start, pause, run end, restart e exit
- evidencia de que overlay visual nao mais carrega ownership de pause
- evidencia de que o input boundary de pause nao depende do overlay para decidir estado
- evidencia de que `ExitToMenuCoordinator` nao atua como adapter permanente
- logs/event trace de `GameRunStartedEvent`, `GameRunEndedEvent`, `PauseStateChangedEvent`, `GameResetRequestedEvent`, `GameExitToMenuRequestedEvent`
- comprovacao de que nao existe polling/log spam para sustentar contrato
- comprovacao de que nao existe dedupe por frame como correção estrutural permanente
- comprovacao de que o handoff para `PostGame` permanece transitório e fora de ownership do `GameLoop`

