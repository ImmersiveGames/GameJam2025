# ADR-0014 — Integration Map (GameplayReset)

**Objetivo:** registrar, em um único lugar, **quais arquivos** se conectam ao GameplayReset para evitar regressão quando editarmos apenas parte do conjunto.

## Contratos (fonte da verdade)

- `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/Reset/GameplayResetContracts.cs`
  - `GameplayResetStep`
  - `GameplayResetTarget`
  - `GameplayResetRequest`
  - `GameplayResetContext`
  - `IGameplayResettable` / `IGameplayResettableSync`
  - `IGameplayResetTargetClassifier`
  - `IGameplayResetOrchestrator`

## Implementação (runtime)

- `Assets/_ImmersiveGames/NewScripts/Runtime/Reset/GameplayResetOrchestrator.cs`
  - Resolve targets via ActorRegistry (registry-first)
  - Fallback de scan somente quando policy permitir
  - Ordena determinístico por `ActorId` e por `IGameplayResetOrder`
  - Executa etapas `Cleanup -> Restore -> Rebind`

- `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/Reset/DefaultGameplayResetTargetClassifier.cs`
  - Classifica targets via registry + `GameplayResetTarget`
  - Fallback string-based para `EaterOnly` (compatibilidade)

- `Assets/_ImmersiveGames/NewScripts/Runtime/Reset/PlayersResetParticipant.cs`
  - Ponte WorldLifecycle (ResetScope.Players) -> GameplayReset (PlayersOnly)

## Wiring / Bootstrap (DI)

- `Assets/_ImmersiveGames/NewScripts/Runtime/Bootstrap/SceneBootstrapper.cs`
  - Registra:
    - `IGameplayResetOrchestrator` -> `GameplayResetOrchestrator`
    - `IGameplayResetTargetClassifier` -> `DefaultGameplayResetTargetClassifier`

## QA / Ferramentas

- `Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetRequestQaDriver.cs`
  - Exercita:
    - criação de `GameplayResetRequest`
    - classificação (`IGameplayResetTargetClassifier`)
    - execução (`IGameplayResetOrchestrator`)

## Assinaturas de log (para evidência rápida)

> Observação: as strings abaixo são úteis para grep em logs; mantenha-as estáveis quando possível.

- `GameplayResetOrchestrator`:
  - `[GameplayReset] Start:`
  - `[GameplayReset] Completed:`
  - `[DEGRADED_MODE]` (fallbacks e no-targets)

## Checklist de mudança segura (quando editar qualquer peça)

1. **Buscar referências** por `IGameplayResetOrchestrator` e `IGameplayResetTargetClassifier`.
2. Validar que `SceneBootstrapper` continua registrando o conjunto completo.
3. Garantir que `GameplayResetTarget` não foi expandido sem atualizar classificador/consumo.
4. Rodar QA: `GameplayResetRequestQaDriver` (ou equivalente) e confirmar logs.