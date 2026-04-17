# ADR-0058 - Actors como bloco semantico acima da Base 1.0

## Status
- Estado: Aceito
- Data: 2026-04-17
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## 1. Contexto

`ADR-0055` congela `Session Integration` como seam explicito acima do baseline.
`ADR-0056` congela o Baseline 4.0 como executor tecnico fino.
`ADR-0057` congela a `Base 1.0` como leitura sistemica composta entre baseline tecnico, `Session Integration` e camadas semanticas acima.

Dentro dessa leitura composta, o eixo de `Actors` ja existe de forma real, mas ainda esta fisicamente espalhado entre:

- `Game/Gameplay/Actors`
- `Spawn`
- `GameplayReset`
- `SessionIntegration`
- `Experience/GameplayCamera`
- `InputModes`
- `Infrastructure/Composition`

O sistema nao precisa de uma nova arquitetura base.
Precisa de um bloco semantico explicito para que o eixo de `Actors` pare de ser lido como costura oportunista entre owners operacionais e semanticos.

## 2. Problema

Sem um bloco semantico proprio, `Actors` tende a ser confundido com:

- registry operacional de vivos
- spawn e despawn
- reset operacional
- binding de input e camera
- bootstrap de cena
- infraestrutura genérica de ids

Essa mistura reabre leituras erradas como:

- `ActorRegistry` como owner semantico
- spawn como definidor de significado
- reset como owner de presencia
- `SessionIntegration` como dono do bloco de actors
- input/camera como semantica de actor

O resultado e um eixo que existe, mas nao tem fronteira arquitetural formal.

## 3. Decisao

Adota-se **Actors** como um **bloco semantico proprio acima da Base 1.0**.

O bloco de `Actors` responde por:

- identidade de actor
- tipo / role de actor
- presenca semantica
- mapeamento participacao -> actor
- regras canonicas de leitura sobre quem e o actor relevante

O bloco de `Actors` nao e um novo executor operacional.
Ele e o lugar onde a leitura semantica do eixo fica canonica e auditavel.

## 4. O que `Actors` nao e

`Actors` nao e owner de:

- spawn
- reset
- input
- camera
- `ActorRegistry`
- bootstrap de cena
- infraestrutura genérica de ids

`Actors` pode conversar com esses dominios, mas nao deve absorve-los.

## 5. Ownerships e boundaries

### Ownerships que permanecem

- `GameplayParticipationFlowService` continua sendo owner semantico de `who participates`, `primary participant` e `local participant`.
- `ActorRegistry` continua sendo source of truth operacional de atores vivos na cena.
- spawn services continuam sendo owners operacionais de criacao, destruicao e registro de actors.
- reset continua sendo operacional.
- `SessionIntegration` continua sendo o seam canonico de entrada e conversa entre semantica e execucao.
- input e camera continuam sendo binders/locators operacionais.

### Fronteira do novo bloco

`Actors` fica acima do baseline e abaixo dos consumidores operacionais adjacentes.

Ele:

- consome `SessionIntegration`
- orienta semanticamente spawn e reset
- usa `ActorRegistry` como truth source operacional
- nao executa spawn/reset diretamente
- nao assume ownership de input/camera

## 6. Relacao com os modulos adjacentes

### Participation

Participacao continua definida em `GameplayParticipationFlowService`.
`Actors` consome essa verdade para resolver quem e o actor relevante em cada contexto.

### `SessionIntegration`

`SessionIntegration` e a porta correta de entrada/conversa.
Ele transporta contexto canonico entre semantica e operacao.
`Actors` nao substitui esse seam e nao deve virar o seam.

### Spawn

Spawn materializa.
`Actors` pode orientar semanticamente quem deve ser materializado, mas a execucao continua no layer de spawn.

### Reset

Reset reorganiza o estado operacional do conjunto de actors.
`Actors` define a leitura semantica que pode alimentar reset, mas nao executa a pipeline.

### `ActorRegistry`

`ActorRegistry` e o registro operacional de atores vivos.
Ele e consultado por `Actors`, mas nao define significado semantico.

### Input

Input continua sendo ponte operacional para o actor vivo.
`PlayerInputLocator` permanece no lado operacional.

### Camera

Camera continua sendo binding operacional por actor/player.
`GameplayCameraBinder` permanece do lado operacional.

### Futuros binders / interactions

Futuros binders e interactions devem entrar como adaptadores ou extensoes do bloco `Actors`, desde que nao assumam ownership de spawn, reset ou do seam `SessionIntegration`.

## 7. Misturas atuais que este ADR quer corrigir

O bloco de `Actors` existe hoje de forma espalhada e alguns pontos ainda carregam ambiguidade:

- `PlayerSpawnService` mistura spawn com leitura de participacao como bridge operacional
- `PlayerActorGroupGameplayResetWorldParticipant` mistura reset de `Players` com bridge semantico-operacional
- `ActorGroupGameplayResetSceneScanDiscoveryStrategy` ainda funciona como fallback operacional historico e nao deve ser lido como shape principal

Esses pontos nao sao erros por si mesmos.
Eles sao sinais de que falta a fronteira semantica formal de `Actors`.

## 8. Consequencias

### Positivas

- O eixo de `Actors` passa a ter leitura arquitetural propria.
- `ActorRegistry` deixa de ser confundido com owner semantico.
- Spawn, reset, input e camera permanecem no lugar certo.
- Fica mais facil auditar quem e o actor relevante em cada fluxo.
- Fica mais facil abrir a proxima frente funcional sem reabrir a Base 1.0.

### Trade-offs

- A consolidacao fisica do eixo ainda pode continuar espalhada por um tempo.
- Este ADR nao exige mover tudo para um unico bucket operacional.
- Alguns bridges e fallback operacionais continuam existindo ate haver substitutos claros.

## 9. Ordem recomendada de evolucao

1. Formalizar a fronteira semantica de `Actors`.
2. Definir o contrato canonico de leitura entre `SessionIntegration` e `Actors`.
3. Consolidar a ponte spawn/reset em torno desse contrato.
4. Revisar binders adjacentes apenas se houver ganho real.
5. Manter `ActorGroupGameplayResetSceneScanDiscoveryStrategy` como fallback ate existir substituto claro.

## 10. Relacao com ADRs anteriores

Este ADR nao substitui os contratos anteriores.
Ele os consome e os estende na fronteira correta:

- `ADR-0055`: continua owner do seam de integracao semantica de sessao
- `ADR-0056`: continua owner do baseline tecnico fino
- `ADR-0057`: continua owner da leitura sistemica composta da Base 1.0

`ADR-0058` adiciona apenas o bloco semantico faltante para o eixo de `Actors`.

## 11. Fechamento

`Actors` passa a ser lido como bloco semantico proprio acima da Base 1.0.
`ActorRegistry` continua operacional.
`SessionIntegration` continua sendo o seam de entrada.
Spawn, reset, input e camera continuam operacionais.

O objetivo deste ADR e impedir que o eixo de `Actors` volte a ser interpretado como costura difusa entre ownerships adjacentes.
