# RM-1.5 - Flag/Toggles Governance + Layering Contract

Date: 2026-03-08
Status: DOC-only

## Objetivo
- consolidar a governanca de guards/toggles em uma unica build matrix canonica
- explicitar o layering entre `DevQA` e `RuntimeMode/Logging`
- reduzir duplicacao documental sem alterar codigo runtime

## Contract
- A build matrix canonica passa a viver em `Docs/Shared/Build-Matrix.md`.

- `Docs/Modules/RuntimeMode-Logging.md` e `Docs/Modules/DevQA.md` deixam de manter tabela propria e passam a referenciar a fonte unica.
- `RuntimeMode/Logging` continua owner do boot logging e da policy runtime.
- `DevQA` continua owner do harness/evidencia de dev, mas nao altera boot order, writer de logging policy ou allowlist de bootstrap.
- A allowlist de `RuntimeInitializeOnLoadMethod` fora de `Dev/**`, `Editor/**`, `Legacy/**`, `QA/**` permanece congelada em 2 arquivos.

## Diff de redundancia
- Antes: havia build matrix local em `Docs/Modules/RuntimeMode-Logging.md` e outra em `Docs/Modules/DevQA.md`.
- Depois: a tabela foi consolidada em `Docs/Shared/Build-Matrix.md`.
- Resultado: `RuntimeMode-Logging.md` e `DevQA.md` mantem apenas regras locais e referencia ao doc shared.

## Evidencia estatica obrigatoria
### A) Inventario de guards/toggles no codigo
Comando:
```text
rg -n "UNITY_EDITOR|DEVELOPMENT_BUILD|NEWSCRIPTS_MODE|NEWSCRIPTS_BASELINE_ASSERTS|NEWSCRIPTS_" . -g "*.cs"
```
Resumo curto:
```text
Core/Logging/DebugUtility.cs: NEWSCRIPTS_MODE, UNITY_EDITOR, DEVELOPMENT_BUILD
Infrastructure/Composition/GlobalCompositionRoot.Entry.cs: NEWSCRIPTS_MODE
Infrastructure/Composition/GlobalCompositionRoot.Baseline.cs: NEWSCRIPTS_BASELINE_ASSERTS
Infrastructure/Composition/GlobalCompositionRoot.DevQA.cs: UNITY_EDITOR || DEVELOPMENT_BUILD
Infrastructure/RuntimeMode/UnityRuntimeModeProvider.cs: UNITY_EDITOR, DEVELOPMENT_BUILD
Modules/**/Dev/** e Modules/**/Editor/**: guards DevQA/editor conforme esperado
Nao apareceram novos simbolos ativos de policy como NEWSCRIPTS_QA ou NEWSCRIPTS_DEV.
```

### B) Gates canonicos A / A2 / B
Fonte unica de execucao:
```text
Tools/Gates/Run-NewScripts-RgGates.ps1
```
Leitura:
- o script substitui o gate antigo ambiguo e roda os 3 checks canonicos em sequencia
- Gate A verifica leak de Editor API fora de `Dev/**`, `Editor/**`, `Legacy/**`, `QA/**`
- Gate A2 verifica `InitializeOnLoadMethod` puro com `-w`, evitando pegar substring dentro de `RuntimeInitializeOnLoadMethod`
- Gate B valida a allowlist de `RuntimeInitializeOnLoadMethod` por path relativo normalizado, sem depender de line number

Gate A canonico:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu" <NewScriptsRoot> -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
```
Esperado:
```text
0 matches
```

Gate A2 canonico:
```text
rg -n -w "InitializeOnLoadMethod" <NewScriptsRoot> -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
```
Esperado:
```text
0 matches
```

Gate B canonico:
```text
rg -n "RuntimeInitializeOnLoadMethod" <NewScriptsRoot> -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
```
Esperado:
```text
Core/Logging/DebugUtility.cs
Infrastructure/Composition/GlobalCompositionRoot.Entry.cs
```
Leitura: qualquer outro path fora dessa allowlist faz o Gate B falhar.

## Checklist de PR gate
- A) rodar `Tools/Gates/Run-NewScripts-RgGates.ps1` como execucao canonica dos gates A / A2 / B.
- B) allowlist runtime init: fora de `Dev/**`/`Editor/**`/`Legacy/**`/`QA/**`, aceitar apenas `Core/Logging/DebugUtility.cs` e `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`.
- C) `NEWSCRIPTS_*` scan: qualquer novo simbolo precisa entrar no inventario documental antes de virar policy canonica.

## Arquivos tocados
- `Docs/Shared/Build-Matrix.md`

- `Docs/Modules/RuntimeMode-Logging.md`
- `Docs/Modules/DevQA.md`
- `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Guard-Governance-Audit-v1.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`



