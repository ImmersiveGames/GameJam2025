# ADR-0014 — Integration Map (GameplayReset)

**Objetivo:** registrar, em um único lugar, **quais arquivos** se conectam ao GameplayReset para evitar regressão quando editarmos apenas parte do conjunto.

## Contratos (fonte da verdade)

- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Runtime/RunRearmContracts.cs`
  - `RunRearmStep`
  - `RunRearmTarget`
  - `RunRearmRequest`
  - `RunRearmContext`
  - `IRunRearmable` / `IRunRearmableSync`
  - `IRunRearmTargetClassifier`
  - `IRunRearmOrchestrator`

## Implementação (runtime)

- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Runtime/RunRearmOrchestrator.cs`
  - Resolve targets via ActorRegistry (registry-first)
  - Fallback de scan somente quando policy permitir
  - Ordena determinístico por `ActorId` e por `IRunRearmOrder`
  - Executa etapas `Cleanup -> Restore -> Rebind`

- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Runtime/DefaultRunRearmTargetClassifier.cs`
  - Classifica targets via registry + `RunRearmTarget`
  - Fallback string-based para `EaterOnly` (compatibilidade)

- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Interop/PlayersRunRearmWorldParticipant.cs`
  - Ponte WorldLifecycle (ResetScope.Players) -> RunRearm (PlayersOnly)

## Wiring / Bootstrap (DI)

- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.cs`
  - Registra:
    - `IRunRearmOrchestrator` -> `RunRearmOrchestrator`
    - `IRunRearmTargetClassifier` -> `DefaultRunRearmTargetClassifier`

## QA / Ferramentas

- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Dev/RunRearmRequestDevDriver.cs`
  - Exercita:
    - criação de `RunRearmRequest`
    - classificação (`IRunRearmTargetClassifier`)
    - execução (`IRunRearmOrchestrator`)

## Assinaturas de log (para evidência rápida)

> Observação: as strings abaixo são úteis para grep em logs; mantenha-as estáveis quando possível.

- `RunRearmOrchestrator`:
  - `[GameplayReset] Start:`
  - `[GameplayReset] Completed:`
  - `[DEGRADED_MODE]` (fallbacks e no-targets)

## Checklist de mudança segura (quando editar qualquer peça)

1. **Buscar referências** por `IRunRearmOrchestrator` e `IRunRearmTargetClassifier`.
2. Validar que `SceneScopeCompositionRoot` continua registrando o conjunto completo.
3. Garantir que `RunRearmTarget` não foi expandido sem atualizar classificador/consumo.
4. Rodar QA: `RunRearmRequestDevDriver` (ou equivalente) e confirmar logs.