# Plan - Baseline 4.0 Slice 3

Subordinado a `ADR-0043`, `ADR-0044` e ao [Blueprint-Baseline-4.0-Ideal-Architecture.md](./Blueprint-Baseline-4.0-Ideal-Architecture.md).

Escopo desta fase:
- `NewScripts` only
- `Docs/Plans` only
- sem tocar em `Scripts` legado
- sem implementar ainda
- sem reabrir `Slice 1` ou `Slice 2`
- `audio contextual` explicitamente fora do slice

## 1. Resumo executivo

Objetivo do Slice 3: provar o downstream visual e de dispatch do pós-run:

`PostRunMenu -> Restart / ExitToMenu -> Navigation primary dispatch`

O slice usa o estado validado dos slices anteriores como inventario de reaproveitamento, nao como contrato final.

Regra do slice:
- `PostRunMenu` = contexto visual downstream do resultado
- `Restart` = intent downstream derivada do contexto visual
- `ExitToMenu` = intent downstream derivada do contexto visual
- `Navigation` = dispatch primario das intents de saida/reentrada

Fora de escopo:
- `Save`
- audio contextual final
- refatoracao ampla de pastas
- renomeacao massiva no codigo
- arquitetura futura fora do blueprint

## 2. Backbone do Slice 3

### Nomes canonicos congelados

- `PostRunMenu`
- `Restart`
- `ExitToMenu`
- `Navigation`

### Nomes temporarios / bridges

- `PostGameEnteredEvent`
- `PostGameExitedEvent`
- `PostGameOverlayController`
- `PostGameOwnershipService`
- `PostLevelActionsService`
- `ExitToMenuCoordinator`
- `MacroRestartCoordinator`
- `MenuQuitButtonBinder`
- `MenuPlayButtonBinder`

### Estado congelado

- `PostRunMenu` e o contexto visual downstream.
- `Restart` e `ExitToMenu` sao intents downstream.
- `Navigation` e o dispatch primario.
- `PostGameEnteredEvent` / `PostGameExitedEvent` sao bridge temporaria do contexto `PostRunMenu`.
- `Frontend/UI` principal de menu e distinto do contexto visual de `PostRunMenu`.

### Estado validado

- `PostRunMenu` ficou validado como contexto visual downstream.
- `Restart` ficou validado como intent downstream derivada do contexto visual.
- `ExitToMenu` ficou validado como intent downstream derivada do contexto visual.
- `Navigation` ficou validado como dispatch primario efetivo.
- `GameLoop` ficou validado sem ownership visual do menu/dispatch.
- `MacroRestartCoordinator` ficou validado como bridge temporaria de restart.

### Ordem runtime alvo

1. `PostRunMenu` entra como contexto visual downstream.
2. `Restart` ou `ExitToMenu` e emitido como intent downstream.
3. `Navigation` recebe o dispatch primario.
4. `SceneFlow` resolve a transicao de rota.
5. O estado visual retorna ao destino canonico sem ownership de menu no `GameLoop`.

### Ordens runtime validadas

#### Restart

1. `PostRunMenu` entra como contexto visual downstream.
2. `RestartRequested` e emitido como intent downstream.
3. `MacroRestartCoordinator` faz o bridge da reentrada.
4. `Navigation` executa o dispatch primario para gameplay.

#### ExitToMenu

1. `PostRunMenu` entra como contexto visual downstream.
2. `ExitToMenuRequested` e emitido como intent downstream.
3. `PostLevelActionsService` observa o downstream e aciona `Navigation`.
4. `Navigation` executa o dispatch primario para frontend.

### Owners por modulo

| Módulo | Papel no slice |
|---|---|
| `PostGame` | owner do contexto visual `PostRunMenu` e emissor downstream de intents |
| `Navigation` | owner do dispatch primario de `Restart` / `ExitToMenu` |
| `Frontend/UI` | emissor de intents e camada visual do menu |
| `GameLoop` | apenas bridges/coordenação legada, sem ownership visual do menu |
| `LevelFlow` | participa apenas indiretamente via restart macro, se necessário |
| `SceneFlow` | consumidor tecnico da transicao de rota |

## 3. Reuse map do estado atual

### PostRunMenu

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/PostGame/Bindings/PostGameOverlayController.cs` | Keep with reshape | deve ser lido como contexto visual downstream, nao owner do rail inteiro |
| `Modules/PostGame/PostGameOwnershipService.cs` | Keep with reshape | gate/input do contexto visual pos-run |
| `Modules/PostGame/PostGameResultService.cs` | Keep | resultado ja consolidado antes da entrada no menu |
| `Modules/PostGame/PostGameFlowEvents.cs` | Keep with reshape | eventos de entrada/saida do contexto visual |

### Restart

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/GameLoop/Interop/MacroRestartCoordinator.cs` | Bridge temporária | converte intent de restart em reset macro e reentrada controlada |
| `Modules/GameLoop/Core/GameLoopEvents.cs` | Keep | `GameResetRequestedEvent` continua sendo intent macro existente |
| `Modules/GameLoop/Input/GameLoopCommands.cs` | Keep with reshape | emissor thin de intents de restart |
| `Modules/Frontend/UI/Bindings/MenuPlayButtonBinder.cs` | Keep with reshape | pode disparar restart downstream, sem ownership de menu |

### ExitToMenu

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/GameLoop/Interop/ExitToMenuCoordinator.cs` | Bridge temporária | mistura dispatch, gate e resultado; nao deve virar owner final |
| `Modules/GameLoop/Core/GameLoopEvents.cs` | Keep | `GameExitToMenuRequestedEvent` continua sendo intent existente |
| `Modules/GameLoop/Input/GameLoopCommands.cs` | Keep with reshape | emissor thin de intents de saida |
| `Modules/Frontend/UI/Bindings/MenuQuitButtonBinder.cs` | Keep with reshape | emissor de intent downstream, nao owner do fluxo |

### Navigation primary dispatch

| Peça atual | Decisão | Observação |
|---|---|---|
| `Modules/Navigation/GameNavigationService.cs` | Keep | dispatch primario canonico |
| `Modules/Navigation/GameNavigationCatalogAsset.cs` | Keep | resolucao canonica de intents e rotas |
| `Modules/Navigation/GameNavigationIntents.cs` | Keep | intents canonicos `Restart` / `ExitToMenu` |
| `Modules/Navigation/Bootstrap/NavigationBootstrap.cs` | Keep | compose do dispatch primario |

### Conflitos que precisam virar bridge/adaptor/substituicao

| Peça | Problema | Destino esperado |
|---|---|---|
| `PostGameOverlayController` | mistura visual, gate e intents | bridge visual, sem ownership de dispatch |
| `Frontend/UI` de menu principal | pode conflitar com o contexto visual do pós-run se tratado como o mesmo owner | manter separado do `PostRunMenu` |
| `PostLevelActionsService` | acopla UI a acoes de pos-run | adapter temporario ou substituicao futura |
| `ExitToMenuCoordinator` | mistura navigation, gate e resultado | bridge temporaria, nao owner final |
| `MacroRestartCoordinator` | restart macro ainda passa por GameLoop | bridge temporaria, nao owner visual |
| `GameLoop` em logs/rotas de menu | pode parecer owner visual de `PostRunMenu` | remover semantica de ownership visual |

## 4. Hooks/eventos mínimos

Slice 3 precisa, no mínimo, destes hooks canônicos:

| Hook/evento | Papel |
|---|---|
| `PostGameEnteredEvent` | entrada observável do contexto `PostRunMenu` |
| `PostGameExitedEvent` | saída observável do contexto `PostRunMenu` |
| `GameResetRequestedEvent` | intent downstream de restart macro |
| `GameExitToMenuRequestedEvent` | intent downstream de saída para menu |
| `GameNavigationService.NavigateAsync(...)` | dispatch primario das intents canonicas |

Regra:
- nao criar novo evento se um existente ja cobre a intencao com clareza
- nao introduzir eventos de `Save`
- nao abrir o slice de audio contextual

## 5. Sequência de implementação em fases curtas

### Fase 0 - congelar o rail

- travar os nomes canonicos: `PostRunMenu`, `Restart`, `ExitToMenu`, `Navigation`
- declarar os bridges/adapters atuais que ainda seguram o downstream
- deixar explÃ­cito que `PostRunMenu` e contexto visual, nao owner do dispatch
- deixar explÃ­cito que `Restart` / `ExitToMenu` sao intents downstream
- deixar explÃ­cito que `Navigation` e o dispatch primario
- deixar explÃ­cito que `PostGameEnteredEvent` / `PostGameExitedEvent` sao bridge temporaria do contexto `PostRunMenu`
- separar no texto do plano `PostRunMenu` do menu principal de `Frontend/UI`

### Fase 1 - menu downstream

- consolidar `PostRunMenu` como contexto visual downstream do `RunResult`
- manter o overlay/presenter como consumidor de resultado, nao owner de fluxo
- preservar o smoke atual de Victory / Defeat / Exit

### Fase 2 - restart downstream

- consolidar `Restart` como intent derivada do contexto visual
- manter `MacroRestartCoordinator` como bridge temporaria
- evitar que `GameLoop` pareca owner da tela de menu

### Fase 3 - exit to menu downstream

- consolidar `ExitToMenu` como intent derivada do contexto visual
- manter `ExitToMenuCoordinator` como bridge temporaria
- garantir que `Navigation` faca o dispatch primario da saida

### Fase 4 - validação do rail

- conectar logs de `PostRunMenu`, `Restart`, `ExitToMenu` e `Navigation`
- validar que `GameLoop` nao absorveu ownership visual do menu
- validar que `Navigation` permaneceu como dispatch primario

### Fase 5 - fechamento documental

- marcar as fases anteriores como concluídas com base no runtime validado
- registrar os owners finais do Slice 3
- manter os follow-ups sem bloqueio documentados

## 6. Observações não bloqueantes / pendências

- No runtime validado, `ExitToMenu` apareceu observavelmente via `PostLevelActionsService -> Navigation`.
- `ExitToMenuCoordinator` permaneceu registrado como bridge temporaria, mas nao ficou comprovado como ponte principal exercida no fluxo validado.
- Isso fica como pendencia arquitetural para clarificar a ponte canonica de `ExitToMenu`, sem reabrir a fase atual como blocker.

## 7. Critérios de aceite do slice

O Slice 3 só é aceito se:

- `PostRunMenu` ficar claramente como contexto visual downstream
- `Restart` e `ExitToMenu` aparecerem como intents downstream
- `Navigation` for o dispatch primario efetivo
- `GameLoop` nao carregar ownership visual do menu
- o runtime nao reabrir `Playing` / `ExitStage` / `RunResult` como parte do slice
- `audio contextual` continuar explicitamente fora do escopo
- o smoke atual continuar funcionando
- nenhuma pasta de legado seja tocada
- nenhuma renomeacao massiva seja necessaria para provar o slice
