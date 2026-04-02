# NewScripts Docs

Esta pasta separa o canon atual, as entradas ativas e o historico arquivado do Baseline 4.0.

## Status documental

- `ADR-0001` e o owner de glossario, intencao e taxonomia.
- As paginas ativas seguem o tree fisico atual, mas algumas mantem nomes historicos por compatibilidade.
- O eixo fisico atual separa `Core`, `Infrastructure`, `Orchestration`, `Game` e `Experience`.
- Termos como `WorldLifecycle`, `ContentSwap`, `LevelManager`, `PostGame`, `PostPlay` e `GameOver` devem ser lidos como historicos fora do canon.
- A superficie ativa de conclusao de run e `IntroStage`, `Run`, `RunOutcome`, `PostRun` e `RunDecision`.
- `Experience/Save` deve ser lido hoje como superficie de hooks e contratos placeholder, nao como sistema final de persistencia.

## Canon atual

- `Docs/ADRs/ADR-0001-Glossario-Fundamental-Contextos-e-Rotas-v2.md`
- `Docs/ADRs/ADR-0043-Ancora-de-Decisao-para-o-Baseline-4.0.md`
- `Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md`
- `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md`
- `Docs/Plans/Plan-Baseline-4.0-Execution-Guardrails.md`
- `Docs/Plans/Plan-Round-2-Object-Lifecycle.md`

## Entradas ativas de auditoria

- `Docs/Reports/Audits/LATEST.md`
- `Docs/Reports/Audits/2026-04-01/Round-2-Cut-3-Runtime-Ownership-Reset-Participation.md`
- `Docs/Reports/Audits/2026-04-01/Round-2-Cut-4-Pooling-Future-Ready-Seam.md`
- `Docs/Reports/Audits/2026-04-01/Round-2-Freeze-Object-Lifecycle.md`
- `Docs/Reports/Audits/2026-04-01/Round-2-Cut-2-Actor-Consumption-Contract.md`
- `Docs/Reports/Audits/2026-04-01/Round-2-Cut-1-Ownership-Taxonomy.md`
- `Docs/Reports/Audits/2026-04-01/Backbone-Round-1-Freeze.md`
- `Docs/Reports/Audits/2026-03-30/Structural-Freeze-Snapshot.md`
- `Docs/Reports/Audits/2026-03-30/Structural-Xray-NewScripts.md`
- `Docs/Reports/Audits/2026-03-30/Docs-Consolidation-Baseline-4.0.md`

## Entradas ativas

- `Docs/Modules/README.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Gameplay.md`
- `Docs/Modules/InputModes.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/Navigation.md`
- `Docs/Modules/PostRun.md`
- `Docs/Modules/Save.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/WorldReset.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
- `Docs/Guides/Event-Hooks-Reference.md`
- `Docs/Guides/GameLoop-Start-Contracts.md`
- `Docs/Guides/How-To-Add-A-New-Module-To-Composition.md`
- `Docs/CHANGELOG-docs.md`

## Estado fisico atual resumido

- `Core`: primitivas fundamentais e base conceitual.
- `Infrastructure`: composicao, pooling, runtime mode, input modes, simulation gate, observability/baseline e suporte transversal.
- `Orchestration`: `SceneFlow`, `WorldReset`, `ResetInterop`, `Navigation`, `LevelLifecycle`, `GameLoop`, `SceneReset` e `SceneComposition`.
- `Game`: `Gameplay`, `Content/Definitions/Levels` e o runtime de estado/GameplayReset.
- `Experience`: `PostRun` como fluxo local intermediario, `RunDecision` como overlay final, `Audio`, `Save` como superficie de hooks placeholder, `Preferences`, `Frontend` e `Camera`.

## Regras de leitura

- O canon atual prevalece sobre docs antigos ou intermediarios.
- `Docs/Archive/` e historico e nao compete com o canon.
- As linhas de compatibilidade ainda vivas sao parte do estado atual, nao do alvo final.
- A proxima direcao do projeto sai da consolidacao atual, nao de um redesenho idealizado.
