# Plan - Baseline 4.0 Slice 2

Subordinado a `ADR-0043`, `ADR-0044` e ao [Blueprint-Baseline-4.0-Ideal-Architecture.md](./Blueprint-Baseline-4.0-Ideal-Architecture.md).

Escopo desta fase:
- `NewScripts` only
- `Docs/Plans` only
- sem tocar em `Scripts` legado
- sem implementar ainda
- sem abrir `Slice 1` novamente

## 1. Resumo executivo

Objetivo do Slice 2: provar a saida canonica do fluxo:

`Playing -> ExitStage -> RunResult -> PostRunMenu`

O slice usa o estado atual como inventario de reaproveitamento, nao como contrato final.

Regra do slice:
- `Playing` = estado de fluxo ativo
- `ExitStage` = estagio local de saida
- `RunResult` = resultado canonico da run
- `PostRunMenu` = contexto visual local apos o resultado

Fora de escopo:
- refatoracao ampla de pastas
- renomeacao massiva no codigo
- `Save`
- arquitetura futura fora do blueprint

## Estado validado

- Fases 0, 1, 2 e 3 concluídas no estado atual validado.
- Backbone validado: `Playing -> ExitStage -> RunResult -> PostRunMenu`.
- Ordem runtime validada: `GameRunEndedEvent -> ExitStageStarted -> ExitStageCompleted -> RunResultUpdated -> PostRunMenuEntered`.
- Follow-ups abaixo permanecem não bloqueantes.
- Fases 4 e 5 também já ficam cobertas pelo estado validado do slice.

## 2. Backbone do Slice 2

### Nomes canonicos congelados

- `Playing`
- `ExitStage`
- `RunResult`
- `PostRunMenu`

### Nomes temporarios / bridges

- `PostStage`
- `PostPlay`
- `GameRunEndedEventBridge`
- `LevelPostStageMockPresenter`
- `ExitToMenuCoordinator`
- `PostLevelActionsService`
- `PostGameOverlayController`
- `GameRunResultSnapshotService`
- `GameLoopPostGameSnapshotResolver`

### Ordem runtime alvo

1. `GameLoop` permanece em `Playing`.
2. `GameRunEndRequestedEvent` dispara o fim da run.
3. `GameRunOutcomeService` publica `GameRunEndedEvent` somente em `Playing`.
4. `ExitStage` inicia como stage local de saida.
5. `ExitStage` conclui.
6. `RunResult` e consolidado.
7. `PostRunMenu` entra como contexto visual local apos o resultado.

### Owners por modulo

| Módulo | Papel no slice |
|---|---|
| `GameLoop` | owner de `Playing`, lifecycle da run e emissao de fim de run; nao e owner de `RunResult` nem `PostRunMenu` |
| `PostGame` | owner de `ExitStage`, `RunResult` e `PostRunMenu` |
| `Frontend/UI` | camada visual do menu pos-run e emissora de intents downstream |
| `SceneFlow` | trilho tecnico de transicao/readiness apenas |
| `Navigation` | apenas dispatch downstream se houver intent de saida, sem ownership de resultado |
| `LevelFlow` | participante secundario apenas como bridge legada a ser contida, nao como owner do slice |

### Owners validados

- `GameLoop` = `Playing` + fronteira de fim de run
- `PostGame` = `ExitStage` + `RunResult` + `PostRunMenu`
- `Restart` / `ExitToMenu` = intents downstream

## 3. Reuse map do estado atual

### Playing

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/GameLoop/Core/GameLoopStateMachine.cs` | Keep | `Playing` ja e o estado ativo correto |
| `Modules/GameLoop/Core/GameLoopService.cs` | Keep with reshape | governa lifecycle e transicao de estado; nao deve absorver pos-run |
| `Modules/GameLoop/Run/GameRunPlayingStateGuard.cs` | Keep | valida `Playing` sem duplicar regra |
| `Modules/GameLoop/Run/GameRunOutcomeService.cs` | Keep | owner terminal do fim de run em `Playing` |
| `Modules/GameLoop/Core/GameLoopEvents.cs` | Keep with reshape | `GameRunEndRequestedEvent`, `GameRunEndedEvent`, `GameRunStartedEvent` sustentam o rail |

### ExitStage

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/PostGame/PostStageCoordinator.cs` | Bridge temporária | faz a ponte transitiva do handoff pos-run |
| `Modules/PostGame/PostStageControlService.cs` | Keep with reshape | controla inicio/conclusao do stage transitivo |
| `Modules/PostGame/PostStagePresenterRegistry.cs` | Keep with reshape | adota presenter do stage sem virar roteador de UI |
| `Modules/PostGame/PostStagePresenterScopeResolver.cs` | Keep | resolve candidatos do stage transitivo |
| `Modules/GameLoop/Run/GameRunEndedEventBridge.cs` | Bridge temporária | Handoff atual entre fim de run e stage transitivo |
| `Modules/PostGame/Bindings/LevelPostStageMockPresenter.cs` | Substituição futura | QA/prototipo; nao deve virar contrato de producao |

### RunResult

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/PostGame/PostGameResultService.cs` | Keep with reshape | projeção canonica do resultado para o pos-run |
| `Modules/PostGame/PostGameResultContracts.cs` | Keep | contrato canonico do resultado |
| `Modules/GameLoop/Run/GameRunResultSnapshotService.cs` | Replace | snapshot paralelo no `GameLoop`; deve deixar de ser fonte principal do resultado |
| `Modules/GameLoop/Flow/GameLoopPostGameSnapshotResolver.cs` | Bridge temporária | apoio historico para leitura de snapshot/result no loop |
| `Modules/GameLoop/Run/GameRunOutcomeService.cs` | Keep | continua como owner terminal da publicacao do fim de run |

### PostRunMenu

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/PostGame/Bindings/PostGameOverlayController.cs` | Move | camada visual local do pos-run; nao pode carregar ownership do resultado |
| `Modules/Frontend/UI/Panels/FrontendPanelsController.cs` | Keep | contexto visual local e controle de tela |
| `Modules/Frontend/UI/Bindings/MenuQuitButtonBinder.cs` | Bridge temporária | emissor de intent de saida, nao owner de fluxo |
| `Modules/Frontend/UI/Bindings/MenuPlayButtonBinder.cs` | Keep with reshape | útil para reiniciar via UI, mas fora do rail de saida canonico |
| `Modules/GameLoop/Interop/ExitToMenuCoordinator.cs` | Forbid adapter | mistura navigation, gate e resultado; não deve consolidar o slice |

### Conflitos que precisam virar bridge/adaptor/substituicao

| Peça | Problema | Destino esperado |
|---|---|---|
| `PostPlay` em `GameLoopStateMachine` | termo legado/ambíguo conflita com a saida canônica | bridge temporária, não contrato final |
| `GameRunEndedEventBridge` | mistura fim de run com stage transitivo | bridge temporária até o owner downstream absorver o handoff |
| `GameRunResultSnapshotService` | snapshot paralelo de resultado no loop | substituição futura por `PostGameResultService` |
| `ExitToMenuCoordinator` | mistura dispatch, gate e marcacao de resultado | forbido como adaptador permanente |
| `PostGameOverlayController` | pode carregar gate/result/visual ao mesmo tempo | mover para UI visual-only |

## 4. Hooks/eventos mínimos

Slice 2 precisa, no mínimo, destes hooks canônicos:

| Hook/evento | Papel |
|---|---|
| `GameRunEndRequestedEvent` | intenção de término da run |
| `GameRunEndedEvent` | fronteira canônica de término da run |
| `PostStageStartRequestedEvent` | bridge de entrada do `ExitStage` transitivo |
| `PostStageStartedEvent` | inicio observado do `ExitStage` |
| `PostStageCompletedEvent` | conclusão observada do `ExitStage` |
| `PostGameEnteredEvent` | entrada no owner de pos-run |
| `PostGameExitedEvent` | saída do pos-run |
| `PostRunMenuShownEvent` | entrada observável do contexto visual pós-resultado, se mantido |

Payload mínimo esperado:
- `Outcome`
- `Reason`
- `Frame`
- `Signature`
- `SceneName`
- `LevelRef`, somente se for necessário para correlação de leitura, nao para ownership

Regra:
- não criar novo evento se um existente já cobre o handoff com clareza
- não introduzir eventos de `Save`
- não reabrir o rail de entrada do Slice 1

## 5. Sequência de implementação em fases curtas

### Fase 0 - congelar o rail

- travar os nomes canônicos: `Playing`, `ExitStage`, `RunResult`, `PostRunMenu`
- declarar os hooks/peças atuais que são temporários/bridges
- deixar explícito que `GameLoop` só governa `Playing` e a fronteira de fim de run
- deixar explícito que `RunResult` pertence ao `PostGame` e não ao `GameLoop`
- deixar explícito que `PostRunMenu` é contexto visual downstream, não owner de resultado
- deixar explícito que `ExitToMenuCoordinator` e UI só podem atuar downstream
- deixar explícito que `SceneFlow` permanece técnico e não interpreta `RunResult`
- confirmar o caminho runtime alvo e os fora de escopo
- fixar o log mínimo esperado do slice abaixo

### Log alvo mínimo

O slice deve conseguir registrar, no mínimo, a sequência conceitual abaixo:

`Playing -> ExitStage -> RunResult -> PostRunMenu`

Leitura prática operacional:
- `Playing` permanece ativo até o pedido de fim e continua sendo o único estado de fluxo validado
- `ExitStage` inicia quando `GameRunEndedEvent` ou `PostStageStartRequestedEvent` forem observados
- `RunResult` fica explícito quando `PostGameResultService` publica ou consolida a leitura final
- `PostRunMenu` fica explícito quando o overlay/presenter visual downstream adota o resultado
- `GameLoop` não deve exibir logs de `RunResult` ou `PostRunMenu` como ownership próprio

Regra de validação:
- se o log não mostrar essa sequência, o slice ainda não está congelado
- se o log mostrar `Level` ou reentrada de `EnterStage`, o slice cruzou a fronteira
- se o log mostrar `RunResult` antes de `ExitStage`, a ordem está errada
- se o log mostrar `PostRunMenu` antes da consolidação do resultado, a fronteira visual está errada
- `ExitToMenuCoordinator` e UI podem emitir intents downstream, mas nunca virar owner de resultado ou stage

### Fase 1 - término da run

- manter `GameLoop` como owner de `Playing`
- alinhar a publicação de `GameRunEndedEvent` ao estado ativo canônico
- não permitir que `GameLoop` publique ou console `RunResult`
- evitar qualquer contaminação de menu ou visual no owner de run

### Fase 2 - ExitStage

- consolidar `PostStage` como bridge temporária de `ExitStage`
- manter `PostStageCoordinator` e `PostStageControlService` como handoff transitivo
- registrar início e conclusão do stage de saida de forma observável

### Fase 3 - RunResult

- concentrar a projeção de resultado em `PostGameResultService`
- reduzir ou aposentar o snapshot paralelo no `GameLoop`
- manter `RunResult` como resultado consolidado, não como estado de fluxo
- garantir que qualquer leitura de resultado no loop seja tratada como bridge temporária, não ownership

### Fase 4 - PostRunMenu

- alinhar `PostGameOverlayController` à camada visual
- manter `PostRunMenu` como contexto visual local após o resultado
- evitar que UI recupere ownership de run, stage ou resultado
- manter `ExitToMenuCoordinator` apenas como downstream bridge até a saída ser absorvida pelo owner correto

### Fase 5 - validação do rail

- conectar logs de `Playing`, `ExitStage`, `RunResult` e `PostRunMenu`
- validar que `GameLoop` não absorveu ownership de menu ou resultado
- validar que `Navigation` permaneceu apenas downstream

## 6. Critérios de aceite do slice

O Slice 2 só é aceito se:

- o runtime executar `Playing -> ExitStage -> RunResult -> PostRunMenu` sem reabrir `Level` / `EnterStage`
- `Playing` continuar sendo o único estado de fluxo canônico validado
- `PostGame` concentrar o ownership do pos-run e do resultado
- `GameLoop` não carregar ownership de `RunResult` ou `PostRunMenu`
- `SceneFlow` permanecer técnico, sem semântica de resultado ou menu
- `Frontend/UI` emitir intents e não ownership
- os logs mostrarem a sequência mínima de saida e a ausência de caminhos de entrada
- nenhuma pasta de legado seja tocada
- nenhuma renomeação massiva seja necessária para provar o slice
## ObservaÃ§Ãµes nÃ£o bloqueantes / follow-ups

- `GameRunResultSnapshotService` ainda usa tag/log semÃ¢ntico `[OBS][ExitStage]`, embora hoje observe `PostGameResultUpdatedEvent`. Isso Ã© ruÃ­do de naming/log, nao blocker de comportamento.
- ainda existem resquÃ­cios internos de naming `PostPlay` em alguns logs/estados/sync. Isso pode confundir a consolidacao futura de naming, mas nao bloqueia a fase atual.
