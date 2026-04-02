# Mock Harness Restore Round 1

## 1. Resumo
Foi implementado um Mock Harness canônico, opt-in e isolado para validação manual do fluxo atual sem restaurar trilho `DevQA`.  
As três ações mínimas foram cobertas: `Complete IntroStage`, `Force Victory` e `Force Defeat`.  
O harness chama apenas entrypoints canônicos já existentes no runtime.

## 2. Entry points canônicos identificados
- Complete IntroStage
  - Entry point: `IIntroStageControlService.CompleteIntroStage(string reason)`
  - Implementação canônica: `IntroStageControlService`
  - Evidência: serviço global registrado por `GlobalCompositionRoot.RegisterIntroStageControlService()` e consumido por `IntroStageCoordinator`/`ConfirmToStartIntroStageStep`.
  - Justificativa: finaliza a IntroStage pelo gate formal do módulo, sem hack de estado interno.
- Force Victory
  - Entry point: `IGameRunEndRequestService.RequestEnd(GameRunOutcome.Victory, reason)`
  - Implementação canônica: `GameRunEndRequestService` (publica `GameRunEndRequestedEvent`)
  - Evidência: registrado por `RegisterGameRunEndRequestService()`, consumido por `GameRunOutcomeCommandBridge` -> `IGameRunOutcomeService`.
  - Justificativa: preserva trilho event-driven oficial para encerramento de run.
- Force Defeat
  - Entry point: `IGameRunEndRequestService.RequestEnd(GameRunOutcome.Defeat, reason)`
  - Implementação canônica: `GameRunEndRequestService` (publica `GameRunEndRequestedEvent`)
  - Evidência: mesmo pipeline de Victory, com outcome terminal distinto.
  - Justificativa: reutiliza a mesma entrada canônica oficial de fim de run.

## 3. Arquitetura do Mock Harness criada
- `MockHarnessConfigAsset`
  - Config explícita de enable/disable (`EnabledInPlayMode`) e superfície (`ShowOverlay`), com validação fail-fast.
- `MockHarnessInstaller`
  - Instala runtime harness apenas quando habilitado.
  - Cria root dedicado `__NewScriptsMockHarness` com `DontDestroyOnLoad`.
- `MockOverlay`
  - Superfície manual em Play Mode (OnGUI), sem `MenuItem` e sem poluir menu principal Unity.
  - Resolve dependências via DI global e dispara ações canônicas.
- `IntroStageMockController`
  - Encapsula ação de conclusão da IntroStage usando apenas `IIntroStageControlService`.
- `PostGameMockController`
  - Encapsula ações de resultado usando apenas `IGameRunEndRequestService`.
- `GlobalCompositionRoot`
  - Novo hook `InstallMockHarnessIfEnabled()` no pipeline, com zero interferência quando desabilitado.
- `NewScriptsBootstrapConfigAsset`
  - Campo opcional `MockHarnessConfig` para opt-in explícito do harness.

## 4. IntroStage mock
- Ação implementada: `Complete IntroStage`.
- Fluxo chamado:
  1. `MockOverlay` aciona `IntroStageMockController`.
  2. Controller valida contexto (`IIntroStageControlService.IsIntroStageActive` ou estado `IntroStage` no `IGameLoopService`).
  3. Controller chama `IIntroStageControlService.CompleteIntroStage(reason)`.
- Resultado: conclusão pelo gate oficial da IntroStage, sem bypass de regras de domínio.

## 5. PostGame mock
- Ações implementadas:
  - `Force Victory`
  - `Force Defeat`
- Fluxo chamado:
  1. `MockOverlay` aciona `PostGameMockController`.
  2. Controller chama `IGameRunEndRequestService.RequestEnd(outcome, reason)`.
  3. Pipeline canônico processa via `GameRunEndRequestedEvent` -> `GameRunOutcomeCommandBridge` -> `IGameRunOutcomeService` -> `GameRunEndedEvent`.
- Resultado: nenhum fluxo paralelo foi criado; somente o trilho oficial de término de run.

## 6. Como habilitar/desabilitar
- Habilitar:
  1. Criar asset `MockHarnessConfigAsset`.
  2. Referenciar no campo `mockHarnessConfig` do `NewScriptsBootstrapConfigAsset`.
  3. Marcar `EnabledInPlayMode = true`.
- Desabilitar:
  - Remover referência no `NewScriptsBootstrapConfigAsset` ou manter `EnabledInPlayMode = false`.
- Comportamento quando desabilitado:
  - `InstallMockHarnessIfEnabled()` sai imediatamente.
  - Nenhum GameObject de harness é criado.
  - Runtime principal permanece sem interferência.

## 7. Sanity checks
- Verificações estruturais:
  - Busca por trilho antigo: sem reintrodução de `DevQA`, `DebugGui` ou `Legacy` no harness novo.
  - Hook centralizado no composition root; sem espalhar condicionais de mock no runtime.
- Verificações funcionais:
  - Entrypoints usados por mock mapeados para serviços canônicos já registrados no DI global.
  - Overlay não usa `MenuItem`, mantendo menu principal da Unity limpo.
- Build:
  - Foi executado `MSBuild` em `Assembly-CSharp.csproj`.
  - Erro observado: projeto C# local estava sem os novos arquivos no snapshot da lista `Compile Include` durante a validação offline, gerando erro de namespace ausente para `Infrastructure.Testing.Mocks`.
  - Não houve erro de sintaxe no código do harness em si na inspeção estática.

## 8. Limitações e próximos passos
- Limitação atual:
  - Validação completa depende de refresh de projeto/import pelo Unity Editor no ambiente local para atualizar snapshot de compilação.
- Próximos passos naturais:
  1. Abrir o projeto no Unity para forçar reimport/regeneração de `.csproj`.
  2. Entrar em Play Mode com `EnabledInPlayMode=true` e validar:
     - `Complete IntroStage` durante `IntroStage`
     - `Force Victory`/`Force Defeat` durante `Playing`
  3. (Opcional) criar prefab/cena de suporte para posicionamento visual do overlay, mantendo o mesmo harness canônico.
