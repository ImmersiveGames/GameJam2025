# Audit — StringsToDirectRefs v1 — Step 05 (P-001 F4.2)

- **Data:** 2026-02-16
- **Escopo:** `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs`
- **Objetivo:** remover heurística de relevância por tokens string e reduzir dependência de matching por ID string no dump QA do SceneFlow.

## Comandos executados

```bash
rg -n "menu|gameplay|postgame|restart|exit" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs
```

```bash
rg -n "RelevantRouteTokens|IsRelevant\(|RouteId\.Value|ToLowerInvariant\(|Contains\(" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs
```

```bash
rg -n "RouteKind|RequiresWorldReset|DebugGetRoutesSnapshot|RouteKindSummary" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs
```

## Evidências pós-correção

1. **Heurística por tokens removida**
   - Não existe mais `RelevantRouteTokens`.
   - Não existe mais método `IsRelevant(...)`.
   - Não existe mais `ToLowerInvariant()`/`Contains(...)` para classificar relevância por nome/ID.

2. **Filtro/dump por string minimizado**
   - `RouteId.Value` não é mais usado no arquivo.
   - Dump agora usa metadados estruturais de `SceneRouteDefinition`:
     - `RouteKind`
     - `RequiresWorldReset`
     - `TargetActiveScene`
   - Mantido uso de `DebugGetRoutesSnapshot()` (catálogo por referência) e logs `[OBS][SceneFlow]`.

3. **Critério direct-ref-first aplicado no tooling Dev**
   - Fonte é o `SceneRouteCatalogAsset` resolvido por referência via DI.
   - Agrupamento/sumário é feito por `RouteKind` e contagem de `RequiresWorldReset`, sem matching por nome de rota.

## Arquivos alterados nesta etapa

- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs`
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-05.md`
