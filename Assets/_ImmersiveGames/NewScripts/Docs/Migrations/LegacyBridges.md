# Legacy Bridges (TEMP)

## LegacySceneFlowBridge (TEMP)

Bridge temporário localizado em `Assets/_ImmersiveGames/NewScripts/Bridges/LegacySceneFlow/LegacySceneFlowBridge.cs`.

### O que faz
- Observa os eventos de transição de cena do pipeline legado e publica os eventos equivalentes no EventBus do NewScripts.
- Converte o contexto legado via reflexão para `SceneTransitionContext` do NewScripts, preenchendo campos seguros.
- Mantém idempotência de registro e suporta descarte via `IDisposable`.

### Por que existe
- Consumidores NewScripts (ex.: `GameReadinessService`, `WorldLifecycleRuntimeDriver`) já reagem aos eventos novos, mas o fluxo de transição ainda é legado.
- Este bridge permite evolução incremental sem alterar o SceneTransitionService legado.

### Checklist de remoção
- [ ] SceneTransitionService publica eventos NewScripts nativamente.
- [ ] Nenhum consumidor depende de eventos legados.
- [ ] Remover registro do GlobalBootstrap e deletar `Bridges/LegacySceneFlow`.

### Remoção planejada
- Gatilho: assim que o `SceneTransitionService` legado passar a publicar eventos NewScripts diretamente, remover este bridge.
- Passos: remover o registro no `GlobalBootstrap`, apagar `Assets/_ImmersiveGames/NewScripts/Bridges/LegacySceneFlow/` e revalidar consumidores.
