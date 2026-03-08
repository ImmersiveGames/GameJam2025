# CORE-1.2i - Core gates and promotion path

Date: 2026-03-07
Status: DOC-only

## Objetivo
- registrar gates de PR/CI para o trilho `Core` a partir do workspace local
- explicitar que `C reserve` e estoque de migracao, nao `Legacy`

## Canonical gates
### A) Strict editor leak sweep
Comando canonico:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/Legacy/**" -g "!**/QA/**"
```

Regra alvo:
- deve retornar `0` hits de leak de Editor no runtime

Observado hoje no workspace local:
```text
.\Infrastructure\Composition\GlobalCompositionRoot.Entry.cs:61:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
.\Core\Logging\DebugUtility.cs:51:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
```

Leitura:
- os dois hits acima nao sao leak de Editor; aparecem por sobreposicao textual com `RuntimeInitializeOnLoadMethod`
- para gate de PR/CI, a interpretacao correta e: `0` hits de `UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod` fora da allowlist de runtime init

### B) Runtime init allowlist
Comando canonico:
```text
rg -n "RuntimeInitializeOnLoadMethod" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Editor/**" -g "!**/Dev/**" -g "!**/Legacy/**" -g "!**/QA/**"
```

Regra alvo:
- deve retornar exatamente os dois arquivos abaixo

Observado hoje no workspace local:
```text
.\Core\Logging\DebugUtility.cs:51:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
.\Infrastructure\Composition\GlobalCompositionRoot.Entry.cs:61:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
```

### C) Reserve promotion probe (informativo)
Comando canonico:
```text
rg -n -w "FilteredEventBus|EventBusUtil|StateMachine|Preconditions|SceneServiceCleaner" Assets/_ImmersiveGames/NewScripts/Infrastructure Assets/_ImmersiveGames/NewScripts/Modules -g "*.cs"
```

Regra alvo:
- idealmente `0` hits reais de runtime hoje; quando surgirem callsites novos, isso vira gatilho de promocao `C -> A`

Observado hoje no workspace local:
```text
.\Modules\Gates\SimulationGateTokens.cs:11:    ///   isso deve vir do GameLoop/StateMachine e eventos, não do Gate.
```

Leitura:
- o hit atual e apenas comentario, nao callsite runtime real
- para promocao, exigir uso concreto por simbolo/owner e nao comentario ou texto incidental

## Promotion path
- `Reserve -> Canonical`: callsite real em `Modules/**` e/ou `Infrastructure/**`, owner definido, evidence `rg` por simbolo e impacto documentado no audit.
- `Canonical/Reserve -> Legacy`: substituto canonico claro, `0` refs reais de codigo e assets, isolamento de compatibilidade e documentacao da transicao.
- `Reserve` nao e `Legacy`: e estoque de migracao mantido no `Core` para futura promocao sem reabrir trilho compat antigo.

## Arquivos relacionados
- `Docs/Modules/Core.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`
