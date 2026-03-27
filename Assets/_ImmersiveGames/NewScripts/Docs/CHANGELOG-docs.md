# Changelog Docs

## 2026-03-27
- Consolidado o ADR-0012 como fonte unica de verdade do `PostStage` implementado e validado.
- Atualizados os indices e sumarios canonicos para tratar `PostStage` como fluxo vigente de `Modules/PostGame`.
- Registrada a politica operacional: default sem presenter, presenter explicito roda `PostStage` real, skip automatico quando ausente.

## 2026-03-26
- Atualizada a documentacao para o modelo canonico atual de rotas.
- `SceneRouteDefinitionAsset` passou a ser a referencia efetiva da rota.
- `GameNavigationCatalogAsset` ficou explicito como catalogo fino de intent.
- `SceneFlow` passou a ser descrito como consumidor de rota ja resolvida.
- `SceneRouteCatalogAsset` foi removido da documentacao ativa e do runtime principal.
