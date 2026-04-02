> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0020 — Level / Content Progression vs SceneRoute

## Status

- Estado: **Implementado**
- Data (decisão): **2026-02-19**
- Última atualização: **2026-03-25**
- Escopo: `LevelFlow`, `SceneFlow`, `Navigation`, `RestartContext`, `SceneComposition`

## Papel deste ADR na cadeia atual

Este é o ADR que separa os domínios:
- **rota macro** = `SceneFlow` / `SceneRoute`
- **semântica local / conteúdo / progressão** = `LevelFlow`

Os ADRs `0022` a `0027` refinam essa decisão. Eles não a substituem; eles a detalham.

## Decisão canônica atual

### 1) `SceneRoute` continua responsável apenas pelo domínio macro

`SceneRouteDefinition`/`SceneRouteCatalog` definem:
- cenas a carregar/descarregar;
- cena ativa alvo;
- `RouteKind`;
- `requiresWorldReset`;
- `LevelCollection` válida para gameplay.

`SceneRoute` não deve carregar a identidade semântica do conteúdo jogável.

### 2) `LevelFlow` continua responsável pela identidade local e progressão

A identidade de level/conteúdo pertence ao trilho de `LevelFlow`, `RestartContext` e snapshot semântico.

Isso permite:
- múltiplos conteúdos apontando para a mesma macro route;
- restart/local swap sem mudar de rota;
- dedupe macro sem perder a noção de “conteúdo atual”.

### 3) `SceneComposition` continua sendo executor técnico

A composição local de cenas não redefine o ownership semântico. Ela executa o que `LevelFlow` decide.

## Consequências

### Positivas
- evita explosão de rotas por conteúdo;
- mantém `SceneFlow` focado em transição macro;
- permite N→1 e swap local sem distorcer a assinatura macro.

### Trade-offs
- exige snapshot/assinatura local claros;
- exige manter a separação semântica viva nos docs e no runtime.

## Relação com os ADRs posteriores

- `ADR-0022`: assina macro e level em domínios separados.
- `ADR-0023`: separa macro reset de level reset.
- `ADR-0024`: define `LevelCollection` por macro route e o contrato do level ativo.
- `ADR-0025`: coloca `LevelPrepare/Clear` no gate macro.
- `ADR-0026`: local swap sem transição macro.
- `ADR-0027`: intro opcional level-owned e pós-run global.

## Regra prática

Sempre que surgir dúvida “isso pertence à rota ou ao level?” use este critério:

- se altera a **composição macro** da navegação, pertence à route;
- se altera a **identidade/localidade/progressão** dentro da gameplay, pertence ao level.
