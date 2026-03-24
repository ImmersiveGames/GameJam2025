# WorldReset

## Estado atual

- `WorldResetService` e o owner do reset macro.
- `WorldResetOrchestrator` coordena o pipeline macro.
- `WorldResetExecutor` faz a ponte do fluxo macro para o trilho local de reset e valida a pos-condicao final.
- `WorldResetCommands` e a superficie canonica para pedir reset no runtime.

## Ownership

- `WorldResetService`: entrypoint macro.
- `WorldResetOrchestrator`: sequenciamento do reset macro.
- `WorldResetExecutor`: handoff para o reset local e verificacao final de atores/estado esperado.
- `WorldResetRequestService`: fila/correlacao de pedidos de reset.
- `Validation/*`: assinatura, validacao e regras do pedido.
- `Policies/*`: policy macro e decisao de reset.
- `Domain/*`: request, scope, flags, origin, reasons e contexto do reset.

## Regras praticas

- `WorldReset*` deve continuar representando apenas o fluxo macro.
- O modulo nao e owner do pipeline local de cena; esse papel pertence a `SceneReset`.
- O modulo nao e owner do `SceneFlow`; a ponte entre os dois fica em `ResetInterop`.

## Leitura cruzada

- `Docs/Modules/SceneReset.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/SceneFlow.md`
