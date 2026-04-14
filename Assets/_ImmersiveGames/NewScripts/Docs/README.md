# NewScripts Docs

Esta pasta separa o canon vivo, a documentacao operacional e o historico arquivado do Baseline 4.0.

## Canon vivo

- `Docs/ADRs/ADR-0052-Session-Transition-Composicao-de-Eixos-Acima-do-Baseline.md`
- `Docs/ADRs/ADR-0045-Gameplay-Runtime-Composition-Centro-Semantico-do-Gameplay.md`
- `Docs/ADRs/ADR-0046-GameplaySessionFlow-como-primeiro-bloco-interno-do-Gameplay-Runtime-Composition.md`
- `Docs/ADRs/ADR-0047-Gameplay-Phase-Construction-Pipeline-dentro-do-GameplaySessionFlow.md`
- `Docs/ADRs/ADR-0048-PhaseDefinition-como-fonte-de-verdade-autoral-da-fase-jogavel.md`
- `Docs/ADRs/ADR-0049-Fluxo-Canonico-de-Fim-de-Run-e-PostRun.md`
- `Docs/ADRs/ADR-0050-IntroStage-Canonical-Content-Presenter-Hook.md`
- `Docs/ADRs/ADR-0030-Fronteiras-Canonicas-do-Stack-SceneFlow-Navigation-LevelFlow.md`
- `Docs/ADRs/ADR-0031-Pipeline-Canonico-da-Transicao-Macro.md`
- `Docs/ADRs/ADR-0032-Semantica-Canonica-de-Route-Level-Reset-e-Dedupe.md`
- `Docs/ADRs/ADR-0038-Modular-DI-Registration-and-Module-Installers.md`
- `Docs/ADRs/ADR-0039-Canonical-Scene-Identity-and-Addressables-Seam.md`

## Leitura atual

- A camada acima do baseline para transformacao composta de sessao/runtime e `Session Transition`, documentada em `ADR-0052`.
- O centro semantico do gameplay e `Gameplay Runtime Composition`.
- `GameplaySessionFlow` e o primeiro bloco interno desse centro.
- `PhaseDefinition` e a fonte de verdade autoral da phase.
- `IntroStage` e scene-local, post-`SceneTransitionCompleted`.
- `RunResultStage` e `RunDecision` sao os owners atuais do fim de run.

## Historico e archive

- Termos como `WorldLifecycle`, `ContentSwap`, `LevelManager`, `LevelLifecycle`, `LevelFlow`, `PostGame`, `PostPlay` e `GameOver` sao historicos.
- `Docs/Archive/` e `Docs/Reports/` guardam material historico e evidencia; nao competem com o canon vivo.
- `ADR-0044` permanece como consolidacao historica do baseline antigo, nao como base operacional principal.

## Entradas ativas

- `Docs/Modules/README.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Gameplay.md`
- `Docs/Modules/InputModes.md`
- `Docs/Modules/Navigation.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/Save.md`
- `Docs/Modules/WorldReset.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
- `Docs/Guides/Event-Hooks-Reference.md`
- `Docs/Guides/GameLoop-Start-Contracts.md`
- `Docs/Guides/How-To-Add-A-New-Module-To-Composition.md`
- `Docs/CHANGELOG-docs.md`

## Historico fisico separado

- `Docs/Archive/Modules/LevelFlow.md`
- `Docs/Archive/Modules/PostRun.md`
- `Docs/Archive/Plans/Plan-Phase-Isolation-Seams-and-Manual-Smokes.md`
- `Docs/Archive/Plans/Plan-Baseline-4.0-Execution-Guardrails.md`
- `Docs/Archive/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md`
- `Docs/Archive/Plans/decision_gameplay_runtime_composition_pre_code.md`

## Regras de leitura

- O canon vivo prevalece sobre docs historicos.
- Referencias legadas servem apenas para rastreio, migracao ou archive.
- O baseline ativo nao deve usar linguagem historica como fonte operacional principal.
