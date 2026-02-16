# Audit — StringsToDirectRefs v1 — Step 03 (FASE 3 Direct-ref-first)

## Resumo
- Reforcei obrigatoriedade de `routeRef` para intents críticos (`to-menu`, `to-gameplay`) no `GameNavigationCatalogAsset` também durante `OnValidate`, para bloquear configuração incompleta ainda no Editor.
- Mantive runtime em fail-fast para críticos sem fallback degradado para `routeId` (via validação crítica + exceção fatal já padronizada no catálogo).
- Reforcei política de Editor no `LevelCatalogAsset`: `OnValidate` agora repropaga exceção após log fatal, impedindo configuração inválida (incluindo níveis críticos sem `routeRef`, ex.: `level.1`, validados via `LevelDefinition.ResolveRouteId`).
- Observabilidade `[OBS][SceneFlow] RouteResolvedVia=AssetRef` foi preservada para rotas críticas e níveis resolvidos por `routeRef`.

## Arquivos tocados
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Bindings/LevelCatalogAsset.cs`

## Evidências de código (critério de saída)

### Críticos `to-menu` / `to-gameplay` com `routeRef` obrigatório + fail-fast
- `GameNavigationCatalogAsset.ValidateCriticalRouteEntryOrFail(...)` exige `routeRef` para intents críticos e falha com `[FATAL][Config]` se ausente/inválido/divergente.
- `GameNavigationCatalogAsset.OnValidate()` agora executa `ValidateCriticalRoutesInEditor()` para barrar asset incompleto no Editor.
- Runtime continua sem fallback degradado para críticos, pois build chama validação crítica antes de resolver rota.

### Levels (mínimo `level.1`) com política equivalente
- `LevelDefinition.ResolveRouteId()` já faz fail-fast quando nível crítico está sem `routeRef`.
- `LevelCatalogAsset.OnValidate()` agora relança (`throw`) após log fatal, bloqueando configuração inválida durante validação de Editor.

### Logs `[OBS]` de resolução via AssetRef
- Navegação crítica: `GameNavigationCatalogAsset.RouteEntry.ResolveRouteId(...)` mantém log `[OBS][SceneFlow] RouteResolvedVia=AssetRef ... intentId='to-menu|to-gameplay' ...` quando `routeRef` está presente.
- Levels: `LevelCatalogAsset.LogResolutionDedupePerFrame(...)` mantém log `[OBS][SceneFlow] RouteResolvedVia=AssetRef levelId='level.1' ...` quando `routeRef` está presente.

## Comandos executados

### 1) Escaneamento de rotas críticas / routeRef / observabilidade
```bash
rg -n "to-menu|to-gameplay|routeRef|RouteResolvedVia=AssetRef" Assets/_ImmersiveGames/NewScripts/Modules
```

Saída (resumo):
- Ocorrências confirmadas em:
  - `GameNavigationCatalogAsset.cs` (validação crítica + log `RouteResolvedVia=AssetRef`).
  - `LevelDefinition.cs` (fail-fast para nível crítico sem `routeRef`).
  - `LevelCatalogAsset.cs` (log `RouteResolvedVia=AssetRef` para níveis).

### 2) Busca por fail-fast utilitário/fatal
```bash
rg -n "Fatal\(|RuntimeFailFastUtility" Assets/_ImmersiveGames/NewScripts
```

Saída:
- Não há `RuntimeFailFastUtility` disponível neste escopo.
- Estratégia aplicada: fail-fast padronizado por exceção fatal (`InvalidOperationException`) + log `[FATAL][Config]` nas validações críticas já existentes.

### 3) Estado do workspace
```bash
git status --short
```

Saída ao final das alterações:
- `M Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Bindings/LevelCatalogAsset.cs`
- `M Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs`
