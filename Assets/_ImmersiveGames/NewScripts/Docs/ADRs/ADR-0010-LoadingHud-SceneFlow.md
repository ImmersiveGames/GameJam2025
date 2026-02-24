# ADR-0010 — Loading HUD + SceneFlow (NewScripts)

## Status

- Estado: Implementado
- Data (decisão): 2025-12-24
- Última atualização: 2026-02-04
- Tipo: Implementação
- Escopo: SceneFlow + Loading HUD (NewScripts)

## Contexto

O SceneFlow executa operações que podem causar “pop” visual (load/unload/setActive), especialmente em transições longas (Menu→Gameplay, Restart). O projeto precisava de um **Loading HUD** com as seguintes propriedades:

- Integrar no envelope do SceneFlow (mesma `signature` / mesma ordem).
- Ser **determinístico** (show/hide em pontos fixos) e **auditável** (logs canônicos).
- Seguir política **Strict vs Release**:
  - Strict (Dev/QA): falhar cedo se o HUD estiver mal configurado.
  - Release: permitir seguir sem HUD somente com degraded explícito.

Além disso, o HUD não deve depender de instanciar UI “em voo” de forma silenciosa.

## Decisão

### Objetivo de produção

Garantir que transições do SceneFlow possam opcionalmente exibir um “Loading HUD” **sobre** o envelope de fade:

**FadeIn (escurece) → LoadingHUD.Show → operações de cena → ScenesReady → completion gate → LoadingHUD.Hide → FadeOut (revela) → Completed**

> O fade continua sendo a primeira/última camada visual (ADR-0009). O Loading HUD é uma camada intermediária, usada quando a transição requer feedback.

### Contrato mínimo (produção)

1) **Pontos de show/hide (invariantes)**
- `Show` ocorre após `FadeInCompleted` e antes das mutações de cena.
- `Hide` ocorre após `ScenesReady` + completion gate e antes do `BeforeFadeOut`/`FadeOut`.
- `Hide` é **forçado** no caminho de erro/early-exit para evitar HUD “preso”.

2) **Strict vs Release (fail-fast + degraded)**
- **Strict (UNITY_EDITOR / DEVELOPMENT_BUILD)**
  - Falha explicitamente quando:
    - `ILoadingHudService` não existe no DI global,
    - a cena/objeto do Loading HUD não está configurado,
    - o controller do HUD não existe/é inválido.
- **Release**
  - Pode seguir sem HUD **apenas** com `DEGRADED_MODE` explícito:
    - `DEGRADED_MODE feature='loading_hud' reason='<...>' detail='<...>'`
  - Após degradar, o HUD vira **no-op** preservando a ordem (show/hide são ignorados).

3) **Não criar UI “em voo”**
- O Loading HUD é fornecido por cena/asset configurado (ou equivalente), não por instância silenciosa em runtime.

### Não-objetivos (resumo)

- UX/arte do HUD (layout, progressos, textos).
- Driver paralelo fora do pipeline canônico.

## Fora de escopo

- Recriar HUD por instância em runtime.
- Alterar o envelope de Fade (ver ADR-0009).

## Consequências

### Benefícios

- Reduz “pop” visual e dá feedback em transições longas.
- Mesma disciplina de contrato do SceneFlow: ordem fixa, evidência e policy.
- Falhas de setup ficam óbvias em Strict; builds Release degradam explicitamente.

### Trade-offs / riscos

- Integração adiciona pontos extras de configuração (cena/controller/DI).
- Release pode degradar (sem HUD) — mas fica explícito via `DEGRADED_MODE`.

## Mapeamento para implementação

Arquivos (NewScripts):

- Orquestração / ordem / anchors `[LoadingHud*]`:
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs`
- Serviço de loading HUD (setup + Strict/Release + no-op em degraded):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
- Controller do HUD (visibilidade/efeito):
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Bindings/LoadingHudController.cs`

## Observabilidade (contrato)

**Contrato canônico:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

### Âncoras mínimas de Loading HUD (evidência)

Emitidas por `LoadingHudService` e `LoadingHudOrchestrator` quando `UseLoadingHud=true`:

- `[LoadingHudEnsure] ...`
- `[LoadingHudShow] ...`
- `[LoadingHudHide] ...`
- `[LoadingDegraded] ...` (fallback)

### Âncora canônica de fallback (Release)

Quando o HUD não pode operar em Release:

- `DEGRADED_MODE feature='loading_hud' reason='<Reason>' detail='<...>'`

## Critérios de pronto (DoD)

### DoD (implementação)

- [x] Pontos de show/hide obedecem a ordem do “Contrato mínimo”.
- [x] Policy Strict vs Release aplicada.
- [x] `DEGRADED_MODE` emitido em Release quando necessário.
- [x] Logs canônicos emitidos (`LoadingHudEnsure/Show/Hide` + `LoadingDegraded`).

### DoD (evidência)

- [ ] Snapshot contendo uma transição com `UseLoadingHud=true` e as âncoras `LoadingHudEnsure/Show/Hide` na mesma `signature`.

## Procedimento de verificação (QA)

1) **Happy path**
- Dispare uma transição com `UseLoadingHud=true` (ex.: Menu→Gameplay em perfis que demandam HUD).
- Confirme no log:
  - `FadeInCompleted` → `[LoadingHudShow]` → operações de cena → `ScenesReady` → gate → `[LoadingHudHide]` → `FadeOut` → `Completed`.

2) **Fail-fast (Strict)**
- Em Editor/Development:
  - remova temporariamente o service/controller do HUD.
- Confirme falha explícita (exception) com `reason/detail`.

3) **Degraded mode (Release)**
- Em build Release:
  - reproduza a ausência e confirme:
  - `DEGRADED_MODE feature='loading_hud' ...` e transição segue sem HUD.

## Evidência

- **Última evidência (log bruto):** `Docs/Reports/lastlog.log`
- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)

## Implementação (arquivos impactados)

### Runtime / Editor (código e assets)

- **Infrastructure**
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Runtime/SceneFlowAdapterFactory.cs`

### Docs / evidências relacionadas

- `Reports/Evidence/LATEST.md`
- `Standards/Standards.md`

## Referências

- [ADR-0009 — Fade + SceneFlow (NewScripts)](ADR-0009-FadeSceneFlow.md)
- [`Standards.md`](../Standards/Standards.md)
