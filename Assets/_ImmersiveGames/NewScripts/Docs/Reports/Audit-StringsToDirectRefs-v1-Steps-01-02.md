# Audit — StringsToDirectRefs v1 (Etapas 1 e 2)

- Data/hora (UTC): 2026-02-16T01:49:51+00:00
- Branch: `work`
- Commit: `2e7906d1c0c23e9c7a5cc13d206b19fa74229fa1`
- Plano-base localizado: `Assets/_ImmersiveGames/NewScripts/Docs/Plans/Plan-Continuous.md#p-002`
- Auditoria anterior localizada: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-DataCleanup-Post-StringsToDirectRefs-v1.md`

## Sumário
A Etapa 1 está **majoritariamente aplicada**: há PropertyDrawers Editor-only para `SceneRouteId`, `TransitionStyleId` e `SceneFlowProfileId`, alimentados por Source Providers com coleta por catálogos/assets e validação de duplicidade/ausência; a serialização dos IDs permanece baseada em `string` encapsulada em structs. O ponto que impede 100% de aderência no checklist da Etapa 1 é a exigência “sem dependência de UnityEditor em runtime assemblies”: embora não haja `asmdef` separando assembly de runtime/editor e as referências estejam protegidas por `#if UNITY_EDITOR`, há ocorrências de `UnityEditor.EditorApplication` em scripts fora de pasta `Editor`. A Etapa 2 está **parcial**: existem constantes canônicas de intents (`GameNavigationIntents`), porém o intent ainda é `string` (`routeId`) no catálogo e nos call sites principais, sem `GameNavigationIntentId` tipado.

## Tabela de status
| Etapa | Status | Observação curta |
|---|---|---|
| Etapa 1 — PropertyDrawers + Source Providers | **PARTIAL** | A/B/C/E aplicados; D parcial (dependência `UnityEditor` em arquivos de runtime, ainda que sob `#if UNITY_EDITOR`). |
| Etapa 2 — IntentId tipado | **PARTIAL** | Apenas constantes canônicas presentes; tipagem forte do intent no catálogo/runtime não encontrada. |

## Etapa 1 — Checklist + evidências

- [x] **A) Existem PropertyDrawers (Editor-only) para `SceneRouteId`, `TransitionStyleId`, `SceneFlowProfileId`.**
  - **Evidence:**
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdDrawers/SceneRouteIdDrawer.cs` — símbolo `SceneRouteIdDrawer` com `[CustomPropertyDrawer(typeof(SceneRouteId))]`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdDrawers/TransitionStyleIdDrawer.cs` — símbolo `TransitionStyleIdDrawer` com `[CustomPropertyDrawer(typeof(TransitionStyleId))]`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdDrawers/SceneFlowProfileIdDrawer.cs` — símbolo `SceneFlowProfileIdDrawer` com `[CustomPropertyDrawer(typeof(SceneFlowProfileId))]`.

- [x] **B) Drawers exibem dropdown com valores de Source Providers (Editor-only).**
  - **Evidence:**
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdDrawers/SceneFlowTypedIdDrawerBase.cs` — usa `EditorGUI.Popup(...)` e `CollectSource()` para montar opções.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdSources/ISceneFlowIdSourceProvider.cs` — contrato de providers.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdSources/SceneRouteIdSourceProvider.cs` — coleta via `AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset")` e `AssetDatabase.FindAssets("t:SceneRouteCatalogAsset")`; lê `routeDefinitions` e `routes`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdSources/TransitionStyleIdSourceProvider.cs` — coleta via `AssetDatabase.FindAssets("t:TransitionStyleCatalogAsset")`; lê `styles[].styleId._value`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdSources/SceneFlowProfileIdSourceProvider.cs` — agrega canônicos (`Startup/Frontend/Gameplay`) + `TransitionStyleCatalogAsset.styles[].profileId._value` + `SceneTransitionProfileCatalogAsset._entries[]._profileId._value`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdSources/SceneFlowIdSourceUtility.cs` — normalização + rastreio de duplicidades.

- [x] **C) Serialização continua STRING (drawer melhora apenas UX).**
  - **Evidence:**
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Runtime/SceneRouteId.cs` — struct serializável com `[SerializeField] private string _value`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Runtime/TransitionStyleId.cs` — struct serializável com `[SerializeField] private string _value`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Runtime/SceneFlowProfileId.cs` — struct serializável com `[SerializeField] private string _value`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/IdDrawers/SceneFlowTypedIdDrawerBase.cs` — escreve em `rawValueProperty.stringValue` no campo relativo `_value`.

- [~] **D) Não há dependência de UnityEditor em assemblies runtime (pastas/asmdef/Editor).**
  - **Evidence (parcial):**
    - Pastas de tooling de IDs estão em escopo Editor: `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/...`.
    - Não foram encontrados arquivos `.asmdef` em `Assets/_ImmersiveGames/NewScripts`, logo não há isolamento explícito de assembly por definição.
    - Há uso de `UnityEditor.EditorApplication.isPlaying` fora de pasta `Editor`, ainda que protegido por `#if UNITY_EDITOR`:
      - `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Bindings/MenuQuitButtonBinder.cs`
      - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - **Conclusão do item D:** **Parcial**, por ausência de separação por asmdef + referências condicionais em scripts de runtime.

- [x] **E) Arquivos envolvidos e símbolos principais listados.**
  - **Drawers:** `SceneRouteIdDrawer`, `TransitionStyleIdDrawer`, `SceneFlowProfileIdDrawer`, `SceneFlowTypedIdDrawerBase`.
  - **Providers/Core:** `ISceneFlowIdSourceProvider<TId>`, `SceneFlowIdSourceResult`, `SceneFlowIdSourceUtility`, `SceneRouteIdSourceProvider`, `TransitionStyleIdSourceProvider`, `SceneFlowProfileIdSourceProvider`.
  - **Tipos de ID tipados:** `SceneRouteId`, `TransitionStyleId`, `SceneFlowProfileId`.
  - **Catálogos/fontes consultadas pelos providers:** `SceneRouteCatalogAsset`, `TransitionStyleCatalogAsset`, `SceneTransitionProfileCatalogAsset`, `SceneRouteDefinitionAsset`.

## Etapa 2 — Checklist + evidências

- [~] **A) Existe tipo forte para Intent ID mantendo serialização string.**
  - **Evidence:** não foi encontrado tipo `GameNavigationIntentId` (nem equivalente claro) nos módulos auditados.
  - **Evidence adicional:** em `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs`, `RouteEntry.routeId` permanece `public string routeId;`.
  - **Status do item:** **Parcial/Não aplicado** (sem tipo forte; serialização segue string direta).

- [ ] **B) Catálogo/asset de intents usa tipo forte (não string solta em runtime).**
  - **Evidence:**
    - `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs` usa `Dictionary<string, GameNavigationEntry> _cache` e `TryGet(string routeId, ...)`.
    - `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/IGameNavigationCatalog.cs` define `IReadOnlyCollection<string> RouteIds` e `TryGet(string routeId, ...)`.
  - **Status do item:** **Não aplicado**.

- [x] **C) Existem constantes/ids canônicos (ex.: `GameNavigationIntentIds.*` ou equivalente).**
  - **Evidence:**
    - `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationIntents.cs` com `GameNavigationIntents.ToMenu = "to-menu"` e `GameNavigationIntents.ToGameplay = "to-gameplay"`.

- [ ] **D) Call sites relevantes migrados para tipo forte (onde antes passava string).**
  - **Evidence:**
    - `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs` mantém fluxo por `string intentId`, incluindo `ExecuteIntentAsync(string intentId, ...)`.
    - Chamadas seguem string/canônicas string: `ExecuteIntentAsync(GameNavigationIntents.ToMenu, ...)`, lookup `_catalog.TryGet(intentId, ...)`.
  - **Status do item:** **Não aplicado**.

- [~] **E) Migração/compat (FormerlySerializedAs/OnValidate/upgrade) para intent tipado.**
  - **Evidence:**
    - Não há evidência de migração para `intentId` tipado (campo novo não encontrado).
    - Existe apenas normalização/migração leve de string atual: `RouteEntry.MigrateLegacy()` faz `routeId = routeId?.Trim();`, chamada em `OnAfterDeserialize()`/`EnsureBuilt()`.
  - **Status do item:** **Parcial** (há compat para string legada atual, não para novo tipo forte).

## Passo 3 — Indícios de “pulo” de etapas posteriores

- **Etapa 4 (formalização de catálogo de profiles como compat/validation-only) aparenta já avançada.**
  - **Evidence:**
    - `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlowTransitionProfiles.cs` explicita em comentário/log que o catálogo é para validação/compat, com runtime privilegiando referência direta de profile.
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Bindings/SceneTransitionProfileCatalogAsset.cs` também documenta papel legado/compat no comentário.

- **Etapa 3 (descontinuar inline routes) ainda não concluída.**
  - **Evidence:**
    - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs` mantém `[SerializeField] private List<RouteEntry> routes = new();` e build de cache a partir de `routes` (fallback inline ativo).

- **Etapa 5 (menu único de validator “Tools/NewScripts/Validate SceneFlow Config…”) sem evidência direta no escopo auditado.**
  - **Evidence:** busca textual não encontrou entrada explícita do menu com esse nome.

## Conclusão
- **Etapa 1:** `PARTIAL` (funcionalmente aplicada em drawers/providers e serialização string preservada; pendência em isolamento de `UnityEditor`/assembly).
- **Etapa 2:** `PARTIAL` (constantes canônicas existem, porém sem `IntentId` tipado no catálogo/runtime e sem migração de call sites).
