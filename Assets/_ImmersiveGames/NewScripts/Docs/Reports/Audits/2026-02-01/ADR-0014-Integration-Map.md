# ADR-0014 — Integration Map (GameplayReset)

**Objetivo:** registrar, em um único lugar, **quais arquivos** se conectam ao GameplayReset para evitar regressões quando editarmos apenas parte do conjunto.

## Contratos (fonte da verdade)

- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/GameplayResetContracts.cs`
  - `GameplayResetRequest`
  - `GameplayResetTargets` (`Players`, `Eaters`)
  - `IGameplayResetOrchestrator`
  - `IGameplayResetTargetClassifier`
  - `IGameplayResetParticipant`

## Implementação (runtime)

- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/GameplayResetOrchestrator.cs`
  - Constrói batches por `GameplayResetTargets`
  - Ordena determinísticamente (`ActorId`)
  - Resolve `IGameplayResetParticipant` por target
  - Tolera targets sem participante (warning) e segue

- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/DefaultGameplayResetTargetClassifier.cs`
  - Classifica `ActorId` → `GameplayResetTargets`
  - Implementa fallback por tipo (`PlayerActor`, `EaterActor`) e por nome

- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/PlayersResetParticipant.cs`
  - Implementa reset do grupo `Players`

## Wiring / Bootstrap (DI)

- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/SceneBootstrapper.cs`
  - Registra:
    - `IGameplayResetOrchestrator` → `GameplayResetOrchestrator`
    - `IGameplayResetTargetClassifier` → `DefaultGameplayResetTargetClassifier`
    - `IGameplayResetParticipant` (multi-binding) → `PlayersResetParticipant`

## QA / Ferramentas

- `Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetRequestQaDriver.cs`
  - Exercita:
    - criação de `GameplayResetRequest`
    - classificação (`IGameplayResetTargetClassifier`)
    - execução (`IGameplayResetOrchestrator`)

## Assinaturas de log (para evidência rápida)

> Observação: as strings abaixo são úteis para grep em logs; mantenha-as estáveis quando possível.

- `GameplayResetOrchestrator`:
  - `"Gameplay reset request completed"`
  - `"Reset participant threw"`
  - `"No participant registered for target"`

## Checklist de mudança segura (quando editar qualquer peça)

1. **Buscar referências** por `IGameplayResetOrchestrator` e `IGameplayResetTargetClassifier`.
2. Validar que `SceneBootstrapper` continua registrando o conjunto completo.
3. Garantir que o conjunto de `GameplayResetTargets` não foi expandido sem adicionar participante.
4. Rodar QA: `GameplayResetRequestQaDriver` (ou equivalente) e confirmar logs.
