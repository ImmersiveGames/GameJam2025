# STYLEID-PROFILEID-DESTRUCTURALIZE

## Data

- 2026-03-11

## Objetivo

Remover `StyleId` e `ProfileId` do papel estrutural remanescente na stack Navigation/Transition, mantendo o runtime 100% direct-ref + fail-fast.

## O que mudou

- `TransitionStyleAsset` ficou apenas com `profileRef + useFade` como contrato estrutural.
- `GameNavigationCatalogAsset` e `GameNavigationService` deixaram de propagar `StyleId`.
- `SceneTransitionRequest`, `SceneTransitionContext` e `SceneTransitionService` passaram a carregar:
  - `TransitionStyleAsset` direto
  - `SceneTransitionProfile` direto
  - labels descritivos (`style`, `profile`) apenas para log/assinatura
- `RouteKind` passou a ser o sinal canonico para comportamento em:
  - `SceneFlowInputModeBridge`
  - `GameReadinessService`
  - `LevelStageOrchestrator`
- `IntroStage` deixou de depender de `ProfileId` estrutural; passou a receber apenas `ProfileLabel` para observabilidade.

## O que deixou de ser estrutural

- `StyleId`
- `ProfileId`
- comparacoes por string/id para escolher comportamento de runtime

## O que permaneceu

- referencias diretas a `TransitionStyleAsset`
- referencias diretas a `SceneTransitionProfile`
- `RouteKind` como classificacao canonica derivada da rota
- labels descritivos de asset para log, assinatura e QA

## Riscos / pendencias

- `TransitionStyleId.cs` e `SceneFlowProfileId.cs` ainda existem como tipos/compatibilidade residual, mas nao participam mais da resolucao estrutural da stack auditada.
- `SceneFlowProfilePaths` ainda usa `SceneFlowProfileId`; ficou fora desta fase por nao dirigir o runtime de Navigation/Transition.
- Recomendado validar no Unity a remocao dos drawers antigos e a geracao de novos `.meta` para o utilitario compartilhado.
