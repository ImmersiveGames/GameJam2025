# PlayerParticipation

Este módulo contém os contratos de participação de jogador da Base 2.0.

## Fronteira congelada

- `SessionOperational` decide e prepara participação antes do handoff para Activity.
- `InputModes` aplica modo de input da rota, mas não decide participação.
- `PlayerInputManager` existe desde o boot como infraestrutura técnica de input.
- `PlayerInput` concreto do jogador nasce com o `PlayerActor` materializado pela Activity.
- `SessionActivity` não resolve `PlayerSlotId -> Actor`.
- `ActivityEntryPipeline` materializa Actors a partir de participação resolvida.

## Contratos

- `PlayerSlotId`: assento/input do jogador.
- `PlayerSlotReservation`: reserva de assento antes da materialização.
- `PlayerSelection`: seleção/configuração de personagem para um slot.
- `SessionParticipantBinding`: participante resolvido pela rota/sessão.
- `SessionParticipationContext`: payload operacional resolvido antes do handoff.
- `ActivityParticipantBinding`: participante aceito por uma Activity.
- `ActorMaterializationRequest/Result`: fronteira futura para materialização por Activity.

## Estado atual

SA-PART-0B adiciona contratos passivos.
SA-PART-0C faz `OperationalPlayerParticipationStage` produzir `SessionParticipationContext` em paralelo ao payload transitório atual.

O handoff da `SessionActivity` ainda pode consumir o contrato transitório até o corte específico de troca de payload.
