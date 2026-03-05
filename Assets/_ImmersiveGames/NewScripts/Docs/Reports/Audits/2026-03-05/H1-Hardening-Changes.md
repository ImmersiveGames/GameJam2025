# H1 Hardening Changes — anti-retrocesso rota/level

Data: 2026-03-05
Escopo: `Assets/_ImmersiveGames/NewScripts/**`
Base: inventário H0 + hardening solicitado (sem pipeline paralelo).

## Resumo

Este pacote H1 endurece caminhos LEGACY/COMPAT/FALLBACK em runtime para impedir regressão de mistura rota/level em produção.

- Em **Strict/Production**: caminhos legados críticos agora fazem fail-fast com anchor `[FATAL][H1]`.
- Em **DEV** (`UNITY_EDITOR`/`DEVELOPMENT_BUILD`/`DebugBuild`): mantém escape hatch explícito com `[WARN][COMPAT]` ou `[WARN][DEGRADED]`.
- Objetivo: preservar diagnósticos em desenvolvimento, mas bloquear retrocesso operacional em produção.

---

## Mudanças por arquivo

## 1) Navigation

### `Modules/Navigation/GameNavigationService.cs`

#### A) `RestartAsync` — bloqueio de `legacy_route_only`
- **Antes:** restart podia cair em `resolveSource='legacy_route_only'` usando apenas `_lastGameplayRouteId`.
- **Agora (Strict/Production):** fail-fast com `[FATAL][H1]` quando branch legacy seria usado.
- **Agora (DEV):** mantém fallback com log explícito `[WARN][COMPAT][NAV]` e contador (`_compatRestartLegacyRouteOnlyCount`).

Anchor de log:
- `[FATAL][H1] [NAV] Restart blocked: legacy_route_only is forbidden...`
- `[WARN][COMPAT][NAV] Restart legacy_route_only fallback ... recommendation='use LevelFlow restart'`

#### B) `StartGameplayRouteAsync` — fallback endurecido
- **Antes:** fallback para `snapshot` ou `last_level_id` em ausência de seleção explícita.
- **Agora (Strict/Production):**
  - `snapshot` só ocorre no branch validado (`snapshot.RouteId == routeId && snapshot.HasLevelId`).
  - `last_level_id` gera fail-fast `[FATAL][H1]` (exige seleção explícita via LevelFlow).
- **Agora (DEV):** fallback permitido com `[WARN][COMPAT][NAV]` + contador (`_compatStartGameplayRouteFallbackCount`).

Anchor de log:
- `[FATAL][H1] [NAV] StartGameplayRouteAsync requires explicit LevelId selection via LevelFlow...`
- `[WARN][COMPAT][NAV] StartGameplayRouteAsync fallback ... source='last_level_id' ...`

#### F) APIs `[Obsolete]` — mitigação anti-uso
- Métodos endurecidos na implementação:
  - `RequestMenuAsync`
  - `RequestGameplayAsync`
  - `StartGameplayAsync(LevelId, ...)`
  - `NavigateAsync(string, ...)`
- **Strict/Production:** bloqueio com `[FATAL][H1]`.
- **DEV:** aviso 1x por sessão `[WARN][LEGACY_API_USED]` (latch estático `_legacyApiWarningEmitted`).

---

### `Modules/Navigation/RestartNavigationBridge.cs`

#### C) fallback operacional do bridge
- **Antes:** se `ILevelFlowRuntimeService` faltasse, bridge chamava `IGameNavigationService.RestartAsync`.
- **Agora (Strict/Production):** fail-fast `[FATAL][H1][NAV][DI]` e sem fallback.
- **Agora (DEV):** fallback mantido com `[WARN][COMPAT][NAV]` e recomendação de corrigir DI.

---

## 2) SceneFlow

### `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs`

#### D) skip silencioso de LevelPrepare
- **Antes:** ausência de provider/serviço resultava em `skipped` e seguia.
- **Agora (Strict/Production):** fail-fast `[FATAL][H1]` (não segue sem LevelPrepare).
- **Agora (DEV):** skip permitido apenas com `[WARN][DEGRADED][SceneFlow]` + recomendação de correção.

Anchor de log:
- `[FATAL][H1] ... MacroLoadingPhase='LevelPrepare' blocked ...`
- `[WARN][DEGRADED][SceneFlow] MacroLoadingPhase='LevelPrepare' skipped (DEV escape hatch)...`

---

## 3) WorldLifecycle

### `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`

#### E) required reset sem serviço crítico
- **Antes:** caminho best-effort podia liberar completion mesmo sem `WorldResetService`.
- **Agora (Strict/Production):** fail-fast `[FATAL][H1][WorldLifecycle]` quando reset é required e serviço falha/ausente; não libera completion por fallback.
- **Agora (DEV):** best-effort mantido com `[WARN][DEGRADED][WorldLifecycle]` e contador (`_degradedFallbackCount`).

Anchor de log:
- `[FATAL][H1][WorldLifecycle] Reset required but WorldResetService missing...`
- `[WARN][DEGRADED][WorldLifecycle] WorldResetService missing in DI (DEV escape hatch)...`

---

## Comportamento final (matriz)

| Cenário | Strict/Production | DEV |
|---|---|---|
| Restart via `legacy_route_only` | Bloqueado (`[FATAL][H1]`) | Permitido com `[WARN][COMPAT][NAV]` |
| `StartGameplayRouteAsync` com `last_level_id` fallback | Bloqueado (`[FATAL][H1]`) | Permitido com `[WARN][COMPAT][NAV]` |
| Restart bridge sem `ILevelFlowRuntimeService` | Bloqueado (`[FATAL][H1][NAV][DI]`) | Permitido com `[WARN][COMPAT][NAV]` |
| `LevelPrepare` skip por DI/serviço ausente | Bloqueado (`[FATAL][H1]`) | Permitido com `[WARN][DEGRADED][SceneFlow]` |
| Reset required sem `WorldResetService` | Bloqueado (`[FATAL][H1][WorldLifecycle]`) | Permitido com `[WARN][DEGRADED][WorldLifecycle]` |
| Uso de API `[Obsolete]` de navegação | Bloqueado (`[FATAL][H1]`) | Warn 1x (`[WARN][LEGACY_API_USED]`) e comportamento legado controlado |

---

## Anchors de log adicionados

- `[FATAL][H1]`
- `[WARN][COMPAT][NAV]`
- `[WARN][DEGRADED][SceneFlow]`
- `[WARN][DEGRADED][WorldLifecycle]`
- `[WARN][LEGACY_API_USED]`

