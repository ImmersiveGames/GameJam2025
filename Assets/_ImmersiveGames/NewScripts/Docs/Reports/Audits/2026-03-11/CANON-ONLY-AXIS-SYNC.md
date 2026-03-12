# CANON-ONLY AXIS SYNC (2026-03-11)

## Escopo auditado

- `LevelFlow`
- `LevelDefinition`
- `Navigation`
- `WorldLifecycle V2`
- tooling/editor/QA associado

## O que passou a ser considerado canon-only

- `LevelFlow` por `LevelRef`, `MacroRouteId` e `LevelSignature`
- `LevelDefinition` por `levelRef + macroRouteRef`
- `Navigation` sem superficie publica string-first
- `WorldLifecycle V2` apenas como telemetria/observabilidade canonica
- tooling/editor/QA do eixo principal sem trilho artificial de compat

## O que foi atualizado nos docs

- `Docs/README.md`
- `Docs/Canon/Canon-Index.md`
- `Docs/Plans/Plan-Continuous.md`
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Audits/LATEST.md`
- `Docs/ADRs/README.md`
- `Docs/ADRs/ADR-0021-Baseline-3.0-Completeness.md`
- `Docs/ADRs/ADR-0022-Assinaturas-e-Dedupe-por-Dominio-MacroRoute-vs-Level.md`
- `Docs/ADRs/ADR-0023-Dois-Niveis-de-Reset-MacroReset-vs-LevelReset.md`
- `Docs/ADRs/ADR-0024-LevelCatalog-por-MacroRoute-e-Contrato-de-Selecao-de-Level-Ativo.md`
- `Docs/ADRs/ADR-0025-Pipeline-de-Loading-Macro-inclui-Etapa-de-Level-antes-do-FadeOut.md`
- `Docs/ADRs/ADR-0026-Troca-de-Level-Intra-Macro-via-Swap-Local-sem-Transicao-Macro.md`
- `Docs/ADRs/ADR-0027-IntroStage-e-PostLevel-como-Responsabilidade-do-Level.md`
- `Docs/CHANGELOG.md`

## Excecoes que permanecem

- `Gameplay ActorGroupRearm` (renomeado de `RunRearm` na rodada posterior de 2026-03-11) ainda aparecia com fallback legado de actor-kind/string neste snapshot
- pequeno residuo editor/serializado em `GameNavigationIntentCatalogAsset`

## Decisao explicita

- **canon-only no eixo principal**
- **nao canon-only absoluto em todo `NewScripts/**` ainda**

