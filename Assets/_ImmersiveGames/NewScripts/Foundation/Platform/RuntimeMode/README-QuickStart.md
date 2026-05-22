# RuntimeModeConfig - Como criar o asset

1) No Unity, crie o asset via menu:
   Create -> ImmersiveGames -> Runtime Mode Config

2) Salve em:
   Assets/_ImmersiveGames/NewScripts/Resources/RuntimeMode/RuntimeModeConfig.asset

3) Referencie explicitamente um `RuntimeConfigSetAsset` valido em `RuntimeModeConfig.runtimeConfigSet`.

4) Valores padrao recomendados (ja configurados no script):
   - ModeOverride: Auto
   - CompositionProfile: Base11Sandbox

Observacao:
- Se o asset nao existir, o bootstrap falha cedo.
- Nao ha fallback via BootstrapConfigAsset.
- Caminho canonico ativo: RuntimeModeConfig -> RuntimeConfigSetAsset -> RuntimeConfigRegistry -> RuntimeConfigSnapshot read-only.

