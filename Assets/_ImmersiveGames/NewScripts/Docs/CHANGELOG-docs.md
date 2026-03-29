# Changelog Docs

## 2026-03-29
- Fechamento documental do Slice 3 do Baseline 4.0 com o rail validado `PostRunMenu -> Restart / ExitToMenu -> Navigation primary dispatch`.
- Marcadas as fases concluídas no plano do Slice 3 e mantidas as pendências sem bloqueio como follow-up arquitetural.
- Consolidado o owner documental: `PostGame` para `PostRunMenu`, `Navigation` para dispatch primario, `GameLoop` sem ownership visual do menu/dispatch.

## 2026-03-29
- Fechamento documental do Slice 2 do Baseline 4.0 com o rail validado `Playing -> ExitStage -> RunResult -> PostRunMenu`.
- Marcadas as fases concluídas no plano do Slice 2 e mantidos os follow-ups sem bloqueio como ruído de naming.
- Consolidado o owner documental: `GameLoop` para `Playing` e fronteira de fim de run; `PostGame` para `ExitStage`, `RunResult` e `PostRunMenu`.

## 2026-03-28
- Registrado o fechamento documental de `Preferences baseline v1: Audio + Video concluídos`.
- Alinhados o report datado, o `LATEST` de auditoria e o overview ativo do slice.
- Mantido como próximo passo o smoke test em build desktop para validar visualmente resolução/fullscreen.

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
