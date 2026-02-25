# Standards

Este arquivo consolida referências canônicas que antes estavam separadas em vários arquivos dentro de `Docs/Standards/`.

## Índice
- [Observability Contract](#observability-contract)
- [Política Strict vs Release](#politica-strict-vs-release)
- [Política de uso do Codex](#politica-de-uso-do-codex)
- [Checklist ADRs](#checklist-adrs)
- [Reason Map legado](#reason-map-legado)

---


## Layout canônico de Resources (Navigation)

Assets carregados via `Resources` devem ficar em subpastas nomeadas e documentadas (não na raiz de `Resources`).

Paths canônicos de Navigation:
- `Navigation/GameNavigationCatalog`
- `Navigation/TransitionStyleCatalog`
- `Navigation/LevelCatalog`

Quando houver exceção intencional (ex.: domínio SceneFlow com catálogo próprio), o path deve ser explicitado no plano/ADR correspondente.

### Regra F3 — Route como fonte única de Scene Data


**Observabilidade obrigatória (F3):**

- Ao resolver rota, emitir log canônico:
  - `SceneFlow`: `[OBS][SceneFlow] RouteResolvedVia=RouteId routeId='...' source='ISceneRouteResolver'.`
  - `Navigation`/`LevelFlow`: `[OBS][SceneFlow] RouteResolvedVia={AssetRef|RouteId} ...`
- Se campo legado estiver preenchido mas for ignorado (porque `routeRef` existe), emitir:
  - `[OBS][Deprecated] Legacy field '...' foi ignorado pois 'routeRef' está presente...`
- `ScenesToLoad`/`ScenesToUnload`/`TargetActiveScene` devem ser definidos no `SceneRouteCatalog` (SceneFlow).
- `Navigation` e `LevelFlow` não devem carregar scene data local em runtime; devem resolver por `SceneRouteId`.
- Campos LEGACY em catálogos/definitions podem permanecer apenas para migração, desde que sejam ignorados funcionalmente e emitam warning observável quando preenchidos.

---
## Observability Contract
<a id="observability-contract"></a>


> **Fonte de verdade** do contrato de observabilidade do pipeline NewScripts.
>
> - Logs são evidência.
> - `reason`/`signature`/`token` são **API pública** do pipeline.
> - Outros documentos devem remeter a este contrato em vez de duplicar listas de strings.

### Escopo

Este contrato consolida, em um único ponto canônico, o que deve ser observado em:

- **SceneFlow** (Started / ScenesReady / Completed)
- **WorldLifecycle** (ResetRequested / ResetCompleted / Skipped / Failed)
- **GameLoop** (Ready / IntroStage / Playing / PostGame)
- **InputMode** (aplicações e `reason`)
- **ContentSwap** (InPlace-only)
- **Level** (progressão: orquestra ContentSwap + IntroStage)

### Princípios

- **Log como evidência**: o pipeline é considerado correto quando as assinaturas canônicas aparecem no log, na ordem e com os campos mínimos.
- **Strings canônicas são contrato**: `reason` e `signature` são tratadas como API pública. Mudanças devem ser explicitadas em docs e/ou changelog.
- **Não duplicar fonte de verdade**: documentos que citam reasons devem apontar para este contrato (a seção "Catálogo de reasons" abaixo).
    - `Reason-Map.md` fica **DEPRECATED** e deve conter apenas um redirect para este arquivo.

### Convenções

#### Campos

- `signature`: assinatura do SceneFlow (ou assinatura de reset direto), usada para correlacionar eventos.
- `profile`: profile do SceneFlow, quando aplicável (ex.: `startup`, `gameplay`).
- `target`: alvo principal do evento (geralmente a ActiveScene).
- `sourceSignature`: origem lógica do gatilho (ex.: `Gameplay/HotkeyR`, `qa_marco0_reset`).
- `token`: token do SimulationGate quando o evento envolve bloqueio/desbloqueio.

#### Formato e estabilidade

- `reason` deve ser estável e legível. Mudanças de nomenclatura são consideradas breaking change para QA.
- `signature` deve ser estável dentro de uma transição e reaparecer de forma consistente nos eventos correlatos.

#### Regra oficial de `reason` (autoria e propagação)

- **O `reason` é autoria de quem inicia a ação** (caller). Ex.: QA, UI, GameLoop, ContentSwap.
- **Sistemas downstream não devem ‘renomear’ o reason**; se precisarem de contexto adicional, usem campos próprios (`sourceSignature`, `label`, `event=...`) ou incluam informação no log, mas preservem o `reason`.
- **Exceção controlada**: quando o gatilho é do próprio pipeline (sem um caller externo), usar reasons canônicos do domínio (ex.: `SceneFlow/ScenesReady`).
- **`WorldLifecycleResetCompletedEvent.reason` deve refletir o reason do reset que acabou de finalizar** (reset real, skip ou fail), garantindo correlação 1:1 com `ResetStarted/ResetCompleted`.

### Contrato por domínio

#### SceneFlow

Eventos observáveis (mínimo):

| Evento | Campos mínimos | Observações |
|---|---|---|
| `SceneTransitionStartedEvent` | `signature`, `profile`, `Load`, `Unload`, `Active` | Fecha gate `flow.scene_transition`. |
| `SceneTransitionScenesReadyEvent` | `signature`, `profile` | Dispara WorldLifecycle (reset/skip) para `profile=gameplay`. |
| `SceneTransitionCompletedEvent` | `signature`, `profile` | O completion gate deve ter sido concluído antes do FadeOut (quando há Fade). |

Reasons canônicos de SceneFlow (quando aplicável):

- `SceneFlow/Started`
- `SceneFlow/ScenesReady`
- `SceneFlow/Completed`

Observação: SceneFlow pode usar sufixos em alguns logs (ex.: `SceneFlow/Completed:Gameplay`) quando o domínio deseja diferenciar contexto.

#### WorldLifecycle

Eventos observáveis (mínimo):

| Evento | Campos mínimos | Observações |
|---|---|---|
| `ResetRequested` (log/OBS) | `sourceSignature`, `reason`, `profile`, `target` | Inicia o contrato de reset do mundo. |
| `WorldLifecycleResetCompletedEvent` | `profile`, `signature`, `reason` | **Sempre** emitido (reset real, skip ou fail). |

Reasons canônicos de WorldLifecycle:

- `SceneFlow/ScenesReady`
- `ProductionTrigger/<source>`
- `contentswap.inplace:<contentId>`
- `Skipped_StartupOrFrontend:profile=<profile>;scene=<scene>`
- `Failed_NoController:<scene>`

### Regra F2 — Reset/WorldLifecycle por rota/policy

- **Fonte de verdade da decisão:** metadado de rota (`SceneRouteDefinition.RouteKind`) + `IRouteResetPolicy.Resolve(...) -> RouteResetDecision`, aplicada pelo `WorldLifecycleSceneFlowResetDriver`.
- O driver aplica a decisão da policy e **não deve duplicar regra** de decisão de reset no próprio handler.
- Ao decidir/aplicar reset, os anchors canônicos `[OBS][WorldLifecycle]` devem carregar no mínimo: `signature`, `routeId`, `profile`, `target`, `decisionSource`, `reason` (em `ResetRequested` e `ResetCompleted`).

#### GameLoop

Estados observáveis (mínimo):

| Estado | Evidência mínima | Observações |
|---|---|---|
| `Ready` | log de `ENTER: Ready` | Ready pode ocorrer com `active=False`. |
| `IntroStage` | log de `ENTER: IntroStage` e eventos `[OBS][IntroStage]` | IntroStage é pós-reveal e não participa do completion gate do SceneFlow. |
| `Playing` | log de `ENTER: Playing` e `GameRunStartedEvent` observado | Ações de gameplay devem ser liberadas apenas em Playing. |
| `PostGame` | log de `ENTER: PostGame` e overlay/eventos relacionados | PostGame deve ser idempotente. |

Assinaturas canônicas (Gameplay → PostGame):
- `[GameLoop] ENTER: PostGame (active=...)`
- `[OBS][PostGame] PostGameEntered signature='...' outcome='...' reason='...' scene='...' profile='...' frame=...`
- `[OBS][PostGame] PostGameExited signature='...' reason='...' nextState='...' scene='...' profile='...' frame=...`
- `[OBS][PostGame] PostGameSkipped reason='scene_not_gameplay' scene='...'`
- `[PostGame] GameRunEndedEvent recebido. Exibindo overlay.`
- `[PostGame] Restart ignorado (ação já solicitada).`
- `[PostGame] ExitToMenu ignorado (ação já solicitada).`
- `[PostGame] GameRunEndedEvent ignorado (ação já solicitada).`

#### InputMode

Eventos observáveis (mínimo):

| Evento | Campos mínimos | Observações |
|---|---|---|
| Apply (log/OBS) | `mode`, `map`, `reason`, `signature`, `scene`, `profile` | Deve ocorrer em pontos canônicos: `SceneFlow/Completed:*`, `IntroStage/*`, `GameLoop/*`, `PostGame/*`. |

Reasons canônicos (prefixos) para InputMode:

- `SceneFlow/Completed:Frontend`
- `SceneFlow/Completed:Gameplay`
- `IntroStage/UIConfirm`
- `GameLoop/Playing`
- `PostGame/RunStarted`

#### ContentSwap

O contrato para ContentSwap é definido em ADR-0016 (ContentSwap InPlace-only).

**Modo canônico**

- `InPlace` — troca dentro da mesma cena de gameplay. Pode usar mini-fade opcional, mas **não** deve usar LoadingHUD.

**Eventos/anchors mínimos**

- `[OBS][ContentSwap] ContentSwapRequested event=content_swap_inplace mode=InPlace contentId='...' reason='...'`
- `[OBS][ContentSwap] ContentSwapRequested ...` (legado; alias do ContentSwap)
- `[ContentSwapContext] ContentSwapPendingSet plan='...' reason='...'` (legado; contexto de ContentSwap)
- `[ContentSwapContext] ContentSwapCommitted prev='...' current='...' reason='...'` (legado; contexto de ContentSwap)

**Regra do `reason`**

- O `reason` da troca de conteúdo é **fornecido pelo caller** (produção/QA).
- Recomendações para QA (prefixos estáveis):
    - `QA/ContentSwap/InPlace/<...>`

#### Level

O contrato para Level Manager é definido em ADR-0017.

**Eventos/anchors mínimos**

- `[OBS][Level] LevelChangeRequested levelId='...' contentId='...' mode='InPlace' reason='...' contentSig='...'`
- `[OBS][Level] LevelChangeStarted levelId='...' contentId='...' mode='InPlace' reason='...'`
- `[OBS][Level] LevelChangeCompleted levelId='...' contentId='...' mode='InPlace' reason='...'`

**Regra do `reason`**

- O `reason` da mudança de nível é **fornecido pelo caller** (produção/QA).
- Recomendações para QA (prefixos estáveis):
    - `QA/Levels/InPlace/<...>`

#### Navigation (GameNavigation)

Eventos/anchors mínimos:

- `[OBS][Navigation] Catalog boot snapshot: resourcePath='...', assetName='...', rawRoutesCount=..., builtRouteIdsCount=..., hasToGameplay=...`
    - Emitido no bootstrap (GlobalCompositionRoot) após carregar `GameNavigationCatalogAsset`.
    - Objetivo: detectar **catálogo vazio** ou **mismatch de schema/snapshot** antes do usuário clicar Play.
- `[Navigation] GameNavigationService inicializado. Entries: [...]`
    - Lista de `routeId` resolvidos pelo catálogo (após migração/EnsureBuilt).
- `[Navigation] Rota desconhecida ou sem request. routeId='...'` + `Entries disponíveis: [...]`
    - Erro determinístico quando o intent não existe no catálogo ou quando o catálogo está vazio.

#### IntroStage

Reasons canônicos:

- `IntroStage/UIConfirm`
- `IntroStage/NoContent`

### Catálogo de reasons canônicos

Este catálogo reúne os principais reasons citados como critérios de aceite, garantindo que documentos e QA usem o mesmo vocabulário.

- SceneFlow
    - `SceneFlow/Started`
    - `SceneFlow/ScenesReady`
    - `SceneFlow/Completed`
- WorldLifecycle
    - `SceneFlow/ScenesReady`
    - `ProductionTrigger/<source>`
    - `contentswap.inplace:<contentId>`
    - `Skipped_StartupOrFrontend:profile=<...>;scene=<...>`
    - `Failed_NoController:<scene>`
- IntroStage
    - `IntroStage/UIConfirm`
    - `IntroStage/NoContent`
- Pause
    - `Pause/Enter`
    - `Pause/Exit`
- ContentSwap
    - `ContentSwap/InPlace/<source>`
    - `QA/ContentSwap/InPlace/<...>`
- PostGame
    - `PostGame/Enter`
    - `PostGame/Exit`
    - `PostGame/Restart`
    - `PostGame/ExitToMenu`
- Level
    - `LevelChange/<source>`
    - `QA/Levels/InPlace/<...>`

Observação: `Reason-Map.md` é mantido apenas como redirect histórico para este contrato (não deve conter lista paralela).

### Invariantes

- **ScenesReady acontece antes de Completed** (na mesma `signature`).
- **ResetCompleted sempre é emitido** (reset real, skip ou fail) e pode ser usado por gates.
- **Completion gate do SceneFlow aguarda ResetCompleted antes de FadeOut** quando configurado.
- **IntroStage é pós-reveal**: ocorre após `SceneFlow/Completed` e não deve atrasar o completion gate.

### Evidências (logs e relatórios)

As evidências abaixo são extraídas de:

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`

#### Skipped startup/frontend

Exemplo de `Skipped_StartupOrFrontend:profile=...;scene=...` (Baseline):

- `[WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.`

#### Reset em ScenesReady (gameplay)

Exemplo de `SceneFlow/ScenesReady` (Baseline):

- `[WorldLifecycle] Reset REQUESTED. reason='SceneFlow/ScenesReady', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', profile='gameplay'.`

#### Reset trigger de produção

Exemplo de `ProductionTrigger/<source>` (validação manual):

- `[WorldLifecycle] Reset REQUESTED. signature='directReset:scene=GameplayScene;src=qa_marco0_reset;seq=4;salt=b3a0e296', reason='ProductionTrigger/qa_marco0_reset', source='qa_marco0_reset', scene='GameplayScene'.`

#### Reset fail por ausência de controller

Exemplo de `Failed_NoController:<scene>` (validação manual):

- `[WorldLifecycle] WorldLifecycleController não encontrado na cena 'MenuScene'. Reset abortado.`
- `Emitting WorldLifecycleResetCompletedEvent. ... reason='Failed_NoController:MenuScene'.`

#### IntroStage

Exemplo de `IntroStage/UIConfirm` (validação manual):

- `Solicitando CompleteIntroStage reason='IntroStage/UIConfirm'.`

Exemplo de `IntroStage/NoContent` (documentado em QA):

- Ver `Docs/Reports/Evidence/LATEST.md` (seção IntroStage).

### Âncora canônica: DEGRADED_MODE

Quando uma feature opera com fallback (Release), registre:

- `DEGRADED_MODE feature='<FeatureName>' reason='<Reason>' detail='<...>'`

Isso evita evidência frágil baseada em warnings genéricos.

---

### Reason Registry

## Reason Registry (canônico)

Este arquivo mantém um **registro prático** de `reason` canônicos usados em logs e eventos (Observability Contract).

> Nota: o antigo “Reason-Map” foi descontinuado e pode não existir no repositório.

### Regras

- `reason` é uma string curta e estável, com hierarquia por `/`.
- Evite incluir IDs dinâmicos no reason; IDs vão em campos próprios (`contentId`, `levelId`, etc) ou em `detail`.

### Convenções

- Prefixos por domínio:
  - `SceneFlow/...`
  - `WorldLifecycle/...`
  - `IntroStage/...`
  - `PostGame/...`
  - `QA/...`

### Catálogo inicial (baseline 2.x)

- `SceneFlow/ScenesReady`
- `IntroStage/UIConfirm`
- `IntroStage/NoContent`
- `PostGame/Restart`
- `PostGame/ExitToMenu`
- `QA/ContentSwap/InPlace/NoVisuals`

### Como atualizar

Quando um ADR introduzir um novo reason:
1) Registrar aqui (nome + contexto)
2) Usar no código/logs conforme `Standards/Standards.md#observability-contract`
3) Cobrir em evidência (log datado) quando chegar a produção

---

## Política Strict vs Release
<a id="politica-strict-vs-release"></a>


Este documento define como o runtime deve se comportar quando **pré-condições** não são atendidas (assets ausentes, serviços DI não registrados, cena/controller inexistente, etc).

### Objetivo

Garantir que:
- Em **Strict (Dev/QA)** o sistema **falhe cedo** (fail-fast) para tornar regressões óbvias.
- Em **Release**, o sistema tenha comportamento **definido** (abort/skip/disable) e **log explícito** quando operar em modo degradado.
- Evidências (logs) sejam **estáveis** e auditáveis.

### Modos

#### Strict (Dev/QA)
- Pré-condições **obrigatórias** → `throw`/assert/fail imediato.
- O objetivo é **forçar correção** durante desenvolvimento.
- Logs devem incluir o contexto (`[OBS]` quando aplicável) **antes** da falha, para diagnóstico.

#### Release
- Pré-condições podem falhar sem derrubar o jogo *apenas se* existir uma política explícita:
  - **Abortar a operação** (ex.: não trocar fase, não iniciar gameplay)
  - **Desabilitar feature** (ex.: sem HUD)
  - **Fallback controlado** (ex.: NoFade)
- O comportamento deve ser determinístico e documentado no ADR do feature.

#### Degraded Mode (Release com fallback)
Quando a operação segue em fallback, **sempre** registrar uma âncora canônica:

- `DEGRADED_MODE feature='<FeatureName>' reason='<Reason>' detail='<...>'`

Exemplos de *feature*:
- `fade`, `loading_hud`, `postgame_inputmode`, `level_catalog`, `world_definition`

### Checklist de invariants de produção (A–F)

> Estes itens derivam das auditorias atuais e devem guiar a normalização do runtime.

| Item | Invariant | Strict (Dev/QA) | Release | Observabilidade mínima |
|---|---|---|---|---|
| A | Fade + LoadingHUD existem quando habilitados | FAIL FAST | DEGRADED_MODE ou abort | `[OBS][SceneFlow]` + DEGRADED_MODE |
| B | Gameplay tem WorldDefinition + spawn mínimo | FAIL FAST | abortar entrar em gameplay | `[OBS][World]` (spawn mínimo) |
| C | LevelCatalog resolve ID/config | FAIL FAST | abortar mudança de nível | `[OBS][LevelCatalog]` |
| D | PostGame depende de Gate/InputMode | FAIL FAST | DEGRADED_MODE com comportamento definido | `[OBS][PostGame]` |
| E | `RequestStart()` ocorre após IntroStage completar | FAIL FAST (assert invariants) | corrigir ordem (sem fallback) | token `sim.gameplay` |
| F | ContentSwap respeita gates (`flow.scene_transition`, `sim.gameplay`) | FAIL FAST (quando violado) | bloquear/adiar conforme política | `[OBS][ContentSwap]` |

### Regras práticas (para implementação)

1) **Não criar objetos “em voo”** como fallback silencioso em runtime (Unity):  
   fallback só é aceitável se for **explícito**, **configurado**, e com **DEGRADED_MODE**.

2) **Serviços críticos** (Gate/InputMode/SceneFlow) devem ser tratados como pré-condição, não como “best effort”.

3) Toda exceção de Strict deve trazer:
- feature
- reason
- signature/profile/target (quando aplicável)

### Referências
- `Standards/Standards.md#observability-contract`
- ADRs relacionadas (Fade, LoadingHUD, PostGame, ContentSwap, etc)

---

## Política de uso do Codex
<a id="politica-de-uso-do-codex"></a>


### Objetivo

Usar o CODEX **somente** para auditorias de sincronização (Docs ⇄ Código) e inventário de implementação.

### Regras

1. **Proibido:** solicitar ao CODEX qualquer ação que altere o repositório (criar/editar/remover arquivos, refatorar, “corrigir”, etc.).
2. **Permitido:** leitura e análise (listar arquivos, localizar símbolos, mapear fluxos e comparar com ADRs/contratos).
3. O output do CODEX deve ser **sempre** um artefato em `Docs/Reports/Audits/<YYYY-MM-DD>/`.
4. Qualquer decisão de implementação entra como plano humano (fora do CODEX) e, só depois, mudanças reais são feitas no repositório.

### Prompt canônico

Use `Docs/Reports/Audits/2026-02-19/ADR-Sync-Audit-Prompt.md`.

---

## Checklist ADRs
<a id="checklist-adrs"></a>


Este documento resume **o mínimo necessário** para considerar cada ADR (0009–0019) “completo para produção” sob a ótica:

- **Strict vs Release** (falha controlada em Dev/QA; degradação explícita em Release).
- **Invariants verificáveis** (ordem, gates, eventos).
- **Observabilidade canônica** (logs âncora do contrato).

> Uso: base para auditorias (CODEX read-only) e para normalização do sistema.

### Tabela (resumo)

| ADR | Tema | Para ficar “ideal de produção” (mínimos) | Evidência esperada |
|---|---|---|---|
| ADR-0009 | Fade + SceneFlow | (1) **Fail-fast em Strict** quando `FadeScene/Controller` não existe; (2) **Degraded mode explícito** em Release (config + log âncora); (3) Ordem: FadeIn → operação → ScenesReady → BeforeFadeOut → FadeOut → Completed; (4) Logs conforme Observability Contract | Logs/anchors `[OBS][Fade]` ou equivalente + trecho de código com branch Strict/Release |
| ADR-0010 | Loading HUD + SceneFlow | (1) Fail-fast em Strict para HUD/controller ausente; (2) Degraded mode explícito em Release; (3) Orquestração por eventos SceneFlow; (4) Logs canônicos | Logs `LoadingHudEnsure/Show/Hide` + branch Strict/Release |
| ADR-0011 | WorldDefinition + multi-actor | (1) Em gameplay: **worldDefinition obrigatório** em Strict; (2) validação de mínimo spawn (Player + Eater); (3) deterministic spawn pipeline | Logs `[OBS][WorldDefinition]`/`[OBS][Spawn]` + validações explícitas |
| ADR-0012 | PostGame | (1) Dependências críticas (Gate/InputMode) falham em Strict; (2) fallback explícito em Release; (3) idempotência do overlay; (4) reason/contextSignature canônicos | Logs `[OBS][PostGame]` + evidências de idempotência |
| ADR-0013 | Ciclo de vida | (1) `RequestStart()` somente após **IntroStageComplete** (ou equivalente); (2) tokens `flow.scene_transition`, `sim.gameplay` coerentes; (3) reset determinístico disparado no ponto “produção” definido | Logs `[OBS][SceneFlow]` + `[OBS][WorldLifecycle]` + ordem comprovada |
| ADR-0014 | Reset targets/grupos | (1) Classificação determinística; (2) inconsistências falham em Strict (ou policy formal); (3) ausência de target/config não vira scan silencioso sem política | Logs `[OBS][GameplayReset]` + validações |
| ADR-0015 | Baseline 2.0 | (1) Evidência canônica arquivada; (2) invariants A–E verificáveis via log; (3) método de atualização de evidências | `Docs/Reports/LATEST.md` + logs arquivados |
| ADR-0016 | ContentSwap in-place | (1) Respeitar gates `flow.scene_transition` e `sim.gameplay`; (2) policy de bloqueio/retry/abort documentada; (3) logs canônicos e reason | Logs `[OBS][ContentSwap]` + checagens de gate |
| ADR-0017 | LevelCatalog/LevelManager | (1) Resolver por ID falha em Strict se catálogo/definição ausente; (2) comportamento Release definido; (3) logs canônicos | Logs `[OBS][LevelCatalog]` + validações e policy |

### Checklist transversal (A–F)

- **A)** Fade/LoadingHUD: Strict + Release + degraded mode explícito
- **B)** WorldDefinition: Strict + mínimo spawn
- **C)** LevelCatalog: Strict + Release
- **D)** PostGame: Strict + Release
- **E)** Ordem do fluxo: RequestStart após IntroStageComplete
- **F)** Gates: ContentSwap respeita `flow.scene_transition` e `sim.gameplay`

---

## Reason Map legado
<a id="reason-map-legado"></a>


Este arquivo existe **apenas** como *redirect* para buscas/ferramentas e para evitar referências órfãs em auditorias.

### Fonte canônica

- **Use:** `Standards/Standards.md#observability-contract`
- **Não use:** este arquivo para registrar novos `reason`.

### Regra

Não manter listas duplicadas de `reason`. Qualquer duplicidade inevitavelmente diverge do runtime e enfraquece evidências.


---
