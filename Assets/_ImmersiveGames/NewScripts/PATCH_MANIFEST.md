# PATCH MANIFEST

Contexto: `SA-17D closure docs + ADR index hygiene`

## Arquivos alterados

- `Docs/Reports/SessionActivity-2.0-Current-Status.md`
- `Docs/ADRs/ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md`
- `SessionActivity/Pipeline/README.md`
- `PATCH_MANIFEST.md`

## Arquivos removidos

- `Docs/Architecture/Plan-2.0-SessionActivity-Refactor.md`
- `Docs/Architecture/Plan-2.0-SessionActivity-Refactor.md.meta`
- `Docs/Reports/ACTIVITY-OBJECT-CAPACITY-1A-FIX1-Compile-Fix.md`
- `Docs/Reports/ACTIVITY-OBJECT-CAPACITY-1A-FIX1-Compile-Fix.md.meta`
- `Docs/Reports/ACTIVITY-OBJECT-CAPACITY-1A-FIX2-Restore-Runtime-References.md`
- `Docs/Reports/ACTIVITY-OBJECT-CAPACITY-1A-FIX2-Restore-Runtime-References.md.meta`
- `Docs/Reports/ACTIVITY-OBJECT-CAPACITY-1A-Object-Lifecycle-Contribution-Provider.md`
- `Docs/Reports/ACTIVITY-OBJECT-CAPACITY-1A-Object-Lifecycle-Contribution-Provider.md.meta`
- `Docs/Reports/ACTOR-CAPACITY-3A-Passive-Actor-Snapshot-Restore-Release-Contracts.md`
- `Docs/Reports/ACTOR-CAPACITY-3A-Passive-Actor-Snapshot-Restore-Release-Contracts.md.meta`

## Motivo do corte

- consolidar o status atual de `SessionActivity` em um unico documento canonico;
- remover espelho documental redundante;
- reduzir repeticao de matriz de fechamento em multiplos README/planos;
- manter ADRs normativos intactos;
- registrar `SA-17D` e `SA-17D-FIX` como `CLOSED / PASS funcional + PASS arquitetural`;
- reafirmar `ActivityObjectExitRuntimeState` como owner tecnico da correlation;
- preservar `NoActivityContentContributors / no_activity_content_contributors` para `activity_02` sem reabrir a policy geral de `RouteActivitySave`.

## Observacao

Este corte altera documentacao canonica, sem executar build, tests ou smoke.
Nesta rodada, a limpeza ficou restrita a indices e ADR canonico; runtime C# nao foi alterado.
