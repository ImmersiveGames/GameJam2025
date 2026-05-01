# ADR-0064 - IntroStage como Activation Stage e Policy de Entrada

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

A Base 1.0 tratou `IntroStage` como rail phase-owned de entrada.
Na Base 1.1 isso continua sendo um comportamento valido, mas a decisao de ativacao passa a ser uma policy executada pelo pipeline, nao um ownership de ativacao local.

## Decisao

`IntroStage` passa a ser lido como `Activation Stage / Policy`.

Regras:

- `IntroStage` nao e owner de ativacao.
- a decisao de abrir, pular ou encerrar a ativacao pertence ao pipeline.
- quando existir presenter valido para a identidade atual, a stage executa.
- quando nao existir presenter valido, o ciclo registra `skip/no-content` explicito.
- resolucao concreta da instancia ocorre somente no momento canonico da pipeline.

## Consequencias

- a entrada deixa de ser decidida por conveniencia local.
- a ausencia valida de conteudo nao gera fallback silencioso.
- a activacao fica semanticamente vinculada ao ciclo correto.
- foreign/stale events nao podem reabrir ou trocar a stage ativa.

## Invariantes

- a validacao da stage depende da identidade explicita do ciclo.
- a entrada acontece somente apos o momento canonico definido pelo pipeline.
- o host local resolve a instancia concreta, nao a regra de ativacao.
- skip/no-content e sinal valido quando o conteudo estiver ausente.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 e historico de leitura phase-owned de `IntroStage`.
- Base 1.1 converte `IntroStage` em policy de ativacao sob controle de pipeline.
- Base 2.0 futura pode extrair padroes de ativacao se a Base 1.1 os provar.

## ADRs historicos relacionados

- `ADR-0037`
- `ADR-0047`
- `ADR-0050`
- `ADR-0056`
- `ADR-0057`
