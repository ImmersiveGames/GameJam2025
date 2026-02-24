# Revisão de Arquitetura — Macro Routes vs Levels (Intenção do Projeto)

Data: 2026-02-19

Este documento registra **riscos arquiteturais** e **melhorias recomendadas** a partir da intenção definida: **Routes (macro)** como navegação entre “espaços” do jogo (Menu, Gameplay, Tutorial, etc.) e **Levels (local)** como conteúdo/variação dentro de um macro, com transições potencialmente “sem cortina” (swap), mas com opção de cortina e IntroStage por level.

---

## 1) Pontos fortes da intenção (por que faz sentido)

- **Separação clara de responsabilidades**
  - **Route (macro):** composição estrutural do “modo” (cenas base, política de reset, transição padrão, HUD/loading global).
  - **Level (local):** composição de conteúdo dentro do modo (conteúdo aditivo, IntroStage, pós-level, progressão).

- **Melhora o modelo mental e reduz “aliasing”**
  - O problema original (Level virar “alias de Route”) é típico quando ambos definem “o que carregar”. Sua proposta elimina ambiguidade ao explicitar que **Level não escolhe descarregar “macro”**, apenas **substitui conteúdo local**.

---

## 2) Riscos/Problemas prováveis (e como tratar)

### 2.1) Assinatura de transição (signature) e dedupe podem bloquear Level→Level
**Sintoma que já apareceu:** dedupe por “signature repetida” em janela curta pode ignorar uma solicitação legítima (ex.: N→1 A->B no mesmo routeId).

**Risco:** Se a assinatura de SceneFlow (macro) não “enxerga” o levelId/contentId, duas mudanças de level podem parecer iguais e serem dedupadas.

**Recomendação**
- Diferenciar **Signature Macro** vs **Signature Local**:
  - **MacroSignature**: (routeId, styleId, profile, scenesToLoad/unload, etc.)
  - **LevelSignature**: (levelId, contentId, chainIndex, localAdditiveSet)
- Dedupe do SceneFlow deve aplicar-se **só** ao MacroSignature.
- Trocas de Level devem ter dedupe **separado**, e geralmente mais permissivo, ou baseado em `levelId/contentId`.

**Resultado esperado:** mudar `qa.level.nto1.a -> qa.level.nto1.b` **não pode** ser dedupado como “transição repetida” só porque o `routeId=level.1` é o mesmo.

---

### 2.2) Reset em dois níveis precisa de “contratos” explícitos (macro vs local)
Você definiu dois resets:
- **Reset Macro**: “volta ao estado inicial do macro”, potencialmente `Level=1` e conteúdo default
- **Reset Local (Level)**: “reinicia o Level atual”

**Risco:** Sem contrato claro, sistemas podem reagir ao reset errado (spawn, snapshot, intro, content swap).

**Recomendação**
- Formalizar dois eventos (ou dois “kinds”) com invariantes:
  - `WorldResetKind.MacroRouteReset`
  - `WorldResetKind.LevelLocalReset`
- Garantir que o **contexto** carregue:
  - `macroRouteId`, `levelId`, `contentId`, `reason`, `kind`, `contextSignature`
- Em especial: **IntroStage e pós-level** devem reagir **apenas** a eventos do nível local (ou ao “enter level”), não ao reset macro genérico.

---

### 2.3) Overlap entre ContentSwap e LevelSwap (você vai querer “um trilho local” canônico)
Hoje você tem um ContentSwap que já resolve “trocar conteúdo” e atualizar snapshot.

**Risco:** se ContentSwap virar “o mecanismo de level”, ele pode ganhar responsabilidades demais e começar a conflitar com progressão e com reset (vira um segundo “navegador” escondido).

**Recomendação**
- Criar um conceito explícito de **LevelLocalFlow** (mesmo que implemente internamente via ContentSwap no início):
  - API canônica: `StartLevel(levelId, reason)` / `AdvanceLevel()` / `RestartLevel()` / `ExitToMacro()`
  - Internamente, ele pode:
    - preparar unload do conteúdo anterior
    - aplicar swaps (additive content set)
    - disparar “LevelEntered/LevelReady/IntroStageCompleted”
- ContentSwap permanece como **mecanismo** (tool) e não como “orquestrador de progressão”.

---

### 2.4) “Um Level por Macro” precisa de isolamento de estado (evitar vazamento entre macros)
Você quer “só existe 1 level por vez em cada macro” — perfeito, mas isso exige delimitar onde o estado vive.

**Risco:** `LastLevelId`, snapshot de restart, `contentId`, etc. vazarem do macro anterior para o próximo macro.

**Recomendação**
- Guardar o estado de Level sob um escopo: `LevelSession` **por MacroRoute** (ou por “GameMode”).
- Ao trocar de macro:
  - invalidar `LevelSession` anterior (ou resetar) e criar uma nova para o macro atual.
- Definir “fonte única de verdade”:
  - Macro: `RouteState` (qual macro está ativo)
  - Local: `LevelState` (qual level/content está ativo dentro do macro)

---

### 2.5) Onde “entra no cálculo de loading” sem virar acoplamento cíclico
Você quer: o “Level Enter” acontecer **antes do FadeOut** do macro, e participar do Loading (HUD, gating).

**Risco:** Orquestrador de Loading ficar sabendo demais do Level, e o Level ficar sabendo demais do SceneFlow.

**Recomendação**
- Introduzir um conceito de **Loading Steps** (pipeline) no SceneFlow:
  - Step A: Macro Scenes Applied
  - Step B: Macro Reset (se necessário)
  - Step C: Level Resolve + Level Load (local content)  ✅ *novo*
  - Step D: Spawn obrigatório (WorldDefinition / spawn registry)
  - Step E: Level IntroStage (opcional)
  - Step F: FadeOut + release gameplay gate
- Cada step é um “handler” registrado (event-driven) e o orquestrador apenas coordena.
- O LevelFlow entra como implementador do Step C/E, sem o SceneFlow conhecer detalhes internos.

---

## 3) Recomendações estruturais (prioridade)

### P0 — Corrigir o “modelo de assinaturas” (evitar dedupe errado)
- Introduzir `LevelSignature` e garantir que operações de nível sejam distintas.
- Garantir logs e evidências com anchors:
  - `[OBS][LevelFlow] LevelResolvedVia=...`
  - `[OBS][LevelFlow] LevelSwapRequested ...`
  - `[OBS][LevelFlow] LevelSwapApplied ...`

### P0 — Criar contratos de eventos (macro vs local)
- Definir claramente eventos/estados e quem publica/consome:
  - `MacroTransitionStarted/Completed`
  - `LevelEnterStarted/LevelReady`
  - `IntroStageStarted/Completed` (escopo: level)
  - `ResetCompleted(kind=Macro/Level)`

### P1 — Separar “policy” de “definition”
- RouteDefinition: cenas macro, unload/load, reset policy, transition defaults.
- LevelDefinition: conteúdo local (additive set), intro/pós-level, regras de progressão local.
- Policies: resolvers e regras (ex.: “tutorial não usa reset macro” ou “tutorial usa reset local”).

### P1 — Unificar o “trilho canônico de gameplay start”
- Um único caminho oficial: **StartMacroRoute -> ResolveFirstLevel -> LoadLevel -> Spawn -> FadeOut -> (IntroStage)**.
- QA/Dev menus devem chamar esse trilho, não atalhos paralelos.

### P2 — “Níveis em cadeia” (mundo 1-1, 1-2) com composição declarativa
- Modelar chain como:
  - catálogo ordenado
  - ou `nextLevelId` por level (grafo)
- Garantir que “avançar” é uma operação local sem tocar em SceneFlow (a menos que troque de macro).

---

## 4) Checklist de qualidade (o que você deve exigir em evidências/logs)

- **Macro (Route)**
  - RouteResolved/Applied
  - Policy decision (requiresWorldReset)
  - FadeIn/AfterFadeIn/HUD
  - Reset macro (se houver)
  - FadeOut/Completed

- **Local (Level)**
  - LevelResolved (levelId, contentId)
  - Local unload/load (conteúdo anterior removido)
  - Spawn obrigatório pós-level-load (se fizer parte do contrato)
  - IntroStage only-if Level has IntroStage
  - LevelReady -> gameplay gate liberado

- **Anti-regressão**
  - Nenhum “dedupe” bloqueia A->B quando `levelId/contentId` mudam.
  - Reset Macro não dispara IntroStage se o contrato for “Intro é do level”.

---

## 5) Sugestão de próximos artefatos (sem implementar ainda)

1. **Plano incremental** (Fases) para migrar do “alias” para o trilho LocalFlow canônico.
2. **ADR por fase** (como você já planejou), começando por:
   - ADR: Separação de signatures e dedupe
   - ADR: Contratos de eventos macro vs local
   - ADR: LevelLocalFlow API (com ContentSwap como mecanismo inicial)

---

## Notas finais

A sua intenção é sólida, mas o “ponto de falha” típico é sempre o mesmo:
> *se o runtime não distingue assinaturas macro vs local, o sistema vai continuar tratando level como rota disfarçada.*

A maior melhoria arquitetural, portanto, é **formalizar a camada local** como trilho independente, com contratos e telemetria, e integrar isso no pipeline de loading do macro sem acoplamento cíclico.

