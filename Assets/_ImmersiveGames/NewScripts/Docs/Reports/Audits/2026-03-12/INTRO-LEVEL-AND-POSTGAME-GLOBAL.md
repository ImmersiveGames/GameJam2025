# Intro Level-Owned And PostGame Global

Data: 2026-03-12

## Contrato final adotado

- O level atual pode expor uma `IntroStage` opcional.
- O owner da orquestracao continua sendo `LevelStageOrchestrator`.
- O pos-run continua global e centralizado.
- `PostGame` foi formalizado com exatamente tres resultados:
  - `Victory`
  - `Defeat`
  - `Exit`
- `Restart` nao participa desse post flow.

## O que ficou level-owned

- A presenca de intro continua no `LevelDefinitionAsset`.
- O level atual pode expor um hook opcional de reacao ao `PostGame`.
- Esse hook pode complementar o pos-run com feedback visual, tutorial final ou animacao.

## O que ficou global

- A entrada e a saida do `PostGame` continuam no fluxo global.
- `GameLoopService`, `PostGameOwnershipService` e `PostGameResultService` centralizam o pos-run.
- Nao existe `PostStage` arbitrario por level no contrato atual.

## Como o hook opcional de post funciona

- O hook recebe `Victory`, `Defeat` ou `Exit`.
- Ele roda apenas como complemento do fluxo global.
- O hook nao substitui overlay, ownership, navegacao ou encerramento da run.
- `Restart` segue pelo trilho de reset e nao chama esse hook.

## Mock reaproveitado

- A GUI atual de intro foi mantida como mock de intro para levels que habilitam esse comportamento.
- O mesmo mock pode ser usado em `Editor` e `Development` para uma reacao simples ao `PostGame`.

## Confirmacao

- `IntroStage` ficou level-owned e opcional.
- `PostGame` permaneceu global.
- Os tres resultados formais do pos-run sao `Victory`, `Defeat` e `Exit`.
- `Restart` ficou fora do post flow e fora do post hook.