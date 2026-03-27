> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de `SceneFlow` / `LevelFlow`.
> Para leitura operacional atual, use os ADRs canônicos mais recentes de `LevelFlow`, `IntroStage` e `PostGame`.
>
> Motivo: consolidação pós-baseline para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0010 — Loading HUD + SceneFlow (NewScripts)

## Status

- Estado: **Implementado com divergências pontuais no runtime**
- Data (decisão): **2025-12-24**
- Última atualização: **2026-03-27**
- Tipo: **Implementação / contrato da apresentação de loading**
- Escopo: `SceneFlow` + `Loading HUD`

## Precedência

Este ADR continua válido **somente** para:

- ownership do loading como **apresentação visual**;
- integração do loading ao pipeline **macro** via eventos do `SceneFlow`.

Este ADR **não** define mais:

- ownership de `IntroStage`;
- ownership de `PostStage`;
- gating de entrada de level;
- gating de saída para `PostGame`;
- ordem canônica de hooks de `LevelFlow`.

Esses pontos pertencem aos ADRs canônicos mais recentes de `LevelFlow`, `IntroStage` e `PostGame`.

A ordem detalhada do gate de level antes do `FadeOut` é refinada por `ADR-0025`.

## Contexto

O `SceneFlow` executa operações macro que podem causar “pop” visual. O projeto precisava de uma HUD de loading integrada à transição, sem transformar o loading em owner da semântica do fluxo.

No runtime atual:

- `LoadingHudOrchestrator` consome eventos do `SceneFlow`;
- `LoadingHudService` garante `LoadingHudScene` + `LoadingHudController`;
- `LoadingProgressOrchestrator` traduz progresso/eventos em `LoadingProgressSnapshot`;
- a decisão da transição continua no pipeline macro (`SceneFlow + ResetInterop/WorldReset + LevelFlow`).

## Decisão canônica atual

### 1) Loading é apresentação, não owner do fluxo

Ownership atual:

- owner do fluxo macro: `SceneTransitionService`, `ResetInterop/WorldReset`, `LevelFlow`;
- owner da apresentação: `ILoadingPresentationService`, `ILoadingHudService`, `LoadingHudService`, `LoadingHudOrchestrator`.

A HUD de loading não decide:

- rota;
- reset macro;
- `LevelPrepare` / `LevelClear`;
- swap local;
- entrada em `Playing`;
- `IntroStage`;
- `PostStage`;
- finalização de gameplay.

### 2) Ordem operacional atual

#### Com fade

1. transição macro inicia;
2. o fade cobre a tela;
3. a HUD é garantida e exibida após `FadeInCompleted` ou, no limite, em `ScenesReady`;
4. o pipeline macro roda;
5. o progresso é atualizado;
6. o completion gate conclui;
7. a HUD é escondida antes do `FadeOut`;
8. o fade revela;
9. a transição completa.

#### Sem fade

1. transição macro inicia;
2. a HUD pode aparecer já no `Started`;
3. o pipeline macro roda;
4. o progresso evolui;
5. a HUD some no final real da transição.

### 3) Progress continua desacoplado da semântica

`LoadingProgressOrchestrator` publica progresso visual usando:

- progresso de composição macro (`SceneFlowRouteLoadingProgressEvent`);
- marcos de reset / prepare / finalização.

Isso não redefine:

- identidade canônica de level;
- assinatura macro;
- hooks de entrada de `IntroStage`;
- hooks de saída de `PostStage`.

## Divergências do runtime atual que precisam ficar explícitas

### A) Política strict não está implementada de forma uniforme

O runtime atual não trata todos os erros de HUD como hard-fail em strict.

Hoje:

- ausência de `LoadingHudScene` no build chama `FailStrict(...)`;
- a maior parte das outras falhas (`controller_missing`, `root_invalid`, `load_op_null`, exceções de controller) cai em `FailOrDegrade(...)` com log / disable / degraded, sem exceção obrigatória.

Portanto, o comportamento atual é **híbrido**, não “strict completo”.

### B) Cleanup em erro não é garantido por evento dedicado de falha

O orquestrador esconde a HUD em:

- `SceneTransitionBeforeFadeOutEvent`;
- `SceneTransitionCompletedEvent`.

O `SceneTransitionService` atual não publica um evento canônico de falha / cancelamento. Então, se a transição falhar após a HUD aparecer e antes desses eventos, o contrato de “hide forçado em erro” não está totalmente fechado no runtime.

## Consequências

### Positivas

- a HUD de loading permanece no papel correto: apresentação visual;
- o pipeline macro continua centralizado em `SceneFlow` + reset + level prepare;
- o progresso visual pode evoluir sem contaminar a semântica de rota / level / postgame.

### Trade-offs

- a política de falha / cleanup ainda precisa de hardening para ficar 100% coerente com a intenção documental;
- a ausência de evento explícito de failure / abort deixa uma borda operacional aberta;
- leitura cruzada com ADRs mais novos pode causar ambiguidade se este documento for usado fora do escopo de loading macro.

## Relação com outros ADRs

- `ADR-0009`: fade continua envolvendo o pipeline macro.
- `ADR-0018`: policy de resiliência do fade / style.
- `ADR-0025`: `LevelPrepare` / `LevelClear` antes do `FadeOut`.
- `ADR-0026`: swap local não usa esse trilho macro.
- ADR canônico de `IntroStage`: define ownership e hooks de entrada de level.
- ADR canônico de `PostStage`: define ownership e hooks de saída para `PostGame`.

## Resumo operacional

Use este ADR para entender **o papel do loading dentro do SceneFlow macro**:

- HUD = apresentação;
- pipeline = `SceneFlow` + reset + `LevelPrepare`.

**Não use este ADR para decidir:**

- `IntroStage`;
- `PostStage`;
- handoff para `Playing`;
- handoff para `PostGame`;
- ownership de hooks locais de level.

Esses pontos pertencem aos ADRs canônicos mais recentes.

Não assuma também que todo erro de HUD já está resolvido como hard-fail em strict ou cleanup garantido em qualquer erro intermediário; o runtime atual ainda é híbrido nesses pontos.
