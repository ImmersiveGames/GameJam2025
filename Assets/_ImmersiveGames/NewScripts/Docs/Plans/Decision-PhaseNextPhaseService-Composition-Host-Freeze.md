# PhaseNextPhaseService Composition Host Freeze

## Status

Frozen.

## Decisao

`IPhaseNextPhaseService` deve permanecer composto no `GameLoopInstaller`.
Essa hospedagem e a forma canonica atual para um seam que atravessa `PhaseDefinition` + `GameLoop/IntroStage`.

## Contexto

Uma regressao anterior ocorreu quando `PhaseDefinitionInstaller` tentou compor cedo demais um fluxo que dependia de `IIntroStageSessionService`.
O fatal observado foi:

`[FATAL][Config][PhaseDefinition] IIntroStageSessionService missing from global DI before next-phase entry handoff registration.`

## Racional

- `PhaseDefinitionInstaller` deve compor apenas a parte phase-side.
- `GameLoopInstaller` ja hospeda o rail de Intro e, portanto, e o primeiro ponto honesto onde `IIntroStageSessionService` e `IIntroStageLifecycleDispatchService` existem.
- `GlobalCompositionRoot` nao e um owner semantico do fluxo; ele apenas orquestra a ordem dos modulos.
- `IPhaseNextPhaseService` depende de selecao phase-side e handoff intro-side, entao seu host final precisa refletir o boundary real, nao um host neutro artificial.

## Nao fazer

- Nao mover a composicao de volta para `PhaseDefinitionInstaller`.
- Nao criar host neutro novo apenas para esconder o boundary.
- Nao reabrir `Level*` como forma de hospedagem.
- Nao transformar `GlobalCompositionRoot` em owner semantico do fluxo.

## Critério de revisao futura

Revisar esta decisao apenas se existir um novo owner canônico de `GameplaySessionFlow` com composition point proprio e boundary explicito para hospedar o seam de next-phase sem distorcer ownership.

