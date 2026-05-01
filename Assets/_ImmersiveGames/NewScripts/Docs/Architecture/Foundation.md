# Foundation

## 1. Visão geral

`Foundation` reúne utilitários e infraestrutura técnica reutilizável de `NewScripts`.

Ela existe para oferecer blocos pequenos, previsíveis e comuns a vários módulos, sem carregar regra de gameplay, sessão, actors, navegação ou fluxo de phase.

Use `Foundation` quando precisar de:

- eventos tipados;
- FSM genérica;
- IDs runtime;
- logging e fail-fast;
- validações simples;
- composição/DI;
- configuração de boot;
- runtime mode/degraded reporting;
- pooling técnico;
- gates de simulação;
- observabilidade técnica.

## 2. Como ler esta camada

`Foundation` deve ser lida como camada instrumental.

Ela responde principalmente:

- o que é;
- para que serve;
- quando usar;
- como usar;
- quando não usar.

Ela não deve ser usada para decidir ownership semântico de módulos maiores.

Regra prática:

> Foundation fornece ferramentas. Módulos de domínio continuam donos da semântica.

---

# Core

## 3. Core / Events

### O que é

Sistema base de eventos tipados para comunicação desacoplada entre módulos.

### Para que serve

- Publicar eventos sem acoplar emissor e consumidor.
- Registrar listeners com lifecycle explícito.
- Permitir substituição controlada do bus global em cenários avançados.
- Usar wrappers como `FilteredEventBus` quando for necessário filtrar eventos em escopos específicos.

### Quando usar

Use quando um módulo precisa avisar que algo aconteceu e não precisa de resposta síncrona imediata.

Exemplos:

- estado mudou;
- etapa de fluxo concluiu;
- evento de observabilidade ocorreu;
- request foi emitido para outro owner processar.

### Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Events;

public readonly struct ExampleEvent : IEvent
{
    public readonly string Reason;

    public ExampleEvent(string reason)
    {
        Reason = reason;
    }
}

public sealed class ExampleListener
{
    private readonly EventBinding<ExampleEvent> _binding;

    public ExampleListener()
    {
        _binding = new EventBinding<ExampleEvent>(OnExample);
    }

    public void Bind()
    {
        EventBus<ExampleEvent>.Register(_binding);
    }

    public void Unbind()
    {
        EventBus<ExampleEvent>.Unregister(_binding);
    }

    private void OnExample(ExampleEvent evt)
    {
        // Reage ao evento sem conhecer o emissor.
    }
}
```

### Quando não usar

- Não usar para chamada direta simples dentro do mesmo serviço.
- Não usar quando o caller precisa de retorno imediato.
- Não usar para esconder dependência obrigatória.
- Não usar para tráfego por frame de alta frequência.

### Arquivos principais

- `Foundation/Core/Events/IEvent.cs`
- `Foundation/Core/Events/IEventBinding.cs`
- `Foundation/Core/Events/EventBinding.cs`
- `Foundation/Core/Events/EventBus.cs`
- `Foundation/Core/Events/InjectableEventBus.cs`
- `Foundation/Core/Events/FilteredEventBus.cs`
- `Foundation/Core/Events/EventBusUtil.cs`
- `Foundation/Core/Events/README.md`

### Cuidados

- Bind/unbind deve seguir lifecycle claro.
- O owner do evento precisa ser explícito.
- Eventos não devem virar bypass de arquitetura.

---

## 4. Core / FSM

### O que é

FSM genérica para fluxos internos simples.

### Para que serve

- Modelar estados locais.
- Separar `enter`, `update`, `fixed update` e `exit`.
- Declarar transições por predicados.
- Usar `AnyTransition` para saídas globais controladas.

### Quando usar

Use em controllers ou fluxos locais que têm estados claros e transições previsíveis.

Exemplos:

- estado local de UI;
- controller de objeto;
- subfluxo interno de gameplay;
- estado técnico pequeno.

### Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Fsm;

public sealed class IdleState : IState
{
    public void OnEnter() { }
    public void Update() { }
    public void FixedUpdate() { }
    public void OnExit() { }
}

public sealed class ExampleFsmDriver
{
    private readonly StateMachine _stateMachine = new();

    public void Configure()
    {
        var idle = new IdleState();
        var active = new ActiveState();

        _stateMachine.RegisterState(idle);
        _stateMachine.RegisterState(active);
        _stateMachine.AddTransition(idle, active, () => CanEnterActive());
        _stateMachine.SetState(idle);
    }

    public void Tick()
    {
        _stateMachine.Update();
    }

    private bool CanEnterActive()
    {
        return true;
    }
}
```

### Quando não usar

- Não usar para substituir fluxo canônico de módulos grandes.
- Não usar para esconder política de domínio.
- Não usar para coordenar vários owners externos.

### Arquivos principais

- `Foundation/Core/Fsm/IState.cs`
- `Foundation/Core/Fsm/ITransition.cs`
- `Foundation/Core/Fsm/IPredicate.cs`
- `Foundation/Core/Fsm/StateMachine.cs`
- `Foundation/Core/Fsm/Transition.cs`
- `Foundation/Core/Fsm/README.md`

### Cuidados

- Estados devem ser registrados antes de uso.
- Evite criar transições novas a cada frame.
- Pause/gates pertencem ao owner do fluxo, não à FSM genérica.

---

## 5. Core / Identifiers

### O que é

Utilitário para geração de IDs runtime únicos e legíveis.

### Para que serve

- Criar IDs de rastreio.
- Criar assinaturas curtas de runtime.
- Melhorar logs e observabilidade.
- Evitar colisões em execuções com Domain Reload desativado.

### Quando usar

Use para identificar instâncias runtime, ciclos de execução, trace IDs e assinaturas temporárias.

### Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Identifiers;

public sealed class ExampleIdConsumer
{
    private readonly UniqueIdFactory _idFactory = new();

    public string CreateRuntimeId(UnityEngine.GameObject owner)
    {
        return _idFactory.GenerateId(owner, prefix: "example");
    }
}
```

### Quando não usar

- Não usar como identidade autoral.
- Não usar como chave persistida de save.
- Não substituir IDs semânticos de domínio.

### Arquivos principais

- `Foundation/Core/Identifiers/UniqueIdFactory.cs`
- `Foundation/Core/Identifiers/README.md`

### Cuidados

- IDs são para runtime/observabilidade.
- Prefixos devem ser curtos e estáveis.
- Identidade semântica pertence ao módulo dono.

---

## 6. Core / Logging

### O que é

Base de logging, níveis de debug e fail-fast.

### Para que serve

- Padronizar logs.
- Controlar verbosidade por classe.
- Emitir evidência de fluxo.
- Falhar cedo em contrato/configuração obrigatória inválida.

### Quando usar

Use para lifecycle relevante, diagnóstico de runtime, erros estruturais e evidência de fluxo.

### Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Logging;

[DebugLevel(DebugLevel.Verbose)]
public sealed class ExampleService
{
    public void Execute(string reason)
    {
        DebugUtility.LogInfo(
            typeof(ExampleService),
            $"Example executed. reason='{reason}'");
    }
}
```

Para falha estrutural obrigatória:

```csharp
using _ImmersiveGames.NewScripts.Core.Logging;

public sealed class RequiredConfigConsumer
{
    public void Configure(object config)
    {
        if (config == null)
        {
            HardFailFastH1.Fail(
                typeof(RequiredConfigConsumer),
                "Required config is missing.");
        }
    }
}
```

### Quando não usar

- Não usar log como controle de fluxo.
- Não usar fail-fast para estado esperado de gameplay.
- Não esconder erro estrutural com fallback silencioso.
- Não criar formatos paralelos sem necessidade.

### Arquivos principais

- `Foundation/Core/Logging/DebugUtility.cs`
- `Foundation/Core/Logging/DebugLevelAttribute.cs`
- `Foundation/Core/Logging/HardFailFastH1.cs`
- `Foundation/Core/Logging/ResetLogTags.cs`
- `Foundation/Core/Logging/Config/LoggingConfigAsset.cs`
- `Foundation/Core/Logging/README.md`

### Cuidados

- Logs úteis carregam campos como `reason`, `source`, `signature`, `target`, `profile` ou `traceId`.
- Fail-fast é para contrato obrigatório quebrado.
- Logs por frame devem ser evitados.

---

## 7. Core / Validation

### O que é

Conjunto simples de precondições para validação defensiva.

### Para que serve

- Validar argumentos.
- Evitar estados inválidos logo na entrada.
- Centralizar checks simples.

### Quando usar

Use em constructors, métodos públicos, serviços e pontos de fronteira onde parâmetros obrigatórios precisam ser claros.

### Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Validation;

public sealed class ExampleValidator
{
    public void Execute(string reason)
    {
        Preconditions.RequireNotNullOrWhiteSpace(reason, nameof(reason));
    }
}
```

### Quando não usar

- Não usar para substituir validação semântica complexa.
- Não usar para mascarar erro que deveria ser fail-fast específico do módulo.

### Arquivos principais

- `Foundation/Core/Validation/Preconditions.cs`

### Cuidados

- Precondition é validação local.
- Política de domínio continua no módulo dono.

---

# Platform

## 8. Platform / Composition

### O que é

Infraestrutura de composição, registro e resolução de serviços.

Inclui tanto registros por escopo quanto o pipeline global de composição modular.

### Para que serve

- Registrar serviços globais, por cena e por objeto.
- Resolver dependências por interfaces.
- Injetar dependências quando necessário.
- Coordenar installers e runtime composers.
- Manter boot determinístico e auditável.

### Quando usar

Use em bootstraps, installers, runtime composers e composition roots.

### Como usar

Registro simples:

```csharp
using _ImmersiveGames.NewScripts.Core.Composition;

public sealed class ExampleInstaller
{
    public void Install()
    {
        DependencyManager.Provider.RegisterGlobal<IExampleService>(new ExampleService());
    }
}
```

Resolução simples:

```csharp
using _ImmersiveGames.NewScripts.Core.Composition;

public sealed class ExampleConsumer
{
    public bool TryExecute()
    {
        if (!DependencyManager.Provider.TryGet<IExampleService>(out var service))
        {
            return false;
        }

        service.Execute();
        return true;
    }
}
```

### Quando não usar

- Não registrar serviço estrutural tarde para corrigir boot incompleto.
- Não usar service locator no hot path quando dependência obrigatória pode ser explícita.
- Não colocar wiring interno de módulo no root global.
- Não depender de `Awake`, `Start` ou `OnEnable` como bootstrap canônico.

### Arquivos principais

- `Foundation/Platform/Composition/CompositionModuleDescriptor.cs`
- `Foundation/Platform/Composition/CompositionPipelineStep.cs`
- `Foundation/Platform/Composition/DependencyInjector.cs`
- `Foundation/Platform/Composition/DependencyManager.cs`
- `Foundation/Platform/Composition/GlobalCompositionRoot.*.cs`
- `Foundation/Platform/Composition/GlobalServiceRegistry.cs`
- `Foundation/Platform/Composition/IDependencyProvider.cs`
- `Foundation/Platform/Composition/ObjectServiceRegistry.cs`
- `Foundation/Platform/Composition/SceneScopeCompositionRoot.cs`
- `Foundation/Platform/Composition/SceneScopeCompositionRoot.ActorGroupRearm.cs`
- `Foundation/Platform/Composition/SceneServiceCleaner.cs`
- `Foundation/Platform/Composition/SceneServiceRegistry.cs`
- `Foundation/Platform/Composition/ServiceRegistry.cs`
- `Foundation/Platform/Composition/README.md`

### Cuidados

- Installer registra contratos e serviços.
- Runtime composer ativa runtime e bridges.
- Dependência obrigatória ausente deve falhar cedo.
- O root global coordena; o módulo continua dono do seu wiring interno.

---

## 9. Platform / Config

### O que é

Configuração de boot global do runtime.

### Para que serve

- Centralizar referências obrigatórias de boot.
- Evitar busca dispersa por Resources.
- Dar ao boot um ponto único de entrada para configs globais.

### Quando usar

Use para configs que precisam existir antes da composição do runtime.

### Como usar

O asset principal identificado no pacote é:

```text
Foundation/Platform/Config/BootstrapConfigAsset.cs
```

Ele deve apontar para configs obrigatórias de boot, como runtime mode e outros assets globais exigidos pelo profile.

### Quando não usar

- Não usar como depósito genérico de configuração de gameplay.
- Não usar para substituir assets autorais de módulos.
- Não usar fallback silencioso quando uma config obrigatória estiver ausente.

### Arquivos principais

- `Foundation/Platform/Config/BootstrapConfigAsset.cs`

### Cuidados

- Config obrigatória inválida deve falhar cedo.
- Cada módulo ainda deve manter seu próprio owner de config quando a configuração for específica dele.

---

## 10. Platform / RuntimeMode

### O que é

Infraestrutura para definir modo de runtime e relatar condições degradadas.

### Para que serve

- Ler configuração de modo runtime.
- Diferenciar modo automático/configurado.
- Reportar condições degradadas com dedupe/cooldown.
- Padronizar comportamento em Editor/build quando algo entra em modo degradado.

### Quando usar

Use para decisões técnicas globais de runtime e degradação operacional controlada.

### Como usar

Fluxo geral:

```text
RuntimeModeConfig -> RuntimeModeConfigLoader -> IRuntimeModeProvider -> RuntimeMode
```

Para reporting:

```text
IDegradedModeReporter -> DegradedModeReporter -> DegradedKeys
```

### Quando não usar

- Não usar para mascarar contrato obrigatório quebrado.
- Não usar como política de gameplay.
- Não usar como fallback genérico quando o certo é fail-fast.

### Arquivos principais

- `Foundation/Platform/RuntimeMode/RuntimeMode.cs`
- `Foundation/Platform/RuntimeMode/RuntimeModeConfig.cs`
- `Foundation/Platform/RuntimeMode/RuntimeModeConfigLoader.cs`
- `Foundation/Platform/RuntimeMode/IRuntimeModeProvider.cs`
- `Foundation/Platform/RuntimeMode/UnityRuntimeModeProvider.cs`
- `Foundation/Platform/RuntimeMode/ConfigurableRuntimeModeProvider.cs`
- `Foundation/Platform/RuntimeMode/IDegradedModeReporter.cs`
- `Foundation/Platform/RuntimeMode/DegradedModeReporter.cs`
- `Foundation/Platform/RuntimeMode/DegradedKeys.cs`
- `Foundation/Platform/RuntimeMode/README-QuickStart.md`

### Cuidados

- O quickstart do pacote indica que `RuntimeModeConfig.asset` deve existir e ser referenciado pelo `BootstrapConfig.asset`.
- Ausência de asset obrigatório deve falhar cedo.
- Degraded mode é uma política técnica controlada, não atalho para configuração ausente.

---

## 11. Platform / Pooling

### O que é

Sistema técnico de reutilização de `GameObject`.

### Para que serve

- Reduzir custo de `Instantiate`/`Destroy`.
- Padronizar `ensure`, `prewarm`, `rent` e `return`.
- Manter pooling como backend técnico, não como owner de gameplay.

### Quando usar

Use para objetos que são criados e descartados com frequência e podem ser reaproveitados.

Exemplos:

- SFX pooled;
- VFX pooled;
- objetos temporários;
- elementos runtime repetitivos.

### Como usar

Fluxo conceitual:

```text
PoolDefinitionAsset -> IPoolService.EnsureRegistered -> Prewarm/Rent/Return
```

Objetos pooled podem implementar hooks:

```csharp
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;

public sealed class ExamplePooledObject : PooledBehaviour
{
    public override void OnPoolRent()
    {
        // Reinicia estado local ao sair do pool.
    }

    public override void OnPoolReturn()
    {
        // Limpa estado local antes de voltar ao pool.
    }
}
```

### Quando não usar

- Não usar pooling para decidir identidade de gameplay object.
- Não usar como substituto de spawn/materialization owner.
- Não usar para objetos cuja reinicialização correta não está garantida.

### Arquivos principais

- `Foundation/Platform/Pooling/Config/PoolDefinitionAsset.cs`
- `Foundation/Platform/Pooling/Contracts/IPoolService.cs`
- `Foundation/Platform/Pooling/Contracts/IPoolableObject.cs`
- `Foundation/Platform/Pooling/Contracts/PooledBehaviour.cs`
- `Foundation/Platform/Pooling/Runtime/GameObjectPool.cs`
- `Foundation/Platform/Pooling/Runtime/PoolService.cs`
- `Foundation/Platform/Pooling/Runtime/PoolRuntimeHost.cs`
- `Foundation/Platform/Pooling/Runtime/PoolRuntimeInstance.cs`
- `Foundation/Platform/Pooling/Runtime/PoolAutoReturnTracker.cs`
- `Foundation/Platform/Pooling/QA/PoolingQaContextMenuDriver.cs`
- `Foundation/Platform/Pooling/QA/PoolingQaMockPooledObject.cs`
- `Foundation/Platform/Pooling/Pooling-How-To.md`
- `Foundation/Platform/Pooling/Pooling-Quick-Access.html`

### Cuidados

- Pooling é backend técnico.
- Spawn ou módulo consumidor continua dono da semântica do objeto.
- `prewarm` declara intenção; não deve virar execução implícita fora do owner correto.
- Todo objeto pooled precisa limpar estado corretamente no retorno.

---

## 12. Platform / SimulationGate

### O que é

Serviço técnico de bloqueio/liberação da simulação por tokens.

### Para que serve

- Bloquear gameplay durante transições, pause, intro ou outros estados técnicos.
- Permitir múltiplas razões simultâneas de bloqueio.
- Liberar simulação apenas quando todos os tokens relevantes forem removidos.

### Quando usar

Use quando o runtime precisa pausar ou bloquear simulação por motivo técnico explícito.

Exemplos:

- pause;
- scene transition;
- intro stage;
- loading;
- preparação de gameplay.

### Como usar

Fluxo conceitual:

```text
Acquire token -> simulação bloqueada
Release token -> simulação pode liberar quando não houver outros bloqueios
```

Tokens centrais ficam em:

```text
Foundation/Platform/SimulationGate/SimulationGateTokens.cs
```

### Quando não usar

- Não usar para definir estado semântico da run.
- Não usar como substituto de GameLoop.
- Não usar token genérico quando existe token canônico.
- Não liberar token que pertence a outro owner.

### Arquivos principais

- `Foundation/Platform/SimulationGate/ISimulationGateService.cs`
- `Foundation/Platform/SimulationGate/SimulationGateService.cs`
- `Foundation/Platform/SimulationGate/SimulationGateTokens.cs`
- `Foundation/Platform/SimulationGate/Interop/GamePauseGateBridge.cs`
- `Foundation/Platform/SimulationGate/GATES_ANALYSIS_REPORT.md`

### Cuidados

- Tokens precisam ter owner claro.
- Todo acquire deve ter release correspondente.
- Bridges, como `GamePauseGateBridge`, devem reagir ao owner do estado; não virar owner do domínio.

---

## 13. Platform / Observability

### O que é

Infraestrutura de observabilidade técnica e asserção de invariantes do baseline.

### Para que serve

- Validar invariantes técnicas.
- Registrar inconsistências estruturais.
- Apoiar auditoria de runtime.

### Quando usar

Use para checagens técnicas que atravessam o baseline e precisam aparecer como evidência clara.

### Como usar

O pacote atual contém:

```text
Foundation/Platform/Observability/Baseline/BaselineInvariantAsserter.cs
```

Use esse tipo de peça para checar invariantes globais/técnicas, não para impor regra semântica de gameplay.

### Quando não usar

- Não usar observability como mecanismo de correção automática.
- Não usar invariant asserter para decidir fluxo de gameplay.
- Não transformar diagnóstico em owner de domínio.

### Arquivos principais

- `Foundation/Platform/Observability/Baseline/BaselineInvariantAsserter.cs`

### Cuidados

- Observabilidade deve detectar e explicar.
- Correção pertence ao módulo dono.
- Invariante estrutural quebrada deve ser tratada como problema explícito.

---

## 14. Platform / Testing

### O que é

Assets auxiliares de teste/QA vinculados à infraestrutura Foundation.

### Para que serve

- Testar pooling.
- Fornecer assets simples para validação manual ou QA local.

### Quando usar

Use apenas em contexto de QA, dev ou demonstração técnica do próprio Foundation.

### Quando não usar

- Não usar assets de testing como conteúdo de produção.
- Não usar como fonte de verdade de configuração real.

### Arquivos principais

- `Foundation/Platform/Testing/PoolDefinitionAsset.asset`
- `Foundation/Platform/Testing/PoolDefinitionAssetSFXGlobal.asset`
- `Foundation/Platform/Testing/TestPools.prefab`

### Cuidados

- Testing é suporte.
- Config real deve morar no owner do módulo consumidor ou no asset canônico de runtime.

---

# Regras gerais de uso

## 15. Use Foundation quando

- A necessidade é técnica e reutilizável.
- O bloco não possui semântica própria de gameplay.
- O mesmo utilitário pode ser usado por vários módulos.
- A dependência pode ser expressa por interface, evento ou serviço pequeno.

## 16. Não use Foundation quando

- A peça define regra de sessão, phase, actor, navigation, save, audio ou gameplay.
- A peça precisa decidir ownership de domínio.
- A peça executa fluxo canônico específico de um módulo.
- A peça existe só para um módulo específico.

Regra rápida:

> Se só um módulo precisa da peça e a peça conhece semântica desse módulo, ela provavelmente não pertence ao Foundation.

---

# Índice rápido

| Área | Arquivos principais | Uso comum |
|---|---|---|
| Events | `EventBus`, `EventBinding`, `InjectableEventBus`, `FilteredEventBus` | Pub/sub tipado entre módulos |
| FSM | `StateMachine`, `Transition`, `IState`, `IPredicate` | Estados locais simples |
| Identifiers | `UniqueIdFactory` | IDs runtime e rastreio |
| Logging | `DebugUtility`, `HardFailFastH1`, `LoggingConfigAsset` | Logs, evidência e fail-fast |
| Validation | `Preconditions` | Guard clauses simples |
| Composition | `DependencyManager`, registries, `GlobalCompositionRoot.*`, `SceneScopeCompositionRoot` | DI, boot e composição runtime |
| Config | `BootstrapConfigAsset` | Configuração obrigatória de boot |
| RuntimeMode | `RuntimeModeConfig`, providers, degraded reporter | Modo runtime e degradação controlada |
| Pooling | `IPoolService`, `PoolService`, `PoolDefinitionAsset` | Reuso técnico de `GameObject` |
| SimulationGate | `SimulationGateService`, `SimulationGateTokens` | Bloqueio/liberação de simulação |
| Observability | `BaselineInvariantAsserter` | Invariantes técnicas e auditoria |
| Testing | pool assets e prefab de teste | Suporte QA/dev |

---

# Checklist rápida

Antes de usar algo de `Foundation`, confirme:

- [ ] Isto é um utilitário técnico reutilizável?
- [ ] O módulo dono da semântica continua fora de Foundation?
- [ ] Existe interface/evento/contrato simples suficiente?
- [ ] A dependência obrigatória falha cedo?
- [ ] O lifecycle de bind/unbind ou acquire/release está claro?
- [ ] Logs e erros ajudam auditoria sem poluir o fluxo comum?

