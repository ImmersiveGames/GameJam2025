# ADR-0048 - PhaseDefinition como fonte de verdade autoral da fase jogavel

## Status
- Estado: Aceito
- Data: 2026-04-03
- Tipo: Direction / Canonical architecture

## Contexto

Os ADRs `ADR-0001`, `ADR-0044`, `ADR-0045`, `ADR-0046` e `ADR-0047` ja fecharam a leitura canonica da gameplay como composicao semantica e do `GameplaySessionFlow` como bloco de montagem da fase jogavel.

Faltava registrar a camada autoral canonica da fase.

## Decisao

`PhaseDefinition` passa a ser a fonte de verdade autoral da fase jogavel.

Ele existe para alimentar `GameplaySessionFlow` e para separar a definicao autoral da fase da leitura runtime.

## Leitura canonica

### `PhaseDefinition`

- e a definicao canonicamente autoral da fase jogavel
- alimenta `GameplaySessionFlow`
- nao e runtime snapshot
- nao e `WorldDefinition`
- nao e bootstrap root
- nao e scene composition operacional

### `Level`

- e um nome / estrutura historica do estado atual
- nao deve permanecer como conceito arquitetural do futuro se nao tiver papel conceitual proprio distinto
- a arquitetura alvo deve ser lida diretamente por `PhaseDefinition`

## Motivacao

- `Level` e um nome historico e semanticamente limitado
- se nao existir papel conceitual proprio distinto, preserva-lo como peca futura cria apenas uma ponte desnecessaria
- o runtime ja consolidou `PhaseRuntime`, entao `PhaseDefinition` cria uma linha conceitual melhor entre authoring e runtime
- a separacao evita que uma estrutura historica vire owner do significado da fase

## Estrategia atual

- `PhaseDefinition` comeca minima
- sem modularizacao precoce
- sem decidir agora quantos sub-assets existirao
- sem definir agora o model completo de authoring
- a evolucao interna deve acontecer apenas quando a necessidade real do jogo exigir

## Em aberto de proposito

- blocos internos obrigatorios
- fragmentacao em sub-assets
- partes reutilizaveis vs partes embutidas
- shape final do modelo de authoring

## Consequencias

- `PhaseDefinition` passa a ser lido diretamente como centro autoral da fase
- `Level` nao e mantido como conceito arquitetural do futuro por conveniencia de transicao
- o projeto ganha uma fronteira mais limpa entre definicao e runtime sem carregar uma peca conceitual redundante
