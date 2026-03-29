# Plan - Baseline 4.0 Slice 5

Subordinado a `ADR-0043`, `ADR-0044` e ao [Blueprint-Baseline-4.0-Ideal-Architecture.md](./Blueprint-Baseline-4.0-Ideal-Architecture.md).

Escopo desta fase:
- `NewScripts` only
- `Docs/Plans` only
- sem tocar em `Scripts` legado
- sem implementar ainda
- sem reabrir `Slice 1`, `Slice 2`, `Slice 3` ou `Slice 4`
- `Save` fora de escopo

## 1. Resumo executivo

Objetivo do Slice 5: consolidar `SceneFlow` como trilho tecnico unico de transicao, loading, fade e readiness, sem ownership de gameplay, post-run ou audio.

O slice parte do runtime ja validado e trata o codigo atual como inventario de reaproveitamento, nao como contrato final.

Foco operacional do slice:

`Navigation primary dispatch -> SceneFlow technical rail -> Loading / Fade / Readiness`

Regra do slice:
- `SceneFlow` = trilho tecnico de transicao/readiness/loading/fade
- `Navigation` = dispatch primario upstream
- `GameLoop` = fonte upstream de run/pause state
- `Audio` = consumidor downstream de contexto, nao owner de transicao
- `Frontend/UI` = emissor de intents, nao owner do rail

Fora de escopo:
- `Gameplay` semantico
- `PostRunMenu`
- `BGM` contextual validado no Slice 4
- ducking de pause validado no Slice 4
- semantica de entidade validada no Slice 4
- `Save`
- refatoracao ampla de pastas
- troca massiva de nomes

## 2. Backbone do Slice 5

### Nomes canonicos congelados

- `SceneFlow`
- `Transition`
- `Loading`
- `Fade`
- `Readiness`
- nomes canônicos nao podem ser substituidos por aliases de gameplay, post-run ou audio

### Nomes temporarios / bridges

- `SceneFlowInputModeBridge`
- `GameLoopSceneFlowSyncCoordinator`
- `SceneFlowWorldResetDriver`
- `LoadingHudOrchestrator`
- `LoadingProgressOrchestrator`
- `SceneFlowSignatureCache`

### Runtime rail canonico

1. `Navigation` ou o consumidor upstream resolve a rota e dispara a transicao.
2. `SceneFlow` inicia a transicao tecnica.
3. `Loading` e `Fade` acompanham a janela de transicao.
4. `Readiness` publica o estado tecnico de disponibilidade.
5. `SceneFlow` conclui a transicao.
6. `GameLoop`, `Audio` e `Frontend/UI` consomem o contexto, sem assumir ownership do rail.

### Parallel rails to eliminate

- loading HUD duplicado fora de `SceneFlow`
- progress/loading observers que parecam donos do fluxo
- sincronizacao de input mode com semantica de gameplay
- sync bridge que pareca ownership de `GameLoop`
- readiness mascarado por bridges temporarias
- qualquer segundo rail de loading fora de `SceneFlow`
- qualquer semantica de rota ou gameplay dentro de `SceneFlow`

### Owners por modulo

| Modulo | Papel no slice |
|---|---|
| `SceneFlow` | owner do trilho tecnico de transicao/readiness/loading/fade |
| `Navigation` | owner do dispatch primario upstream |
| `GameLoop` | fonte upstream de run/pause state |
| `Audio` | consumidor downstream de contexto de transicao |
| `Frontend/UI` | emissor de intents e camada visual local |

## 3. Reuse map

| Peca atual | Decisao | Observacao |
|---|---|---|
| `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | Keep with reshape | trilho tecnico central da transicao |
| `Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs` | Keep | lifecycle tecnico canonicamente observavel |
| `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs` | Keep with reshape | readiness tecnica e gate de disponibilidade |
| `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs` | Keep with reshape | apresentacao de loading sob SceneFlow |
| `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs` | Keep with reshape | orquestracao de janela de loading |
| `Modules/SceneFlow/Loading/Runtime/LoadingProgressOrchestrator.cs` | Keep with reshape | progresso tecnico sem ownership de dominio |
| `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs` | Keep | cache tecnico de assinatura de transicao |
| `Modules/SceneFlow/Interop/SceneFlowInputModeBridge.cs` | Bridge temporaria | sincroniza input mode a partir de transicao tecnica |
| `Modules/GameLoop/Interop/GameLoopSceneFlowSyncCoordinator.cs` | Bridge temporaria | handoff tecnico entre SceneFlow e GameLoop |
| `Modules/ResetInterop/Runtime/SceneFlowWorldResetDriver.cs` | Bridge temporaria | se ainda necessario para reset macro, sem ownership de SceneFlow |
| `Modules/SceneFlow/Navigation/Runtime/SceneTransitionPayload.cs` | Keep | payload tecnico de composicao/dispatch |
| `Modules/SceneFlow/Transition/Bindings/SceneTransitionProfile.cs` | Keep | configuracao de fade/loading do rail tecnico |
| `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs` | Keep | entrada de rota para o trilho tecnico |
| `Modules/SceneFlow/Navigation/Bindings/TransitionStyleAsset.cs` | Keep | estilo tecnico da transicao |

## 4. Hooks/eventos minimos

Slice 5 precisa, no minimo, destes hooks canonicos:

| Hook/evento | Papel |
|---|---|
| `SceneTransitionStartedEvent` | inicio da transicao tecnica |
| `SceneTransitionFadeInCompletedEvent` | fim da entrada visual tecnica |
| `SceneTransitionScenesReadyEvent` | signal tecnico de cenas prontas |
| `SceneTransitionBeforeFadeOutEvent` | checkpoint tecnico antes de sair |
| `SceneTransitionCompletedEvent` | fim da transicao tecnica |
| `ReadinessChangedEvent` | snapshot observavel de disponibilidade |

Regra:
- nao criar novo evento se o lifecycle atual ja cobre o rail
- nao introduzir semantica de gameplay em `SceneFlow`
- nao usar fallback silencioso para mascarar readiness fraco

## 5. Sequencia de implementacao em fases curtas

### Fase 0 - congelar o rail

- declarar `SceneFlow`, `Transition`, `Loading`, `Fade` e `Readiness` como nomes canonicos
- marcar `SceneFlowInputModeBridge`, `GameLoopSceneFlowSyncCoordinator` e `SceneFlowWorldResetDriver` como bridges temporarias
- marcar `LoadingHudOrchestrator`, `LoadingProgressOrchestrator` e `SceneFlowSignatureCache` como bridges temporarias
- deixar explicito que `SceneFlow` e tecnico e nao interpreta gameplay, post-run, audio ou intencao de navegacao

### Fase 1 - lifecycle tecnico unico

- alinhar os eventos `SceneTransitionStartedEvent`, `SceneTransitionFadeInCompletedEvent`, `SceneTransitionScenesReadyEvent`, `SceneTransitionBeforeFadeOutEvent` e `SceneTransitionCompletedEvent` como lifecycle unico
- garantir que a assinatura de transicao seja o identificador tecnico primario
- evitar que loading/progress criem trilho paralelo
- manter lifecycle tecnico sem semantica de gameplay

### Fase 2 - readiness e loading

- consolidar `GameReadinessService` como publicador de readiness tecnico
- manter `LoadingHudService` e orchestrators como consumidores de janela tecnica
- evitar duplicacao de loading HUD entre consumidores
- Fase 2 so e considerada fechada apos o saneamento final de observabilidade de `LoadingProgressOrchestrator` e `GameReadinessService`
- o fechamento desta fase nao altera o lifecycle tecnico nem introduz trilho paralelo
- depois deste saneamento, o proximo passo segue sendo a Fase 3

### Fase 3 - bridges temporarias

Fechada nesta rodada:
- `SceneFlowInputModeBridge` permaneceu OK como adapter tecnico
- `SceneFlowWorldResetDriver` permaneceu OK como handoff tecnico
- `GameLoopSceneFlowSyncCoordinator` foi endurecido com fail-fast para `startPlan` obrigatorio

- manter `SceneFlowInputModeBridge` como adapter de input mode, sem ownership de estado
- manter `GameLoopSceneFlowSyncCoordinator` como sync tecnico, sem ownership de rota
- manter `SceneFlowWorldResetDriver` apenas se ainda for necessario ao handoff tecnico

### Fase 4 - validacao runtime

- validar o rail com as rotas ja existentes, sem abrir novas frentes
- confirmar que `Navigation` continua sendo dispatch primario
- confirmar que `GameLoop`, `Audio` e `Frontend/UI` apenas consomem contexto

## 6. Criterios de aceite

O Slice 5 so e aceito se:

- `SceneFlow` permanecer estritamente tecnico
- `Navigation` continuar como dispatch primario upstream
- `GameLoop` continuar como fonte upstream de run/pause state
- `Audio` nao ganhar ownership de transicao
- `Frontend/UI` nao ganhar ownership de readiness ou loading
- o lifecycle tecnico de transicao estiver completo e observavel em logs
- nao houver trilhos paralelos de loading ou input mode mascarando o owner canonico
- nao houver readiness mascarado por bridges temporarias
- os slices 1, 2, 3 e 4 continuarem fechados sem reabertura

## 7. Pendencias herdadas / nao bloqueantes

- `ExitToMenu` do Slice 3 continua como contexto herdado e anotado, sem bloqueio.
- O fechamento documental do Slice 4 permanece valido e nao e reaberto neste plano.
- Qualquer ajuste fino restante em bridges temporarias continua como follow-up nao bloqueante, nao como nova fronteira canonica.
- Residuo de Fase 2 resolvido por saneamento final; nao ha follow-up bloqueante novo neste corte.
- `Save` nao entra neste corte.
