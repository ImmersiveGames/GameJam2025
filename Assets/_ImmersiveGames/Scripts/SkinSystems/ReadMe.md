# Sistema de Skins - Documentação

## 📋 Visão Geral

O Sistema de Skins é uma solução modular para gerenciamento de aparências e modelos em entidades de jogo, com foco em variações dinâmicas baseadas em thresholds de recursos (ex: health). Ele permite que componentes visuais (partes de modelos 3D) sejam ativados/desativados progressivamente conforme recursos mudam, usando eventos e configs para reatividade. O sistema suporta inicialização com todas partes ativas e reset para estado inicial.

## 🏗️ Arquitetura

### Diagrama de Componentes
```
ResourceThresholdService (Global) → EventBus<ResourceThresholdEvent> → ResourceThresholdListener (Local)
                                                                 ↑
                                                                 │
                                                                 └── PartsController (Gerencia partes da skin)
```

### Princípios de Design
- **Separação de Responsabilidades**: Listener filtra e despacha eventos; controller gerencia partes específicas.
- **Baixo Acoplamento**: Comunicação via EventBus global e UnityEvents locais.
- **Flexibilidade**: Configs serializadas para thresholds e direções (ascending/descending/both).
- **Extensibilidade**: Fácil adição de novos consumidores via UnityEvents (ex: SFX ou partículas).

## 🎯 Componentes Principais

### 1. ResourceThresholdListener
**Propósito**: Ouve eventos globais de thresholds de recursos e despacha ações locais via UnityEvents configuráveis.

```csharp
public class ResourceThresholdListener : MonoBehaviour
{
    // Configs para thresholds, direção e ações
    [SerializeField] private ThresholdConfig[] thresholdConfigs;

    // Filtra por ActorId automaticamente
    private string _expectedActorId;

    // Registra no EventBus e despacha onThresholdCrossed
}
```

### 2. PartsController
**Propósito**: Gerencia ativação/desativação de partes da skin baseado em percentage e direção do threshold.

```csharp
public class PartsController : MonoBehaviour
{
    // Array de partes (GameObjects)
    [SerializeField] private GameObject[] parts;

    // Handler unificado para UnityEvents
    public void HandleThresholdCrossed(float threshold, float percentage, bool ascending);

    // Reset para estado inicial
    public void Reset();
}
```

### 3. Interfaces e Enums Relacionados

#### TriggerDirection
```csharp
public enum TriggerDirection
{
    Ascending,    // Quando recurso sobe
    Descending,   // Quando recurso desce
    Both          // Ambas direções
}
```

## 📁 Estrutura de Dados

### ThresholdConfig
```csharp
[System.Serializable]
public class ThresholdConfig
{
    public ResourceType resourceType = ResourceType.Health;
    [Range(0f, 1f)] public float threshold = 0.5f;
    public TriggerDirection direction = TriggerDirection.Descending;
    public UnityEvent<float, float, bool> onThresholdCrossed;
}
```

## 🚀 Guia de Uso

### 1. Configuração Básica

#### Configurando Thresholds no Listener
1. **Anexe ResourceThresholdListener ao GameObject da skin.**
2. No Inspector:
    - Adicione entradas em `thresholdConfigs` (ex: Health, 0.5, Both).
    - Registre métodos como `HandleThresholdCrossed` do PartsController no `onThresholdCrossed`.

#### Configurando Parts no Controller
1. **Anexe PartsController ao mesmo GameObject.**
2. No Inspector:
    - Atribua GameObjects em `parts` (ordem importa para thresholds sequenciais).

### 2. Implementação na Cena

#### Cenário 1: Skin Reativa a Recursos
- O listener detecta ActorId automaticamente via parent.
- Ao cruzar threshold (ex: health < 0.5 descending), invoca handler, que desativa partes.
- Em ascending (ex: regeneração), reativa partes.

#### Cenário 2: Integração com Damage
- Use `ResourceSystem.Modify` para alterar health – propaga via service para listener/controller.

### 3. Exemplos de Código

#### Exemplo 1: Reset Manual
```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private PartsController partsController;

    public void RestartLevel()
    {
        partsController.Reset(); // Volta ao estado inicial (todas ativas)
    }
}
```

#### Exemplo 2: Consumidor Adicional (ex: SFX)
```csharp
public class SkinSFX : MonoBehaviour
{
    public void PlaySFX(float threshold, float percentage, bool ascending)
    {
        // Toca som baseado em direção
        if (!ascending)
        {
            // Som de dano em descending
            AudioSource.PlayClipAtPoint(damageClip, transform.position);
        }
        else
        {
            // Som de regeneração em ascending
            AudioSource.PlayClipAtPoint(healClip, transform.position);
        }
    }
}
```
- Registre `PlaySFX` no UnityEvent do listener para o mesmo threshold.

## 🔧 Configuração no Inspector

### ResourceThresholdListener
```
✓ thresholdConfigs [Array] - Configs de thresholds e ações
✓ showDebugLogs [bool] - Ativar logs de depuração
```

### PartsController
```
✓ parts [Array] - GameObjects das partes da skin
✓ thresholdListener [Reference] - Referência ao listener
✓ showDebugLogs [bool] - Ativar logs de depuração
```

## 🎮 Casos de Uso Comuns

### 1. Personagens Jogáveis
- Desativa partes em dano (descending), reativa em heal (ascending).

### 2. Inimigos e NPCs
- Variações visuais em thresholds de health (ex: partes quebradas em low health).

### 3. Efeitos Visuais
- Integre com SFX/partículas via UnityEvents para reações multimodais.

## ⚠️ Solução de Problemas

### Problema: Eventos duplicados
**Solução**: Verifique logs – o auto-filtro por ActorId deve resolver; ajuste hierarquia se IActor não encontrado.

### Problema: Partes não atualizam
**Solução**: Confirme configs no listener (threshold 0-1, direção correta) e registro de handler no UnityEvent.

### Problema: Inicialização errada
**Solução**: O init usa SetAllParts(true) – verifique se parts estão assignados no inspector.

## 🔄 Melhores Práticas

1. **Use floats 0-1** para thresholds consistentes.
2. **Prefira Both** para handlers unificados quando ascending/descending compartilham lógica.
3. **Teste direções**: Simule danos (descending) e heals (ascending) para consistência.
4. **Modularize**: Adicione consumidores via UnityEvents sem modificar código.
5. **Debug com logs**: Ative showDebugLogs para rastrear matches e atualizações.

---

*Esta documentação será atualizada conforme o sistema evolui. Para dúvidas específicas, consulte os exemplos de código ou entre em contato com a equipe de desenvolvimento.*