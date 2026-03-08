# CORE-1.2c - InjectableEventBus classification

Date: 2026-03-07
Decision: A canonical

## Objetivo
- classificar `Core/Events/InjectableEventBus.cs` com base apenas no workspace local
- distinguir entre canonical, dev-only, reserve ou legacy compat

## Evidencia
### 1) Uso em codigo
Comandos:
```text
rg -n -w "InjectableEventBus" . -g "*.cs"
rg -n -w "IInjectableEventBus|InjectableEventBus<" . -g "*.cs"
```

Resultado curto:
- `Core/Events/InjectableEventBus.cs:6: public class InjectableEventBus<T> : IEventBus<T>`
- `Core/Events/EventBus.cs:7: private static readonly InjectableEventBus<T> InternalBus = new();`
- nenhum `IInjectableEventBus` encontrado

Leitura:
- `InjectableEventBus<T>` nao tem callsites em `Modules/**` ou `Infrastructure/**`
- ainda assim ele e parte do trilho baseline porque `EventBus<T>` o instancia como implementacao concreta default de `GlobalBus`

### 2) GUID scan
- script GUID (`InjectableEventBus.cs.meta`): `ef48e4f1a4a71254fa5ae1097e211151`

Comando:
```text
rg -n "ef48e4f1a4a71254fa5ae1097e211151" . -g "*.unity" -g "*.prefab" -g "*.asset"
```

Resultado:
- 0 hits em assets

Leitura:
- ausencia de asset refs nao muda a classificacao, porque o uso e puramente em codigo dentro de `EventBus<T>`

### 3) Leak Editor
Comando:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" Core/Events/InjectableEventBus.cs
```

Resultado:
- 0 hits

## Decisao final
- classificacao: `A canonical`
- rationale: `InjectableEventBus<T>` e o backend concreto default do `EventBus<T>`, que e parte do trilho runtime canonico do Core
- nao e `B dev-only`: nao ha tooling/editor no arquivo
- nao e `C reserve`: ha consumo ativo, ainda que interno ao Core
- nao e `D legacy compat`: nao existe marcador de compat/deprecacao; o tipo e parte da implementacao atual

## Mudancas de codigo
- nenhuma

## Confirmacao
- arquivo mantido no lugar: `Core/Events/InjectableEventBus.cs`
- decisao fechada por evidencia estatica local
