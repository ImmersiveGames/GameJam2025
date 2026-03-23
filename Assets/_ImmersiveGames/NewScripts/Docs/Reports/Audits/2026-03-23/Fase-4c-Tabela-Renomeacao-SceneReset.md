# Fase 4.c — Tabela de Renomeação do Reset Local (`SceneReset*`)

## Objetivo

Aplicar na IDE a limpeza de naming do trilho **local** do reset, sem mexer no contrato **macro** de `WorldReset`.

## Baseline real desta tabela

Esta tabela foi revisada em cima do estado atual do módulo, que **já contém** o pipeline local explícito em:

- `Runtime/SceneReset/SceneResetContext.cs`
- `Runtime/SceneReset/SceneResetPipeline.cs`
- `Runtime/SceneReset/ISceneResetPhase.cs`
- `Runtime/SceneReset/Phases/*`

Por isso, a renomeação **não** pode reutilizar nomes que já existem nesse namespace.

## Regra principal

- `WorldReset*` = fluxo macro / API pública / integração com `SceneFlow`
- `SceneReset*` = trilho local determinístico por cena
- `WorldLifecycle*` = legado de naming que ainda sobrou na superfície local

## Tabela de renomeação

| Alvo atual real | Nome alvo | Papel real hoje | Tipo de mudança | Status recomendado |
|---|---|---|---|---|
| `WorldLifecycleController` | `SceneResetController` | fila e lifecycle do reset local por cena | classe + arquivo | renomear |
| `WorldLifecycleSceneResetRunner.cs` *(arquivo)* / `SceneResetRunner` *(classe atual)* | `SceneResetRunner.cs` | montagem das dependências efêmeras do trilho local | **arquivo apenas** | renomear |
| `WorldLifecycleOrchestrator` | `SceneResetFacade` | façade fina que delega ao pipeline local já existente | classe + arquivo | renomear |
| `WorldLifecycleControllerLocator` | `SceneResetControllerLocator` | resolução dos controllers ativos da cena | classe + arquivo | renomear |
| `WorldLifecycleHookRegistry` | `SceneResetHookRegistry` | registro dos hooks do reset local | classe + arquivo | avaliar junto |
| `IWorldLifecycleHook` | `ISceneResetHook` | contrato de hook do reset local | interface + arquivo | avaliar junto |
| `IWorldLifecycleHookOrdered` | `ISceneResetHookOrdered` | ordenação de hooks do reset local | interface + arquivo | avaliar junto |
| `WorldLifecycleHookBase` | `SceneResetHookBase` | base opcional para hooks do reset local | classe + arquivo | avaliar junto |
| `WorldLifecycleResetStartedEvent` | **manter** | evento público canônico do reset observado pelo restante do sistema | sem mudança | manter por enquanto |
| `WorldLifecycleResetCompletedEvent` | **manter** | evento público canônico do reset observado pelo restante do sistema | sem mudança | manter por enquanto |
| `WorldLifecycleSceneFlowResetDriver` | **manter** | bridge `SceneFlow -> WorldReset` | sem mudança | manter |
| `WorldLifecycleResetCompletionGate` | **manter** | gate observado pelo pipeline macro | sem mudança | manter |

## Sequência recomendada na IDE

1. Renomear `WorldLifecycleController` -> `SceneResetController`
2. Renomear o **arquivo** `WorldLifecycleSceneResetRunner.cs` -> `SceneResetRunner.cs`
3. Renomear `WorldLifecycleOrchestrator` -> `SceneResetFacade`
4. Renomear `WorldLifecycleControllerLocator` -> `SceneResetControllerLocator`
5. Avaliar hooks (`HookRegistry`, interfaces e base) no mesmo bloco
6. **Não** renomear nesta fase os eventos públicos de reset nem o driver do `SceneFlow`

## O que não entra nesta fase

- `WorldResetService`
- `WorldResetOrchestrator`
- `WorldResetExecutor`
- `IWorldResetCommands`
- `WorldLifecycleSceneFlowResetDriver`
- `WorldLifecycleResetStartedEvent`
- `WorldLifecycleResetCompletedEvent`
- `Runtime/SceneReset/SceneResetPipeline`
- `Runtime/SceneReset/SceneResetContext`
- `Runtime/SceneReset/ISceneResetPhase`
- `Runtime/SceneReset/Phases/*`

## Motivo da separação

A camada macro já está bem identificada como `WorldReset`. O que ainda está com naming ruim é a **superfície local** da cena. Como o pipeline real já existe em `Runtime/SceneReset/*`, o trabalho desta fase é alinhar os nomes da camada de entrada/compatibilidade com a tarefa real — sem colidir com o pipeline que já foi criado.
