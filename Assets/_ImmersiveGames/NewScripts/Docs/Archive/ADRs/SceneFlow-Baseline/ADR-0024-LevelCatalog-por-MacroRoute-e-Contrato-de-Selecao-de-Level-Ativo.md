> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0024 — LevelCollection por MacroRoute e Contrato de Seleção de Level Ativo

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): **2026-02-19**
- Última atualização: **2026-03-25**

## Contexto

Com `SceneRoute` restrita ao domínio macro e `LevelFlow` responsável pela semântica local, o sistema precisava de uma fonte única para os levels válidos de cada macro route de gameplay.

## Decisão canônica atual

### 1) A fonte única de levels em gameplay é `SceneRouteDefinitionAsset.LevelCollection`

Cada macro route de gameplay válida deve carregar sua `LevelCollection`.

### 2) A coleção é ordenada e define o default

No shape atual:
- a coleção é ordenada;
- o default operacional continua sendo o primeiro item válido da coleção quando não houver seleção explícita anterior.

### 3) Macro route sem `LevelCollection` em gameplay é inválida

Gameplay exige:
- `requiresWorldReset=true`;
- `LevelCollection` válida.

Frontend exige:
- `requiresWorldReset=false`;
- `LevelCollection=null`.

### 4) Route sem level ativo pode limpar em vez de preparar

Quando a macro route não carrega domínio de level jogável, o prepare service executa `clear` idempotente em vez de preparar conteúdo inexistente.

## Consequências

### Positivas
- elimina a necessidade de catálogo paralelo solto para o trilho canônico de gameplay;
- deixa claro quais levels pertencem a cada domínio macro;
- reduz a chance de seleção inválida cruzando rotas incompatíveis.

### Trade-offs
- exige manutenção cuidadosa de `LevelCollection` nas rotas de gameplay.

## Relação com outros ADRs

- `ADR-0020`: route macro vs semântica local.
- `ADR-0022`: assinatura local não é a assinatura macro.
- `ADR-0025`: prepare/clear ocorre no gate antes do `FadeOut`.
- `ADR-0026`: swap local usa a `LevelCollection` da macro atual.
