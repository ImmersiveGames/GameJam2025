# ADR-0016 — ContentSwap InPlace-only (SUPERSEDED)

## Status
- Estado: **Superseded**
- Data original: 2026-02-18
- Última atualização: 2026-03-22

## Motivo da substituição
O módulo `ContentSwap` foi removido do trilho canônico.

A arquitetura vigente agora separa:
- `LevelFlow` / `Navigation` como donos da **semântica**;
- `RestartContext` como dono do **snapshot semântico**;
- `SceneComposition` como executor técnico de **composição de cenas**.

## Efeito prático
Este ADR não deve mais ser usado como fonte de integração nem como contrato operacional.

As referências anteriores a:
- `IContentSwapChangeService`
- `IContentSwapContextService`
- eventos `ContentSwap*`
- `contentId` in-place como trilho canônico

passam a ser históricas.

## Referência vigente
Usar, em vez deste ADR:
- `ADR-0020-LevelContent-Progression-vs-SceneRoute.md` como direção de separação semântica;
- `Docs/Reports/Audits/2026-03-22/Fase-2b-Contrato-SceneComposition.md`;
- `Docs/Reports/Audits/2026-03-22/Fase-2c-Plano-Migracao-Local-SceneComposition.md`.
