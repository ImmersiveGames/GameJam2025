# ADR-0010 — Loading HUD + SceneFlow (NewScripts)

## Status

- Estado: Implementado
- Data: 2025-12-25
- Escopo: SceneFlow + Loading HUD (NewScripts)

## Contexto

O SceneFlow do NewScripts já possui Fade (ADR-0009), porém o HUD de loading precisa ser um módulo separado,
sem depender de legados e sem acoplar ao Fade. Além disso, a transição pode aguardar o completion gate
(WorldLifecycle reset), o que exige manter o HUD visível até o momento correto, mesmo quando o HUD nasce tarde.

## Decisão

### Objetivo de produção (sistema ideal)

Exibir feedback de loading em transições do SceneFlow (quando aplicável) sem acoplar o fluxo de produção ao HUD e sem quebrar determinismo/observabilidade.

### Contrato de produção (mínimo)

- Loading HUD é opcional e não pode bloquear o SceneFlow (no máximo espelha progresso/estado).
- Abertura/fechamento do HUD segue o envelope: (opcional) fade-out → loading → scenes ready → fade-in → hide HUD.
- Falha de binding do HUD deve ser fail-fast em dev/QA; em produção, preferir fallback 'HUD desabilitado' apenas se explicitamente configurado.
- Eventos/estado expostos devem ser observáveis via logs (ver Observability Contract).

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

## Fora de escopo

- Implementar o sistema de fade (ADR-0009).
- Auto-instanciar UI/hud quando assets não existirem (preferir erro).

Evolução futura de Addressables e tarefas agregadas (sem implementação atual):

Diretriz para evolução (sem código por enquanto): tratar o loading como **tarefas agregadas**,
para que o HUD reflita o estado de cada etapa.

Exemplo **PSEUDOCÓDIGO / FUTURO** de vocabulário (nomes de tarefas):
- `SceneLoadTask` (load/unload additive e active scene)
- `WorldResetTask` (reset + spawn/preparação do mundo)
- `AddressablesWarmupTask` (warmup/preload de assets)

O HUD poderia exibir fases agregadas (“Carregando…”, “Preparando…”, “Aquecendo assets…”)
com base na conclusão dessas tarefas. As implementações reais ainda **não existem** e devem
seguir as regras já descritas para SceneFlow/WorldLifecycle.

## Consequências

### Benefícios

- Fade e Loading permanecem responsabilidades separadas.
- O HUD pode existir sem exigir QA ou legados.
- A experiência mantém consistência mesmo quando o HUD nasce após o `Started` (no-fade) ou após o `FadeInCompleted` (com fade).
- O LoadingHUD é carregado como cena additive `LoadingHudScene` via `INewScriptsLoadingHudService`.

### Trade-offs / Riscos

- (não informado)

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- SceneFlow publica estado suficiente (started/ready/completed) para o HUD reagir.
- HUD não cria dependência circular com SceneFlow/WorldLifecycle.
- Evidência: transições com loading exibem logs âncora ou, se não implementado, ADR permanece 'Aberto/Parcial'.

## Notas de implementação

**Ordem observada (UseFade=true):**
- `SceneTransitionStarted` → `FadeIn` (alpha=1)
- `SceneTransitionFadeInCompleted` → `LoadingHUD.Show`
- Load/Unload/Additive + `SceneTransitionScenesReady`
- `WorldLifecycleResetCompletedEvent` (ou SKIP) **antes** do completion gate liberar
- `SceneTransitionBeforeFadeOutEvent` → `LoadingHUD.Hide`
- `FadeOut` (alpha=0)
- `SceneTransitionCompletedEvent` → safety hide

- `Show` acontece após o `FadeInCompleted` (overlay opaco) e antes da carga pesada (load/unload/active).
- `SceneFlow` aguarda o `WorldLifecycleResetCompletionGate` após `ScenesReady`.
- `Hide` ocorre em `BeforeFadeOut`, ou seja, **após** o gate liberar e **antes** do FadeOut.
- Mesmo que o HUD falhe em inicializar, o SceneFlow não deve quebrar (logs + fallback silencioso).

### Fases do LoadingHUD (integração com SceneFlow)
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
    - Quando a IntroStage roda, ela termina por confirmação UI (`IntroStage/UIConfirm`) ou auto skip (`IntroStage/NoContent`),
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

### Atualizações

**2025-12-27**
- O pipeline atual mantém o gate de completion antes do `FadeOut`, garantindo janela segura para o HUD
  permanecer visível até a conclusão do reset (quando aplicável).

**2025-12-28**
- Documentada a integração de loading com GameLoop/InputMode, explicitando que:
    - bootstrap/startup é orquestrado pelo `GameLoopSceneFlowCoordinator` (Boot → Ready);
    - Menu → Gameplay em produção usa `InputModeSceneFlowBridge` em `SceneTransitionCompleted(Profile=gameplay)`
      para iniciar a IntroStage (PostReveal) e só então avançar para `GameLoop.RequestStart()` após `UIConfirm`/`NoContent`;
    - `IGameNavigationService` **não** emite `RequestStart()` diretamente; ele apenas dispara transições de cena.

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - TODO: adicionar evidência de HUD de loading (não aparece na evidência canônica atual).
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Evidências

- Metodologia: [`Reports/Evidence/README.md`](../Reports/Evidence/README.md)
- Evidência canônica (LATEST): [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
- Snapshot  (2026-01-17): [`Baseline-2.1-Evidence-2026-01-17.md`](../Reports/Evidence/2026-01-17/Baseline-2.1-Evidence-2026-01-17.md)
- Contrato: [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Referências

- [ADR-0009 — Fade + SceneFlow (NewScripts)](ADR-0009-FadeSceneFlow.md)
- [WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
- [Observability-Contract.md](../Reports/Observability-Contract.md) — contrato canônico de reasons, campos mínimos e invariantes
- [`Observability-Contract.md`](../Reports/Observability-Contract.md)
- [`Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
