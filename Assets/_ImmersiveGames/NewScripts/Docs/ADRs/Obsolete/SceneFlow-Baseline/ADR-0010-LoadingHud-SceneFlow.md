> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0010 — Loading HUD + SceneFlow (NewScripts)

## Status

- Estado: **Implementado com divergências pontuais no runtime**
- Data (decisão): **2025-12-24**
- Última atualização: **2026-03-25**
- Tipo: **Implementação / contrato da apresentação de loading**
- Escopo: `SceneFlow` + `Loading HUD`

## Precedência

Este ADR continua válido para:
- ownership do loading como **apresentação**;
- integração do loading ao pipeline macro via eventos do `SceneFlow`.

A ordem detalhada do gate de level antes do `FadeOut` é refinada por `ADR-0025`.

## Contexto

O SceneFlow executa operações macro que podem causar “pop” visual. O projeto precisava de uma HUD de loading integrada à transição, sem transformar o loading em owner da semântica do fluxo.

No runtime atual:
- `LoadingHudOrchestrator` consome eventos do `SceneFlow`;
- `LoadingHudService` garante `LoadingHudScene` + `LoadingHudController`;
- `LoadingProgressOrchestrator` traduz progresso/eventos em `LoadingProgressSnapshot`;
- a decisão da transição continua no pipeline macro (`SceneFlow + ResetInterop/WorldReset + LevelFlow`).

## Decisão canônica atual

### 1) Loading é apresentação, não owner do fluxo

Ownership atual:
- owner do fluxo: `SceneTransitionService`, `ResetInterop/WorldReset`, `LevelFlow`;
- owner da apresentação: `ILoadingPresentationService`, `ILoadingHudService`, `LoadingHudService`, `LoadingHudOrchestrator`.

A HUD não decide:
- rota;
- reset macro;
- `LevelPrepare/Clear`;
- swap local;
- finalização de gameplay.

### 2) Ordem operacional atual

#### Com fade
1. transição macro inicia;
2. o fade cobre a tela;
3. a HUD é garantida e exibida após `FadeInCompleted` ou, no limite, em `ScenesReady`;
4. o pipeline macro roda;
5. o progress é atualizado;
6. o completion gate conclui;
7. a HUD é escondida antes do `FadeOut`;
8. o fade revela;
9. a transição completa.

#### Sem fade
1. transição macro inicia;
2. a HUD pode aparecer já no `Started`;
3. o pipeline macro roda;
4. o progress evolui;
5. a HUD some no final real da transição.

### 3) Progress continua desacoplado da semântica

`LoadingProgressOrchestrator` publica progresso visual usando:
- progresso de composição macro (`SceneFlowRouteLoadingProgressEvent`);
- marcos de reset/prepare/finalização.

Isso não redefine a identidade canônica de level nem a assinatura macro.

## Divergências do runtime atual que precisam ficar explícitas

### A) Política strict não está implementada de forma uniforme

O runtime atual não trata todos os erros de HUD como hard-fail em strict.

Hoje:
- ausência de `LoadingHudScene` no build chama `FailStrict(...)`;
- a maior parte das outras falhas (`controller_missing`, `root_invalid`, `load_op_null`, exceções de controller) cai em `FailOrDegrade(...)` com log/disable/degraded, sem exceção obrigatória.

Portanto, o comportamento atual é **híbrido**, não “strict completo”.

### B) Cleanup em erro não é garantido por evento dedicado de falha

O orquestrador esconde a HUD em:
- `SceneTransitionBeforeFadeOutEvent`;
- `SceneTransitionCompletedEvent`.

O `SceneTransitionService` atual não publica um evento canônico de falha/cancelamento. Então, se a transição falhar após a HUD aparecer e antes desses eventos, o contrato de “hide forçado em erro” não está totalmente fechado no runtime.

## Consequências

### Positivas
- a HUD de loading permanece no papel correto: apresentação visual;
- o pipeline macro continua centralizado no SceneFlow e nos gates/reset/level;
- o progress visual pode evoluir sem contaminar a semântica de rota/level.

### Trade-offs
- a política de falha/cleanup ainda precisa de hardening para ficar 100% coerente com a intenção documental original;
- a ausência de evento explícito de failure/abort deixa uma borda operacional aberta.

## Relação com outros ADRs

- `ADR-0009`: fade continua envolvendo o pipeline macro.
- `ADR-0018`: policy de resiliência do fade/style.
- `ADR-0025`: `LevelPrepare/Clear` antes do `FadeOut`.
- `ADR-0026`: swap local não usa esse trilho macro.

## Resumo operacional

Use este ADR para entender **o papel do loading dentro do SceneFlow atual**:
- HUD = apresentação;
- pipeline = SceneFlow + reset + level prepare.

Não use este ADR para assumir que todo erro de HUD já está resolvido como hard-fail em strict ou cleanup garantido em qualquer erro intermediário; o runtime atual ainda é híbrido nesses pontos.
