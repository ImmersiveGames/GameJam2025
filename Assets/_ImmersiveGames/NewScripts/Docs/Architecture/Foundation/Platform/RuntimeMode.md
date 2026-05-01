# Foundation / Platform / RuntimeMode

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Infraestrutura para definir modo de runtime e relatar condições degradadas.

## Para que serve

- Ler configuração de modo runtime.
- Diferenciar modo automático/configurado.
- Reportar condições degradadas com dedupe/cooldown.
- Padronizar comportamento em Editor/build quando algo entra em modo degradado.

## Quando usar

Use para decisões técnicas globais de runtime e degradação operacional controlada.

## Como usar

Fluxo geral:

```text
RuntimeModeConfig -> RuntimeModeConfigLoader -> IRuntimeModeProvider -> RuntimeMode
```

Para reporting:

```text
IDegradedModeReporter -> DegradedModeReporter -> DegradedKeys
```

## Quando não usar

- Não usar para mascarar contrato obrigatório quebrado.
- Não usar como política de gameplay.
- Não usar como fallback genérico quando o certo é fail-fast.

## Arquivos principais

- `Foundation/Platform/RuntimeMode/RuntimeMode.cs`
- `Foundation/Platform/RuntimeMode/RuntimeModeConfig.cs`
- `Foundation/Platform/RuntimeMode/RuntimeModeConfigLoader.cs`
- `Foundation/Platform/RuntimeMode/IRuntimeModeProvider.cs`
- `Foundation/Platform/RuntimeMode/UnityRuntimeModeProvider.cs`
- `Foundation/Platform/RuntimeMode/ConfigurableRuntimeModeProvider.cs`
- `Foundation/Platform/RuntimeMode/IDegradedModeReporter.cs`
- `Foundation/Platform/RuntimeMode/DegradedModeReporter.cs`
- `Foundation/Platform/RuntimeMode/DegradedKeys.cs`
- `Foundation/Platform/RuntimeMode/README-QuickStart.md`

## Cuidados

- O quickstart do pacote indica que `RuntimeModeConfig.asset` deve existir e ser referenciado pelo `BootstrapConfig.asset`.
- Ausência de asset obrigatório deve falhar cedo.
- Degraded mode é uma política técnica controlada, não atalho para configuração ausente.
