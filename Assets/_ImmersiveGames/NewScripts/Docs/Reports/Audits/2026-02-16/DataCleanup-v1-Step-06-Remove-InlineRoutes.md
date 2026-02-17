# DataCleanup v1 — Step 06 (Remove Inline Routes)

## Objetivo
Remover definitivamente o legado inline routes do `SceneRouteCatalogAsset`, mantendo o catálogo operando exclusivamente com `routeDefinitions` (referências diretas).

## Impacto
### O que muda
- O campo serializado `routes[]` foi removido do `SceneRouteCatalogAsset`.
- O runtime/editor não possui mais qualquer fluxo de leitura/validação/fallback baseado em rotas inline.
- A validação do SceneFlow passa a verificar consistência de `routeDefinitions` sem política de inline routes.

### O que não muda
- A API pública do catálogo (`TryGet`, `DebugGetRoutesSnapshot`) continua disponível.
- O modelo canônico por `routeDefinitions` permanece como única fonte de verdade.
- O validator continua gerando report com `VERDICT: PASS/FAIL` e mantém fail-fast com FATAL ao final.

## Evidências (CLI)
- `rg -n "\broutes\b" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs || true`
- `rg -n "Inline routes|inline routes|routes\[\]" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs || true`
- `rg -n "Reserialize SceneFlow Assets \(DataCleanup v1\)" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/Validation -g '*.cs' || true`

## Validação manual no Unity
1. Executar menu: `ImmersiveGames/NewScripts/Config/Validate SceneFlow Config (DataCleanup v1)`.
2. (Se aplicável) Executar menu: `ImmersiveGames/NewScripts/Config/Reserialize SceneFlow Assets (DataCleanup v1)`.
3. Confirmar que o report é gerado e que configurações inválidas continuam resultando em FATAL.
