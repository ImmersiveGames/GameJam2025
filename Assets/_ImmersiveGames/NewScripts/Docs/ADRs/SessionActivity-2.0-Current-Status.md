# SessionActivity 2.0 — Current Status

**Atualizado:** 2026-06-14  
**Escopo:** Base 2.0 / SessionActivity / SA-19 normalization

## Status consolidado recente

```text
BASE-ID identity stabilization — CLOSED FOR NOW
SA-19B2-COMPLEX-AUDIT — CLOSED / No runtime changes
SA-19B2-G1 — CLOSED / PASS funcional + PASS arquitetural
SA-19B2-G2-H1 — CLOSED / PASS
SA-19B2-G2 — CLOSED / PASS funcional + PASS arquitetural
```

## Fechamento SA-19B2-G1/G2

### G1 — Entry Setup Composite + Participant Resolution Narrowing

- `ActivityCapabilityInventoryCoordinator` foi removido do caminho ativo.
- O preview de inventory passou a usar `IActivityCapabilityInventoryPreviewSource` / `ActivityCapabilityInventoryPreviewSource`.
- `ActivityEntryPipeline` continuou dono da ordem de setup.
- Retained-player path foi preservado.

### G2 — ActivityEntryObjectSetup Composite Split

O antigo composite ativo foi dividido em stages concretos:

- `ActivityEntryObjectContributorDiscoveryStage`
- `ActivityEntrySetupInventoryStage`
- `ActivityEntryCapabilityInventoryPreviewStage`
- `ActivityEntryObjectResetStage`
- `ActivityEntryObjectSnapshotRestoreStage`

`ActivityEntryObjectSetupStages.cs` permanece aceito apenas como utility compartilhada, sem ownership de lifecycle.

### G2-H1 — Owner label hygiene

- O label legado `owner='ActivityEntryObjectSetupStages'` foi removido do caminho ativo.
- Os owners concretos aparecem em log/fact:
  - `ActivityEntryObjectContributorDiscoveryStage`
  - `ActivityEntryCapabilityInventoryPreviewStage`

## Evidência aceita

```text
sem error CS
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem foreign/stale indevido
sem fallback silencioso
sem ActivityCapabilityInventoryCoordinator no caminho ativo
sem owner='ActivityEntryObjectSetupStages' no caminho ativo
ActivityCapabilityInventoryPreviewObserved preservado
ActivityObjectContributorDiscovery Passed preservado
ActivityObjectReset PassedApplied em activity_01 preservado
ActivityObjectReset PassedNoCommands em activity_02 preservado
ActivityObjectSnapshotRestore Passed quando payload existe
CapabilitySnapshotEnvelopeCapture Passed preservado
ActivityObjectRelease Passed preservado
ActivityObjectContributorUnregister Passed preservado
ActivityEntryParticipantBindingCompleted preservado
ActivityEntryParticipantResetCompleted preservado
ActivityParticipantResetAppliedFromInventory preservado
ActivityParticipantActorMaterializationRetained preservado
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

## Próximos passos permitidos

- Não reabrir `BASE-ID` sem evidência concreta.
- Não reabrir ObjectSetup composite sem regressão concreta.
- Não iniciar Content unload/release apenas por limpeza; auditoria recente classificou como owner correto / baixo retorno.
- Se continuar `SA-19B2`, auditar primeiro:
  - `ActivityEntryParticipantBindingStage` / participant binding completo;
  - `ActivityExitActorTeardownStage`.
- Depois de B2, seguir para `SA-19C0/C1/C2` Host thinness + Route-Exit ownership.

## Regras preservadas

```text
ActivityEntryPipeline decide ordem.
Stages executam passos determinísticos.
Commands carregam payload runtime resolvido.
Facts/logs não executam side-effects.
Inventory é índice técnico.
Registry é índice técnico.
Sem fallback silencioso.
Sem bridge/coordinator genérico novo.
Sem compat desnecessária.
```
