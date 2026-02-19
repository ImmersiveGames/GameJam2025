# ADR-0025 — Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: Proposto
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-19
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, Navigation, LevelFlow, WorldLifecycle)


## Contexto

No fluxo macro atual:
1) FadeIn
2) Apply Route (load/unload/active)
3) WorldReset (quando política exige)
4) ScenesReady
5) Completion gate
6) FadeOut

A intenção nova é: **se a MacroRoute possui Levels**, então o “Level 1” (ou default) deve ser carregado **antes do macro concluir o loading e abrir a cortina**.

Ou seja, o usuário só vê o mundo após:
- base macro pronta + reset macro executado
- level selecionado + conteúdo aplicado
- spawns obrigatórios concluídos

## Decisão

Adicionar ao pipeline macro uma etapa explícita **LevelPrepareStep** (condicional):

**Macro Transition Pipeline (com levels):**
1) FadeIn (cortina fecha)
2) Apply Route (load/unload/active)
3) WorldReset (macro), se `requiresWorldReset`
4) ScenesReady (base macro pronta)
5) **LevelPrepareStep**:
   - resolve `LevelCollection` do macro
   - seleciona default level (ou mantém selecionado se cenário permitir)
   - aplica conteúdo do level (swap/load)
   - executa reset/local-init do level conforme contrato
   - garante spawns obrigatórios (player/inimigos etc.)
6) Completion gate macro concluído
7) FadeOut (cortina abre)

**Macro Transition Pipeline (sem levels):**
- mantém pipeline atual (sem etapa 5).

### Observabilidade

- Logs [OBS] do pipeline devem evidenciar a fase:
  - `[OBS][SceneFlow] MacroLoadingPhase='LevelPrepare' ...`
  - `[OBS][LevelFlow] LevelPrepared ...`
- A conclusão do completion gate só ocorre após `LevelPrepared`.

## Implicações

### Positivas
- Coerência com intenção: MacroRoute “entrega” um gameplay pronto (inclui level inicial).
- Reduz “flash” de mundo vazio e melhora UX.
- Simplifica IntroStage: passa a ser do Level (executa após FadeOut ou com cortina opcional).

### Negativas / custos
- A etapa LevelPrepare adiciona dependências ordenadas (SceneFlow → LevelFlow → WorldLifecycle).
- Precisa de cuidado para não duplicar reset/spawn (idempotência).

## Alternativas consideradas

1) **Carregar Level depois do FadeOut macro**  
Rejeitado: expõe mundo incompleto e confunde responsabilidade do macro.

2) **Sempre ter Fade para troca de level**  
Rejeitado: perde o benefício de swap local; a cortina do level é opcional e controlada pelo level.

## Critérios de aceite (DoD)

- Em macro com levels:
  - FadeOut macro acontece apenas após `LevelPrepared`.
  - Logs comprovam ordem: `ScenesReady` → `LevelPrepare` → `TransitionCompleted`.
- Em macro sem levels:
  - pipeline não muda (sem regressão).
- Baseline 3.0:
  - evidência “Macro enter gameplay + Level 1 ready antes do FadeOut”.

## Referências

- ADR-0013 — Ciclo de vida do jogo
- ADR-0016 — ContentSwap ↔ WorldLifecycle
- ADR-0020 — separação Macro vs Level
- ADR-0021 — Baseline 3.0
