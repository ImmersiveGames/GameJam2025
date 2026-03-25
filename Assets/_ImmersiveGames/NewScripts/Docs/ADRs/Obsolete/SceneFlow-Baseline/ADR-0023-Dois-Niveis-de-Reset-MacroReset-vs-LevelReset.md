> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0023 — Dois níveis de reset: MacroReset vs LevelReset

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): **2026-02-19**
- Última atualização: **2026-03-25**

## Contexto

Depois da separação entre route macro e semântica local, o projeto precisava impedir que “reset” virasse um conceito único e ambíguo.

## Decisão canônica atual

### 1) Macro reset permanece no domínio macro

`ResetMacroAsync(...)` continua pertencendo ao eixo de:
- `SceneFlow`
- `ResetInterop`
- `WorldReset`

Ele opera sobre a entrada/saída macro de gameplay e sua correlação com a transição.

### 2) Level reset permanece no domínio local

`ResetLevelAsync(...)` continua pertencendo ao eixo de `LevelFlow` / gameplay local.

A identidade local é resolvida por `levelRef` e snapshot semântico, não por uma promoção indevida de ids antigos para o domínio macro.

### 3) ResetInterop não vira owner semântico de level

`ResetInterop` continua sendo ponte/gate/correlação para o pipeline macro. Ele não absorve a semântica de seleção/localidade do level.

## Consequências

### Positivas
- reduz ambiguidade entre restart macro e restart local;
- evita que o domínio macro carregue detalhes que pertencem ao level;
- preserva a correlação do SceneFlow com reset sem reintroduzir shape antigo.

### Trade-offs
- exige documentação clara para evitar que features novas voltem a tratar reset como um único eixo.

## Relação com outros ADRs

- `ADR-0020`: base semântica da separação.
- `ADR-0022`: identidade por domínio.
- `ADR-0025`: `LevelPrepare/Clear` entra no gate da transição macro, mas isso não mistura os domínios.
