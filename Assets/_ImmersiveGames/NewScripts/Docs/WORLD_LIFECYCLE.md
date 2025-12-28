# World Lifecycle — Reset determinístico por escopos (NewScripts)

Este documento descreve a semântica operacional do **reset determinístico do mundo** no NewScripts, incluindo integração com SceneFlow e o comportamento atual de SKIP em startup/menu.

> Nota de consolidação: versões anteriores mais extensas podem existir no histórico do repositório, mas o conteúdo canônico e mantido é este arquivo.

## Objetivo
Garantir que o “mundo” possa ser reinicializado de forma previsível, para:
- transições de cena,
- reinício de partida,
- retorno ao menu,
- e testes determinísticos.

## Explicação simples
Pense no reset como a “faxina + reconstrução” do mundo. Durante o loading real, o jogo:
1) carrega as cenas necessárias,
2) limpa o que precisa ser limpo,
3) respawna/prepara o que precisa existir.

Esse loading **só termina** quando o reset finaliza e o `WorldLifecycleResetCompletedEvent`
é emitido. Por isso, o `FadeOut` só deve acontecer **depois** do reset — o jogo só está pronto
após o **ResetCompleted**.

## Conceitos
- **Escopo (scope):** unidade lógica do reset (ex.: Players, Enemies, WorldUI).
- **Participante (participant):** registra-se para executar reset em um escopo específico.
- **Hook:** callbacks ordenados (OnBefore/OnAfter Despawn/Spawn) para inspeção e integrações.

## Fases de reset (ordem determinística)
A execução pode variar por implementação, mas o contrato do NewScripts é:

1. **Acquire gate** (bloquear simulação/gameplay)
2. **OnBeforeDespawn hooks**
3. **Despawn / limpeza de entidades do escopo**
4. **OnAfterDespawn hooks**
5. **OnBeforeSpawn hooks**
6. **Spawn / reconstrução de entidades do escopo**
7. **OnAfterSpawn hooks**
8. **Release gate** (liberar simulação/gameplay)

A ordem dos escopos deve ser estável e explícita (ex.: World → Players → NPCs), evitando dependências cíclicas.

## Integração com Scene Flow
Durante transições de cena, o reset é coordenado por eventos:

- `SceneTransitionStarted`:
    - `GameReadinessService` adquire token do `ISimulationGateService` (ex.: `flow.scene_transition`)
    - jogo fica “NOT READY” durante load/unload

- `SceneTransitionScenesReadyEvent`:
    - `WorldLifecycleRuntimeCoordinator` é acionado
    - decide executar reset ou SKIP

- `SceneTransitionCompleted`:
    - `GameReadinessService` libera token
    - jogo pode voltar a “READY” (sujeito ao estado do GameLoop e outras condições)

### SKIP (startup/menu)
Para estabilizar o pipeline sem contaminar testes com Gameplay, o driver faz SKIP quando:
- `profile == 'startup'` **ou**
- `activeScene == 'MenuScene'`

Mesmo no SKIP, o driver deve emitir:
- `WorldLifecycleResetCompletedEvent(contextSignature, reason)`

Isso é necessário porque o `GameLoopSceneFlowCoordinator` aguarda esse sinal para chamar `GameLoop.RequestStart()`.

## Quando remover o SKIP e por quê
O SKIP continua necessário enquanto o `MenuScene` não possui mundo a ser resetado e o boot precisa
de um pipeline simples.

**Decisão atual (usuário):** remover o SKIP apenas quando a `GameplayScene` estiver pronta para
concluir **reset + spawn/preparação** antes do `FadeOut`. Remover antes muda o comportamento do
pipeline e pode introduzir reset/spawn em cenas que não deveriam ter mundo.

### Completion gate (SceneFlow)
O `WorldLifecycleResetCompletionGate` bloqueia o final da transição (antes do `FadeOut`) até que:
- `WorldLifecycleRuntimeCoordinator` emita `WorldLifecycleResetCompletedEvent(signature, reason)`.

Esse gate garante que o `SceneTransitionService` só siga para `FadeOut`/`Completed` quando o reset terminou (ou SKIP foi emitido).

## Participantes e registros de cena
### Gameplay Reset por grupos (cleanup/restore/rebind)

Além do **reset por escopos** do WorldLifecycle (`ResetScope` + `IResetScopeParticipant`), existe um módulo de reset **de gameplay** em `Gameplay/Reset/` para validar e executar resets por **alvos** (targets) com fases fixas:

- **Alvos (`GameplayResetTarget`)**: `AllActorsInScene`, `PlayersOnly`, `EaterOnly`, `ActorIdSet`.
- **Fases (`GameplayResetPhase`)**: `Cleanup`, `Restore`, `Rebind`.
- **Participantes**: componentes de gameplay implementam `IGameplayResettable` (e opcionais `IGameplayResetOrder` / `IGameplayResetTargetFilter`).

Integração:
- Participantes de escopo do WorldLifecycle podem atuar como **bridge** para o gameplay. Ex.: `PlayersResetParticipant` (gameplay) implementa `IResetScopeParticipant` (escopo `Players`) e, ao executar, solicita `IGameplayResetOrchestrator.RequestResetAsync(...)` para `PlayersOnly`.
- `IGameplayResetOrchestrator` e `IGameplayResetTargetClassifier` são serviços **por cena** (registrados no `NewSceneBootstrapper`) para manter o reset local ao escopo additive correto.

QA:
- Quando o spawn ainda não é a fonte de verdade, o `GameplayResetQaSpawner` cria alvos de teste e o `GameplayResetQaProbe` valida via log a execução das três fases.

Em uma cena NewScripts típica, o `NewSceneBootstrapper` cria e registra:
- `INewSceneScopeMarker`
- `IWorldSpawnContext`
- `IActorRegistry`
- `IWorldSpawnServiceRegistry`
- `WorldLifecycleHookRegistry`
- `IResetScopeParticipant` (ex.: PlayersResetParticipant)

Quando `WorldDefinition` está ausente (ex.: `MenuScene`), é válido ter:
- zero spawn services registrados,
- mas manter registries e hooks para consistência do pipeline.

## Operação em bootstrap
`WorldLifecycleController` pode existir na cena de bootstrap para debug e/ou integração futura, mas:
- quando `AutoInitializeOnStart` está desabilitado, ele não executa reset automaticamente,
- e aguarda um acionamento externo (ex.: pipeline de gameplay).

## Critérios mínimos de “saúde” (logs)
Ao validar o pipeline, busque no log:
- `SceneTransitionScenesReady recebido`
- `Reset SKIPPED (profile/menu)` **ou** reset executado
- `Emitting WorldLifecycleResetCompletedEvent`
- `SceneTransitionCompleted → gate liberado`

Se o Coordinator não “destrava”, quase sempre faltou:
- `WorldLifecycleResetCompletedEvent` ou
- assinatura `contextSignature` incompatível com a esperada.

## Fluxo de produção (Menu → Gameplay → Pause → Resume → ExitToMenu → Menu)
1. `MenuPlayButtonBinder` → `IGameNavigationService.RequestToGameplay(reason)`.
2. `SceneTransitionService`:
    - `Started` → `FadeIn`
    - Load/Unload → `ScenesReady`
    - completion gate (`WorldLifecycleResetCompletionGate`)
    - `FadeOut` → `Completed`
3. `WorldLifecycleRuntimeCoordinator`:
    - **Gameplay**: executa reset após `ScenesReady` e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
    - **Startup/Frontend**: SKIP com reason `Skipped_StartupOrFrontend`.
4. `GameReadinessService`:
    - token `flow.scene_transition` no `Started`
    - libera no `Completed`
5. PauseOverlay:
    - `GamePauseCommandEvent` / `GameResumeRequestedEvent` / `GameExitToMenuRequestedEvent`
    - `SimulationGateTokens.Pause` controla pausa sem congelar física.

## Evidências (log)
- Startup profile `startup` com reset SKIPPED + `WorldLifecycleResetCompletedEvent(reason=Skipped_StartupOrFrontend)`.
- Transição para profile `gameplay` executa reset após `ScenesReady` e antes do gate liberar.
- Completion gate aguarda `WorldLifecycleResetCompletedEvent` para prosseguir ao `FadeOut`.
- `GameReadinessService` usa token `flow.scene_transition` durante a transição.
- `PauseOverlay` publica `GamePauseCommandEvent`, `GameResumeRequestedEvent`, `GameExitToMenuRequestedEvent`
  e o gate mostra `state.pause`.
