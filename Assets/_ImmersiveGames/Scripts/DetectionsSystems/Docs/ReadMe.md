# 🛰️ Sistema de Detecção — Documentação Oficial (v1.0)

## 📚 Índice

1. [Visão Geral](#visão-geral)
2. [Fluxo de Detecção](#fluxo-de-detecção)
3. [Componentes Core](#componentes-core)
4. [Scriptable Objects e Dados](#scriptable-objects-e-dados)
5. [Serviço de Sensores](#serviço-de-sensores)
6. [Eventos e Integrações](#eventos-e-integrações)
7. [Ferramentas de Debug](#ferramentas-de-debug)
8. [Configuração Passo a Passo](#configuração-passo-a-passo)
9. [Extensibilidade e Boas Práticas](#extensibilidade-e-boas-práticas)
10. [Troubleshooting](#troubleshooting)

---

## 🎯 Visão Geral

O **Detection System** garante que atores detectem alvos dentro do multiplayer local com latência baixa e zero alocação em runtime.
Todo o pipeline é orientado à interface (`IDetector` ⇄ `IDetectable`) e desacoplado via **EventBus**, respeitando SOLID, favorecendo
substituição por implementações customizadas e testes isolados.

### ✨ Destaques

- **Contrato comum** — `IDetector` e `IDetectable` padronizam callbacks de entrada e saída (`OnDetected`/`OnLost`).
- **Sensores componíveis** — Cada `SensorConfig` encapsula comportamento (raio, layer, modo esférico/cônico, frequência).
- **Processamento sem GC** — `Physics.OverlapSphereNonAlloc` e caches de frame evitam duplicidade de eventos e alocações.
- **Integração por eventos** — `DetectionEnterEvent`/`DetectionExitEvent` trafegam pelo `EventBus`, mantendo acoplamento mínimo.
- **Ferramentas de debug** — `SensorDebugVisualizer` e editores customizados facilitam inspeção e tuning em tempo real.

---

## 🧭 Fluxo de Detecção

```
SensorController (MonoBehaviour)
    └─ DetectorService
          ├─ Sensor (1..N por Collection)
          │     ├─ Physics.OverlapSphereNonAlloc
          │     ├─ Filtro por modo (Esfera / Cone)
          │     ├─ Cache de frame ENTER/EXIT
          │     └─ EventBus<DetectionEnter/Exit>
          └─ Atualização por Update(deltaTime)

EventBus
    ├─ AbstractDetector → OnDetected / OnLost
    └─ AbstractDetectable → OnEnterDetection / OnExitDetection
```

1. `SensorController` injeta `DetectorService` com um `IDetector` e uma `SensorCollection`.
2. `DetectorService.Update` percorre cada `Sensor` com a cadência configurada em `SensorConfig.MaxFrequency`.
3. `Sensor` coleta colisores (`Physics.OverlapSphereNonAlloc`), filtra self-collisions e modo cônico (`IsInCone`).
4. Novas detecções geram `DetectionEnterEvent`; saídas geram `DetectionExitEvent`, ambos publicados no `EventBus`.
5. `AbstractDetector` e `AbstractDetectable` consomem os eventos e disparam os métodos abstratos para a lógica específica.

---

## 🧱 Componentes Core

### `IDetector` e `IDetectable`
Interfaces declaradas em `Core/IDetector.cs`. Exigem um `Owner : IActor` para identificação unificada e callbacks para entrada/saída
(`OnDetected`, `OnLost`, `OnEnterDetection`, `OnExitDetection`).

### `DetectionType`
ScriptableObject (`Core/DetectionType.cs`) usado como **namespace lógico** para múltiplos sensores coexistirem sem interferência. Deve
ser atribuído tanto no detector quanto no detectável correspondente.

### `AbstractDetector`
Base MonoBehaviour para detectores (`Mono/AbstractDetector.cs`):
- Resolve `IActor` em `Awake` e registra `EventBinding` para `DetectionEnterEvent` e `DetectionExitEvent`.
- Mantém `HashSet<IDetectable>` e cache por frame para evitar notificações duplicadas no mesmo frame.
- Implementações concretas sobrescrevem `OnDetected`/`OnLost` e podem consultar `GetDetectedItems()` para lógica adicional.

### `AbstractDetectable`
Base para alvos detectáveis (`Mono/AbstractDetectable.cs`):
- Valida `IActor` e `DetectionType` em `Awake`.
- Filtra eventos recebidos pelo `EventBus`, cacheando por frame (`Dictionary<string,int>`) para evitar múltiplos callbacks.
- Implementações concretas apenas lidam com `OnEnterDetection`/`OnExitDetection` e podem usar o helper `GetName` para logs.

---

## 📦 Scriptable Objects e Dados

### `SensorConfig`
`Runtime/SensorConfig.cs` define as propriedades do sensor:
- **DetectionType** (ScriptableObject obrigatório).
- **TargetLayer** (LayerMask) com os layers válidos.
- **Radius** + **Min/MaxFrequency** (limite inferior de update; o sistema utiliza `MaxFrequency`).
- **DebugMode** ativa gizmos/rays.
- **DetectionMode** (`Spherical` ou `Conical`), com `ConeAngle` e `ConeDirection` (vetor local).
- Paleta de cores para gizmos (`Idle`, `Detecting`, `Selected`).

### `SensorCollection`
Lista serializada de `SensorConfig` (`Runtime/SensorCollection.cs`). Facilita reutilização de pacotes de sensores entre múltiplos
atores. Existem coleções exemplo em `Scripts/DetectionsSystems/Data` (Player/Eater).

### `DetectionType` Assets
Arquivos `.asset` na pasta `Data/` exemplificam a separação de domínios (ex.: `PlayerDetector`, `PlanetDetector`).

---

## 🛠️ Serviço de Sensores

### `DetectorService`
`Runtime/DetectorService.cs` instancia `Sensor` para cada `SensorConfig` e os atualiza no `Update` do controlador. Oferece métodos de
consulta (`GetSensors`, `IsAnySensorDetecting`, `GetTotalDetections`) úteis para UI ou FSMs.

### `Sensor`
`Runtime/Sensor.cs` é a unidade operacional:
- Reutiliza um array fixo de `Collider[5]` para sobreposições (evitando GC).
- `ProcessDetections` compara lista atual (`current`) com cache `_detected` para identificar entradas/saídas.
- Usa `Dictionary<IDetectable,int>` para garantir que cada alvo gera apenas um evento por frame (ENTER/EXIT).
- Expõe estado para debug (`CurrentlyDetected`, `IsDetecting`, `GetConeEdgeDirections`, `GetConeArcPoints`).

> ⚠️ Ajuste `Collider[5]` se o mesmo sensor precisar detectar mais de cinco objetos simultâneos.

---

## 🔔 Eventos e Integrações

### `DetectionEnterEvent` / `DetectionExitEvent`
Estruturas em `DetectionEvents.cs` implementam `IEvent` e carregam `IDetectable`, `IDetector` e `DetectionType`.

### Consumo de Eventos
- `AbstractDetector` atua como **ouvinte** e dispara `OnDetected`/`OnLost` somente quando o cache confirma transição válida.
- `AbstractDetectable` responde a `OnEnterDetection`/`OnExitDetection`, permitindo lógica de impacto local (FX, HUD, AI, etc.).
- Outros sistemas podem registrar `EventBinding` diretamente no `EventBus` para lógica cross-system (ex.: habilitar `DamageSystem`).

### Integração com Actor System
Ambos os lados dependem de `IActor` (`ActorSystems`). Garante identificação consistente em multiplayer local e facilita logs.

---

## 🧪 Ferramentas de Debug

### `SensorDebugVisualizer`
MonoBehaviour opcional (`Mono/SensorDebugVisualizer.cs`):
- Executa no Editor e Play Mode (`[ExecuteInEditMode]`).
- Desenha esferas/cones, linhas para objetos detectados e labels com contagem ativa.
- Otimiza desenho de cones cacheando pontos (`_cachedConePoints`).
- Permite habilitar `Debug.DrawRay` para inspeção quadro-a-quadro.

### Inspectors Customizados
- `SensorConfigEditor` valida `DetectionType` e `Radius` diretamente no Inspector.
- `SensorCollectionEditor` exibe resumo (DetectionType, Layer, alertas) e botões para manipular a lista de sensores.

---

## ⚙️ Configuração Passo a Passo

1. **Criar/selecionar `DetectionType`** para o domínio desejado (ex.: `Planet` vs `Player`).
2. **Configurar `SensorConfig`**:
   - Atribuir `DetectionType`, `TargetLayer`, `Radius`, frequências e modo (esfera/cone).
   - Marcar `DebugMode` para visualizar gizmos durante a calibração.
3. **Montar `SensorCollection`** adicionando um ou mais `SensorConfig`.
4. **Adicionar `SensorController`** ao GameObject que implementa `IDetector` (pode herdar de `AbstractDetector`).
5. **Referenciar a `SensorCollection`** no `SensorController` via Inspector.
6. **Garantir que o alvo** herde de `AbstractDetectable`, atribuindo o mesmo `DetectionType`.
7. **Implementar callbacks**:
   - Detector: sobrescrever `OnDetected`/`OnLost`.
   - Detectável: sobrescrever `OnEnterDetection`/`OnExitDetection`.
8. (Opcional) **Adicionar `SensorDebugVisualizer`** no mesmo GameObject para suporte visual.

---

## 🧠 Extensibilidade e Boas Práticas

- **Segregação por Tipo**: Use `DetectionType` diferentes para pipelines independentes (ex.: visão, cheiro, coleta).
- **Frequência adaptativa**: `MaxFrequency` controla o intervalo mínimo entre varreduras. Para sensores críticos use valores baixos,
  para sensores periféricos mantenha acima de `0.2f`.
- **Override de Cache**: Ao implementar novos detectores, utilize `GetDetectedItems()` para lógica derivada sem recriar coleções.
- **Integração com FSMs**: Utilize `DetectorService.IsAnySensorDetecting()` para transições em `StateMachineSystems`.
- **Testes**: Simule eventos publicando manualmente `DetectionEnterEvent` com mocks de `IDetectable`/`IDetector`.

---

## 🧯 Troubleshooting

| Sintoma | Causa Provável | Ação Recomendada |
| --- | --- | --- |
| Detector nunca dispara callbacks | `SensorController` sem `SensorCollection` válida ou `IDetector` ausente | Valide logs de erro em `Awake` e atribua referências corretas. |
| Eventos duplicados no mesmo frame | `DetectionType` divergente entre detector e detectável | Garanta que ambos apontem para o mesmo asset `DetectionType`. |
| Alvos não detectados | Layer errada ou raio insuficiente | Revise `TargetLayer` no `SensorConfig` e ajuste `Radius` / `ConeAngle`. |
| GC spikes durante partidas | Muitos objetos simultâneos para o array interno | Aumente o tamanho do array em `Sensor` ou adicione múltiplos sensores segmentados. |
| Gizmos não aparecem | `DebugMode` desativado ou `SensorDebugVisualizer` ausente | Ative `DebugMode` nos configs e adicione o visualizador ao prefab. |

---

> 📌 **Checklist rápido antes de commitar prefabs**: validar `DetectionType`, layers, `MaxFrequency`, cone direcional e se o objeto possui
`IActor` compatível.
