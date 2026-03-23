# Fase 4.d1 — Reclassificação arquitetural do módulo atual de WorldLifecycle

## Objetivo
Parar de tratar `Modules/WorldLifecycle` como um módulo único com subpastas técnicas e assumir o shape real que já emergiu no código:

- **WorldReset** = domínio macro de reset
- **SceneReset** = domínio local de reset por cena
- **ResetInterop** = bridges, eventos públicos e tokens observáveis

Esta fase **não** é micro-refactor de classe grande.
Ela é um plano de reorganização estrutural do módulo, preservando apenas:
- contratos externos já validados
- ordem do pipeline local
- pontos públicos observáveis necessários

## Problema
O módulo atual ainda mistura três responsabilidades diferentes em uma mesma raiz (`Modules/WorldLifecycle`):
- comandos/serviços macro (`WorldReset*`, `WorldRearm/*`)
- trilho local (`SceneReset*`, `Runtime/SceneReset/*`)
- interop/superfície (`WorldLifecycleSceneFlowResetDriver`, eventos, tokens)

Isso prejudica:
- leitura do ownership
- evolução isolada de macro vs local
- clareza de namespaces
- redução de arquivos grandes depois

## Decisão
A próxima reorganização deve dividir o módulo atual em **três áreas reais de responsabilidade**.

---

# Árvore alvo

```text
Modules/
  WorldReset/
    Runtime/
      IWorldResetCommands.cs
      IWorldResetRequestService.cs
      IWorldResetService.cs
      ResetKind.cs
      WorldResetCommands.cs
      WorldResetRequestService.cs
      WorldResetResult.cs
    Application/
      WorldResetService.cs
      WorldResetOrchestrator.cs
      WorldResetExecutor.cs
    Domain/
      WorldResetContext.cs
      WorldResetFlags.cs
      WorldResetScope.cs
      WorldResetOrigin.cs
      WorldResetReasons.cs
      WorldResetRequest.cs
      ResetDecision.cs
      ResetFeatureIds.cs
    Validation/
      IWorldResetValidator.cs
      WorldResetSignatureValidator.cs
      WorldResetValidationPipeline.cs
    Policies/
      IWorldResetPolicy.cs
      ProductionWorldResetPolicy.cs
      IRouteResetPolicy.cs
      SceneRouteResetPolicy.cs
    Guards/
      IWorldResetGuard.cs
      SimulationGateWorldResetGuard.cs

  SceneReset/
    Bindings/
      SceneResetController.cs
      SceneResetRunner.cs
    Runtime/
      SceneResetControllerLocator.cs
      SceneResetFacade.cs
      SceneReset/
        SceneResetContext.cs
        SceneResetHookRunner.cs
        SceneResetPipeline.cs
        ISceneResetPhase.cs
        Phases/
          AcquireResetGatePhase.cs
          BeforeDespawnHooksPhase.cs
          DespawnPhase.cs
          AfterDespawnHooksPhase.cs
          ScopedParticipantsResetPhase.cs
          SpawnPhase.cs
          AfterSpawnHooksPhase.cs
    Hooks/
      ISceneResetHook.cs
      ISceneResetHookOrdered.cs
      SceneResetHookBase.cs
      SceneResetHookRegistry.cs
    Spawn/
      IWorldSpawnContext.cs
      IWorldSpawnService.cs
      IWorldSpawnServiceRegistry.cs
      WorldSpawnContext.cs
      WorldSpawnServiceFactory.cs
      WorldSpawnServiceRegistry.cs

  ResetInterop/
    Runtime/
      WorldLifecycleSceneFlowResetDriver.cs
      WorldLifecycleResetStartedEvent.cs
      WorldLifecycleResetCompletedEvent.cs
      WorldLifecycleResetV2Events.cs
      WorldLifecycleResetCompletionGate.cs
      WorldLifecycleTokens.cs
```

---

# Regras de ownership

## WorldReset
Owner de:
- API macro de reset
- coordenação macro
- pós-condição macro
- políticas/guards/validation do macro

## SceneReset
Owner de:
- pipeline local determinístico
- controller/runner/facade local
- hooks locais
- spawn services / registry do reset local

## ResetInterop
Owner de:
- bridge `SceneFlow -> WorldReset`
- eventos públicos observáveis do reset
- completion gate observável
- tokens observáveis usados por outros módulos

---

# Mapeamento arquivo atual → destino novo

## Permanecer como WorldReset (macro)

| Atual | Destino novo |
|---|---|
| `Runtime/IWorldResetCommands.cs` | `Modules/WorldReset/Runtime/IWorldResetCommands.cs` |
| `Runtime/IWorldResetRequestService.cs` | `Modules/WorldReset/Runtime/IWorldResetRequestService.cs` |
| `Runtime/IWorldResetService.cs` | `Modules/WorldReset/Runtime/IWorldResetService.cs` |
| `Runtime/ResetKind.cs` | `Modules/WorldReset/Runtime/ResetKind.cs` |
| `Runtime/WorldResetCommands.cs` | `Modules/WorldReset/Runtime/WorldResetCommands.cs` |
| `Runtime/WorldResetRequestService.cs` | `Modules/WorldReset/Runtime/WorldResetRequestService.cs` |
| `Runtime/WorldResetResult.cs` | `Modules/WorldReset/Runtime/WorldResetResult.cs` |
| `WorldRearm/Application/WorldResetService.cs` | `Modules/WorldReset/Application/WorldResetService.cs` |
| `WorldRearm/Application/WorldResetOrchestrator.cs` | `Modules/WorldReset/Application/WorldResetOrchestrator.cs` |
| `WorldRearm/Application/WorldResetExecutor.cs` | `Modules/WorldReset/Application/WorldResetExecutor.cs` |
| `WorldRearm/WorldResetContext.cs` | `Modules/WorldReset/Domain/WorldResetContext.cs` |
| `WorldRearm/WorldResetFlags.cs` | `Modules/WorldReset/Domain/WorldResetFlags.cs` |
| `WorldRearm/WorldResetScope.cs` | `Modules/WorldReset/Domain/WorldResetScope.cs` |
| `WorldRearm/Domain/WorldResetOrigin.cs` | `Modules/WorldReset/Domain/WorldResetOrigin.cs` |
| `WorldRearm/Domain/WorldResetReasons.cs` | `Modules/WorldReset/Domain/WorldResetReasons.cs` |
| `WorldRearm/Domain/WorldResetRequest.cs` | `Modules/WorldReset/Domain/WorldResetRequest.cs` |
| `WorldRearm/Domain/ResetDecision.cs` | `Modules/WorldReset/Domain/ResetDecision.cs` |
| `WorldRearm/Domain/ResetFeatureIds.cs` | `Modules/WorldReset/Domain/ResetFeatureIds.cs` |
| `WorldRearm/Validation/*` | `Modules/WorldReset/Validation/*` |
| `WorldRearm/Policies/*` | `Modules/WorldReset/Policies/*` |
| `WorldRearm/Guards/*` | `Modules/WorldReset/Guards/*` |
| `Runtime/IRouteResetPolicy.cs` | `Modules/WorldReset/Policies/IRouteResetPolicy.cs` |
| `Runtime/SceneRouteResetPolicy.cs` | `Modules/WorldReset/Policies/SceneRouteResetPolicy.cs` |

## Permanecer/assumir como SceneReset (local)

| Atual | Destino novo |
|---|---|
| `Bindings/WorldLifecycleController.cs` | `Modules/SceneReset/Bindings/SceneResetController.cs` |
| `Bindings/WorldLifecycleSceneResetRunner.cs` *(classe `SceneResetRunner`)* | `Modules/SceneReset/Bindings/SceneResetRunner.cs` |
| `Runtime/WorldLifecycleControllerLocator.cs` | `Modules/SceneReset/Runtime/SceneResetControllerLocator.cs` |
| `Runtime/WorldLifecycleOrchestrator.cs` | `Modules/SceneReset/Runtime/SceneResetFacade.cs` |
| `Runtime/SceneReset/*` | `Modules/SceneReset/Runtime/SceneReset/*` |
| `Hooks/IWorldLifecycleHook.cs` | `Modules/SceneReset/Hooks/ISceneResetHook.cs` |
| `Hooks/IWorldLifecycleHookOrdered.cs` | `Modules/SceneReset/Hooks/ISceneResetHookOrdered.cs` |
| `Hooks/WorldLifecycleHookBase.cs` | `Modules/SceneReset/Hooks/SceneResetHookBase.cs` |
| `Hooks/WorldLifecycleHookRegistry.cs` | `Modules/SceneReset/Hooks/SceneResetHookRegistry.cs` |
| `Spawn/*` | `Modules/SceneReset/Spawn/*` |

## Permanecer como ResetInterop (bridge/superfície)

| Atual | Destino novo |
|---|---|
| `Runtime/WorldLifecycleSceneFlowResetDriver.cs` | `Modules/ResetInterop/Runtime/WorldLifecycleSceneFlowResetDriver.cs` |
| `Runtime/WorldLifecycleResetStartedEvent.cs` | `Modules/ResetInterop/Runtime/WorldLifecycleResetStartedEvent.cs` |
| `Runtime/WorldLifecycleResetCompletedEvent.cs` | `Modules/ResetInterop/Runtime/WorldLifecycleResetCompletedEvent.cs` |
| `Runtime/WorldLifecycleResetV2Events.cs` | `Modules/ResetInterop/Runtime/WorldLifecycleResetV2Events.cs` |
| `Runtime/WorldLifecycleResetCompletionGate.cs` | `Modules/ResetInterop/Runtime/WorldLifecycleResetCompletionGate.cs` |
| `Runtime/WorldLifecycleTokens.cs` | `Modules/ResetInterop/Runtime/WorldLifecycleTokens.cs` |

---

# Namespace alvo

## WorldReset
Prefixo alvo:
`_ImmersiveGames.NewScripts.Modules.WorldReset`

Exemplos:
- `_ImmersiveGames.NewScripts.Modules.WorldReset.Runtime`
- `_ImmersiveGames.NewScripts.Modules.WorldReset.Application`
- `_ImmersiveGames.NewScripts.Modules.WorldReset.Domain`
- `_ImmersiveGames.NewScripts.Modules.WorldReset.Validation`
- `_ImmersiveGames.NewScripts.Modules.WorldReset.Policies`
- `_ImmersiveGames.NewScripts.Modules.WorldReset.Guards`

## SceneReset
Prefixo alvo:
`_ImmersiveGames.NewScripts.Modules.SceneReset`

Exemplos:
- `_ImmersiveGames.NewScripts.Modules.SceneReset.Bindings`
- `_ImmersiveGames.NewScripts.Modules.SceneReset.Runtime`
- `_ImmersiveGames.NewScripts.Modules.SceneReset.Runtime.SceneReset`
- `_ImmersiveGames.NewScripts.Modules.SceneReset.Runtime.SceneReset.Phases`
- `_ImmersiveGames.NewScripts.Modules.SceneReset.Hooks`
- `_ImmersiveGames.NewScripts.Modules.SceneReset.Spawn`

## ResetInterop
Prefixo alvo:
`_ImmersiveGames.NewScripts.Modules.ResetInterop`

Exemplo:
- `_ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime`

---

# Renomeações de classe/arquivo recomendadas

## Renomear agora

| Atual | Alvo |
|---|---|
| `WorldLifecycleController` | `SceneResetController` |
| `WorldLifecycleController.cs` | `SceneResetController.cs` |
| `WorldLifecycleControllerLocator` | `SceneResetControllerLocator` |
| `WorldLifecycleControllerLocator.cs` | `SceneResetControllerLocator.cs` |
| `WorldLifecycleOrchestrator` | `SceneResetFacade` |
| `WorldLifecycleOrchestrator.cs` | `SceneResetFacade.cs` |
| `IWorldLifecycleHook` | `ISceneResetHook` |
| `IWorldLifecycleHookOrdered` | `ISceneResetHookOrdered` |
| `WorldLifecycleHookBase` | `SceneResetHookBase` |
| `WorldLifecycleHookRegistry` | `SceneResetHookRegistry` |

## Arquivo já compatível com o nome interno

| Atual | Observação |
|---|---|
| `WorldLifecycleSceneResetRunner.cs` | a classe já é `SceneResetRunner`; renomear só o arquivo/caminho |

## Não renomear nesta fase

| Nome | Motivo |
|---|---|
| `WorldReset*` | owner macro / superfície pública do reset |
| `WorldLifecycleSceneFlowResetDriver` | bridge macro com SceneFlow |
| `WorldLifecycleReset*Event` | eventos observáveis já estabilizados |
| `WorldLifecycleTokens` | token observável já consumido por outros módulos |

---

# Ordem segura de execução na IDE

## Passo 1 — SceneReset local
1. mover `Runtime/SceneReset/*` para `Modules/SceneReset/Runtime/SceneReset/*`
2. mover `Bindings/WorldLifecycleSceneResetRunner.cs` para `Modules/SceneReset/Bindings/SceneResetRunner.cs`
3. renomear `WorldLifecycleController` → `SceneResetController`
4. renomear `WorldLifecycleControllerLocator` → `SceneResetControllerLocator`
5. renomear `WorldLifecycleOrchestrator` → `SceneResetFacade`
6. renomear hooks `WorldLifecycle*` → `SceneReset*`
7. mover `Spawn/*` para `Modules/SceneReset/Spawn/*`

## Passo 2 — WorldReset macro
1. mover `WorldRearm/*` para `Modules/WorldReset/*`
2. mover `Runtime/WorldReset*` para `Modules/WorldReset/Runtime/*`
3. mover `IRouteResetPolicy` + `SceneRouteResetPolicy` para `Modules/WorldReset/Policies/*`

## Passo 3 — ResetInterop
1. mover `WorldLifecycleSceneFlowResetDriver` para `Modules/ResetInterop/Runtime/*`
2. mover eventos/tokens/completion gate para `Modules/ResetInterop/Runtime/*`

## Passo 4 — Composition root / referências
1. ajustar registrations
2. ajustar namespaces/using
3. validar boot → menu → gameplay → restart → exit to menu

---

# O que não fazer nesta fase
- não refatorar comportamento do pipeline
- não mexer em ordem de reset
- não alterar eventos públicos além de namespace/localização
- não tocar `SceneComposition`, `LevelFlow`, `SceneFlow`, `Gameplay`

---

# Critério de aceite
A 4.d1 termina quando:
- o código local de reset está concentrado em `Modules/SceneReset`
- o código macro de reset está concentrado em `Modules/WorldReset`
- bridge/eventos/tokens ficam em `Modules/ResetInterop`
- não sobra código local relevante sob `Modules/WorldLifecycle`
- comportamento do runtime continua igual ao já validado por log
