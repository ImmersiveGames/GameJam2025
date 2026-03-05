# ADR-0024 — LevelCatalog por MacroRoute e Contrato de Seleção de Level Ativo

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-04
- Tipo: Implementação
- Escopo: NewScripts/Modules (LevelFlow, Navigation)

## Resumo

Padronizar a relação **macroRoute -> catálogo de levels** e garantir seleção determinística de level ativo por macro.

## Decisão

1. `LevelDefinition` usa `macroRouteRef` obrigatório como referência canônica da macro rota.
2. `LevelCatalogAsset` mantém caches explícitos:
   - level -> macroRoute
   - macroRoute -> level (único quando não ambíguo)
   - macroRoute -> lista de levels
3. Quando necessário preparar um macro sem snapshot válido, o default é o primeiro level do grupo da macro (`catalog_first`).

## Implementação atual (fonte de verdade: código)

### Vínculo canônico Level -> MacroRoute

- `LevelDefinition.ResolveMacroRouteId()` usa `macroRouteRef` obrigatório e falha rápido quando ausente/inválido.
- `LevelDefinition.IsValid` depende de `levelId` válido + macro route válida.

### Catálogo e caches por macro

- `LevelCatalogAsset.EnsureCache()` constrói `_levelToMacroRouteCache`, `_macroRouteToLevelCache`, `_macroRouteToLevelsCache` e detecta ambiguidade.
- `LevelCatalogAsset.TryGetLevelsForMacroRoute(...)` e `TryGetNextLevelInMacro(...)` expõem fluxo por macro.
- `LevelCatalogAsset.TryResolve(...)` resolve `levelId` para `macroRouteId + contentId + payload`.

### Seleção determinística no prepare macro

- `LevelMacroPrepareService.PrepareAsync(...)` usa snapshot atual se pertencer à macro.
- Se não houver snapshot aplicável, seleciona `levelIds[0]` do catálogo (`source='catalog_first'`) e publica `LevelSelectedEvent` antes do reset local.

## Critérios de aceite (DoD)

- [x] `LevelDefinition` exige `macroRouteRef` para resolução canônica.
- [x] `LevelCatalogAsset` mantém e usa caches por macro route.
- [x] Existe fallback determinístico de seleção de level por macro em `LevelMacroPrepareService`.
- [ ] Hardening: suporte a default explícito por macro (além do primeiro da lista) no asset/contrato.

## Changelog

- 2026-03-04: ADR auditado contra o código; status atualizado para Implementado.
