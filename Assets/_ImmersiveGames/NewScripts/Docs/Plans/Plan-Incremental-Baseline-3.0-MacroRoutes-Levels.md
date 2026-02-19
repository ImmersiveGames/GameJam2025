# Plano incremental — Macro Routes vs Levels (Baseline 3.0)

Data: 2026-02-19

Este plano descreve uma **migração incremental** do estado atual (onde *Level* ainda tem pontos de “alias de Route”) para o modelo-alvo:

- **Route (macro)** = navegação entre “espaços” do jogo (Menu, Gameplay, Tutorial, HUB etc.), com **fade/loading**, **políticas**, e **composição macro de cenas**.
- **Level (local)** = conteúdo dentro de um macro, com **troca local** (por default sem cortina), **IntroStage opcional por level**, **pós-level opcional**, e **reset local** distinto do reset macro.

O plano foi desenhado para ser:

- **event-driven / SOLID**
- **fail-fast** em configurações obrigatórias
- **observável** (logs [OBS] ancorados)
- **compatível** (passos com compat temporária, sem “big bang”)

> Importante: cada fase tem **invariantes + evidências**. Só avança quando a fase anterior tiver evidência canônica registrada.

---

## Visão geral de trilhos

### Trilho Macro (SceneFlow)
- Resolve intent/route macro
- Executa FadeIn
- Aplica RouteExecutionPlan (load/unload/active)
- Aplica política de reset macro (`requiresWorldReset`)
- Integra HUD/Loading
- **Antes do FadeOut** executa steps obrigatórios (inclui *enter level* quando aplicável)
- Executa FadeOut
- Publica `TransitionCompleted`

### Trilho Local (LevelFlow)
- Resolve o **first level** ao entrar num macro (quando macro suporta levels)
- Carrega/unload conteúdo local (additive set)
- Dispara spawns obrigatórios (se fizer parte do contrato do macro/level)
- Roda IntroStage (se o Level definir)
- Permite Level→Level sem cortina (default) com opção de cortina por level
- Mantém **estado local** (`levelId/contentId`) isolado por macro

---

## Fase 0 — Congelar baseline e anchors (rede de proteção)

### Objetivo
Garantir que o sistema atual tem **logs e anchors suficientes** para detectar regressões durante a refatoração.

### Entregáveis
- Documento de evidência canônica (Baseline 3.0) com:
  - Boot → Menu
  - Menu → Gameplay
  - ContentSwap in-place
  - N→1 (A, B, A→B)
- Lista de anchors obrigatórios em logs (grep-friendly)

### Invariantes
- Nenhuma mudança funcional.
- Todos os testes atuais continuam PASS.

### Evidências (mínimas)
- `[OBS][SceneFlow] RouteApplied ...`
- `[OBS][WorldLifecycle] ResetCompleted ...` (quando aplicável)
- `[OBS][ContentSwap] ContentSwapRequested ...`
- `[QA][LevelFlow] NTo1 start ...`

---

## Fase 1 — Separar **Signature Macro vs Signature Local** (e dedupe)

### Problema que resolve
Dedupe por assinatura macro pode bloquear trocas legítimas de level (ex.: A→B) quando o `routeId` é o mesmo.

### Objetivo
- Formalizar dois “domínios” de identidade:
  - `MacroSignature` (SceneFlow)
  - `LevelSignature` (LevelFlow)
- Garantir que dedupe do SceneFlow **não** dedupa operações locais.

### Entregáveis
- Tipo/estrutura clara de `LevelSignature` (pode ser string estruturada, mas preferir tipo/struct)
- Logs novos (sem quebrar os antigos):
  - `[OBS][LevelFlow] LevelSignature ...`
  - `[OBS][LevelFlow] LevelOperationDedupe ...` (quando ocorrer)

### Invariantes
- `TransitionAsync` (macro) continua protegida por dedupe.
- `StartLevel/SwapLevel` (local) **nunca** é dedupada por macro signature.

### Evidências
- Cenário A→B não gera:
  - `[SceneFlow] Dedupe: TransitionAsync ignorado ...` **para a operação local**
- Logs ancorados confirmam:
  - mudança de `levelId` e/ou `contentId`

---

## Fase 2 — Contratos explícitos de reset: **MacroReset vs LevelReset**

### Problema que resolve
Reset “único” gera ambiguidade: intro/pós-level e spawn podem rodar em momentos errados.

### Objetivo
- Definir `ResetKind` (ou eventos distintos):
  - `MacroRouteReset`
  - `LevelLocalReset`
- Garantir que *IntroStage* é do **Level**.

### Entregáveis
- Evento(s) com payload mínimo:
  - `macroRouteId`, `levelId`, `contentId`, `reason`, `kind`, `contextSignature`
- Logs:
  - `[OBS][WorldLifecycle] ResetRequested kind=...`
  - `[OBS][WorldLifecycle] ResetCompleted kind=...`

### Invariantes
- **MacroReset** ao entrar no gameplay volta para o **first level** (ou default do macro).
- **LevelReset** reinicia apenas o level atual (mantém macro).

### Evidências
- Dois testes distintos:
  1) Reset macro em gameplay → volta para level inicial
  2) Reset local em level N → reinicia N

---

## Fase 3 — Introduzir o trilho canônico **LevelLocalFlow** (API), mantendo ContentSwap como mecanismo

### Problema que resolve
ContentSwap tende a virar “navegador escondido” se ele também orquestra progressão.

### Objetivo
- Criar um serviço/fachada de **orquestração local**:
  - `ILevelLocalFlow` (nome sugerido)
- Internamente ele pode chamar ContentSwap e outros serviços.

### API mínima (canônica)
- `EnterFirstLevelForMacro(macroRouteId, reason)`
- `StartLevel(levelId, reason)`
- `AdvanceLevel(reason)` / `PreviousLevel(reason)` (se catálogo ordenado)
- `RestartLevel(reason)`
- `ExitToMacro(reason)` (se necessário)

### Entregáveis
- Interface + implementação
- Estado local isolado:
  - `LevelSession` por macro
- Logs:
  - `[OBS][LevelFlow] EnterFirstLevel ...`
  - `[OBS][LevelFlow] LevelSwapRequested ...`
  - `[OBS][LevelFlow] LevelReady ...`

### Invariantes
- QA menus chamam **apenas** a API canônica (sem atalhos paralelos).
- ContentSwap permanece como “tool” (mecanismo), não como “policy/progressão”.

### Evidências
- N→1 (A/B/A→B) passa usando o LevelLocalFlow.

---

## Fase 4 — Integrar LevelLocalFlow no **pipeline do Macro Loading** (antes do FadeOut)

### Problema que resolve
Você quer “Level Enter” participar do loading e acontecer **antes do FadeOut** do macro.

### Objetivo
- Transformar o loading macro em **pipeline de steps**.
- Adicionar step local:
  - `LevelResolve + LevelLoad` antes do `FadeOut`.

### Pipeline sugerido (macro)
1) `TransitionStarted`
2) FadeIn
3) ApplyRoute (macro scenes)
4) MacroReset (se policy exigir)
5) **LevelStep: ResolveFirstLevel + LoadLocalContent** ✅
6) Spawn obrigatório (se contrato exigir)
7) IntroStage (se Level definir; pode ser step opcional)
8) FadeOut
9) `TransitionCompleted`

### Entregáveis
- “Step runner” (infra) + handlers (macro/local)
- Gating consistente:
  - gameplay gate só abre após Step 7/8 (conforme política)

### Invariantes
- Se macro não tem levels, Step 5 é SKIP explícito (logado).
- Se macro tem levels, Step 5 é obrigatório e fail-fast se catálogo/config faltar.

### Evidências
- Log mostra claramente:
  - `LevelStep completed` ocorrendo **antes** de `FadeOutStarted`.

---

## Fase 5 — IntroStage e Pós-Level totalmente “level-owned”

### Objetivo
- Mover definitivamente IntroStage (e potencial pós-level) para decisões do **LevelDefinition**.

### Entregáveis
- `LevelDefinition` suporta:
  - `hasIntroStage`, `introStageId` (ou referência)
  - `hasOutroStage`/`postLevelFlow` (se aplicável)
- Logs:
  - `[OBS][IntroStage] Started ... levelId=...`
  - `[OBS][IntroStage] Completed ...`

### Invariantes
- Macro não decide intro/pós-level.
- Troca de level pode:
  - ocorrer sem cortina (default)
  - opcionalmente usar cortina por level

### Evidências
- Dois levels no mesmo macro:
  - um com IntroStage
  - um sem
- Logs confirmam comportamento distinto.

---

## Fase 6 — Hardening e remoção de compat (limpeza final)

### Objetivo
- Remover o que sobrou do “alias” (IDs/fields legados) quando não for mais necessário.

### Entregáveis
- Validações Editor + runtime fail-fast em configs obrigatórias
- Redução de warning noise
- ADRs por fase marcados como DONE com evidências

### Invariantes
- Nenhum fallback silencioso em produção.

---

## Sequência recomendada (ordem de implementação)

1) **F0** (evidências)
2) **F1** (signature/dedupe) — *pré-requisito para confiar em Level→Level*
3) **F2** (reset kind) — *contrato de comportamento*
4) **F3** (LevelLocalFlow API) — *trilho local canônico*
5) **F4** (pipeline macro) — *level antes do fadeout*
6) **F5** (IntroStage level-owned)
7) **F6** (hardening/cleanup)

---

## Critérios de “pronto para avançar” (gate)

Uma fase só avança se:

- existe **log canônico** com anchors daquela fase
- existe **checklist** atualizado (Baseline 3.0)
- a fase não introduziu regressões nos cenários anteriores

---

## Notas finais

O maior risco prático (já observado) é **dedupe por assinatura macro** engolindo operações locais.
Por isso, F1 é o primeiro passo técnico real.

A “jogada” arquitetural central é: **LevelLocalFlow como trilho canônico**, integrado ao SceneFlow por um **pipeline de steps**, sem acoplamento cíclico.
