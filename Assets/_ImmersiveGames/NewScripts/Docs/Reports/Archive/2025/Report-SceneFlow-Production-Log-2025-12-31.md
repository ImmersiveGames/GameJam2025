# Report — SceneFlow Production Log (2025-12-31)

Este report contém **recortes** do log usados como evidência para o estado operacional descrito em:

- `README.md`
- `WORLD_LIFECYCLE.md`
- ADR-0009 / ADR-0012 / ADR-0013

> Observação: este arquivo foi arquivado em `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/`.

## Cenário validado

1. Boot global (DI + serviços).
2. Produção: Start → transição `startup` para `MenuScene`.
3. Menu: botão Play → navegação `to_gameplay` com `profile='gameplay'`.
4. Gameplay: `ScenesReady` → WorldLifecycle hard reset (spawn Player + Eater).
5. Pós-gameplay: `GameRunEndedEvent` → pausa + overlay.
6. Restart e ExitToMenu: navegação por bridges e SceneFlow.

## Recortes do log (trechos)

### Registro de SceneFlow + profiles

- `Profile resolvido: name='startup', path='SceneFlow/Profiles/startup'`
- `Profile resolvido: name='gameplay', path='SceneFlow/Profiles/gameplay'`
- `Profile resolvido: name='frontend', path='SceneFlow/Profiles/frontend'`

### Sequência Fade + Loading HUD (ordem)

- `FadeInCompleted → Show`
- `ScenesReady → Update pending`
- `BeforeFadeOut → Hide`
- `Completed → Safety hide`

### Completion gate

- `ISceneTransitionCompletionGate ... WorldLifecycleResetCompletionGate ... timeoutMs=20000`
- `Aguardando completion gate antes do FadeOut`
- `WorldLifecycleResetCompletedEvent recebido ... Prosseguindo para FadeOut`

### WorldLifecycle — reset em gameplay

- `SceneTransitionScenesReady recebido ... profile='gameplay'`
- `Disparando hard reset após ScenesReady`
- `Gate Acquired (WorldLifecycle.WorldReset)`
- `Spawn service started: PlayerSpawnService`
- `Actor spawned: ... Player_NewScripts`
- `Spawn service started: EaterSpawnService`
- `Actor spawned: ... Eater_NewScripts`
- `World Reset Completed`
- `Emitting WorldLifecycleResetCompletedEvent ... reason='ScenesReady/GameplayScene'`

### GameLoop + gating

- `ENTER: Ready` (startup)
- `ENTER: Playing` (gameplay)
- `Action 'Move' liberada (...)`
- `GamePauseCommandEvent(true)` → `Acquire token='state.pause'` → `ENTER: Paused`

### Navegação pós-gameplay

- Restart:
  - `GameResetRequestedEvent recebido -> RequestGameplayAsync`
  - `NavigateAsync ... routeId='to_gameplay' ... profile='gameplay'`
- Exit to Menu:
  - `ExitToMenu recebido -> RequestMenuAsync`
  - `NavigateAsync ... routeId='to_menu' ... profile='frontend'`
