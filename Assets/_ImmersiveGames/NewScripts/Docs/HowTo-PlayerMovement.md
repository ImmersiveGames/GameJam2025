# HowTo — Player Movement (NewScripts)

## Visão geral
- Stack mínimo e determinístico para o Player no **NewScripts**.
- Componentes principais (namespace `_ImmersiveGames.NewScripts.Gameplay.Player.Movement`):
  - `NewPlayerInputReader` — lê `Input.GetAxis`/`GetAxisRaw` (Horizontal/Vertical) com deadzone e clamp.
  - `NewPlayerMovementController` — aplica deslocamento e rotação usando `CharacterController`, `Rigidbody` ou `Transform` (fallback).
- Gate-aware: respeita `ISimulationGateService` (Scene Flow/Resets) e `IStateDependentService` (Pause/GameLoop). Input e deslocamento param automaticamente quando o gate fecha.
- Reset-safe: sem estado estático e com limpeza de bindings em `OnDisable`/`OnDestroy` e nos ciclos de reset.

## Onde estão os scripts
```
Assets/_ImmersiveGames/NewScripts/Gameplay/Player/Movement/
  ├─ NewPlayerInputReader.cs
  └─ NewPlayerMovementController.cs
```
O spawn garante os componentes no Player via `PlayerSpawnService` (Infrastructure/World).

## Configuração rápida (Inspector)
No prefab **Player_NewScripts** ou no objeto instanciado:
- `Move Speed` — unidades por segundo (default: `5`).
- `Rotation Speed` — graus por segundo para alinhar com a direção do movimento (default: `360`).
- `Input Deadzone` — deadzone extra além da aplicada pelo `NewPlayerInputReader` (default: `0.1`).
- `Use Fixed Update For Physics` — habilite se estiver usando `Rigidbody` (default: ligado). Em `CharacterController`/`Transform` o movimento roda no `Update`.
- Campo `Input Reader` referencia automaticamente o `NewPlayerInputReader` do mesmo GameObject (adicionado no spawn se faltar).

## Fluxo de gate/reset
- Gate fecha com tokens do `ISimulationGateService` (ex.: `SimulationGateTokens.SceneTransition`, `SimulationGateTokens.SoftReset`, `SimulationGateTokens.Pause`).
- Enquanto o gate estiver fechado ou `IStateDependentService.CanExecuteAction(ActionType.Move)` retornar `false`, o input é ignorado e a velocidade horizontal é zerada (mantendo componente Y do `Rigidbody`).
- Durante reset (`WorldLifecycle`), o controlador participa do ciclo `Cleanup/Restore/Rebind` para zerar inputs e restaurar pose inicial.

## Validação manual em Play Mode
1. Abra uma cena com o prefab **Player_NewScripts** ou deixe o fluxo de spawn padrão instanciar o Player (Scene Flow/WorldLifecycle).
2. Pressione Play e use os eixos padrão (`Horizontal`/`Vertical`) para mover. Se houver `CharacterController`, ele será usado; se não, o `Rigidbody` (ou `Transform.Translate`) assume.
3. Para validar o gate:
   - (Opcional) Em um `MonoBehaviour` temporário, adquira um token: `DependencyManager.Provider.TryGetGlobal(out ISimulationGateService gate); using var handle = gate?.Acquire(SimulationGateTokens.SceneTransition);`
   - Verifique que o Player para de receber input/movimentar com o gate fechado e retoma ao liberar o token (`handle.Dispose()`).
4. Para testar reset determinístico, execute um hard reset pelo `WorldLifecycleOrchestrator`/`WorldLifecycleController`: o Player é despawnado/respawnado e continua movendo normalmente.

## Notas
- Projeto alvo: **Unity 6**, multiplayer local. Nenhum estado estático é persistido entre spawns/resets.
- Logs verbosos usam tags `[Movement]`/`[Gate]`/`[StateDependent]` e respeitam o `DebugLevel` configurado.
