# Plan: Macro Baseline vs Local Phase

## 1. Objetivo
Congelar a intenção arquitetural atual antes de novos refactors, separando `Macro / Baseline` de `Local` e tratando `Phase` como a primeira subárea local em isolamento.

## 2. Intenção arquitetural

### Macro / Baseline
- Define o contexto macro, a composição de alto nível, os contratos globais e os gates de baseline.
- Possui navegação macro, reset macro, boot e observabilidade estrutural.
- Não resolve presenter local, stage local ou regras locais de phase.

### Local / Phase
- É a primeira subárea local sendo isolada agora.
- Possui o ciclo semântico da phase, sua projeção operacional e os handoffs locais permitidos.
- Não é sinônimo de `Local` inteiro.

### Local / Actors-Entities
- Futuro.
- Deve permanecer fora deste corte.
- Owns:
  - spawn/despawn
  - registro de atores e entidades
  - ciclo de vida local
  - composição local do mundo jogável
  - estado local das entidades
- `Phase` não absorve esse ownership.

### Local / Gameplay
- Futuro.
- Deve permanecer fora deste corte.
- Owns:
  - regras
  - objetivos
  - fluxo jogável
  - validações de execução
  - interação entre players e entidades
- `Phase` não absorve esse ownership.

## 3. Escopo desta etapa
- Isolar `Phase` como boundary local inicial.
- Consolidar o canônico operacional atual em torno de `Gameplay Runtime Composition` e `GameplaySessionFlow`.
- Ficam fora:
  - `Actors-Entities`
  - `Gameplay` como subárea local própria
  - qualquer expansão de `Phase` para cobrir o Local inteiro

## 4. Ownership por área

| área | owns | does not own |
|---|---|---|
| Macro / Baseline | composição macro, gates globais, reset macro, boot, observabilidade estrutural | presenter local, stage local, regras locais de phase |
| Local / Phase | contrato semântico da phase, projeção local da phase, handoffs phase-owned | Actors-Entities, Gameplay, baseline macro |
| Local / Actors-Entities | spawn/despawn, registro de atores e entidades, ciclo de vida local, composição local do mundo jogável, estado local das entidades | phase pipeline, macro baseline, presenter local de phase |
| Local / Gameplay | regras, objetivos, fluxo jogável, validações de execução, interação entre players e entidades | baseline macro, contrato semântico de phase, ownership de entidades |

## 5. Boundary points oficiais
- Macro -> Local/Phase:
  - `PhaseDefinitionSelected`
  - `PhaseContentApplied`
  - `PhaseDerivationCompleted`
  - `SceneTransitionCompleted`
- Local/Phase -> Macro:
  - `IntroStageCompleted`
  - `RunResultStageCompleted`
  - `RunDecision...` ou o handoff terminal canônico
- Novos boundaries só entram se tiverem owner único e motivo explícito.

## 6. Proibições explícitas
- `Phase` não pode virar fachada de `Actors-Entities`.
- `Phase` não pode virar fachada de `Gameplay`.
- `Macro` não decide presenter/stage local.
- Bridge transitório não pode permanecer como forma final.
- Compatibilidade histórica não pode redefinir ownership.

## 7. Determinismo e observabilidade
- Todo boundary oficial deve ser determinístico e observável.
- Logs e contratos devem refletir o owner real.
- Não pode haver duplicidade semântica entre Macro e Local.
- Qualquer label observável precisa apontar para o boundary correto.

## 8. Critérios de aceite arquiteturais
- Boundaries nomeados e operacionais estão explícitos.
- Ownership único por área.
- `Phase` não absorve `Actors-Entities` nem `Gameplay`.
- Observabilidade está coerente com os owners reais.
- Bridge histórico não aparece como forma final.

## 9. Ordem recomendada dos próximos cortes
1. Fechar o boundary físico de `Phase`.
2. Isolar `Actors-Entities` como subárea local própria.
3. Isolar `Gameplay` como subárea local própria.
4. Revisar bridges transitórios para reduzir compatibilidade histórica.
5. Consolidar observabilidade final por owner.
