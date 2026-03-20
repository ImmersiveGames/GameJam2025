# ADR-0008: RuntimeModeConfig (Strict/Release + Degraded)

## Status

- Estado: **Implementado**
- Data (decisÃ£o): **2026-02-05**
- Ãšltima atualizaÃ§Ã£o: **2026-02-18**
- Decisores: **NewScripts / Infra**

## EvidÃªncias canÃ´nicas (atualizado em 2026-02-18)

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- `Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md`

## Contexto

O NewScripts jÃ¡ possui dois serviÃ§os globais que influenciam comportamento em produÃ§Ã£o:

- `IRuntimeModeProvider`: define se o jogo roda em modo **Strict** ou **Release**.
- `IDegradedModeReporter`: registra quando um sistema precisa operar com limitaÃ§Ã£o (fallback / feature ausente / contrato nÃ£o cumprido).

Hoje o modo Ã© determinado apenas pelo tipo de build (Editor/Development â†’ Strict; Release â†’ Release). Isso Ã© bom como padrÃ£o, mas nÃ£o permite:

- ForÃ§ar **Strict** (ou **Release**) para QA/diagnÃ³stico sem recompilar.
- Padronizar â€œcomoâ€ e â€œquantoâ€ o `DegradedModeReporter` deve logar (evitar spam, ter chaves consistentes, ter sumÃ¡rio).

TambÃ©m nÃ£o hÃ¡ um local Ãºnico para documentar padrÃµes de chaves, cooldown de logs e comportamento esperado quando a configuraÃ§Ã£o nÃ£o existe.

## DecisÃ£o

Adicionar uma configuraÃ§Ã£o opcional `RuntimeModeConfig` (ScriptableObject) carregada via `Resources`, e evoluir os serviÃ§os para respeitar essa configuraÃ§Ã£o:

- Criar `RuntimeModeConfig` com:
    - `modeOverride` (Auto/ForceStrict/ForceRelease)
    - parÃ¢metros do reporter (cooldown, max keys, sumÃ¡rio, severidade)
- Criar `ConfigurableRuntimeModeProvider`:
    - Usa o provider atual (build-based) como fallback.
    - Se `RuntimeModeConfig` existir e `modeOverride` nÃ£o for Auto, o valor da config prevalece.
- Evoluir `DegradedModeReporter`:
    - Aceita `IRuntimeModeProvider` + `RuntimeModeConfig`.
    - Aplica dedupe/cooldown, sumÃ¡rio e severidade conforme config.
    - MantÃ©m comportamento antigo quando config nÃ£o existe.

IntegraÃ§Ã£o:

- O `GlobalCompositionRoot` passa a registrar `IRuntimeModeProvider` como `ConfigurableRuntimeModeProvider` e `IDegradedModeReporter` com injeÃ§Ã£o do provider + config.

## Alternativas consideradas

1) Usar `WorldDefinition` como config de modo
- Rejeitada: `WorldDefinition` Ã© configuraÃ§Ã£o por cena (spawn/ordem), enquanto runtime mode Ã© global e afeta mÃºltiplos mÃ³dulos (policy, logging, fallback).

2) Definir override via `Scripting Define Symbols`
- Rejeitada: exige recompilar e torna QA mais lento.

3) Ler config de JSON/ini
- Rejeitada (por agora): adiciona parsing IO e um novo formato; ScriptableObject Ã© mais simples para Unity e evita bugs de parsing.

## ConsequÃªncias

**Positivas**

- QA pode forÃ§ar Strict/Release via asset, sem rebuild.
- Logs de â€œdegradedâ€ ficam padronizados (chaves, cooldown, sumÃ¡rio), reduzindo ruÃ­do.
- Contrato explÃ­cito: sem config â†’ comportamento antigo.

**Negativas / riscos**

- Requer disciplina para manter o asset de config no projeto (ou aceitar o fallback).
- `Resources.Load` Ã© um mecanismo global; por isso o loader Ã© best-effort e deve rodar apenas em init.

## Invariantes / contrato

- AusÃªncia de `RuntimeModeConfig` **nÃ£o quebra produÃ§Ã£o** e mantÃ©m o comportamento atual.
- `IRuntimeModeProvider.GetMode()` deve ser determinÃ­stico dentro do mesmo run.
- `IDegradedModeReporter.Report(...)` nÃ£o deve causar exceÃ§Ãµes nem travar fluxo.
- Reporter deve limitar spam (cooldown + cap de chaves) e emitir um sumÃ¡rio periÃ³dico quando habilitado.

## Plano de implementaÃ§Ã£o

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
    - registrar provider configurÃ¡vel
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
    - Disparar mÃºltiplas chaves e confirmar cap.
    - Confirmar sumÃ¡rio periÃ³dico quando habilitado.

## EvidÃªncias de ImplementaÃ§Ã£o

EvidÃªncia histÃ³rica: **log de inicializaÃ§Ã£o** enviado pelo projeto (data do log: **2026-02-05**).

Ponteiros operacionais atuais:
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`

Sinais mÃ­nimos de que o ADR estÃ¡ **implementado e ativo**:

- O **asset** foi registrado no DI global:
    - `ServiÃ§o RuntimeModeConfig registrado no escopo global.`
- O `GlobalCompositionRoot` carregou o asset e anunciou:
    - `[RuntimePolicy] RuntimeModeConfig carregado (asset='RuntimeModeConfig').`
- O `IRuntimeModeProvider` foi registrado e resolvido:
    - `ServiÃ§o IRuntimeModeProvider registrado no escopo global.`
    - `ServiÃ§o IRuntimeModeProvider encontrado no escopo global...`
- O `IDegradedModeReporter` foi registrado e resolvido:
    - `ServiÃ§o IDegradedModeReporter registrado no escopo global.`
    - `ServiÃ§o IDegradedModeReporter encontrado no escopo global...`
- A policy de reset em produÃ§Ã£o foi registrada **dependendo** desses serviÃ§os:
    - `ServiÃ§o IWorldResetPolicy registrado no escopo global.`
    - `[RuntimePolicy] IRuntimeModeProvider + IDegradedModeReporter + IWorldResetPolicy registrados no DI global.`

Resultado observado no mesmo log: o fluxo segue normalmente (SceneFlow/WorldLifecycle/GameLoop), indicando que o modo e o reporter nÃ£o travam o boot.

## Nota de atualizaÃ§Ã£o (2026-02-16) â€” Boot canÃ´nico do NewScripts

Para alinhar o boot ao plano `StringsToDirectRefs v1`, esta ADR registra as seguintes regras vigentes:

- `RuntimeModeConfig` Ã© a **raiz canÃ´nica** de configuraÃ§Ã£o do boot global do NewScripts.
- `Resources` Ã© permitido **somente** para carregar `RuntimeModeConfig` no path fixo `RuntimeModeConfig`.
- `NewScriptsBootstrapConfigAsset` Ã© resolvido por **referÃªncia direta** dentro de `RuntimeModeConfig` (`NewScriptsBootstrapConfig`).
- NÃ£o existe caminho obrigatÃ³rio via **provider/manifest em cena** para resolver bootstrap config.
- O entrypoint global do `GlobalCompositionRoot` Ã© Ãºnico e determinÃ­stico em `BeforeSceneLoad`.

