# Adendo para README dos ADRs — registrar ADR-1.2-0008

Atualizar a lista de ADRs vivos da Base 1.2 para incluir:

```text
8. ADR-1.2-0008 — Actor Model, Actor Typing e ActorCapabilitySurface
```

Adicionar ao checkpoint atual da Base 1.2:

```text
Actor Model / Actor Typing / ActorCapabilitySurface — INTENÇÃO NORMATIVA CONGELADA
```

Adicionar em Conceitos chave / Actors:

```text
Actor é raiz abstrata de entidade de gameplay.
PlayerActor, NonPlayerActor, NpcActor, EnemyActor, ObjectActor e InteractiveActor são especializações concretas ou direção de especialização.
ActorKind/ActorRole são metadata de authoring/log/migração, não switch funcional principal.
ActorCapabilitySurface expõe endpoints locais, mas não decide lifecycle.
Pipeline consome contracts/capabilities/endpoints, não categorias soltas.
```

Adicionar em Precedência normativa:

```text
Em decisões sobre modelo central de Actor, ADR-1.2-0008 complementa ADR-1.2-0001 a ADR-1.2-0007.
Em conflito entre ActorKind como switch funcional e tipo/capability/endpoint, prevalece ADR-1.2-0008.
```
