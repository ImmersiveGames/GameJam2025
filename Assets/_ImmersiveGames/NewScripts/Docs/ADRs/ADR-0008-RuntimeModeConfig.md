# ADR-0008: RuntimeModeConfig (Strict/Release + Degraded)

## Status

- Estado: **Implementado**
- Data (decisão): **2026-02-05**
- Última atualização: **2026-02-18**
- Decisores: **NewScripts / Infra**

## Evidências canônicas (atualizado em 2026-02-18)

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- `Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md`

## Contexto

O NewScripts já possui dois serviços globais que influenciam comportamento em produção:

- `IRuntimeModeProvider`: define se o jogo roda em modo **Strict** ou **Release**.
- `IDegradedModeReporter`: registra quando um sistema precisa operar com limitação (fallback / feature ausente / contrato não cumprido).

Hoje o modo é determinado apenas pelo tipo de build (Editor/Development → Strict; Release → Release). Isso é bom como padrão, mas não permite:

- Forçar **Strict** (ou **Release**) para QA/diagnóstico sem recompilar.
- Padronizar “como” e “quanto” o `DegradedModeReporter` deve logar (evitar spam, ter chaves consistentes, ter sumário).

Também não há um local único para documentar padrões de chaves, cooldown de logs e comportamento esperado quando a configuração não existe.

## Decisão

Adicionar uma configuração `RuntimeModeConfig` como dependência obrigatória do bootstrap canônico, referenciada diretamente por `BootstrapConfigAsset`, e evoluir os serviços para respeitar essa configuração:

- Criar `RuntimeModeConfig` com:
    - `modeOverride` (Auto/ForceStrict/ForceRelease)
    - parâmetros do reporter (cooldown, max keys, sumário, severidade)
- Criar `ConfigurableRuntimeModeProvider`:
    - Usa o provider atual (build-based) como fallback interno de modo.
    - Se `RuntimeModeConfig` existir e `modeOverride` não for Auto, o valor da config prevalece.
- Evoluir `DegradedModeReporter`:
    - Aceita `IRuntimeModeProvider` + `RuntimeModeConfig`.
    - Aplica dedupe/cooldown, sumário e severidade conforme config.
    - Não precisa mais resolver config por `Resources`.

Integração:

- O `GlobalCompositionRoot` passa a registrar `IRuntimeModeProvider` como `ConfigurableRuntimeModeProvider` e `IDegradedModeReporter` com injeção do provider + config.
- A resolução do `RuntimeModeConfig` é feita via `BootstrapConfigAsset`; ausência da referência é fail-fast.

## Alternativas consideradas

1) Usar `WorldDefinition` como config de modo
- Rejeitada: `WorldDefinition` é configuração por cena (spawn/ordem), enquanto runtime mode é global e afeta múltiplos módulos (policy, logging, fallback).

2) Definir override via `Scripting Define Symbols`
- Rejeitada: exige recompilar e torna QA mais lento.

3) Ler config de JSON/ini
- Rejeitada (por agora): adiciona parsing IO e um novo formato; ScriptableObject é mais simples para Unity e evita bugs de parsing.

## Consequências

**Positivas**

- QA pode forçar Strict/Release via asset, sem rebuild.
- Logs de “degraded” ficam padronizados (chaves, cooldown, sumário), reduzindo ruído.
- Contrato explícito: sem referência obrigatória → fail-fast.

**Negativas / riscos**

- Requer disciplina para manter o asset de config e a referência no bootstrap canônico.
- `Resources.Load<RuntimeModeConfig>` não faz mais parte do boot policy.

## Invariantes / contrato

- Ausência de `RuntimeModeConfig` na referência canônica **quebra o boot** com fail-fast.
- `IRuntimeModeProvider.GetMode()` deve ser determinístico dentro do mesmo run.
- `IDegradedModeReporter.Report(...)` não deve causar exceções nem travar fluxo.
- Reporter deve limitar spam (cooldown + cap de chaves) e emitir um sumário periódico quando habilitado.

## Plano de implementação

1. Adicionar arquivos em `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/`:
    - `RuntimeModeConfig.cs`
    - `RuntimeModeConfigLoader.cs`
    - `ConfigurableRuntimeModeProvider.cs`
    - `DegradedKeys.cs`
    - atualizar `DegradedModeReporter.cs`
2. Referenciar `RuntimeModeConfig` em `BootstrapConfigAsset`.
3. Atualizar `GlobalCompositionRoot.RegisterRuntimePolicyServices()` para:
    - resolver config via bootstrap canônico;
    - registrar provider configurável;
    - registrar reporter com provider + config.
4. Validar em build Dev e Release com logs.

## Como testar

- Dev/Editor:
    - Sem asset: mode = Strict.
    - Com asset `ForceRelease`: mode = Release.
- Release build:
    - Sem asset: mode = Release.
    - Com asset `ForceStrict`: mode = Strict.
- Reporter:
    - Disparar a mesma chave repetidas vezes e confirmar cooldown.
    - Disparar múltiplas chaves e confirmar cap.
    - Confirmar sumário periódico quando habilitado.

## Evidências de Implementação

Evidência histórica: **log de inicialização** enviado pelo projeto (data do log: **2026-02-05**).

Ponteiros operacionais atuais:
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`

Sinais mínimos de que o ADR está **implementado e ativo**:

- O **asset** foi registrado no DI global:
    - `Serviço RuntimeModeConfig registrado no escopo global.`
- O `GlobalCompositionRoot` resolveu o asset via `BootstrapConfigAsset` e anunciou:
    - `[RuntimePolicy] RuntimeModeConfig resolvido via BootstrapConfigAsset (asset='RuntimeModeConfig').`
- O `IRuntimeModeProvider` foi registrado e resolvido:
    - `Serviço IRuntimeModeProvider registrado no escopo global.`
    - `Serviço IRuntimeModeProvider encontrado no escopo global...`
- O `IDegradedModeReporter` foi registrado e resolvido:
    - `Serviço IDegradedModeReporter registrado no escopo global.`
    - `Serviço IDegradedModeReporter encontrado no escopo global...`
- A policy de reset em produção foi registrada **dependendo** desses serviços:
    - `Serviço IWorldResetPolicy registrado no escopo global.`
    - `[RuntimePolicy] IRuntimeModeProvider + IDegradedModeReporter + IWorldResetPolicy registrados no DI global.`

Resultado observado no mesmo log: o fluxo segue normalmente (SceneFlow/WorldLifecycle/GameLoop), indicando que o modo e o reporter não travam o boot.

## Nota de atualização (2026-02-16) — Boot canônico do NewScripts

Atualização consolidada da rodada estrutural atual:

- `RuntimeModeConfig` é dependência obrigatória do `BootstrapConfigAsset`.
- não existe fallback oculto por `RuntimeModeConfigLoader` nem `Resources.Load<RuntimeModeConfig>` no boot policy.
- a resolução de `RuntimeModeConfig` acontece por referência direta no bootstrap canônico.
- ausência da referência obrigatória é fail-fast.
- o entrypoint global do `GlobalCompositionRoot` permanece único e determinístico em `BeforeSceneLoad`.

