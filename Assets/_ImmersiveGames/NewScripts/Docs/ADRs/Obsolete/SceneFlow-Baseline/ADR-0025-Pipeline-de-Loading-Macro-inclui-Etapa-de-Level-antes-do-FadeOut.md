> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0025 — Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): **2026-02-19**
- Última atualização: **2026-03-25**

## Precedência

Este ADR refina a ordem macro descrita em `ADR-0009` e `ADR-0010`.

Quando houver conflito sobre a fase entre `ScenesReady` e `FadeOut`, prevalece este ADR.

## Contexto

O pipeline macro precisava garantir que a preparação/limpeza do domínio de level não acontecesse solta, nem depois da revelação visual.

## Decisão canônica atual

### 1) Existe uma etapa de level no gate macro

O completion gate macro inclui uma etapa de `LevelPrepare/Clear` antes do `FadeOut`.

No runtime atual isso ocorre via `MacroLevelPrepareCompletionGate`, que envelopa o gate interno e chama `ILevelMacroPrepareService.PrepareOrClearAsync(...)`.

### 2) A ordem refinada do pipeline é

1. `SceneTransitionStarted`
2. `FadeIn` (quando `UseFade=true`)
3. `load/unload/setActive`
4. `ScenesReady`
5. completion gate interno
6. `LevelPrepare/Clear`
7. `BeforeFadeOut`
8. `FadeOut`
9. `Completed`

### 3) Prepare ou clear dependem da route atual

- gameplay + `LevelCollection` válida => `prepare`
- macro sem `LevelCollection` => `clear` idempotente

## Consequências

### Positivas
- o conteúdo local fica pronto antes da revelação visual;
- o pipeline macro continua dono da ordenação;
- o loading/hud pode representar melhor o estado real da transição.

### Trade-offs
- a etapa de level deixa de ser “extra” e vira parte formal do gate macro;
- qualquer regressão nessa fase afeta a percepção de conclusão da transição.

## Relação com outros ADRs

- `ADR-0010`: loading como apresentação sobre esse pipeline.
- `ADR-0024`: `LevelCollection` da route define o domínio válido.
- `ADR-0026`: swap local fora desse trilho macro.
