# ADR-0023 — Dois níveis de reset: MacroReset vs LevelReset

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-25
- Tipo: Implementação
- Escopo: NewScripts/Modules (WorldLifecycle, SceneFlow, LevelFlow, ContentSwap)

## Resumo

Definir e instrumentar **dois níveis de reset**, com objetivos e custos diferentes:

- **MacroReset**: reset completo do “mundo” dentro de um macro (despawn/spawn, hooks, limpeza de estado).
- **LevelReset**: reset local (conteúdo/variantes/objetivos) sem exigir transição macro.

## Contexto

O projeto precisa:

- Reset determinístico e rastreável (Baseline / evidências por log).
- Evitar re-carregar cenas macro quando a intenção é apenas “reiniciar o level” ou “trocar conteúdo”.

## Decisão

### 1) MacroReset (WorldLifecycle)

- Executa pipeline completo do WorldLifecycle (hooks + despawn + spawn).
- Deve ser acionado quando:
  - Entramos em um macro que requer reset (`requiresWorldReset=True`).
  - Reinício macro (ex.: Restart -> Boot determinístico).
  - QA: “ResetMacro” para verificar invariantes do pipeline.

### 2) LevelReset (Local)

- Deve ser mais barato e não depende de transição macro.
- Pode envolver:
  - Reaplicar `contentId` (ContentSwap in-place).
  - Reiniciar objetivos/estado de level.
  - Opcionalmente disparar um MacroReset se o level exigir (policy futura).

### 3) Contrato de eventos e logs

- `WorldResetCommands` publica:
  - `ResetRequested(kind='Macro'| 'Level', ...)`
  - `ResetCompleted(kind='Macro'| 'Level', success, ...)`
- Em MacroReset, deve haver `ResetWorldStarted/ResetCompleted` do WorldLifecycle.

## Implementação atual (2026-02-25)

### Evidências (anchors do log canônico)

- LevelReset:
  - `WorldResetCommands [OBS][WorldLifecycle] ResetRequested kind='Level' ... levelId='level.1' contentId='content.default' ...`
  - `InPlaceContentSwapService [OBS][ContentSwap] ContentSwapRequested ... mode=InPlace ...`
  - `WorldResetCommands [OBS][WorldLifecycle] ResetCompleted kind='Level' ... success=True`

- MacroReset:
  - `WorldResetCommands [OBS][WorldLifecycle] ResetRequested kind='Macro' ... macroSignature='r:level.1|s:style.gameplay|...'`
  - `WorldResetOrchestrator [ResetStart][OBS][WorldLifecycle] ResetWorldStarted signature='...'`
  - `WorldResetOrchestrator [ResetCompleted][OBS][WorldLifecycle] ResetCompleted signature='...'`
  - `WorldResetCommands [OBS][WorldLifecycle] ResetCompleted kind='Macro' ... success=True`

### Regras validadas pelo log

- MacroReset executa despawn/spawn (ActorRegistry volta a 0 e sobe de novo).
- LevelReset pode operar sem transição macro (não aparece `TransitionStarted` no trecho de ResetLevel).

## Implicações

- O sistema passa a ter “ferramentas” de QA claras:
  - ResetLevel = “troca local / reinício local”
  - ResetMacro = “reset determinístico completo”
- Permite evolução incremental de LevelSwap (ADR-0026) usando LevelReset como base.

## Critérios de aceite (DoD)

- [x] Existem dois comandos distinguíveis (Macro vs Level).
- [x] Logs [OBS] exibem kind + ids relevantes + signature/levelSignature quando aplicável.
- [x] MacroReset executa pipeline completo do WorldLifecycle.
- [x] LevelReset dispara ContentSwap in-place e completa com sucesso (quando aplicável).
- [ ] Hardening: políticas por level (ex.: “este level exige MacroReset”) documentadas e validadas.

## Changelog

- 2026-02-25: Marcado como **Implementado**, adicionadas evidências do log (ResetRequested/ResetCompleted para Level e Macro).
