# Baseline v3 Blockers Fix

## Blockers atacados

- `Menu -> Gameplay` no entrypoint canônico de produção.
- Fonte real de `Victory / Defeat` para fechar a run no baseline atual.

## Correção do menu

- O prefab canônico [Menus.prefab](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/Prefabs/UI/Menus.prefab) já continha `Play`, `Quit` e os binders oficiais.
- O problema estava nos `Button.OnClick()` persistentes ainda serializados com namespace legado `_ImmersiveGames.NewScripts.UI.*`.
- Os callbacks foram atualizados para os tipos reais atuais em `Modules.Navigation.Bindings`, preservando a UI local de painéis e o entrypoint oficial via `MenuPlayButtonBinder -> ILevelFlowRuntimeService.StartGameplayDefaultAsync(...)`.

## Fonte real de Victory

- Foi adotado um trigger de produção mínimo por level canônico.
- Novo componente: `GameplayOutcomeTrigger`.
- `SceneTest.unity` e `SceneTest2.unity` agora expõem um `GoalTrigger` real com `Collider.isTrigger=true`, configurado para publicar `Victory` com reason `Gameplay/ReachGoal`.

## Fonte real de Defeat

- A `GameplayScene` canônica agora habilita o timeout real de produção no `GameplayEndConditionsController`.
- Configuração adotada:
  - `enableTimeout = true`
  - `timeoutSeconds = 60`
  - `timeoutOutcome = Defeat`
  - `timeoutReason = Gameplay/Timeout`
- Os triggers dev continuam disponíveis apenas como apoio secundário.

## Arquivos modificados

- `Modules/GameLoop/Bindings/EndConditions/GameplayOutcomeTrigger.cs`
- `Modules/GameLoop/Bindings/EndConditions/GameplayOutcomeTrigger.cs.meta`
- `../Prefabs/UI/Menus.prefab`
- `../Scenes/GameplayScene.unity`
- `../Scenes/SceneTest.unity`
- `../Scenes/SceneTest2.unity`
- `Docs/Reports/Audits/2026-03-13/BASELINE-V3-BLOCKERS-FIX.md`
- `Docs/Reports/Audits/2026-03-13/BASELINE-V3-BLOCKERS-FIX.md.meta`
- `Docs/Reports/Audits/2026-03-13.meta`

## QA executado

- Validação estática de wiring do menu:
  - `Play_Btn` segue no prefab canônico com `MenuPlayButtonBinder`.
  - `Quit_Btn` segue no prefab canônico com `MenuQuitButtonBinder`.
  - Botões de painéis locais continuam usando `FrontendShowPanelButtonBinder`.
- Validação estática do fechamento de run:
  - `GameplayScene` possui `Defeat` por timeout em produção.
  - `SceneTest` e `SceneTest2` possuem `GoalTrigger` real para `Victory`.
  - `PostGameOverlayController`, `RestartLevelAsync` e `ExitToMenuAsync` permanecem no trilho já consolidado.

## Limitação do QA

- Não foi possível executar smoke runtime completo dentro deste ambiente porque não houve execução do projeto no Unity Player/Editor nesta tarefa.
- O fechamento foi validado por inspeção direta do código, dos assets e do wiring serializado canônico.

## Resultado final esperado

- `Menu -> Gameplay` passa a entrar pelo trilho oficial.
- Existe uma fonte real de `Victory` no conteúdo canônico atual.
- Existe uma fonte real de `Defeat` na `GameplayScene` canônica.
- `PostGame`, `Restart` e `Exit to Menu` continuam no fluxo global já consolidado.
- O baseline atual volta a ter condição real de reavaliação para `PASS`.
