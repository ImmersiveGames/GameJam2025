# ADR-0008: RuntimeModeConfig (Strict/Release + Degraded)

## Status

- Estado: **Implementado**
- Data (decisão): **2026-02-05**
- Última atualização: **2026-02-18**
- Decisores: **NewScripts / Infra**

## Evidências canônicas (atualizado em 2026-02-18)

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/lastlog.log`
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

Adicionar uma configuração opcional `RuntimeModeConfig` (ScriptableObject) carregada via `Resources`, e evoluir os serviços para respeitar essa configuração:

- Criar `RuntimeModeConfig` com:
    - `modeOverride` (Auto/ForceStrict/ForceRelease)
    - parâmetros do reporter (cooldown, max keys, sumário, severidade)
- Criar `ConfigurableRuntimeModeProvider`:
    - Usa o provider atual (build-based) como fallback.
    - Se `RuntimeModeConfig` existir e `modeOverride` não for Auto, o valor da config prevalece.
- Evoluir `DegradedModeReporter`:
    - Aceita `IRuntimeModeProvider` + `RuntimeModeConfig`.
    - Aplica dedupe/cooldown, sumário e severidade conforme config.
    - Mantém comportamento antigo quando config não existe.

Integração:

- O `GlobalCompositionRoot` passa a registrar `IRuntimeModeProvider` como `ConfigurableRuntimeModeProvider` e `IDegradedModeReporter` com injeção do provider + config.

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
- Contrato explícito: sem config → comportamento antigo.

**Negativas / riscos**

- Requer disciplina para manter o asset de config no projeto (ou aceitar o fallback).
- `Resources.Load` é um mecanismo global; por isso o loader é best-effort e deve rodar apenas em init.

## Invariantes / contrato

- Ausência de `RuntimeModeConfig` **não quebra produção** e mantém o comportamento atual.
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
2. Criar asset:
    - `Assets/_ImmersiveGames/NewScripts/Resources/NewScripts/RuntimeModeConfig.asset`
3. Atualizar `GlobalCompositionRoot.RegisterRuntimePolicyServices()` para:
    - carregar config (best-effort)
    - registrar provider configurável
    - registrar reporter com provider + config
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
- O `GlobalCompositionRoot` carregou o asset e anunciou:
    - `[RuntimePolicy] RuntimeModeConfig carregado (asset='RuntimeModeConfig').`
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

Para alinhar o boot ao plano `StringsToDirectRefs v1`, esta ADR registra as seguintes regras vigentes:

- `RuntimeModeConfig` é a **raiz canônica** de configuração do boot global do NewScripts.
- `Resources` é permitido **somente** para carregar `RuntimeModeConfig` no path fixo `RuntimeModeConfig`.
- `NewScriptsBootstrapConfigAsset` é resolvido por **referência direta** dentro de `RuntimeModeConfig` (`NewScriptsBootstrapConfig`).
- Não existe caminho obrigatório via **provider/manifest em cena** para resolver bootstrap config.
- O entrypoint global do `GlobalCompositionRoot` é único e determinístico em `BeforeSceneLoad`.
