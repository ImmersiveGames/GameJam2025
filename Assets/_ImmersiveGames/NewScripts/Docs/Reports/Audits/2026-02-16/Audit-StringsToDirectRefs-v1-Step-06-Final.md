# Audit — StringsToDirectRefs v1 — Step 06 (Final) — P-001 F5.1

- **Data:** 2026-02-16
- **Escopo:** `Assets/_ImmersiveGames/NewScripts/**`
- **Objetivo:** validação final do P-001 (Strings -> DirectRefs) para runtime e tooling.

## Comandos executados (mínimos solicitados)

```bash
rg -n "Resources\.Load\(" Assets/_ImmersiveGames/NewScripts --glob '*.cs'
```

```bash
rg -n "RouteId\.Value|SceneRouteId\.From|NavigationIntentId\.FromName" Assets/_ImmersiveGames/NewScripts --glob '*.cs'
```

```bash
rg -n "\"to-menu\"|\"to-gameplay\"|\"victory\"|\"defeat\"|\"restart\"|\"exit-to-menu\"|\"gameover\"" Assets/_ImmersiveGames/NewScripts --glob '*.cs'
```

```bash
rg -n "criticalRequired|ValidateCritical" Assets/_ImmersiveGames/NewScripts --glob '*.cs'
```

## Resumo executivo

1. **Resources.Load em C#**
   - Resultado: **sem ocorrências** no escopo `NewScripts`.
   - Conclusão: `Dev/Editor/QA` não usa `Resources.Load` para catálogo/rotas.

2. **Runtime wiring routeRef-first**
   - `GameNavigationCatalogAsset.RouteEntry.ResolveRouteId(...)` prioriza `routeRef` e só usa `sceneRouteId` como fallback legível quando `routeRef` não existe.
   - `ResolveCoreOrFail(...)` exige `routeRef` válido para core.
   - Conclusão: runtime segue **direct-ref-first** quando `routeRef` está disponível.

3. **Core mandatory intents**
   - Confirmado em validação de catálogo/intents:
     - mandatórios: `to-menu`, `to-gameplay`.
   - `ResolveCriticalCoreIntentsForValidation()` retorna apenas fallback canônico para esses dois.

4. **Extras não críticos**
   - `gameover/victory/defeat/restart/exit-to-menu` estão tratados como opcionais (OBS/WARN), sem fail-fast obrigatório em runtime quando ausentes.

## Achados e classificação

Legenda:
- **OK** = aderente ao objetivo final.
- **AllowedTooling** = uso de string/scan aceitável por ser tooling/editor.
- **NeedsChange** = deveria ser ajustado para concluir P-001 F5.1.

| Item | Local | Classificação | Evidência / Observação |
|---|---|---|---|
| Ausência de `Resources.Load(` | Escopo `NewScripts` | **OK** | `rg` retornou vazio para comando 1. |
| Resolução de rota por referência direta (`routeRef`) com observabilidade `[OBS][SceneFlow] RouteResolvedVia=AssetRef` | `Modules/Navigation/GameNavigationCatalogAsset.cs` | **OK** | `ResolveRouteId` prioriza `routeRef`; core usa `ResolveCoreOrFail` com `routeRef` obrigatório. |
| Core mandatory apenas `to-menu`/`to-gameplay` | `Modules/Navigation/GameNavigationIntentCatalogAsset.cs` e `GameNavigationCatalogAsset.cs` | **OK** | `EnsureCoreIntentsForProductionOrFail` exige só menu/gameplay; validação crítica editor retorna apenas fallback canônico de dois slots. |
| Extras opcionais não-fatais | `Modules/Navigation/GameNavigationIntentCatalogAsset.cs` e `GameNavigationCatalogAsset.cs` | **OK** | extras com configuração parcial/ausente geram OBS/WARN; cache trata extras como `required:false`. |
| `NavigationIntentId.FromName("...")` em runtime navigation | `Modules/Navigation/GameNavigationCatalogAsset.cs` | **AllowedTooling** | Usado para canonicalização/fallback de IDs; não é wiring de rota quando `routeRef` existe e não introduz `Resources.Load`. |
| `NavigationIntentId.FromName("...")` e strings canônicas de intent | `Modules/Navigation/GameNavigationIntentCatalogAsset.cs` | **AllowedTooling** | Definição de canônicos no catálogo de intents (contrato). |
| `RouteId.Value` em providers editoriais | `Modules/SceneFlow/Editor/IdSources/SceneRouteIdSourceProvider.cs` | **AllowedTooling** | Provider de IDs para drawers/editor; não runtime. |
| `criticalRequired` legado presente em serialização | `Modules/Navigation/GameNavigationIntentCatalogAsset.cs` + normalizer | **AllowedTooling** | Campo legado preservado para compatibilidade; criticidade efetiva está fixa em menu/gameplay. |

## Veredito final F5.1

- **Status geral:** ✅ **Conforme objetivo do P-001 F5.1** no escopo auditado.
- **NeedsChange:** **nenhum** nesta etapa.
- **Patch mínimo proposto:** não aplicável (sem pendência crítica identificada).
