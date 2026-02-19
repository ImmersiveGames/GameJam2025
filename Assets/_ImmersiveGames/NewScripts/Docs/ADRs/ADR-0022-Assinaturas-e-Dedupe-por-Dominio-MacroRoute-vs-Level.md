# ADR-0022 — Assinaturas e Dedupe por Domínio: MacroRoute vs Level

## Status

- Estado: Proposto
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-19
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, Navigation, LevelFlow, WorldLifecycle)


## Contexto

Hoje o sistema usa uma *signature* única para dedupe/telemetria do SceneFlow (ex.: `r:<route>|s:<style>|p:<profile>|...`), e *Level* foi historicamente tratado como “alias” de rota.  
Com a separação MacroRoutes × Levels (ADR-0020/Plano MacroRoutes vs Levels), precisamos **evitar colisões conceituais**:

- Transições de **rota macro** (Menu → Gameplay, Gameplay → Menu etc.) são “mudanças de espaço macro”.
- Trocas de **level** (Level 1 → Level 2 dentro de Gameplay) são “mudanças locais” e podem ocorrer **sem cortina**.
- Ambos possuem dedupe, observabilidade, contexto e *reason* — porém com semânticas diferentes.

Se usarmos uma única signature (ou uma única janela de dedupe) para tudo, geramos falsos positivos:
- dedupe de *level swap* bloqueando transição macro;
- ou transição macro mascarando evidência de troca de level.

## Decisão

Definir **dois domínios explícitos** de assinatura/telemetria/dedupe:

1) **MacroSignature (SceneFlow / Route)**
- Identifica a transição macro do SceneFlow.
- Fonte: `routeId`, `styleId`, `profile`, `profileAsset`, `activeScene`, `scenesToLoad/unload`, `useFade`.
- Usada por: `SceneTransitionService` (Started/ScenesReady/Completed), gates de scene transition, loading macro.

2) **LevelSignature (LevelFlow / Level)**
- Identifica a transição local de *level* dentro de uma MacroRoute.
- Fonte: `macroRouteId` (ou RouteKind), `levelId`, `contentId`, `levelChainStep` (A/B/Index), e `reason`.
- Usada por: `LevelFlowRuntimeService` / `LevelLocalContentLoader` / ContentSwap (in-place), intro/post-level (se existir).

### Regras de dedupe

- **Dedupe Macro**: permanece no `SceneTransitionService` (janela curta contra chamadas repetidas).
- **Dedupe Level**: novo dedupe no domínio do LevelFlow (*se necessário*), jamais bloqueando Macro.
- Um evento “LevelSwapRequested” não invalida/consome o budget de dedupe macro.

### Regras de observabilidade (logs)

Ancorar logs [OBS] em domínios distintos:

- Macro:
  - `[OBS][SceneFlow] TransitionStarted ... signature='<macroSignature>'`
  - `[OBS][SceneFlow] RouteResolvedVia=AssetRef ...`
- Level:
  - `[OBS][LevelFlow] LevelSelected ... levelSignature='<levelSignature>'`
  - `[OBS][Level] ContentApplied ... contentId='...'`

## Implicações

### Positivas
- Elimina ambiguidade (Level ≠ Route).
- Dedupe passa a ser determinístico e “correto por domínio”.
- Evidências (Baseline 3.0) ficam robustas: não dependem de coincidência de assinatura.

### Negativas / custos
- Ajuste incremental em telemetria e estruturas internas (propagação de signatures).
- Mais um conceito para devs (macro vs level), mitigado por documentação e nomes explícitos.

## Alternativas consideradas

1) **Manter signature única e “anexar” levelId no final**  
Rejeitado: mistura domínios e dificulta dedupe separado; risco de regressões e de “signature explosiva”.

2) **Level como “Route” de segunda classe**  
Rejeitado: reintroduz confusão. Nosso objetivo é **separar**.

## Critérios de aceite (DoD)

- Logs de transição macro permanecem inalterados semanticamente e continuam emitindo MacroSignature.
- Logs de LevelFlow passam a emitir LevelSignature em pontos-chave:
  - seleção do level;
  - aplicação do conteúdo;
  - (opcional) troca de level A→B.
- Dedupe macro não bloqueia operações de level (e vice-versa).
- Evidência em Baseline 3.0:
  - “Macro: Menu→Gameplay” com MacroSignature.
  - “Level: N→1 A/B” com LevelSignature (ou equivalente).

## Referências

- ADR-0020 — Level/Content/Progression vs SceneRoute
- ADR-0021 — Baseline 3.0 (Completeness)
- Plan-Incremental-Baseline-3.0-MacroRoutes-Levels.md
