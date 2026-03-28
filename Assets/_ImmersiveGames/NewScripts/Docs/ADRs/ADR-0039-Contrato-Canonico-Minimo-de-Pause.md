# ADR-0039: Contrato Canônico Mínimo de Pause

## Status
- Aceito e validado em runtime

## Evidências canônicas
- `ADR-0037` de hooks oficiais
- `Baseline 3.5`
- logs recentes de `Pause` com:
    - `RequestPause`
    - `RequestResume`
    - `PauseWillEnterEvent`
    - `PauseWillExitEvent`
    - `PauseStateChangedEvent`
    - `InputModeRequestEvent(PauseOverlay)` na entrada
    - `InputModeRequestEvent(Gameplay)` na saída
- runtime atual de `GameLoop`, `GamePauseOverlayController`, `GameplayStateGate` e `GamePauseGateBridge`
- bridge de ducking de áudio reagindo aos hooks canônicos de pause

## Contexto
O pause existia de forma funcional, mas com ownership e seams pouco claros. O fluxo misturava estado do `GameLoop`, overlay, gates, input mode e bridges reativos, com compensações locais e pouca clareza sobre o que era contrato público e o que era detalhe interno.

Antes desta decisão, o risco principal era promover `Pause` a módulo próprio cedo demais, empacotando acoplamentos ainda imaturos em vez de estabilizar primeiro o contrato do domínio.

A necessidade desta decisão é fechar um contrato mínimo, explícito e observável, sem modularizar `Pause` prematuramente.

## Decisão
- O owner canônico do estado `Paused` continua sendo `GameLoop`.
- A superfície pública mínima do domínio passa a ser:
    - `IPauseCommands.RequestPause(string reason)`
    - `IPauseCommands.RequestResume(string reason)`
- A leitura canônica passa a ser:
    - `IPauseStateService.IsPaused`
- O hook oficial de observação do domínio passa a ser:
    - `PauseStateChangedEvent`
- Hooks precoces passam a existir apenas para consumidores que precisam reagir antes do `ENTER` final do estado:
    - `PauseWillEnterEvent`
    - `PauseWillExitEvent`
- O overlay de pause é apresentação e reação de UI, não ownership do domínio.
- O ducking de áudio reage aos hooks oficiais de pause, sem depender do overlay como seam.
- `SimulationGate` continua sendo infraestrutura de bloqueio/liberação da simulação.
- `InputMode` continua sendo infraestrutura transversal de contexto de input.
- `Navigation` continua sendo owner da saída para menu, sem assumir o estado `Paused`.
- `GamePauseCommandEvent` e `GameResumeRequestedEvent` continuam internos e não oficiais.

## Canonical Pause Ownership
O estado `Paused` pertence ao `GameLoop`.

Isso significa que:
- a decisão final de entrar ou sair de `Paused` é do `GameLoop`;
- o restante do sistema reage ao estado e aos hooks canônicos;
- nenhum controller de UI, bridge ou overlay pode se tornar owner implícito do pause.

## Canonical Public Surface
A API pública mínima do domínio é:

- `IPauseCommands.RequestPause(string reason)`
- `IPauseCommands.RequestResume(string reason)`
- `IPauseStateService.IsPaused`

Esses contratos são a superfície pública do pause.

Chamadas externas não devem depender de:
- `GamePauseCommandEvent`
- `GameResumeRequestedEvent`
- métodos internos do `GameLoopService`
- callbacks de overlay
- `OnDisable`
- hotkey handler específico

## Canonical Hooks
### Hook oficial de estado
- `PauseStateChangedEvent`

Este é o hook canônico de observação do domínio.
Consumidores que precisam saber a verdade final do estado devem reagir a este evento.

### Hooks precoces
- `PauseWillEnterEvent`
- `PauseWillExitEvent`

Esses hooks existem para consumidores que precisam reagir cedo, mas ainda dentro do contrato canônico, antes do `ENTER` final do novo estado.

Regra:
- `PauseWillEnterEvent` e `PauseWillExitEvent` não substituem `PauseStateChangedEvent`;
- `PauseStateChangedEvent` continua sendo a verdade final do estado;
- hooks precoces não podem virar superfície pública primária do domínio.

## Canonical Runtime Order
Entrada em pause:
1. chamada de `IPauseCommands.RequestPause(reason)`
2. request chega ao `GameLoop`
3. transição é aceita
4. `EXIT: Playing`
5. `PauseWillEnterEvent`
6. `ENTER: Paused`
7. `PauseStateChangedEvent(isPaused=true)`

Saída do pause:
1. chamada de `IPauseCommands.RequestResume(reason)`
2. request chega ao `GameLoop`
3. transição é aceita
4. `EXIT: Paused`
5. `PauseWillExitEvent`
6. `ENTER: Playing`
7. `PauseStateChangedEvent(isPaused=false)`

## What the Overlay Is
O overlay de pause é:
- apresentação visual;
- consumidor do estado;
- emissor de intenção via `IPauseCommands`, quando necessário.

O overlay não é:
- owner do estado `Paused`;
- owner de navegação;
- owner de input mode;
- owner de gate de gameplay.

Consequência prática:
- o overlay abre e fecha por reação ao estado;
- não deve esconder-se por writer local fora do trilho canônico;
- não deve disparar `resume` em `OnDisable()`.

## What SimulationGate and InputMode Are
### SimulationGate
`SimulationGate` continua sendo infraestrutura para bloquear/liberar a simulação durante pause e resume.

Ele não é owner do domínio, apenas reage ao hook canônico.

### InputMode
`InputMode` continua sendo infraestrutura transversal.

No pause:
- entrada deve trocar para `PauseOverlay / UI`;
- saída deve retornar para `Gameplay / Player`.

A mudança de input mode deve ser observável em log e não deve redefinir ownership do pause.

## What Navigation Is
`Navigation` continua sendo owner da navegação para menu e de seus fluxos próprios.

O pause não vira dono da navegação.
O exit-to-menu deve continuar passando pelo trilho canônico de `Navigation`.

## Internal Events That Remain Internal
Os seguintes eventos permanecem internos ao trilho atual:

- `GamePauseCommandEvent`
- `GameResumeRequestedEvent`

Eles podem continuar existindo como detalhe de implementação, mas não são parte do contrato público do domínio.

## Audio Ducking
O ducking de áudio no pause é permitido, mas não muda o ownership do domínio.

Regras:
- owner do ducking = módulo de Audio;
- trigger canônico = hooks do pause;
- `PauseWillEnterEvent` / `PauseWillExitEvent` podem ser usados para reação precoce;
- `PauseStateChangedEvent` continua sendo usado para reconciliação/idempotência;
- o áudio não deve depender de overlay, botão, hotkey ou `OnDisable`.

## What Is Still Missing Before a First-Class Pause Module
Antes de promover `Pause` a módulo próprio, ainda faltam:

- `ModuleId` próprio;
- `PauseInstaller`;
- `PauseBootstrap` ou `PauseRuntimeComposer`;
- reduzir o acoplamento residual do pause dentro do `GameLoopBootstrap` e do stack atual;
- fechar com confiança se `Pause` tem lifecycle próprio suficiente para ser módulo first-class.

## Non-Goals
- não promover `Pause` a módulo próprio neste ADR
- não mover ownership de `Paused` para fora do `GameLoop`
- não transformar overlay em owner
- não mover `InputMode` para dentro do domínio Pause
- não mover `SimulationGate` para dentro do domínio Pause
- não reabrir ownership de `Navigation`
- não resolver neste ADR a arquitetura completa do módulo de Audio

## Consequences
- o pause passa a ter contrato mínimo explícito e observável;
- o `GameLoop` permanece owner do estado;
- overlay, gate, input mode e áudio passam a reagir ao domínio de forma mais limpa;
- hooks oficiais ficam claros para consumidores novos;
- a promoção futura de `Pause` a módulo próprio fica condicionada a maturidade real, não a preferência estrutural.

## Validation Status
O contrato está validado em runtime para:

- gameplay normal;
- entrada e saída de pause;
- overlay reagindo ao estado;
- `SimulationGate` reagindo ao hook oficial;
- `InputMode` observável na entrada e na saída;
- ducking de áudio reagindo aos hooks precoces e reconciliando no hook final.

A semântica final ficou:

- `PauseWillEnterEvent` / `PauseWillExitEvent` = hooks precoces válidos
- `PauseStateChangedEvent` = verdade final do estado
- o overlay segue reativo, e não owner do pause

## References
- `ADR-0037`
- `ADR-0038`

## Checklist
- [ ] `IPauseCommands` é a superfície pública do domínio.
- [ ] `IPauseStateService.IsPaused` é a leitura canônica.
- [ ] `PauseStateChangedEvent` é o hook oficial de estado.
- [ ] `PauseWillEnterEvent` e `PauseWillExitEvent` só são usados como hooks precoces.
- [ ] O `GameLoop` continua owner do estado `Paused`.
- [ ] O overlay permanece reativo e sem ownership.
- [ ] `SimulationGate` reage ao hook canônico.
- [ ] `InputMode` entra em `PauseOverlay/UI` e sai para `Gameplay/Player`.
- [ ] `Navigation` continua owner do exit-to-menu.
- [ ] `GamePauseCommandEvent` e `GameResumeRequestedEvent` permanecem internos.
- [ ] O áudio reage por eventos, sem acoplamento ao overlay.
- [ ] `Pause` ainda não é módulo first-class até fechar lifecycle e descriptor próprios.
