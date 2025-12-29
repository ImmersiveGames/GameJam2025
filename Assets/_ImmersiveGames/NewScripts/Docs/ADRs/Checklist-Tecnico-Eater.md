## 1. Checklist técnico – implementação da ADR-0011 (Eater como segundo ator)

### 1) WorldDefinition da GameplayScene

* [ ] Garantir que a `WorldDefinition` da **GameplayScene** tenha:

    * [ ] Entrada para o **Player** (já existe, só confirmar).
    * [ ] Nova entrada para o **Eater**:

        * `Kind` específico para Eater (ex.: `Eater` ou equivalente existente).
        * Prefab do Eater (já existente no projeto).
        * Ordem de execução **depois** do Player.
* [ ] Confirmar que o `WorldSpawnServiceRegistry` registra os serviços na ordem desejada:

    * `PlayerSpawnService` com ordem `1`.
    * `EaterSpawnService` com ordem `2`.

### 2) Serviço de spawn do Eater

* [ ] Criar `EaterSpawnService` (nome exato pode seguir padrão do Player, ex.: mesmo namespace/pasta de `PlayerSpawnService`):

    * Implementa `IWorldSpawnService`.
    * Usa:

        * `IWorldSpawnContext` para pegar `WorldRoot`.
        * `IUniqueIdFactory` para ID do ator.
        * `ActorRegistry` da cena para registrar o ator.
    * `SpawnAsync`:

        * Instancia o prefab do Eater sob o `WorldRoot`.
        * Registra o ator no `ActorRegistry` com ID único.
        * Injeta o que for necessário (gate, state dependent etc.) via DI/serviços, não via `Find`.
    * `DespawnAsync`:

        * Despawn simétrico ao Player (sem reset in place).
        * Limpa todos os Eaters daquela cena registrados no `ActorRegistry`.

### 3) Integração com ActorRegistry / StateDependent

* [ ] Confirmar que o Eater:

    * [ ] É registrado no `ActorRegistry` com algum identificador claro (ex.: prefixo `A_..._Eater_...` ou o padrão atual).
    * [ ] É compatível com os ganchos de `WorldLifecycleOrchestrator`:

        * `OnAfterActorSpawn` é chamado para o Eater (ou pelo menos não quebra nada).
* [ ] Garantir que scripts de movimento/IA do Eater:

    * [ ] Consultem `IStateDependentService` (ou serviço equivalente) para só agir quando:

        * `GameLoopState == Playing`.
        * `gateOpen == true`.
        * `gameplayReady == true`.
        * `paused == false`.
    * [ ] Congelam em pause (`Paused` + token `state.pause`) e durante transições (`flow.scene_transition`).

### 4) Invariantes de SceneFlow / GameLoop / Gate

* [ ] Nenhum ajuste em:

    * `SceneTransitionService`, `NewScriptsSceneFlowFadeAdapter`, `SceneFlowLoadingService`, `WorldLifecycleResetCompletionGate`.
    * `GameLoopService`, `GameLoopSceneFlowCoordinator`, `GameReadinessService`, `SimulationGateService`, `InputModeService`, `PauseOverlayController`, `GamePauseGateBridge`.
* [ ] Garantir via leitura de log que:

    * [ ] O fluxo **Startup → Menu → Gameplay** continua igual.
    * [ ] Pausa, `ExitToMenu` e retorno ao Menu funcionam com Player + Eater, sem regressão.

### 5) QA / validação

* [ ] Criar/atualizar um QA doc, por exemplo:
  `Assets/_ImmersiveGames/NewScripts/Docs/QA/WorldLifecycle-MultiActor-Eater.md`

    * Cenários:

        * Boot → Menu → Gameplay (Player + Eater spawnados).
        * Gameplay → Pause → Resume (Player + Eater respeitando pause).
        * Gameplay → ExitToMenu → Menu → Gameplay (dois ciclos com resets completos).
* [ ] (Opcional) Criar/ajustar um QA runner/harness se já existe padrão para WorldLifecycle/GameplayScene.
