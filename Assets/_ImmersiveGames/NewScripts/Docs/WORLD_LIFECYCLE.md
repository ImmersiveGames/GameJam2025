# World Lifecycle (NewScripts)

> Este documento implementa operacionalmente as decisões descritas no **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas**.

## Atualização (2025-12-31)

- **Sem flash** confirmado: `LoadingHudScene` é exibida somente após `FadeInCompleted` e é ocultada antes do `FadeOut` (`phase='BeforeFadeOut'`), com `Hide(phase='Completed')` como segurança.
- Ordem operacional validada: **FadeIn → LoadingHUD Show → Load/Unload → ScenesReady → WorldLifecycle Reset (ou Skip) → completion gate → LoadingHUD Hide → FadeOut → Completed**.
- Evidência: logs de produção (startup → Menu → Gameplay), com `GameReadinessService` mantendo o `SimulationGate` fechado durante transição/reset e liberando no `SceneTransitionCompletedEvent`.

## Atualização (2025-12-30)

- Fluxo de **produção** validado end-to-end: Startup → Menu → Gameplay via SceneFlow + Fade + LoadingHUD + Navigation.
- `WorldLifecycleRuntimeCoordinator` reage a `SceneTransitionScenesReadyEvent`:
    - Profile `startup`/frontend: reset **skip** + emite `WorldLifecycleResetCompletedEvent(contextSignature, reason)`.
    - Profile `gameplay`: dispara **hard reset** (`ResetWorldAsync`) e emite `WorldLifecycleResetCompletedEvent(contextSignature, reason)` ao concluir.
- `SceneTransitionService` aguarda o `WorldLifecycleResetCompletedEvent` (via `ISceneTransitionCompletionGate`) antes do `FadeOut`.
    - A chave é o `contextSignature` do `SceneTransitionContext` (derivado de `SceneTransitionContext.ToString()`).
- Hard reset em Gameplay confirma spawn via `WorldDefinition` (Player/Eater) e execução do orchestrator com gate (`WorldLifecycle.WorldReset`).
- `IStateDependentService` bloqueia input/movimento enquanto `SimulationGate` está fechado e/ou `gameplayReady=false`; libera ao final. Pausa também fecha gate via `GamePauseGateBridge`.

```log
[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] SceneTransitionScenesReady recebido. Context=SceneTransitionContext(Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], TargetActive='MenuScene', UseFade=True, Profile='startup')
[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset SKIPPED (startup/frontend). profile='startup', activeScene='MenuScene'.
[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='SceneTransitionContext(Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], TargetActive='MenuScene', UseFade=True, Profile='startup')', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.
```
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
    - no profile `gameplay`, executa reset após o `ScenesReady` e finaliza com
      `WorldLifecycleResetCompletedEvent(reason='ScenesReady/<TargetActiveScene>')`

- `SceneTransitionCompleted`:
    - `GameReadinessService` libera token
    - jogo pode voltar a “READY” (sujeito ao estado do GameLoop e outras condições)


### Ordem operacional de Fade e Loading HUD (evidência de log)
Durante `SceneTransitionService.TransitionAsync(...)`, a ordem observada é:

1. `SceneTransitionStartedEvent`
2. `FadeIn` (alpha=1 / hide) → `FadeInCompletedEvent`
3. `SceneFlowLoadingService` chama `LoadingHud.Show()` **após** `FadeInCompleted`
4. Load/Unload/Active → `SceneTransitionScenesReadyEvent`
5. `WorldLifecycleRuntimeCoordinator` executa reset (profile `gameplay`) ou emite SKIP (profiles `startup`/`frontend`)
6. `WorldLifecycleResetCompletionGate` libera a transição **apenas após** `WorldLifecycleResetCompletedEvent`
7. `BeforeFadeOutEvent`: `SceneFlowLoadingService` chama `LoadingHud.Hide()` → `FadeOut` (alpha=0 / reveal)
8. `SceneTransitionCompletedEvent` (inclui safety `LoadingHud.Hide()` no final)


### Fluxo de produção integrado (SceneFlow + WorldLifecycle)
- **Perfis startup/frontend** (ex.: transição terminando em `MenuScene`):
    - `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent`, mas **não** dispara hard reset.
    - Emite `WorldLifecycleResetCompletedEvent(reason='Skipped_StartupOrFrontend')` para destravar o pipeline.
    - O `GameLoopSceneFlowCoordinator` considera o reset como concluído (skip) e o GameLoop permanece em **Ready**.

```log
[WorldLifecycleRuntimeCoordinator] SceneTransitionScenesReady recebido. Context=... Profile='startup'
[WorldLifecycleRuntimeCoordinator] Reset SKIPPED (startup/frontend). Emitting WorldLifecycleResetCompletedEvent...
```

- **Perfis gameplay** (ex.: transição para `GameplayScene`):
    - `SceneTransitionScenesReadyEvent` aciona `WorldLifecycleController.ResetWorldAsync(...)`.
    - O `WorldLifecycleOrchestrator` executa as fases determinísticas antes de liberar o gate de conclusão da transição.

#### Gate e readiness
- Durante o reset, o `WorldLifecycleOrchestrator` adquire o token
  `WorldLifecycleTokens.WorldResetToken` no `ISimulationGateService`
  (ex.: gate `WorldLifecycle.WorldReset`), bloqueando simulação até o final do reset.
- O `GameReadinessService` só marca `gameplayReady=True` após:
    - `WorldLifecycleResetCompletedEvent` (reset ou skip), e
    - `SceneTransitionCompletedEvent` (final da transição).

### Integração com GameLoop (pós-game)
- O **WorldLifecycle** continua responsável por resetar/despawnar/spawnar atores.
- O **GameLoop** coordena o estado macro da run (ex.: `Playing` / `PostPlay`) e publica:
    - `GameRunStartedEvent` / `GameRunEndedEvent` (resultado da run),
    - `GameLoopActivityChangedEvent` (telemetria de atividade).
- UI e sistemas em cenas globais/gameplay podem consultar `IGameRunStatusService`
  para exibir o resultado da última run sem depender diretamente de gameplay específico.
- **Restart pós-game** usa o fluxo oficial:
    - `GameResetRequestedEvent` → `RestartNavigationBridge` → `IGameNavigationService.RequestGameplayAsync(...)`
    - `SceneTransitionScenesReadyEvent` (profile gameplay) → `WorldLifecycleRuntimeCoordinator` → reset determinístico.

### SKIP (startup/menu)
Para estabilizar o pipeline sem contaminar testes com Gameplay, o driver faz SKIP quando:
- `profile == 'startup'` **ou**
- `activeScene == 'MenuScene'`

Mesmo no SKIP, o driver deve emitir:
- `WorldLifecycleResetCompletedEvent(contextSignature, reason)`

### Contrato de `contextSignature` e `reason` (WorldLifecycleResetCompletedEvent)

O evento `WorldLifecycleResetCompletedEvent(string contextSignature, string reason)` é a **confirmação oficial** (incluindo *skip*) usada por:

- `GameLoopSceneFlowCoordinator` (sincronização do GameLoop após ScenesReady)
- `WorldLifecycleResetCompletionGate` / `ISceneTransitionCompletionGate` (liberação do `FadeOut` no SceneFlow)

#### `contextSignature`

- Deve ser **exatamente** `SceneTransitionContext.ToString()` do *mesmo* `SceneTransitionContext` emitido pelo SceneFlow.
- O objetivo é que **Started / ScenesReady / Completed** e todos os consumidores comparem a **mesma string**.
- Observação: alguns logs internos exibem o campo como `signature='...'`; neste documento, esse valor é o `contextSignature`.

#### `reason`

- String curta, **machine-readable**, sem localização; preferir `CamelCase`/`PascalCase` + separadores previsíveis.
- Formatos operacionais já validados:
    - Reset em gameplay: `ScenesReady/<ActiveScene>` (ex.: `ScenesReady/GameplayScene`)
    - Skip em startup/frontend: `Skipped_StartupOrFrontend:profile=<profile>;scene=<activeScene>`
- Extensões devem ser feitas por *key-values* com `;` (ex.: `...;extraKey=extraValue`) para facilitar parsing em logs e QA.

#### Regras de consistência

- `WorldLifecycleRuntimeCoordinator` é o **único publisher** do evento em runtime de produção.
- `WorldLifecycleResetCompletionGate` deve cachear/confirmar por **contextSignature** (não por referência de struct).
### Completion gate (SceneFlow)
O `WorldLifecycleResetCompletionGate` bloqueia o final da transição (antes do `FadeOut`) até que:
- `WorldLifecycleRuntimeCoordinator` emita `WorldLifecycleResetCompletedEvent(signature, reason)`.

Esse gate garante que o `SceneTransitionService` só siga para `FadeOut`/`Completed` quando o reset terminou (ou SKIP foi emitido).

## Participantes e registros de cena
### GameplayScene (produção)
- Escopo atual de produção inclui dois atores principais:
    - `ActorKind.Player`
    - `ActorKind.Eater`
- Ordem determinística de spawn na `GameplayScene`:
    1. `Player` (via `WorldDefinition` + `WorldSpawnServiceRegistry`)
    2. `Eater` (via `WorldDefinition` + `WorldSpawnServiceRegistry`)
- O `Eater` passa a ser tratado como ator de **primeira classe** no pipeline de reset determinístico
  (spawn/despawn/reset), não apenas QA ou placeholder.

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

## QA/Verificações
- `WorldLifecycleMultiActorSpawnQa`:
    - Escuta `WorldLifecycleResetCompletedEvent` na `GameplayScene`.
    - Resolve `IActorRegistry` da cena e valida a presença de `Player` e `Eater`.
    - Loga summary (Total/Players/Eaters/scene/reason) e erro de QA se algum ator estiver ausente.

## Fluxo de produção (Menu → Gameplay → Pause → Resume → ExitToMenu → Menu)
1. `MenuPlayButtonBinder` → `IGameNavigationService.RequestGameplayAsync(reason)`.
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
