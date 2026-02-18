# Plano (P-004) — Validação (Codex): SceneFlow / Navigation / RouteResetPolicy

## Status

- ActivityId: **P-004**
- Estado: **DONE**
- Última atualização: **2026-02-18**

### Fonte de verdade (referências)

- ADRs: `Docs/ADRs/` (principalmente decisões de SceneFlow/Navigation/LevelFlow)
- Contrato canônico: `Docs/Standards/Standards.md#observability-contract`
- Evidência vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

### Artefatos de fechamento (evidência real)

- Audit datado: `Docs/Reports/Audits/2026-02-17/Audit-SceneFlow-RouteResetPolicy.md`
- Smoke: `Docs/Reports/lastlog.log`
- Validator PASS: `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`

## Checklist rastreável

- [x] Confirmar contrato de `IGameNavigationService` e wrappers legados `[Obsolete]`.
- [x] Auditar call-sites legados (`RequestMenuAsync`, `RequestGameplayAsync`, `NavigateAsync`).
- [x] Confirmar evidência de `routePolicy:Frontend` no smoke.
- [x] Confirmar evidência de `routePolicy:Gameplay` no smoke.
- [x] Confirmar ausência de `policy:missing` no smoke.
- [x] Confirmar `VERDICT: PASS` no validator de SceneFlow Config.

## Evidências usadas no fechamento

- Âncoras no smoke (`Docs/Reports/lastlog.log`):
  - `[OBS][WorldLifecycle] ResetPolicy routeId='to-menu' ... decisionSource='routePolicy:Frontend'`
  - `[OBS][WorldLifecycle] ResetPolicy routeId='level.1' ... decisionSource='routePolicy:Gameplay'`
- Não há ocorrência de `policy:missing` no smoke usado para fechamento.
- O report de validação de configuração está em PASS.

## Comandos de prova (CLI)

- `rg -n "ResetPolicy routeId='to-menu'|routePolicy:Frontend" Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log`
- `rg -n "ResetPolicy routeId='level.1'|routePolicy:Gameplay" Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log`
- `rg -n "policy:missing" Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log`
- `rg -n "VERDICT:" Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`

## Histórico (resolvido)

- **Ordem de DI/pipeline:** cenário de `ISceneRouteResolver` ausente no caminho canônico foi tratado via registro obrigatório de rotas antes do SceneFlow native + fail-fast quando resolver não existe.
- **Decisão de reset:** `SceneRouteResetPolicy` opera por rota (`RouteKind`/`RequiresWorldReset`) e o smoke confirma decisões `routePolicy:Frontend` e `routePolicy:Gameplay`.
- **Regressão de catálogo:** não há fallback canônico por `Resources.Load` para catálogo/resolver de rota no fluxo validado.

## Evidências

- `Docs/Reports/lastlog.log`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- `Docs/Reports/Audits/2026-02-17/Audit-SceneFlow-RouteResetPolicy.md`
- `Docs/Plans/Codex-Validation-SceneFlow-RouteResetPolicy.md`
