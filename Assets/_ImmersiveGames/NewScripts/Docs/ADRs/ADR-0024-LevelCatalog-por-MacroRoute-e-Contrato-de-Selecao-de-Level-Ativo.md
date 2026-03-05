# ADR-0024 - LevelCatalog por MacroRoute e Contrato de Selecao de Level Ativo

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Modules (LevelFlow, Navigation)

## Resumo

Padronizar a relacao **macroRoute -> catalogo de levels** e garantir selecao deterministica de level ativo por macro.

## Decisao

1. `LevelDefinition` usa `macroRouteRef` obrigatorio como referencia canonica da macro rota.
2. `LevelCatalogAsset` mantem caches explicitos:
   - level -> macroRoute
   - macroRoute -> level (unico quando nao ambiguo)
   - macroRoute -> lista de levels
3. Sem snapshot valido para o macro, o default e o primeiro level do grupo (`catalog_first`).

## Implementacao atual (fonte de verdade: codigo)

### Vinculo canonico Level -> MacroRoute

- `LevelDefinition.ResolveMacroRouteId()` usa `macroRouteRef` obrigatorio e falha rapido quando ausente/invalido.
- `LevelDefinition.IsValid` depende de `levelId` valido + macro route valida.

### Catalogo e caches por macro

- `LevelCatalogAsset.EnsureCache()` constroi `_levelToMacroRouteCache`, `_macroRouteToLevelCache`, `_macroRouteToLevelsCache` e detecta ambiguidade.
- `LevelCatalogAsset.TryGetLevelsForMacroRoute(...)` e `TryGetNextLevelInMacro(...)` expoem fluxo por macro.
- `LevelCatalogAsset.TryResolve(...)` resolve `levelId` para `macroRouteId + contentId + payload`.

### Selecao deterministica no prepare macro

- `LevelMacroPrepareService.PrepareAsync(...)` usa snapshot atual se pertencer a macro.
- Sem snapshot aplicavel, seleciona `levelIds[0]` (`source='catalog_first'`) e publica `LevelSelectedEvent` antes do reset local.

## Criterios de aceite (DoD)

- [x] `LevelDefinition` exige `macroRouteRef` para resolucao canonica.
- [x] `LevelCatalogAsset` mantem e usa caches por macro route.
- [x] Existe fallback deterministico de selecao de level por macro em `LevelMacroPrepareService`.
- [ ] Hardening: suporte a default explicito por macro no asset/contrato.

## Changelog

- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
- 2026-03-04: ADR auditado contra o codigo; status atualizado para Implementado.
