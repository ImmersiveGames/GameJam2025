# Production Policy — Strict / Release / Degraded Mode

Este documento define como o runtime deve se comportar quando **pré-condições** não são atendidas (assets ausentes, serviços DI não registrados, cena/controller inexistente, etc).

## Objetivo

Garantir que:
- Em **Strict (Dev/QA)** o sistema **falhe cedo** (fail-fast) para tornar regressões óbvias.
- Em **Release**, o sistema tenha comportamento **definido** (abort/skip/disable) e **log explícito** quando operar em modo degradado.
- Evidências (logs) sejam **estáveis** e auditáveis.

## Modos

### Strict (Dev/QA)
- Pré-condições **obrigatórias** → `throw`/assert/fail imediato.
- O objetivo é **forçar correção** durante desenvolvimento.
- Logs devem incluir o contexto (`[OBS]` quando aplicável) **antes** da falha, para diagnóstico.

### Release
- Pré-condições podem falhar sem derrubar o jogo *apenas se* existir uma política explícita:
  - **Abortar a operação** (ex.: não trocar fase, não iniciar gameplay)
  - **Desabilitar feature** (ex.: sem HUD)
  - **Fallback controlado** (ex.: NoFade)
- O comportamento deve ser determinístico e documentado no ADR do feature.

### Degraded Mode (Release com fallback)
Quando a operação segue em fallback, **sempre** registrar uma âncora canônica:

- `DEGRADED_MODE feature='<FeatureName>' reason='<Reason>' detail='<...>'`

Exemplos de *feature*:
- `fade`, `loading_hud`, `postgame_inputmode`, `level_catalog`, `world_definition`

## Checklist de invariants de produção (A–F)

> Estes itens derivam das auditorias atuais e devem guiar a normalização do runtime.

| Item | Invariant | Strict (Dev/QA) | Release | Observabilidade mínima |
|---|---|---|---|---|
| A | Fade + LoadingHUD existem quando habilitados | FAIL FAST | DEGRADED_MODE ou abort | `[OBS][SceneFlow]` + DEGRADED_MODE |
| B | Gameplay tem WorldDefinition + spawn mínimo | FAIL FAST | abortar entrar em gameplay | `[OBS][World]` (spawn mínimo) |
| C | LevelCatalog resolve ID/config | FAIL FAST | abortar mudança de nível | `[OBS][LevelCatalog]` |
| D | PostGame depende de Gate/InputMode | FAIL FAST | DEGRADED_MODE com comportamento definido | `[OBS][PostGame]` |
| E | `RequestStart()` ocorre após IntroStage completar | FAIL FAST (assert invariants) | corrigir ordem (sem fallback) | token `sim.gameplay` |
| F | ContentSwap respeita gates (`flow.scene_transition`, `sim.gameplay`) | FAIL FAST (quando violado) | bloquear/adiar conforme política | `[OBS][ContentSwap]` |

## Regras práticas (para implementação)

1) **Não criar objetos “em voo”** como fallback silencioso em runtime (Unity):  
   fallback só é aceitável se for **explícito**, **configurado**, e com **DEGRADED_MODE**.

2) **Serviços críticos** (Gate/InputMode/SceneFlow) devem ser tratados como pré-condição, não como “best effort”.

3) Toda exceção de Strict deve trazer:
- feature
- reason
- signature/profile/target (quando aplicável)

## Referências
- `Standards/Observability-Contract.md`
- ADRs relacionadas (Fade, LoadingHUD, PostGame, ContentSwap, etc)
