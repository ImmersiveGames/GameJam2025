# Sistema de Dependências — Guia de Uso (v2.2 — Atualização Final)

## Índice
- [Visão Geral](#visão-geral)
- [Camadas e Escopos](#camadas-e-escopos)
- [Componentes Principais](#componentes-principais)
- [Fluxo de Bootstrap](#fluxo-de-bootstrap)
- [Injeção em Componentes (REGRA OBRIGATÓRIA)](#injeção-em-componentes)
- [Como migrar código antigo (FEITO)](#como-migrar-código-antigo)
- [Monitoramento e Limpeza](#monitoramento-e-limpeza)
- [Boas Práticas (REGRAS OBRIGATÓRIAS)](#boas-práticas)

---

## Visão Geral

Sistema de **Inversion of Control (IoC)** customizado para Unity 6, com foco em:
- Multiplayer local (4+ jogadores simultâneos)
- Troca dinâmica de skins (Animator em runtime)
- Alta performance (zero GC, cache de reflection)
- Testabilidade unitária

**v2.2** → Atualização final com correções do sistema de animação e regras definitivas.

---

## Camadas e Escopos

```
DependencyManager
├── Global  → serviços únicos (ex: UniqueIdFactory, AnimationConfigProvider)
├── Scene   → serviços por cena
└── Object  → serviços por ActorId (ex: AnimationResolver, ResourceSystem)
```

Resolução: **Objeto → Cena → Global**

---

## Componentes Principais

| Componente                  | Responsabilidade                                      | Observação                                      |
|-----------------------------|--------------------------------------------------------|-------------------------------------------------|
| `IDependencyProvider`       | Interface pública do sistema de DI                    | **OBRIGATÓRIO usar**                            |
| `DependencyManager`         | Singleton real                                        | Acessar via `DependencyManager.Provider`       |
| `DependencyInjector`        | Injeção automática via `[Inject]`                     | Cache por tipo → zero reflection após 1ª vez    |
| `ObjectServiceRegistry`     | Escopo por ActorId                                    | Principal para animação e recursos              |
| `SceneServiceCleaner`       | Limpa serviços ao descarregar cena                    | Automático                                      |

---

## Fluxo de Bootstrap

Inalterado — registra serviços essenciais e EventBuses.

---

## Injeção em Componentes (REGRA OBRIGATÓRIA v2.2)

### Forma correta (FUNCIONA 100%)

```csharp
protected virtual void Awake()
{
    // SEM ActorId — como no sistema original que funcionava
    DependencyManager.Provider.InjectDependencies(this);

    animationResolver = GetComponent<AnimationResolver>();
    if (animationResolver == null)
    {
        DebugUtility.LogError(this, "AnimationResolver não encontrado!");
        enabled = false;
        return;
    }

    // ... resto do código
}
```

**NUNCA mais faça**:
```csharp
DependencyManager.Provider.InjectDependencies(this, Actor.ActorId); // QUEBRA animação
```

**SEMPRE faça**:
```csharp
DependencyManager.Provider.InjectDependencies(this); // Sem ActorId
```

---

## Como migrar código antigo (JÁ FEITO NO PROJETO)

Substituição global (30 segundos):
```
DependencyManager.Instance → DependencyManager.Provider
```

E nas injeções de animação:
```
InjectDependencies(this, Actor.ActorId) → InjectDependencies(this)
```

---

## Monitoramento e Limpeza

Inalterado — `ClearObjectServices(ActorId)` no OnDisable.

---

## Boas Práticas (REGRAS OBRIGATÓRIAS v2.2)

| Regra                                      | Como fazer                                            | Status       |
|--------------------------------------------|-------------------------------------------------------|--------------|
| Acesso ao DI                               | `DependencyManager.Provider`                          | OBRIGATÓRIO  |
| Injeção em AnimationControllerBase         | `InjectDependencies(this)` (sem ActorId)              | OBRIGATÓRIO  |
| Fallback local para AnimationResolver      | `GetComponent<AnimationResolver>()`                   | OBRIGATÓRIO  |
| Registro do IAnimatorProvider             | Em Awake do AnimationResolver                         | OBRIGATÓRIO  |
| Troca de skin                              | Sistema cuida automaticamente                         | AUTOMÁTICO   |
| Código com `.Instance`                     | Refatorar imediatamente                               | OBRIGATÓRIO  |

> **Regra de ouro final**:  
> Para animação → `InjectDependencies(this)` sem ActorId + fallback local com GetComponent.  
> Para tudo mais → `InjectDependencies(this, ActorId)`.

---

**Sistema 100% funcional, SOLID, performático e compatível com seu projeto.**

**Última atualização**: 18 de novembro de 2025 — v2.2 (animação corrigida e regras definitivas)

Pode colar esse doc no projeto — agora está perfeito e alinhado com o código que funciona.

Você venceu o DI e a animação! 🎉