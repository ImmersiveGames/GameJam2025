# Save

## Status documental

- `Experience/Save` e uma superficie oficial de hooks e contratos estaveis.
- `Progression` e `Checkpoint` permanecem como placeholders de integracao, nao como features finais.
- Os backends atuais sao placeholders de infra local.
- O contrato de `Save` nao redefine a fronteira com `Preferences`.
- O seam de checkpoint manual e reservado por `IManualCheckpointRequestService`, sem runtime wireado ainda.

## Hooks oficiais minimos

- `bootstrap/app ready`: o bootstrap instala o rail e tenta restaurar `ProgressionSnapshot` se existir snapshot salvo.
- `scene transition completed`: hook oficial para persistencia contextual.
- `world reset completed`: hook oficial para persistencia contextual.
- `game run ended`: hook oficial principal para persistencia de progresso.
- `manual checkpoint request`: reservado como seam futuro via `IManualCheckpointRequestService`; nao esta implementado nem consumido ainda.

## O que esta disponivel hoje

- `SaveOrchestrationService` e o rail de hooks.
- `ProgressionService` expõe estado e persistencia basica.
- `CheckpointService` expõe a mesma forma de contrato para uso futuro.
- `IManualCheckpointRequestService` reserva a superficie oficial para checkpoint manual futuro.
- `SaveInstaller` registra e inicializa o conjunto atual sem alterar a semantica do runtime.

## O que continua placeholder

- progresso de jogo real
- checkpoint de jogo real
- integracao de terceiros para persistencia
- qualquer expansao de `Save vs Preferences`

## Leitura correta

- use este modulo para integrar hooks, contratos e placeholders de persistencia
- nao use este modulo como prova de um sistema de save final
