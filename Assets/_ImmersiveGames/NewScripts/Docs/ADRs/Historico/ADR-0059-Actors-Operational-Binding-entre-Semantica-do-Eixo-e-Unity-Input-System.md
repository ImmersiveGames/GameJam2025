# ADR-0059 - Actors Operational Binding entre semantica do eixo e Unity Input System

## Status
- Estado: Aceito
- Data: 2026-04-23
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## 1. Problema

O projeto precisa integrar `PlayerInput` / `PlayerInputManager` para join/leave local, pairing de input e controle operacional de players.

Sem boundary explicito, ha risco de colapso de ownership:
- IDs operacionais da Unity (`playerIndex`, `InputUser.id`) sendo usados como identidade semantica;
- `PlayerInputManager` virando pseudo-owner de roster;
- binding implicito por heuristica ou scan global de `PlayerInput`;
- mistura entre participation semantica, eixo de actors e runtime de input.

Isso viola a Base 1.0 (`ADR-0057`), a separacao de participacao semantica (`ADR-0054`) e o ownership do conjunto em `ActorsSystem` (`ADR-0058`).

## 2. Decisao

Adota-se um boundary proprio de **Actors Operational Binding** para ligar identidade semantica do projeto e identidade operacional da Unity.

Nome recomendado:
- pasta: `ActorsSystem/Integration/OperationalBinding`
- namespace: `_ImmersiveGames.NewScripts.ActorsSystem.Integration.OperationalBinding`

Papel do binding:
- traduzir e manter o vinculo explicito entre IDs semanticos/canonicos e handles operacionais da Unity;
- publicar estado de binding como snapshot/evento operacional;
- nunca redefinir ownership semantico.

## 3. Ownership de identificadores

- `ParticipantId`: owner = `Participation` (`SessionFlow` semantico).
- `AxisActorId`: owner = `ActorsSystem`.
- `RuntimeActorId`: owner = materializacao operacional (`Spawn`/`ActorRegistry`).
- `playerIndex`: owner = Unity (`PlayerInput`/`PlayerInputManager`).
- `InputUser.id`: owner = Unity Input System.

Regra normativa:
- IDs da Unity nao substituem `ParticipantId`, `AxisActorId` ou `RuntimeActorId`.

## 4. Momento do binding no fluxo

Ordem canonica:
1. semantica pronta (`Participation` / `ActorsSystem`).
2. actor do eixo resolvido (`AxisActorId` definido).
3. materializacao concreta concluida (`RuntimeActorId` valido).
4. bind operacional com Unity (`PlayerInput`, `playerIndex`, `InputUser.id`).
5. publicacao de snapshot/estado de binding.

Regra normativa:
- binding nasce tarde, apos semantica + materializacao;
- e proibido depender de ID operacional da Unity antes desse momento.

## 5. Estados minimos do binding

Estados minimos canonicos:
- `Unbound`
- `Bound`
- `Active`
- `Disconnected`

Semantica minima:
- `Unbound`: sem vinculo operacional valido.
- `Bound`: vinculo criado, ainda sem garantia de atividade.
- `Active`: vinculo operacional ativo e pronto para uso.
- `Disconnected`: vinculo existia e perdeu conectividade operacional.

## 6. O que a Unity faz

`PlayerInput` / `PlayerInputManager` entram como camada operacional para:
- join/leave local;
- pairing de device/user;
- atribuicao e manutencao de `playerIndex`;
- atribuicao e manutencao de `InputUser.id`;
- gestao operacional de `PlayerInput`;
- troca de action maps;
- spawn operacional local quando esse trilho for adotado.

## 7. O que a Unity nao faz

`PlayerInput` / `PlayerInputManager` nao devem:
- definir semantica do conjunto de actors/players;
- definir `ParticipantId`;
- definir `AxisActorId`;
- substituir `RuntimeActorId` canonico;
- virar registry arquitetural do eixo;
- decidir materialization plan semantico.

## 8. Anti-padroes proibidos

- scan global de `PlayerInput` como base canonica de identidade/binding;
- uso de `playerIndex` ou `InputUser.id` como identidade semantica;
- colapso entre `Participation`, `ActorsSystem` e input runtime no mesmo owner;
- `PlayerInputManager` tratado como owner de roster semantico.

## 9. Consequencias

- proximos slices devem consumir o boundary de binding explicito, sem atalhos implicitos;
- o eixo preserva ownership correto entre semantica e operacao;
- fica autorizado reconstruir partes inadequadas do shape atual para convergir no boundary correto;
- elimina necessidade de compatibilidade por inercia quando conflitar com ownership canonico.

## 10. Congelamento normativo do binding vs legitimidade (2026-04-23)

Fica explicitamente congelado:

- binding operacional nao legitima actor.
- binding operacional nao descobre actor canonico.
- binding operacional so consome actor previamente legitimado no `ActorsSystem` por `ActorSpec`.

Regra de fronteira:

- `ActorSpec` define existencia canonica, recipe e placeholder;
- binding apenas conecta o actor ja resolvido aos handles operacionais de input/runtime;
- qualquer tentativa de inferir roster canonico via `PlayerInput`/`PlayerInputManager` e desvio arquitetural.
