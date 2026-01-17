# WorldLifecycle Module - Reorganização e Estrutura

## Visão Geral

A pasta `Runtime/` foi reorganizada para separar claramente a infraestrutura core do lifecycle do mundo dos drivers/integrações runtime.

## Estrutura

### `/Runtime` (este diretório)
Contém interfaces, tipos de eventos e utilitários **core** que implementam o ciclo de vida do mundo de forma determinística e independente de plataforma.

**Arquivos:**
- `IWorldResetRequestService.cs` - Interface pública para solicitar resets
- `WorldLifecycleController.cs` - MonoBehaviour de cena que orquestra pedidos de reset
- `WorldLifecycleOrchestrator.cs` - Implementação determinística do fluxo de reset (Acquire Gate → Hooks → Despawn → Spawn → Release Gate)
- `WorldLifecycleResetCompletedEvent.cs` - Evento emitido quando um reset conclui
- `WorldLifecycleResetReason.cs` - Strings padronizadas para reasons de reset
- `WorldLifecycleDirectResetSignatureUtil.cs` - Utilitário para signatures de resets diretos (fora do SceneFlow)
- `WorldLifecycleResetCompletionGate.cs` - Gate que aguarda evento de reset para liberar SceneFlow

### `/Runtime/Drivers` (nova pasta)
Contém drivers e integrações que conectam a infraestrutura core com SceneFlow, GameLoop ou outras subsistemas runtime.

**Arquivos:**
- `WorldLifecycleRuntimeCoordinator.cs` - Driver que observa SceneFlow events e dispara resets via WorldLifecycleController

## Namespace Changes

- **Core files** permanecem em: `_ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime`
- **Driver files** estão agora em: `_ImmersiveGames.NewScripts.Runtime.Drivers.WorldLifecycle`

## Migração (se aplicável)

Se você tem código que importa `WorldLifecycleRuntimeCoordinator` do namespace antigo:

```csharp
// ANTIGO (deprecado):
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
```

**Atualize para:**

```csharp
// NOVO:
using _ImmersiveGames.NewScripts.Runtime.Drivers.WorldLifecycle;
```

## Próximos Passos Recomendados

1. **Consolidação de Signature Utils**: Compare `WorldLifecycleDirectResetSignatureUtil` com `SceneTransitionSignatureUtil` para eliminar duplicação de lógica de normalização.
2. **Testes**: Adicionar testes que validem:
   - Que RuntimeCoordinator emite `WorldLifecycleResetCompletedEvent` quando ScenesReady
   - Que `WorldLifecycleResetCompletionGate` aguarda e libera corretamente
3. **Cleanup**: Remover os arquivos deprecados após validação completa.

## Referências

- **Core Interface**: `IWorldResetRequestService` - implementado por `WorldLifecycleRuntimeCoordinator`
- **Gate Integration**: `WorldLifecycleResetCompletionGate` registra-se em `EventBus<WorldLifecycleResetCompletedEvent>`
- **SceneFlow Bridge**: `WorldLifecycleRuntimeCoordinator` observa `EventBus<SceneTransitionScenesReadyEvent>`
