# ADR-0040: InputModes - Estado Canônico e Hook Oficial

> STATUS NORMATIVO: HISTORICO - NAO NORMATIVO PARA DECISOES DE OWNERSHIP DA BASE 1.0.
> Em conflito, prevalecem ADR-0057, ADR-0056, ADR-0055, ADR-0058, ADR-0054 e ADR-0052.

## Status
- Aceito
- Implementado em `NewScripts`

## Contexto
O módulo `InputModes` já possuía um writer canônico (`InputModeCoordinator`) e um serviço de aplicação (`InputModeService`), mas ainda faltavam dois contratos explícitos:

- leitura canônica do modo ativo;
- hook oficial de mudança efetiva de modo.

Sem esses contratos, consumidores precisavam inferir estado por efeito colateral ou repetir lógica de observação local.

## Decisão
- O owner canônico do estado de input continua no próprio `InputModes`.
- `InputModeCoordinator` continua sendo o writer canônico dos requests.
- `InputModeService` continua sendo o serviço de política/aplicação.
- `InputModeService` passa a implementar `IInputModeStateService` e expor `CurrentMode`.
- `InputModeService` passa a publicar `InputModeChangedEvent` quando o modo realmente muda.
- No-op redundante não publica evento.
- A descoberta concreta de `PlayerInput` continua encapsulada no `IPlayerInputLocator`.
- Os requests canônicos usam `InputModeRequestKind` (`FrontendMenu`, `Gameplay`, `PauseOverlay`).

## Superfície Pública
A superfície pública canônica do módulo passa a ser:

- `IInputModeService`
- `IInputModeStateService`
- `InputModeChangedEvent`

`InputModeRequestEvent` continua sendo o request seam já existente, consumido pelo `InputModeCoordinator`.

## Leitura Canônica
`IInputModeStateService.CurrentMode` é a leitura canônica do modo ativo.

Isso permite que consumidores consultem o estado atual sem depender de:

- logs;
- dedupe interno;
- `PlayerInput` concreto;
- detalhes de aplicação de mapas.

## Hook Oficial
`InputModeChangedEvent` é o hook oficial de observação do módulo.

Regras:

- só é publicado quando o modo muda de fato;
- não é publicado em reaplicação redundante do mesmo modo;
- carrega `PreviousMode`, `CurrentMode` e `Reason`.

## Papel do Locator
`IPlayerInputLocator` permanece como detalhe de infraestrutura do módulo.

Ele existe para:

- encapsular a descoberta concreta de `PlayerInput`;
- manter `InputModeService` focado em política/aplicação;
- evitar que outros consumidores acoplem ao `FindObjectsByType<PlayerInput>()`.

O locator não vira registry complexo, nem novo owner do domínio.

## Estado consolidado da rodada

- `InputModeService` continua sem conhecer regra semântica de `SceneFlow` por string.
- a leitura canônica de estado fica em `IInputModeStateService.CurrentMode`.
- o hook oficial continua em `InputModeChangedEvent`.
- não há registry adicional documentado como contrato.

## Fora de Escopo
Explicitamente fora de escopo nesta etapa:

- criar `InputModeChangedEvent` mais rico com payload adicional;
- criar registry de `PlayerInput`;
- modularizar `InputModes` como módulo independente;
- mover `InputModeCoordinator` para outro desenho;
- trocar o locator por cache/registry avançado;
- abrir refatoração ampla do wiring.

## Consequências
- o estado de input fica consultável de forma canônica;
- consumidores ganham um hook oficial estável;
- o serviço central continua simples;
- o módulo mantém o comportamento funcional atual para frontend, gameplay e pause.

## Referências
- `ADR-0037`
- `ADR-0039`
