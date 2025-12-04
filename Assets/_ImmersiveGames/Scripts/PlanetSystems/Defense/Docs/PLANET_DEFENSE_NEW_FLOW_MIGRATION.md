# Migração e Boas Práticas do Novo Fluxo de Defesa Planetária (03/12/2025)

## Visão geral (fluxo end-to-end)
- **Editor**: configure cada planeta com uma lista de `DefenseEntryConfigSO` e escolha o `DefenseChoiceMode` (Random ou Sequential). Cada entrada referencia `DefenseMinionConfigSO` + `WavePresetSo` por role detectado e um preset default obrigatório.
- **Preload**: o `PlanetDefenseOrchestrationService` faz preload único de todos os `PoolData` presentes nas entradas/binds/defaults, emitindo `LogError` via `DebugUtility` se algum pool estiver ausente.
- **Runtime**: a cada detecção, o orquestrador resolve a entrada (modo escolhido), pega o `WavePresetSo` pelo role (ou default), calcula o raio dinâmico via `SkinRuntimeStateTracker` (`ApproxRadius` + `SpawnOffset`) e entrega o contexto para o runner.
- **Spawn**: o runner instancia inimigos usando `NumberOfEnemiesPerWave`, `IntervalBetweenWaves` e `SpawnPattern` opcional, sempre com Y = 0 (top-down). Falhas de configuração são sinalizadas cedo para evitar comportamentos silenciosos.

## Boas práticas
- **SRP primeiro**: `DefenseEntryConfigSO` apenas mapeia role → (minion config + wave preset) e offset; `WavePresetSo` guarda dados da onda e do pool; lógica de spawn fica no serviço/runner.
- **Nomenclatura clara**: use sufixo `So` em ScriptableObjects, nomes explícitos (`EntryDefaultWavePreset`, `NumberOfEnemiesPerWave`, `SpawnOffset`) e tooltips em português para orientar designers.
- **Fail-fast em todas as etapas**: mantenha `OnValidate` para presets/entries e logs de runtime no orquestrador/runner. Não dependa de fallbacks implícitos.
- **Multiplayer local**: reuse presets compartilhados entre planetas para reduzir GC e manter consistência de dificuldade; evite duplicar `PoolData` quando o comportamento é o mesmo.
- **Performance**: confie no cache de radius e no cache de entradas sequenciais. Não recalcule binds ou radius por frame.

## Dicas de uso (Editor e Runtime)
- **Editor**:
  - Sempre preencha `EntryDefaultWavePreset` antes de mapear roles específicos para evitar erros de configuração.
  - Defina `SpawnOffset` somente para ajustar levemente o perímetro (raios extremos devem ser resolvidos no SkinTracker).
  - Prefira presets modulares: um `WavePresetSo` por padrão de minion + contagem/intervalo, reutilizado em múltiplas entradas.
- **Runtime**:
  - Invoque `ConfigureDefenseService` no `OnEnable` do planeta, garantindo que a lista de entradas esteja carregada antes de qualquer detecção.
  - Monitore logs do `DebugUtility` durante testes: mensagens de preload ou resolução de presets indicam problemas de assets.

## Exemplos rápidos
- **Planeta com comportamento único**: lista com uma `DefenseEntryConfigSO`, `DefenseChoiceMode.Sequential`. Apenas o default preenchido, binds por role vazios.
- **Planeta com reações distintas**: duas entradas; Sequential alterna entre "onda padrão" e "onda agressiva". Dentro de cada entrada, mapear `DefenseRole.Player` para um preset especializado e manter default para `Unknown`.
- **Random para variação**: uma lista de três entradas leves/pesadas; `DefenseChoiceMode.Random` sorteia a cada detecção, mantendo a mesma resolução de bind (role → preset) dentro daquela entrada.

## Plano de migração de testes (manual)
1. **Inventário dos testes antigos**
   - Localize testes que dependem de `DefenseLoadout` ou perfis legados. Liste cenas e fixtures afetadas.
2. **Preparar assets do novo fluxo**
  - Crie `DefenseEntryConfigSO` com default obrigatório e binds necessários, referenciando `WavePresetSo` existentes e `DefenseMinionConfigSO` para cada bind.
  - Confirme `PoolData`, contagem e intervalos em cada `WavePresetSo` (fail-fast no `OnValidate`).
3. **Atualizar fixtures**
  - Substitua mocks/loadouts por coleções de `DefenseEntryConfigSO` + `DefenseChoiceMode` injetados no orquestrador.
   - Ajuste asserts para verificar preload de `PoolData`, seleção de entrada (random/sequential) e resolução de bind por role.
4. **Cobrir radius dinâmico**
   - Nos testes, forneça `SkinRuntimeStateTracker` com `ApproxRadius` controlado; valide `SpawnOffset` aplicado e Y fixo em 0.
5. **Validar fail-fast**
   - Inclua casos com `PoolData` ausente e `NumberOfEnemiesPerWave <= 0` para checar `LogError` via `DebugUtility`.
6. **Executar sequência completa**
   - Simule detecção → resolução de entrada → preload → spawn de waves. Confirme que caches (radius e sequência) são reutilizados entre chamadas.

## Atualizações
- **03/12/2025**: documentação do novo fluxo (Entries + WavePreset), migração de testes e recomendações de uso em Unity 6 (multiplayer local).
