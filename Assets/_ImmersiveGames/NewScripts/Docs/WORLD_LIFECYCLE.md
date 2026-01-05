# World Lifecycle (NewScripts)

> Este documento implementa operacionalmente as decisões descritas no **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas**.

## Objetivo
Garantir que o “mundo” possa ser reinicializado de forma previsível, para:
- transições de cena,
- reinício de partida,
- retorno ao menu,
- e testes determinísticos.

## Baseline 2.0 (status)
- **Status:** FECHADO / OPERACIONAL (2026-01-05).
- **ADR:** [ADR-0016-Baseline-2.0-Fechamento](ADRs/ADR-0016-Baseline-2.0-Fechamento.md).
- **Spec frozen:** [Baseline 2.0 — Spec](Reports/Baseline-2.0-Spec.md).
- **Evidência checklist-driven (Pass):** [Baseline 2.0 — ChecklistVerification Last Run](Reports/Baseline-2.0-ChecklistVerification-LastRun.md).

## Fluxo de transição (canônico)
**Resumo:** `SceneTransitionStarted` → `SceneTransitionScenesReady` → (reset/skip) → `SceneTransitionCompleted`.

**Ordem observada (UseFade=true):**
1. `SceneTransitionStartedEvent` → gate `flow.scene_transition` **fecha**.
2. `FadeIn`.
3. `LoadingHUD.Show`.
4. Load/Unload/Active das cenas.
5. `SceneTransitionScenesReadyEvent`.
6. `WorldLifecycleRuntimeCoordinator` executa **reset** (gameplay) ou **SKIP** (startup/frontend).
7. `WorldLifecycleResetCompletedEvent` libera o completion gate.
8. `LoadingHUD.Hide`.
9. `FadeOut`.
10. `SceneTransitionCompletedEvent` → gate `flow.scene_transition` **abre**.

**Fallback (UseFade=false):** `LoadingHUD.Show` pode ocorrer no `Started`, com `Hide` antes do `FadeOut` (safety hide no `Completed`).

## Módulo Loading HUD (SceneFlowLoadingService)
O módulo de Loading HUD é orquestrado pelo `SceneFlowLoadingService` e usa o `INewScriptsLoadingHudService`
para garantir que a HUD esteja carregada e visível **sem bloquear** o SceneFlow.

Contrato de ordem (UseFade=true):
- `SceneTransitionFadeInCompletedEvent` → Show (HUD fica visível antes de load/unload).
- `SceneTransitionBeforeFadeOutEvent` → Hide (após ResetCompleted e antes do FadeOut).
- `SceneTransitionCompletedEvent` → Safety hide (idempotente).

Fallback (UseFade=false):
- `SceneTransitionStartedEvent` → Show.
- `SceneTransitionBeforeFadeOutEvent` → Hide.
- `SceneTransitionCompletedEvent` → Safety hide.

Assinaturas de log estáveis (exemplos):
- `[Loading] FadeInCompleted → Show. signature='...'`
- `[LoadingHUD] Show aplicado. signature='...', phase='AfterFadeIn'`
- `[Loading] BeforeFadeOut → Hide. signature='...'`
- `[LoadingHUD] Hide aplicado. signature='...', phase='BeforeFadeOut'`
- Fallback HUD ausente: `[Loading] HUD indisponível nesta transição. ...`

## Explicação simples
Pense no reset como a “faxina + reconstrução” do mundo. Durante o loading real, o jogo:
1) carrega as cenas necessárias,
2) limpa o que precisa ser limpo,
3) respawna/prepara o que precisa existir.

Esse loading **só termina** quando o reset finaliza e o `WorldLifecycleResetCompletedEvent`
é emitido. Por isso, o `FadeOut` só deve acontecer **depois** do reset — o jogo só está pronto
após o **ResetCompleted**.

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

## Production Reset Trigger
Há dois caminhos controlados para reset do mundo:

1) **Durante transição (canônico)**
   - `SceneTransitionScenesReadyEvent` → `WorldLifecycleRuntimeCoordinator` → `ResetWorldAsync(...)`.
   - Em gameplay, o reason é `ScenesReady/<ActiveScene>` e o fluxo termina com
     `WorldLifecycleResetCompletedEvent(signature, reason)`.

2) **Fora de transição (produção)**
   - `IWorldResetRequestService.RequestResetAsync(source)` dispara um reset manual quando **não**
     há transição ativa (gate `flow.scene_transition`).
   - Limitação atual: como não há `SceneTransitionContext`, **não** existe
     `WorldLifecycleResetCompletedEvent` nesse caminho (não há signature para correlacionar).

### Completion gate (SceneFlow)
O `WorldLifecycleResetCompletionGate` bloqueia o final da transição (antes do `FadeOut`) até que:
- `WorldLifecycleRuntimeCoordinator` emita `WorldLifecycleResetCompletedEvent(signature, reason)`.

Parâmetros operacionais:
- `timeoutMs=20000` (protege contra transição travada sem reset completed).

## Reset por escopos (WorldLifecycle)

### Conceitos
- **Escopo (scope):** unidade lógica do reset (ex.: Players, Enemies, WorldUI).
- **Participante (participant):** registra-se para executar reset em um escopo específico.
- **Hook:** callbacks ordenados (OnBefore/OnAfter Despawn/Spawn) para inspeção e integrações.

### Fases de reset (ordem determinística)
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

## SKIP (startup/menu)
Para estabilizar o pipeline sem contaminar testes com Gameplay, o driver faz SKIP quando:
- `profile == 'startup'` **ou**
- `activeScene == 'MenuScene'`

Mesmo no SKIP, o driver **deve emitir**:
- `WorldLifecycleResetCompletedEvent(contextSignature, reason)`

## Contrato de `contextSignature` e `reason` (WorldLifecycleResetCompletedEvent)

O evento `WorldLifecycleResetCompletedEvent(string contextSignature, string reason)` é a **confirmação oficial** (incluindo *skip*) usada por:

- `GameLoopSceneFlowCoordinator` (sincronização do GameLoop após ScenesReady)
- `WorldLifecycleResetCompletionGate` / `ISceneTransitionCompletionGate` (liberação do `FadeOut` no SceneFlow)

### `contextSignature`

- Deve ser **exatamente** `SceneTransitionContext.ContextSignature` do *mesmo* `SceneTransitionContext` emitido pelo SceneFlow.
- `SceneTransitionSignatureUtil.Compute(context)` retorna esse mesmo valor.
- O objetivo é que **Started / ScenesReady / Completed** e todos os consumidores comparem a **mesma string**.

### `reason`

- String curta, **machine-readable**, sem localização; preferir `CamelCase`/`PascalCase` + separadores previsíveis.
- Formatos operacionais já validados:
    - Reset em gameplay: `ScenesReady/<ActiveScene>` (ex.: `ScenesReady/GameplayScene`)
    - Skip em startup/frontend: `Skipped_StartupOrFrontend:profile=<profile>;scene=<activeScene>`
- Extensões devem ser feitas por *key-values* com `;` (ex.: `...;extraKey=extraValue`) para facilitar parsing em logs e QA.

### Regras de consistência
- `WorldLifecycleRuntimeCoordinator` é o **único publisher** do evento em runtime de produção.
- `WorldLifecycleResetCompletionGate` deve cachear/confirmar por **contextSignature** (não por referência de struct).

## Gate / Readiness (tokens)
- `flow.scene_transition`: durante transições de cena (Readiness/Gate).
- `WorldLifecycle.WorldReset`: durante hard reset do mundo.
- `state.pause`: durante pausa/overlay.
- `state.postgame`: durante overlay de pós-game.

## Integração com GameLoop (pós-game)
- O **WorldLifecycle** continua responsável por resetar/despawnar/spawnar atores.
- O **GameLoop** coordena o estado macro da run (ex.: `Playing` / `PostPlay`) e publica:
    - `GameRunStartedEvent` / `GameRunEndedEvent` (resultado da run),
    - `GameLoopActivityChangedEvent` (telemetria de atividade).
- UI e sistemas em cenas globais/gameplay podem consultar `IGameRunStatusService`
  para exibir o resultado da última run sem depender diretamente de gameplay específico.
- **Restart pós-game** usa o fluxo oficial:
    - `GameResetRequestedEvent` → `RestartNavigationBridge` → `IGameNavigationService.RequestToGameplay(...)`
    - `SceneTransitionScenesReadyEvent` (profile gameplay) → `WorldLifecycleRuntimeCoordinator` → reset determinístico.

## Gameplay Reset por grupos (cleanup/restore/rebind)

Além do **reset por escopos** do WorldLifecycle (`ResetScope` + `IResetScopeParticipant`), existe um módulo de reset **de gameplay** em `Gameplay/Reset/` para validar e executar resets por **alvos** (targets) com fases fixas:

- **Alvos (`GameplayResetTarget`)**: `AllActorsInScene`, `PlayersOnly`, `EaterOnly`, `ActorIdSet`, `ByActorKind`.
- **Fases (`GameplayResetPhase`)**: `Cleanup`, `Restore`, `Rebind`.
- **Participantes**: componentes de gameplay implementam `IGameplayResettable` (e opcionais `IGameplayResetOrder` / `IGameplayResetTargetFilter`).

Integração:
- Participantes de escopo do WorldLifecycle podem atuar como **bridge** para o gameplay. Ex.: `PlayersResetParticipant` (gameplay) implementa `IResetScopeParticipant` (escopo `Players`) e, ao executar, solicita `IGameplayResetOrchestrator.RequestResetAsync(...)` para `PlayersOnly`.
- `IGameplayResetOrchestrator` e `IGameplayResetTargetClassifier` são serviços **por cena** (registrados no `NewSceneBootstrapper`) para manter o reset local ao escopo additive correto.

QA:
- Quando o spawn ainda não é a fonte de verdade, o `GameplayResetQaSpawner` cria alvos de teste e o `GameplayResetQaProbe` valida via log a execução das três fases.

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

## Evidências (log)
- Startup profile `startup` com reset SKIPPED + `WorldLifecycleResetCompletedEvent(reason=Skipped_StartupOrFrontend:profile=<profile>;scene=<activeScene>)`.
- Transição para profile `gameplay` executa reset após `ScenesReady` e antes do gate liberar.
- Completion gate aguarda `WorldLifecycleResetCompletedEvent` para prosseguir ao `FadeOut`.
- `GameReadinessService` usa token `flow.scene_transition` durante a transição.
- `PauseOverlay` publica `GamePauseCommandEvent`, `GameResumeRequestedEvent`, `GameExitToMenuRequestedEvent`
  e o gate mostra `state.pause`.

## Referências rápidas
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [Baseline 2.0 — Spec](Reports/Baseline-2.0-Spec.md)
- [Baseline 2.0 — Checklist](Reports/Baseline-2.0-Checklist.md)
