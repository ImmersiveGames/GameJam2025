# QA — GameLoop + SceneFlow (State Flow)

## Objetivo
Validar que o fluxo de produção **Boot → Menu → Gameplay → Menu → Gameplay** está consistente com o pipeline
**SceneFlow + WorldLifecycle + GameLoop + InputMode**, incluindo o comportamento de **skip** em startup/frontend.

## Pré-requisitos
- Build ou Play Mode com logs verbosos habilitados.
- Transições via `IGameNavigationService` (Menu → Gameplay / Gameplay → Menu).
- QA hotkeys ativos (`PostGameQaHotkeys`).

---

## 1) Boot + Startup profile (MenuScene)
### Verificações visuais
- **Fade** entra e sai (FadeIn/FadeOut) durante o boot.
- **LoadingHUD** aparece e é atualizado nas fases corretas.

### Checkpoints de log (exemplos curtos)
- Registro do completion gate:
  - `WorldLifecycleResetCompletionGate` registrado como `ISceneTransitionCompletionGate`.
- Cena pronta (startup/frontend):
  - `SceneTransitionScenesReadyEvent` recebido.
  - `WorldLifecycle reset concluído (ou skip). reason='Skipped_StartupOrFrontend'`.
- Coordinator destravado:
  - `[GameLoopSceneFlow] Ready: TransitionCompleted + WorldLifecycleResetCompleted. Chamando GameLoop.RequestStart().`
- Resultado esperado:
  - GameLoop entra em **Ready**, **sem** ir para `Playing`.
  - `InputModeService` fica em **FrontendMenu**.

---

## 2) Menu → Gameplay → Menu → Gameplay (múltiplas rodadas)
### Repetibilidade sem vazamentos
- A transição pode ser executada repetidamente sem acumular serviços de cena.
- Serviços registrados por cena devem aparecer/limpar corretamente:
  - `IActorRegistry`, `IWorldSpawnServiceRegistry`, `WorldLifecycleHookRegistry`, `IResetScopeParticipant`.

### Reset determinístico e spawn
- Ao entrar em Gameplay:
  - `SceneTransitionScenesReadyEvent` dispara hard reset (profile `gameplay`).
  - `WorldLifecycleOrchestrator` executa fases determinísticas na ordem esperada.
  - `Player` e `Eater` aparecem na `GameplayScene` após o reset.

### InputMode + GameLoop
- No `SceneTransitionCompleted(profile='gameplay')`:
  - `InputMode` muda para **Gameplay**.
  - Log curto esperado:
    - `[InputModeSceneFlowBridge] [GameLoop] SceneFlow/Completed:Gameplay -> solicitando GameLoop.RequestStart().`
- GameLoop deve ir **Ready → Playing**.

---

## 3) QA de pós-game (hotkeys)
### Defeat / Victory forçados
- F6 → dispara `GameRunEndedEvent` com `Outcome=Defeat`, `Reason='QA_ForcedDefeat'`.
- F7 → dispara `GameRunEndedEvent` com `Outcome=Victory`, `Reason='QA_ForcedVictory'`.
- `PostGameOverlayController` exibe o overlay correspondente.

### Warnings esperados
- Ao usar hotkeys QA, é esperado ver:
  - `[WARNING] [GameRunStatusService] [GameLoop] GameLoopService indisponível ao processar GameRunEndedEvent. RequestEnd() não foi chamado.`
- Esses warnings **não** devem aparecer durante gameplay normal (sem hotkeys).

---

## Observações
- Sempre que referenciar logs, use apenas trechos curtos.
- O fluxo só é considerado **OK** quando:
  - reset/hard reset conclui antes do `FadeOut`,
  - e o GameLoop chega em `Playing` **apenas** no profile `gameplay`.
