> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0009 — Fade + SceneFlow (NewScripts)

## Status

- Estado: **Implementado com sobrescritas posteriores**
- Data (decisão): **2025-12-24**
- Última atualização: **2026-03-25**
- Tipo: **Implementação / contrato base de envelope macro**
- Escopo: `SceneFlow` + `Fade`

## Precedência

Este ADR continua válido para:
- ownership do fade dentro do trilho macro de `SceneFlow`;
- ordem macro do envelope visual (`FadeIn -> scene ops -> ScenesReady -> gate -> FadeOut -> Completed`).

Este ADR **não é mais a fonte primária** para:
- política de resiliência/falha do fade e de `TransitionStyleAsset`;
- refinamentos do gate de level antes do `FadeOut`.

Nesses pontos, prevalecem:
- `ADR-0018` para política de resiliência do fade/style;
- `ADR-0025` para a etapa de `LevelPrepare/Clear` no gate macro.

## Contexto

O projeto precisava de um envelope visual determinístico para transições macro de cena sem depender de wiring legado.

No runtime atual:
- `SceneTransitionService` é o owner da timeline de transição;
- `SceneFlowFadeAdapter` traduz `SceneTransitionProfile` em `FadeConfig`;
- `FadeService` garante `FadeScene` + `FadeController` e não cria UI “em voo” fora desse contrato.

## Decisão canônica que permanece válida

### 1) Fade continua pertencendo ao SceneFlow macro

O fade continua sendo parte do **envelope da transição macro**. Ele não decide rota, reset ou stage de level; apenas cobre visualmente a transição.

Ownership atual:
- `SceneTransitionService`: ordenação da timeline;
- `SceneFlowFadeAdapter`: aplicação do profile/configuração do fade;
- `FadeService`: infraestrutura da `FadeScene` e execução do fade.

### 2) Ordem macro do envelope

Quando `UseFade=true`, a ordem canônica continua sendo:

1. `SceneTransitionStarted`
2. `FadeIn`
3. operações de cena (`load/unload/setActive`)
4. `ScenesReady`
5. completion gate
6. `BeforeFadeOut`
7. `FadeOut`
8. `SceneTransitionCompleted`

### 3) O fade não é owner de semântica de fluxo

O fade não define:
- `RouteKind`;
- policy de reset;
- `LevelPrepare/Clear`;
- swap local;
- semântica de intro/post.

Essas responsabilidades pertencem a `SceneRoute`, `ResetInterop/WorldReset` e `LevelFlow`.

## Runtime atual observado

### O que está alinhado

- `SceneTransitionService` executa `FadeIn` antes das scene ops.
- `ScenesReady` ocorre antes do completion gate terminar.
- `BeforeFadeOut` e `FadeOut` ocorrem depois do completion gate.
- `Completed` ocorre ao final do envelope.

### O que foi sobrescrito ou refinado depois

- A política de resiliência do fade não deve mais ser lida por este ADR; ela está consolidada em `ADR-0018`.
- A presença da etapa de `LevelPrepare/Clear` entre `ScenesReady` e `BeforeFadeOut` foi formalizada depois em `ADR-0025`.

## Consequências

### Positivas
- O SceneFlow preserva um envelope visual único para transições macro.
- O fade permanece desacoplado da semântica de level e reset.
- O runtime continua auditável por eventos/âncoras de transição.

### Trade-offs
- O contrato de envelope fica distribuído entre este ADR, `ADR-0018` e `ADR-0025`.
- Ler este ADR isoladamente não é mais suficiente para inferir a policy completa do fade.

## Relação com outros ADRs

- `ADR-0010`: HUD de loading como camada de apresentação sobre o pipeline macro.
- `ADR-0018`: prevalece na policy de resiliência do fade/style.
- `ADR-0019`: direct-ref + fail-fast para route/style.
- `ADR-0025`: gate macro inclui etapa de level antes do `FadeOut`.

## Resumo operacional

Use este ADR para entender **onde o fade entra** na transição macro.

Não use este ADR como fonte única para decidir:
- se erro de fade deve derrubar ou degradar;
- se `TransitionStyleAsset` inválido é soft-fail;
- a ordem detalhada do loading/level gate.
