# ADR-0018 — ContentSwap (Phase) — Contrato e Observability

## Status
- Estado: Aceito
- Data: 2026-01-18
- Escopo: ContentSwap (Phase) + Observability (NewScripts)

## Contexto

Na arquitetura atual, o termo **Phase** foi usado para representar **troca de conteúdo** (swap) dentro do runtime, com dois modos canônicos (In-Place e SceneTransition). Essa semântica ficou ambígua com a evolução do Baseline 2.2, que introduz o conceito de **Level/Nível** como progressão do jogo.

Para evitar confusão entre **conteúdo** (o que está carregado) e **progressão** (qual nível está ativo), este ADR redefine *Phase* como **ContentSwap**. O código permanece compatível: contratos públicos e eventos atuais continuam válidos, e a nomenclatura “Phase” segue existindo por compatibilidade.

> **Nota:** IntroStage não é responsabilidade do ContentSwap. IntroStage é orquestrada pelo Level Manager (ver ADR-0019).

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

Se houver renomeação futura para ContentSwap no código, **devem existir aliases/bridges** compatíveis para não quebrar build nem chamadas existentes.

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
- **Legacy aceito (compatibilidade)**: `QA/Phases/InPlace/...` e `QA/Phases/WithTransition/...` continuam válidos enquanto houver evidências anteriores.
- **Logs canônicos** seguem os eventos existentes:
    - `[OBS][Phase] PhaseChangeRequested ... mode=InPlace ...`
    - `[OBS][Phase] PhaseChangeRequested ... mode=SceneTransition ...`
    - `[PhaseContext] PhasePendingSet ...`
    - `[PhaseContext] PhaseCommitted ...`

### 5) Responsabilidade explícita (não escopo)
- **IntroStage não é responsabilidade do ContentSwap**.
- IntroStage deve ser orquestrada por **Level Manager** (ADR-0019), que decide a política de execução por nível.

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
- ADR-0016 — Phases + IntroStage opcional (histórico do contrato)
- Observability Contract — `Docs/Reports/Observability-Contract.md`
