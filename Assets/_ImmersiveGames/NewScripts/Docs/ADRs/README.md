# ADRs (Architecture Decision Records)

Este diretorio contem decisoes arquiteturais do **NewScripts**.

## Convencoes de status

Cada ADR possui 3 eixos:

- **Decisao:** Proposta | Aceita | Rejeitada | Substituida | Obsoleta
- **Implementacao:** Nao iniciada | Em andamento | Parcial | Implementada | Obsoleta
- **Manutencao:** Ativa | Fechada | Obsoleta

> Regra pratica: uma decisao pode estar **Aceita** mesmo com implementacao **Parcial**; isso evita confundir aprovacao do design com trabalho concluido.

## Indice

| ADR | Titulo | Decisao | Implementacao | Manutencao | Ultima atualizacao |
|---|---|---:|---:|---:|---:|
| [`ADR-0005-GlobalCompositionRoot-Modularizacao.md`](ADR-0005-GlobalCompositionRoot-Modularizacao.md) | ADR-0005 - Modularizacao do GlobalCompositionRoot (registro global por Feature Modules) | Aceita | Implementada | Fechada | 2026-02-18 |
| [`ADR-0007-InputModes.md`](ADR-0007-InputModes.md) | ADR-0007 - Formalizar InputModes e responsabilidade do modulo | Aceita | Implementada | Ativa | 2026-02-18 |
| [`ADR-0008-RuntimeModeConfig.md`](ADR-0008-RuntimeModeConfig.md) | ADR-0008 - RuntimeModeConfig (Strict/Release + Degraded) | Aceita | Implementada | Ativa | 2026-02-18 |
| [`ADR-0009-FadeSceneFlow.md`](ADR-0009-FadeSceneFlow.md) | ADR-0009 - Fade + SceneFlow (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0010-LoadingHud-SceneFlow.md`](ADR-0010-LoadingHud-SceneFlow.md) | ADR-0010 - Loading HUD + SceneFlow (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`](ADR-0011-WorldDefinition-MultiActor-GameplayScene.md) | ADR-0011 - WorldDefinition multi-actor para GameplayScene (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`](ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md) | ADR-0012 - Fluxo Pos-Gameplay (GameOver, Vitoria, Restart, ExitToMenu) | Aceita | Implementada | Fechada | 2026-02-04 |
| [`ADR-0013-Ciclo-de-Vida-Jogo.md`](ADR-0013-Ciclo-de-Vida-Jogo.md) | ADR-0013 - Ciclo de Vida do Jogo (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0014-GameplayReset-Targets-Grupos.md`](ADR-0014-GameplayReset-Targets-Grupos.md) | ADR-0014 - GameplayReset: Targets por Grupos | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0015-Baseline-2.0-Fechamento.md`](ADR-0015-Baseline-2.0-Fechamento.md) | ADR-0015 - Baseline 2.0: Fechamento | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0016-ContentSwap-WorldLifecycle.md`](ADR-0016-ContentSwap-WorldLifecycle.md) | ADR-0016 - ContentSwap InPlace-only (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0017-LevelManager-Config-Catalog.md`](ADR-0017-LevelManager-Config-Catalog.md) | ADR-0017 - LevelManager: Config + Catalog (Single Source of Truth) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0018-Fade-TransitionStyle-SoftFail.md`](ADR-0018-Fade-TransitionStyle-SoftFail.md) | ADR-0018 - Fade/TransitionStyle e Soft-Fail (nao interrompe o jogo) | Aceita | Implementada | Ativa | 2026-02-18 |
| [`ADR-0019-Navigation-IntentCatalog.md`](ADR-0019-Navigation-IntentCatalog.md) | ADR-0019 - Navigation Intent Catalog (IntentCatalog + GameNavigationCatalog) | Aceita | Implementada | Ativa | 2026-02-18 |
| [`ADR-0020-LevelContent-Progression-vs-SceneRoute.md`](ADR-0020-LevelContent-Progression-vs-SceneRoute.md) | ADR-0020 - Separar LevelContent/Progression de SceneRoute/Scene Data | Aberto | Nao iniciada | Ativa | 2026-02-18 |
| [`ADR-0021-Baseline-3.0-Completeness.md`](ADR-0021-Baseline-3.0-Completeness.md) | ADR-0021 - Baseline 3.0 (Completeness) | Aceita | Implementada | Fechada | 2026-03-11 |
| [`ADR-0022-Assinaturas-e-Dedupe-por-Dominio-MacroRoute-vs-Level.md`](ADR-0022-Assinaturas-e-Dedupe-por-Dominio-MacroRoute-vs-Level.md) | ADR-0022 - Assinaturas e Dedupe por Dominio (MacroRoute vs Level) | Aceita | Implementada | Ativa | 2026-03-11 |
| [`ADR-0023-Dois-Niveis-de-Reset-MacroReset-vs-LevelReset.md`](ADR-0023-Dois-Niveis-de-Reset-MacroReset-vs-LevelReset.md) | ADR-0023 - Dois niveis de reset: MacroReset vs LevelReset | Aceita | Implementada | Ativa | 2026-03-11 |
| [`ADR-0024-LevelCatalog-por-MacroRoute-e-Contrato-de-Selecao-de-Level-Ativo.md`](ADR-0024-LevelCatalog-por-MacroRoute-e-Contrato-de-Selecao-de-Level-Ativo.md) | ADR-0024 - LevelCollection por MacroRoute e Contrato de Selecao de Level Ativo | Aceita | Implementada | Ativa | 2026-03-11 |
| [`ADR-0025-Pipeline-de-Loading-Macro-inclui-Etapa-de-Level-antes-do-FadeOut.md`](ADR-0025-Pipeline-de-Loading-Macro-inclui-Etapa-de-Level-antes-do-FadeOut.md) | ADR-0025 - Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut | Aceita | Implementada | Ativa | 2026-03-11 |
| [`ADR-0026-Troca-de-Level-Intra-Macro-via-Swap-Local-sem-Transicao-Macro.md`](ADR-0026-Troca-de-Level-Intra-Macro-via-Swap-Local-sem-Transicao-Macro.md) | ADR-0026 - Troca de Level Intra-Macro via Swap Local (sem Transicao Macro) | Aceita | Implementada | Ativa | 2026-03-11 |
| [`ADR-0027-IntroStage-e-PostLevel-como-Responsabilidade-do-Level.md`](ADR-0027-IntroStage-e-PostLevel-como-Responsabilidade-do-Level.md) | ADR-0027 - IntroStage e PostLevel como Responsabilidade do Level | Aceita | Implementada | Ativa | 2026-03-11 |

## Nota de fechamento atual (ADR-0021..0027)

- O eixo principal de `NewScripts` esta efetivamente **canon-only** em `LevelFlow`, `LevelDefinition`, `Navigation`, `WorldLifecycle V2` e tooling/editor/QA associado.
- A excecao remanescente relevante fica fora/borda desse fechamento principal: `Gameplay RunRearm` com fallback legado de actor-kind/string.
- Permanece tambem um residuo menor editor/serializado em `GameNavigationIntentCatalogAsset`, sem reabrir trilho paralelo de runtime.

## Templates

- **Implementacao:** [`ADR-TEMPLATE.md`](ADR-TEMPLATE.md)
- **Completude / Governanca:** [`ADR-TEMPLATE-COMPLETENESS.md`](ADR-TEMPLATE-COMPLETENESS.md)

> Regra: ADRs de implementacao seguem o template de implementacao; ADRs de fechamento/baseline seguem o template de completude (ex.: ADR-0015).
