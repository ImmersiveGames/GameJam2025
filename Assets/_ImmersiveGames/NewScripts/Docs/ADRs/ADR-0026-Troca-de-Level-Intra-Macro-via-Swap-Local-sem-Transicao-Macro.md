# ADR-0026 - Troca de Level Intra-Macro via Swap Local (sem Transição Macro)

## Status atual (2026-03-06)
- Status: **DONE**
- Implementado no código:
  - `LevelSwapLocalService` aplica unload/load local sem transição macro, montando request técnico para `SceneComposition`.
  - Restart local (`Level2 -> Level2`) faz reload local (unload+load do mesmo set).
  - QA confirma `transitionStartedCount='0'` no swap local.
- Evidência:
  - `Docs/Reports/Audits/LATEST.md`
  - `Docs/Reports/Evidence/LATEST.md`
- LEGACY / Histórico:
  - Troca de level via trilho macro no caminho que hoje é local.

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-12

## Decisão canônica atual

- Swap local usa `levelRef` (`LevelDefinitionAsset`) no domínio da macro atual.
- Fonte de levels no swap: `SceneRouteDefinitionAsset.LevelCollection`.
- Sem fallback para `LevelCatalog` no trilho canônico.
- A API pública principal não promove mais overloads por `LevelId`.

## Evidência (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... localContentId='level-content:Level1'` *(shape atual equivalente)*
- `lastlog:2181` `LevelAdditiveClearSummary ...`
- `lastlog:2185` `LevelCleared ...`
- `lastlog:1211` `IntroStageStartRequested ... levelSignature='level:Level1|route:to-gameplay|reason:Menu/PlayButton'`

## Observação histórica

- Qualquer referência a `LevelId` neste contexto deve ser lida como histórico fora do trilho canônico atual.
