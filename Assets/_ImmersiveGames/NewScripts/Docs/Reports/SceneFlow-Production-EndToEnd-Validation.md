# SceneFlow — Validação End-to-End de Produção (Menu → Gameplay → Menu)

## Objetivo
Validar em Play Mode o ciclo completo de produção **Menu → Gameplay → Menu** usando apenas o caminho oficial de navegação (`IGameNavigationService`).
Este report é o **master** para evidência mínima do fluxo de produção.

## Pré-requisitos
- **Build Settings** inclui as cenas abaixo (com estes nomes exatos):
  - `MenuScene`
  - `GameplayScene`
  - `FadeScene`
  - `LoadingHudScene`
- **Profiles no Resources** em `Resources/SceneFlow/Profiles/`:
  - `startup`
  - `frontend`
  - `gameplay`
  - opcional: `gameplay_to_frontend` (se existir, deve apontar para `MenuScene`)
- **Navegação de produção** confirmada:
  - `MenuPlayButtonBinder` → `IGameNavigationService.RequestToGameplay(reason)`
  - `PauseOverlayController.ReturnToMenuFrontend()` → `GameExitToMenuRequestedEvent` → `ExitToMenuNavigationBridge` → `IGameNavigationService.RequestToMenu(...)`

## Passos de execução (Editor)
> **Nota:** não usar gatilhos QA-only. Use UI/fluxo real de produção.

### 1) Startup → Menu
1. Abra o Unity Editor e pressione **Play**.
2. Observe o Console. Valide logs de bootstrap/flow (ver seção **Logs essenciais**).
3. Confirme que a cena ativa chega em **MenuScene** e que o fluxo `startup` é processado (SKIP de reset esperado).

### 2) Menu → Gameplay (via `IGameNavigationService`)
1. Na UI do menu, clique no botão **Play** (controle real de produção).
2. O `MenuPlayButtonBinder` dispara `RequestToGameplay` e o `GameNavigationService` executa `SceneTransitionService.TransitionAsync` (profile `gameplay`).
3. Aguarde o término do fade + loading + reset (WorldLifecycle) + completion gate.

### 3) Gameplay → Menu (via `IGameNavigationService` / ExitToMenu)
1. No gameplay, abra o **Pause Overlay** e selecione **Menu** (fluxo real de produção).
2. O `PauseOverlayController.ReturnToMenuFrontend()` publica `GameExitToMenuRequestedEvent`, aciona a bridge e chama `IGameNavigationService.RequestToMenu(...)`.
3. Aguarde a transição concluir e o menu reaparecer.

## Logs essenciais (capturar trechos reais)
> **Não inventar nomes** — abaixo estão os logs reais do projeto.

### SceneFlow (eventos + pipeline)
- `SceneTransitionStartedEvent` (emitido pelo SceneFlow):
  - `"[SceneFlow] Iniciando transição: SceneTransitionContext(...)"`
  - `"[Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context=..."`
  - `"[Loading] Started → Ensure + Show. signature='...'"`
- `SceneTransitionScenesReadyEvent`:
  - `"[Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context=..."`
  - `"[WorldLifecycle] SceneTransitionScenesReady recebido. Context=..."`
  - `"[Loading] ScenesReady → Update pending. signature='...'"`
- `SceneTransitionBeforeFadeOutEvent`:
  - `"[Loading] BeforeFadeOut → Hide. signature='...'"`
- `SceneTransitionCompletedEvent`:
  - `"[Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. Context=..."`
  - `"[Loading] Completed → Safety hide. signature='...'"`
  - `"[SceneFlow] Transição concluída com sucesso."`

### Gate / Readiness (tokens e liberação)
- Token de simulação (SceneFlow):
  - `"[Readiness] SimulationGate adquirido com token='flow.scene_transition'. Active=..., IsOpen=..."`
  - `"[Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. Context=..."`
- Completion gate (WorldLifecycleResetCompletionGate):
  - `"[SceneFlowGate] WorldLifecycleResetCompletionGate registrado. timeoutMs=..."`
  - `"[SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='...', reason='...'"`
  - `"[SceneFlowGate] Concluído. signature='...', reason='...'"`

### WorldLifecycle (reset + signature)
- Recebimento de ScenesReady:
  - `"[WorldLifecycle] SceneTransitionScenesReady recebido. Context=..."`
- Reset concluído (ou skip) com assinatura:
  - `"[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='...', signature='...', reason='...'"`
- SKIP esperado para startup/frontend:
  - `"[WorldLifecycle] Reset SKIPPED (startup/frontend). profile='...', activeScene='...'"`

### Fade / Loading
- Fade (controller):
  - `"[Fade] Iniciando Fade para alpha=1 (dur=...)"`
  - `"[Fade] Fade concluído para alpha=1"`
  - `"[Fade] Iniciando Fade para alpha=0 (dur=...)"`
  - `"[Fade] Fade concluído para alpha=0"`
- Loading HUD:
  - `"[LoadingHUD] Show aplicado. signature='...', phase='...'"`
  - `"[LoadingHUD] Hide aplicado. signature='...', phase='...'"`

## Critérios de aceite
- **Sem exceptions** no Console.
- `SceneTransitionCompletedEvent` **sempre ocorre** em cada transição.
- `WorldLifecycleResetCompletedEvent` contém `signature` que **casa** com `SceneTransitionSignatureUtil.Compute(context)`.
- `Loading HUD` **não fica preso ativo** após o completed (log de Hide aplicado deve ocorrer).
- `Fade` **não fica preso** (alpha final coerente: 0 após FadeOut).
- `GameLoop` **não inicia antes** de `WorldLifecycleResetCompletedEvent`:
  - `"[GameLoopSceneFlow] Ready: TransitionCompleted + WorldLifecycleResetCompleted. Chamando GameLoop.RequestStart()."`

## Troubleshooting (atalhos rápidos)
- **“Faltou controller na cena”** (`FailedNoController`):
  - Log: `"[WorldLifecycle] WorldLifecycleController não encontrado na cena '...'. Reset abortado."`
  - Esperado em caso de cena sem controller. Corrigir scene/prefab.
- **“Context default”**:
  - Log: `"[WorldLifecycle] SceneTransitionScenesReady recebido com Context default. Ignorando."`
  - Normal em bootstrap/edge cases — pode ser ignorado.
- **“Profile não encontrado”**:
  - Logs de profile/fade default indicam profile ausente em `Resources/SceneFlow/Profiles/`.
  - Verifique `startup`, `frontend`, `gameplay`.
- **“HUD/Fade sorting errado”**:
  - Referência: [SceneFlow-Assets-Checklist.md](SceneFlow-Assets-Checklist.md)

## Registro (resultado)
- Data:
- Responsável:
- Build/Branch:
- Observações:

