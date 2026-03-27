# ADR-0039: Contrato Canônico Mínimo de Pause

## Status
- Estado: Aceito
- Data: 2026-03-27
- Escopo: `NewScripts` only

## Contexto

`Pause` existe hoje como comportamento distribuído entre `GameLoop`, `Gameplay`, `SimulationGate`, `InputModes` e `Navigation`.

Antes de qualquer promoção a módulo first-class, a baseline precisa separar:
- estado canônico de pause;
- comandos públicos de pause/resume;
- leitura canônica de estado;
- hook oficial de mudança de estado;
- responsabilidades de overlay, gate, input e navegação.

Este ADR não promove `Pause` a módulo próprio.
Ele apenas fecha o contrato mínimo do domínio para reduzir ambiguidade e preparar uma possível modularização futura.

## Decisão

### 1. Owner do estado `Paused`

Enquanto `Pause` permanecer fora de módulo próprio, o owner canônico do estado `Paused` continua sendo o `GameLoop`.

Regra:
- `GameLoop` é a fonte de verdade do estado macro `Paused`.
- Nenhum overlay, gate ou bridge pode substituir esse ownership.
- Outros consumidores podem refletir, bloquear ou apresentar o estado, mas não redefini-lo.

### 2. Comandos públicos canônicos

O contrato público mínimo do domínio passa a ser:

- `IPauseCommands.RequestPause(string reason)`
- `IPauseCommands.RequestResume(string reason)`

Semântica:
- `RequestPause` solicita a entrada em `Paused`.
- `RequestResume` solicita a saída de `Paused`.
- O `reason` é obrigatório semanticamente quando houver contexto operacional relevante; vazio só deve ser usado quando a origem não tiver motivo específico.

### 3. Leitura canônica

O contrato de leitura passa a ser:

- `IPauseStateService.IsPaused`

Semântica:
- `true` quando o jogo está em estado canônico de pause.
- `false` nos demais estados.
- A leitura deve derivar do owner canônico, não de inferência local do overlay ou de gate.

### 4. Hook oficial

O hook oficial do domínio passa a ser:

- `PauseStateChangedEvent`

Semântica:
- representa uma mudança observável de estado de pause;
- é o seam oficial para UI, telemetria e consumidores internos que precisam reagir ao pause sem conhecer o wiring interno;
- não é comando;
- não é substituto do estado canônico.

### 5. Papel do overlay

O overlay de pause é apresentação, não ownership.

Responsabilidades:
- mostrar/ocultar UI de pause;
- expor ações de usuário como `RequestPause`, `RequestResume` e `ReturnToMenu`;
- reagir ao estado de pause para refletir a UI correta;
- não decidir estado macro por conta própria;
- não servir como fonte de verdade de pause.

### 6. Papel do `SimulationGate`

`SimulationGate` continua sendo infraestrutura.

Responsabilidades:
- bloquear/liberar simulação enquanto o pause estiver ativo;
- usar token explícito para refletir o efeito do pause sobre gameplay;
- não ser fonte de verdade do estado `Paused`;
- não substituir o owner do estado macro.

### 7. Papel do `InputMode`

`InputMode` continua sendo infraestrutura transversal.

Responsabilidades:
- alternar entre `Gameplay`, `PauseOverlay` e `FrontendMenu`;
- refletir a intenção de input da UI;
- não definir estado de pause;
- não publicar significado de domínio.

### 8. Papel do `Navigation` no exit-to-menu

`Navigation` continua sendo owner do fluxo de saída para menu.

Responsabilidades:
- executar a navegação macro de saída;
- encerrar o contexto de pause quando a saída for efetivada;
- não assumir ownership do estado `Paused`;
- não transformar `ExitToMenu` em parte do contrato de estado de pause.

### 9. O que continua interno e não oficial

Os seguintes itens continuam internos:

- `GamePauseCommandEvent`
- `GameResumeRequestedEvent`
- `GameLoopInputCommandBridge`
- `GamePauseGateBridge`
- dedupe por frame em consumidores de pause
- hotkeys de debug/teardown do overlay
- qualquer wiring específico entre overlay e `GameLoop` que exista só para suportar a implementação atual

Esses elementos podem continuar existindo como detalhes de implementação, mas não devem ser tratados como API pública do domínio.

### 10. O que precisa mudar antes de `Pause` virar módulo próprio

Antes de promover `Pause` a módulo first-class, este contrato ainda precisa de:

- descriptor canônico próprio com `ModuleId`;
- installer próprio para registrar contratos de pause;
- runtime composer próprio para wiring operacional;
- separação explícita entre comando de pause e bridge de runtime;
- remoção do acoplamento físico de pause dentro do bootstrap de `GameLoop`;
- definição de ownership explícito do overlay fora do módulo de loop;
- revisão do vínculo de `Navigation` para sair de pause sem depender de wiring escondido.

## Consequências

- `Pause` passa a ter fronteira conceitual clara sem virar módulo ainda.
- consumidores externos e internos ganham contrato mínimo estável para pause/resume.
- a promoção a módulo só deve acontecer depois que o contrato acima estiver implementado de forma consistente no runtime.

## Non-goals

- não promover `Pause` a módulo próprio nesta decisão;
- não alterar o runtime agora;
- não mudar a policy global de input ou gate;
- não criar novos eventos além do hook oficial desta ADR.
