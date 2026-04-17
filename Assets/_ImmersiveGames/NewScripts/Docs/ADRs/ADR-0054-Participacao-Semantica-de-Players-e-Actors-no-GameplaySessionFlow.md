# ADR-0054 - Bloco semantico de participacao de players e actors no GameplaySessionFlow

## Status
- Estado: Aceito
- Data: 2026-04-17
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## 1. Contexto

O dominio de players hoje esta espalhado entre `Phase.Players`, `GameplayPhaseFlowService`, `ActorRegistry`, spawn, reset e readiness.

Esse arranjo funciona no baseline atual, mas nao e suficiente como contrato semantico para a sessao jogavel. Falta um owner claro para participacao, lifecycle de participante, identidade separada de `ActorId`, ownership kind em runtime e binding hints para consumo adjacente por `InputModes`.

O problema nao e apenas ownership semantico. Ha tambem uma questao de posicao no pipeline: o bloco de participacao nasce no layer acima do baseline e entra como parte da preparacao semantica da sessao jogavel, antes da liberacao efetiva do gameplay.

O objetivo deste ADR e congelar o desenho do bloco semantico de participacao como evolucao do shape atual de `GameplaySessionFlow`, sem transforma-lo em `Phase.Players 2.0` nem absorver a etapa posterior de binding/interacao com o palco ja composto.

## 2. Decisao

Adota-se um bloco semantico de participacao de players e actors, integrado ao `GameplaySessionFlow` como owner do roster semantico da sessao jogavel.

Esse bloco pertence ao layer semantico acima do baseline e participa do fluxo de preparacao da sessao jogavel.

Esse bloco:

- centraliza roster semantico de participantes
- centraliza identidade de participante
- centraliza primary/local participant
- centraliza ownership kind em runtime
- centraliza readiness semantica do proprio bloco
- publica binding hints para seams adjacentes
- expoe snapshot e assinatura de participacao para observabilidade e gating
- responde por quem participa, nao por como o palco conecta esses participantes

Esse bloco nao:

- nao e owner de spawn
- nao e owner de reset
- nao e owner de `PlayerInput`
- nao e owner de movement/gameplay behavior
- nao substitui `ActorRegistry`
- nao assume multiplayer completo como requisito de implementacao imediata
- nao resolve wiring de gameplay com o palco ja montado

## 3. Source of truth

### 3.1 Roster semantico

A fonte de verdade do roster semantico e o bloco de participacao.

`Phase.Players` continua sendo fonte autoral de configuracao da phase, mas nao e owner runtime do roster.

### 3.2 Participantes vivos vs atores vivos

O bloco de participacao responde por participantes semanticamente vigentes na sessao.

`ActorRegistry` continua sendo a fonte de verdade dos atores vivos em cena.

Participante vivo e ator vivo sao dominios relacionados, mas nao identicos:

- participante vivo: entidade semantica da sessao
- ator vivo: instancia operacional materializada em cena

### 3.3 Primary/local participant

O primary/local participant e definido pelo bloco de participacao.

Ele nao e inferido por `ActorId`, por spawn ou por registry.

### 3.4 Ownership kind

Ownership kind e contrato runtime do bloco de participacao.

Ele nao deve ficar implicito em `ActorKind` nem em `PhasePlayerRole` sozinho.

## 4. Lifecycle

O bloco deve tratar lifecycle de participante como contrato minimo explicito.

Estados conceituais minimos:

- `Declared`: participante existe no roster semanticamente derivado
- `Expected`: participante e esperado para esta sessao/phase
- `Materialized`: participante possui representacao concreta pronta ou em preparo
- `Bound`: participante esta associado ao seu seam de input/ownership
- `Active`: participante esta apto a participar da sessao
- `Suspended`: participante existe, mas esta temporariamente indisponivel
- `Disconnected`: participante remoto perdeu vinculo operacional
- `Ended`: participante saiu do ciclo da sessao

Esse lifecycle e semanticamente separado do lifecycle de actor.

## 5. Contratos minimos

### 5.1 Participant identity

A identidade de participante deve ser estavel dentro da sessao e separada de `ActorId`.

Ela pode carregar:

- `ParticipantId`
- `ParticipantKind`
- `OwnershipKind`
- `IsPrimary`
- `IsLocal`
- `AuthoringRef`
- `BindingHint`

### 5.2 Participant snapshot

O snapshot de participacao deve, no minimo, expor:

- validade semantica
- assinatura de participacao
- phase/session signature de origem
- lista de participantes
- primary/local participant
- contagem relevante
- readiness do bloco
- lifecycle resumido por participante

### 5.3 Readiness do bloco

Readiness do bloco e a condicao semantica de participacao estar consistente para o proximo passo do gameplay.

Ela nao substitui `GameReadinessService` nem `GameplayStateGate`.

`ParticipationReadinessState.NotReady` existe como parte do contrato, mas sua politica operacional ainda nao esta congelada.

Ate decisao explicita posterior, `Ready` e `NoContent` sao os estados liberadores do gameplay.
`NotReady` nao deve adquirir semantica operacional implicita por interpretacao local, fallback ou compatibilidade.

Qualquer decisao futura de usar `NotReady` como bloqueio, warning, fallback ou outro gate exige atualizacao explicita deste ADR.

### 5.4 Binding hint

Binding hint e uma pista semantica para consumo adjacente por `InputModes`.

Ele nao resolve `PlayerInput`, nao conhece device concreto e nao vira registry de input.

### 5.5 Relacao participant <-> actor

A relacao entre participante e actor e uma associacao de materializacao operacional.

`ActorRegistry` confirma a existencia concreta do actor, mas nao define a verdade semantica do participante.

### 5.6 Distincao conceitual

Este ADR separa explicitamente dois problemas:

- participacao semantica: quem participa, com que identidade, ownership, readiness semantica e binding hints
- gameplay interaction / binders: como os participantes se conectam ao palco ja composto por objetos, spawn points e anchors

A participacao prepara o roster e seus sinais semanticos.
O binding/interacao de gameplay lida com a conexao operacional posterior ao palco montado.

## 6. Relacao com outros dominios

### 6.1 GameplaySessionFlow

`GameplaySessionFlow` continua sendo o owner da orquestracao da sessao.

O bloco de participacao vive dentro dele ou imediatamente ao lado dele como subdominio semantico, e esta antes da liberacao efetiva do gameplay.

Ele nao substitui a fase posterior de binding/interacao com o palco ja composto.

### 6.2 Phase

`Phase` continua sendo a fonte autoral de configuracao.

`Phase` continua compondo o palco e o conteudo da phase.

`Phase.Players` nao e a fronteira runtime final; e apenas a entrada autoral para derivacao da participacao.

A participacao semantica nao e o mesmo problema que binders, spawn points ou anchors vindos do conteudo da phase.

### 6.3 ActorRegistry

`ActorRegistry` continua sendo owner dos atores vivos.

Ele nao deve ser rebaixado a roster semantico nem promovido a owner de participacao.

### 6.4 Spawn / despawn

Spawn e despawn continuam sendo owners operacionais.

O bloco de participacao pode informar expectativa e binding hints, mas nao executa materializacao nem resolve conexao com o palco.

### 6.5 Reset

Reset continua sendo pipeline operacional.

O bloco de participacao pode fornecer scoping semantico, mas nao executa reset.

### 6.6 InputModes

`InputModes` e um seam adjacente de binding concreto.

Ele consome `BindingHint` e ownership semantico, mas nao vira owner do roster nem do lifecycle de participante.

## 7. Consequencias

- o ownership semantico de players fica consolidado
- `ActorRegistry` permanece limpo como source of truth operacional
- `InputModes` ganha seam adequado sem acoplamento a actor vivo
- o desenho fica preparado para multiplayer sem exigir implementacao imediata
- o contrato de participacao fica observavel, assinavel e migravel
- `Phase.Players` deixa de ser owner runtime e passa a ser apenas autoria de entrada

## 8. Nao-objetivos

Este bloco nao e:

- owner de spawn
- owner de reset
- owner de `PlayerInput`
- owner de movement
- owner de gameplay behavior
- replacement de `ActorRegistry`
- mecanica de multiplayer completa
- registry de device
- registry de input global
- regra de pausa, readiness tecnica ou gate de `GameLoop`
- owner de spawn points do palco
- owner de anchors do palco
- owner de binders de objetos de gameplay
- owner de wiring do cenario ja composto
- substituto de um futuro grupo de gameplay interaction/binders

## 9. Migracao de alto nivel

1. consolidar a participacao semantica no novo bloco
2. manter `Phase.Players` como entrada autoral
3. fazer `GameplayPhaseFlowService` consumir e publicar o snapshot do bloco
4. manter bridges temporarias para reset, spawn e input
5. depois introduzir o grupo de binding/interacao de gameplay, se necessario
6. somente entao reduzir o uso direto de `Phase.Players` nos consumidores runtime
