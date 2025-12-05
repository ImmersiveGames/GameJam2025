# Plano de Testes de Fluxo de Defesa Planetária

Este plano descreve verificações para o fluxo completo do orquestrador de defesas, cobrindo preload, escolha de entradas, cálculo de raio dinâmico e validações fail-fast.

## Objetivos
- Garantir que o preload de pools seja executado apenas uma vez por PoolData.
- Validar que a escolha de entradas respeita o modo (Random/Sequential) e reutiliza o cache quando aplicável.
- Confirmar que o raio dinâmico usa o `ApproxRadius` do `SkinRuntimeStateTracker` somado ao `SpawnOffset` da entrada.
- Assegurar que validações fail-fast são disparadas para presets inválidos em runtime.

## Cenários e Scripts
- **Preload único por PoolData**
  - Script: `TestPlanetDefenseFlow.PreloadRegistersUniquePools`
  - Passo a passo: criar dois `WavePresetSo` que referenciam o mesmo `PoolData`, configurar duas entradas e chamar `ConfigureDefenseEntries`. Verificar que o `PoolManager` registra apenas uma vez.

- **Escolha sequencial com cache por índice**
  - Script: `TestPlanetDefenseFlow.SequentialEntryIsCachedPerIndex`
  - Passo a passo: configurar duas entradas, modo `Sequential`, resolver configuração duas vezes e confirmar que o cache mantém o mesmo `WavePreset` para o mesmo índice, avançando para o próximo na sequência.

- **Raio dinâmico com offset**
  - Script: `TestPlanetDefenseFlow.RespectsCachedRadiusWithOffset`
  - Passo a passo: preencher cache de raio no serviço, aplicar um `SpawnOffset` na entrada e verificar que o `SpawnRadius` no contexto soma ambos os valores.

- **Fail-fast em presets inválidos**
  - Script: `TestPlanetDefenseFlow.FlagsInvalidWavePresetAtRuntime`
  - Passo a passo: criar `WavePresetSo` com `NumberOfMinionsPerWave` zero e `SpawnPattern` preenchido, configurar entrada e resolver contexto. Verificar que o log de erro é emitido (usando `LogAssert` em EditMode) e que o contexto ainda é retornado para inspeção.

## Observações
- Os testes devem rodar em modo de edição para evitar dependência de cenas específicas.
- Injeções no orquestrador podem ser feitas via reflexão para stubs simples de runner/log.
- Manter comentários e mensagens em português para facilitar depuração pela equipe de game design.
