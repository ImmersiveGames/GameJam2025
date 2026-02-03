# ADR-0009 — Fade + SceneFlow (NewScripts)

## Status

- Estado: Implementado
- Data (decisão): 2025-12-24
- Última atualização: 2026-01-31
- Escopo: SceneFlow + Fade (NewScripts). *(Loading HUD é ADR-0010.)*

## Contexto

O pipeline NewScripts precisava:

- Aplicar **FadeIn/FadeOut** durante transições do SceneFlow.
- Evitar dependência do fade legado.
- Permitir configurar timings por **profile** (startup/frontend/gameplay).
- Garantir **comportamento determinístico** (ordem fixa) e **observável** (logs canônicos).
- Aplicar política **Strict vs Release**:
  - Strict (Dev/QA): **falhar cedo** quando pré-condições críticas não existem.
  - Release: permitir fallback **somente via modo degradado explícito**.

## Decisão

### Objetivo de produção (sistema ideal)

Garantir que TODA transição de cena do SceneFlow tenha um envelope visual determinístico:

**FadeIn (escurece) → operações de cena → ScenesReady → completion gate → FadeOut (revela) → Completed**

> Nota: no naming atual do runtime, `FadeInAsync()` = “escurecer” (0→1) e `FadeOutAsync()` = “revelar” (1→0).

### Contrato mínimo (produção)

1) **Ordem (invariantes)**
- **FadeIn** inicia **antes** de qualquer mutação visual (load/unload/setActive).
- `ScenesReady` é emitido **após** operações de cena e **antes** de `Completed` (mesma `signature`).
- **Completion gate** é aguardado **antes** do `BeforeFadeOut` e do `FadeOut`.
- `Completed` só ocorre **após** `FadeOut` (quando `UseFade=true`), preservando “reveal before completion”.

2) **Strict vs Release (fail-fast + degraded)**
- **Strict (UNITY_EDITOR / DEVELOPMENT_BUILD)**
  - Falha explicitamente quando:
    - profile não é encontrado em Resources,
    - `IFadeService` não existe no DI global,
    - `FadeScene` não carrega,
    - `FadeController` não existe na `FadeScene`.
- **Release**
  - Pode seguir sem fade **apenas** com `DEGRADED_MODE` explícito:
    - `DEGRADED_MODE feature='fade' reason='<...>' detail='<...>'`
  - Após degradar, o fade vira **no-op** (dur=0) mantendo a ordem do pipeline.

3) **Não criar UI “em voo”**
- O fade depende de `FadeScene` + `FadeController`.
- O runtime não deve instanciar canvas/câmera de forma silenciosa.

## Consequências

### Benefícios

- Envelope visual determinístico em todas as transições do SceneFlow.
- Timings declarativos por profile, sem strings/timings espalhados.
- Comportamento auditável via âncoras canônicas `[OBS][Fade]`.
- Política Strict/Release explícita, reduzindo “fallback silencioso”.

### Trade-offs / riscos

- Strict pode “quebrar cedo” durante integração (o objetivo é expor setup incompleto imediatamente).
- Release pode degradar visualmente (sem fade) — mas isso fica **explícito** via `DEGRADED_MODE`.

## Mapeamento para implementação

Arquivos (NewScripts):

- Orquestração / ordem / anchors `[OBS][Fade]`:
  - `Runtime/Scene/SceneTransitionService.cs`
- Policy Strict/Release + degraded reporter:
  - `Runtime/Mode/IRuntimeModeProvider.cs`
  - `Runtime/Mode/UnityRuntimeModeProvider.cs`
  - `Runtime/Mode/IDegradedModeReporter.cs`
  - `Runtime/Mode/DegradedModeReporter.cs`
- Adapter (profile → config → policy):
  - `Runtime/SceneFlow/SceneFlowAdapters.cs` (`SceneFlowFadeAdapter`)
- Serviço de fade (garante FadeScene + Controller; falha explícita se inválido):
  - `Runtime/SceneFlow/Fade/FadeService.cs`

## Observabilidade (contrato)

**Contrato canônico:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

### Âncoras mínimas de Fade (evidência)

Emitidas por `SceneTransitionService` quando `UseFade=true`:

- `[OBS][Fade] FadeInStarted ...`
- `[OBS][Fade] FadeInCompleted ...`
- `[OBS][Fade] FadeOutStarted ...`
- `[OBS][Fade] FadeOutCompleted ...`

### Âncora canônica de fallback (Release)

Quando o fade não pode operar em Release:

- `DEGRADED_MODE feature='fade' reason='<Reason>' detail='<...>'`

## Critérios de pronto (DoD)

### DoD (implementação)

- [x] Ordem e gating conforme “Contrato mínimo”.
- [x] Policy Strict vs Release aplicada no caminho do fade.
- [x] `DEGRADED_MODE` emitido em Release quando necessário.
- [x] Logs `[OBS][Fade]` emitidos no envelope (Start/Complete por fase).

### DoD (evidência)

- [x] Snapshot com transições (startup + gameplay) contendo as 4 âncoras `[OBS][Fade]` na mesma `signature`: `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`.
- [ ] (Opcional) Um snapshot de falha em Strict comprovando erro explícito para `FadeScene`/controller ausente.

## Procedimento de verificação (QA)

1) **Happy path**
- Use: `QA/SceneFlow/EnterGameplay (TC: Menu->Gameplay ResetWorld)` (ContextMenu).
- Verifique no log da transição:
  - `SceneTransitionStartedEvent` → `[OBS][Fade] FadeInStarted/Completed` → `ScenesReady` → gate → `[OBS][Fade] FadeOutStarted/Completed` → `SceneTransitionCompletedEvent`.

2) **Fail-fast (Strict)**
- Em Editor/Development:
  - Remova temporariamente a `FadeScene` do Build Settings **ou** remova `FadeController` dela.
- Dispare a mesma transição e confirme:
  - exceção clara (`InvalidOperationException`) com `reason`/`detail` (sem seguir “silenciosamente”).

3) **Degraded mode (Release)**
- Em build Release:
  - reproduza a ausência (scene/controller/service) e confirme:
  - `DEGRADED_MODE feature='fade' ...` e transição segue sem fade (no-op), sem crash.

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Snapshot datado (PASS, startup + gameplay):** `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
  - Contém âncoras `[OBS][Fade]` (`FadeInStarted/Completed`, `FadeOutStarted/Completed`) para `profile=startup` e `profile=gameplay` com `signature` completa.
  - Contém evidência de ordenação: `FadeInCompleted` ocorre antes do load; `ScenesReady` + completion gate antes de `FadeOut`; `FadeOutCompleted` antes de `TransitionCompleted`.

## Implementação (arquivos impactados)

### Runtime / Editor (código e assets)

- **Infrastructure**
  - `Runtime/Mode/DegradedModeReporter.cs`
  - `Runtime/Mode/IDegradedModeReporter.cs`
  - `Runtime/Mode/IRuntimeModeProvider.cs`
  - `Runtime/Mode/UnityRuntimeModeProvider.cs`
  - `Runtime/Scene/SceneTransitionService.cs`
  - `Runtime/SceneFlow/Fade/FadeService.cs`
  - `Runtime/SceneFlow/SceneFlowAdapters.cs`

### Docs / evidências relacionadas

- `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`
- `Reports/Evidence/LATEST.md`
- `Standards/Standards.md`

## Referências

- [ADR-0010 — Loading HUD + SceneFlow (NewScripts)](ADR-0010-LoadingHud-SceneFlow.md)
- [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)
- [`Production-Policy-Strict-Release.md`](../Standards/Standards.md#politica-strict-vs-release)
