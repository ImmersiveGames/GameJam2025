# SA-7A6 — ActivityEntry Macro Phase Bridge Cleanup

## Status

Implementado para smoke manual.

## Objetivo

Reduzir a bridge grossa entre `ActivityEntryPipeline` e `SessionActivityPipeline` para fases macro da entry sem alterar o lifecycle macro de Activity.

## Decisão

`ActivityEntryPipeline` passa a internalizar a execução de:

- `BeginActivitySetupReadiness`;
- `EmitActivityParticipantReadiness`;
- `CompleteActivitySetupReadiness`.

Essas operações deixam de existir como métodos públicos/contratuais da `IActivityEntryRuntimeBridge`.

## Fronteira restante

`SessionActivityPipeline` ainda fornece primitivas transitórias de runtime state via `IActivityEntryRuntimeBridge`:

- reset de estado de movement control da entry;
- abertura de activity actor scope;
- limpeza factual de actor participations ativas;
- lookup de active player actor identities;
- emissão de visual readiness quando não há ActivationWindow.

Essas primitivas são bridge transitória. Elas não definem a ordem da entry.

## Owner correto

| Responsabilidade | Owner após SA-7A6 |
|---|---|
| Ordem de setup/readiness | `ActivityEntryPipeline` |
| Fases internas de setup/readiness | `ActivityEntryPipeline` |
| State storage transitório | runtime states/bridge |
| Macro lifecycle após readiness | `SessionActivityPipeline` |
| ActivationWindow/DeactivationWindow | `SessionActivityPipeline` |
| Restart/RouteExit | `SessionActivityPipeline` |

## Evidência esperada no smoke

- `ActivityEntrySetupReadinessStarted` com `phaseOwner='ActivityEntryPipeline'`;
- `ActivityEntrySetupReadinessCompleted` com `phaseOwner='ActivityEntryPipeline'`;
- `bridgeReduction='macro_phase_internalized'`;
- `macroLifecycleOwner='SessionActivityPipeline'` preservado no completed;
- `commandOwner='SessionActivityPipeline'` ausente.

## Próximo alvo

Separar stores da `ActivityEntryRuntimeBridge` em bridges menores ou runtime state accessors explícitos, sem tocar em Activation/Deactivation/RouteExit.
