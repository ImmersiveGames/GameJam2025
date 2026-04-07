# IntroStage Structural Freeze

## Veredito

**APROVADO PARA FREEZE**

Fonte de verdade canonica: `Docs/ADRs/ADR-0059-IntroStage-Canonical-Content-Presenter-Hook.md`.

## Leitura consolidada

O trilho atual da `IntroStage` esta coerente e congelado:

- o presenter local e passivo
- o `LevelIntroStagePresenterHost` e o owner unico de descoberta, attach, ativacao visual e detach
- o `IntroStageControlService` e o owner canonico de complete/skip
- o `IntroStageCoordinator` governa o bloqueio/liberacao e o handoff para `Playing`
- o gate macro de entrada e `SceneTransitionCompleted`
- o presenter nao expõe `<pending>`
- o ciclo novo nasce com contrato completo e surface valida
- nao existe dependencia canonica de `Task` como semantica de negocio

## Itens historicos / superseded

- `LevelIntroStageMockPresenter` conserva nome historico, mas o runtime canonico nao depende de compatibilidade nem de self-register.
- `LevelSelectedRestartSnapshotBridge` foi removido do runtime e absorvido por owner canonico do restart.
- A nomenclatura `Level*` nos logs e contratos de transporte e heranca historica sem ownership semantico.
- O objeto local da cena e apenas o hook concreto resolvido pelo host.

## O que ficou fora desta leitura

- renomeacao ampla do eixo `Level*`
- migracao de `RunResultStage`
- migracao de `RunDecision`
- troca do presenter de cena por controller global
- troca do hook local por Canvas/UGUI como premissa arquitetural

## Conclusao

A `IntroStage` pode ser tratada como baseline do projeto neste ponto.
Nao restam compatibilidades funcionais no caminho canonico; o que pode sobrar e apenas nomenclatura historica fora da semantica.
