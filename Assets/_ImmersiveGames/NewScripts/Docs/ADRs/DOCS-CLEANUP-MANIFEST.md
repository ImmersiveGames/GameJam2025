# Limpeza documental — Base 1.1

## Objetivo

Eliminar ambiguidade documental na pasta `Docs/ADRs` e manter apenas a fonte normativa viva da Base 1.1.

## Fonte normativa viva após esta limpeza

Manter na raiz de `Assets/_ImmersiveGames/NewScripts/Docs/ADRs` apenas:

```text
README.md
ADR-0001-Base-1.1-Pipeline-Convergence-e-Identidade-Explicita.md
ADR-0002-Run-Pipeline-Canonico.md
ADR-0003-Session-Operational-Pipeline.md
ADR-0004-Session-Activity-Pipeline.md
ADR-0005-Modules-Facts-Commands-Adapters.md
ADR-0006-Route-Scene-Composition-Fade-Loading-Audio.md
ADR-0007-Gates-InputModes-e-Simulation-Executors.md
ADR-0008-SaveSystem-Canonico.md
ADR-0009-Session-Player-Slots-e-Operational-Input-Runtime.md
ADR-0010-Player-Preparation-Player-Participation-e-Unity-PlayerInput.md
ADR-0011-Runtime-Configuration-Registry-e-Config-Sets.md
ADR-0012-Operational-Camera-Runtime-e-Future-Activity-Camera-Binding.md
ADR-0013-Camera-Presentation-Runtime-e-Activity-Camera-Director.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline.md
```

## Arquivos antigos/duplicados que devem sair da raiz dos ADRs

Mover para `Docs/ADRs/Historico/` ou remover se forem cópias locais/intermediárias:

```text
ADR-0003-Session-Operational-Pipeline.updated.md
ADR-0004-Session-Activity-Pipeline.updated.md
ADR-0004-Checkpoint-SessionActivity-ActivityTransition-MVP.md
ADR-0008-SaveSystem-Canonico-ATUALIZADO.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-ATUALIZADO.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-F4D7.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-F4D8.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-F6B.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-F6C.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-F6D-FINAL.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-F6E-FINAL.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-F6E-FINAL-CORRIGIDO.md
ADR-0014-ActivityContent-WindowTemplateLibrary-e-ActivityEntryPipeline-otimizado.md
ADR-0014-Implementation-Matrix.md
RuntimeModeConfig-Migration-Matrix.md
REORGANIZATION-SUMMARY.md
MIGRATION-MAP.md
```

## Regra final de leitura

```text
ADRs de ADR-0001 a ADR-0014 são a única fonte normativa viva da Base 1.1.
ADRs anteriores e arquivos intermediários são histórico, não contrato ativo.
Em conflito, prevalece a Base 1.1 viva.
```

## Checkpoint documental aplicado

```text
Progression Save MVP — RouteActivitySave + ActivityObjectSnapshotRestore — PASS funcional e semântico
ActivityObjectSnapshotContractValidation -> ActivityObjectReset -> ActivityObjectSnapshotRestore
Restore sem payload = Skipped
Restore com payload = Passed + restoreVerified=true
```

## Dívidas não bloqueantes preservadas

```text
captureTargetTransformPath: melhorar propagação/observabilidade no payload restaurado.
Run Pipeline materializado: ainda futuro.
Progression Save genérico: ainda futuro além do MVP test_object_01.
Gameplay input final / PlayerActor final: ainda futuro.
ActivityEntryPipeline completo/ObjectEntry genérico: ainda futuro.
```
