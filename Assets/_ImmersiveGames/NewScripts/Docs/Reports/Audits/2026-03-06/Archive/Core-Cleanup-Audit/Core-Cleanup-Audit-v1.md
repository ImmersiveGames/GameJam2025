# CORE-1.1 - Core inventory + redundancy/dead-surface audit

Date: 2026-03-07
Scope:
- `Core/**`
- callsites em `Infrastructure/**` e `Modules/**`
- asset scan local em `*.unity`, `*.prefab`, `*.asset` dentro do workspace `NewScripts`

Constraints:
- DOC-only
- nenhum `.cs` alterado
- workspace local como fonte da verdade

## 1. Objetivo
- separar runtime critico de tooling/dev/legacy
- identificar duplicidade funcional em Core
- listar superficies mortas ou sem evidencia local de uso
- preparar recomendacoes A/B/C para `CORE-1.2` sem tocar em codigo nesta etapa

## 2. Leak sweep local do Core
Comando:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" Core -g "*.cs"
```

Resultado curto:
```text
Core/Logging/DebugUtility.cs:51: [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
```

Leitura:
- nenhum arquivo de `Core/**` apresentou `UnityEditor`, `EditorApplication`, `AssetDatabase`, `FindAssets`, `MenuItem`, `ContextMenu` ou `InitializeOnLoadMethod`
- unico hit do sweep foi `RuntimeInitializeOnLoadMethod` em `Core/Logging/DebugUtility.cs`
- nao ha leak Editor/dev no Core pelo criterio pedido

## 3. Evidencia obrigatoria de callsites
Namespace-level query:
```text
rg -n "using _ImmersiveGames\.NewScripts\.Core\.|_ImmersiveGames\.NewScripts\.Core\." Infrastructure Modules -g "*.cs"
```

Resultado resumido:
- Composition: uso intenso de `DependencyManager` / `IDependencyProvider` em composition roots, bridges e runtime services
- Events: uso intenso de `EventBus<T>`, `EventBinding<T>` e `IEvent` em GameLoop, SceneFlow, WorldLifecycle, LevelFlow, Gates, PostGame
- Logging: `DebugUtility`, `DebugLevelAttribute`, `ResetLogTags` e `HardFailFastH1` aparecem no trilho runtime e observability
- Identifiers: `IUniqueIdFactory` / `UniqueIdFactory` usados por spawn/runtime
- sem evidencia local de consumo para `FilteredEventBus*`, `InjectableEventBus`, `EventBusUtil`, `DebugManagerConfig`, `DebugLogSettings`, `Preconditions` e `Core/Fsm/**`

Symbol-level highlights:
- `HardFailFastH1`: callsites em `Modules/Navigation/**`, `Modules/WorldLifecycle/**`, `Modules/LevelFlow/**`, `Modules/SceneFlow/**`
- `ResetLogTags`: callsites em `Modules/WorldLifecycle/**` e `Modules/Gameplay/Runtime/RunRearm/**`
- `IUniqueIdFactory` / `UniqueIdFactory`: callsites em `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`, `Modules/Gameplay/**`, `Modules/WorldLifecycle/Spawn/**`
- `FilteredEventBus`, `InjectableEventBus`, `EventBusUtil`, `SceneServiceCleaner`, `DebugManagerConfig`, `DebugLogSettings`, `Preconditions`: `rg` sem hits em `Infrastructure/**` e `Modules/**`
- `StateMachine`, `IState`, `ITransition`, `IPredicate`: sem uso real; apareceu apenas comentario em `Modules/Gates/SimulationGateTokens.cs`

## 4. Inventory por subdominio
| Component | FilePath | Type | PublicSurface | CallSites (fora do Core) | AssetRef | Recommendation | Notes |
|---|---|---|---|---|---|---|---|
| Composition / root DI | `Core/Composition/DependencyManager.cs` | Runtime | sim: `DependencyManager`, `Provider`, registros e lookup de servicos | high, 200+ hits | n/a | A KEEP | owner canonico do DI runtime |
| Composition / contract | `Core/Composition/IDependencyProvider.cs` | Runtime | sim: `IDependencyProvider` | medium, 10+ hits | n/a | A KEEP | contrato usado por world reset/spawn/adapters |
| Composition / injector | `Core/Composition/DependencyInjector.cs` | Runtime | sim: `DependencyInjector`, `ServiceRegistryExtensions`, `InjectAttribute` | baixo/indireto | n/a | A KEEP | suporte de injecao usado via `DependencyManager` |
| Composition / base registry | `Core/Composition/ServiceRegistry.cs` | Runtime | sim: `ServiceRegistry` | baixo, indireto | n/a | A KEEP | base abstrata compartilhada pelos registries |
| Composition / global registry | `Core/Composition/GlobalServiceRegistry.cs` | Runtime | sim: `GlobalServiceRegistry` | 0 direto | n/a | A KEEP | construido internamente por `DependencyManager` |
| Composition / object registry | `Core/Composition/ObjectServiceRegistry.cs` | Runtime | sim: `ObjectServiceRegistry` | 0 direto | n/a | A KEEP | construido internamente por `DependencyManager` |
| Composition / scene registry | `Core/Composition/SceneServiceRegistry.cs` | Runtime | sim: `SceneServiceRegistry`, `OnSceneServicesCleared` | 0 direto | n/a | A KEEP | construido internamente; limpeza automatica de cena |
| Composition / cleaner helper | `Core/Composition/SceneServiceCleaner.cs` | Runtime | sim: `SceneServiceCleaner` | 0 direto | nome/guid sem hits | C RISK | usado apenas indiretamente pelo ctor de `SceneServiceRegistry`; revisar se a superficie precisa ser publica |
| Events / canonical bus | `Core/Events/EventBus.cs` | Runtime | sim: `EventBus<T>` | high, 200+ hits | n/a | A KEEP | trilho canonico cross-module |
| Events / binding | `Core/Events/EventBinding.cs` | Runtime | sim: `EventBinding<T>` | high, 100+ hits | n/a | A KEEP | binding canonico de subscribe/unsubscribe |
| Events / contracts | `Core/Events/IEvent.cs` | Runtime | sim: `IEvent`, `IEventBus<T>` | medium | n/a | A KEEP | base de eventos e bus injetavel |
| Events / binding contract | `Core/Events/IEventBinding.cs` | Runtime | sim: `IEventBinding<T>` | baixo/indireto | n/a | A KEEP | contrato fino usado por binding |
| Events / filtered bus | `Core/Events/FilteredEventBus.cs` | Runtime | sim: `FilteredEventBus<TScope,TEvent>` | 0 | nome/guid sem hits | C RISK | sem callsites locais; duplica semantica de roteamento do `EventBus<T>` |
| Events / legacy wrapper | `Core/Events/FilteredEventBus.Legacy.cs` | Legacy | sim: `FilteredEventBus<TEvent>` | 0 | nome/guid sem hits | B MOVE | wrapper de compat sem evidencia de uso; candidato natural a `Legacy/` |
| Events / injectable bus | `Core/Events/InjectableEventBus.cs` | Unknown | sim: `InjectableEventBus<T>` | 0 | nome/guid sem hits | C RISK | alternativa ao `EventBus<T>` sem callsites locais |
| Events / util | `Core/Events/EventBusUtil.cs` | Unknown | sim: `EventBusUtil`, `ClearAllBuses` | 0 | nome/guid sem hits | C RISK | superficie publica sem consumo fora do Core; possivel `internal`/remocao |
| FSM / predicates | `Core/Fsm/IPredicate.cs` | Unknown | sim: `IPredicate` | 0 real | nome/guid sem hits | C RISK | sem trilho ativo no workspace |
| FSM / state contract | `Core/Fsm/IState.cs` | Unknown | sim: `IState` | 0 real | nome/guid sem hits | C RISK | sem uso concreto fora do comentario em Gates |
| FSM / transition contract | `Core/Fsm/ITransition.cs` | Unknown | sim: `ITransition` | 0 real | nome/guid sem hits | C RISK | sem uso concreto local |
| FSM / machine | `Core/Fsm/StateMachine.cs` | Unknown | sim: `StateMachine` | 0 real | nome/guid sem hits | C RISK | superficie aparentemente morta |
| FSM / transitions | `Core/Fsm/Transition.cs` | Unknown | sim: `Transition`, `Transition<T>`, `FuncPredicate`, `ActionPredicate`, `EventTriggeredPredicate` | 0 real | nome/guid sem hits | C RISK | familia inteira sem consumo local |
| Identifiers / contract+impl | `Core/Identifiers/UniqueIdFactory.cs` | Runtime | sim: `IUniqueIdFactory`, `UniqueIdFactory` | medium, 20+ hits | n/a | A KEEP | factory canonica para ActorId runtime |
| Logging / debug attr | `Core/Logging/DebugLevelAttribute.cs` | Runtime | sim: `DebugLevelAttribute` | medium via `[DebugLevel(...)]` | n/a | A KEEP | metadata consumida por `DebugUtility` |
| Logging / levels+runtime init | `Core/Logging/DebugUtility.cs` | Runtime | sim: `DebugUtility`, `DebugLevel` | very high, 1000+ hits | n/a | A KEEP | trilho canonico de logs; unico `RuntimeInitializeOnLoadMethod` do sweep |
| Logging / fail-fast | `Core/Logging/HardFailFastH1.cs` | Runtime | sim: `HardFailFastH1` | medium, 30+ hits | n/a | A KEEP | owner canonico de abort H1 |
| Logging / reset tags | `Core/Logging/ResetLogTags.cs` | Runtime | sim: `ResetLogTags` | medium, 20+ hits | n/a | A KEEP | ancora canonica de logs de reset |
| Logging / config MonoBehaviour | `Core/Logging/DebugManagerConfig.cs` | DevQA | sim: `DebugManagerConfig` | 0 | nome/guid sem hits; arquivo sob `UNITY_EDITOR || DEVELOPMENT_BUILD` | B MOVE | nao faz parte do trilho runtime critico |
| Logging / config asset | `Core/Logging/DebugLogSettings.cs` | DevQA | sim: `DebugLogSettings` | 0 `.cs` | nome hit apenas em `Core/Logging/DebugLogSettings.asset`; GUID hit apenas no proprio asset | B MOVE | acompanha `DebugManagerConfig`; sem evidencias de uso por scene/prefab no workspace |
| Validation / preconditions | `Core/Validation/Preconditions.cs` | Unknown | sim: `Preconditions` | 0 | nome/guid sem hits | C RISK | utilitario sem callsites locais; pode estar morto |

## 5. Asset scan obrigatorio para candidatos B/C
Comando-base por candidato:
```text
rg -n "<TypeName>" -g "*.unity" -g "*.prefab" -g "*.asset" .
rg -n "<guid>" -g "*.unity" -g "*.prefab" -g "*.asset" .
```

Resultados consolidados:
| Candidate | TypeName scan | GUID scan | Leitura |
|---|---|---|---|
| `FilteredEventBus.Legacy` | 0 | 0 (`8c9e6efed682d3e438f923f0370a560a`) | sem ref de asset local |
| `FilteredEventBus` | 0 | n/a | sem ref de asset local |
| `InjectableEventBus` | 0 | 0 (`ef48e4f1a4a71254fa5ae1097e211151`) | sem ref de asset local |
| `EventBusUtil` | 0 | 0 (`9935ee62e5dc83448bd6e2a7574defa5`) | sem ref de asset local |
| `SceneServiceCleaner` | 0 | 0 (`cb566b8cc178ea64ab5871eaef4b1de6`) | sem ref de asset local |
| `DebugManagerConfig` | 0 | 0 (`e1a960ba9013456db4097f7dae8f25d7`) | sem scene/prefab/asset consumindo o MonoBehaviour no workspace auditado |
| `DebugLogSettings` | 1 hit no proprio `Core/Logging/DebugLogSettings.asset` | 1 hit no proprio asset (`8c6d4ad0c2044d4bb4b6c1a5c07f9d5d`) | asset existe, mas nao ha evidencia de consumo por scene/prefab fora dele |
| `Preconditions` | 0 | 0 (`5dc26d807148a5640bc1e3cdff036bc5`) | sem ref de asset local |
| `StateMachine` / `IState` / `ITransition` / `IPredicate` | 0 real; apenas comentario em `.cs` | n/a | sem ref de asset local |

## 6. Lista A/B/C para CORE-1.2
### A KEEP
- `DependencyManager`, `IDependencyProvider`, `DependencyInjector`, registries, `EventBus<T>`, `EventBinding<T>`, `IEvent*`, `DebugUtility`, `HardFailFastH1`, `ResetLogTags`, `DebugLevelAttribute`, `UniqueIdFactory`

### B MOVE
1. `Core/Events/FilteredEventBus.Legacy.cs`
2. `Core/Logging/DebugManagerConfig.cs`
3. `Core/Logging/DebugLogSettings.cs`

### C RISK
1. `Core/Events/InjectableEventBus.cs`
2. `Core/Events/EventBusUtil.cs`
3. `Core/Events/FilteredEventBus.cs`
4. `Core/Fsm/StateMachine.cs`
5. `Core/Fsm/Transition.cs`
6. `Core/Fsm/IState.cs`
7. `Core/Fsm/ITransition.cs`
8. `Core/Fsm/IPredicate.cs`
9. `Core/Validation/Preconditions.cs`
10. `Core/Composition/SceneServiceCleaner.cs`

## 7. Top 10 candidates para CORE-1.2
| Rank | Candidate | Bucket | Recomendacao |
|---|---|---|---|
| 1 | `Core/Events/FilteredEventBus.Legacy.cs` | B MOVE | mover para `Legacy/` e iniciar deprecacao formal |
| 2 | `Core/Logging/DebugManagerConfig.cs` | B MOVE | mover para trilho DevQA/Editor |
| 3 | `Core/Logging/DebugLogSettings.cs` | B MOVE | mover junto do config DevQA e reavaliar o asset |
| 4 | `Core/Events/InjectableEventBus.cs` | C RISK | confirmar ausencia de uso reflexivo/test harness antes de remover |
| 5 | `Core/Events/EventBusUtil.cs` | C RISK | reduzir superficie publica ou remover se nao houver owner |
| 6 | `Core/Events/FilteredEventBus.cs` | C RISK | validar se ainda precisa existir ao lado do `EventBus<T>` |
| 7 | `Core/Fsm/StateMachine.cs` | C RISK | mover para `Legacy/` ou remover apos confirmacao |
| 8 | `Core/Fsm/Transition.cs` | C RISK | mesma decisao da stack FSM |
| 9 | `Core/Validation/Preconditions.cs` | C RISK | confirmar se nao ha consumo externo fora do workspace |
| 10 | `Core/Composition/SceneServiceCleaner.cs` | C RISK | avaliar se deve virar detalhe `internal` do registry |

## 8. Recomendacao executavel para CORE-1.2
- fase 1: mover `FilteredEventBus.Legacy`, `DebugManagerConfig` e `DebugLogSettings` para trilho `Legacy/DevQA`
- fase 2: confirmar por grep simbolico e smoke local se `InjectableEventBus`, `EventBusUtil`, `FilteredEventBus`, `Preconditions` e `Core/Fsm/**` podem sair do rail publico
- fase 3: se `SceneServiceCleaner` permanecer, reduzir superficie para detalhe interno do `SceneServiceRegistry`

## 9. Confirmacao
- entrega DOC-only
- nenhum arquivo `.cs` foi alterado
