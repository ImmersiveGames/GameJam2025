# ADR-0026 — Troca de Level Intra-Macro via Swap Local sem Transição Macro

## Status

- Estado: Proposto
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-19
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, Navigation, LevelFlow, WorldLifecycle)


## Contexto

A troca entre MacroRoutes sempre tem transição (Fade + LoadingHud + gates).  
Já a troca de levels dentro do mesmo macro **não precisa**, por padrão, de cortina.

Hoje existe ContentSwap “in-place” com telemetria e hooks (ADR-0016), o que sugere reaproveitamento do mecanismo para “LevelSwap”.

Também há a necessidade de permitir que *alguns* levels optem por:
- “cortina ao entrar” (intro cinematic)
- “cortina ao sair”
- intro/post-level (stages)

Mas isso deve ser **decisão do Level**, não da MacroRoute.

## Decisão

Definir `LevelSwap` como uma operação **local** (mesmo macro) com contrato:

- `LoadLevelAsync(levelId, mode=SwapLocal, options)`
  - `mode=SwapLocal` por padrão (sem fade macro)
  - troca conteúdo do level atual por conteúdo do próximo
  - garante unload do conteúdo anterior (sempre)
  - pode (opcionalmente) usar “curtain” do próprio level:
    - `level.allowCurtainIn/out`
    - `level.hasIntroStage`

### Regras

- LevelSwap:
  - **não** chama `SceneTransitionService.TransitionAsync` (sem transição macro).
  - **não** altera macroRouteId.
  - pode adquirir gates locais (simulação/inputs) durante swap.

- ContentSwap:
  - permanece como mecanismo genérico de troca de conteúdo;
  - LevelSwap pode ser uma camada/caso de uso sobre ContentSwap.

## Implicações

- Reduz custo/tempo de troca de level.
- Mantém MacroRoute como trilho macro “fixo” durante gameplay.
- IntroStage migra para o Level (executa quando o level sinaliza).

## Alternativas consideradas

1) **Implementar LevelSwap como nova Route (micro-route)**  
Rejeitado: obriga unload/load de cenas macro e reintroduz confusão Route=Level.

2) **Sempre usar Fade macro para level**  
Rejeitado: UX pior, e impede swap rápido (ex.: fases curtas).

## Critérios de aceite (DoD)

- Existe evidência (log) de LevelSwap A→B sem `TransitionStarted` macro.
- A troca descarrega sempre o conteúdo anterior (sem leaks).
- Se `allowCurtainIn/out` estiver habilitado, o level pode produzir fade local sem afetar macro.
- Baseline 3.0: cenários N→1 (A/B/Sequence) demonstram swap local.

## Referências

- ADR-0016 — ContentSwap ↔ WorldLifecycle
- ADR-0020 — MacroRoutes vs Levels
- ADR-0021 — Baseline 3.0
