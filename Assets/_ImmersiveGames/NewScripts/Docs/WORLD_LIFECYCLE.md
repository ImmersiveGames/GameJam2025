# World Lifecycle (NewScripts)

> Este documento implementa operacionalmente as decisões descritas no **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas**.

## Evidências de execução (2026-01-05)

**Fonte:** log de produção (startup → Menu → Gameplay → Pause/Resume → PostGame (Victory/Defeat) → Restart → ExitToMenu → Menu).

### Evidências consolidadas (snapshot)
- **Gate em transição:** `SceneTransitionStarted` → acquire `flow.scene_transition` (`SimulationGateService`), e `SceneTransitionCompleted` → release do token.
- **Reset no momento correto:** `SceneTransitionScenesReadyEvent` → `WorldLifecycleRuntimeCoordinator` executa **SKIP** em `startup/frontend` e **HARD reset** em `gameplay`, emitindo `WorldLifecycleResetCompletedEvent(signature, reason)`.
- **Completion gate:** `WorldLifecycleResetCompletionGate` armazena `lastSignature/lastReason` e destrava `SceneTransitionService` **antes** do FadeOut.
- **Pausa/PostGame:** tokens `state.pause` (pause/resume) e `state.postgame` (overlay) controlam o gate.
- **Rotas de navegação:** `to-gameplay` (MenuPlayButtonBinder), `GameResetRequestedEvent → RequestGameplayAsync` (Restart), `GameExitToMenuRequestedEvent → RequestMenuAsync` (ExitToMenu).
- **Higiene de escopo:** `SceneServiceCleaner` remove serviços do escopo de cena ao unload; `GlobalServiceRegistry` descarrega registros no shutdown.

> Para exemplos detalhados de assinaturas e logs, ver seções **Atualização (2025-12-31)** e **Evidências (log)** abaixo.


## Atualização (2026-01-03)

- **Assinatura canônica:** `contextSignature` é `SceneTransitionContext.ContextSignature`, e `SceneTransitionSignatureUtil.Compute(context)` retorna esse valor. `SceneTransitionContext.ToString()` é apenas debug/log.
- **Loading HUD sem flash (runtime):**
    - **UseFade=true:** `FadeInCompleted → Show` e `BeforeFadeOut → Hide`, com safety hide em `Completed`.
    - **UseFade=false:** `Started → Show` e `BeforeFadeOut → Hide`, com safety hide em `Completed`.
- **Ordem operacional (UseFade=true) validada:** **SceneTransitionStarted → gate `flow.scene_transition` fechado → FadeIn → LoadingHUD Show (AfterFadeIn) → Load/Unload/Active → ScenesReady → WorldLifecycle Reset (ou Skip) → WorldLifecycleResetCompletedEvent → completion gate → BeforeFadeOut (LoadingHUD Hide) → FadeOut → Completed (gate release + InputMode + GameLoop sync)**.
- **Completion gate registrado:** `ISceneTransitionCompletionGate = WorldLifecycleResetCompletionGate`, `timeoutMs=20000`.

## Atualização (2025-12-31)

- Evidência: logs de produção (startup → Menu → Gameplay) confirmam SceneFlow + WorldLifecycle + gate.

### Sinais e ordem operacional (UseFade=true)
- `SceneTransitionStarted` fecha o gate via token `flow.scene_transition` (GameReadinessService + SimulationGateService).
- Ordem observada do Loading/HUD (SceneFlowLoadingService): `Started` → (após `FadeInCompleted`) `Show` → `ScenesReady` (update pending) → `BeforeFadeOut` `Hide` → `Completed` (safety hide).
- `SceneTransitionService` aguarda o completion gate **antes** do FadeOut (`Aguardando completion gate antes do FadeOut` → `Completion gate concluído` → FadeOut).

### WorldLifecycle (runtime)
- `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent` e:
    - **SKIP** em perfis `startup` e `frontend` (razão: `Skipped_StartupOrFrontend:profile=startup;scene=MenuScene` ou `Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene`).
    - **Hard reset** em perfil `gameplay` (razão: `ScenesReady/GameplayScene`).
- `WorldLifecycleResetCompletionGate` recebe `WorldLifecycleResetCompletedEvent(signature, reason)` e destrava a transição.

### Pausa e PostGame (gating)
- Pausa: `GamePauseGateBridge` adquire `state.pause` e libera em `GameResumeRequestedEvent`.
- PostGame: `PostGameOverlayController` adquire `state.postgame` ao exibir overlay e libera ao finalizar a ação.

### Navegação (produção)
- Menu → Gameplay: `MenuPlayButtonBinder` → `IGameNavigationService.NavigateAsync(routeId='to-gameplay', profile='gameplay')`.
- Restart (PostGame): `RestartNavigationBridge` → `GameResetRequestedEvent` → `RequestGameplayAsync`.
- ExitToMenu: `ExitToMenuNavigationBridge` → `GameExitToMenuRequestedEvent` → `RequestMenuAsync(routeId='to-menu', profile='frontend')`.

### Assinaturas observadas (exemplos)
- Startup/Menu: `p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap`.
- Gameplay: `p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene`.
- ExitToMenu: `p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene`.



## Atualização (2025-12-30)

- Fluxo de **produção** validado end-to-end: Startup → Menu → Gameplay via SceneFlow + Fade + LoadingHUD + Navigation.
- `WorldLifecycleRuntimeCoordinator` reage a `SceneTransitionScenesReadyEvent`:
    - Profile `startup`/frontend: reset **skip** + emite `WorldLifecycleResetCompletedEvent(contextSignature, reason)`.
    - Profile `gameplay`: dispara **hard reset** (`ResetWorldAsync`) e emite `WorldLifecycleResetCompletedEvent(contextSignature, reason)` ao concluir.
- `SceneTransitionService` aguarda o `WorldLifecycleResetCompletedEvent` (via `ISceneTransitionCompletionGate`) antes do `FadeOut`.
    - A chave é o `contextSignature` do `SceneTransitionContext` (`SceneTransitionContext.ContextSignature`).
- Hard reset em Gameplay confirma spawn via `WorldDefinition` (Player/Eater) e execução do orchestrator com gate (`WorldLifecycle.WorldReset`).
- `IStateDependentService` bloqueia input/movimento enquanto `SimulationGate` está fechado e/ou `gameplayReady=false`; libera ao final. Pausa também fecha gate via `GamePauseGateBridge`.

```log
[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] SceneTransitionScenesReady recebido. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'
[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.
[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.
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
      `WorldLifecycleResetCompletedEvent(reason='ScenesReady/<ActiveScene>')`

- `SceneTransitionCompleted`:
    - `GameReadinessService` libera token
    - jogo pode voltar a “READY” (sujeito ao estado do GameLoop e outras condições)

### Fluxo de produção integrado (SceneFlow + WorldLifecycle)
- **Perfis startup/frontend** (ex.: transição terminando em `MenuScene`):
    - `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent`, mas **não** dispara hard reset.
    - Emite `WorldLifecycleResetCompletedEvent(...)` para destravar o pipeline (ex.: `reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'`).
    - O `GameLoopSceneFlowCoordinator` considera o reset como concluído (skip) e o GameLoop permanece em **Ready**.

```log
[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] SceneTransitionScenesReady recebido. Context=Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'
[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset SKIPPED (startup/frontend). Emitting WorldLifecycleResetCompletedEvent(signature='p:startup|a:MenuScene|l:MenuScene,UIGlobalScene|u:NewBootstrap|f:1', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene')
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
    - `GameResetRequestedEvent` → `RestartNavigationBridge` → `IGameNavigationService.RequestToGameplay(...)`
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

- Deve ser **exatamente** `SceneTransitionContext.ContextSignature` do *mesmo* `SceneTransitionContext` emitido pelo SceneFlow.
- `SceneTransitionSignatureUtil.Compute(context)` retorna esse mesmo valor.
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

Parâmetros operacionais:
- `timeoutMs=20000` (protege contra transição travada sem reset completed).

### Solicitação de fim de run (source agnostic)

O módulo de pós-game não “descobre” vitória/derrota. Ele apenas reage quando **alguém solicita** o fim da run.

- **Solicitar (input):** use `IGameRunEndRequestService` (DI global) ou publique diretamente `GameRunEndRequestedEvent(GameRunOutcome outcome, string reason = null)`.
- **Bridge (produção):** `GameRunOutcomeEventInputBridge` observa `GameRunEndRequestedEvent` e traduz para chamadas no `IGameRunOutcomeService`.
- **Concluir (output):** `GameRunOutcomeService` valida **estado do GameLoop (Playing)** + **idempotência (uma vez por run)** e publica `GameRunEndedEvent(GameRunOutcome outcome, string reason = null)`.

Notas:
- Em produção, **evite** publicar `GameRunEndedEvent` diretamente; isso é útil apenas para QA/injeções controladas.
- Qualquer sistema de gameplay pode solicitar fim de run (timer, morte, objetivo, sequência de eventos etc.); a política de “quem decide” fica **fora** do módulo de pós-game.

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

- **Alvos (`GameplayResetTarget`)**: `AllActorsInScene`, `PlayersOnly`, `EaterOnly`, `ActorIdSet`, `ByActorKind`.
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

## Fluxo de produção (Menu → Gameplay → Pause/Resume → PostGame → Restart/ExitToMenu → Menu)
1. `MenuPlayButtonBinder` → `IGameNavigationService.RequestToGameplay(reason)`.
2. `SceneTransitionService`:
    - `Started` → `FadeIn`
    - Load/Unload → `ScenesReady`
    - completion gate (`WorldLifecycleResetCompletionGate`)
    - `FadeOut` → `Completed`
3. `WorldLifecycleRuntimeCoordinator`:
    - **Gameplay**: executa reset após `ScenesReady` e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
    - **Startup/Frontend**: SKIP com reason `Skipped_StartupOrFrontend:profile=<profile>;scene=<activeScene>`.
4. `GameReadinessService`:
    - token `flow.scene_transition` no `Started` (**evidenciado no log de 2026-01-05**):
        - `SimulationGateService` Acquire token='flow.scene_transition' (IsOpen=False)
        - `GameReadinessService` Snapshot: gateOpen=False, activeTokens=1
    - libera no `Completed` (**evidenciado no log de 2026-01-05**):
        - `SimulationGateService` Release token='flow.scene_transition' (IsOpen=True)
        - `GameReadinessService` Snapshot: gateOpen=True, activeTokens=0
5. PauseOverlay:
    - `GamePauseCommandEvent` / `GameResumeRequestedEvent` / `GameExitToMenuRequestedEvent`
    - `SimulationGateTokens.Pause` controla pausa sem congelar física.

## Evidências (log)
- Gameplay profile `gameplay` com reset HARD após `SceneTransitionScenesReady` e emissão de `WorldLifecycleResetCompletedEvent(signature, reason='ScenesReady/GameplayScene')`.
- Spawn pipeline evidenciado:
    - `NewSceneBootstrapper` registrou 2 spawn services (`PlayerSpawnService` ordem 1, `EaterSpawnService` ordem 2).
    - `WorldLifecycleOrchestrator` confirmou `ActorRegistry count` 0→2 após `Spawn`.
- Pause/Resume evidenciado:
    - `GamePauseGateBridge` adquiriu token `state.pause` (gate fechado) e liberou no resume.
    - `GameLoopService` sincronizou `Playing → Paused → Playing`.
- PostGame evidenciado (Victory e Defeat no mesmo run):
    - `PostGameOverlayController` adquiriu token `state.postgame` e liberou após ação do overlay.
    - `GameRunStatusService` atualizou outcome + reason e publicou `GameRunEndedEvent`.
- Restart evidenciado:
    - `RestartNavigationBridge` recebeu `GameResetRequestedEvent` → `IGameNavigationService` routeId `to-gameplay`.
    - Reset subsequente despawnou `Player`/`Eater` (ActorRegistry 2→0) e respawnou (0→2).
- ExitToMenu evidenciado:
    - `ExitToMenuNavigationBridge` recebeu `GameExitToMenuRequestedEvent` → routeId `to-menu` (profile `frontend`).
    - `WorldLifecycleRuntimeCoordinator` SKIP (frontend) e gate completado via `WorldLifecycleResetCompletionGate`.
- Cleanup de encerramento evidenciado:
    - `SceneServiceRegistry` removeu serviços por cena no unload (ex.: 8 serviços em `GameplayScene` e `MenuScene`).
    - `GlobalServiceRegistry` removeu serviços globais no shutdown (log: 24 serviços).

- Startup profile `startup` com reset SKIPPED + `WorldLifecycleResetCompletedEvent(reason=Skipped_StartupOrFrontend:profile=<profile>;scene=<activeScene>)`.
- Transição para profile `gameplay` executa reset após `ScenesReady` e antes do gate liberar.
- Completion gate aguarda `WorldLifecycleResetCompletedEvent` para prosseguir ao `FadeOut`.
- `GameReadinessService` usa token `flow.scene_transition` durante a transição (**evidenciado no log de 2026-01-05**).
- `PauseOverlay` publica `GamePauseCommandEvent`, `GameResumeRequestedEvent`, `GameExitToMenuRequestedEvent`
  e o gate mostra `state.pause`.

## Ownership e limpeza de serviços

### Objetivo
Evitar vazamentos (subscriptions) e efeitos colaterais entre transições/resets, deixando explícito **o que pertence ao escopo Global vs Scene vs Object**, e **quem é responsável pela limpeza**.

### Regras de escopo (contrato)
- **Global**: serviços `DontDestroyOnLoad` registrados no `GlobalServiceRegistry`. Permanecem vivos entre transições de cena e só devem ser limpos por **shutdown/reset global** (ex.: `DependencyManager.Dispose` / `ResetStatics`).
- **Scene**: serviços registrados via `DependencyManager.Provider.RegisterScene(...)` e armazenados no `SceneServiceRegistry` por `sceneName`. Devem ser limpos **sempre que a cena descarrega**.
- **Object**: serviços vinculados a um GameObject/instância (quando usado) e limpos quando o objeto é destruído ou quando o registry do objeto é limpo.

### Fonte de verdade (implementação atual)
| Escopo | Registry | Quem registra | Quem limpa | Gatilho principal | Fallback | Observações |
|---|---|---|---|---|---|---|
| Global | `GlobalServiceRegistry` | `GlobalBootstrap` | `DependencyManager.Dispose` (via `GlobalServiceRegistry.Clear()`), ou reset de estáticos | Encerramento do jogo / reset global | n/a | Serviços globais que assinam eventos devem implementar `IDisposable` para desinscrever no clear. |
| Scene | `SceneServiceRegistry` | `NewSceneBootstrapper.Awake()` | `SceneServiceCleaner` (global) | `SceneManager.sceneUnloaded` | `NewSceneBootstrapper.OnDestroy()` chama `DependencyManager.Provider.ClearSceneServices(sceneName)` | O registry descarta `IDisposable` no clear por cena. `SceneServiceCleaner` é idempotente (ver `TryClear`). |
| Object | `ObjectServiceRegistry` | Quem cria o objeto/owner | Owner do objeto ou clear do registry do objeto | `OnDestroy` / ciclo do objeto | n/a | Usado para serviços que não podem viver além de um objeto específico. |

### Exemplos práticos
- **Global** (vive entre cenas): `SceneTransitionService`, `GameReadinessService`/gate, `WorldLifecycleRuntimeCoordinator`, `GameLoopSceneFlowCoordinator`.
- **Scene** (reinicializa a cada cena): registries de gameplay (ex.: `IActorRegistry`/spawn registries), serviços de UI da cena, câmeras/bridges de cena, participantes do reset (`IResetScopeParticipant`).

### Implicações para ResetWorld
- **ResetWorld (hard reset)** deve atuar no *mundo* (atores/spawn/hooks) e **não** substituir a responsabilidade de descarregar escopos: a limpeza de serviços **por cena** é responsabilidade do descarregamento da cena.
- Serviços globais devem ser **resilientes a múltiplos resets** e evitar manter referências a objetos/serviços de cena sem validar se ainda estão vivos.

