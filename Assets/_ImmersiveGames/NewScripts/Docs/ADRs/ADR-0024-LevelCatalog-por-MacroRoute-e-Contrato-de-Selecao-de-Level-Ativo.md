# ADR-0024 — LevelCatalog por MacroRoute e Contrato de Seleção de Level Ativo

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-05
- Tipo: Implementação
- Escopo: NewScripts/Modules (LevelFlow, Navigation)

## Resumo

O catálogo de levels está modelado por macro rota, com seleção determinística e suporte a default por macro.

## Decisão

- `LevelDefinition` usa `macroRouteRef` como referência obrigatória da macro rota canônica.
- `LevelCatalogAsset` é a fonte de verdade em runtime para:
  - resolver level -> macro route/content/payload;
  - resolver defaults por macro;
  - listar próximos níveis por macro.
- Ambiguidades de macro com múltiplos levels são tratadas por caches dedicados e regras explícitas de fallback/compat.

## Implementação atual (fonte de verdade = código)

- `LevelDefinition.macroRouteRef` é obrigatório e validado em `ResolveMacroRouteId()`.
- `LevelCatalogAsset` mantém caches por macro:
  - `_macroRouteToLevelCache`
  - `_macroRouteToDefaultLevelCache`
  - `_levelToMacroRouteCache`
  - `_macroRouteToLevelsCache`
- Métodos de contrato implementados:
  - `TryResolve(...)`
  - `TryResolveMacroRouteId(...)`
  - `TryGetDefaultLevelId(...)`
  - `TryGetNextLevelInMacro(...)`
  - `TryGetLevelsForMacroRoute(...)`

## Critérios de aceite (DoD)

- [x] Catálogo resolve level para macroRoute/content/payload.
- [x] Default por macro implementado e consultável.
- [x] Navegação de next-level intra-macro suportada por catálogo.
- [x] Contrato explícito `macroRouteRef` obrigatório na definição de level.
- [ ] Hardening: testes automatizados para rotas ambíguas e políticas de default.

## Changelog

- 2026-03-05: seção de decisão e implementação atualizadas para o shape real em código (sem dependência de log).
