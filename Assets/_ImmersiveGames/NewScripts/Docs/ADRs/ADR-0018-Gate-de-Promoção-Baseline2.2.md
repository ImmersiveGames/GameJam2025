# ADR-0018 — Mudança de semântica: Phase => ContentSwap + Level/Phase Manager

## Status
- Estado: Aceito
- Data: 2026-01-18
- Escopo: ContentSwap (Phase) + Level/Phase Manager (NewScripts)

## Contexto

Historicamente, o termo **Phase** foi usado para dois significados diferentes:

- **Troca de conteúdo** do runtime (troca de fase dentro da mesma cena ou via SceneFlow).
- **Progressão de nível/fase do jogo** (o “capítulo” ou estágio de gameplay).

Essa ambiguidade gera problemas práticos:

- Documentação e QA ficam inconsistentes ("phase" pode significar troca de conteúdo ou progresso de nível).
- `reason`/logs perdem precisão semântica, enfraquecendo evidências e diagnósticos.
- Roadmap confunde *executor técnico* (troca de conteúdo) com *orquestração de progressão*.

## Decisão

### 1) Termos formais e boundaries

- **ContentSwap** = módulo **exclusivo** para trocar conteúdo no runtime.
  - Executa o reset e o commit de conteúdo (in-place ou com SceneFlow).
  - É a camada técnica (executor) e continua exposta via `IPhaseChangeService`.
- **Level/Phase Manager** = **orquestrador** da progressão de níveis/fases do jogo.
  - Decide quando avançar/retroceder de nível.
  - Usa ContentSwap por baixo.
  - É responsável por **sempre disparar IntroStage** ao entrar em um nível (neste ciclo).
- **Phase** passa a ser termo técnico associado ao ContentSwap (compatível com contratos existentes).

### 2) Contratos públicos (mantidos)

Os contratos abaixo **permanecem válidos** e são o ponto de integração pública do ContentSwap:

- `IPhaseChangeService`
- `PhasePlan`
- `PhaseChangeMode` (`InPlace`, `SceneTransition`)
- `PhaseChangeOptions`
- `IPhaseContextService`
- `IPhaseTransitionIntentRegistry`

> Se houver renomeação futura para ContentSwap no código, **devem existir aliases/bridges** compatíveis para não quebrar build nem chamadas existentes.

### 3) Contratos públicos (novo Level/Phase Manager)

- `ILevelManager`
- `LevelPlan`
- `LevelChangeOptions`

O Level/Phase Manager reutiliza o ContentSwap existente e **sempre** executa IntroStage após mudança de nível neste ciclo.

### 4) Relação com ADR-0017 (modos)

- ADR-0017 define os **modos canônicos** de ContentSwap:
  - **In-Place**: troca de conteúdo sem SceneFlow.
  - **SceneTransition**: troca com SceneFlow + intent registry.
- ADR-0018 **não altera** o escopo do ADR-0017; apenas reposiciona a semântica de “Phase”.

## Consequências

### Benefícios
- Elimina ambiguidade entre “fase” (conteúdo) e “nível” (progressão).
- Mantém compatibilidade com APIs atuais (`PhaseChangeService`).
- Isola responsabilidades (ContentSwap vs Level/Phase Manager), alinhando ao princípio de responsabilidade única (SRP).

### Trade-offs / Riscos
- Exige disciplina documental para não reintroduzir “Phase” como sinônimo de nível.
- A mudança é **semântica**: não exige refactor imediato de código além do necessário para o Baseline 2.2.

## Evidências
- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica: `Docs/Reports/Evidence/LATEST.md`

## Referências
- ADR-0017 — Tipos de troca de fase (ContentSwap: In-Place vs SceneTransition)
- ADR-0019 — Promoção/fechamento do Baseline 2.2
- ADR-0016 — Phases + IntroStage opcional (histórico do contrato)
- Observability Contract — `Docs/Reports/Observability-Contract.md`
