# ADR-0018 — Mudança de semântica: Phase => ContentSwap + introdução do LevelManager

## Status
- Estado: Aceito
- Data: 2026-01-18
- Escopo: ContentSwap (Phase) + LevelManager (NewScripts)

## Contexto

O termo **Phase** foi usado para representar **troca de conteúdo** no runtime, com dois modos canônicos (In-Place e SceneTransition). A evolução do Baseline 2.2 introduz **Level/Nível** como progressão do jogo, criando ambiguidade entre “fase” (conteúdo) e “nível” (progressão).

Este ADR formaliza a **mudança de semântica**: Phase passa a significar **ContentSwap** e a progressão de nível passa a ser orquestrada pelo **LevelManager**.

> **Nota crítica:** ContentSwap **não** é responsável por IntroStage. IntroStage é responsabilidade do LevelManager.

## Decisão

### 1) Semântica canônica
- **Phase == ContentSwap**: troca de conteúdo do runtime (mesmo contexto ou via SceneFlow).
- **Level/Nível**: progressão do jogo, orquestra ContentSwap + IntroStage.

### 2) Contratos públicos (mantidos)
Os contratos abaixo **permanecem válidos** e são o ponto de integração pública do ContentSwap:
- `IPhaseChangeService`
- `PhasePlan`
- `PhaseChangeMode` (`InPlace`, `SceneTransition`)
- `PhaseChangeOptions`
- `IPhaseContextService`
- `IPhaseTransitionIntentRegistry`

> Se houver renomeação futura para ContentSwap no código, **devem existir aliases/bridges** compatíveis para não quebrar build nem chamadas existentes.

### 3) Contratos públicos (novo LevelManager)
- `ILevelManager`
- `LevelPlan`
- `LevelChangeOptions`

O LevelManager reutiliza o ContentSwap existente e **sempre** executa IntroStage após mudança de nível neste ciclo.

### 4) Modos canônicos (referência)
- Os **modos de ContentSwap** são definidos no ADR-0017 (“modes”).
- **In-Place**: troca de conteúdo sem SceneFlow.
- **WithTransition** (SceneTransition): troca de conteúdo com SceneFlow + intent registry.

### 5) Reasons e logs canônicos
- **Reasons canônicos (recomendado)**:
    - `QA/ContentSwap/InPlace/<case>`
    - `QA/ContentSwap/WithTransition/<case>`
    - `ContentSwap/InPlace/<source>`
    - `ContentSwap/WithTransition/<source>`
    - `LevelChange/<source>`
    - `QA/Levels/InPlace/<case>`
    - `QA/Levels/WithTransition/<case>`
- **Legacy aceito (compatibilidade)**:
    - `QA/Phases/InPlace/<...>`
    - `QA/Phases/WithTransition/<...>`

- **Logs canônicos** seguem os eventos existentes e **incluem alias explícito**:
    - `[OBS][Phase] PhaseChangeRequested ...`
    - `[OBS][ContentSwap] ContentSwapRequested ...`
    - `[OBS][Level] LevelChangeRequested ...`
    - `[OBS][Level] LevelChangeStarted ...`
    - `[OBS][Level] LevelChangeCompleted ...`
    - `[PhaseContext] PhasePendingSet ...`
    - `[PhaseContext] PhaseCommitted ...`

### 6) IntroStage fora do escopo do ContentSwap
- **IntroStage não é responsabilidade do ContentSwap**.
- O LevelManager é quem decide política e execução de IntroStage.

## Consequências

### Benefícios
- Elimina ambiguidade entre “fases” de conteúdo e “níveis” de progressão.
- Mantém compatibilidade com APIs atuais.
- Observability continua baseada nas mesmas assinaturas/logs já evidenciadas.

### Trade-offs / Riscos
- Exige disciplina documental para não reintroduzir “Phase” como progressão de nível.

## Evidências
- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica: `Docs/Reports/Evidence/LATEST.md`
- Snapshot vigente: `Docs/Reports/Evidence/2026-01-18/Baseline-2.1-Evidence-2026-01-18.md`

## Referências
- ADR-0017 — Tipos de troca de fase (modos In-Place vs SceneTransition)
- ADR-0019 — Promoção/fechamento do Baseline 2.2
- ADR-0016 — Phases + IntroStage opcional (histórico do contrato)
- Observability Contract — `Docs/Reports/Observability-Contract.md`
