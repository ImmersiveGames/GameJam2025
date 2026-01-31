# Reason Registry (canônico)

Este arquivo mantém um **registro prático** de `reason` canônicos usados em logs e eventos (Observability Contract).

> Nota: o antigo “Reason-Map” foi descontinuado e pode não existir no repositório.

## Regras

- `reason` é uma string curta e estável, com hierarquia por `/`.
- Evite incluir IDs dinâmicos no reason; IDs vão em campos próprios (`contentId`, `levelId`, etc) ou em `detail`.

## Convenções

- Prefixos por domínio:
  - `SceneFlow/...`
  - `WorldLifecycle/...`
  - `IntroStage/...`
  - `PostGame/...`
  - `QA/...`

## Catálogo inicial (baseline 2.x)

- `SceneFlow/ScenesReady`
- `IntroStage/UIConfirm`
- `IntroStage/NoContent`
- `PostGame/Restart`
- `PostGame/ExitToMenu`
- `QA/ContentSwap/InPlace/NoVisuals`

## Como atualizar

Quando um ADR introduzir um novo reason:
1) Registrar aqui (nome + contexto)
2) Usar no código/logs conforme `Standards/Observability-Contract.md`
3) Cobrir em evidência (log datado) quando chegar a produção
