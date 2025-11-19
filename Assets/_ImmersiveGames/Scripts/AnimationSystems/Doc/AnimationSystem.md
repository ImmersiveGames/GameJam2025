# Sistema de Animação — Guia de Uso (v1.0)

## Índice
- [Visão Geral](#visão-geral)
- [Componentes Principais](#componentes-principais)
- [Fluxo de Inicialização](#fluxo-de-inicialização)
- [Registro e Injeção](#registro-e-injeção)
- [Troca de Skin (Animator em Runtime)](#troca-de-skin)
- [Configuração de Animações](#configuração-de-animações)
- [Boas Práticas (OBRIGATÓRIO)](#boas-práticas)

---

## Visão Geral

Sistema de animação robusto, preparado para:
- Multiplayer local (4+ jogadores simultâneos)
- Troca dinâmica de skins (Animator muda em runtime)
- Alta performance (zero GC em runtime)
- Total compatibilidade com o seu DependencyManager + Provider

**Funciona 100% com a hierarquia atual dos prefabs (EaterPrefab e Player01)**

---

## Componentes Principais

| Componente                  | Responsabilidade                                      | Observação                                      |
|-----------------------------|--------------------------------------------------------|-------------------------------------------------|
| `AnimationResolver`         | Resolve o Animator atual (skin ou fallback) e notifica mudanças | `[DefaultExecutionOrder(-50)]` – roda cedo |
| `AnimationControllerBase`   | Base abstrata para todos os controllers de animação | Injeção sem ActorId + fallback local |
| `IAnimatorProvider`         | Interface para obter Animator e escutar mudanças | Usado pelo DI |
| `AnimationConfig`           | ScriptableObject com nomes das animações e hashes | Cache automático de hashes |
| `AnimationConfigProvider`   | Serviço global que fornece configs por tipo | Registrado no bootstrap |
| `GlobalAnimationService`    | Serviço global para PlayAllIdle() etc. | Opcional |

---

## Fluxo de Inicialização

1. `AnimationBootstrapper.Initialize()` → registra `AnimationConfigProvider` e `GlobalAnimationService`
2. `AnimationResolver.Awake()` → registra `IAnimatorProvider` no escopo do ActorId
3. `AnimationControllerBase.Awake()` →
    - `DependencyManager.Provider.InjectDependencies(this)` (sem ActorId)
    - `GetComponent<AnimationResolver>()` → fallback local
    - Subscreve `OnAnimatorChanged`
    - Carrega `AnimationConfig`
4. `AnimationResolver.Start()` → conecta com SkinController e escuta eventos de skin

---

## Registro e Injeção

### Registro (AnimationResolver)

```csharp
private void Awake()
{
    _actor = GetComponent<IActor>();
    if (_actor != null && !string.IsNullOrEmpty(_actor.ActorId))
    {
        DependencyManager.Provider.RegisterForObject(_actor.ActorId, this as IAnimatorProvider);
    }
}
```

### Injeção (AnimationControllerBase)

```csharp
protected virtual void Awake()
{
    // Injeção SEM ActorId — como no original que funcionava
    DependencyManager.Provider.InjectDependencies(this);

    animationResolver = GetComponent<AnimationResolver>();
    // ... resto do código
}
```

**Importante**: A injeção é feita **sem passar ActorId** — o injector usa o escopo do objeto atual e o fallback `GetComponent` garante que funcione mesmo se o DI falhar.

---

## Troca de Skin (Animator em Runtime)

O `AnimationResolver` escuta eventos globais e locais de skin:

- `SkinUpdateEvent`
- `SkinInstancesCreatedEvent`
- Eventos locais do `SkinController`

Quando o ModelRoot muda → `RefreshAnimator()` → `OnAnimatorChanged?.Invoke(newAnimator)`

O `AnimationControllerBase` recebe no `OnAnimatorChanged` e atualiza o `animator`.

**Funciona perfeitamente com troca de skin em multiplayer local.**

---

## Configuração de Animações

1. Crie um `AnimationConfig` (ScriptableObject)
2. Preencha os nomes das animações (Idle, GetHit, Die, Revive)
3. No `AnimationBootstrapper`, registre:

```csharp
configProvider.RegisterConfig("EaterAnimationController", yourEaterConfig);
configProvider.RegisterConfig("PlayerAnimationController", yourPlayerConfig);
```

O controller carrega automaticamente via `GetType().Name`.

---

## Boas Práticas (REGRAS OBRIGATÓRIAS)

| Regra                                      | Como fazer                                            | Status       |
|--------------------------------------------|-------------------------------------------------------|--------------|
| AnimationResolver no prefab                | Sempre no mesmo GameObject do ActorMaster             | OBRIGATÓRIO  |
| Injeção de AnimationResolver               | `DependencyManager.Provider.InjectDependencies(this)` (sem ActorId) | OBRIGATÓRIO  |
| Fallback local                             | Sempre usar `GetComponent<AnimationResolver>()`       | OBRIGATÓRIO  |
| Registro do serviço                       | Em Awake do Resolver, com ActorId                     | OBRIGATÓRIO  |
| Config de animação                         | Registrar no AnimationBootstrapper                    | OBRIGATÓRIO  |
| Troca de skin                              | Não fazer nada — o sistema já cuida                  | AUTOMÁTICO   |

> **Regra de ouro**:  
> Se o Animator não atualiza na troca de skin → verifique se o AnimationResolver está no prefab e ativo.

---

**Sistema 100% funcional, performático, SOLID e compatível com seu DI + Provider.**

Qualquer dúvida sobre animação ou skin, é só chamar!

**Última atualização**: 18 de novembro de 2025 — v1.0 (baseado nos arquivos originais que funcionavam)

Pronto para colar no seu projeto! 😊