# IM-1.2a - InputModeBootstrap legacy isolation (behavior-preserving)

Date: 2026-03-08
Status: CODE + DOC

## Objetivo
- neutralizar o bootstrap automático legado de `InputModeBootstrap`
- garantir que o registro canônico de `IInputModeService` fique somente em `GlobalCompositionRoot.InputModes.cs`
- isolar o trilho legado em `Modules/InputModes/Legacy/Bootstrap`

## Evidência pré-change
### Localização do alvo
```text
rg -n "class\s+InputModeBootstrap\b|InputModeBootstrap\b" . -g "*.cs"
```
Resultado resumido:
```text
Modules/InputModes/Bindings/InputModeBootstrap.cs
```

### Init automático no bootstrap
```text
rg -n "RuntimeInitializeOnLoadMethod|InitializeOnLoadMethod|InitializeOnLoad|DidReloadScripts" Modules/InputModes -g "*.cs"
```
Resultado:
```text
0 matches
```
Leitura: não havia atributo de init automático; o comportamento automático vinha do `Awake()` do `MonoBehaviour`.

### Registro do serviço no bootstrap
```text
rg -n "RegisterGlobal<\s*IInputModeService\s*>|RegisterGlobal\(\s*new\s+InputModeService|IInputModeService" Modules/InputModes/Bindings/InputModeBootstrap.cs -g "*.cs"
```
Resultado resumido:
```text
InputModeBootstrap.cs: fazia RegisterGlobal<IInputModeService>(service)
InputModeBootstrap.cs: construía new InputModeService(...)
```

### GUID scan do bootstrap
- GUID do `.meta`: `fa9f66bffc590944abfdedd7851becaa`
```text
rg -n "fa9f66bffc590944abfdedd7851becaa" -g "*.unity" -g "*.prefab" -g "*.asset" .
rg -n "InputModeBootstrap" -g "*.unity" -g "*.prefab" -g "*.asset" .
```
Resultado:
```text
0 matches
0 matches
```
Leitura: sem refs locais em assets dentro do escopo auditado; move pôde seguir.

## Mudança aplicada
- `Modules/InputModes/Bindings/InputModeBootstrap.cs` -> `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs`
- `.meta` do arquivo movido junto, preservando GUID
- criadas pastas Unity-safe:
  - `Modules/InputModes/Legacy/`
  - `Modules/InputModes/Legacy/Bootstrap/`
- namespace e tipo público preservados
- `Awake()` removido; o tipo não executa mais bootstrap automático
- shim legado mantido com `EnsureInstalled()` / `EnsureRegistered(...)`, ambos no-op com log `[OBS][LEGACY][InputModes]`

## Evidência pós-change
### Unicidade do registro canônico
```text
rg -n "RegisterGlobal<\s*IInputModeService\s*>|new\s+InputModeService\(" Infrastructure/Composition Modules -g "*.cs"
```
Resultado:
```text
Infrastructure/Composition/GlobalCompositionRoot.InputModes.cs:74: provider.RegisterGlobal<IInputModeService>(new InputModeService(playerMapName, menuMapName));
```

### Sem init automático remanescente em InputModes
```text
rg -n "RuntimeInitializeOnLoadMethod|InitializeOnLoadMethod|InitializeOnLoad|DidReloadScripts" Modules/InputModes -g "*.cs"
```
Resultado:
```text
0 matches
```

### Leak sweep pós-change
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod|RuntimeInitializeOnLoadMethod" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
```
Resultado observado:
```text
.Core/Logging/DebugUtility.cs:62: [RuntimeInitializeOnLoadMethod(...)]
.Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61: [RuntimeInitializeOnLoadMethod(...)]
```
Leitura: nenhuma regressão nova; permanecem apenas os 2 runtime-init allowlisted globais já documentados.

## Contratos preservados
- nenhum contrato público de `IInputModeService` / `InputModeService` foi alterado
- nenhuma mudança na ordem/stages de `GlobalCompositionRoot.Pipeline.cs`
- namespace/tipo público de `InputModeBootstrap` preservados
- trilho legado agora exige callsite explícito para qualquer uso futuro

## Validação manual pendente
- não executei Unity/smoke nesta etapa
- checklist manual sugerido:
  - Gameplay -> Pause -> Resume
  - PostGame -> Menu
  - confirmar que o registro do serviço continua vindo só de `GlobalCompositionRoot.InputModes.cs`

## Arquivos tocados
- `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs`
- `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs.meta`
- `Modules/InputModes/Legacy.meta`
- `Modules/InputModes/Legacy/Bootstrap.meta`
- `Docs/Modules/InputModes.md`
- `Docs/Reports/Audits/2026-03-06/Modules/InputModes-Cleanup-Audit-v2.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`
