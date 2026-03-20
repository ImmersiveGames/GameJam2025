# Mock Harness Visual Overlay Round 2

## 1. Resumo
A correção substituiu o comportamento temporal de avanço da IntroStage por interação visual explícita no `Mock Harness`.  
O overlay agora é orientado por estado canônico real (`IntroStage`, `Playing`, `PostPlay`) e expõe somente ações manuais via botões canônicos.

## 2. Problema corrigido (timer -> GUI visual)
- Problema encontrado:
  - `IntroStageCoordinator` ainda tinha fallback temporal (`Task.WhenAny(..., Task.Delay(...))`) com `SkipIntroStage("timeout")`.
- Correção aplicada:
  - remoção do timeout de conclusão da IntroStage no coordenador;
  - conclusão passa a depender de ação canônica manual (ou step canônico), sem auto-avanço por tempo;
  - overlay visual passou a mostrar seções por estado para orientar a validação manual em Play Mode.

## 3. Entry points canônicos usados
- `Complete IntroStage`:
  - `IIntroStageControlService.CompleteIntroStage(string reason)`
- `Force Victory`:
  - `IGameRunEndRequestService.RequestEnd(GameRunOutcome.Victory, reason)`
- `Force Defeat`:
  - `IGameRunEndRequestService.RequestEnd(GameRunOutcome.Defeat, reason)`
- Estado/PostGame para leitura visual:
  - `IGameLoopService.CurrentStateIdName`
  - `IPostGameResultService` (preferencial)
  - `IGameRunStateService` (fallback de leitura)

## 4. Arquitetura do overlay visual
- `MockOverlay`:
  - renderiza painel único compacto;
  - decide visibilidade por estado canônico;
  - não usa timer para controlar fluxo;
  - não depende de menu da Unity.
- `IntroStageMockController`:
  - mantém ação manual de conclusão via serviço canônico.
- `PostGameMockController`:
  - agora valida estado `Playing` para liberar `Force Victory/Defeat`.
- `MockHarnessConfigAsset`:
  - mantém opt-in do harness e parâmetros visuais.

## 5. IntroStage visual
- Quando `IIntroStageControlService.IsIntroStageActive == true` ou estado `IntroStage`:
  - overlay exibe seção `IntroStage Active`;
  - botão `Complete IntroStage` é mostrado/habilitado.
- Ação do botão:
  - chama `IIntroStageControlService.CompleteIntroStage(...)`.
- Sem auto-complete temporal.

## 6. Victory/Defeat visual controls
- Quando estado canônico é `Playing`:
  - overlay exibe seção `Gameplay Active`;
  - botões `Force Victory` e `Force Defeat` aparecem.
- Ações dos botões:
  - chamam `IGameRunEndRequestService.RequestEnd(...)` com outcome correspondente.
- `PostGameMockController` bloqueia chamadas fora de `Playing`.

## 7. PostGame visual state
- Quando estado canônico é `PostPlay`:
  - overlay exibe seção `PostGame Active`;
  - mostra `Result` e `Reason`.
- Fonte de dados:
  - primeiro `IPostGameResultService`;
  - fallback em `IGameRunStateService` se necessário.
- O overlay apenas observa e informa; não substitui a UI real de PostGame.

## 8. Como habilitar/desabilitar
- Habilitar:
  1. Configurar `MockHarnessConfigAsset` no `NewScriptsBootstrapConfigAsset.mockHarnessConfig`.
  2. Definir `EnabledInPlayMode = true`.
- Desabilitar:
  - remover referência de config ou deixar `EnabledInPlayMode = false`.
- Quando desabilitado:
  - o overlay não é instalado.

## 9. Sanity checks
- Confirmado:
  - remoção do timeout no `IntroStageCoordinator` (sem `Task.WhenAny` e sem `SkipIntroStage("timeout")`);
  - overlay mostra seções por estado real (`IntroStage`, `Playing`, `PostPlay`);
  - `Complete IntroStage` chama serviço canônico de IntroStage;
  - `Force Victory/Defeat` chamam serviço canônico de fim de run;
  - `PostGame` mostra indicador visual e resultado canônico;
  - `Mock Harness` permanece opt-in e isolado.

## 10. Limitações e próximos passos
- Limitação:
  - validação visual final depende de execução em Play Mode no Unity Editor.
- Próximos passos:
  1. validar em cena real a transição `IntroStage -> Playing -> PostPlay`;
  2. ajustar apenas layout/estilo do overlay se necessário (sem alterar entrypoints);
  3. manter o harness sem timers e sem trilho paralelo.
