# Validation Report: Phase Content Unload em Gameplay -> Menu

## Resumo curto

A run atual passa no checklist canonico de unload de `Phase Content` em `Gameplay -> Menu`. O boundary Base/Phase esta correto: o provider contribui o unload suplementar, o `SceneFlow` executa o unload real e o cleaner limpa o read model apos `SceneTransitionCompleted`.

## Tabela

| item | status | evidência | observação |
|---|---|---|---|
| `PhaseContentSceneTransitionUnloadSupplementProvider` registrado no boot | PASS | [`PhaseDefinitionInstaller.cs:46-47`](C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Orchestration\PhaseDefinition\Bootstrap\PhaseDefinitionInstaller.cs) registra provider e cleaner; log de boot em [`PhaseDefinitionInstaller.cs:213`](C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Orchestration\PhaseDefinition\Bootstrap\PhaseDefinitionInstaller.cs) confirma registro. | Wiring presente e canônico. |
| `PhaseContentSceneTransitionCompletionCleaner` registrado no boot | PASS | [`PhaseDefinitionInstaller.cs:46-47`](C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Orchestration\PhaseDefinition\Bootstrap\PhaseDefinitionInstaller.cs) e log de registro em [`PhaseDefinitionInstaller.cs:231`](C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Orchestration\PhaseDefinition\Bootstrap\PhaseDefinitionInstaller.cs). | Wiring presente e canônico. |
| `RunDecision/ExitToMenu` entra como rota macro `to-menu` | PASS | `Editor.log:1984-1992` mostra `ExitToMenu` delegado ao `IGameplaySessionFlowContinuityService` e `DispatchIntent -> intentId='to-menu'`; `Editor.log:1997` mostra `TransitionStarted ... routeId='to-menu'`. | Fluxo real passou pelo rail macro correto. |
| O rail consulta e contribui unload suplementar de `Phase Content` | PASS | `Editor.log:2028` mostra `PhaseContentSupplementalUnloadProvided ... activeScenes=[SceneTest2]`; `SceneTransitionService.cs:498-502` mostra a consulta/merge antes do plan. | Contribuição suplementar efetiva no caso real. |
| O unload real remove a cena local da phase | PASS | `Editor.log:2078` mostra `MacroCompositionApplied ... removedScenes=[GameplayScene,SceneTest2]`; `SceneCompositionExecutor.cs:15-23` e `:38-58` executam o unload real. | A cena local foi descarregada de verdade. |
| O cleaner limpa o read model após `SceneTransitionCompleted` | PASS | `Editor.log:2129-2131` mostra `SceneTransitionCompletedEvent` seguido de `PhaseContentClearedOnSceneTransitionCompleted ... clearedScenes=[SceneTest2]`; `PhaseDefinitionInstaller.cs:292-341` define o cleaner no completion. | Clear pós-completion observado. |
| O log final mostra `PhaseContentClearedOnSceneTransitionCompleted` | PASS | `Editor.log:2131` registra explicitamente `PhaseContentClearedOnSceneTransitionCompleted ... clearedScenes=[SceneTest2]`. | Evidência direta e suficiente. |
| O caso nao depende de hardcode de `SceneTest2` | PASS | `PhaseDefinitionInstaller.cs:269-285` usa `PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames`; `SceneTransitionService.cs:696-702` consome provider genérico. O `SceneTest2` aparece no log porque é o estado local ativo, nao hardcode. | O nome vem do read model local, nao de excecao fixa. |
| A base nao conhece `PhaseContentSceneRuntimeApplier` diretamente | PASS | `SceneTransitionService.cs:696-702` referencia apenas `SceneTransitionUnloadSupplementRegistry`; não há dependencia direta de `PhaseContentSceneRuntimeApplier`. | Boundary Base/Phase preservado. |
| A phase nao executa unload macro por conta propria | PASS | `PhaseDefinitionInstaller.cs:253-285` so fornece cenas suplementares; `PhaseDefinitionInstaller.cs:292-341` so limpa no completion. O unload real fica em `SceneCompositionExecutor.cs:15-23`. | Phase fornece/limpa; Base executa. |

## Veredito final

PASS. A run atual confirma o rail canonico de unload de `Phase Content` em `Gameplay -> Menu` sem acoplamento indevido da base ao estado local.

## Arquivo criado

- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Validation-PhaseContent-Unload-Gameplay-Menu-20260412-1118.md`
