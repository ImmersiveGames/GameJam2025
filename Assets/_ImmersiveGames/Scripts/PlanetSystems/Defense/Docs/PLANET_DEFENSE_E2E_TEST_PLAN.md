# Plano de testes end-to-end — Migração para PlanetDefensePreset (02/12/2025)

## Visão geral
Este plano orienta a validação completa da migração do sistema de defesa planetária para o modelo centrado em `PlanetDefensePresetSo` no Unity 6 (multiplayer local). O foco é garantir SRP, nomenclatura consistente, comportamento de targeting via `DefenseTargetMode`, ondas corretas e desempenho sem picos de GC.

## Escopo e pré-requisitos
- Unity 6 com Test Runner habilitado.
- Cena de multiplayer local com planetas usando `PlanetsMaster` e `PlanetDefenseController` já configurados.
- Prefabs/Assets migrados para usar `PlanetDefensePresetSo` via `PlanetDefenseLoadoutSo` (campos legados permanecem ocultos apenas para fallback).

## Scripts de teste automáticos
Os seguintes testes estão em `_ImmersiveGames.Scripts.PlanetSystems.Defense.Tests` e devem ser executados pelo Unity Test Runner:
- `TestDefenseTargeting.cs`: valida resolução de roles baseada no `DefenseTargetMode` e cache interno de `SimplePlanetDefenseStrategy`.
- `TestDefenseWaves.cs`: valida geração de `ResolvedWaveProfile` no preset (raio, altura, intervalo, contagem) e reutilização de cache sem alocar a cada chamada.
- `TestSRPCompliance.cs`: valida que a estratégia simples não muta o `PlanetDefenseSetupContext` e que o preset mantém SRP ao expor apenas dados de minion, não comportamento.
- `TestPerformanceMultiplayer.cs`: valida reutilização de contexto pelo `PlanetDefensePresetAdapter` em partidas locais para reduzir alocações e logs.

## Passos de execução (Unity Test Runner)
1. Abra o Unity 6, carregue o projeto e a cena de multiplayer local.
2. Abra **Window > General > Test Runner**.
3. Selecione a aba **PlayMode** e rode todos os testes do namespace `_ImmersiveGames.Scripts.PlanetSystems.Defense.Tests`.
4. Verifique que todos os testes passam; caso contrário, ajuste os presets ou loadouts antes de prosseguir.

## Validação manual complementar
### Targeting (enum vs. legado)
- No Inspector do preset, alterne o campo **Target Mode** e valide em runtime que minions alternam entre Player/Eater conforme o enum.
- Confirme que nenhum `ScriptableObject` extra é necessário para targeting.

### Waves (quantidade/intervalo)
- No Profiler ou via logs do Runner, valide que `enemiesPerWave`, `secondsBetweenWaves`, `spawnRadius` e `spawnHeightOffset` do `ResolvedWaveProfile` correspondem ao preset.
- Se `spawnPatternOverride` estiver presente, confira que o perfil em runtime usa o padrão custom sem alterar o asset original.

### SRP e nomenclatura
- Garanta que planetas não alteram comportamento de minion: apenas apontam para `DefensesMinionData` no preset (nenhum método do planeta chama setters de minion).
- Revise nomes: sufixos `So`, `WaveEnemiesCount`, `TargetMode` e `SimplePlanetDefenseStrategy` devem estar consistentes.

### Performance (multiplayer local)
- Execute uma sessão multiplayer local (2+ jogadores) e monitore o **Profiler**:
  - Observe **GC Alloc**; com cache de contexto e wave profile, não deve haver picos significativos ao iniciar/alternar waves.
  - Observe **CPU Timeline** durante `StartWaves` e `ResolveEffectiveConfig` para garantir ausência de alocações repetidas.
- Verifique logs verbose: mensagens de cache reutilizado indicam caminho otimizado.

## Execução em multiplayer local (Unity 6)
1. Inicie cena com dois jogadores locais e um planeta com preset configurado.
2. Dispare detectores (Player e Eater) e confirme que o **DefenseTargetMode** aplicado corresponde ao esperado (ver logs/visual).
3. Troque de preset no `PlanetDefenseLoadoutSo` em runtime e confirme que o cache é invalidado (logs de reconfiguração) e reaplicado sem picos de GC.

## Remoção manual gradual de legados
Após todos os testes passarem:
1. Revisar prefabs e assets que ainda usam campos ocultos (`DefensePoolData`, `WaveProfileOverride`, `DefenseStrategy`).
2. Atualizar para o preset equivalente e salvar prefabs.
3. Remover campos ocultos do `PlanetDefenseLoadoutSo` e apagar assets SO legados não referenciados (ex.: `DefenseWaveProfileSo` redundantes), validando referências quebradas pelo Unity antes de commit.

## Checklist final
- Todos os testes automáticos e manuais concluídos sem falhas.
- Nenhum pico de GC no Profiler durante partidas multiplayer locais.
- Targeting e waves conferem com valores do preset.
- Campos legados revisados e removidos manualmente quando não mais usados.
- Documentação e nomes padronizados confirmados (data desta versão: 02/12/2025).
