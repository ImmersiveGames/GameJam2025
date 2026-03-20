# ADR-0010 â€” Loading HUD + SceneFlow (NewScripts)

## Status

- Estado: Implementado
- Data (decisÃ£o): 2025-12-24
- Ãšltima atualizaÃ§Ã£o: 2026-02-04
- Tipo: ImplementaÃ§Ã£o
- Escopo: SceneFlow + Loading HUD (NewScripts)

## Contexto

O SceneFlow executa operaÃ§Ãµes que podem causar â€œpopâ€ visual (load/unload/setActive), especialmente em transiÃ§Ãµes longas (Menuâ†’Gameplay, Restart). O projeto precisava de um **Loading HUD** com as seguintes propriedades:

- Integrar no envelope do SceneFlow (mesma `signature` / mesma ordem).
- Ser **determinÃ­stico** (show/hide em pontos fixos) e **auditÃ¡vel** (logs canÃ´nicos).
- Seguir polÃ­tica **Strict vs Release**:
  - Strict (Dev/QA): falhar cedo se o HUD estiver mal configurado.
  - Release: permitir seguir sem HUD somente com degraded explÃ­cito.

AlÃ©m disso, o HUD nÃ£o deve depender de instanciar UI â€œem vooâ€ de forma silenciosa.

## DecisÃ£o

### Objetivo de produÃ§Ã£o

Garantir que transiÃ§Ãµes do SceneFlow possam opcionalmente exibir um â€œLoading HUDâ€ **sobre** o envelope de fade:

**FadeIn (escurece) â†’ LoadingHUD.Show â†’ operaÃ§Ãµes de cena â†’ ScenesReady â†’ completion gate â†’ LoadingHUD.Hide â†’ FadeOut (revela) â†’ Completed**

> O fade continua sendo a primeira/Ãºltima camada visual (ADR-0009). O Loading HUD Ã© uma camada intermediÃ¡ria, usada quando a transiÃ§Ã£o requer feedback.

### Contrato mÃ­nimo (produÃ§Ã£o)

1) **Pontos de show/hide (invariantes)**
- `Show` ocorre apÃ³s `FadeInCompleted` e antes das mutaÃ§Ãµes de cena.
- `Hide` ocorre apÃ³s `ScenesReady` + completion gate e antes do `BeforeFadeOut`/`FadeOut`.
- `Hide` Ã© **forÃ§ado** no caminho de erro/early-exit para evitar HUD â€œpresoâ€.

2) **Strict vs Release (fail-fast + degraded)**
- **Strict (UNITY_EDITOR / DEVELOPMENT_BUILD)**
  - Falha explicitamente quando:
    - `ILoadingHudService` nÃ£o existe no DI global,
    - a cena/objeto do Loading HUD nÃ£o estÃ¡ configurado,
    - o controller do HUD nÃ£o existe/Ã© invÃ¡lido.
- **Release**
  - Pode seguir sem HUD **apenas** com `DEGRADED_MODE` explÃ­cito:
    - `DEGRADED_MODE feature='loading_hud' reason='<...>' detail='<...>'`
  - ApÃ³s degradar, o HUD vira **no-op** preservando a ordem (show/hide sÃ£o ignorados).

3) **NÃ£o criar UI â€œem vooâ€**
- O Loading HUD Ã© fornecido por cena/asset configurado (ou equivalente), nÃ£o por instÃ¢ncia silenciosa em runtime.

### NÃ£o-objetivos (resumo)

- UX/arte do HUD (layout, progressos, textos).
- Driver paralelo fora do pipeline canÃ´nico.

## Fora de escopo

- Recriar HUD por instÃ¢ncia em runtime.
- Alterar o envelope de Fade (ver ADR-0009).

## ConsequÃªncias

### BenefÃ­cios

- Reduz â€œpopâ€ visual e dÃ¡ feedback em transiÃ§Ãµes longas.
- Mesma disciplina de contrato do SceneFlow: ordem fixa, evidÃªncia e policy.
- Falhas de setup ficam Ã³bvias em Strict; builds Release degradam explicitamente.

### Trade-offs / riscos

- IntegraÃ§Ã£o adiciona pontos extras de configuraÃ§Ã£o (cena/controller/DI).
- Release pode degradar (sem HUD) â€” mas fica explÃ­cito via `DEGRADED_MODE`.

## Mapeamento para implementaÃ§Ã£o

Arquivos (NewScripts):

- OrquestraÃ§Ã£o / ordem / anchors `[LoadingHud*]`:
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs`
- ServiÃ§o de loading HUD (setup + Strict/Release + no-op em degraded):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
- Controller do HUD (visibilidade/efeito):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Bindings/LoadingHudController.cs`

## Observabilidade (contrato)

**Contrato canÃ´nico:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

### Ã‚ncoras mÃ­nimas de Loading HUD (evidÃªncia)

Emitidas por `LoadingHudService` e `LoadingHudOrchestrator` quando `UseLoadingHud=true`:

- `[LoadingHudEnsure] ...`
- `[LoadingHudShow] ...`
- `[LoadingHudHide] ...`
- `[LoadingDegraded] ...` (fallback)

### Ã‚ncora canÃ´nica de fallback (Release)

Quando o HUD nÃ£o pode operar em Release:

- `DEGRADED_MODE feature='loading_hud' reason='<Reason>' detail='<...>'`

## CritÃ©rios de pronto (DoD)

### DoD (implementaÃ§Ã£o)

- [x] Pontos de show/hide obedecem a ordem do â€œContrato mÃ­nimoâ€.
- [x] Policy Strict vs Release aplicada.
- [x] `DEGRADED_MODE` emitido em Release quando necessÃ¡rio.
- [x] Logs canÃ´nicos emitidos (`LoadingHudEnsure/Show/Hide` + `LoadingDegraded`).

### DoD (evidÃªncia)

- [ ] Snapshot contendo uma transiÃ§Ã£o com `UseLoadingHud=true` e as Ã¢ncoras `LoadingHudEnsure/Show/Hide` na mesma `signature`.

## Procedimento de verificaÃ§Ã£o (QA)

1) **Happy path**
- Dispare uma transiÃ§Ã£o com `UseLoadingHud=true` (ex.: Menuâ†’Gameplay em perfis que demandam HUD).
- Confirme no log:
  - `FadeInCompleted` â†’ `[LoadingHudShow]` â†’ operaÃ§Ãµes de cena â†’ `ScenesReady` â†’ gate â†’ `[LoadingHudHide]` â†’ `FadeOut` â†’ `Completed`.

2) **Fail-fast (Strict)**
- Em Editor/Development:
  - remova temporariamente o service/controller do HUD.
- Confirme falha explÃ­cita (exception) com `reason/detail`.

3) **Degraded mode (Release)**
- Em build Release:
  - reproduza a ausÃªncia e confirme:
  - `DEGRADED_MODE feature='loading_hud' ...` e transiÃ§Ã£o segue sem HUD.

## EvidÃªncia

- **Ãšltima evidÃªncia (log bruto):** `Docs/Reports/Evidence/LATEST.md`
- **Fonte canÃ´nica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)

## ImplementaÃ§Ã£o (arquivos impactados)

### Runtime / Editor (cÃ³digo e assets)

- **Infrastructure**
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Runtime/SceneFlowAdapterFactory.cs`

### Docs / evidÃªncias relacionadas

- `Reports/Evidence/LATEST.md`
- `Standards/Standards.md`

## ReferÃªncias

- [ADR-0009 â€” Fade + SceneFlow (NewScripts)](ADR-0009-FadeSceneFlow.md)
- [`Standards.md`](../Standards/Standards.md)

