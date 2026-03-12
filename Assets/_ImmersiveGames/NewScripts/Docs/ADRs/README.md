# ADRs

Este diretorio mantem apenas ADRs ainda vigentes para entender o desenho atual do sistema. A leitura operacional deve priorizar as ADRs implementadas e ativas.

## Cadeia operacional atual

As ADRs abaixo sustentam o estado atual em runtime:

| ADR | Foco atual |
|---|---|
| `ADR-0007-InputModes.md` | ownership de InputModes |
| `ADR-0008-RuntimeModeConfig.md` | politica de runtime/degraded |
| `ADR-0009-FadeSceneFlow.md` | fade + SceneFlow |
| `ADR-0010-LoadingHud-SceneFlow.md` | Loading HUD no pipeline |
| `ADR-0011-WorldDefinition-MultiActor-GameplayScene.md` | world definition gameplay |
| `ADR-0013-Ciclo-de-Vida-Jogo.md` | ciclo de vida do jogo |
| `ADR-0014-GameplayReset-Targets-Grupos.md` | ActorGroupRearm e grupos canonicos |
| `ADR-0016-ContentSwap-WorldLifecycle.md` | content swap in-place |
| `ADR-0017-LevelManager-Config-Catalog.md` | level config/catalog |
| `ADR-0018-Fade-TransitionStyle-SoftFail.md` | resiliencia do fade/style |
| `ADR-0019-Navigation-IntentCatalog.md` | consolidacao da ownership atual de navigation |
| `ADR-0022-Assinaturas-e-Dedupe-por-Dominio-MacroRoute-vs-Level.md` | dedupe por dominio |
| `ADR-0023-Dois-Niveis-de-Reset-MacroReset-vs-LevelReset.md` | macro reset vs level reset |
| `ADR-0024-LevelCatalog-por-MacroRoute-e-Contrato-de-Selecao-de-Level-Ativo.md` | selecao de level ativo |
| `ADR-0025-Pipeline-de-Loading-Macro-inclui-Etapa-de-Level-antes-do-FadeOut.md` | etapa de level no macro loading |
| `ADR-0026-Troca-de-Level-Intra-Macro-via-Swap-Local-sem-Transicao-Macro.md` | swap local intra-macro |
| `ADR-0027-IntroStage-e-PostLevel-como-Responsabilidade-do-Level.md` | ownership de IntroStage/PostLevel |

## ADR vigente, mas nao operacional hoje

- `ADR-0020-LevelContent-Progression-vs-SceneRoute.md`
  - permanece como decisao aberta para futuro;
  - nao define o contrato operacional atual e nao deve sobrescrever os docs canonicos.

## Nota operacional atual

A documentacao operacional vigente assume o estado atual validado em runtime:
- `startup` no bootstrap
- `frontend/gameplay` em `RouteKind`
- Navigation/Transition em direct-ref + fail-fast

Qualquer ADR antiga removida desta superficie deixou de ser leitura operacional.
