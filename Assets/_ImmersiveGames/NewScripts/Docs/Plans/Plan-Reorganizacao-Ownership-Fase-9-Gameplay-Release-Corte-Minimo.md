# Plano de Corte Mínimo - Fase 9 / Gameplay Release

## Objetivo
Clarificar a fase 9 como o ponto em que a gameplay fica realmente liberada para operar/jogar, sem redesenhar o backbone inteiro e sem reabrir fases 4, 5, 6, 7 ou 8.

## Escopo
- Clarificar a menor leitura correta da fase 9
- Fixar o owner semantico mais coerente da liberacao jogavel real
- Separar claramente:
  - readiness tecnico
  - gate operacional
  - transicao de intro
  - estado final jogavel
- Reduzir a mistura entre `GameReadinessService`, `IntroStageCoordinator`, `GameLoopService` e `GameplayStateGate`
- Preparar apenas o corte minimo necessario para tornar a fase 9 legivel

## Fora do escopo
- Reabrir fases 4, 5, 6, 7 ou 8
- Redesenhar o backbone inteiro
- Criar adapter, bridge ou wrapper so para mascarar boundary ruim
- Fazer reorganizacao fisica ampla de pastas
- Propor implementacao detalhada agora
- Criar solucao temporaria que vire permanente

## Leitura atual consolidada
- `ReadinessSnapshot` representa readiness tecnico, nao liberacao jogavel final
- `GameReadinessService` consolida o estado tecnico de readiness e gate
- `IntroStageCoordinator` bloqueia e libera o enter-stage operacionalmente
- `GameLoopService` em `Playing` e o melhor candidato a sinal final de gameplay liberada
- `GameplayStateGate` consome readiness, gate e estado do loop; nao deve ser owner da fase 9

## Corte minimo proposto
### 1. `GameLoopService` como sinal canonico da fase 9
- Tratar `Playing` como o ponto final de liberacao real da gameplay
- Tratar `GameRunStartedEvent` como consequencia operacional dessa liberacao
- Evitar que outros servicos parecam decidir a liberacao jogavel final

### 2. `GameReadinessService` como readiness tecnico
- Manter como consumidor de `SceneTransition` e publicador de `ReadinessSnapshot`
- Limitar sua semantica a pronto tecnico / gate tecnico
- Nao expandir para owner da liberacao jogavel final

### 3. `IntroStageCoordinator` como gate operacional do enter-stage
- Manter o bloqueio/liberacao da simulacao durante o intro
- Tratar `BeginIntroStage` / `CompleteIntroStage` / `SkipIntroStage` como mecanismo operacional do enter-stage
- Nao atribuir ao coordinator a semantica final de "jogo liberado"

### 4. `GameplayStateGate` como consumidor
- Manter como combinacao de sinais para consumo de gameplay
- Nao permitir que ele vire owner semantico da fase 9
- Tratar sua logica como leitura derivada, nao como autoridade final

## Onde hoje ha mistura indevida
- `GameReadinessService` cruza readiness tecnico com a ideia de "jogavel" ao publicar `GameplayReady`
- `IntroStageCoordinator` mistura gate operacional do enter-stage com a transicao pratica para a liberacao
- `GameLoopService` recebe o handoff do intro e ao mesmo tempo marca `Playing`, entao ele e o melhor candidato a owner final, mas ainda precisa ficar conceitualmente separado dos sinais anteriores
- `GameplayStateGate` mistura consumo de readiness, gate e `GameLoop`, o que e aceitavel como consumo, mas nao como ownership

## O que deve mudar agora
- Formalizar `Playing` como o sinal canonico da liberacao real de gameplay
- Tratar `GameReadinessService` como readiness tecnico, sem semantica final de liberacao
- Tratar `IntroStageCoordinator` como gate operacional do enter-stage
- Reduzir a leitura da fase 9 para uma cadeia clara:
  - readiness tecnico
  - gate operacional
  - intro concluido
  - `Playing` como liberacao final

## O que deve ficar explicitamente adiado
- Hardening de `GameplayStateGate`
- Reescrita da publicacao de readiness snapshots
- Dedupe estrutural entre readiness e enter-stage
- Qualquer simplificacao maior do game loop
- Consolidacao fisica dos modulos de leitura/jogo

## Ordem de execucao minima
1. Fixar conceitualmente `GameLoopService` como ponto final da liberacao jogavel real
2. Manter `GameReadinessService` restrito a readiness tecnico
3. Manter `IntroStageCoordinator` restrito ao gate operacional do enter-stage
4. Validar `GameplayStateGate` apenas como consumidor derivado
5. Registrar hardening futuro separado, sem implementacao agora

## Riscos
- Tentar mover o ownership da fase 9 para um novo contrato e criar remendo
- Fazer `GameReadinessService` virar autoridade final de gameplay liberada
- Confundir gate operacional com liberacao jogavel real
- Deixar `GameplayStateGate` parecer owner da fase 9
- Reabrir indiretamente fases 4, 5, 6, 7 ou 8 por dependencia de boundary

## Critérios de pronto
- A fase 9 tem uma leitura unica e curta: readiness tecnico + gate operacional + intro concluido culminam em `Playing`
- `GameLoopService` e o melhor candidato a owner semantico da liberacao jogavel real
- `GameReadinessService` permanece como readiness tecnico
- `IntroStageCoordinator` permanece como gate operacional do enter-stage
- `GameplayStateGate` permanece como consumidor
- Nao ha adapter/bridge/wrapper sustentando boundary errado
- Nao ha reabertura das fases 4, 5, 6, 7 ou 8

## Status consolidado apos a execucao

### Fase 9 - Gameplay Release
- Status: **congelada com ressalva pequena**
- Leitura: correta e validada por log
- Observacao principal:
  - `GameLoopService` / estado `Playing` ficou como **sinal canonico final** da liberacao real de gameplay
- Observacoes complementares:
  - `GameReadinessService` permanece como **readiness tecnico**
  - `IntroStageCoordinator` permanece como **gate operacional do enter-stage**
  - `GameplayStateGate` permanece como **consumidor derivado**
  - a cadeia canonica fica:
    - readiness tecnico
    - gate operacional
    - intro concluido
    - `Playing` como liberacao final

### Ressalva R4
- **R4 - hardening futuro da cadeia distribuida de Gameplay Release**
- Descricao:
  - a fase 9 ficou correta, mas a cadeia ainda e distribuida entre `GameReadinessService`, `IntroStageCoordinator` e `GameLoopService`
  - isso e aceitavel no estado atual
  - nao e blocker arquitetural atual

### Quando retomar R4
- quando houver etapa de hardening de boundary da fase 9
- ou se `GameReadinessService`, `IntroStageCoordinator` ou `GameplayStateGate` voltarem a crescer de modo que enfraquecam `Playing` como sinal canonico final
- ou se a cadeia distribuida comecar a gerar confusao recorrente de manutencao

### Regra para retomada futura
- nao mover so chamadas de lugar
- corrigir ownership semantico de verdade
- nao aceitar adapter/bridge/wrapper so para mascarar boundary ruim
- preservar `Playing` como sinal canonico final da liberacao jogavel real

### Conclusao consolidada
- a **fase 9 esta fechada por agora**
- a pendencia aberta e:
  - **R4**: hardening futuro da cadeia distribuida de gameplay release
- R4 e pendencia de qualidade arquitetural, nao blocker atual
