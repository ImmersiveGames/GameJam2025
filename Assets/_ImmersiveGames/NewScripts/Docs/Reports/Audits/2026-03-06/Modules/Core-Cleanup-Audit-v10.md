# CORE-1.2h - EventBusUtil classification and runtime/editor isolation

Date: 2026-03-07
Decision: C reserve

## Objetivo
- classificar `Core/Events/EventBusUtil.cs` a partir do workspace local
- confirmar se existe leak Editor fora de `Editor/**` / `Dev/**` / `Legacy/**` / `QA/**`
- aplicar isolamento minimo somente se necessario

## Evidencia
### A) Inventario geral
Comando:
```text
rg -n -w "EventBusUtil|OnPlayModeStateChanged|playModeStateChanged|InitializeOnLoadMethod|InitializeOnLoad|EditorApplication" . -g "*.cs"
```

Resultado curto:
- `Core/Events/EventBusUtil.cs:11: public static partial class EventBusUtil`
- `Core/Events/EventBus.cs:13: EventBusUtil.RegisterEventType(typeof(T));`
- `Core/Events/FilteredEventBus.cs:15: EventBusUtil.RegisterFilteredEventType(typeof(TScope), typeof(TEvent));`
- `Editor/Core/Events/EventBusUtil.Editor.cs:8: [InitializeOnLoadMethod]`
- `Editor/Core/Events/EventBusUtil.Editor.cs:11: EditorApplication.playModeStateChanged += OnPlayModeStateChanged;`

Leitura:
- `EventBusUtil` existe como helper de registro/cleanup conhecido por `EventBus<T>` e `FilteredEventBus<TScope, TEvent>`
- o hook de Editor ja esta isolado em arquivo separado sob `Editor/**`

### B) Leak check estrito (runtime)
Comando:
```text
rg -n "UnityEditor|EditorApplication|InitializeOnLoadMethod|InitializeOnLoad|DidReloadScripts" . -g "*.cs" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/Legacy/**" -g "!**/QA/**"
```

Resultado curto:
- `Core/Logging/DebugUtility.cs:51: [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61: [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`
- nenhum hit de `UnityEditor`, `EditorApplication` ou `InitializeOnLoadMethod` fora de `Editor/**` / `Dev/**` / `Legacy/**` / `QA/**`

Leitura:
- check B permaneceu limpo para leak de Editor no runtime
- os dois hits restantes sao bootstrap runtime permitido, nao hooks de Editor

### C) Callsites reais fora do Core
Comando:
```text
rg -n -w "EventBusUtil" Infrastructure Modules -g "*.cs"
```

Resultado:
- 0 hits

Leitura:
- nao existe callsite real em `Infrastructure/**` ou `Modules/**`
- isso impede classificar `EventBusUtil` como `A canonical`

### D) GUID scan
- script GUID (`Core/Events/EventBusUtil.cs.meta`): `9935ee62e5dc83448bd6e2a7574defa5`
- editor GUID (`Editor/Core/Events/EventBusUtil.Editor.cs.meta`): `12bc6d378f5ae094`

Comandos:
```text
rg -n "9935ee62e5dc83448bd6e2a7574defa5" . -g "*.unity" -g "*.prefab" -g "*.asset"
rg -n "12bc6d378f5ae094" . -g "*.unity" -g "*.prefab" -g "*.asset"
```

Resultado:
- 0 hits para ambos os GUIDs

Leitura:
- nao ha refs de asset/scene/prefab para os scripts `EventBusUtil`

## Decisao final
- classificacao: `C reserve`
- rationale:
  - helper runtime conhecido por `EventBus<T>` e `FilteredEventBus<TScope, TEvent>`, mas sem callsites reais em `Modules/**`/`Infrastructure/**`
  - hook de Editor ja esta corretamente isolado em `Editor/Core/Events/EventBusUtil.Editor.cs`
  - nao e dead; pode voltar a `A` durante migracao legado -> canonico, portanto nao remover sem evidencia nova

## Arquivos tocados
- `Docs/Modules/Core.md`
- `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v10.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`

## Mudancas de codigo
- nenhuma; nenhum `.cs` precisou ser alterado
- isolamento Editor/Runtime ja estava correto antes desta etapa
