# WorldLifecycle Module - Runtime

## Visão Geral

Este diretório contém a implementação do **hard reset macro** do mundo e as integrações mínimas necessárias para cumprir o contrato atual de reset.

Pontos-chave:

- O reset macro do mundo é coordenado por `WorldResetService` + `WorldResetOrchestrator`.
- `WorldLifecycleController` permanece como owner de **fila/lifecycle do reset local da cena**; `WorldLifecycleSceneResetRunner` monta o trilho local; `WorldLifecycleOrchestrator` permanece como owner do pipeline determinístico `Despawn -> ScopedReset -> Spawn -> Hooks`.
- `WorldResetExecutor` não executa spawn concreto; ele **aciona o trilho local** e valida as **pós-condições mínimas** do hard reset macro.
- A integração com SceneFlow ocorre via **driver** (`WorldLifecycleSceneFlowResetDriver`), que dispara reset em `ScenesReady` (apenas em `profile=gameplay`) via `WorldResetService` e publica `WorldLifecycleResetCompletedEvent` em SKIP/fallback.
- O módulo **não deve** inferir semântica de Gameplay por nome de classe ou nome de serviço.
- O contrato com Gameplay para presença mínima após o reset ocorre via `IWorldSpawnService` com metadata explícita (`SpawnedActorKind`, `IsRequiredForWorldReset`).
- `ActorGroupRearm` pertence ao domínio de Gameplay e não substitui o hard reset macro; a integração entre ambos deve ocorrer via participants/bridges explícitos.

## Estrutura

### Runtime / Contratos públicos

- `IWorldResetRequestService.cs` — Interface pública para solicitar reset (via DI global).
- `IWorldResetService.cs` — Contrato do serviço canônico de hard reset macro.
- `IWorldResetCommands.cs` — Comandos públicos usados por callers e tooling.
- `WorldResetRequestService.cs` — Implementação canônica de `IWorldResetRequestService`.
- `WorldResetResult.cs` — Resultado do reset canônico.
- `WorldLifecycleResetStartedEvent.cs` — Evento publicado no início do reset.
- `WorldLifecycleResetCompletedEvent.cs` — Evento emitido ao final do reset.
- `WorldLifecycleResetCompletionGate.cs` — Gate que aguarda `WorldLifecycleResetCompletedEvent` antes de liberar `FadeOut/Completed`.
- `WorldLifecycleTokens.cs` — Tokens canônicos do WorldLifecycle usados com `SimulationGate`.

### Runtime / Execução local determinística

- `WorldLifecycleController.cs` — MonoBehaviour de cena que enfileira resets, governa lifecycle do rail local e delega a execução concreta.
- `WorldLifecycleSceneResetRunner.cs` — Runner interno que coleta serviços de spawn da cena e cria o orchestrator do trilho local.
- `WorldLifecycleOrchestrator.cs` — Fluxo determinístico local (Gate → Hooks → Despawn → ScopedReset → Spawn → Hooks → Release).
- `WorldLifecycleControllerLocator.cs` — Resolução dos controllers ativos por cena.

### WorldRearm / Aplicação do reset macro

- `WorldResetService.cs` — Owner do caso de uso canônico de hard reset macro.
- `WorldResetOrchestrator.cs` — Sequenciamento do reset macro (guards -> validation -> scene-local rail -> publish).
- `WorldResetExecutor.cs` — Bridge interna entre o trilho macro e o trilho local; dispara os controllers e valida pós-condições essenciais do hard reset.
- `WorldResetRequest.cs` / `WorldResetOrigin.cs` / `WorldResetReasons.cs` / `WorldResetScope.cs` — dados e enums do domínio do reset.
- `WorldResetValidationPipeline.cs` / `WorldResetSignatureValidator.cs` — validação do pedido de reset.
- `IWorldResetPolicy.cs` / `ProductionWorldResetPolicy.cs` — políticas do reset macro.
- `IWorldResetGuard.cs` / `SimulationGateWorldResetGuard.cs` — guards de execução.

### Spawn / Integração com Gameplay

- `IWorldSpawnService.cs` — contrato de spawn/despawn para o mundo atual.
- `IWorldSpawnServiceRegistry.cs` / `WorldSpawnServiceRegistry.cs` — registro ordenado de serviços de spawn da cena.
- `WorldSpawnServiceFactory.cs` — factory explícita de serviços de spawn.
- `IWorldSpawnContext.cs` / `WorldSpawnContext.cs` — contexto de spawn da cena.

### Integração com SceneFlow

- `WorldLifecycleSceneFlowResetDriver.cs` — Driver canônico SceneFlow → WorldLifecycle:
  - Observa `SceneTransitionScenesReadyEvent`.
  - Em `profile=gameplay`, chama `WorldResetService` com `WorldResetRequest`.
  - Publica `WorldLifecycleResetCompletedEvent(signature, reason)` apenas em SKIP/fallback (best-effort), evitando timeout do gate.

## Namespaces

Os arquivos deste diretório estão distribuídos principalmente em:

- `_ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime`
- `_ImmersiveGames.NewScripts.Modules.WorldLifecycle.Bindings`
- `_ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn`
- `_ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Application`
- `_ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain`
- `_ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Validation`
- `_ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies`
- `_ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Guards`

## Ownership congelado (Fase A + Fase B)

### WorldLifecycle é owner de

- pipeline macro de hard reset
- ordem e determinismo do reset macro
- validação/guards/gates do reset
- queue/lifecycle do trilho local via `WorldLifecycleController`
- bootstrap/assemblagem do trilho local via `WorldLifecycleSceneResetRunner`
- execução do pipeline local via `WorldLifecycleOrchestrator`
- validação das pós-condições mínimas após o trilho local
- publicação canônica de start/completion do reset

### Gameplay é owner de

- taxonomia de atores (`ActorKind`)
- implementação concreta dos spawn services
- rearm/reset local por ator/grupo
- bridges/participants que conectam reset macro a resets locais especializados

### WorldLifecycle não deve fazer

- inferência por `GetType().Name`
- inferência por `service.Name`
- codificação textual de `Player`, `Eater` ou equivalentes
- reexecutar spawn concreto no nível macro depois que o trilho local de cena já concluiu

## Notas de manutenção

- **Não** mover lógica de reset para o driver do SceneFlow. O driver deve permanecer fino e best-effort.
- `WorldResetService` + `WorldResetOrchestrator` são o ponto de verdade do hard reset macro.
- `WorldLifecycleController` é o ponto de verdade para fila/lifecycle do reset local por cena.
- `WorldLifecycleSceneResetRunner` monta dependências efêmeras do trilho local.
- `WorldLifecycleOrchestrator` é o ponto de verdade para a ordem/pipeline do reset local determinístico por cena.
- `WorldResetExecutor` deve permanecer fino: bridge entre trilho macro e trilho local + validação de pós-condição.
- Sempre que alterar contratos de `reason`/`signature`, atualizar evidências e regras de matching.
