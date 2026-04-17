# Docs Consolidation - Baseline 4.0

## Objective

Consolidar `Assets/_ImmersiveGames/NewScripts/Docs` depois da estabilização do Baseline 4.0, deixando:

- canon atual curto e inequívoco;
- entradas ativas fáceis de localizar;
- histórico útil preservado sem competir com o canon;
- material redundante, espelhado ou superado fora do fluxo principal.

### Inventory matrix

| Caminho | Tipo | Status recomendado | Motivo curto |
|---|---|---|---|
| `Docs/ADRs/ADR-0001-Glossario-Fundamental-Contextos-e-Rotas-v2.md` | ADR | Keep Canonical | glossario e taxonomia base do projeto |
| `Docs/ADRs/ADR-0043-Ancora-de-Decisao-para-o-Baseline-4.0.md` | ADR | Keep Canonical | ancora de decisao do baseline atual |
| `Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md` | ADR | Keep Canonical | canon arquitetural do baseline 4.0 |
| `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md` | Plan | Keep Canonical | blueprint operacional ainda valido |
| `Docs/Plans/Plan-Baseline-4.0-Execution-Guardrails.md` | Plan | Keep Canonical | guardrails de execucao ativos |
| `Docs/README.md` | Index | Keep Active | entrada humana principal para leitura da pasta |
| `Docs/CHANGELOG-docs.md` | Changelog | Keep Active | trilha de mudancas da documentacao |
| `Docs/Reports/Audits/LATEST.md` | Audit index | Keep Active | ponto de entrada da trilha de auditoria atual |
| `Docs/Reports/Audits/2026-03-30/Structural-Freeze-Snapshot.md` | Audit | Keep Active | congela o estado estrutural consolidado |
| `Docs/Reports/Audits/2026-03-30/Structural-Xray-NewScripts.md` | Audit | Keep Active | mapa estrutural resumido e vigente |
| `Docs/Reports/Audits/2026-03-30/Docs-Consolidation-Baseline-4.0.md` | Audit | Keep Active | registro formal desta limpeza |
| `Docs/Guides/Production-How-To-Use-Core-Modules.md` | Guide | Keep Active | guia operacional ainda util |
| `Docs/Guides/Event-Hooks-Reference.md` | Guide | Keep Active | referencia pratica de hooks ativos |
| `Docs/Guides/How-To-Add-A-New-Module-To-Composition.md` | Guide | Keep Active | guia operacional de composicao |
| `Docs/Modules/README.md` | Index | Keep Active | indice operacional dos modulos ativos |
| `Docs/Modules/GameLoop.md` | Module | Keep Active | leitura atual do owner de run lifecycle |
| `Docs/Archive/Modules/LevelFlow.md` | Module | Archive | leitura operacional de level lifecycle |
| `Docs/Modules/SceneReset.md` | Module | Keep Active | referencia do executor local de reset |
| `Docs/Modules/WorldReset.md` | Module | Keep Active | owner do reset macro |
| `Docs/Modules/ResetInterop.md` | Module | Keep Active | ponte entre SceneFlow e WorldReset |
| `Docs/Modules/Save.md` | Module | Keep Active | superficie oficial de hooks e contratos |
| `Docs/Archive/Modules/PostRun.md` | Module | Archive | borda de pos-run ainda em uso |
| `Docs/Modules/Navigation.md` | Module | Keep Active | dispatch macro e intents de navegacao |
| `Docs/Modules/Gameplay.md` | Module | Keep Active | leitura de gameplay/state/GameplayReset |
| `Docs/Modules/InputModes.md` | Module | Keep Active | contrato ativo de input mode |
| `Docs/Reports/Evidence/LATEST.md` | Evidence | Keep Active | indice de evidencia ainda util |
| `Docs/ADRs/README.md` | Index | Keep Historical | leitura de precedencia e contexto historico dos ADRs |
| `Docs/Reports/Audits/2026-03-30/Folder-Naming-Audit-NewScripts.md` | Audit | Keep Historical | evidencia recente, mas fora do canon Baseline 4.0 |
| `Docs/Archive/TopLevel/Canon-Index.md` | Historical doc | Archive | espelho historico sem valor operacional |
| `Docs/Archive/TopLevel/Modular-Composition-Pipeline.md` | Historical doc | Archive | repetia o canon de composicao em outra linguagem |
| `Docs/Archive/TopLevel/Official-Baseline-Hooks.md` | Historical doc | Archive | duplicava o contrato oficial de hooks |
| `Docs/Archive/Reports/Audits/2026-03-29/Baseline-4.0-Residual-Housekeeping-Audit.md` | Audit | Archive | registro residual agora supersedido pelo freeze/consolidation |
| `Docs/Canon/` | Folder | Delete | pasta vazia apos mover os espelhos redundantes |
| `Docs/Canon.meta` | Meta | Delete | meta da pasta vazia removida |
| `Docs/Reports/Audits/2026-03-29/` | Folder | Delete | pasta vazia apos arquivar o audit residual |
| `Docs/Reports/Audits/2026-03-29.meta` | Meta | Delete | meta da pasta vazia removida |

## Canonical Set Kept

- `Docs/ADRs/ADR-0001-Glossario-Fundamental-Contextos-e-Rotas-v2.md`
- `Docs/ADRs/ADR-0043-Ancora-de-Decisao-para-o-Baseline-4.0.md`
- `Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md`
- `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md`
- `Docs/Plans/Plan-Baseline-4.0-Execution-Guardrails.md`

## Active Set Kept

- `Docs/README.md`
- `Docs/CHANGELOG-docs.md`
- `Docs/Reports/Audits/LATEST.md`
- `Docs/Reports/Audits/2026-03-30/Structural-Freeze-Snapshot.md`
- `Docs/Reports/Audits/2026-03-30/Structural-Xray-NewScripts.md`
- `Docs/Reports/Audits/2026-03-30/Docs-Consolidation-Baseline-4.0.md`
- `Docs/Guides/Event-Hooks-Reference.md`
- `Docs/Guides/How-To-Add-A-New-Module-To-Composition.md`
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
- `Docs/Modules/README.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/Gameplay.md`
- `Docs/Modules/InputModes.md`
- `Docs/Archive/Modules/LevelFlow.md`
- `Docs/Modules/Navigation.md`
- `Docs/Archive/Modules/PostRun.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/Save.md`
- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/WorldReset.md`
- `Docs/Reports/Evidence/LATEST.md`

## Historical Set Kept

- `Docs/ADRs/README.md`
- `Docs/Reports/Audits/2026-03-30/Folder-Naming-Audit-NewScripts.md`

## Archived

- `Docs/Archive/TopLevel/Canon-Index.md`
- `Docs/Archive/TopLevel/Modular-Composition-Pipeline.md`
- `Docs/Archive/TopLevel/Official-Baseline-Hooks.md`
- `Docs/Archive/Reports/Audits/2026-03-29/Baseline-4.0-Residual-Housekeeping-Audit.md`

## Deleted

- `Docs/Canon/`
- `Docs/Canon.meta`
- `Docs/Reports/Audits/2026-03-29/`
- `Docs/Reports/Audits/2026-03-29.meta`

## Notes

- Os `.meta` dos arquivos movidos foram preservados junto com os próprios documentos.
- O canon atual ficou concentrado em ADRs + blueprint + guardrails, sem concorrencia com espelhos top-level antigos.
- Os pontos de entrada ativos agora apontam para a trilha atual de auditoria e para os guias operacionais que ainda ajudam a ler o Baseline 4.0.
- O material histórico útil continua acessivel no `Docs/Archive`, mas nao disputa autoridade com o canon.
- A proxima direcao do projeto permanece fora do baseline estabilizado e segue para `player`, `enemies` e objetos programaticos.

