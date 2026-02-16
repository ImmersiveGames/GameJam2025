# Audit — StringsToDirectRefs v1 (Etapas 1 e 2)

- Data/hora (UTC): 2026-02-16T01:56:42Z
- Branch: `work`
- Commit (HEAD no início da auditoria): `e34f6eb6fd527231c4d006744c152b9154745ddc`
- Plano-base: `Assets/_ImmersiveGames/NewScripts/Docs/Plans/Plan-Post-StringsToDirectRefs-v1-DataCleanup.md`
- Auditoria de referência: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-DataCleanup-Post-StringsToDirectRefs-v1.md`
- Baseline de etapas 01-02 encontrado: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-StringsToDirectRefs-v1-Steps-01-02.md`
- Escopo auditado (pastas):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Runtime/`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/`
  - `Assets/_ImmersiveGames/NewScripts/Core/`

## Sumário
A Etapa 1 está **PARTIAL**: os PropertyDrawers e Source Providers editor-only existem e a serialização dos IDs permanece string encapsulada, porém o critério “sem dependência UnityEditor em runtime assemblies” não fecha por múltiplas ocorrências fora de `Editor/` (mesmo quando condicionadas por `#if UNITY_EDITOR`) e ausência de isolamento por `.asmdef`. A Etapa 2 está **PARTIAL**: há constantes canônicas (`GameNavigationIntents`), mas não foi encontrado tipo forte de intent (`GameNavigationIntentId`) e o catálogo/runtime continuam com `string` para intents.

## Tabela de status
| Item | Status | Justificativa objetiva |
|---|---|---|
| Etapa 1 — PropertyDrawers + Source Providers | **PARTIAL** | A/B/C/E com evidência direta; D não fecha por usos de `UnityEditor` fora de `Editor/` + ausência de `.asmdef`. |
| Etapa 2 — IntentId tipado | **PARTIAL** | Só C aplicado; A/B/D/E sem evidência de tipagem forte no catálogo/contratos/call sites. |
| Etapa 5 (extra) — Menu validator | **FAIL** | Não encontrada evidência do menu `Tools/NewScripts/Validate SceneFlow Config…` no código auditado. |

---

## Etapa 1 — Checklist + evidências

- [x] **A) PropertyDrawers Editor-only para `SceneRouteId` / `TransitionStyleId` / `SceneFlowProfileId`.**
  - **Evidence (paths + símbolos):**
    - `Modules/SceneFlow/Editor/IdDrawers/SceneRouteIdDrawer.cs` → `SceneRouteIdDrawer` + `[CustomPropertyDrawer(typeof(SceneRouteId))]`.
    - `Modules/SceneFlow/Editor/IdDrawers/TransitionStyleIdDrawer.cs` → `TransitionStyleIdDrawer` + `[CustomPropertyDrawer(typeof(TransitionStyleId))]`.
    - `Modules/SceneFlow/Editor/IdDrawers/SceneFlowProfileIdDrawer.cs` → `SceneFlowProfileIdDrawer` + `[CustomPropertyDrawer(typeof(SceneFlowProfileId))]`.
  - **Trecho curto:**
    ```csharp
    [CustomPropertyDrawer(typeof(SceneRouteId))]
    public sealed class SceneRouteIdDrawer : SceneFlowTypedIdDrawerBase
    {
        private static readonly SceneRouteIdSourceProvider Provider = new();
        protected override SceneFlowIdSourceResult CollectSource() => Provider.Collect();
    }
    ```

- [x] **B) Drawers usam dropdown via Source Providers.**
  - **Evidence (paths + símbolos):**
    - `Modules/SceneFlow/Editor/IdDrawers/SceneFlowTypedIdDrawerBase.cs` → método `OnGUI(...)` usa `CollectSource()` e `EditorGUI.Popup(...)`.
    - `Modules/SceneFlow/Editor/IdSources/SceneRouteIdSourceProvider.cs` → `Collect()`, `CollectFromRouteDefinitionAssets`, `CollectFromRouteCatalogAssets`.
    - `Modules/SceneFlow/Editor/IdSources/TransitionStyleIdSourceProvider.cs` → `Collect()` lendo `styles[].styleId._value`.
    - `Modules/SceneFlow/Editor/IdSources/SceneFlowProfileIdSourceProvider.cs` → `AddCanonicalProfiles`, coleta de `TransitionStyleCatalogAsset` e `SceneTransitionProfileCatalogAsset`.
  - **Trecho curto (dropdown):**
    ```csharp
    SceneFlowIdSourceResult source = CollectSource();
    var options = BuildOptions(source.Values, allowEmpty, currentNormalized, isMissing, out int currentIndex);
    int selected = EditorGUI.Popup(lineRect, label.text, currentIndex, options);
    rawValueProperty.stringValue = selectedValue == "<none>" ? string.Empty : selectedValue;
    ```
  - **Trecho curto (fonte SceneRouteId):**
    ```csharp
    string[] guids = AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset");
    ...
    string[] guids = AssetDatabase.FindAssets("t:SceneRouteCatalogAsset");
    ...
    ReadRouteDefinitionReferences(serializedObject, allValues);
    ReadInlineRoutes(serializedObject, allValues, duplicates);
    ```

- [x] **C) Serialização continua string encapsulada em structs.**
  - **Evidence (paths + símbolos):**
    - `Modules/SceneFlow/Navigation/Runtime/SceneRouteId.cs` → `SceneRouteId._value` (`[SerializeField] private string _value;`).
    - `Modules/SceneFlow/Navigation/Runtime/TransitionStyleId.cs` → `TransitionStyleId._value` (`[SerializeField] private string _value;`).
    - `Modules/SceneFlow/Runtime/SceneFlowProfileId.cs` → `SceneFlowProfileId._value` (`[SerializeField] private string _value;`).
    - `Modules/SceneFlow/Editor/IdDrawers/SceneFlowTypedIdDrawerBase.cs` → grava em `rawValueProperty.stringValue` do campo relativo `_value`.
  - **Trecho curto:**
    ```csharp
    [Serializable]
    public struct SceneRouteId : IEquatable<SceneRouteId>
    {
        [SerializeField] private string _value;
        public string Value => _value ?? string.Empty;
    }
    ```

- [~] **D) “Sem dependência UnityEditor em runtime assemblies”.**
  - **Status:** **PARTIAL**.
  - **Evidence 1 — isolamento por assembly:** nenhum `.asmdef` encontrado em `Assets/_ImmersiveGames/NewScripts/`.
  - **Evidence 2 — ocorrências fora de `Editor/` (todas listadas):**

    1. `Infrastructure/Composition/GlobalCompositionRoot.FadeLoading.cs` — método `HandleFadeFailureOrDegrade(...)`.
       - `UnityEditor.EditorApplication.isPlaying = false;` sob `#if UNITY_EDITOR`.

    2. `Infrastructure/Composition/GlobalCompositionRoot.BootstrapConfig.cs` — método `FailFast(string message)`.
       - `UnityEditor.EditorApplication.isPlaying = false;` sob `#if UNITY_EDITOR`.

    3. `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs` — método `HandleFadeStartFailure(string detail)`.
       - `UnityEditor.EditorApplication.isPlaying = false;` sob `#if UNITY_EDITOR`.

    4. `Modules/Navigation/Bindings/MenuQuitButtonBinder.cs` — método `OnClickCore(string actionReason)`.
       - `UnityEditor.EditorApplication.isPlaying = false;` sob `#if UNITY_EDITOR`.

    5. `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs` — método `FailFastConfig(string detail)`.
       - `UnityEditor.EditorApplication.isPlaying = false;` sob `#if UNITY_EDITOR`.

    6. `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` — métodos `EnsureTransitionProfileOrFailFast(...)` e `FailFastTransitionRequest(...)`.
       - `UnityEditor.EditorApplication.isPlaying = false;` sob `#if UNITY_EDITOR`.

    7. `Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs` — método `HandleFatalConfig(string detail)`.
       - `UnityEditor.EditorApplication.isPlaying = false;` sob `#if UNITY_EDITOR`.

    8. `Modules/PostGame/Bindings/PostGameOverlayController.cs` — `using UnityEditor;` sob `#if UNITY_EDITOR`.

    9. `Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs` — `using UnityEditor;` sob `#if UNITY_EDITOR`.

    10. `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs` — `using UnityEditor;` sob `#if UNITY_EDITOR`.

    11. `Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs` — `using UnityEditor;` sob `#if UNITY_EDITOR`.

    12. `Core/Events/EventBusUtil.cs` — `using UnityEditor;` **sem** `#if` no `using` (uso de APIs editor protegido no corpo por `#if UNITY_EDITOR`).

    13. `Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs` — `using UnityEditor;` sem guarda no `using`; arquivo em pasta `Dev` (fora de `Editor/`).

  - **Trecho curto (exemplo de guarda):**
    ```csharp
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    if (!Application.isEditor) Application.Quit();
    ```

- [x] **E) Arquivos/símbolos principais envolvidos listados.**
  - **Drawers:** `SceneRouteIdDrawer`, `TransitionStyleIdDrawer`, `SceneFlowProfileIdDrawer`, `SceneFlowTypedIdDrawerBase`.
  - **Providers:** `SceneRouteIdSourceProvider`, `TransitionStyleIdSourceProvider`, `SceneFlowProfileIdSourceProvider`, `ISceneFlowIdSourceProvider<TId>`, `SceneFlowIdSourceUtility`, `SceneFlowIdSourceResult`.
  - **IDs tipados:** `SceneRouteId`, `TransitionStyleId`, `SceneFlowProfileId`.
  - **Catálogos-fontes:** `SceneRouteCatalogAsset`, `TransitionStyleCatalogAsset`, `SceneTransitionProfileCatalogAsset`, `SceneRouteDefinitionAsset`.

---

## Etapa 2 — Checklist + evidências

- [ ] **A) Existe tipo forte para IntentId mantendo serialização string (ex.: `GameNavigationIntentId`).**
  - **Evidence:** busca em `Modules/`, `Infrastructure/`, `Core/` por `GameNavigationIntentId|GameNavigationIntentIds` → **não encontrado**.
  - **Evidence adicional:** `Modules/Navigation/GameNavigationCatalogAsset.cs` mantém `RouteEntry.routeId` como `public string routeId;`.

- [ ] **B) `GameNavigationCatalogAsset` e `IGameNavigationCatalog` usam tipo forte (não string solta).**
  - **Evidence (`GameNavigationCatalogAsset`):**
    - `public string routeId;`
    - `_cache` é `Dictionary<string, GameNavigationEntry>`
    - `TryGet(string routeId, out GameNavigationEntry entry)`.
  - **Evidence (`IGameNavigationCatalog`):**
    - `IReadOnlyCollection<string> RouteIds { get; }`
    - `bool TryGet(string routeId, out GameNavigationEntry entry);`

- [x] **C) Constantes canônicas existem (`GameNavigationIntents` ou equivalente).**
  - **Evidence:** `Modules/Navigation/GameNavigationIntents.cs` define:
    - `ToMenu = "to-menu"`
    - `ToGameplay = "to-gameplay"`.

- [ ] **D) Call sites relevantes migrados para tipo forte (onde antes passava string).**
  - **Evidence:** `Modules/Navigation/GameNavigationService.cs` ainda opera intents como `string`:
    - `ExecuteIntentAsync(string intentId, string reason = null)`
    - `_catalog.TryGet(intentId, out var entry)`
    - `ExecuteIntentAsync(GameNavigationIntents.ToMenu, reason)` (constante string).

- [~] **E) Existe migração/compat para campo antigo (FormerlySerializedAs/OnValidate/upgrade) se aplicável.**
  - **Status:** **PARTIAL**.
  - **Evidence:**
    - Há compat/migração do shape atual string (`MigrateLegacy()` com `routeId = routeId?.Trim();` chamado em `OnAfterDeserialize()` e `EnsureBuilt()`).
    - Não há evidência de migração de `routeId:string` para `intentId:GameNavigationIntentId` (porque o tipo/campo novos não foram encontrados).

---

## Checagem extra — Etapa 5 (menu validator)

- **Resultado:** **FAIL (não encontrado)** para o menu exato `Tools/NewScripts/Validate SceneFlow Config…`.
- **Evidence:** busca textual no escopo não encontrou `MenuItem`/método com esse nome.
- **Observação:** há referência no plano, mas sem implementação rastreável com esse label no código auditado.

---

## Conclusão

### O que bloqueia “DONE” da Etapa 1
1. Critério D não fecha: há diversas referências a `UnityEditor` fora de `Editor/` (mesmo condicionadas por `#if UNITY_EDITOR`) e não há isolamento por `.asmdef` em `NewScripts`.

### O que bloqueia “DONE” da Etapa 2
1. Tipo forte de intent (`GameNavigationIntentId`) não encontrado.
2. Catálogo/contrato (`GameNavigationCatalogAsset` / `IGameNavigationCatalog`) ainda tipados com `string` para intent.
3. Call sites de navegação continuam com `string intentId`.
4. Não há trilha de migração para troca de campo string → tipo forte (apenas trim/normalização do campo atual).

### Pendências objetivas (sem refatoração ampla)
- Confirmar introdução (ou ausência oficial) de `GameNavigationIntentId` no código de runtime.
- Confirmar contrato alvo de `IGameNavigationCatalog` para intents tipados.
- Confirmar se o menu de validator da Etapa 5 foi implementado com outro nome/caminho.
