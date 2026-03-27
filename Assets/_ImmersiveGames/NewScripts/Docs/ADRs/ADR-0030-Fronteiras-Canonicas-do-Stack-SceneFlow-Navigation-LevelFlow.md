# ADR-0030 — Fronteiras Canônicas do Stack SceneFlow / Navigation / LevelFlow

## Status

- Estado: **Aceito (baseline canônica pós-0027)**
- Data (decisão): **2026-03-25**
- Tipo: **Consolidação canônica**
- Supersede: `ADR-0017`, `ADR-0019`, `ADR-0020` e, como leitura operacional primária, a baseline `ADR-0009` a `ADR-0027` do eixo SceneFlow/LevelFlow

## Contexto

Até a baseline `0027`, o entendimento do módulo exigia leitura cruzada de múltiplos ADRs menores, cada um cobrindo uma parte da separação entre rota macro, semântica local, loading, fade e reset.

Isso aumentou o custo de leitura e passou a reintroduzir ambiguidades já resolvidas no runtime atual.

## Decisão

As fronteiras canônicas do stack passam a ser:

### 1) `Navigation` decide intenção e referencia assets estruturais

`Navigation` resolve a intenção do usuário/sistema em:
- `routeRef`
- `transitionStyleRef`

`Navigation` não executa a transição e não vira owner de pipeline macro.

### 2) `SceneFlow` é owner da transição macro

`SceneFlow` é responsável por:
- timeline da transição;
- publicação de eventos de transição;
- sequencing macro;
- composição macro de cenas;
- correlação com gates macro;
- momento de `set-active`.

`SceneFlow` não é owner da semântica local de level.

### 3) `SceneRoute` é owner da composição macro

`SceneRouteDefinition`/`SceneRouteCatalog` definem:
- cenas a carregar/descarregar;
- cena ativa alvo;
- `RouteKind`;
- policy macro como `requiresWorldReset`;
- `LevelCollection` válida para gameplay.

`SceneRoute` não define identidade semântica do conteúdo jogável.

### 4) `LevelFlow` é owner da identidade local e da progressão

`LevelFlow` é responsável por:
- level ativo;
- snapshot semântico local;
- progressão/local swap;
- intro opcional level-owned;
- preparo/clear local correlacionado à route atual.

O hook canônico pós-aplicação/ativo do level é `LevelEnteredEvent`; ele nasce depois do level aplicado e ativo e é o seam oficial para disparar IntroStage level-owned.
`GameLoop` não decide o timing da intro nem funciona como gate canônico dela; no máximo reflete o estado alto nível depois.

### 5) `WorldReset` / `ResetInterop` continuam no eixo macro de reset

- `WorldReset` executa o reset de mundo;
- `ResetInterop` faz ponte/correlação/gate com o pipeline macro;
- esse eixo não absorve a semântica de seleção de level.

### 6) `Loading` e `Fade` são serviços consumidos pelo trilho macro

- `Loading` é owner da apresentação visual de loading;
- `Fade` é owner da cobertura/revelação visual;
- nenhum dos dois redefine ownership de rota, reset ou level.

## Regras práticas

- `startup` pertence ao bootstrap e não passa por `Navigation`.
- `frontend` e `gameplay` pertencem ao domínio de `SceneRouteKind`.
- route macro e identidade local são domínios distintos.
- swap local não sobe para trilho macro.
- loading e fade não viram owner de fluxo.

## Consequências

### Positivas
- reduz ambiguidade de ownership;
- diminui leitura obrigatória para entender o stack;
- protege o runtime contra regressões de semântica entre macro e local.

### Trade-offs
- ADRs antigos do eixo deixam de ser leitura primária;
- detalhes históricos permanecem em `Obsolete/SceneFlow-Baseline`.

## Leitura mínima

Para entender o stack atual, este ADR deve ser lido junto com:
- `ADR-0031`
- `ADR-0032`
- `ADR-0033`
