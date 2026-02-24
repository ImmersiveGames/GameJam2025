# Plano (P-003) — Navigation: Play Button → `to-gameplay`

## Status

- ActivityId: **P-003**
- Estado: **DONE**
- Última atualização: **2026-02-17**

### Fonte de verdade (referências)

- Contrato canônico: `Docs/Standards/Standards.md#observability-contract`
- Evidência vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

### Evidência / auditoria relacionada

- `Docs/Reports/Audits/2026-02-11/Audit-NavigationRuntime-Mismatch.md` (investigação do sintoma "Entries: []" e riscos de catálogo/Resources)

## Objetivo
Corrigir erro no Play (`routeId='to-gameplay'`) com mudança mínima, robusta e evidência de runtime (DI + resolver).

## Checklist rastreável

- [x] Mapear fluxo Play (`MenuPlayButtonBinder`) até `GameNavigationService.ExecuteIntentAsync`.
- [x] Confirmar condições do log `[Navigation] Rota desconhecida ou sem request`.
- [x] Validar assets em `Resources` usados no DI (`GameNavigationCatalog`, `SceneRouteCatalog`, `TransitionStyleCatalog`).
- [x] Aplicar correção mínima para compatibilidade de serialização do catálogo de navegação.
- [x] Adicionar log `[OBS]` de wiring/runtime (`catalogType`, `resolverType`, `TryResolve('to-gameplay')`).
- [x] Validar por inspeção estática + checklist de logs esperados.

### Artefatos esperados

- Auditoria (CODEX read-only): `Docs/Reports/Audits/<YYYY-MM-DD>/Audit-PlayButton-ToGameplay.md`
- Evidência (runtime): snapshot em `Docs/Reports/Evidence/<YYYY-MM-DD>/...` + atualização de `Docs/Reports/Evidence/LATEST.md`

## Critério de sucesso
- `MenuPlayButtonBinder` chama `StartGameplayAsync(...)` (e dispara intent `to-gameplay`).
- `GameNavigationCatalogAsset.TryGet("to-gameplay", ...)` retorna entry válido.
- `GameNavigationService` deixa de logar erro de rota desconhecida para `to-gameplay`.
- Boot registra observabilidade `[OBS][Navigation] ... tryResolve('to-gameplay')=True`.

## Evidência de smoke (2026-02-17)

- Fonte: `Docs/Reports/lastlog.log`
- Trecho relevante (PlayButton → Gameplay):

```log
[MenuPlayButtonBinder] [OBS][LevelFlow] MenuPlay -> StartGameplayAsync levelId='level.1' reason='Menu/PlayButton'.
[GameNavigationService] [OBS][Navigation] DispatchIntent -> intentId='to-gameplay', sceneRouteId='level.1', styleId='style.gameplay', reason='Menu/PlayButton'
[SceneTransitionService] [SceneFlow] TransitionStarted id=2 ... routeId='level.1' ... reason='Menu/PlayButton'
[SceneTransitionService] [OBS][SceneFlow] RouteExecutionPlan routeId='level.1' activeScene='GameplayScene' toLoad=[GameplayScene, UIGlobalScene] toUnload=[NewBootstrap, MenuScene]
```

## Observação histórica

- A auditoria de origem do bloqueio permanece registrada em `Docs/Reports/Audits/2026-02-11/Audit-NavigationRuntime-Mismatch.md`.
