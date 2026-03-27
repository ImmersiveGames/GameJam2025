# ADR-0031 — Pipeline Canônico da Transição Macro

## Status

- Estado: **Aceito (baseline canônica pós-0027)**
- Data (decisão): **2026-03-25**
- Tipo: **Consolidação canônica**
- Supersede: `ADR-0009`, `ADR-0010`, `ADR-0025` como leitura operacional primária do pipeline macro

## Contexto

A timeline macro estava distribuída entre ADRs de fade, loading e gate de level. Isso tornava a ordem operacional difícil de ler e incentivava interpretações divergentes.

## Decisão

A transição macro canônica passa a ser lida como um único pipeline.

## Ordem canônica

### Com fade ativo

1. `SceneTransitionStarted`
2. `FadeIn` / cobertura visual
3. abertura da apresentação de loading
4. `load / unload / set-active`
5. `ScenesReady`
6. completion gate interno
7. `LevelPrepare/Clear` no gate macro
8. `BeforeFadeOut`
9. fechamento da apresentação de loading no fim real
10. `FadeOut` / revelação visual
11. `SceneTransitionCompleted`

### Sem fade

1. `SceneTransitionStarted`
2. abertura da apresentação de loading
3. `load / unload / set-active`
4. `ScenesReady`
5. completion gate interno
6. `LevelPrepare/Clear`
7. fechamento da apresentação de loading no fim real
8. `SceneTransitionCompleted`

## Regras obrigatórias

### 1) `set-active` permanece no trilho macro

A troca da cena ativa pertence ao `SceneFlow`, não ao `Loading` e não ao `LevelFlow`.

### 2) `LevelPrepare/Clear` acontece antes da revelação final

O domínio local deve estar pronto — ou limpo de forma idempotente — antes do término visual real da transição macro.

### 3) A apresentação de loading acompanha o pipeline, mas não o comanda

`LoadingHudService` e orquestradores refletem o estado do pipeline. Eles não decidem rota, reset ou prepare.

### 4) O fim visual não pode antecipar o fim funcional

HUD e fade só devem encerrar quando o pipeline macro estiver de fato concluído.

## Casos cobertos

Este pipeline cobre:
- `startup` visual;
- `menu -> gameplay`;
- `gameplay -> menu`;
- `restart` macro.

Não cobre:
- swap local intra-macro sem transição macro.

## Consequências

### Positivas
- uma única leitura descreve a ordem real do fluxo;
- reduz conflito entre ADR de fade e ADR de loading;
- deixa explícita a posição do gate de level.

### Trade-offs
- políticas de resiliência ainda são detalhadas em `ADR-0033`;
- detalhes históricos ficam obsoletos para leitura operacional.
