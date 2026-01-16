# SceneFlow — Gameplay Blockers Report

> **Master:** [SceneFlow-Production-EndToEnd-Validation.md](../../SceneFlow-Production-EndToEnd-Validation.md)
>  
> **Nota:** usar este report apenas para investigação de blockers; validação final é no master.

- Data: 2025-12-28
- Fluxo analisado: Menu → Gameplay (Play button / navigation)
- Objetivo: identificar e corrigir os 3 primeiros erros críticos que impediam a GameplayScene de funcionar em produção.

## Blocker 1 — Assinatura de contexto inconsistente entre eventos de transição

**Erro (resumo):**
- `[GameLoopSceneFlow] TransitionCompleted ignorado (signature mismatch). expected='…', got='…'`.

**Causa raiz:**
- O `GameLoopSceneFlowCoordinator` calculava a assinatura esperada via `context.ToString()` e comparava com outras fontes.
- O pipeline de SceneFlow/WorldLifecycle já centraliza a assinatura em `SceneTransitionSignatureUtil.Compute(context)`. Divergências na forma de gerar a assinatura impediam o Coordinator de liberar o start do GameLoop.

**Correção aplicada:**
- Padronização do cálculo da assinatura para `SceneTransitionSignatureUtil.Compute(context)`.
- Arquivo alterado:
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs`

**Evidência (trecho de log):**
```
[GameLoopSceneFlow] TransitionCompleted ignorado (signature mismatch). expected='...', got='...'
```

---

## Blocker 2 — Prefab do Player sem PlayerActor resultava em spawn abortado

**Erro (resumo):**
- `Prefab não possui PlayerActor. Player não será instanciado.`

**Causa raiz:**
- O `PlayerSpawnService` exigia obrigatoriamente `PlayerActor`. Em cenários onde o prefab tinha `PlayerActorAdapter` (ou outro `IActor` válido), o serviço destruía a instância, impedindo spawn e input/câmera.

**Correção aplicada:**
- Aceitar `PlayerActorAdapter` e outros `IActor` válidos com `ActorId`.
- Fallback: adicionar `PlayerActor` quando nenhum `IActor` existe no prefab.
- Arquivo alterado:
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Spawn/PlayerSpawnService.cs`

**Evidência (trecho de log):**
```
Prefab não possui PlayerActor. Player não será instanciado.
```

---

## Blocker 3 — Start do GameLoop bloqueado após erro de DI

**Erro (resumo):**
- `[GameLoopSceneFlow] IGameLoopService indisponível no DI global; não foi possível RequestStart().`

**Causa raiz:**
- Quando o `IGameLoopService` não era resolvido no DI global durante o gate de start, o coordinator mantinha `_startInProgress=true`. Com isso, novos pedidos de start eram ignorados e o fluxo ficava travado.

**Correção aplicada:**
- Reset do estado `_startInProgress` em erro de DI para permitir novas tentativas de start quando o serviço estiver disponível.
- Arquivo alterado:
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs`

**Evidência (trecho de log):**
```
[GameLoopSceneFlow] IGameLoopService indisponível no DI global; não foi possível RequestStart().
```

---

## Resultado pós-fix

- O fluxo Menu → Gameplay completa sem exceções.
- O GameLoop avança para `Playing` após `TransitionCompleted` + `WorldLifecycleResetCompleted`.
- Player spawna com input/câmera funcional.
