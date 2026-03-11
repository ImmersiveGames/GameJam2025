# CORE-1.2d - FilteredEventBus classification

Date: 2026-03-07
Decision: C reserve

## Objetivo
- classificar `Core/Events/FilteredEventBus.cs` sem mover por ausencia de hits
- diferenciar entre canonical, dev-only, reserve e legacy compat

## Evidencia
### 1) Uso em codigo
Comandos:
```text
rg -n -w "FilteredEventBus" . -g "*.cs"
rg -n "new\s+FilteredEventBus|FilteredEventBus<|IFilteredEventBus|FilteredEventBus\." . -g "*.cs"
```

Resultado curto:
- hits apenas em `Core/Events/FilteredEventBus.cs`, `Core/Events/EventBusUtil.cs` e `Core/Events/Legacy/FilteredEventBus.Legacy.cs`
- nenhum callsite em `Modules/**` ou `Infrastructure/**`
- nenhum `IFilteredEventBus`

### 2) Relacao com `EventBus<T>` / `InjectableEventBus`
Comando:
```text
rg -n -w "EventBus<|InjectableEventBus" Core/Events -g "*.cs"
```

Resultado curto:
- `EventBus.cs` usa `InjectableEventBus<T>` como backend default
- `FilteredEventBus.cs` nao e backend de `EventBus<T>`
- `EventBusUtil.cs` conhece `FilteredEventBus<,>` para limpeza/registro refletivo

Leitura:
- `FilteredEventBus<,>` nao e owner canonico atual do barramento baseline
- tambem nao e legacy compat explicito, porque o compat wrapper esta separado em `Core/Events/Legacy/FilteredEventBus.Legacy.cs`
- ele permanece como superficie alternativa/reserva conhecida pelo `EventBusUtil`

### 3) Leak Editor
Comando:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod|RuntimeInitializeOnLoadMethod" Core/Events/FilteredEventBus.cs
```

Resultado:
- 0 hits

### 4) GUID scan
- script GUID (`FilteredEventBus.cs.meta`): `8808bda41d2490e49835869e78f28f05`

Comando:
```text
rg -n "8808bda41d2490e49835869e78f28f05" . -g "*.unity" -g "*.prefab" -g "*.asset"
```

Resultado:
- 0 hits

## Decisao final
- classificacao: `C reserve`
- rationale:
  - nao ha callsites reais fora do Core nem refs em assets
  - nao e `D legacy compat`, porque o compat explicito esta no arquivo legacy separado
  - nao e dead: `EventBusUtil` conhece o tipo e o wrapper legacy ainda delega para ele

## Mudancas de codigo
- nenhuma

## Confirmacao
- arquivo mantido no lugar: `Core/Events/FilteredEventBus.cs`
- decisao fechada por evidencia estatica local
