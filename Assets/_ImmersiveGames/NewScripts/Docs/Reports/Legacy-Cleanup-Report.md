# Legacy Cleanup Report — NewScripts (Standalone)

## Objetivo
Remover qualquer dependência do legado (`Assets/_ImmersiveGames/Scripts` e namespaces `_ImmersiveGames.Scripts.*`)
dentro de `Assets/_ImmersiveGames/NewScripts`, mantendo o pipeline operacional:
SceneFlow + Fade + WorldLifecycle + Gate + GameLoop.

## Varredura executada (excluindo Docs)
Termos pesquisados:
- `_ImmersiveGames.Scripts`
- `Legacy`
- `Scripts.Scene` / `Scripts.Utils` / `Scripts.Game`
- `using Legacy`
- `FindObjectsOfType`
- `Resources.FindObjects`

## Resultado da varredura
- **0 ocorrências** de `_ImmersiveGames.Scripts` (codebase NewScripts, excluindo Docs).
- **0 ocorrências** de `Legacy` em runtime/QA.
- **0 ocorrências** de `using Legacy`.
- **0 ocorrências** de `Resources.FindObjects`.
- **0 ocorrências** de `FindObjectsOfType` (após ajuste em QA).

## Mudanças recentes aplicadas
- Updated: `Infrastructure/WorldLifecycle/Spawn/QA/WorldMovementPermissionQaRunner.cs`
    - Substituição de `FindObjectsOfType(..., true)` por `FindObjectsByType(..., FindObjectsInactive.Include, FindObjectsSortMode.None)`.
    - Motivo: aderir à API moderna do Unity 6 e evitar uso obsoleto.

- Updated: `Infrastructure/SceneFlow/Loading/SceneFlowLoadingService.cs`
    - Log de HUD ausente rebaixado para **Verbose** (fluxo esperado quando o HUD nasce tarde).
    - Motivo: reduzir warnings falsos durante transições normais.

## Evidência (logs esperados)
- Startup: HUD pode não existir no `Started` e **não** gera warning (apenas verbose).
- Transição: `[Loading] Started → Show pending` → `[Loading] ScenesReady → Update pending` → `[Loading] BeforeFadeOut → Hide` → `[Loading] Completed → Safety hide`.

## Mini changelog
- QA: troca de API de busca de objetos para `FindObjectsByType`.
- Loading HUD: ajuste de severidade de log para evitar warning em fluxo esperado.

## Verificações finais recomendadas
1) Search: `_ImmersiveGames.Scripts` em `Assets/_ImmersiveGames/NewScripts` (excluindo Docs) → 0 results.
2) Search: `FindObjectsOfType` em `Assets/_ImmersiveGames/NewScripts` (excluindo Docs) → 0 results.
