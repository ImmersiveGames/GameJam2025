# RuntimeModeConfig - Como criar o asset

1) No Unity, crie o asset via menu:
   Create -> ImmersiveGames -> Runtime Mode Config

2) Salve em:
   Assets/_ImmersiveGames/NewScripts/Resources/RuntimeModeConfig.asset

3) Valores padrão recomendados (já configurados no script):
   - ModeOverride: Auto
   - DedupStrategy: CooldownSeconds
   - CooldownSeconds: 5
   - EmitSummaryEverySeconds: 30
   - MaxUniqueKeys: 256
   - LogFirstOccurrence: true
   - IncludeCountInLog: true
   - Strict: DegradedAsError=true, DegradedAsException=false

Observação:
- Se o asset não existir, o sistema deve cair em modo Auto (comportamento atual).
