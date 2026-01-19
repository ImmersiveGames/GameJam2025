# ADR-0018 — ContentSwap (Phase) — Contrato, Observability e Compatibilidade

## Status
- Estado: Aceito
- Data: 2026-01-18
- Escopo: ContentSwap (Phase) + Observability (NewScripts)

## Contexto

O termo **Phase** foi usado para representar **troca de conteúdo** no runtime, com dois modos canônicos (In-Place e SceneTransition). A evolução do Baseline 2.2 introduz **Level/Nível** como progressão do jogo, criando ambiguidade entre “fase” (conteúdo) e “nível” (progressão).

Para eliminar essa ambiguidade, este ADR redefine **Phase** como **ContentSwap** (troca de conteúdo). A nomenclatura de código permanece por compatibilidade: APIs e eventos atuais continuam válidos. A progressão de nível passa a ser orquestrada pelo **Level Manager** (ADR-0019).

> **Nota crítica:** ContentSwap **não** é responsável por IntroStage. IntroStage é responsabilidade do Level Manager.

## Decisão

### 1) Semântica canônica
- **Phase == ContentSwap**: troca de conteúdo do runtime (mesmo contexto ou via SceneFlow).
- **Level/Nível**: progressão do jogo, orquestra ContentSwap + IntroStage.

### 2) Contratos públicos (mantidos)
Os contratos abaixo **permanecem válidos** e são o ponto de integração pública:
- `IPhaseChangeService`
- `PhasePlan`
- `PhaseChangeMode` (`InPlace`, `SceneTransition`)
- `PhaseChangeOptions`
- `IPhaseContextService`
- `IPhaseTransitionIntentRegistry`

> Se houver renomeação futura para ContentSwap no código, **devem existir aliases/bridges** compatíveis para não quebrar build nem chamadas existentes.

### 3) Modos canônicos (referência)
- Os **modos de ContentSwap** são definidos no ADR-0017 (“modes”).
- **In-Place**: troca de conteúdo sem SceneFlow.
- **WithTransition** (SceneTransition): troca de conteúdo com SceneFlow + intent registry.

### 4) Reasons e logs canônicos
- **Reasons canônicos (recomendado)**:
    - `QA/ContentSwap/InPlace/<case>`
    - `QA/ContentSwap/WithTransition/<case>`
    - `ContentSwap/InPlace/<source>`
    - `ContentSwap/WithTransition/<source>`
- **Legacy aceito (compatibilidade)**:
    - `QA/Phases/InPlace/<...>`
    - `QA/Phases/WithTransition/<...>`

- **Logs canônicos** seguem os eventos existentes e **incluem alias explícito**:
    - `[OBS][Phase] PhaseChangeRequested ...`
    - `[OBS][ContentSwap] ContentSwapRequested ...`
    - `[PhaseContext] PhasePendingSet ...`
    - `[PhaseContext] PhaseCommitted ...`

### 5) IntroStage fora do escopo
- **IntroStage não é responsabilidade do ContentSwap**.
- Para evitar ambiguidade de QA, ContentSwap **não** deve forçar IntroStage.
- O Level Manager (ADR-0019) é quem decide política e execução de IntroStage.

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
- ADR-0019 — Level Manager (progressão) + gates Baseline 2.2
- ADR-0016 — Phases + IntroStage opcional (histórico do contrato)
- Observability Contract — `Docs/Reports/Observability-Contract.md`
