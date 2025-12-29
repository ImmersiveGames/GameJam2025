# ADR-0010 — Loading HUD + SceneFlow (NewScripts)

**Data:** 2025-12-25
**Status:** Implementado

## Contexto

O Scene Flow do NewScripts já possui Fade (ADR-0009), porém o HUD de loading precisa ser um módulo separado,
sem depender de legados e sem acoplar ao Fade. Além disso, a transição pode aguardar o completion gate
(WorldLifecycle reset), o que exige manter o HUD visível até o momento correto, mesmo quando o HUD nasce tarde.

## Decisão

Implementar um módulo de Loading HUD separado do Fade, com as regras:

1) **Show no Started**
- Ao receber `SceneTransitionStartedEvent`, o HUD é exibido com texto "Carregando...".

2) **Manter durante o gate**
- O HUD permanece visível durante `SceneTransitionScenesReadyEvent` e durante o completion gate.
- Texto atualizado para "Preparando..." como fase intermediária.

3) **Hide antes do FadeOut**
- Um novo evento `SceneTransitionBeforeFadeOutEvent` é emitido antes do FadeOut.
- O HUD é ocultado nesse evento para evitar piscar no momento de revelar a cena.

4) **HUD pronto + ordenação acima do Fade**
- O HUD vive no `LoadingHudScene` (Additive) via `NewScriptsLoadingHudController`.
- O `NewScriptsLoadingHudService` garante o carregamento do HUD e aplica o estado pendente assim que o controller é localizado.
- O Canvas do HUD deve usar `overrideSorting=true` e `sortingOrder` maior que o Fade (ex.: 12050).

5) **Pending quando HUD nasce tarde**
- Se o HUD ainda não existir no `Started`/`ScenesReady`, o serviço guarda pendências por assinatura da transição.
- Assim que o `EnsureLoadedAsync` localiza o controller, aplica a fase pendente imediatamente.

6) **Host no LoadingHudScene (Additive)**
- O `NewScriptsLoadingHudController` vive no `LoadingHudScene`.
- Caso o HUD nasça tarde, o serviço aplica o estado atual imediatamente.

## Consequências

- Fade e Loading permanecem responsabilidades separadas.
- O HUD pode existir sem exigir QA ou legados.
- A experiência mantém consistência mesmo quando o HUD nasce após o `Started`.

## Loading inclui Reset/Spawn

O “loading real” não é apenas **load/unload** de cenas. Ele inclui:
- **Reset do mundo**, com hooks e participantes por escopo.
- **Spawn/Preparação** do mundo (quando aplicável).

Na prática, o loading só é considerado **concluído** quando o
`WorldLifecycleResetCompletedEvent` é emitido. O `FadeOut` deve acontecer **depois** disso,
garantindo que o jogador só veja a cena quando o mundo já foi preparado.

### Integração com GameLoop e InputMode

No fluxo de produção:

- O `GameLoopSceneFlowCoordinator` orquestra o `GameLoop.RequestStart()` na transição de **bootstrap/startup**
  (ex.: `NewBootstrap` → `MenuScene`), levando o GameLoop de `Boot` para `Ready` após `ScenesReady + gate`.

- Em transições **Menu → Gameplay** com `Profile='gameplay'`, a responsabilidade é dividida:
    - O SceneFlow + WorldLifecycle garantem:
        - `SceneTransitionScenesReadyEvent` (cenas carregadas).
        - `WorldLifecycleResetCompletedEvent` (reset/spawn concluído).
        - `SceneTransitionCompletedEvent(Profile=gameplay)` após o gate e FadeOut.
    - O `InputModeService` aplica o modo `'Gameplay'` em `SceneFlow/Completed:Gameplay`.
    - O `InputModeSceneFlowBridge` observa `SceneTransitionCompletedEvent` com `Profile='gameplay'` e,
      ao detectar o GameLoop em `Ready`, solicita `GameLoop.RequestStart()` para entrar em `Playing`.

Com isso:

- O GameLoop só entra em `Playing` **depois** que:
    - O reset/spawn foi concluído (WorldLifecycle).
    - A transição foi marcada como `Completed(Profile=gameplay)`.
    - O `InputMode` já foi trocado para `'Gameplay'` (input pronto).

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
      para solicitar `GameLoop.RequestStart()` (Ready → Playing), após reset/spawn e troca de input;
    - `IGameNavigationService` **não** emite `RequestStart()` diretamente; ele apenas dispara transições de cena.
