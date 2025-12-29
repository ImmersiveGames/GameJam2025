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

## Logs essenciais (padrões de busca)
> **Objetivo:** coletar evidências com **padrões de busca (grep)** sem depender de textos exatos.
> **Se incluir exemplos, marque como “Exemplo (pode variar)”.**

### SceneFlow (eventos + pipeline)
- **Eventos/classes relevantes**:
  - `SceneTransitionStartedEvent`
  - `SceneTransitionScenesReadyEvent`
  - `SceneTransitionBeforeFadeOutEvent`
  - `SceneTransitionCompletedEvent`
- **Tokens curtos para buscar**:
  - `SceneTransitionStarted`
  - `ScenesReady`
  - `BeforeFadeOut`
  - `TransitionCompleted`
  - `SceneFlow`
- **Exemplo (pode variar)**:
  - `"[SceneFlow] Iniciando transição: SceneTransitionContext(...)"`

### Readiness / Gate (tokens e liberação)
- **Eventos/classes relevantes**:
  - `GameReadinessService`
  - `WorldLifecycleResetCompletionGate`
  - `SimulationGateTokens.SceneTransition`
- **Tokens curtos para buscar**:
  - `[Readiness]`
  - `SimulationGate`
  - `flow.scene_transition`
  - `SceneFlowGate`
  - `signature=`
- **Exemplo (pode variar)**:
  - `"[Readiness] SceneTransitionCompleted → gate liberado"`

### WorldLifecycle (reset + signature)
- **Eventos/classes relevantes**:
  - `WorldLifecycleRuntimeCoordinator`
  - `WorldLifecycleResetCompletedEvent`
  - `SceneTransitionSignatureUtil`
- **Tokens curtos para buscar**:
  - `[WorldLifecycle]`
  - `Reset SKIPPED`
  - `ResetCompleted`
  - `signature=`
  - `reason=`
- **Exemplo (pode variar)**:
  - `"[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='...', signature='...', reason='...'"`

### Fade
- **Eventos/classes relevantes**:
  - `INewScriptsFadeService`
  - `NewScriptsFadeController`
- **Tokens curtos para buscar**:
  - `[Fade]`
  - `FadeIn`
  - `FadeOut`
  - `alpha=`
- **Exemplo (pode variar)**:
  - `"[Fade] Iniciando Fade para alpha=0"`

### Loading HUD
- **Eventos/classes relevantes**:
  - `INewScriptsLoadingHudService`
  - `NewScriptsLoadingHudController`
- **Tokens curtos para buscar**:
  - `[LoadingHUD]`
  - `Show`
  - `Hide`
  - `phase=`
- **Exemplo (pode variar)**:
  - `"[LoadingHUD] Hide aplicado. signature='...', phase='...'"`

## Critérios de aceite
- **Sem exceptions** no Console.
- `SceneTransitionCompletedEvent` **sempre ocorre** em cada transição.
- `WorldLifecycleResetCompletedEvent` contém `signature` que **casa** com `SceneTransitionSignatureUtil.Compute(context)`.
- Após concluir a transição, a cena deve estar revelada (**fade alpha=0**), e o HUD oculto.
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

