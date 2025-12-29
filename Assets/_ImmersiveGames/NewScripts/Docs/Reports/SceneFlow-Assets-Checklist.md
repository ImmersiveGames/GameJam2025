# SceneFlow — Assets Checklist (Produção)

Checklist executável para validar os assets necessários ao pipeline **SceneFlow + Fade + Loading HUD**.
**Não criar assets nesta etapa**: apenas verificar se os existentes atendem aos critérios.

> Referências: ADR-0009 (Fade) e ADR-0010 (Loading HUD). Este checklist **não** reescreve ADRs; apenas aponta para eles.

---

## A) Profiles (Resources)

**Onde devem ficar**
- [ ] `Assets/**/Resources/SceneFlow/Profiles/`
  - Resolução via `Resources.Load` com path padrão `SceneFlow/Profiles/<profileName>`.

**Tipo de asset**
- [ ] `NewScriptsSceneTransitionProfile` (`ScriptableObject`).

**Profiles esperados (mínimo)**
- [ ] `startup`
- [ ] `frontend` (Menu)
- [ ] `gameplay` (Menu → Gameplay)
- [ ] *(opcional)* `gameplay_to_frontend` ou equivalente (Gameplay → Menu), conforme o projeto.

**Campos a validar em cada profile (nomes reais do SO)**
- [ ] `useFade`
- [ ] `fadeInDuration`
- [ ] `fadeOutDuration`
- [ ] `fadeInCurve`
- [ ] `fadeOutCurve`
- [ ] **Loading HUD**: não existe flag no `NewScriptsSceneTransitionProfile`.
  - A exibição do HUD é orquestrada por `SceneFlowLoadingService` via eventos do SceneFlow (ADR-0010).
- [ ] **Cenas / cena ativa**: não são definidas no profile.
  - Validar no **SceneTransitionRequest** (campos reais):
    - `ScenesToLoad`, `ScenesToUnload`, `TargetActiveScene`, `UseFade`, `TransitionProfileName`.
    - Fontes típicas:
      - `Assets/_ImmersiveGames/NewScripts/Infrastructure/Navigation/GameNavigationService.cs`
        - `BuildToGameplayRequest()` e `BuildToMenuRequest()`.
      - `Assets/_ImmersiveGames/NewScripts/Infrastructure/GlobalBootstrap.cs`
        - `startPlan` (bootstrap → menu) em `RegisterGameLoopSceneFlowCoordinatorIfAvailable()`.

**Como validar no Editor**
- [ ] Localizar cada asset em `Resources/SceneFlow/Profiles/`.
- [ ] Inspecionar os campos do `NewScriptsSceneTransitionProfile` e confirmar valores coerentes com o fluxo.
- [ ] Confirmar que o **profile name** usado nos `SceneTransitionRequest` corresponde ao asset existente.
- [ ] (Play Mode) Verificar logs: `"[SceneFlow] Profile resolvido: name='...'"`.

---

## B) FadeScene (cena aditiva)

**Nome/caminho esperado**
- [ ] Cena: **`FadeScene`** (Additive).
  - Definido em `NewScriptsFadeService`.

**Componentes obrigatórios**
- [ ] `NewScriptsFadeController`.
- [ ] `CanvasGroup` (no mesmo GameObject ou referenciado).
- [ ] Canvas configurado para ordenação (sorting) de UI.

**Regras de ordenação (ADR-0009 + tooltip do controller)**
- [ ] Fade **abaixo do Loading HUD**.
- [ ] `sortingOrder` do Fade recomendado: **11000** (valor padrão do `NewScriptsFadeController`).

**Como validar**
- [ ] Abrir a `FadeScene`.
- [ ] Conferir se o `NewScriptsFadeController` está presente e com `CanvasGroup` válido.
- [ ] Verificar `sortingOrder` do Canvas (esperado 11000) e se o Fade fica **abaixo** do HUD.
- [ ] Rodar uma transição de teste (Menu → Gameplay) e observar FadeIn/FadeOut.

---

## C) LoadingHudScene (cena aditiva)

**Nome/caminho esperado**
- [ ] Cena: **`LoadingHudScene`** (Additive).
  - Definido em `NewScriptsLoadingHudService`.

**Componentes obrigatórios**
- [ ] `NewScriptsLoadingHudController`.
- [ ] `CanvasGroup` configurado para show/hide.
- [ ] Canvas configurado para overlay/sorting.

**Regras de ordenação (ADR-0010)**
- [ ] `overrideSorting = true`.
- [ ] `sortingOrder` **maior que o Fade** (ex.: **12050** conforme ADR).
- [ ] Loading HUD deve ficar **acima do Fade**.

**Como validar**
- [ ] Abrir a `LoadingHudScene`.
- [ ] Conferir se o `NewScriptsLoadingHudController` está presente e com `CanvasGroup` válido.
- [ ] Validar `overrideSorting` e `sortingOrder` acima do Fade.
- [ ] Rodar uma transição de teste e confirmar HUD aparece no `Started/ScenesReady` e some em `BeforeFadeOut`.

---

## D) Cross-check com ADRs

**Referências obrigatórias**
- [ ] `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0009-FadeSceneFlow.md`
- [ ] `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md`

**Invariantes (ordem esperada do pipeline)**
- [ ] `SceneTransitionStartedEvent` → **Loading HUD Show** (fase Started).
- [ ] `FadeIn` (se `UseFade=true`).
- [ ] Load/Unload/Active → `SceneTransitionScenesReadyEvent`.
- [ ] **Completion gate** (WorldLifecycle reset) antes de revelar a cena.
- [ ] `SceneTransitionBeforeFadeOutEvent` → **Loading HUD Hide** (fase BeforeFadeOut).
- [ ] `FadeOut`.
- [ ] `SceneTransitionCompletedEvent` → **Loading HUD Hide** (safety hide).
