# ADR-0027 — IntroStage e PostLevel como Responsabilidade do Level

## Status

- Estado: Proposto
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-19
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, Navigation, LevelFlow, WorldLifecycle)


## Contexto

No desenho antigo, parte do “intro” (ex.: IntroStage/UIConfirm) estava acoplada ao fluxo macro.  
Com Levels, o intro faz sentido como parte do **conteúdo/experiência do level**, não da rota macro.

Além disso, alguns levels podem querer:
- IntroStage (antes do gameplay liberar inputs)
- PostLevel (antes de avançar para o próximo level, ou para retornar ao macro hub)

## Decisão

Mover a ownership de stages para o domínio de LevelFlow:

- `ILevelStageOrchestrator` (ou equivalente) executa stages do level:
  - `IntroStage` (opcional)
  - `PostLevelStage` (opcional)

### Integração com gates

- Durante stages, `SimulationGate`/InputMode podem ficar bloqueados:
  - IntroStage segura `sim.gameplay` até completar
  - PostLevel segura progressão/troca de level até completar

### Ordem

- Macro enter gameplay:
  - macro pipeline conclui e abre cortina
  - **então** LevelStageOrchestrator roda IntroStage se existir
- LevelSwap:
  - se `allowCurtainIn/out`: pode fechar cortina local, trocar conteúdo, abrir
  - roda IntroStage do novo level se existir

## Implicações

- MacroRoute fica “limpa”: define só espaço macro, transição macro e política de reset.
- Levels definem experiência específica (intro/post, gating local).
- Melhor alinhamento com seu objetivo: “intro/post é do level”.

## Alternativas consideradas

1) **Manter IntroStage no macro e parametrizar por level**  
Rejeitado: macro continuaria dependente de lógica específica de gameplay.

## Critérios de aceite (DoD)

- Logs [OBS] de IntroStage referenciam levelId/contentId (domínio Level), não route macro.
- IntroStage não executa em macros sem levels (ex.: Menu).
- Baseline 3.0 inclui evidência:
  - “Macro abriu cortina” → “IntroStage (level)” → “Gameplay unblocked”.

## Referências

- ADR-0015 — Baseline 2.0 fechamento (histórico)
- ADR-0020 — MacroRoutes vs Levels
- ADR-0021 — Baseline 3.0
