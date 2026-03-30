# Fase 4.e — Tabela de Renomeação da Superfície de ResetInterop

## Objetivo
Eliminar o naming residual `WorldLifecycle*` da superfície pública de interop, sem mudar comportamento.

## Estado atual confirmado
Arquivos atuais em `Modules/ResetInterop/Runtime/`:
- `WorldLifecycleResetStartedEvent.cs`
- `WorldLifecycleResetCompletedEvent.cs`
- `WorldLifecycleResetEvents.cs`
- `WorldLifecycleResetCompletionGate.cs`
- `WorldLifecycleSceneFlowResetDriver.cs`
- `WorldLifecycleTokens.cs`

## Regra desta fase
- Aplicar renomeações na IDE
- Não mexer em comportamento
- Não mexer em `WorldReset` nem em `SceneReset`
- Não remover eventos/tokens/gate/driver; apenas alinhar naming

## Renomeações recomendadas

| Atual | Alvo | Tipo | Aplicar agora | Observação |
|---|---|---|---|---|
| `WorldLifecycleSceneFlowResetDriver` | `SceneFlowWorldResetDriver` | classe + arquivo | sim | bridge entre `SceneFlow` e `WorldReset` |
| `WorldLifecycleResetStartedEvent` | `WorldResetStartedEvent` | classe + arquivo | sim | evento público macro |
| `WorldLifecycleResetCompletedEvent` | `WorldResetCompletedEvent` | classe + arquivo | sim | evento público macro |
| `WorldLifecycleResetEvents` | `WorldResetEvents` | classe + arquivo | sim | agrupador/helper de eventos |
| `WorldLifecycleResetCompletionGate` | `WorldResetCompletionGate` | classe + arquivo | sim | gate da conclusão do reset |
| `WorldLifecycleTokens` | `WorldResetTokens` | classe + arquivo | sim | tokens da superfície de reset |

## Arquivos de Composition Root para revisar na IDE
Esses arquivos devem ser revisados logo após as renomeações acima:
- `Infrastructure/Composition/GlobalCompositionRoot.WorldLifecycle.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs`

### Renomeações recomendadas
| Atual | Alvo |
|---|---|
| `GlobalCompositionRoot.WorldLifecycle.cs` | `GlobalCompositionRoot.WorldReset.cs` |
| `GlobalCompositionRoot.SceneFlowWorldLifecycle.cs` | `GlobalCompositionRoot.SceneFlowWorldReset.cs` |

## Ordem segura na IDE
1. Renomear eventos (`Started`, `Completed`, `Events`)
2. Renomear `WorldLifecycleResetCompletionGate`
3. Renomear `WorldLifecycleTokens`
4. Renomear `WorldLifecycleSceneFlowResetDriver`
5. Ajustar partials do `GlobalCompositionRoot`
6. Recompilar
7. Rodar fluxo:
   - boot -> menu
   - menu -> gameplay
   - restart
   - exit to menu

## O que não renomear nesta fase
- `IWorldResetCommands`
- `IWorldResetService`
- `WorldResetCommands`
- `WorldResetService`
- `WorldResetOrchestrator`
- `WorldResetExecutor`
- `IWorldResetGuard`
- `SimulationGateWorldResetGuard`
- qualquer arquivo em `SceneReset`

## Critério de aceite
- Não sobra `WorldLifecycle*` em `Modules/ResetInterop/Runtime/`
- Os partials do composition root param de usar `WorldLifecycle` no nome do arquivo
- O runtime continua emitindo:
  - evento de started/completed
  - gate de completion
  - driver de SceneFlow
- Sem regressão em boot/gameplay/restart/exit
