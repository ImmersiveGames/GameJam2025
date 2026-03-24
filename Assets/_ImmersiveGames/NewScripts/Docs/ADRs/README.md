# ADRs

Este diretorio mantem apenas ADRs vigentes para entender o desenho atual.

## Cadeia operacional atual

| ADR | Foco atual |
|---|---|
| `ADR-0007-InputModes.md` | ownership de InputModes |
| `ADR-0008-RuntimeModeConfig.md` | politica de runtime/degraded |
| `ADR-0009-FadeSceneFlow.md` | fade + SceneFlow |
| `ADR-0010-LoadingHud-SceneFlow.md` | loading HUD no pipeline |
| `ADR-0011-WorldDefinition-MultiActor-GameplayScene.md` | world definition de gameplay |
| `ADR-0013-Ciclo-de-Vida-Jogo.md` | ciclo de vida da run |
| `ADR-0014-GameplayReset-Targets-Grupos.md` | ActorGroupRearm |
| `ADR-0017-LevelManager-Config-Catalog.md` | configuracao de level e catalogos de level |
| `ADR-0018-Fade-TransitionStyle-SoftFail.md` | resiliencia do fade/style |
| `ADR-0019-Navigation-IntentCatalog.md` | navigation asset unico e direct-ref |
| `ADR-0022-Assinaturas-e-Dedupe-por-Dominio-MacroRoute-vs-Level.md` | identidade de level por `LevelSignature` |
| `ADR-0023-Dois-Niveis-de-Reset-MacroReset-vs-LevelReset.md` | macro reset vs level reset |
| `ADR-0024-LevelCatalog-por-MacroRoute-e-Contrato-de-Selecao-de-Level-Ativo.md` | selecao do level ativo |
| `ADR-0025-Pipeline-de-Loading-Macro-inclui-Etapa-de-Level-antes-do-FadeOut.md` | etapa de level no loading macro |
| `ADR-0026-Troca-de-Level-Intra-Macro-via-Swap-Local-sem-Transicao-Macro.md` | swap local intra-macro |
| `ADR-0027-IntroStage-e-PostLevel-como-Responsabilidade-do-Level.md` | intro level-owned e post global |

## ADR vigente, mas fora da superficie operacional principal

- `ADR-0020-LevelContent-Progression-vs-SceneRoute.md`
  - permanece como referencia de direcao futura;
  - nao sobrescreve a documentacao operacional atual.

## Regras de uso

- Leia ADRs como explicacao do desenho atual, nao como guia de integracao do dia a dia.
- Quando houver conflito entre historico e docs oficiais, prevalece a cadeia oficial em `Docs/README.md` sustentada pelo runtime atual.


## ADRs substituídos

- `ADR-0016-ContentSwap-WorldLifecycle.md`
  - mantido apenas como registro histórico;
  - não representa mais o trilho canônico.

## Leitura de modulos para o estado atual

A superficie atual de reset esta dividida em:
- `Docs/Modules/WorldReset.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/ResetInterop.md`

Use esses documentos quando um ADR antigo mencionar `WorldLifecycle` como area unica.
