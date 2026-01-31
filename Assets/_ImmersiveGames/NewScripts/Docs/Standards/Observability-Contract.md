# Observability Contract — SceneFlow, WorldLifecycle, GameLoop, InputMode, ContentSwap, Level

> **Fonte de verdade** do contrato de observabilidade do pipeline NewScripts.
>
> - Logs são evidência.
> - `reason`/`signature`/`token` são **API pública** do pipeline.
> - Outros documentos devem remeter a este contrato em vez de duplicar listas de strings.

## Escopo

Este contrato consolida, em um único ponto canônico, o que deve ser observado em:

- **SceneFlow** (Started / ScenesReady / Completed)
- **WorldLifecycle** (ResetRequested / ResetCompleted / Skipped / Failed)
- **GameLoop** (Ready / IntroStage / Playing / PostGame)
- **InputMode** (aplicações e `reason`)
- **ContentSwap** (InPlace-only)
- **Level** (progressão: orquestra ContentSwap + IntroStage)

## Princípios

- **Log como evidência**: o pipeline é considerado correto quando as assinaturas canônicas aparecem no log, na ordem e com os campos mínimos.
- **Strings canônicas são contrato**: `reason` e `signature` são tratadas como API pública. Mudanças devem ser explicitadas em docs e/ou changelog.
- **Não duplicar fonte de verdade**: documentos que citam reasons devem apontar para este contrato (a seção "Catálogo de reasons" abaixo).
    - `Reason-Map.md` fica **DEPRECATED** e deve conter apenas um redirect para este arquivo.

## Convenções

### Campos

- `signature`: assinatura do SceneFlow (ou assinatura de reset direto), usada para correlacionar eventos.
- `profile`: profile do SceneFlow, quando aplicável (ex.: `startup`, `gameplay`).
- `target`: alvo principal do evento (geralmente a ActiveScene).
- `sourceSignature`: origem lógica do gatilho (ex.: `Gameplay/HotkeyR`, `qa_marco0_reset`).
- `token`: token do SimulationGate quando o evento envolve bloqueio/desbloqueio.

### Formato e estabilidade

- `reason` deve ser estável e legível. Mudanças de nomenclatura são consideradas breaking change para QA.
- `signature` deve ser estável dentro de uma transição e reaparecer de forma consistente nos eventos correlatos.

### Regra oficial de `reason` (autoria e propagação)

- **O `reason` é autoria de quem inicia a ação** (caller). Ex.: QA, UI, GameLoop, ContentSwap.
- **Sistemas downstream não devem ‘renomear’ o reason**; se precisarem de contexto adicional, usem campos próprios (`sourceSignature`, `label`, `event=...`) ou incluam informação no log, mas preservem o `reason`.
- **Exceção controlada**: quando o gatilho é do próprio pipeline (sem um caller externo), usar reasons canônicos do domínio (ex.: `SceneFlow/ScenesReady`).
- **`WorldLifecycleResetCompletedEvent.reason` deve refletir o reason do reset que acabou de finalizar** (reset real, skip ou fail), garantindo correlação 1:1 com `ResetStarted/ResetCompleted`.

## Contrato por domínio

### SceneFlow

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

### WorldLifecycle

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

### GameLoop

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

### InputMode

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

### ContentSwap

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

### Level

O contrato para Level Manager é definido em ADR-0018/ADR-0019.

**Eventos/anchors mínimos**

- `[OBS][Level] LevelChangeRequested levelId='...' contentId='...' mode='InPlace' reason='...' contentSig='...'`
- `[OBS][Level] LevelChangeStarted levelId='...' contentId='...' mode='InPlace' reason='...'`
- `[OBS][Level] LevelChangeCompleted levelId='...' contentId='...' mode='InPlace' reason='...'`

**Regra do `reason`**

- O `reason` da mudança de nível é **fornecido pelo caller** (produção/QA).
- Recomendações para QA (prefixos estáveis):
    - `QA/Levels/InPlace/<...>`

### IntroStage

Reasons canônicos:

- `IntroStage/UIConfirm`
- `IntroStage/NoContent`

## Catálogo de reasons canônicos

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

## Invariantes

- **ScenesReady acontece antes de Completed** (na mesma `signature`).
- **ResetCompleted sempre é emitido** (reset real, skip ou fail) e pode ser usado por gates.
- **Completion gate do SceneFlow aguarda ResetCompleted antes de FadeOut** quando configurado.
- **IntroStage é pós-reveal**: ocorre após `SceneFlow/Completed` e não deve atrasar o completion gate.

## Evidências (logs e relatórios)

As evidências abaixo são extraídas de:

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`

### Skipped startup/frontend

Exemplo de `Skipped_StartupOrFrontend:profile=...;scene=...` (Baseline):

- `[WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.`

### Reset em ScenesReady (gameplay)

Exemplo de `SceneFlow/ScenesReady` (Baseline):

- `[WorldLifecycle] Reset REQUESTED. reason='SceneFlow/ScenesReady', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', profile='gameplay'.`

### Reset trigger de produção

Exemplo de `ProductionTrigger/<source>` (validação manual):

- `[WorldLifecycle] Reset REQUESTED. signature='directReset:scene=GameplayScene;src=qa_marco0_reset;seq=4;salt=b3a0e296', reason='ProductionTrigger/qa_marco0_reset', source='qa_marco0_reset', scene='GameplayScene'.`

### Reset fail por ausência de controller

Exemplo de `Failed_NoController:<scene>` (validação manual):

- `[WorldLifecycle] WorldLifecycleController não encontrado na cena 'MenuScene'. Reset abortado.`
- `Emitting WorldLifecycleResetCompletedEvent. ... reason='Failed_NoController:MenuScene'.`

### IntroStage

Exemplo de `IntroStage/UIConfirm` (validação manual):

- `Solicitando CompleteIntroStage reason='IntroStage/UIConfirm'.`

Exemplo de `IntroStage/NoContent` (documentado em QA):

- Ver `Docs/Reports/Evidence/LATEST.md` (seção IntroStage).

## Âncora canônica: DEGRADED_MODE

Quando uma feature opera com fallback (Release), registre:

- `DEGRADED_MODE feature='<FeatureName>' reason='<Reason>' detail='<...>'`

Isso evita evidência frágil baseada em warnings genéricos.

---

## Reason Registry

# Reason Registry (canônico)

Este arquivo mantém um **registro prático** de `reason` canônicos usados em logs e eventos (Observability Contract).

> Nota: o antigo “Reason-Map” foi descontinuado e pode não existir no repositório.

## Regras

- `reason` é uma string curta e estável, com hierarquia por `/`.
- Evite incluir IDs dinâmicos no reason; IDs vão em campos próprios (`contentId`, `levelId`, etc) ou em `detail`.

## Convenções

- Prefixos por domínio:
  - `SceneFlow/...`
  - `WorldLifecycle/...`
  - `IntroStage/...`
  - `PostGame/...`
  - `QA/...`

## Catálogo inicial (baseline 2.x)

- `SceneFlow/ScenesReady`
- `IntroStage/UIConfirm`
- `IntroStage/NoContent`
- `PostGame/Restart`
- `PostGame/ExitToMenu`
- `QA/ContentSwap/InPlace/NoVisuals`

## Como atualizar

Quando um ADR introduzir um novo reason:
1) Registrar aqui (nome + contexto)
2) Usar no código/logs conforme `Standards/Observability-Contract.md`
3) Cobrir em evidência (log datado) quando chegar a produção
