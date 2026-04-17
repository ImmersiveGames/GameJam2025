# Plan - ADR-0054 Introducao incremental do bloco semantico de participacao no GameplaySessionFlow

## 1. Objetivo

Introduzir o novo bloco/etapa semantica de participacao de players e actors dentro do `GameplaySessionFlow`, com ownership correto para a sessao jogavel.

O foco e criar o bloco certo no flow, consolidar participacao semantica e reduzir a gordura de `GameplayPhaseFlowService` sem depender de remover `Phase.Players` cedo demais.

## 2. Direcao Arquitetural

Alvo:

- bloco de participacao como owner do roster semantico
- `GameplaySessionFlow` como owner da orquestracao
- `Phase.Players` como input autoral temporario
- `ActorRegistry` como source of truth dos atores vivos
- `InputModes` como seam adjacente de binding concreto

Boundaries:

- participacao semantica responde por quem participa
- binders, spawn points, anchors e gameplay interaction ficam fora deste plano
- `GameplayPhaseFlowService` nao deve permanecer como owner detalhado da derivacao de participacao
- compatibilidade excessiva nao deve bloquear a arquitetura alvo

## 3. Fases de Implementacao

### Fase 1 - Contratos e shape do bloco

Objetivo:

- definir o contrato minimo do bloco de participacao antes de mover comportamento
- estabilizar a forma do ownership semantico

Motivacao:

- evitar que o novo bloco nasca acoplado ao shape antigo de `Phase.Players`
- garantir identidade semantica propria, separada de `ActorId`

Arquivos mais provaveis:

- `Assets/_ImmersiveGames/NewScripts/**/GameplaySessionFlow*`
- `Assets/_ImmersiveGames/NewScripts/**/GameplayPhaseFlowService*`
- `Assets/_ImmersiveGames/NewScripts/**/Participant*`
- `Assets/_ImmersiveGames/NewScripts/**/OwnershipKind*`
- `Assets/_ImmersiveGames/NewScripts/**/BindingHint*`
- `Assets/_ImmersiveGames/NewScripts/**/Readiness*`

Reaproveitamentos possiveis:

- tipos e enums ja existentes que expressem identidade, ownership e readiness
- snapshots atuais se puderem ser convertidos sem distorcer o contrato

Refatoracao mais profunda preferivel quando:

- o shape legado obriga `ActorId` a continuar sendo identidade principal
- o snapshot antigo mistura autoral, runtime e materializacao

Risco principal:

- copiar o modelo atual em vez de corrigir o ownership semantico

Pronto quando:

- `ParticipantId`, `ParticipantKind`, `OwnershipKind` e `BindingHint` existem como contrato claro
- lifecycle minimo e readiness do bloco estao definidos
- estados e transicoes minimas de lifecycle estao explicitados no contrato
- esta decidido se o bloco publica apenas snapshots ou tambem eventos de transicao de lifecycle
- snapshot e assinatura de participacao nao dependem de `ActorId` como eixo principal

### Fase 2 - Etapa de derivacao semantica no flow

Objetivo:

- inserir o bloco como etapa clara no `GameplaySessionFlow`
- mover para ele a derivacao de participacao hoje espalhada ou concentrada em `GameplayPhaseFlowService`

Motivacao:

- tirar o flow detalhado do papel de owner semantico da participacao
- alinhar a preparacao da sessao ao desenho do ADR-0054

Arquivos mais provaveis:

- `GameplaySessionFlow`
- `GameplayPhaseFlowService`
- contratos e runtime do novo bloco de participacao
- `Phase` ou adaptadores de entrada autoral, se necessario

Reaproveitamentos possiveis:

- logica existente de derivacao que ja produza os dados corretos
- helpers de compose derivado, se nao misturarem responsabilidade

Refatoracao mais profunda preferivel quando:

- `GameplayPhaseFlowService` continua acumulando classificacao, composicao e ownership ao mesmo tempo
- a derivacao nova precisa ser escondida dentro de um metodo legado

Risco principal:

- manter o service como owner real por inercia

Pronto quando:

- o novo bloco existe como etapa identificavel do flow
- `GameplayPhaseFlowService` passa a consumir e publicar o snapshot, nao a derivar tudo sozinho
- `Phase.Players` ainda existe, mas nao domina o runtime da participacao

### Fase 3 - Readiness e gating do flow

Objetivo:

- fazer o flow consumir a readiness semantica do novo bloco
- alinhar gates de entrada no gameplay com a participacao consolidada

Motivacao:

- a liberacao do gameplay deve depender do estado semantico da participacao, nao do shape antigo disperso

Arquivos mais provaveis:

- `GameplaySessionFlow`
- `GameplayPhaseFlowService`
- `IntroStage` e pontos de gate do flow
- contratos de readiness ja existentes

Reaproveitamentos possiveis:

- gates e sinais atuais, se puderem ler o novo snapshot sem reinventar o pipeline

Refatoracao mais profunda preferivel quando:

- readiness tecnica e readiness semantica estao misturadas no mesmo ponto
- o gate depende de detalhes locais de `Phase.Players`

Risco principal:

- quebrar a ordem canonica do fluxo ao trocar o ponto de decisao

Pronto quando:

- readiness de participacao vem do novo bloco
- o flow nao depende mais do shape antigo para liberar a entrada no gameplay
- `IntroStage` nao precisa inferir roster semantico por detalhe de cena

### Fase 4 - Observabilidade e consumers

Objetivo:

- migrar signatures, logs, QA e snapshots para o novo owner semantico
- tornar o bloco audivel e verificavel

Motivacao:

- sem observabilidade alinhada, o novo bloco vira apenas uma camada invisivel

Arquivos mais provaveis:

- runtime do `GameplaySessionFlow`
- `GameplayPhaseFlowService`
- reporters, logs e QA hooks de participacao
- snapshots consumidos por tooling interno

Reaproveitamentos possiveis:

- formatos de log e QA existentes, se puderem apontar para o novo owner

Refatoracao mais profunda preferivel quando:

- a observabilidade precisa continuar lendo o owner legado para fazer sentido
- o snapshot novo fica subordinado a um formato antigo que distorce ownership

Risco principal:

- manter o owner antigo como unica fonte pratica de debug

Pronto quando:

- signatures e logs principais refletem o novo bloco
- QA consegue validar participacao sem depender do desenho antigo
- o snapshot novo e o ponto normal de inspeccao

### Fase 5 - Bridges adjacentes

Objetivo:

- definir bridges finas com spawn, reset, `InputModes` e `ActorRegistry`
- manter esses dominios adjacentes, nao owners do bloco

Motivacao:

- o bloco precisa servir ao runtime sem absorver responsabilidades de materializacao ou input concreto

Arquivos mais provaveis:

- `ActorRegistry`
- pontos de spawn/despawn
- reset pipeline
- `InputModes`
- adaptadores de bridge do flow

Reaproveitamentos possiveis:

- bridges atuais que so precisem trocar a origem dos dados

Tipos de bridge:

- bridge semantica -> operacional: spawn, reset e `ActorRegistry`
- bridge semantica -> binding concreto: `InputModes`

Refatoracao mais profunda preferivel quando:

- a bridge precisa virar regra central do bloco
- spawn/reset/input tentam assumir responsabilidade de ownership semantico

Risco principal:

- deixar o bloco vazado para os dominios adjacentes ou, no extremo oposto, acopla-lo demais

Pronto quando:

- esses dominios consomem saidas do bloco sem virar owners dele
- `ActorRegistry` continua operacional
- `InputModes` continua como seam de binding concreto

### Fase 6 - Detox progressivo de `Phase.Players`

Objetivo:

- reduzir o uso direto de `Phase.Players` nos consumidores runtime
- preparar a retirada futura do campo do asset, se e quando fizer sentido

Motivacao:

- a reducao de `Phase.Players` deve ser consequencia do bloco estar estavel, nao meta principal precoce

Arquivos mais provaveis:

- consumidores runtime restantes de `Phase.Players`
- `GameplaySessionFlow`
- `GameplayPhaseFlowService`
- `Phase` / asset de phase, apenas quando houver corte seguro

Reaproveitamentos possiveis:

- bridges temporarias durante a transicao
- adaptadores leves enquanto o runtime migra

Refatoracao mais profunda preferivel quando:

- manter compatibilidade passa a impedir o corte correto do ownership

Risco principal:

- remover a entrada autoral antes de o novo bloco sustentar o fluxo sozinho

Pronto quando:

- `Phase.Players` fica claramente rebaixado a input autoral temporario
- o runtime principal ja usa o novo bloco
- existe um mapa claro para o corte futuro, sem pressa artificial

## 4. Sequencia Canonica

Ordem recomendada:

1. Fase 1
2. Fase 2
3. Fase 3
4. Fase 4
5. Fase 5
6. Fase 6

Nao misturar gameplay interaction / binders neste plano inicial.
Nao antecipar a remocao de `Phase.Players` antes da estabilidade do novo bloco.

## 5. Resultado Esperado

Ao final do plano:

- o bloco semantico de participacao fica dentro do `GameplaySessionFlow`
- `GameplayPhaseFlowService` encolhe para orquestracao e consumo
- `ActorRegistry` permanece como truth source operacional
- `InputModes` segue como seam adjacente de binding concreto
- `Phase.Players` deixa de dirigir o runtime e passa a ser apenas entrada autoral temporaria
