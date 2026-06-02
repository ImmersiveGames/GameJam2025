# SA-7A5 — ActivityEntry final result / bridge reduction audit

## Status

`ActivityEntryPipeline` já é o owner da ordem de setup/readiness da entry. O `SessionActivityPipeline` ainda é owner do lifecycle macro: handoff, activation window, deactivation window, restart e route-exit.

Este corte não cria novo pipeline de exit.

## Resultado aplicado

`ActivityEntryPreparationResult` e `ActivityEntrySetupReadinessResult` deixam de ser apenas flags booleanas e passam a expor `Kind` explícito:

- `ActivityEntryPreparationResultKind.Prepared`
- `ActivityEntryPreparationResultKind.Failed`
- `ActivityEntryPreparationResultKind.RejectedStaleOrForeign`
- `ActivityEntrySetupReadinessResultKind.Completed`
- `ActivityEntrySetupReadinessResultKind.SkippedNoContent`
- `ActivityEntrySetupReadinessResultKind.Failed`
- `ActivityEntrySetupReadinessResultKind.RejectedStaleOrForeign`

O caminho ativo atual retorna `Prepared` e `Completed`. Os demais kinds existem para fechar o contrato sem voltar para `Accepted` prematuro ou bool ambíguo.

## Matriz de bridge atual

| Método / grupo | Uso atual | Owner correto final | Classificação | Ação neste corte |
|---|---|---|---|---|
| `BuildIdentity`, `SetCurrentIdentity` | State macro ainda hospedado no `SessionActivityPipeline` | State/context store próprio | Bridge transitória | Manter por enquanto |
| `EmitFact`, `EmitSnapshot` | Facts/snapshots ainda agregados pelo pipeline macro | Fact recorder / snapshot recorder próprio | Bridge transitória | Manter por enquanto |
| `LogEntryOwnerEvent`, `LogPhaseBoundary` | Observabilidade centralizada | Logger/fact recorder de entry | Bridge transitória aceitável | Manter, com resultKind explícito |
| `SetCurrentActivityContentLoadedSet`, `BuildActivityContentPendingOperation`, `SetPendingOperation`, `RunActivityContentOperation` | Content load ainda usa pending operation do pipeline macro | Stage/adapter de ActivityContent load | Bridge transitória de async scene load | Manter até corte de content load ownership |
| `ClearCurrentActivityContentLoadedSet`, `ClearCurrentActivityObjectContributorDiscoveryResult`, `ClearCurrentActivitySetupInventory`, `ClearCurrentActorInventoryFeedResult` | Limpeza de entry preparation | Runtime state stores próprios | Bridge transitória | Manter; já não usa `Accepted` |
| `BeginActivitySetupReadiness`, `ObserveActivitySceneContractOrSkip`, `EmitActivityParticipantReadiness`, `CompleteActivitySetupReadiness` | Fases macro/facts ainda materializadas no pipeline antigo | ActivityEntryPipeline + stages/facts | Bridge de lifecycle residual | Próximo alvo seguro |

## Separação macro preservada

Continua fora do `ActivityEntryPipeline`:

- `ActivationWindow`
- `DeactivationWindow`
- `RestartCurrentActivity`
- `RouteExitBackToMenu`
- operação assíncrona de cenas via pending operation

## Próximo corte recomendado

`SA-7A6 — ActivityEntry macro phase bridge cleanup`

Foco:

1. reduzir `BeginActivitySetupReadiness`, `EmitActivityParticipantReadiness` e `CompleteActivitySetupReadiness` como bridge;
2. manter `ActivationWindow` e `DeactivationWindow` no `SessionActivityPipeline`;
3. não mexer em actor setup, participant binding, movement, camera ou route-exit.
