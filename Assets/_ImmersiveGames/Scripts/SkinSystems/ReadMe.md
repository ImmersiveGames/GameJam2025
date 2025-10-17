# Sistema de Skins - Documentação

## 📋 Visão Geral

O Sistema de Skins permite compor, trocar e sincronizar modelos visuais para atores do jogo. Ele foi projetado para cenários de multiplayer local, garantindo baixo acoplamento entre prefabs de skin, fluxos de UI e demais sistemas (ex: animação, VFX, áudio). A arquitetura combina um `SkinController` focado em orquestrar eventos e um `SkinService` responsável pela criação/gerenciamento de instâncias.

Principais objetivos:
- **Isolar regras de composição** em serviços reaproveitáveis.
- **Permitir injeção de dependências** para testes ou variações em runtime.
- **Propagar eventos locais e globais** sem duplicações quando objetos são habilitados/desabilitados.
- **Minimizar alocações** ao aplicar skins de forma recorrente durante a partida.

## 🏗️ Arquitetura

```
SkinController (MonoBehaviour)
    ↳ ISkinService (SkinService padrão)
        ↳ ContainerService • ModelFactory
        ↳ ISkinInstancePostProcessor (ex: DynamicCanvasBinderPostProcessor)
```

### Fluxo em alto nível
1. O `SkinController` encontra dependências (`IActor`, `IHasSkin`) na hierarquia e inicializa o `ISkinService`.
2. O `SkinService` cria contêineres de modelo e instancia prefabs definidos pelo `ISkinConfig`.
3. Pós-processadores opcionais tratam ligações dinâmicas (UI, animators, etc.).
4. O `SkinController` notifica ouvintes locais e publica eventos globais filtrados por `ActorId`.

## 🎯 Componentes Principais

### SkinController
- Mantém o estado de inicialização e expõe eventos locais: `OnSkinApplied`, `OnSkinCollectionApplied`, `OnSkinInstancesCreated`.
- Registra bindings no `FilteredEventBus` em `OnEnable` e remove em `OnDisable`, evitando duplicações quando o GameObject é reativado.
- Publica eventos globais (`SkinUpdateEvent`, `SkinCollectionUpdateEvent`, `SkinInstancesCreatedEvent`) apenas quando habilitado.
- Conecta-se ao `DependencyManager` para expor serviços para outros módulos do ator.
- APIs públicas:
  - `Initialize()` para setups manuais.
  - `ApplySkin(ISkinConfig)` e `ApplySkinCollection(SkinCollectionData)`.
  - Métodos utilitários para consultar instâncias, contêineres e componentes específicos.

### SkinService
- Implementa `ISkinService`, gerencia contêineres e instâncias por `ModelType`.
- Sempre garante a presença do `DynamicCanvasBinderPostProcessor`, mesmo com listas injetadas vazias ou nulas.
- Evita alocações desnecessárias ao iterar prefabs manualmente (sem LINQ), reduzindo GC spikes durante trocas frequentes de skin.
- Armazena instâncias em um dicionário por `ModelType`, permitindo consultas `ReadOnly` para outros sistemas.

### ISkinInstancePostProcessor
- Interface que permite anexar lógica pós-instanciação (ex: binders de canvas, configuração de animators, etc.).
- O pós-processador padrão (`DynamicCanvasBinderPostProcessor`) mantém compatibilidade com UI dinâmica.
- É possível registrar outros pós-processadores via construtores do `SkinService` ou injeção externa.

## 📦 Estruturas de Dados

### SkinCollectionData
- Define coleções serializadas de `ISkinConfig` por `ModelType`.
- Fornece APIs para recuperar configs e enumerar tipos disponíveis.

### ISkinConfig / SkinConfigData
- Expõe prefabs selecionados conforme o `InstantiationMode` (All, First, Random, Specific).
- Fornece posição, rotação, escala e estado ativo inicial utilizados na instância.

## 🔁 Ciclo de Vida e Eventos

- `Awake`: resolve dependências e injeta `SkinService` padrão (quando nenhum serviço externo é fornecido).
- `OnEnable`: registra bindings no `FilteredEventBus`. Safe guard impede múltiplas inscrições simultâneas.
- `Start`: registra o controlador no `DependencyManager` (quando houver `ActorId`).
- `OnDisable`: remove bindings do bus mantendo integridade do escopo global.
- `OnDestroy`: limpeza final no `DependencyManager` e no bus (fallback).

## 🚀 Guia de Uso

1. **Configuração no Inspector**
   - Atribua `SkinCollectionData` em `defaultSkinCollection` para inicialização automática.
   - Marque `autoInitialize` para aplicar a coleção padrão no `Awake`.
   - Ative `enableGlobalEvents` quando desejar sincronizar estados via eventos globais.

2. **Aplicando Skins em Runtime**
   ```csharp
   public class SkinSwitcher : MonoBehaviour
   {
       [SerializeField] private SkinController controller;
       [SerializeField] private SkinCollectionData alternateCollection;

       public void SwapToAlternate()
       {
           controller.ApplySkinCollection(alternateCollection);
       }
   }
   ```

3. **Injetando Serviços Personalizados**
   ```csharp
   void Awake()
   {
       var controller = GetComponent<SkinController>();
       controller.SetSkinService(new SkinService(customContainerSvc, customModelFactory, customPostProcessors));
   }
   ```
   O `SkinService` garantirá que o `DynamicCanvasBinderPostProcessor` seja preservado mesmo quando a lista injetada não o incluir.

4. **Escutando Eventos Globais**
   - Utilize `FilteredEventBus<SkinUpdateEvent>.Register(binding, actor)` para reagir a atualizações filtradas por ator.
   - A nova gestão de ciclo de vida evita múltiplos bindings quando o controlador é desabilitado/habilitado.

## 🧪 Boas Práticas

- **Inicialize explicitamente em testes**: chame `Initialize()` após injetar dependências mockadas.
- **Evite reter listas mutáveis**: trate o retorno de `GetSkinInstances` como somente leitura ou faça cópia se precisar alterar.
- **Use pós-processadores para cross-cutting**: encapsule lógica adicional (UI, efeitos) sem modificar o serviço principal.
- **Sincronize com sistemas de animação** via eventos `OnSkinInstancesCreated`, que retornam as instâncias válidas do modelo.
- **Limpeza determinística**: implemente `OnDisable` em listeners para remover bindings próprios do `FilteredEventBus`.

## 🛠️ Solução de Problemas

| Sintoma | Possível causa | Correção |
|--------|----------------|----------|
| Eventos globais duplicados | Controladores eram reativados sem desregistrar bindings | Já tratado pela inscrição em `OnEnable`/`OnDisable`. Verifique se outros listeners seguem o mesmo padrão. |
| Prefabs não instanciam | Contêiner para o `ModelType` não existe | Confirme setup dos contêineres no `ContainerService` ou no prefab da skin. |
| UI dinâmica não atualiza | Lista de pós-processadores customizados removeu o binder padrão | `SkinService` agora injeta automaticamente o `DynamicCanvasBinderPostProcessor`; revise customizações caso o comportamento persista. |
| Pico de GC ao trocar skins | Uso excessivo de coleções temporárias | O serviço removeu LINQ e mantém lista reutilizável por instância; monitore outros sistemas envolvidos. |

## 📚 Referências Cruzadas

- `_ImmersiveGames/Scripts/SkinSystems/Controllers/SkinController.cs`
- `_ImmersiveGames/Scripts/SkinSystems/Services/SkinService.cs`
- `_ImmersiveGames/Scripts/SkinSystems/Services/DynamicCanvasBinderPostProcessor.cs`
- `_ImmersiveGames/Scripts/SkinSystems/Data/SkinCollectionData.cs`

---

*Atualizado para refletir o ciclo de vida de eventos e otimizações de alocação do release atual.*
