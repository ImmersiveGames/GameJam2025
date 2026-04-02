# ADR-0027 Alignment — Level-Owned Intro / Global PostGame — Round 5

## 1. Resumo
Esta rodada removeu o eixo global de mock/config para Intro e realinhou a Intro visual ao trilho level-owned do projeto.  
O `PostGame` foi mantido no eixo global (`GameLoop + PostGame`), com hook de level apenas opcional e reativo.

## 2. Contrato do ADR aplicado
- `IntroStage` permanece opcional e level-owned (derivado de `LevelDefinitionAsset.HasIntroStage` via contrato de apresentação do level atual).
- `PostGame` permanece global e centralizado.
- Hook de level no pós-run permanece opcional e não assume ownership do fluxo.
- `Restart` segue fora do hook de pós-run.
- Se o level não tiver intro, o fluxo segue direto para gameplay sem erro.

## 3. Problema arquitetural da solução global anterior
- A solução anterior introduzia `MockHarnessConfig` no bootstrap global e overlay global para Intro.
- Isso criava acoplamento transversal para uma responsabilidade que deve nascer do level atual.
- Também induzia interpretação incorreta de que Intro poderia ser governada por configuração global.

## 4. Como a Intro passou a ser fornecida pelo level default
- A apresentação visual temporária foi movida para `LevelFlow`, no mesmo eixo do contrato de level:
  - `DefaultLevelIntroOverlayPresenter` foi implementado em `LevelStageOrchestrator.cs`.
  - instalação ocorre no construtor do `LevelStageOrchestrator` (`EnsureInstalled()`), sem config de bootstrap.
- Regras de visibilidade do overlay:
  - estado canônico `GameLoopStateId.IntroStage`;
  - `IIntroStageControlService.IsIntroStageActive == true`;
  - contrato atual de level válido com `HasIntroStage == true`.
- Assim, a Intro visual só existe quando o level atual realmente expõe intro.

## 5. Entry point canônico da conclusão da Intro
- Botão do overlay level-owned chama exclusivamente:
  - `IIntroStageControlService.CompleteIntroStage("LevelIntro/ConfirmButton")`

## 6. Como o PostGame permaneceu global
- Nenhuma ownership de pós-run foi movida para Level Stage.
- O fluxo global continua no eixo:
  - `IGameRunEndRequestService` -> `GameRunOutcome`/`GameRunEndedEvent`
  - `GameLoop` (`PostPlay`)
  - serviços de `PostGame` globais.
- Não foi criado `PostStage` de level nem variação equivalente.

## 7. Hook opcional do level no pós-run (se aplicável)
- `ILevelPostGameHookService`/`LevelPostGameHookService` foi preservado como reação opcional por level.
- O hook não substitui nem captura o fluxo global de `PostGame`.
- `Restart` permanece fora do hook.

## 8. O que foi removido da solução anterior
- Removida dependência global de bootstrap para mock da Intro:
  - `NewScriptsBootstrapConfigAsset` sem `mockHarnessConfig`.
- Removida instalação global de mock no pipeline:
  - retirada de `InstallMockHarnessIfEnabled()`.
- Removidos arquivos globais de mock harness:
  - `Infrastructure/Composition/GlobalCompositionRoot.MockHarness.cs`
  - `Infrastructure/Testing/Mocks/MockHarnessConfigAsset.cs`
  - `Infrastructure/Testing/Mocks/MockHarnessInstaller.cs`
  - `Infrastructure/Testing/Mocks/MockOverlay.cs`
  - `Infrastructure/Testing/Mocks/IntroStageMockController.cs`
  - `Infrastructure/Testing/Mocks/PostGameMockController.cs`
- Removido comportamento temporal de Intro:
  - `IntroStageCoordinator` sem timeout de conclusão;
  - `ConfirmToStartIntroStageStep` sem caminho de timeout/auto-complete.

## 9. Sanity checks
- Confirmado:
  - sem referência de runtime a `MockHarness`/`mockHarnessConfig` fora de docs históricos;
  - Intro visual depende de contrato do level atual (`ILevelStagePresentationService`) + estado canônico;
  - botão usa `IIntroStageControlService.CompleteIntroStage("LevelIntro/ConfirmButton")`;
  - sem timer para concluir Intro (`timeout` removido dos pontos canônicos);
  - `PostGame` global preservado;
  - nenhum `PostStage` por level foi introduzido;
  - `Restart` não foi desviado para hook de pós-run.

## 10. Limitações e próximos passos
- A intro visual atual é fallback temporário para o level default, adequada enquanto os levels finais não tiverem suas próprias apresentações.
- Próximos passos:
  1. mover apresentação de intro para assets/views específicos de cada level quando existirem;
  2. manter `DefaultLevelIntroOverlayPresenter` apenas como fallback transitório de desenvolvimento do conteúdo;
  3. atualizar docs históricas de rounds anteriores para evitar ambiguidade arquitetural.
