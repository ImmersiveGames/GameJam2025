# ADR-0026 — Troca de Level Intra-Macro via Swap Local (sem Transição Macro)

## Status

- Estado: **Aceito (Em implementação)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-01
- Tipo: Implementação
- Escopo: NewScripts/Modules (LevelFlow, ContentSwap, WorldLifecycle, QA)

## Resumo

Permitir trocar de level dentro do mesmo macro (Gameplay) **sem** disparar transição macro do SceneFlow, usando um **swap local**:

- muda `levelId` e/ou `contentId`
- reaplica conteúdo (ContentSwap) e/ou executa LevelReset
- preserva cenas macro (GameplayScene + UIGlobal)

## Contexto

Hoje o sistema já suporta:

- Macro transitions (SceneFlow) para entrar/sair do macro.
- MacroReset e LevelReset (ADR-0023).
- ContentSwap in-place (modo local).

O que falta é fechar o trilho “Next/Prev/SelectLevel” sem depender de `SceneTransitionService`.

## Decisão (contrato)

- Dentro de um macro com levels:
  - `SelectLevel(levelId)` atualiza seleção ativa (levelSignature + v).
  - `ApplyLevelSelection()` executa:
    - LevelReset (local) e/ou
    - ContentSwap (in-place) e/ou
    - hooks de LevelStages (Intro/Post, quando aplicável).
- A troca local **não** chama `NavigateAsync` / `SceneTransitionService`.

## Implementação atual (2026-02-25)

### O que já existe (base)

- **ContentSwap in-place** observado em log:
  - `InPlaceContentSwapService [OBS][ContentSwap] ContentSwapRequested ... mode=InPlace ...`
- **LevelReset** observado em log (ADR-0023):
  - `ResetRequested kind='Level' ...`
  - `ResetCompleted kind='Level' ... success=True`

### O que ainda falta (para fechar ADR-0026)

- Uma API canônica de runtime:
  - `SwapToLevelAsync(nextLevelId, reason)` ou equivalente,
  - que atualize seleção + execute swap local end-to-end.
- Evidência de QA N→1 (A/B/Sequence) sem `TransitionStarted`.

## Implementação atual (2026-03-01)

Anchors curtas observadas no log atual:

- `routeId='to-menu'` e `routeId='to-gameplay'` nos trilhos macro de navegação.
- `MacroLoadingPhase='LevelPrepare'` antes da conclusão visual da transição.
- Resets por domínio:
  - macro: `ResetWorldStarted` / `ResetCompleted`;
  - level: `ResetRequested kind='Level'` + `LevelPrepared`.
- IntroStage: bloqueio/liberação de `sim.gameplay` (block/unblock) no fluxo de entrada em gameplay.
- Pause/Resume com token dedicado `state.pause`.
- Pós-partida: `PostGame`, `Restart->Boot` e `ExitToMenu` evidenciados.

## Critérios de aceite (DoD)

- [x] Existe ação runtime “Select/Next/Prev” que altera `levelId` dentro do mesmo macro.
- [x] Logs [OBS] demonstram:
  - mesmo macroSignature;
  - levelSignature mudando (v incrementa);
  - ContentSwap/LevelReset executado;
  - **sem** SceneFlow TransitionStarted.
- [x] Blocos base disponíveis: ContentSwap in-place + LevelReset.

## Changelog

- 2026-03-01: Atualização de status, seção de implementação atual e revisão de DoD/observabilidade com base no log mais recente.
- 2026-02-25: Atualizado para **Em implementação** com base no log; registrado que os blocos base (ContentSwap in-place + LevelReset) já existem, faltando o trilho end-to-end de swap local.
