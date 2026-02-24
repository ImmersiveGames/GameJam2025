# ADR-0023 — Dois Níveis de Reset: MacroReset vs LevelReset

## Status

- Estado: Proposto
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-19
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, Navigation, LevelFlow, WorldLifecycle)


## Contexto

O fluxo atual faz reset “macro” (WorldLifecycle/WorldResetService) acoplado ao SceneFlow quando entra em Gameplay.  
Com Levels reais (não-alias), surge a necessidade de dois resets com semânticas diferentes:

- **MacroReset**: reinicia o “espaço macro” (ex.: Gameplay) e retorna ao estado inicial do macro (inclui voltar ao level inicial).
- **LevelReset**: reinicia o *mesmo* level atual (ex.: Level 3) ao seu estado inicial, sem trocar de macro e sem alterar seleção macro.

Sem distinção explícita, o sistema tende a:
- resetar “demais” (voltando para Level 1 quando o usuário queria reset local);
- ou resetar “de menos” (preservando estado que deveria ter sido refeito no macro).

## Decisão

Introduzir dois comandos/contratos explícitos:

1) `ResetMacroAsync(macroRouteId, reason, contextSignature)`
- Trigger típico: transição macro para Gameplay (SceneFlow).
- Efeito:
  - reexecuta pipeline de reset macro (WorldResetService/WorldLifecycle);
  - seleciona **Level inicial** do catálogo do macro (se existir) e prepara conteúdo;
  - finaliza gates macro (SceneFlow completion) somente após “macro ready”.

2) `ResetLevelAsync(levelId, reason, levelSignature)`
- Trigger típico: retry/restart no mesmo level (ex.: morreu e “retry”).
- Efeito:
  - mantém MacroRoute constante;
  - mantém o mesmo levelId selecionado;
  - descarrega conteúdo do level e recarrega conteúdo inicial do mesmo level;
  - reexecuta spawns obrigatórios do level (player, inimigos etc.) conforme regras.

### Regras

- **MacroReset implica LevelReset do level inicial** (por definição de “voltar ao começo do macro”).
- **LevelReset nunca altera** macroRouteId selecionado.
- `reason` deve identificar domínio:
  - `reason='SceneFlow/ScenesReady'` (macro) vs `reason='LevelFlow/Retry'` (level).

## Implicações

### Positivas
- Semântica clara e testável (Baseline 3.0).
- PostGame/Restart pode escolher explicitamente o tipo de reset.
- Evita hacks com “trocar route para resetar level”.

### Negativas / custos
- Exige pontos de integração: Navigation ↔ LevelFlow ↔ WorldLifecycle.
- Ajuste de QA/Dev menus: ações separadas (Reset Macro / Reset Level).

## Alternativas consideradas

1) **Reset único com flags (`resetScope=Macro|Level`)**  
Aceitável, mas tende a virar “boolean soup” em APIs e logs; preferimos contratos explícitos.

2) **Reset sempre macro e “simular” reset local com content swap**  
Rejeitado: perde semântica e acopla demais o Level ao ContentSwap.

## Critérios de aceite (DoD)

- API/fluxo distinguem “MacroReset” e “LevelReset” (telemetria + reason).
- Evidência (Baseline 3.0):
  - MacroReset em entrada de Gameplay retorna para level inicial.
  - LevelReset mantém level atual e reexecuta spawns do mesmo level.
- Não há regressão nos cenários de Baseline 2.0 (Menu↔Gameplay) — apenas logs adicionais [OBS].

## Referências

- ADR-0014 — GameplayReset targets/grupos
- ADR-0016 — ContentSwap ↔ WorldLifecycle
- ADR-0020 — separação MacroRoute vs Level
- ADR-0021 — Baseline 3.0 (Completeness)
