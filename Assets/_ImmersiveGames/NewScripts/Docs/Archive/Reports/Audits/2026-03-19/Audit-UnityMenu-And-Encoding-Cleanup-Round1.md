# Audit Unity Menu And Encoding Cleanup Round 1

## 1. Resumo executivo
- Estado encontrado: 5 entradas `MenuItem` ativas em `Assets/_ImmersiveGames/**`; 4 eram superfícies obsoletas (QA legado ou diagnóstico histórico) e 1 era tooling canônico.
- Problemas principais: poluição do menu raiz `ImmersiveGames/*` por scripts legados e inconsistência de encoding textual (UTF-8 com BOM, arquivos não UTF-8 e mojibake em comentários/docs).
- Limpeza aplicada: remoção das superfícies de menu obsoletas (incluindo scripts inteiros quando eram menu-only), preservação do menu canônico de validação SceneFlow e normalização de encoding para UTF-8 sem BOM em `NewScripts` com reparo de mojibake quando seguro.

## 2. Inventário de menus Unity encontrados
| Menu Path | Script | Kind | Decision | Reason |
|---|---|---|---|---|
| `ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs` | `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs` | Utility / QA | `REMOVE_OBSOLETE` | Ferramenta QA auxiliar, fora do trilho canônico atual. |
| `ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config` | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs` | Utility / canonical config validation | `KEEP_CANONICAL` | Valida assets/config canônicos (`NavigationCatalog`, `SceneRouteCatalog`, `BootstrapConfig`) e gera relatório oficial. |
| `ImmersiveGames/Scene Flow/Listar grupos e cenas (SceneFlowMap)` | `Assets/_ImmersiveGames/Scripts/SceneManagement/Editor/SceneFlowDebugTools.cs` | Legacy diagnostics | `REMOVE_OBSOLETE` | Diagnóstico do trilho legado `SceneFlowMap/SceneGroupProfile`; não canônico. |
| `ImmersiveGames/Scene Flow/Validar cenas em Build Settings` | `Assets/_ImmersiveGames/Scripts/SceneManagement/Editor/SceneFlowDebugTools.cs` | Legacy diagnostics | `REMOVE_OBSOLETE` | Mesmo script legado, sem papel no fluxo atual de NewScripts. |
| `ImmersiveGames/Diagnostics/Generate Architecture Audit (Step 0)` | `Assets/_ImmersiveGames/Scripts/Utils/Diagnostics/Editor/Diagnostics.Editor.cs` | Legacy diagnostics/report generator | `REMOVE_OBSOLETE` | Auditoria histórica de etapa inicial; não representa o estado canônico atual. |

## 3. Menus removidos
| Menu Path | Script/File | Removal Type | Reason |
|---|---|---|---|
| `ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs` | `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Editor/Tools/GameNavigationCatalogNormalizer.cs` | Script removal (`.cs` + `.meta`) | Menu QA não canônico e arquivo dedicado a essa superfície. |
| `ImmersiveGames/Scene Flow/Listar grupos e cenas (SceneFlowMap)` | `Assets/_ImmersiveGames/Scripts/SceneManagement/Editor/SceneFlowDebugTools.cs` | Script removal (`.cs` + `.meta`) | Ferramental de debug legado do trilho antigo. |
| `ImmersiveGames/Scene Flow/Validar cenas em Build Settings` | `Assets/_ImmersiveGames/Scripts/SceneManagement/Editor/SceneFlowDebugTools.cs` | Script removal (`.cs` + `.meta`) | Mesmo motivo acima (legado). |
| `ImmersiveGames/Diagnostics/Generate Architecture Audit (Step 0)` | `Assets/_ImmersiveGames/Scripts/Utils/Diagnostics/Editor/Diagnostics.Editor.cs` | Script removal (`.cs` + `.meta`) | Diagnóstico histórico obsoleto sem função canônica atual. |

## 4. Menus mantidos
| Menu Path | Script/File | Why Kept |
|---|---|---|
| `ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config` | `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs` | Ferramenta editorial canônica para validação fail-fast da configuração de SceneFlow/NewScripts. |

## 5. Encoding auditado
| File Path | Previous Encoding / Status | Action Taken | Notes |
|---|---|---|---|
| `Assets/_ImmersiveGames/NewScripts/**` (extensões: `.cs`, `.md`, `.txt`, `.json`, `.yaml`, `.yml`, `.uxml`, `.uss`, `.asmdef`, `.cginc`, `.shader`, `.html`) | Varredura inicial: `NON_UTF8=7`, `UTF8_BOM=121`, `UTF8_NO_BOM=restante` | Conversão para `UTF-8` sem BOM em todos os casos necessários | Sem alteração de binários; line ending preservado onde possível. |
| `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Infrastructure/Actors/Bindings/Player/PlayerActorAdapter.cs` | `NON_UTF8` | `CONVERT_CP1252_TO_UTF8` | Conteúdo preservado e normalizado. |
| `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupRearm/Core/ActorGroupRearmContracts.cs` | `NON_UTF8` | `CONVERT_CP1252_TO_UTF8` | Conteúdo preservado e normalizado. |
| `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupRearm/Core/ActorGroupRearmOrchestrator.cs` | `NON_UTF8` | `CONVERT_CP1252_TO_UTF8` | Conteúdo preservado e normalizado. |
| `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupRearm/Core/DefaultActorGroupRearmTargetClassifier.cs` | `NON_UTF8` | `CONVERT_CP1252_TO_UTF8` | Conteúdo preservado e normalizado. |
| `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupRearm/Core/IActorGroupRearmTargetClassifier.cs` | `NON_UTF8` | `CONVERT_CP1252_TO_UTF8` | Conteúdo preservado e normalizado. |
| `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Runtime/MacroRestartCoordinator.cs` | `NON_UTF8` | `CONVERT_CP1252_TO_UTF8` | Conteúdo preservado e normalizado. |
| `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/Policies/ProductionWorldResetPolicy.cs` | `NON_UTF8` | `CONVERT_CP1252_TO_UTF8` | Conteúdo preservado e normalizado. |
| `Assets/_ImmersiveGames/NewScripts/**` (121 arquivos) | `UTF8_BOM` | `REMOVE_BOM` | Normalização para UTF-8 sem BOM. |
| `Assets/_ImmersiveGames/NewScripts/**` (múltiplos arquivos com comentários/docs) | `UTF8_NO_BOM` + mojibake visível | `FIX_MOJIBAKE` pontual | Correções aplicadas por heurística e mapa de sequências quebradas. |

## 6. Textos corrompidos corrigidos
- Correções de acentuação e símbolos aplicadas, entre outros, em:
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/RuntimeModeConfig.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Bindings/WorldLifecycleController.cs`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Core/Logging/DebugUtility.cs`
  - `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0007-InputModes.md`
  - `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0008-RuntimeModeConfig.md`
  - `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0009-FadeSceneFlow.md`
  - `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md`
  - `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md`
  - `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0020-LevelContent-Progression-vs-SceneRoute.md`
- Exemplos de reparo: sequências quebradas como `ConfiguraÃ§Ã£o`, `NÃƒO`, `Ã‚ncoras`, `â‰ `, `âš ï¸` foram normalizadas para texto legível.

## 7. Itens que exigem revisão manual
- Não houve bloqueio funcional.
- Observação: busca automática por padrões (`Ã|Â|â|�`) ainda retorna ocorrências legítimas de português com acentuação (ex.: `NÃO`, `Âncoras`, `âncoras`), então a inspeção residual deve ser semântica, não apenas por regex bruta.

## 8. Sanity checks
- Menus:
  - Revarredura global em `Assets/_ImmersiveGames/**` com `rg "\[MenuItem\("`.
  - Resultado final: apenas 1 menu canônico remanescente (`SceneFlow/Validate Config`).
- Encoding:
  - Auditoria inicial de encoding em `NewScripts` (status por arquivo).
  - Conversão em lote para UTF-8 sem BOM.
  - Revalidação final com decoder UTF-8 estrito: `NON_UTF8=0; UTF8_BOM=0`.
- Integridade:
  - Removidos `.meta` correspondentes dos scripts excluídos.
  - Sem alterações em binários/assets não textuais.

## 9. Resumo final
- Estado do menu principal após limpeza: removidas superfícies obsoletas de QA/diagnóstico legado; mantida somente a entrada editorial canônica de validação SceneFlow.
- Estado de UTF-8 no repositório (escopo da rodada): `NewScripts` ficou totalmente normalizado para UTF-8 sem BOM, com correções de mojibake visível aplicadas onde houve evidência segura.