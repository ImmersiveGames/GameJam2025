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
- [ ] SceneTransitionService publica eventos NewScripts nativamente (NEWSCRIPTS_SCENEFLOW_NATIVE ligado ou config equivalente).
- [ ] QAs do SceneTransitionServiceSmokeQATester passam em produção (commit 2).
- [ ] Nenhum consumidor depende de eventos legados.
- [ ] Remover registro do GlobalBootstrap e deletar `Bridges/LegacySceneFlow`.

### Remoção planejada
- Gatilho: assim que o `SceneTransitionService` NewScripts for a fonte primária dos eventos (flag NEWSCRIPTS_SCENEFLOW_NATIVE ativa e QAs verdes).
- Passos: remover o registro no `GlobalBootstrap`, apagar `Assets/_ImmersiveGames/NewScripts/Bridges/LegacySceneFlow/` e revalidar consumidores.

### QA associado
- `LegacySceneFlowBridgeSmokeQATester` valida que eventos de Scene Flow do legado são refletidos no EventBus do NewScripts.
- O bridge é temporário; remover quando o SceneTransitionService NewScripts for a fonte primária dos eventos (sem dependência do fade/loadscene legado).
