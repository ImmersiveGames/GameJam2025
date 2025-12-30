# ADR-0010 – LoadingHUD integrado ao Scene Flow (NewScripts)

**Status:** Implementado
**Data:** 2025-12-28
**Escopo:** `SceneFlow`, `LoadingHudScene`, `UIGlobalScene`, `Fade`

---

## 1. Contexto

O pipeline NewScripts precisava de uma HUD de loading **não-legada**, integrada ao `SceneTransitionService`
para cobrir transições (startup, menu→gameplay, gameplay→menu) com:

- exibição e ocultação determinística (por fases do Scene Flow);
- desacoplamento do conteúdo de gameplay;
- compatibilidade com o Fade (o loading não deve “piscar” atrás do fade).

---

## 2. Decisão

1. Criar `LoadingHudScene` (Additive) contendo `NewScriptsLoadingHudController` (CanvasGroup).
2. Disponibilizar um serviço global `INewScriptsLoadingHudService` que:
    - garante que `LoadingHudScene` esteja carregada (Additive) quando necessário;
    - localiza o controller e aplica `Show/Hide` via CanvasGroup.
3. Criar `SceneFlowLoadingService` (global) que escuta eventos do Scene Flow e aciona o HUD por fase:

- `SceneTransitionStartedEvent` → Ensure + Show (fase `Started`)
- `SceneTransitionScenesReadyEvent` → Show (fase `ScenesReady`, “still loading but world loaded is in progress”)
- `BeforeFadeOut` (ponto interno do SceneFlow) → Hide (fase `BeforeFadeOut`)
- `SceneTransitionCompletedEvent` → Safety Hide (fase `Completed`)

O serviço é **idempotente**: chamar `Show/Hide` repetidamente é seguro.

---

## 3. Regras de ordenação com Fade

- `Show` acontece antes do FadeIn iniciar a carga pesada.
- `Hide` deve ocorrer **antes** do FadeOut, para evitar ver o HUD durante o retorno à cena.
- Mesmo que o HUD falhe em inicializar, o Scene Flow não deve quebrar (logs + fallback silencioso).

---

## 4. Consequências

### Benefícios
- HUD de loading padronizada para todas as transições.
- Sem acoplamento com `UIGlobalScene` (overlays podem coexistir).
- Observabilidade: cada fase registra `signature` (context signature) e `phase`.

### Trade-offs
- Mantém uma cena adicional (`LoadingHudScene`) sempre disponível quando o jogo transiciona frequentemente.
- Exige que o serviço global esteja registrado antes de qualquer transição (GlobalBootstrap).

---

## 5. Evidências

Logs típicos:
- `[Loading] Started → Ensure + Show`
- `[Loading] ScenesReady → Update pending`
- `[Loading] BeforeFadeOut → Hide`
- `[Loading] Completed → Safety hide`
