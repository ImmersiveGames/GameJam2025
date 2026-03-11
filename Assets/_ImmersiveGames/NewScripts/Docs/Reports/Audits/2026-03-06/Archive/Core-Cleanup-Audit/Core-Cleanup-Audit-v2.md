# CORE-1.2a - FilteredEventBus.Legacy isolation

Date: 2026-03-07
Scope:
- `Core/Events/**`
- target: `FilteredEventBus.Legacy.cs`

## Objetivo
- isolar `FilteredEventBus.Legacy.cs` em `Core/Events/Legacy/`
- preservar comportamento e contratos publicos
- iniciar deprecacao minima por comentario, sem impacto de compilacao

## Evidencia pre-move
Comandos:
```text
rg -n -w "FilteredEventBus" . -g "*.cs"
rg -n "FilteredEventBus\.Legacy" . -g "*.cs"
rg -n "FilteredEventBus" -g "*.unity" -g "*.prefab" -g "*.asset" .
```

Resumo curto:
- `FilteredEventBus` aparecia apenas em `Core/Events/EventBusUtil.cs`, `Core/Events/FilteredEventBus.cs` e `Core/Events/FilteredEventBus.Legacy.cs`
- `FilteredEventBus.Legacy` por nome nao teve hits fora do proprio arquivo
- assets `.unity/.prefab/.asset`: 0 hits

## Move executado
Arquivos movidos:
- `Core/Events/FilteredEventBus.Legacy.cs` -> `Core/Events/Legacy/FilteredEventBus.Legacy.cs`
- `Core/Events/FilteredEventBus.Legacy.cs.meta` -> `Core/Events/Legacy/FilteredEventBus.Legacy.cs.meta`
- criado `Core/Events/Legacy/.meta`

## Deprecacao minima
- comentario de topo adicionado no arquivo movido:
  - `LEGACY: isolado em Core/Events/Legacy. Nao usar em codigo novo. Mantido por compatibilidade.`
- `[Obsolete]` nao aplicado para evitar risco com warnings-as-errors desconhecidos no workspace local

## Pos-check
Comandos:
```text
rg -n -w "FilteredEventBus" . -g "*.cs"
rg -n "FilteredEventBus" -g "*.unity" -g "*.prefab" -g "*.asset" .
```

Resumo curto:
- ocorrencias de `FilteredEventBus` continuam restritas a `Core/Events/**`
- `FilteredEventBus.Legacy.cs` agora aparece apenas em `Core/Events/Legacy/FilteredEventBus.Legacy.cs`
- zero callsites em `Infrastructure/**` e `Modules/**`
- assets `.unity/.prefab/.asset`: 0 hits

## Confirmacao
- behavior-preserving
- namespace intacto: `_ImmersiveGames.NewScripts.Core.Events`
- tipo publico intacto: `FilteredEventBus<TEvent>`
- nenhuma alteracao em pipeline/composition
