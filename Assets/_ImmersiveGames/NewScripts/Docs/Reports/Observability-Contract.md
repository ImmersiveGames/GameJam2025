# Observability Contract — SceneFlow, WorldLifecycle, GameLoop, InputMode, PhaseChange

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
- **PhaseChange** (in-place vs scene transition)

## Princípios

- **Log como evidência**: o pipeline é considerado correto quando as assinaturas canônicas aparecem no log, na ordem e com os campos mínimos.
- **Strings canônicas são contrato**: `reason` e `signature` são tratadas como API pública. Mudanças devem ser explicitadas em docs e/ou changelog.
- **Não duplicar fonte de verdade**: documentos que citam reasons devem apontar para a seção "Mapa de reasons" abaixo.

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

- `ScenesReady/<scene>`
- `ProductionTrigger/<source>`
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

### InputMode

Eventos observáveis (mínimo):

| Evento | Campos mínimos | Observações |
|---|---|---|
| Apply (log/OBS) | `mode`, `map`, `reason`, `signature`, `scene`, `profile` | Deve ocorrer em pontos canônicos: `SceneFlow/Completed:*`, `IntroStage/*`, `GameLoop/*`, `PostGame/*`. |

Reasons canônicos (prefixos) para InputMode:

- `SceneFlow/Completed:Frontend`
- `SceneFlow/Completed:Gameplay`
- `IntroStage/ConfirmToStart`
- `GameLoop/Playing`
- `PostGame/RunStarted`

### PhaseChange

O contrato para PhaseChange é definido em ADR-0017 (Tipos de troca de fase).

Tipos (canônicos):

- `PhaseChange/In-Place` (troca no mesmo gameplay, sem trocar cenas)
- `PhaseChange/SceneTransition` (troca com transição completa de cenas, Fade e Loading)

Reasons canônicos (prefixos):

- `PhaseChange/In-Place/<phaseId>`
- `PhaseChange/SceneTransition/<phaseId>`

Observação: detalhes de `phaseId` (formato, numeração, nomes) pertencem à decisão do domínio de gameplay, mas o prefixo deve permanecer estável.

### IntroStage

Reasons canônicos:

- `IntroStage/UIConfirm`
- `IntroStage/NoContent`

## Mapa de reasons canônicos

Este mapa reúne os principais reasons citados como critérios de aceite do Item 8:

- WorldLifecycle
    - `ScenesReady/<scene>`
    - `ProductionTrigger/<source>`
    - `Skipped_StartupOrFrontend:profile=<...>;scene=<...>`
    - `Failed_NoController:<scene>`
- IntroStage
    - `IntroStage/UIConfirm`
    - `IntroStage/NoContent`

Para um índice/glossário (com possíveis aliases), ver: [Reason-Map.md](./Reason-Map.md).

## Invariantes

- **ScenesReady acontece antes de Completed** (na mesma `signature`).
- **ResetCompleted sempre é emitido** (reset real, skip ou fail) e pode ser usado por gates.
- **Completion gate do SceneFlow aguarda ResetCompleted antes de FadeOut** quando configurado.
- **IntroStage é pós-reveal**: ocorre após `SceneFlow/Completed` e não deve atrasar o completion gate.

## Evidências (logs e relatórios)

As evidências abaixo são extraídas de:

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/2026-01-16/Baseline-2.1-ContractEvidence-2026-01-16.md`
- `Docs/Reports/Evidence/2026-01-16/Baseline-2.1-Smoke-LastRun.log`

### Skipped startup/frontend

Exemplo de `Skipped_StartupOrFrontend:profile=...;scene=...` (Baseline):

- `[WorldLifecycle] Reset SKIPPED (startup/frontend). why='profile', profile='startup', activeScene='MenuScene', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.`

### Reset em ScenesReady (gameplay)

Exemplo de `ScenesReady/<scene>` (Baseline):

- `[WorldLifecycle] Reset REQUESTED. reason='ScenesReady/GameplayScene', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', profile='gameplay'.`

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
