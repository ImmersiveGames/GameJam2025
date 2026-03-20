# ADR-0009 â€” Fade + SceneFlow (NewScripts)

## Status

- Estado: Implementado
- Data (decisÃ£o): 2025-12-24
- Ãšltima atualizaÃ§Ã£o: 2026-02-04
- Tipo: ImplementaÃ§Ã£o
- Escopo: SceneFlow + Fade (NewScripts). *(Loading HUD Ã© ADR-0010.)*

## Contexto

O pipeline NewScripts precisava:

- Aplicar **FadeIn/FadeOut** durante transiÃ§Ãµes do SceneFlow.
- Evitar dependÃªncia do fade legado.
- Permitir configurar timings por **profile** (startup/frontend/gameplay).
- Garantir **comportamento determinÃ­stico** (ordem fixa) e **observÃ¡vel** (logs canÃ´nicos).
- Aplicar polÃ­tica **Strict vs Release**:
  - Strict (Dev/QA): **falhar cedo** quando prÃ©-condiÃ§Ãµes crÃ­ticas nÃ£o existem.
  - Release: permitir fallback **somente via modo degradado explÃ­cito**.

## DecisÃ£o

### Objetivo de produÃ§Ã£o (sistema ideal)

Garantir que TODA transiÃ§Ã£o de cena do SceneFlow tenha um envelope visual determinÃ­stico:

**FadeIn (escurece) â†’ operaÃ§Ãµes de cena â†’ ScenesReady â†’ completion gate â†’ FadeOut (revela) â†’ Completed**

> Nota: no naming atual do runtime, `FadeInAsync()` = â€œescurecerâ€ (0â†’1) e `FadeOutAsync()` = â€œrevelarâ€ (1â†’0).

### Contrato mÃ­nimo (produÃ§Ã£o)

1) **Ordem (invariantes)**
- **FadeIn** inicia **antes** de qualquer mutaÃ§Ã£o visual (load/unload/setActive).
- `ScenesReady` Ã© emitido **apÃ³s** operaÃ§Ãµes de cena e **antes** de `Completed` (mesma `signature`).
- **Completion gate** Ã© aguardado **antes** do `BeforeFadeOut` e do `FadeOut`.
- `Completed` sÃ³ ocorre **apÃ³s** `FadeOut` (quando `UseFade=true`), preservando â€œreveal before completionâ€.

2) **Strict vs Release (fail-fast + degraded)**
- **Strict (UNITY_EDITOR / DEVELOPMENT_BUILD)**
  - Falha explicitamente quando:
    - profile nÃ£o Ã© encontrado em Resources,
    - `IFadeService` nÃ£o existe no DI global,
    - `FadeScene` nÃ£o carrega,
    - `FadeController` nÃ£o existe na `FadeScene`.
- **Release**
  - Pode seguir sem fade **apenas** com `DEGRADED_MODE` explÃ­cito:
    - `DEGRADED_MODE feature='fade' reason='<...>' detail='<...>'`
  - ApÃ³s degradar, o fade vira **no-op** (dur=0) mantendo a ordem do pipeline.

3) **NÃ£o criar UI â€œem vooâ€**
- O fade depende de `FadeScene` + `FadeController`.
- O runtime nÃ£o deve instanciar canvas/cÃ¢mera de forma silenciosa.

### NÃ£o-objetivos (resumo)

- Loading HUD (ver ADR-0010).
- UX/arte fina do fade (layout, brand, animaÃ§Ãµes especÃ­ficas).

## Fora de escopo

- AlteraÃ§Ãµes no pipeline de Loading HUD.
- RefatoraÃ§Ãµes fora do envelope de Fade/SceneFlow.

## ConsequÃªncias

### BenefÃ­cios

- Envelope visual determinÃ­stico em todas as transiÃ§Ãµes do SceneFlow.
- Timings declarativos por profile, sem strings/timings espalhados.
- Comportamento auditÃ¡vel via Ã¢ncoras canÃ´nicas `[OBS][Fade]`.
- PolÃ­tica Strict/Release explÃ­cita, reduzindo â€œfallback silenciosoâ€.

### Trade-offs / riscos

- Strict pode â€œquebrar cedoâ€ durante integraÃ§Ã£o (o objetivo Ã© expor setup incompleto imediatamente).
- Release pode degradar visualmente (sem fade) â€” mas isso fica **explÃ­cito** via `DEGRADED_MODE`.

## Mapeamento para implementaÃ§Ã£o

Arquivos (NewScripts):

- OrquestraÃ§Ã£o / ordem / anchors `[OBS][Fade]`:
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- Policy Strict/Release + degraded reporter:
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/IRuntimeModeProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/UnityRuntimeModeProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/IDegradedModeReporter.cs`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/DegradedModeReporter.cs`
- Adapter (profile â†’ config â†’ policy):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Adapters/SceneFlowFadeAdapter.cs` (`SceneFlowFadeAdapter`)
- ServiÃ§o de fade (garante FadeScene + Controller; falha explÃ­cita se invÃ¡lido):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Fade/Runtime/FadeService.cs`

## Observabilidade (contrato)

**Contrato canÃ´nico:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

### Ã‚ncoras mÃ­nimas de Fade (evidÃªncia)

Emitidas por `SceneTransitionService` quando `UseFade=true`:

- `[OBS][Fade] FadeInStarted ...`
- `[OBS][Fade] FadeInCompleted ...`
- `[OBS][Fade] FadeOutStarted ...`
- `[OBS][Fade] FadeOutCompleted ...`

### Ã‚ncora canÃ´nica de fallback (Release)

Quando o fade nÃ£o pode operar em Release:

- `DEGRADED_MODE feature='fade' reason='<Reason>' detail='<...>'`

## CritÃ©rios de pronto (DoD)

### DoD (implementaÃ§Ã£o)

- [x] Ordem e gating conforme â€œContrato mÃ­nimoâ€.
- [x] Policy Strict vs Release aplicada no caminho do fade.
- [x] `DEGRADED_MODE` emitido em Release quando necessÃ¡rio.
- [x] Logs `[OBS][Fade]` emitidos no envelope (Start/Complete por fase).

### DoD (evidÃªncia)

- [x] Snapshot com transiÃ§Ãµes (startup + gameplay) contendo as 4 Ã¢ncoras `[OBS][Fade]` na mesma `signature`: `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`.
- [ ] (Opcional) Um snapshot de falha em Strict comprovando erro explÃ­cito para `FadeScene`/controller ausente.

## Procedimento de verificaÃ§Ã£o (QA)

1) **Happy path**
- Use: `QA/SceneFlow/EnterGameplay (TC: Menu->Gameplay ResetWorld)` (ContextMenu).
- Verifique no log da transiÃ§Ã£o:
  - `SceneTransitionStartedEvent` â†’ `[OBS][Fade] FadeInStarted/Completed` â†’ `ScenesReady` â†’ gate â†’ `[OBS][Fade] FadeOutStarted/Completed` â†’ `SceneTransitionCompletedEvent`.

2) **Fail-fast (Strict)**
- Em Editor/Development:
  - Remova temporariamente a `FadeScene` do Build Settings **ou** remova `FadeController` dela.
- Dispare a mesma transiÃ§Ã£o e confirme:
  - exceÃ§Ã£o clara (`InvalidOperationException`) com `reason`/`detail` (sem seguir â€œsilenciosamenteâ€).

3) **Degraded mode (Release)**
- Em build Release:
  - reproduza a ausÃªncia (scene/controller/service) e confirme:
  - `DEGRADED_MODE feature='fade' ...` e transiÃ§Ã£o segue sem fade (no-op), sem crash.

## EvidÃªncia

- **Ãšltima evidÃªncia (log bruto):** `Docs/Reports/Evidence/LATEST.md`
- **Fonte canÃ´nica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Snapshot datado (PASS, startup + gameplay):** `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
  - ContÃ©m Ã¢ncoras `[OBS][Fade]` (`FadeInStarted/Completed`, `FadeOutStarted/Completed`) para `profile=startup` e `profile=gameplay` com `signature` completa.
  - ContÃ©m evidÃªncia de ordenaÃ§Ã£o: `FadeInCompleted` ocorre antes do load; `ScenesReady` + completion gate antes de `FadeOut`; `FadeOutCompleted` antes de `TransitionCompleted`.

## ImplementaÃ§Ã£o (arquivos impactados)

### Runtime / Editor (cÃ³digo e assets)

- **Infrastructure**
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/DegradedModeReporter.cs`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/IDegradedModeReporter.cs`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/IRuntimeModeProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/UnityRuntimeModeProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Fade/Runtime/FadeService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Adapters/SceneFlowFadeAdapter.cs`

### Docs / evidÃªncias relacionadas

- `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
- `Reports/Evidence/LATEST.md`
- `Standards/Standards.md`

## ReferÃªncias

- [ADR-0010 â€” Loading HUD + SceneFlow (NewScripts)](ADR-0010-LoadingHud-SceneFlow.md)
- [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)
- [`Production-Policy-Strict-Release.md`](../Standards/Standards.md#politica-strict-vs-release)

