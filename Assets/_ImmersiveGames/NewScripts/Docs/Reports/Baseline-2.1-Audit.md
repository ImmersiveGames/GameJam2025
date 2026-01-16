# Baseline 2.1 — Auditoria QA (Read-only)

Relatório curto com inventário e classificação do QA atual, sem alterações de comportamento.

## Tabela de referência (QA e Baseline)

| Arquivo | Responsabilidade | Versão | Status |
|---|---|---|---|
| `Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Baseline2SmokeLastRunTool.cs` | Captura do log smoke + geração do relatório Markdown do Baseline 2.0. | 2.0 | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Verifier/Baseline2ChecklistDrivenVerifier.cs` | Validação checklist-driven do Baseline 2.0 a partir do log. | 2.0 | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Verifier/Baseline2ChecklistDrivenVerifierMenu.cs` | Menus do editor para verificar e gerar relatório checklist-driven. | 2.0 | ativo |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Checklist.md` | Checklist operacional (evidências hard) do Baseline 2.0. | 2.0 | ativo |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Spec.md` | Spec congelada (histórica) usada pelo relatório do Baseline 2.0. | 2.0 | ativo |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.log` | Log canônico do smoke 2.0 (fonte de verdade). | 2.0 | ativo |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.md` | Relatório Markdown gerado a partir do log smoke 2.0. | 2.0 | ativo |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md` | Resultado do verificador checklist-driven do Baseline 2.0. | 2.0 | ativo |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Observability-Contract.md` | Contrato de observabilidade (fonte de verdade) para validar Baseline 2.x. | 2.x | ativo |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.1-Smoke-LastRun.log` | Log de smoke 2.1 já existente (sem tooling dedicado no código atual). | 2.1 | desconhecido |
| `Assets/_ImmersiveGames/NewScripts/QA/IntroStage/IntroStageQaContextMenu.cs` | Context menu de QA do IntroStage (execução em runtime). | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/IntroStage/IntroStageQaInstaller.cs` | Installer do QA IntroStage (setup/registro). | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/IntroStage/Editor/IntroStageQaMenuItems.cs` | Menus QA do IntroStage (Complete/Skip). | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/Editor/IntroStageQaTools.cs` | Menu QA utilitário para selecionar QA_IntroStage. | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaDummyActor.cs` | Dummy actor para QA de reset. | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaProbe.cs` | Probe de QA para reset (instrumentação). | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaSpawner.cs` | Spawner de QA para reset. | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetKindQaEaterActor.cs` | Actor de QA para validar reset por tipo. | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetRequestQaDriver.cs` | Driver de QA para requests de reset. | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/Marco0QaToolsContextMenu.cs` | Context menu QA relacionado a WorldLifecycle/Marco0. | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/Marco0PhaseObservabilityQaRunner.cs` | Runner de QA para observabilidade de fases. | n/a | ativo |
| `Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/WorldLifecycleMultiActorSpawnQa.cs` | QA de spawn multi-actor no WorldLifecycle. | n/a | ativo |

## Candidatos a obsoleto (revisão sugerida)

- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/*` (reports arquivados). 
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-Docs-Inventory.md` (audit pontual). 
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-Reports-Cleanup.md` (audit pontual). 
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-ResetTrigger-Production.md` (audit pontual). 
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-Audit-2026-01-03.md` (audit datado). 
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/ResetWorld-Audit-2026-01-05.md` (audit datado). 

## Paths encontrados

### Assets/_ImmersiveGames/NewScripts/QA/**

```
Assets/_ImmersiveGames/NewScripts/QA/IntroStage.meta
Assets/_ImmersiveGames/NewScripts/QA/IntroStage/IntroStageQaContextMenu.cs
Assets/_ImmersiveGames/NewScripts/QA/IntroStage/Editor.meta
Assets/_ImmersiveGames/NewScripts/QA/IntroStage/IntroStageQaContextMenu.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/IntroStage/Editor/IntroStageQaMenuItems.cs
Assets/_ImmersiveGames/NewScripts/QA/IntroStage/Editor/IntroStageQaMenuItems.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/IntroStage/IntroStageQaInstaller.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/IntroStage/IntroStageQaInstaller.cs
Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaDummyActor.cs
Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaProbe.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/Baseline2.meta
Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaProbe.cs
Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetKindQaEaterActor.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetKindQaEaterActor.cs
Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetRequestQaDriver.cs
Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetRequestQaDriver.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/Marco0QaToolsContextMenu.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/Marco0QaToolsContextMenu.cs
Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/WorldLifecycleMultiActorSpawnQa.cs
Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/Marco0PhaseObservabilityQaRunner.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/Marco0PhaseObservabilityQaRunner.cs
Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle/WorldLifecycleMultiActorSpawnQa.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/WorldLifecycle.meta
Assets/_ImmersiveGames/NewScripts/QA/Editor.meta
Assets/_ImmersiveGames/NewScripts/QA/Editor/IntroStageQaTools.cs
Assets/_ImmersiveGames/NewScripts/QA/Editor/IntroStageQaTools.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaSpawner.cs
Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaDummyActor.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/GameplayResetKindQaSpawner.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Verifier/Baseline2ChecklistDrivenVerifier.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Verifier/Baseline2ChecklistDrivenVerifierMenu.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Verifier/Baseline2ChecklistDrivenVerifierMenu.cs
Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Verifier/Baseline2ChecklistDrivenVerifier.cs
Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Baseline2SmokeLastRunTool.cs
Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Baseline2SmokeLastRunTool.cs.meta
Assets/_ImmersiveGames/NewScripts/QA/Baseline2/Verifier.meta
Assets/_ImmersiveGames/NewScripts/QA/GameplayReset.meta
```

### Assets/_ImmersiveGames/NewScripts/Docs/Reports/**

```
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Checklist.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Checklist-Phase.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/QA-GameplayReset-RequestMatrix.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Spec.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Reason-Map.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/ResetWorld-Audit-2026-01-05.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-Reports-Cleanup.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Checklist.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Checklist-Phase.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-ResetTrigger-Production.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.log
Assets/_ImmersiveGames/NewScripts/Docs/Reports/QA-PhaseChange-Smoke.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.log.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/QA-GameplayResetKind.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/README.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-Audit-2026-01-03.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Production-EndToEnd-Validation.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Observability-Contract.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/QA-PhaseChange-Smoke.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/QA-GameplayResetKind.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/GameLoop.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Spec.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Production-EndToEnd-Validation.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/GameLoop.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-Reports-Cleanup.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Assets-Checklist.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-ResetTrigger-Production.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Observability-Contract.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/QA-IntroStage-Smoke.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Assets-Checklist.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.1-Smoke-LastRun.log.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/QA-GameplayReset-RequestMatrix.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/QA-IntroStage-Smoke.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/README.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Reason-Map.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Profile-Audit.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Gameplay-Blockers-Report.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Production-Evidence-2025-12-31.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/Report-SceneFlow-Production-Log-2025-12-31.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Gameplay-Blockers-Report.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/QA-Audit-2025-12-27.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Production-Evidence-2025-12-31.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/Report-SceneFlow-Production-Log-2025-12-31.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Profile-Audit.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/QA-Audit-2025-12-27.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Gameplay-To-Menu-Report.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/Marco0-Phase-Observability-Checklist.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/Marco0-Phase-Observability-Checklist.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Smoke-Result.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/Legacy-Cleanup-Report.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Gameplay-To-Menu-Report.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/Legacy-Cleanup-Report.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025/SceneFlow-Smoke-Result.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Archive/2025.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-Docs-Inventory.md
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.1-Smoke-LastRun.log
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audit-Docs-Inventory.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/ResetWorld-Audit-2026-01-05.md.meta
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-Audit-2026-01-03.md.meta
```
