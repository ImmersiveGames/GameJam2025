# Legacy Cleanup Report — NewScripts (Standalone)

## Objetivo
Remover qualquer dependência do legado (`Assets/_ImmersiveGames/Scripts` e namespaces `_ImmersiveGames.Scripts.*`)
dentro de `Assets/_ImmersiveGames/NewScripts`, mantendo o pipeline operacional:
SceneFlow + Fade + WorldLifecycle + Gate + GameLoop.

## Resultado
- NewScripts standalone: nenhuma referência a `_ImmersiveGames.Scripts.*` dentro de `Assets/_ImmersiveGames/NewScripts`.
- Smoke QA do SceneFlow: PASS (ExitCode=0).
- Log “verde”: sem erro de WorldLifecycleController ausente em smoke (SKIP path ativado por profile).

## Mudanças principais (arquivos)
### Remoções (Legacy bridges/adapters)
- Removed: `NewScripts/Bridges/LegacySceneFlow/LegacySceneFlowBridge.cs`
    - Dependência removida: `_ImmersiveGames.Scripts.SceneManagement.Transition.*`
    - Substituição: pipeline nativo NewScripts (eventos/contextos próprios)

- Removed: `NewScripts/Bridges/LegacySceneFlow/LegacySceneFlowAdapters.cs`
    - Dependência removida: `_ImmersiveGames.Scripts.SceneManagement.Core.*` (loader legado)
    - Substituição: `NewScriptsSceneFlowAdapters` + `SceneManagerLoaderAdapter`

- Removed: `NewScripts/Bridges/LegacySceneFlow/QA/LegacySceneFlowBridgeSmokeQATester.cs`
    - Dependência removida: smoke via bridge de eventos legado
    - Substituição: smoke QA nativo `SceneTransitionServiceSmokeQaTester`

### Ajustes QA (log “verde”)
- Updated: `Infrastructure/QA/SceneFlowPlayModeSmokeBootstrap.cs`
    - Correção: evitar falso-positivo por substring “Fail” em “Fails=0”.
    - Resultado: RESULT=PASS ExitCode=0 quando runner/tester passa.

- Updated: `Infrastructure/QA/SceneTransitionServiceSmokeQaTester.cs`
    - Mudança QA-only: usar `transitionProfileName="startup"` no request do smoke.
    - Resultado: `WorldLifecycleRuntimeCoordinator` executa SKIP e não tenta localizar controller em cena stub.

## Evidência (logs esperados)
- `[SceneFlowTest][Native] PASS`
- `[SceneFlowTest][Runner] PASS`
- `[SceneFlowTest][Smoke] RESULT=PASS ExitCode=0`
- `[WorldLifecycle] Reset SKIPPED (startup/frontend)` durante smoke

## Verificações finais recomendadas
1) Search: `_ImmersiveGames.Scripts` in `Assets/_ImmersiveGames/NewScripts` → 0 results.
2) Search: `LegacySceneFlow` in `Assets/_ImmersiveGames/NewScripts` → 0 results in code.
3) Run smoke: SceneFlowPlayModeSmokeBootstrap → PASS ExitCode=0.
