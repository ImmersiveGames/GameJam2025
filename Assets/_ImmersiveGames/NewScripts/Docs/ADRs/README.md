# ADRs

Este diretorio mantem o acervo de ADRs e sua precedencia normativa.

## Base 1.1 viva

A partir do freeze de Base 1.1, estes ADRs permanecem vivos na pasta principal:

1. `ADR-0060-Base-1.1-Pipeline-Convergence-e-Identidade-Explicita.md`
2. `ADR-0061-Run-Pipeline-Canonico-e-Substituicao-do-Conceito-de-Macro.md`
3. `ADR-0062-Session-Pipeline-Canonico-e-Substituicao-do-Conceito-de-Local.md`
4. `ADR-0063-Modules-Produzem-Fatos-ou-Comandos-e-Adapters-Executam-Side-Effects.md`
5. `ADR-0064-IntroStage-como-Activation-Stage-e-Policy-de-Entrada.md`
6. `ADR-0065-Deactivation-e-Continuity-RunResult-RunDecision-PostRun.md`
7. `ADR-0066-Gates-InputModes-e-GameLoop-como-Executores-de-Estado-e-Efeitos.md`
8. `ADR-0067-Identidade-Explicita-de-Ciclo-e-Isolamento-Contra-Eventos-Foreign.md`
9. `ADR-0068-SceneRouteProfile-e-Rebaixamento-de-SceneFlow-Navigation-para-Pipeline-Adapter.md`
10. `ADR-0069-SessionTransitionEnvelope-e-SessionOperationalSetup-Base-1.1.md`
11. `ADR-0070-Base-1.1-Rail-Canonico-de-Rotas-Loading-Fade-e-Handoff-do-Base11Sandbox.md`

## Precedencia normativa atual

Em decisoes de arquitetura e ownership, prevalecem:

1. `ADR-0060`
2. `ADR-0061`
3. `ADR-0062`
4. `ADR-0063`
5. `ADR-0064`
6. `ADR-0065`
7. `ADR-0066`
8. `ADR-0067`
9. `ADR-0068`
10. `ADR-0069`
11. `ADR-0070`

Regra obrigatoria:

- ADRs anteriores devem ser lidos como historicos, exceto quando explicitamente referenciados por estes ADRs normativos.
- Em caso de conflito, prevalece o ADR mais novo e/ou explicitamente normativo da Base 1.1.
- Ownership nao e decidido por conveniencia operacional, e sim pelo papel arquitetural definido na Base 1.1.
- foreign/stale events nao podem alterar o pipeline ativo.

## Classificacao normativa do acervo

### NORMATIVO_ATUAL

- `ADR-0060`
- `ADR-0061`
- `ADR-0062`
- `ADR-0063`
- `ADR-0064`
- `ADR-0065`
- `ADR-0066`
- `ADR-0067`
- `ADR-0068`
- `ADR-0069`
- `ADR-0070`

### HISTORICO (nao normativo para ownership)

Todo ADR anterior a `ADR-0060` foi movido para `Docs/ADRs/Historico/` e deve ser lido como referencia historica, nao como fonte normativa viva.

## Regra de uso rapido

Se um ADR historico conflitar com a Base 1.1:

- nao abrir excecao local;
- nao usar compatibilidade narrativa;
- aplicar a precedencia normativa deste indice.

## Checkpoint Base11Sandbox congelado

O checkpoint validado da Base 1.1 para o sandbox minimo e:

- `Base11Sandbox Minimal Route + Session Activity Cycle - PASS`
- `Base11Sandbox rail canonico de rotas/loading/fade/handoff congelado`

Referencias de materializacao:

- `Docs/ADRs/ADR-0070-Base-1.1-Rail-Canonico-de-Rotas-Loading-Fade-e-Handoff-do-Base11Sandbox.md`
- `Docs/ADRs/Base-1.1-Consolidado-Atualizado-Base11Sandbox.md`
- `Docs/ADRs/MiniADRs/MiniADR-Base11Sandbox-Operational-Routing-and-SessionActivity-Handoff.md`
- `Docs/ADRs/Base-1.1-Matriz-Inicial-de-Migracao.md`
- `Docs/ADRs/Base-1.1-Plano-de-Migracao-Pipeline-Convergence.md`
