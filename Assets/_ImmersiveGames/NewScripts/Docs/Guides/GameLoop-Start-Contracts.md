# GameLoop Start Contracts

## Status

- Guia curto e especializado.
- Mantem o split entre boot/start-plan e intent de Play do usuario.

## Boot / Start-Plan

- `BootStartPlanRequestedEvent` pertence ao rail de bootstrap.
- Ele e emitido uma unica vez por `BootStartPlanRequestEmitter` no inicio da cena.
- Ele inicia o handshake canonico entre `SceneFlow` e `GameLoop`.
- Ele nao representa intent de Play do usuario.

## User Play Intent

- `GamePlayRequestedEvent` pertence a intent do usuario no frontend.
- Ele e emitido pelo UI binder de Play.
- Ele pede ao backbone a entrada em gameplay pela rota canonica.
- Ele nao deve ser reutilizado para boot ou start-plan.

## Regra

- Mantenha boot/start-plan e Play do usuario como contratos separados.
- Se um novo fluxo precisar entrar em gameplay, ele deve respeitar esse split e seguir a rota canonica, nao o atalho de outro contrato.
