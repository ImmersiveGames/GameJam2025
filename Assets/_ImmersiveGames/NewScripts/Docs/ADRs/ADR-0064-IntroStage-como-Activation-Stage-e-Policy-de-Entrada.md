# ADR-0064 - IntroStage como Activation Stage e Pipeline Policy de Entrada

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 tratou `IntroStage` como rail phase-owned de entrada.
Na Base 1.1 isso continua sendo um comportamento válido, mas a decisão de ativação passa a ser uma `Pipeline Policy` executada pelo pipeline, não um ownership de ativação local.

## Decisão

`IntroStage` passa a ser lido como `Activation Stage / Pipeline Policy`.

Regras:

- `IntroStage` não é owner de ativação.
- a decisão de abrir, pular ou encerrar a ativação pertence ao pipeline.
- quando existir presenter válido para a identidade atual, a stage executa.
- quando não existir presenter válido, o ciclo registra `skip/no-content` explícito.
- resolução concreta da instância ocorre somente no momento canônico da pipeline.

## Consequências

- a entrada deixa de ser decidida por conveniência local.
- a ausência válida de conteúdo não gera fallback silencioso.
- a ativação fica semanticamente vinculada ao ciclo correto.
- foreign/stale events não podem reabrir ou trocar a stage ativa.

## Invariantes

- a validação da stage depende da identidade explícita do ciclo.
- a entrada acontece somente após o momento canônico definido pelo pipeline.
- o host local resolve a instância concreta, não a regra de ativação.
- `skip/no-content` é sinal válido quando o conteúdo estiver ausente.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 é histórico de leitura phase-owned de `IntroStage`.
- Base 1.1 converte `IntroStage` em `Pipeline Policy` de ativação sob controle de pipeline.
- Base 2.0 futura pode extrair padrões de ativação se a Base 1.1 os provar.

## ADRs históricos relacionados

- `ADR-0037`
- `ADR-0047`
- `ADR-0050`
- `ADR-0056`
- `ADR-0057`
