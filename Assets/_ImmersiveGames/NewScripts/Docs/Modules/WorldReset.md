# WorldReset

## Estado atual

- `WorldResetService` e o owner do reset macro.
- `WorldResetOrchestrator` coordena o pipeline macro.
- `WorldResetExecutor` faz a ponte do fluxo macro para o boundary local neutro e valida a pos-condicao final.
- `WorldResetCommands` e a superficie canonica para pedir reset no runtime.
- `IWorldResetLocalExecutorRegistry` e o registro explicito de executores locais por cena.
- `SceneReset` e o executor local observado hoje; `SceneResetFacade` continua como compat historica.

## Ownership

- `WorldResetService`: entrypoint macro.
- `WorldResetOrchestrator`: sequenciamento do reset macro.
- `WorldResetExecutor`: handoff para o reset local neutro e verificacao final de atores/estado esperado.
- `WorldResetRequestService`: fila/correlacao de pedidos de reset.
- `IWorldResetLocalExecutor`: boundary neutro de execucao local consumido pelo macro.
- `IWorldResetLocalExecutorRegistry`: composicao explicita dos executores locais (sem locator global por scan).
- `Validation/*`: assinatura, validacao e regras do pedido.
- `Policies/*`: policy macro e decisao de reset.
- `Domain/*`: request, scope, flags, origin, reasons e contexto do reset.
- `ResetInterop`: ponte entre `SceneFlow` e `WorldReset`.

## Regras praticas

- `WorldReset*` deve continuar representando apenas o fluxo macro.
- O modulo nao e owner do pipeline local de cena; esse papel pertence ao executor local concreto.
- O executor local concreto observado hoje e `SceneReset`, mas isso e detalhe de implementacao e nao identidade conceitual do boundary.
- O modulo nao e owner do `SceneFlow`; a ponte entre os dois fica em `ResetInterop`.
- `LevelLifecycle` nao e owner do reset macro; ele so consome o resultado do pipeline.

## Leitura cruzada

- `Docs/Modules/SceneReset.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/SceneFlow.md`
