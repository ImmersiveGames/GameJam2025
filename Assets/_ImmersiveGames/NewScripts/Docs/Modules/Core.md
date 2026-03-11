# Core

## Status atual (Baseline 3.1)
- Escopo auditado em `CORE-1.1`: `Core/**` com evidencia de callsites em `Infrastructure/**` e `Modules/**`.
- Classificacao atual:
  - Runtime critico: `Core/Composition/**`, `Core/Events/EventBus.cs`, `Core/Events/EventBinding.cs`, `Core/Events/IEvent*.cs`, `Core/Logging/DebugUtility.cs`, `Core/Logging/HardFailFastH1.cs`, `Core/Logging/ResetLogTags.cs`, `Core/Identifiers/UniqueIdFactory.cs`.
  - Tooling / dev / legacy / suspeitos: `Core/Events/FilteredEventBus*.cs`, `Core/Events/EventBusUtil.cs`, `Core/Fsm/**`, `Core/Logging/DebugManagerConfig.cs`, `Core/Logging/DebugLogSettings.cs`, `Core/Validation/Preconditions.cs`.
- Leak sweep local em `Core/*.cs`: sem `UnityEditor`, `AssetDatabase`, `MenuItem`, `ContextMenu` ou `InitializeOnLoadMethod`; unico hit do sweep foi `RuntimeInitializeOnLoadMethod` em `Core/Logging/DebugUtility.cs`.
- `CORE-1.1`: inventory done; `CORE-1.2`: pending.

## Ownership por dominio
- Events: `EventBus<T>` + `EventBinding<T>` + `IEvent` sao o trilho canonico de publish/subscribe cross-module.
- Composition: `DependencyManager.Provider` + `IDependencyProvider` sao o trilho canonico de resolucao e registro de servicos.
- Logging/fail-fast: `DebugUtility`, `HardFailFastH1` e `ResetLogTags` concentram observabilidade e aborts H1.
- Identifiers: `IUniqueIdFactory` / `UniqueIdFactory` continuam como owner canonico de ActorId runtime.
- Legacy/dev candidates: `FilteredEventBus.Legacy`, `EventBusUtil`, `DebugManagerConfig`, `DebugLogSettings` e `Core/Fsm/**` nao apresentaram callsites runtime canonicos fora do `Core`; `InjectableEventBus` foi reclassificado como canonical por ser o backend concreto de `EventBus<T>`.

## Top candidates para CORE-1.2
| Bucket | Component | Acao recomendada |
|---|---|---|
| B MOVE | `Core/Events/FilteredEventBus.Legacy.cs` | mover para `Legacy/` ou marcar deprecated com plano de remocao |
| B MOVE | `Core/Logging/DebugManagerConfig.cs` | mover para trilho DevQA/Editor; arquivo ja esta sob `UNITY_EDITOR || DEVELOPMENT_BUILD` |
| B MOVE | `Core/Logging/DebugLogSettings.cs` | mover junto do config DevQA; sem callsites `.cs`, somente asset proprio |
| C RESERVE | `Core/Events/EventBusUtil.cs` | runtime helper sem callsite em `Modules/**`/`Infrastructure/**`; hook editor ja isolado em `Editor/**`, manter como reserva nao-dead |
| C RESERVE | `Core/Events/FilteredEventBus.cs` | sem callsites em `Modules/Infrastructure` e sem asset refs; manter no lugar como backend alternativo/reserva, nao-dead |
| C RESERVE | `Core/Fsm/StateMachine.cs` | sem callsites reais fora do Core e sem asset refs; manter como reserva de migracao |
| C RESERVE | `Core/Fsm/Transition.cs` | sem callsites reais fora do Core e sem asset refs; manter como reserva de migracao |
| C RESERVE | `Core/Validation/Preconditions.cs` | uso apenas interno ao Core (`Core/Fsm`), sem asset refs; manter como reserva para migracao futura |
| C RESERVE | `Core/Composition/SceneServiceCleaner.cs` | uso apenas interno ao Core via `SceneServiceRegistry`, sem asset refs; manter como reserva para migracao futura |

## Referencias canonicas
- Live doc: `Docs/Modules/Core.md`
- Snapshot audit: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v10.md`

## LEGACY/Compat
- `FilteredEventBus.Legacy.cs` movido para `Core/Events/Legacy/FilteredEventBus.Legacy.cs`.
- Namespace e tipos publicos mantidos intactos; arquivo segue apenas por compatibilidade.
- `DebugManagerConfig.cs` movido para `Dev/Core/Logging/DebugManagerConfig.cs` e `DebugLogSettings.cs` / `DebugLogSettings.asset` movidos para `Dev/Core/Logging`.

## Legacy != Dead / Promocao
- `FilteredEventBus.Legacy.cs` foi isolado em `Core/Events/Legacy/FilteredEventBus.Legacy.cs` por compatibilidade explicita.
- `DebugManagerConfig.cs` e `DebugLogSettings.cs` foram reclassificados como Dev-only com base em evidencia estatica local e movidos para `Dev/Core/Logging/`.
- Matriz atual de `DebugLogSettings`: classificacao `B dev-only`; sem loader runtime, sem refs em scene/prefab, asset referenciado apenas por ele mesmo.
- `Core/Fsm stack` (`StateMachine`, `Transition`, `IState`, `ITransition`, `IPredicate`) classificado como `C reserve`: sem callsites reais em `Modules/**`/`Infrastructure/**`, sem refs em assets e sem leak Editor; pode voltar a ser canônico durante migração futura.
- `Core/Validation/Preconditions.cs` classificado como `C reserve`: hoje so usado dentro do `Core` (`Core/Fsm/StateMachine.cs`), sem refs em assets e sem leak Editor; pode voltar a ser canônico em migrações futuras.
- `Core/Composition/SceneServiceCleaner.cs` classificado como `C reserve`: hoje so usado dentro do `Core` por `SceneServiceRegistry`, sem refs em assets e sem leak Editor; pode virar `A` se a limpeza de serviços de cena subir para um trilho mais explícito de composição.


## Reserve (nao e dead)
- `EventBusUtil`: status `C reserve`; sem callsites reais em `Modules/**`/`Infrastructure/**`, sem refs em assets, e com hook editor ja isolado em `Editor/Core/Events/EventBusUtil.Editor.cs`. Reserve/legacy pode voltar a `A` durante migracao legado -> canonico; nao remover sem evidencia nova.
- `FilteredEventBus<TScope, TEvent>` classificado como `C Reserve`: sem callsites reais hoje fora do Core, sem refs em assets, mas com relacao estrutural com `EventBusUtil` e com o wrapper legacy em `Core/Events/Legacy/FilteredEventBus.Legacy.cs`.
- `InjectableEventBus<T>` foi reclassificado como `A canonical`, nao `C Reserve`: ele e instanciado diretamente por `Core/Events/EventBus.cs` como `InternalBus` e sustenta `EventBus<T>.GlobalBus` por padrao.













## Reserve <> Legacy
- `C reserve` nao e `Legacy`: e estoque de migracao, mantido no trilho `Core` por ainda poder voltar a ser canonico sem restaurar API antiga.
- `Legacy` significa compatibilidade explicita ou trilho substituido, preferencialmente isolado em `Legacy/**` ou fora do runtime canonico.
- Durante migracao do legado, itens `C` podem virar `A`; nao remover ou mover sem evidencia nova.

## Promotion path
- `C -> A (Reserve -> Canonical)`: exige callsites reais em `Modules/**` e/ou `Infrastructure/**`, evidencia estatica (`rg` por simbolo e owner) e justificativa de ownership/contrato no audit.
- `A/C -> Legacy`: exige substituto canonico claro, `0` refs reais de codigo/asset para o componente alvo e evidencia de compatibilidade residual ou deprecacao documentada.
- Gates de PR/CI para promocao: strict editor leak sweep limpo, runtime init dentro da allowlist congelada e reserve promotion probe revisado sem falso positivo de comentario.
- Gates de PR/CI para rebaixamento a legacy: mesmo pacote de evidencias mais prova de substituto ativo e ausencia de callsites novos fora do Core.
- Lembrete: durante migracao do legado, itens `C` podem virar `A`; nao remover/mover sem evidencia nova.


