# Foundation / Platform / SimulationGate

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Serviço técnico de bloqueio/liberação da simulação por tokens.

## Para que serve

- Bloquear gameplay durante transições, pause, intro ou outros estados técnicos.
- Permitir múltiplas razões simultâneas de bloqueio.
- Liberar simulação apenas quando todos os tokens relevantes forem removidos.

## Quando usar

Use quando o runtime precisa pausar ou bloquear simulação por motivo técnico explícito.

Exemplos:

- pause;
- scene transition;
- intro stage;
- loading;
- preparação de gameplay.

## Como usar

Fluxo conceitual:

```text
Acquire token -> simulação bloqueada
Release token -> simulação pode liberar quando não houver outros bloqueios
```

Tokens centrais ficam em:

```text
Foundation/Platform/SimulationGate/SimulationGateTokens.cs
```

## Quando não usar

- Não usar para definir estado semântico da run.
- Não usar como substituto de GameLoop.
- Não usar token genérico quando existe token canônico.
- Não liberar token que pertence a outro owner.

## Arquivos principais

- `Foundation/Platform/SimulationGate/ISimulationGateService.cs`
- `Foundation/Platform/SimulationGate/SimulationGateService.cs`
- `Foundation/Platform/SimulationGate/SimulationGateTokens.cs`
- `Foundation/Platform/SimulationGate/Interop/GamePauseGateBridge.cs`
- `Foundation/Platform/SimulationGate/GATES_ANALYSIS_REPORT.md`

## Cuidados

- Tokens precisam ter owner claro.
- Todo acquire deve ter release correspondente.
- Bridges, como `GamePauseGateBridge`, devem reagir ao owner do estado; não virar owner do domínio.
