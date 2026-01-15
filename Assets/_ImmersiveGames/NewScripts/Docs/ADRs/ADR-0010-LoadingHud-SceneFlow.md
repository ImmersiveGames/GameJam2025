# ADR-0010 — Loading HUD + SceneFlow (NewScripts)

**Data:** 2025-12-25
**Status:** Implementado

## Contexto

O Scene Flow do NewScripts já possui Fade (ADR-0009), porém o HUD de loading precisa ser um módulo separado,
sem depender de legados e sem acoplar ao Fade. Além disso, a transição pode aguardar o completion gate
(WorldLifecycle reset), o que exige manter o HUD visível até o momento correto, mesmo quando o HUD nasce tarde.

## Decisão

Implementar um módulo de Loading HUD separado do Fade, com as regras:

1) O `SceneFlowLoadingService` deve chamar `INewScriptsLoadingHudService.EnsureLoadedAsync()` no `SceneTransitionStartedEvent`.
    - Se `UseFade=false`, a HUD pode ser exibida imediatamente (mantém comportamento antigo).
    - Se `UseFade=true`, **não** exibir no Started: aguardar o `SceneTransitionFadeInCompletedEvent`.

2) Quando `UseFade=true`, o `SceneFlowLoadingService` deve exibir a HUD apenas após o `SceneTransitionFadeInCompletedEvent`
   (tela já escura), mantendo-a visível durante `Load/Unload/ActiveScene` e durante o gate do `WorldLifecycle`.

3) Em `SceneTransitionScenesReadyEvent`, a HUD pode atualizar a fase/status (idempotente), sem esconder.

4) Em `SceneTransitionBeforeFadeOutEvent`, a HUD deve ser escondida.

5) Em `SceneTransitionCompletedEvent`, executar um “safety hide” (idempotente).

**Ordem observada (UseFade=true):**
- `SceneTransitionStarted` → `FadeIn` (alpha=1)
- `SceneTransitionFadeInCompleted` → `LoadingHUD.Show`
- Load/Unload/Additive + `SceneTransitionScenesReady`
- `WorldLifecycleResetCompletedEvent` (ou SKIP) **antes** do completion gate liberar
- `SceneTransitionBeforeFadeOutEvent` → `LoadingHUD.Hide`
- `FadeOut` (alpha=0)
- `SceneTransitionCompletedEvent` → safety hide

## Consequências

- Fade e Loading permanecem responsabilidades separadas.
- O HUD pode existir sem exigir QA ou legados.
- A experiência mantém consistência mesmo quando o HUD nasce após o `Started` (no-fade) ou após o `FadeInCompleted` (com fade).
- O LoadingHUD é carregado como cena additive `LoadingHudScene` via `INewScriptsLoadingHudService`.

- `Show` acontece após o `FadeInCompleted` (overlay opaco) e antes da carga pesada (load/unload/active).
- `SceneFlow` aguarda o `WorldLifecycleResetCompletionGate` após `ScenesReady`.
- `Hide` ocorre em `BeforeFadeOut`, ou seja, **após** o gate liberar e **antes** do FadeOut.
- Mesmo que o HUD falhe em inicializar, o Scene Flow não deve quebrar (logs + fallback silencioso).

## Fases do LoadingHUD (integração com SceneFlow)
- **Started:** `EnsureLoadedAsync` apenas (sem `Show` quando `UseFade=true`).
- **AfterFadeIn/ScenesReady:** `Show`/update idempotente.
- **BeforeFadeOut:** `Hide`.
- **Completed:** safety hide (idempotente).

O “loading real” não é apenas **load/unload** de cenas. Ele inclui:
- **Reset do mundo**, com hooks e participantes por escopo.
- **Spawn/Preparação** do mundo (quando aplicável).

Na prática, o loading só é considerado **concluído** quando o
`WorldLifecycleResetCompletedEvent` é emitido. O `FadeOut` deve acontecer **depois** disso,
garantindo que o jogador só veja a cena quando o mundo já foi preparado.

### Integração com GameLoop e InputMode

No fluxo de produção:

- O `GameLoopSceneFlowCoordinator` orquestra um `StartPlan` e, ao final do pipeline (`ScenesReady` + `WorldLifecycleResetCompletedEvent` + completion gate),
  solicita `GameLoop.RequestStart()`.
  - Em **startup/frontend**, isso normalmente leva `Boot → Ready` (não-inicia gameplay).
  - Em **gameplay**, se o GameLoop ainda estiver em `Boot/Ready`, isso pode levar `Ready → Playing`.

- Em transições **Menu → Gameplay** com `Profile='gameplay'`, a responsabilidade é dividida:
    - O SceneFlow + WorldLifecycle garantem:
        - `SceneTransitionScenesReadyEvent` (cenas carregadas).
        - `WorldLifecycleResetCompletedEvent` (reset/spawn concluído).
        - `SceneTransitionCompletedEvent(Profile=gameplay)` após o gate e FadeOut.
    - O `InputModeSceneFlowBridge` aplica o modo `Gameplay` em `SceneFlow/Completed:Gameplay` e tenta iniciar a **IntroStage** (PostReveal),
      mantendo o gameplay bloqueado via `sim.gameplay`.
    - **Nota (estado atual do código):** se o GameLoop já tiver entrado em `Playing` antes do `SceneTransitionCompletedEvent` (por exemplo, via `GameLoopSceneFlowCoordinator`),
      o bridge considera o gameplay ativo e **não** inicia a IntroStage.
    - Quando a IntroStage roda, ela termina por confirmação UI (`IntroStage/UIConfirm`) ou auto-skip (`IntroStage/NoContent`),
      e só então o GameLoop faz `RequestStart()` para entrar em `Playing`.

Contrato esperado:

- O GameLoop só entra em `Playing` **depois** que:
    - O reset/spawn foi concluído (WorldLifecycle).
    - A transição foi marcada como `Completed(Profile=gameplay)`.
    - A IntroStage foi concluída (ou pulada) e o token `sim.gameplay` foi liberado.

> Observação: no estado atual do código, o GameLoop pode entrar em `Playing` antes do `Completed` em alguns fluxos
> (ex.: quando `RequestStart()` é solicitado cedo). Ao consolidar a IntroStage como gate de entrada, a chamada
> `RequestStart()` em gameplay deve ser postergada (preferindo `RequestReady()` até a conclusão da IntroStage).

- O `IGameNavigationService` continua responsável apenas por **disparar transições de cena**
  (`SceneTransitionService.TransitionAsync(...)`), sem chamar `RequestStart()` diretamente.

Essa integração garante que:

- O conceito de “loading real” abrange **SceneFlow + Fade + Loading HUD + WorldLifecycle + GameLoop/InputMode**.
- O jogador só tem input de gameplay quando:
    - a cena de Gameplay está visível (FadeOut concluído),
    - o mundo foi resetado/spawnado,
    - e o GameLoop está em `Playing`.

## Evolução futura: Addressables

Diretriz para evolução (sem código por enquanto): tratar o loading como **tarefas agregadas**,
para que o HUD reflita o estado de cada etapa.

Exemplo **PSEUDOCÓDIGO / FUTURO** de vocabulário (nomes de tarefas):
- `SceneLoadTask` (load/unload additive e active scene)
- `WorldResetTask` (reset + spawn/preparação do mundo)
- `AddressablesWarmupTask` (warmup/preload de assets)

O HUD poderia exibir fases agregadas (“Carregando…”, “Preparando…”, “Aquecendo assets…”)
com base na conclusão dessas tarefas. As implementações reais ainda **não existem** e devem
seguir as regras já descritas para SceneFlow/WorldLifecycle.

## Referências

- [ADR-0009 — Fade + SceneFlow (NewScripts)](ADR-0009-FadeSceneFlow.md)

## Atualização (2025-12-27)

- O pipeline atual mantém o gate de completion antes do `FadeOut`, garantindo janela segura para o HUD
  permanecer visível até a conclusão do reset (quando aplicável).

## Atualização (2025-12-28)

- Documentada a integração de loading com GameLoop/InputMode, explicitando que:
    - bootstrap/startup é orquestrado pelo `GameLoopSceneFlowCoordinator` (Boot → Ready);
    - Menu → Gameplay em produção usa `InputModeSceneFlowBridge` em `SceneTransitionCompleted(Profile=gameplay)`
      para iniciar a IntroStage (PostReveal) e só então avançar para `GameLoop.RequestStart()` após `UIConfirm`/`NoContent`;
    - `IGameNavigationService` **não** emite `RequestStart()` diretamente; ele apenas dispara transições de cena.
