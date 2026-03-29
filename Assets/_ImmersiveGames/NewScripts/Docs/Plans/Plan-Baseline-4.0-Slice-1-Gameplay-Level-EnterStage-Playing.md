# Plan - Baseline 4.0 Slice 1

Subordinado a `ADR-0043`, `ADR-0044` e ao [Blueprint-Baseline-4.0-Ideal-Architecture.md](./Blueprint-Baseline-4.0-Ideal-Architecture.md).

Escopo desta fase:
- `NewScripts` only
- `Docs/Plans` only
- sem tocar em `Scripts` legado
- sem implementar ainda
- sem abrir `Slice 2`

## 1. Resumo executivo

Objetivo do Slice 1: provar o trilho canônico de entrada:

`Gameplay -> Level -> EnterStage -> Playing`

O slice usa o estado atual como inventário de reaproveitamento, não como contrato final.

Regra do slice:
- `Gameplay` = Contexto Macro
- `Level` = Contexto Local de Conteúdo
- `EnterStage` = Estágio Local de entrada
- `Playing` = Estado de Fluxo principal

Fora de escopo:
- `ExitStage`
- `RunResult`
- `PostRunMenu`
- `Restart` / `ExitToMenu`
- refatoração ampla de pastas
- troca massiva de nomes

## 2. Backbone do Slice 1

### Ordem runtime alvo

1. Frontend/UI emite o start do gameplay.
2. `Gameplay` aceita a entrada macro.
3. `Level` é selecionado como conteúdo local.
4. `SceneFlow` executa apenas o trilho técnico de transição/readiness.
5. `EnterStage` prepara a entrada local.
6. `EnterStage` conclui.
7. `Playing` entra como estado ativo canônico.

### Owners por módulo

| Módulo | Papel no slice |
|---|---|
| `GameLoop` | owner do estado de fluxo e da entrada em `Playing` |
| `LevelFlow` | owner do conteúdo local e do handoff para `EnterStage` |
| `SceneFlow` | owner do trilho técnico de transição/readiness |
| `Frontend/UI` | emissor de intenção de start, sem ownership de domínio |
| `Navigation` | somente se permanecer como resolução/dispatch técnico já existente |

## 3. Reuse map do estado atual

### Gameplay

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/GameLoop/Core/GameLoopStateMachine.cs` | Keep with reshape | já expressa `Playing` como estado ativo; `PostPlay` é conflito fora deste slice |
| `Modules/GameLoop/Core/GameLoopEvents.cs` | Keep with reshape | `GameStartRequestedEvent`, `GameRunStartedEvent` e `GameRunEndRequestedEvent` sustentam o rail; `PostPlay` não entra no Slice 1 |
| `Modules/GameLoop/Core/GameLoopService.cs` | Keep with reshape | coordena start/lifecycle, mas deve ficar sem semântica pos-run neste slice |
| `Modules/GameLoop/Run/GameRunPlayingStateGuard.cs` | Keep | útil para validar `Playing` sem duplicar regra |
| `Modules/Gameplay/State/GameplayStateGate.cs` | Keep with reshape | já concentra readiness/gate; precisa ficar focado em liberar entrada em `Playing` |
| `Modules/Gameplay/State/GameplayStateGateBindings.cs` | Keep | bom como binding fino de eventos canônicos |
| `Modules/LevelFlow/Runtime/GameplayStartSnapshot.cs` | Keep | payload canônico de entrada, útil para Level/EnterStage |

### Level

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs` | Keep with reshape | é a melhor base para a entrada canônica, mas o nome `StartGameplayDefaultAsync` ainda é bridge |
| `Modules/LevelFlow/Runtime/LevelSelectedEvent.cs` | Keep | bom handoff de seleção do conteúdo local |
| `Modules/LevelFlow/Runtime/RestartContextService.cs` | Keep with reshape | reaproveitável como contexto local, sem puxar pos-run para o slice |
| `Modules/LevelFlow/Runtime/LevelSceneCompositionRequestFactory.cs` | Keep | útil para traduzir nível em plano técnico |
| `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs` | Keep with reshape | candidato natural para preparar a entrada local |
| `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs` + `Modules/LevelFlow/Runtime/LevelFlowBootstrap.cs` | Keep with reshape | continuam como trilho de entrada do conteúdo local, mas sem expandir para Slice 2 |

### EnterStage

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/LevelFlow/Runtime/LevelIntroStageSessionService.cs` | Bridge temporária | faz o papel de materializar a sessão de entrada, mas o nome ainda carrega `IntroStage` |
| `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs` | Bridge temporária | bom ponto para orquestrar a entrada local; precisa convergir para `EnterStage` |
| `Modules/LevelFlow/Runtime/LevelStagePresentationService.cs` | Keep with reshape | útil se continuar estritamente como apresentação da entrada |
| `Modules/LevelFlow/Runtime/LevelIntroStagePresenterHost.cs` | Adapter temporário | apresentação/scope da entrada pode ser reaproveitada, mas o nome é legado |
| `Modules/LevelFlow/Runtime/LevelIntroStagePresenterScopeResolver.cs` | Adapter temporário | resolve presenters do estágio de entrada, mas ainda sob nomenclatura antiga |
| `Modules/LevelFlow/Runtime/LevelIntroStageMockPresenter.cs` | Substituição futura | só faz sentido como QA; não deve virar contrato de produção |

### Playing

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/GameLoop/Core/GameLoopStateMachine.cs` | Keep | `Playing` já é o estado ativo correto |
| `Modules/GameLoop/Core/GameLoopContracts.cs` | Keep | contrato de `IGameLoopService` e guard de `Playing` seguem válidos |
| `Modules/GameLoop/Run/GameRunOutcomeService.cs` | Bridge fora do slice | não entra no Slice 1, mas precisa permanecer isolado para não contaminar a entrada |
| `Modules/GameLoop/Run/GameRunResultSnapshotService.cs` | Bridge fora do slice | também fica fora; serve só como inventário downstream |
| `Modules/Gameplay/State/GameplayStateGate.cs` | Keep | valida liberação para ação em `Playing` |

### Frontend/UI e SceneFlow

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/Frontend/UI/Bindings/MenuPlayButtonBinder.cs` | Bridge temporária | emissor de intenção de start, não owner de domínio |
| `Modules/Frontend/UI/Panels/FrontendPanelsController.cs` | Keep | útil para navegação visual, sem virar contexto de gameplay |
| `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | Keep with reshape | trilho técnico de transição; não deve ganhar semântica de gameplay |
| `Modules/SceneFlow/Transition/Runtime/RouteSceneCompositionRequestFactory.cs` | Keep | bom suporte técnico para macro transition |
| `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs` | Keep | gate técnico para o handoff em `Playing` |

### Conflitos que precisam virar bridge/adaptor/substituição

| Peça | Problema | Destino esperado |
|---|---|---|
| `PostPlay` em `GameLoopStateMachine` | conflita com o backbone novo para o Slice 1 | substituição futura, fora do slice |
| `IntroStage` na família `LevelIntroStage*` | nome não bate com `EnterStage` | bridge temporária até convergência posterior |
| `StartGameplayDefaultAsync` em `LevelFlowRuntimeService` | expressa atalho semântico, não o backbone canônico | bridge temporária |
| `MenuPlayButtonBinder` chamando `LevelFlow` direto | UI está acoplada ao rail de entrada | bridge temporária, sem ownership |
| `SceneTransitionService` com fallback técnico demais | pode mascarar borda errada se absorver semântica de gameplay | manter como suporte técnico, não como owner |

## 4. Hooks/eventos mínimos

Slice 1 precisa, no mínimo, destes hooks canônicos:

| Hook/evento | Papel |
|---|---|
| `GameStartRequestedEvent` | intenção macro de iniciar o fluxo |
| `LevelSelectedEvent` | handoff do conteúdo local selecionado |
| `LevelEnteredEvent` | início do `EnterStage` |
| `LevelIntroCompletedEvent` | fim do `EnterStage` e handoff para `Playing` |
| `GameRunStartedEvent` | marca a entrada em `Playing` |

Payload mínimo esperado:
- `LevelRef`
- `MacroRouteId`
- `LocalContentId`
- `SelectionVersion`
- `Reason`
- `LevelSignature`

Regra:
- não criar novo evento se um existente já cobre o handoff com clareza
- não introduzir eventos de `ExitStage`, `RunResult` ou `PostRunMenu` neste slice

## 5. Sequência de implementação em fases curtas

### Fase 0 - congelar o rail

- travar a terminologia do slice
- mapear `Keep`, `Keep with reshape`, `Bridge temporária`, `Adapter temporário`, `Substituição futura`
- confirmar o caminho runtime alvo e os fora de escopo

### Fase 1 - entrada macro

- alinhar `GameLoop` para expor somente a entrada canônica do fluxo
- manter `Playing` como único estado de fluxo do slice
- evitar qualquer dependência de pos-run

### Fase 2 - conteúdo local

- consolidar `LevelFlow` como owner do conteúdo local
- usar `LevelSelectedEvent` e `GameplayStartSnapshot` como payload canônico
- manter `LevelFlowRuntimeService` como bridge de entrada, sem expandir escopo

### Fase 3 - EnterStage

- reaproveitar a família `LevelIntroStage*` como implementação temporária de `EnterStage`
- alinhar `LevelStageOrchestrator` e `LevelStagePresentationService` ao handoff de entrada
- evitar que `EnterStage` vire contexto visual independente

### Fase 4 - handoff para Playing

- fechar o caminho `EnterStage -> Playing`
- confirmar que `GameRunStartedEvent` é o único marco de entrada no estado ativo
- manter `SceneFlow` apenas como trilho técnico de readiness/transição

### Fase 5 - UI e validação

- conectar o `MenuPlayButtonBinder` ao start canônico sem agregar lógica de domínio
- registrar logs mínimos de cada handoff
- validar o rail completo em runtime

## 6. Critérios de aceite do slice

O Slice 1 só é aceito se:

- o runtime executar `Gameplay -> Level -> EnterStage -> Playing` sem abrir `ExitStage`
- `Playing` for o único estado de fluxo canônico validado neste slice
- `LevelFlow` continuar como owner do conteúdo local, sem absorver pos-run
- `EnterStage` existir como estágio local, ainda que parcialmente coberto por bridges temporárias
- `Frontend/UI` emitir intenção e não ownership
- `SceneFlow` permanecer técnico, sem semântica de gameplay
- os logs mostrarem a sequência mínima de entrada e a ausência de caminhos de `RunResult` / `PostRunMenu`
- nenhuma pasta de legado seja tocada
- nenhuma renomeação massiva seja necessária para provar o slice

