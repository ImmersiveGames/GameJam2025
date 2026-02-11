# Audit — Navigation runtime “Entries empty” / mismatch de snapshot

## Objetivo
Determinar se o sintoma `GameNavigationService inicializado. Entries: []` pode ocorrer no estado atual do repositório, ou se o log necessariamente veio de snapshot antigo (string/arquivo/assembly diferente).

## Conclusão
**FAIL**: no estado atual do repositório, a string `Entries` está presente no código-fonte de `GameNavigationService` (incluindo o log de inicialização), portanto o sintoma pode ocorrer sem depender de snapshot antigo; adicionalmente, o asset canônico contém `to-menu` e `to-gameplay`, então o problema não é ausência dessas rotas no YAML canônico.

## Evidências

### 1) Prova de mismatch de string
**Comando:**
```bash
rg -n "Entries disponíveis:|GameNavigationService inicializado\. Entries:|GameNavigationService inicializado\.\s*Entries:|\bEntries:\b" Assets/_ImmersiveGames/NewScripts/ --glob '!**/Docs/**' || true
```
**Saída:**
```text
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:69:                $"[Navigation] GameNavigationService inicializado. Entries: [{string.Join(", ", _catalog.RouteIds)}]",
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:113:                        $"Entries disponíveis: [{string.Join(", ", _catalog.RouteIds)}]");
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:173:                        $"Entries disponíveis: [{string.Join(", ", _catalog.RouteIds)}].");
```

**Comando:**
```bash
rg -n "GameNavigationService inicializado\. Rotas:|Rotas:" Assets/_ImmersiveGames/NewScripts/Modules/Navigation --glob '!**/Docs/**' || true
```
**Saída:**
```text
<sem resultados>
```

**Veredito do item 1:** existe `Entries` no código atual e não há `Rotas:` no módulo auditado.

### 2) Duplicatas de implementação (arquivo/classe)
**Comando:**
```bash
rg -n "class\s+GameNavigationService\b" Assets/_ImmersiveGames/NewScripts/ --glob '!**/Docs/**'
```
**Saída:**
```text
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:17:    public sealed class GameNavigationService : IGameNavigationService
```

**Comando:**
```bash
rg -n "class\s+GameNavigationCatalogAsset\b" Assets/_ImmersiveGames/NewScripts/ --glob '!**/Docs/**'
```
**Saída:**
```text
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs:23:    public sealed class GameNavigationCatalogAsset : ScriptableObject, IGameNavigationCatalog
```

**Paths encontrados:**
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs`

### 3) Prova do layout canônico de Resources
**Comando:**
```bash
find Assets/Resources -name 'GameNavigationCatalog.asset' -o -name 'TransitionStyleCatalog.asset' -o -name 'LevelCatalog.asset' -o -name 'SceneRouteCatalog.asset' | sort
```
**Saída:**
```text
Assets/Resources/Levels/LevelCatalog.asset
Assets/Resources/Navigation/GameNavigationCatalog.asset
Assets/Resources/Navigation/LevelCatalog.asset
Assets/Resources/Navigation/TransitionStyleCatalog.asset
Assets/Resources/SceneFlow/SceneRouteCatalog.asset
```

**Comando (duplicatas por nome):**
```bash
find Assets -name 'GameNavigationCatalog.asset' -o -name 'TransitionStyleCatalog.asset' -o -name 'LevelCatalog.asset' -o -name 'SceneRouteCatalog.asset' | xargs -n1 basename | sort | uniq -cd
```
**Saída:**
```text
      2 LevelCatalog.asset
```

**Registro:** há risco de confusão humana por nome duplicado (`LevelCatalog.asset`) em paths distintos.

### 4) Prova de conteúdo mínimo do GameNavigationCatalog canônico
**Comando:**
```bash
sed -n '1,220p' Assets/Resources/Navigation/GameNavigationCatalog.asset
```
**Saída (trecho YAML):**
```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: c955e7a29f314edda14aaa33e5763f86, type: 3}
  m_Name: GameNavigationCatalog
  m_EditorClassIdentifier:
  routes:
  - routeId: to-menu
    sceneRouteId:
      _value: to-menu
    transitionStyleId:
      _value: style.frontend
    scenesToLoad:
    - MenuScene
    - UIGlobalScene
    scenesToUnload:
    - GameplayScene
    targetActiveScene: MenuScene
  - routeId: to-gameplay
    sceneRouteId:
      _value: to-gameplay
    transitionStyleId:
      _value: style.gameplay
    scenesToLoad:
    - GameplayScene
    - UIGlobalScene
    scenesToUnload:
    - MenuScene
    targetActiveScene: GameplayScene
  warnOnInvalidRoutes: 1
```

**Comando (contagem de rotas no YAML):**
```bash
rg -n "^  - routeId:" Assets/Resources/Navigation/GameNavigationCatalog.asset
```
**Saída:**
```text
16:  - routeId: to-menu
27:  - routeId: to-gameplay
```

**Veredito do item 4:** catálogo canônico contém as rotas `to-menu` e `to-gameplay` (2 rotas).

### 5) Prova do fluxo do botão Play
**Comando:**
```bash
rg -n "MenuPlayButtonBinder|to-gameplay|RestartAsync|ExitToMenuAsync|StartGameplayAsync" Assets/_ImmersiveGames/NewScripts/Modules/Navigation --glob '!**/Docs/**'
```
**Saída (recorte relevante):**
```text
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Bindings/MenuPlayButtonBinder.cs:54:                _navigation.RestartAsync(actionReason),
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationIntents.cs:9:        public const string ToGameplay = "to-gameplay";
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:76:        public Task RestartAsync(string reason = null)
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:79:        public Task ExitToMenuAsync(string reason = null)
Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:90:        public async Task StartGameplayAsync(LevelId levelId, string reason = null)
```

**Comando:**
```bash
nl -ba Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Bindings/MenuPlayButtonBinder.cs | sed -n '1,220p'
```
**Saída (recorte relevante):**
```text
48	            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
49	                $"[Navigation] Play solicitado. reason='{actionReason}'.",
50	                DebugUtility.Colors.Info);
...
53	            NavigationTaskRunner.FireAndForget(
54	                _navigation.RestartAsync(actionReason),
55	                typeof(MenuPlayButtonBinder),
56	                "Menu/Play");
```

**Veredito do item 5:** o botão Play chama `RestartAsync(actionReason)`; o intent textual `to-gameplay` existe em `GameNavigationIntents`.

## PASS/FAIL final
- Critério **PASS** exigia ausência de `Entries` e presença de `Rotas:`; isso **não foi atendido**.
- Resultado final: **FAIL**.

## Ação humana recomendada (apenas 1)
Corrigir especificamente as strings de logging/diagnóstico da implementação atual de navegação para eliminar a divergência entre terminologia esperada (`Rotas`) e efetivamente emitida (`Entries`) no código em produção.
