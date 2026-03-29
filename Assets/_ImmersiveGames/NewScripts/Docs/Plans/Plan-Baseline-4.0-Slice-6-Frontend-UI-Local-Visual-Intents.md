# Plan - Baseline 4.0 Slice 6

Subordinado a `ADR-0043`, `ADR-0044` e ao [Blueprint-Baseline-4.0-Ideal-Architecture.md](./Blueprint-Baseline-4.0-Ideal-Architecture.md).

Escopo desta fase:
- `NewScripts` only
- `Docs/Plans` only
- sem tocar em `Scripts` legado
- sem implementar ainda
- sem reabrir `Slice 1`, `Slice 2`, `Slice 3`, `Slice 4` ou `Slice 5`
- `Save` fora de escopo

## 1. Resumo executivo

Objetivo do Slice 6: consolidar `Frontend/UI` como contexto local visual e emissor de intents, sem ownership de flow, resultado, rota, readiness ou dispatch primario downstream.

O slice parte do runtime ja validado e trata o codigo atual como inventario de reaproveitamento, nao como contrato final.

Foco operacional do slice:

`SceneFlow technical rail -> Frontend/UI local visual contexts -> derived intents`

Regra do slice:
- `Frontend/UI` = contexto local visual + emissor de intents
- `PostRunMenu` / `PauseMenu` = contextos visuais locais
- `Frontend/UI` nao e owner de run state
- `Frontend/UI` nao e owner de route
- `Frontend/UI` nao e owner de result
- `Frontend/UI` nao e owner de readiness
- `Frontend/UI` nao e dispatch primario downstream
- `GamePauseOverlayController` / `PostGameOverlayController` = apresentacao local, nao ownership de fluxo

Fora de escopo:
- `Save`
- nova arquitetura de navegacao
- nova semantica de gameplay
- reabertura de `GameLoop`, `PostGame`, `LevelFlow`, `Navigation`, `Audio` ou `SceneFlow`
- refatoracao ampla de pastas
- troca massiva de nomes

## 2. Backbone do Slice 6

### Nomes canonicos congelados

- `Frontend/UI`
- `PostRunMenu`
- `PauseMenu`
- `Intent`
- `Overlay`
- `Panel`

### Nomes temporarios / bridges

- `GamePauseOverlayController`
- `PostGameOverlayController`
- `FrontendPanelsController`
- `MenuPlayButtonBinder`
- `MenuQuitButtonBinder`
- `FrontendButtonBinderBase`
- `PostLevelActionsService`
- `GamePauseGateBridge`
- `GameExitToMenuRequestedEvent`
- `GameResetRequestedEvent`

### Runtime rail canonico

1. `SceneFlow` e os owners upstream consolidam contexto.
2. `Frontend/UI` apresenta o contexto local visual correspondente.
3. `Frontend/UI` emite intents derivadas quando o usuario age.
4. `GameLoop`, `PostGame`, `LevelFlow` e `Navigation` consomem a intent sem UI ownership.
5. Hooks upstream podem informar a UI, mas nao substituem o contexto consolidado de `PostRunMenu` / `PauseMenu`.

### Parallel rails to eliminate

- overlay visual com ownership de pause ou resultado
- menu principal competindo com `PostRunMenu`
- binder de UI acoplando regra de dominio
- intent visual virando dispatch direto sem owner downstream
- input mode mascarando ownership canonico
- overlay nao pode mascarar owner de pause
- overlay nao pode mascarar owner de post-run
- binder nao pode carregar regra de dominio
- input mode nao pode mascarar ownership visual
- intent visual nao pode virar dispatch primario

### Owners por modulo

| Modulo | Papel no slice |
|---|---|
| `Frontend/UI` | owner do contexto visual local e emissor de intents |
| `PostGame` | owner upstream do pos-run e fonte de resultado consolidado |
| `GameLoop` | owner upstream de run/pause state |
| `LevelFlow` | executor downstream de restart/exit quando aplicavel |
| `Navigation` | dispatch primario downstream quando aplicavel |
| `SceneFlow` | trilho tecnico upstream ja fechado |
| `Audio` | consumidor downstream de contexto, sem ownership visual |

## 3. Reuse map

| Peca atual | Decisao | Observacao |
|---|---|---|
| `Modules/PostGame/Bindings/PostGameOverlayController.cs` | Keep with reshape | deve permanecer visual-only e emitter de intents |
| `Modules/GameLoop/Pause/GamePauseOverlayController.cs` | Keep with reshape | overlay de pause fica na camada visual, sem ownership do estado canonico |
| `Modules/Frontend/UI/Panels/FrontendPanelsController.cs` | Keep | contexto visual local e troca de telas |
| `Modules/Frontend/UI/Bindings/MenuPlayButtonBinder.cs` | Keep with reshape | emissor de intent de start/reentrada |
| `Modules/Frontend/UI/Bindings/MenuQuitButtonBinder.cs` | Keep with reshape | emissor de intent de saida |
| `Modules/Frontend/UI/Bindings/FrontendButtonBinderBase.cs` | Keep | base de binders de intent |
| `Modules/LevelFlow/Runtime/PostLevelActionsService.cs` | Keep with reshape | executor downstream, nao owner visual |
| `Modules/GameLoop/Interop/GamePauseGateBridge.cs` | Temporary bridge | ponte de infraestrutura, nao ownership visual |

## 4. Hooks/eventos minimos

Slice 6 precisa, no minimo, destes hooks canonicos:

| Hook/evento | Papel |
|---|---|
| `PauseStateChangedEvent` | atualizar contexto visual de pause |
| `GameRunStartedEvent` | abrir/fechar overlays conforme run ativa |
| `GameRunEndedEvent` | ocultar contexto visual de pause e abrir pos-run |
| `GameExitToMenuRequestedEvent` | intent visual downstream de saida |
| `GameResetRequestedEvent` | intent visual downstream de restart |
| `InputModeRequestEvent` | request tecnico de input mode sem ownership de UI |

Regra:
- nao criar novo evento se o contract atual ja cobre o contexto visual
- nao introduzir semantica de gameplay em `Frontend/UI`
- nao usar fallback silencioso para mascarar binders ausentes

## 5. Sequencia de implementacao em fases curtas

### Fase 0 - congelar o rail

Fase 0 fechada como freeze documental do slice.

- declarar `Frontend/UI`, `PostRunMenu`, `PauseMenu`, `Overlay` e `Panel` como nomes canonicos
- marcar overlays e binders existentes como bridges/temporarios
- deixar explicito que UI nao e owner de run, route, resultado ou readiness
- deixar explicito que UI nao e dispatch primario downstream
- confirmar o caminho runtime alvo e os fora de escopo

### Fase 1 - contexto visual local

- consolidar `PostRunMenu` e `PauseMenu` como contextos locais visuais
- manter overlays apenas como apresentacao e reacao a eventos canonicos
- evitar que menu ou pause invadam ownership upstream
- nao abrir intents novas ainda
- nao integrar downstream ainda

Fase 1 fechada: `PostRunMenu` e `PauseMenu` permanecem como contextos visuais locais, sem ownership de estado, rota, resultado, readiness ou dispatch downstream.

### Fase 2 - intents derivadas

- consolidar binders como emissores de intents
- manter `Restart`, `ExitToMenu`, `Pause` e `Resume` como intents, nao estados visuais
- evitar dispatch primario dentro da camada visual

Fase 2 fechada: `Frontend/UI` emite intents derivadas explicitas, sem ownership de estado, rota, resultado, readiness ou dispatch primario downstream.

### Fase 3 - integracao downstream

- conectar intents visuais aos executores downstream ja existentes
- preservar fail-fast para configs obrigatorias
- manter `LevelFlow` e `Navigation` como consumidores, nao como UI

Follow-up nao bloqueante:
- `FrontendQuitService` funcionou corretamente como servico tecnico de quit, mas deve ser extraido futuramente para um arquivo tecnico proprio, em vez de permanecer no mesmo arquivo de `MenuQuitButtonBinder`.

### Fase 4 - validacao runtime

- conectar logs de `Frontend/UI`, `PostRunMenu`, `PauseMenu` e intents derivadas
- validar que overlays nao viraram owners de fluxo
- validar que `SceneFlow` e `GameLoop` continuam fora da camada visual

Fase 4 fechada: runtime validado com `Frontend/UI` mantendo apenas contexto visual local, emissao de intents e delegacao downstream; `Menu -> Play`, `Menu -> Quit`, `Pause -> Resume`, `PostRunMenu -> Restart` e `PostRunMenu -> ExitToMenu` observados no log, com `Quit` executado no Editor via `IFrontendQuitService`.

## 6. Critrios de aceite

O Slice 6 so e aceito se:

- `Frontend/UI` permanecer contexto local visual e emissor de intents
- `PostRunMenu` e `PauseMenu` nao virarem ownership de estado ou rota
- `GameLoop`, `PostGame`, `LevelFlow` e `Navigation` continuarem owners upstream/downstream corretos
- `SceneFlow` permanecer tecnico e fechado
- nao houver trilho paralelo de input mode ou overlay mascarando owner canonico
- nenhum fallback silencioso novo for introduzido
- os slices 1, 2, 3, 4 e 5 continuarem fechados sem reabertura

## 7. Pendencias herdadas / nao bloqueantes

- `ExitToMenuCoordinator` continua como contexto herdado fora deste slice
- `GamePauseGateBridge` permanece como ponte de infraestrutura, nao como owner visual
- `PostLevelActionsService` continua executor downstream, nao dono de UI
- qualquer ajuste fino de binders antigos continua como follow-up nao bloqueante
- `Save` nao entra neste corte
