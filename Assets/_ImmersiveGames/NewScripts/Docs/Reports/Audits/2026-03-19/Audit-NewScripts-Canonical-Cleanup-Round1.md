# Audit NewScripts Canonical Cleanup Round 1

## 1. Resumo executivo
- Estado encontrado: coexistência de trilho canônico com trilho `Dev/QA`, além de histórico documental operacional antigo em `Docs/Reports/**`.
- Redundância/legado detectado: alta em runtime auxiliar (arquivos `Dev`/`*.DevQA.cs`) e em reports históricos (audits 2026-03-12/13, baseline congelado e log runtime antigo).
- Limpeza executada: remoção agressiva de trilho `Dev/QA`, remoção de docs históricas não canônicas em reports, limpeza de placeholders vazios e alinhamento dos ponteiros `LATEST`.

## 2. Trilho canônico atual detectado
- composition/bootstrap: `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` + `GlobalCompositionRoot.Pipeline.cs` + parciais runtime (`SceneFlow`, `GameLoop`, `LevelFlow`, `Navigation`, `WorldLifecycle`, `InputModes`, `RuntimePolicy`).
- scene flow: `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` com rotas em `Modules/SceneFlow/Navigation/Bindings/*` e loading em `Modules/SceneFlow/Loading/Runtime/*`.
- navigation: `Modules/Navigation/GameNavigationCatalogAsset.cs` + `GameNavigationService.cs` + coordenadores `ExitToMenuCoordinator` e `MacroRestartCoordinator`.
- level flow: `Modules/LevelFlow/Runtime/*` com `LevelFlowRuntimeService`, `LevelMacroPrepareService`, `LevelSwapLocalService` e `LevelStageOrchestrator`.
- world lifecycle: `Modules/WorldLifecycle/Runtime/*` + `WorldRearm/*` + `SceneScopeCompositionRoot.cs` (registro de escopo e hooks de cena).
- runtime/gameplay: `Modules/Gameplay/Runtime/*`, `Modules/GameLoop/Runtime/*`, `Modules/PostGame/*`, `Modules/Gates/*`, `Modules/InputModes/*`.
- presentation: loading HUD (`Modules/SceneFlow/Loading/*`), fade (`Modules/SceneFlow/Fade/*`), bindings de gameplay/navigation em `Modules/*/Bindings/*` canônicos.
- tooling/editor/qa: mantido apenas tooling editor canônico de validação/normalização; removido trilho `Dev/QA` de contexto de runtime auxiliar.

## 3. Itens removidos
| Path | Type | Category | Reason | Canonical Replacement (if any) |
|---|---|---|---|---|
| `Assets/_ImmersiveGames/NewScripts/Docs/Plans.meta` | UnityMeta | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | Docs/Canon/Canon-Index.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Plans/Plan-Continuous.md` | Doc | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | Docs/Canon/Canon-Index.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Plans/Plan-Continuous.md.meta` | UnityMeta | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | Docs/Canon/Canon-Index.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/DOCS-CURRENT-STATE-CLEANUP.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/DOCS-CURRENT-STATE-CLEANUP.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/DOCS-FINAL-CLOSEOUT.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/DOCS-FINAL-CLOSEOUT.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/INTRO-LEVEL-AND-POSTGAME-GLOBAL.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/INTRO-LEVEL-AND-POSTGAME-GLOBAL.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/LOADING-DOCS-CLOSEOUT.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/LOADING-DOCS-CLOSEOUT.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/LOADING-HUD-SCENE-INTEGRATION.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/LOADING-HUD-SCENE-INTEGRATION.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/LOADING-PROGRESS-BAR-AND-SPINNER.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/LOADING-PROGRESS-BAR-AND-SPINNER.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/PRODUCTION-GUIDES-FULL-PRACTICAL-DEEPENING.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/PRODUCTION-GUIDES-FULL-PRACTICAL-DEEPENING.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-13.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-13/BASELINE-V3-BLOCKERS-FIX.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-13/BASELINE-V3-BLOCKERS-FIX.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-13/BASELINE-V3-CLOSEOUT.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-13/BASELINE-V3-CLOSEOUT.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-13/BASELINE-V3-OUTCOME-MOCK-FIX.md` | Doc | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-13/BASELINE-V3-OUTCOME-MOCK-FIX.md.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Docs/Reports/Audits/LATEST.md -> 2026-03-19 |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline.meta` | UnityMeta | REMOVE_UNUSED | Evidencia/baseline antigo sem papel canônico atual; substituído por ponteiros LATEST. | Docs/Reports/Evidence/LATEST.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline/2026-03-13.meta` | UnityMeta | REMOVE_UNUSED | Evidencia/baseline antigo sem papel canônico atual; substituído por ponteiros LATEST. | Docs/Reports/Evidence/LATEST.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline/2026-03-13/Baseline-V3-Freeze.md` | Doc | REMOVE_UNUSED | Evidencia/baseline antigo sem papel canônico atual; substituído por ponteiros LATEST. | Docs/Reports/Evidence/LATEST.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline/2026-03-13/Baseline-V3-Freeze.md.meta` | UnityMeta | REMOVE_UNUSED | Evidencia/baseline antigo sem papel canônico atual; substituído por ponteiros LATEST. | Docs/Reports/Evidence/LATEST.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log` | Log | REMOVE_UNUSED | Evidencia/baseline antigo sem papel canônico atual; substituído por ponteiros LATEST. | Docs/Reports/Evidence/LATEST.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log.meta` | UnityMeta | REMOVE_UNUSED | Evidencia/baseline antigo sem papel canônico atual; substituído por ponteiros LATEST. | Docs/Reports/Evidence/LATEST.md |
| `Assets/_ImmersiveGames/NewScripts/Docs/Shared.meta` | UnityMeta | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | N/A |
| `Assets/_ImmersiveGames/NewScripts/Docs/Standards.meta` | UnityMeta | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | N/A |
| `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs |
| `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs |
| `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Infrastructure/Composition/SceneScopeCompositionRoot.cs |
| `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Infrastructure/Composition/SceneScopeCompositionRoot.cs |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Bindings.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Runtime.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Runtime/ContentSwapDevInstaller.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Runtime/ContentSwapDevInstaller.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Editor.meta` | UnityMeta | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | N/A |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs` | CSharp | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | N/A |
| `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Editor/ContentSwapQaMenuItems.cs.meta` | UnityMeta | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | N/A |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Dev/GameLoopSceneFlowCoordinator.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Dev/GameLoopSceneFlowCoordinator.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev/IntroStageDevContextMenu.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev/IntroStageDevContextMenu.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev/IntroStageDevInstaller.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev/IntroStageDevInstaller.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev/IntroStageDevTester.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev/IntroStageDevTester.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev/IntroStageRuntimeDebugGui.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Dev/IntroStageRuntimeDebugGui.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Editor.meta` | UnityMeta | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | N/A |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs` | CSharp | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | N/A |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Editor/IntroStageQaMenuItems.cs.meta` | UnityMeta | REMOVE_PLACEHOLDER | Artefato vazio/auxiliar sem função canônica após cleanup estrutural. | N/A |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Pause/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Pause/Dev/PauseOverlayController.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Pause/Dev/PauseOverlayController.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Config/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Config/Dev/SceneBuildIndexRef.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Config/Dev/SceneBuildIndexRef.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Dev/LevelFlowDevInstaller.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Dev/LevelFlowDevInstaller.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Dev/GameNavigationCatalogAsset.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Dev/GameNavigationCatalogAsset.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Dev/MenuQuitButtonBinder.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Dev/MenuQuitButtonBinder.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/PostGame/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/PostGame/Dev/PostGameOverlayController.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/PostGame/Dev/PostGameOverlayController.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevInstaller.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevInstaller.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Dev/SceneRouteCatalogAsset.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Dev/SceneRouteCatalogAsset.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Dev/SceneRouteDefinitionAsset.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Dev/SceneRouteDefinitionAsset.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Dev/SceneTransitionService.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Dev/SceneTransitionService.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev/SceneRouteResetPolicy.DevQA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev/SceneRouteResetPolicy.DevQA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev/WorldLifecycleHookLoggerA.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyBridge.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyBridge.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyDevBootstrap.cs` | CSharp | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Dev/WorldResetRequestHotkeyDevBootstrap.cs.meta` | UnityMeta | REMOVE_SUPERSEDED | Trilho Dev/QA ou historico de auditoria antigo removido para manter somente rail canônico atual. | Runtime canônico do módulo correspondente (sem trilho Dev/QA) |

## 4. Itens revisados mas mantidos
| Path | Responsibility | Why Kept |
|---|---|---|
| `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` | Composition/bootstrap global | RuntimeInitializeOnLoad canônico do rail NewScripts. |
| `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs` | Pipeline de instalação canônica | Orquestra módulos de produção sem rail Dev/QA. |
| `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs` | SceneFlow bootstrap | Registra transition/loading/routes no trilho principal. |
| `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.cs` | Scene scope lifecycle | Fonte canônica para registro/limpeza de serviços por cena. |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | Scene transition runtime | Serviço central de transição de cenas. |
| `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs` | Catálogo de rotas | Asset canônico de rotas para resolução fail-fast. |
| `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs` | Catálogo de intents de navegação | Fonte única de verdade para intents de navegação. |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs` | Level flow runtime | Coordenação canônica de lifecycle de level. |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs` | Prepare macro de level | Etapa canônica do pipeline macro antes de swap local. |
| `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelSwapLocalService.cs` | Swap local de level | Execução canônica de troca intra-macro. |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleOrchestrator.cs` | World lifecycle orchestrator | Coordenação principal de reset/lifecycle de mundo. |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldResetRequestService.cs` | Entrada de reset | Serviço canônico de solicitação de reset. |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldResetCommands.cs` | Comandos de reset | Comandos canônicos de execução de reset. |
| `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Services/GameLoopService.cs` | Game loop runtime | Owner canônico do estado macro da run. |
| `Assets/_ImmersiveGames/NewScripts/Modules/PostGame/PostGameResultService.cs` | Post game global | Resultado formal global (Victory/Defeat/Exit). |
| `Assets/_ImmersiveGames/NewScripts/Modules/PostGame/PostGameOwnershipService.cs` | Ownership post game | Ownership canônico de estado pós-jogo. |
| `Assets/_ImmersiveGames/NewScripts/Modules/InputModes/InputModeService.cs` | Input mode service | Serviço canônico de sincronização de input mode. |
| `Assets/_ImmersiveGames/NewScripts/Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` | Bridge runtime canônica | Bridge ativa entre route kind e input mode. |
| `Assets/_ImmersiveGames/NewScripts/Modules/Gates/SimulationGateService.cs` | Simulation gate | Gate canônico de pause/resume/simulação. |
| `Assets/_ImmersiveGames/NewScripts/Core/Events/Legacy/FilteredEventBus.Legacy.cs` | Compat externa obrigatória | Mantido por uso ativo em Assets/_ImmersiveGames/Scripts/**. |
| `Assets/_ImmersiveGames/NewScripts/Docs/README.md` | Índice canônico de docs | Atualizado para nova superfície canônica pós-cleanup. |
| `Assets/_ImmersiveGames/NewScripts/Docs/Canon/Canon-Index.md` | Contrato canônico | Atualizado com trilho oficial e ponteiros LATEST. |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/LATEST.md` | Ponteiro de auditoria | Aponta para o relatório vigente desta rodada. |
| `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md` | Ponteiro de evidência | Ponteiro único de evidência vigente. |
| `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/README.md` | Mapa de ADRs vigentes | Mantém documentação arquitetural vigente. |
| `Assets/_ImmersiveGames/NewScripts/Docs/Guides/Production-How-To-Use-Core-Modules.md` | Guia operacional | Guia principal de operação canônica. |

## 5. Arquivos vazios / placeholders / stubs encontrados
- Arquivos zero-byte encontrados: nenhum.
- Placeholders/stubs removidos nesta rodada:
  - `Assets/_ImmersiveGames/NewScripts/Docs/Shared.meta` (placeholder sem pasta ativa).
  - `Assets/_ImmersiveGames/NewScripts/Docs/Standards.meta` (placeholder sem pasta ativa).
  - `Assets/_ImmersiveGames/NewScripts/Docs/Plans/**` (plano contínuo histórico fora da superfície canônica atual).
  - `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Editor/**` (QA menu obsoleto).
  - `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/IntroStage/Editor/**` (QA menu obsoleto).

## 6. Fluxos paralelos, duplicidades e resíduos de arquitetura antiga
- Conflito: coexistência de runtime canônico com trilho `Dev/QA` no bootstrap global (`CompositionInstallStage.DevQa` + parciais `*.DevQA.cs`).
  - Canônico mantido: pipeline de produção em `GlobalCompositionRoot.Pipeline.cs` sem estágio DevQa.
  - Removido: `GlobalCompositionRoot.DevQA.cs`, `SceneScopeCompositionRoot.DevQA.cs` e módulos `Dev/**` associados.
- Conflito: documentação de reports históricos misturada com ponteiros vigentes.
  - Canônico mantido: `Docs/Reports/Audits/LATEST.md` e `Docs/Reports/Evidence/LATEST.md` atualizados.
  - Removido: relatórios datados antigos e baseline congelado antigo em `Docs/Reports/Baseline/**`.

## 7. Docs removidas ou consolidadas
- Removidas por superação/histórico operacional antigo:
  - `Docs/Reports/Audits/2026-03-12/**`
  - `Docs/Reports/Audits/2026-03-13/**`
  - `Docs/Reports/Baseline/**`
  - `Docs/Reports/lastlog.log`
  - `Docs/Plans/Plan-Continuous.md`
- Consolidadas via ponteiros vigentes:
  - `Docs/Reports/Audits/LATEST.md`
  - `Docs/Reports/Evidence/LATEST.md`

## 8. Pendências e review manual
- Build pós-limpeza no `MSBuild` ainda falha com `CS2001` porque o `Assembly-CSharp.csproj` estava desatualizado listando fontes Dev/QA removidas.
- Tentativa de refresh por Unity batch (`2026-03-19`) abortou porque o projeto estava aberto em outra instância (`HandleProjectAlreadyOpenInAnotherInstance`).
- Compat externa mantida intencionalmente: `Core/Events/Legacy/FilteredEventBus.Legacy.cs` segue necessária por uso ativo em `Assets/_ImmersiveGames/Scripts/**`.

## 9. Sanity checks executados
- Busca de referências remanescentes a trilho Dev/QA em `NewScripts` (`DevQA`, installers/context menus QA, hooks Dev): resultado **sem ocorrências**.
- Verificação de `.meta` órfãos em `NewScripts`: resultado **NO_ORPHAN_META**.
- Verificação de diretórios vazios após limpeza: resultado **nenhum diretório vazio remanescente**.
- Build: `MSBuild Assembly-CSharp.csproj` executado; falha observada por `CS2001` apontando arquivos removidos ainda listados no `.csproj` gerado anteriormente.
- Refresh Unity batch: tentativa executada; bloqueada por instância já aberta do projeto.

## 10. Resumo final do estado pós-limpeza
- `NewScripts` saiu sem trilho `Dev/QA` em runtime/bootstrap e sem menus/contextos QA obsoletos desse rail.
- `Docs` foi reduzida para superfície canônica + ADRs + ponteiros `LATEST`, removendo histórico operacional antigo em reports.
- Estrutura canônica remanescente está centrada em composition global + scene scope + módulos runtime (SceneFlow/Navigation/LevelFlow/WorldLifecycle/GameLoop/Gameplay/PostGame/InputModes/Gates).
- Próximo alvo natural (Round 2): regenerar artefatos de projeto Unity com editor fechado e revalidar build clean para eliminar erros de `.csproj` desatualizado.


## Fechamento 2026-03-25

- O plano `Plan-MacroFlow-Stack-Consolidation.md` foi conclu�do e absorveu os pontos residuais do stack macro.
- `SceneFlow`, `LevelFlow`, `Navigation`, `ResetInterop` e `GameLoop` ficaram com boundaries mais claras do que o estado descrito neste audit.
- Este documento permanece como ponte hist�rica, sem gerar novo backlog.
