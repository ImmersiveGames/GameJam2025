# Sistema de Animação — Guia de Uso (v1.1)

## Índice
- [Visão Geral](#visão-geral)
- [Componentes Principais](#componentes-principais)
- [Fluxo de Inicialização](#fluxo-de-inicialização)
- [Registro e Injeção](#registro-e-injeção)
- [Troca de Skin (Animator em Runtime)](#troca-de-skin-animator-em-runtime)
- [Configuração de Animações](#configuração-de-animações)
- [Boas Práticas (OBRIGATÓRIO)](#boas-práticas-obrigatório)
- [Histórico de Versão](#histórico-de-versão)

---

## Visão Geral

Sistema de animação desacoplado, preparado para:

- Multiplayer local (uso de `ActorId` para escopo de serviços).
- Troca de **skin** em runtime (Animator pode mudar).
- Separação clara entre:
   - Descoberta/fornecimento de `Animator` (`AnimationResolver` / `IAnimatorProvider`).
   - Regras de animação de gameplay (`AnimationControllerBase` + controllers concretos).
   - Dados de animação (`AnimationConfig` via ScriptableObject).
   - Serviços de apoio (`AnimationConfigProvider`, `GlobalAnimationService`).

Objetivos principais:

- Respeitar SOLID:
   - SRP: cada classe com responsabilidade única bem definida.
   - DIP: sistemas dependem de **interfaces** (`IAnimatorProvider`, `IActorAnimationController`), não de implementações concretas.
- Facilitar manutenção e testes:
   - Controllers de animação testáveis isoladamente.
   - Resolvedor de Animator plugável e sensível ao sistema de skin.

---

## Componentes Principais

| Componente                   | Responsabilidade                                               | Observações                                         |
|-----------------------------|----------------------------------------------------------------|-----------------------------------------------------|
| `AnimationResolver`         | Resolve o `Animator` atual do ator e notifica mudanças         | Integra com `ActorSkinController` + `FilteredEventBus` |
| `AnimationControllerBase`   | Base abstrata para controllers de animação de cada ator        | Resolve `AnimationConfig`, gerencia DI por `ActorId` |
| `IAnimatorProvider`         | Interface para obter um `Animator`                             | Implementada por `AnimationResolver`                |
| `IActorAnimationController` | Interface para comandos de animação básicos (Hit, Death, etc.) | Usada por `GlobalAnimationService` e gameplay       |
| `AnimationConfig`           | ScriptableObject com nomes e hashes de animações               | Por tipo de controller / prefab                     |
| `AnimationConfigProvider`   | Serviço global de fornecimento de configs                      | Registrado no bootstrap                             |
| `GlobalAnimationService`    | Serviço global opcional (ex.: `PlayAllIdle`)                   | Controllers podem se registrar nele                 |
| `AnimationBootstrapper`     | Inicializa configs e serviços globais na carga do jogo         | Usa `RuntimeInitializeOnLoadMethod`                 |

---

## Fluxo de Inicialização

1. **Bootstrap global**

   - `AnimationBootstrapper.Initialize()` é chamado pelo Unity em `SubsystemRegistration`.
   - Responsabilidades:
      - Criar `AnimationConfigProvider`.
      - Registrar configs padrão por tipo de controller:
         - `"PlayerAnimationController"` → `DefaultPlayerAnimationConfig`
         - `"EaterAnimationController"` → `DefaultEaterAnimationConfig`
         - `"EnemyAnimationController"` → `DefaultEnemyAnimationConfig`
      - Registrar `AnimationConfigProvider` como serviço global no `DependencyManager`.
      - Criar e registrar `GlobalAnimationService` global.

2. **Resolver de Animator por ator**

   - Em cada prefab com animação:
      - `AnimationResolver.Awake()`:
         - Obtém `IActor` local.
         - Registra `IAnimatorProvider` no escopo daquele `ActorId` usando o `DependencyManager`.
      - `AnimationResolver.Start()`:
         - Localiza `ActorSkinController` (via DI por `ActorId` ou via componentes na hierarquia).
         - Registra listeners nos eventos de skin (globais e locais).

3. **Controller de Animação do ator**

   - `AnimationControllerBase.Awake()`:
      - Injeta dependências do objeto via `DependencyManager.Provider.InjectDependencies(this)`.
      - Garante presença de:
         - `AnimationResolver` (obrigatório).
         - `ActorMaster` (obrigatório).
      - Assina o evento `OnAnimatorChanged` do `AnimationResolver`.
      - Resolve `AnimationConfig`:
         - Se `animationConfig` não estiver atribuída no Inspector:
            - Tenta obter via `AnimationConfigProvider.GetConfig(GetType().Name)`.
         - Se ainda assim for `null`:
            - Cria uma `AnimationConfig` padrão em runtime e loga um warning.
      - Obtém o `Animator` atual via `_animationResolver.GetAnimator()`.
      - Registra o próprio controller para o `ActorId` no `DependencyManager.Instance`.

4. **Desligamento / Ciclo de vida**

   - `AnimationControllerBase.OnDisable()`:
      - Se registrou serviços para o `ActorId`, chama `ClearObjectServices(Actor.ActorId)` e loga a remoção.
   - `AnimationControllerBase.OnDestroy()`:
      - Remove assinatura de `OnAnimatorChanged` no `AnimationResolver`.
   - `AnimationResolver.OnDisable()` / `OnDestroy()`:
      - Desregistra todos os listeners (EventBus e eventos do `ActorSkinController`) de forma segura.

---

## Registro e Injeção

### 1. Configs de animação (`AnimationConfig`)

- Registradas por **ID de config** (`string`) no `AnimationConfigProvider`.
- Convenção padrão:
   - ID = `GetType().Name` do controller de animação.
   - Exemplo: `PlayerAnimationController` → `DefaultPlayerAnimationConfig`.

- Registro padrão acontece no `AnimationBootstrapper`:
   - Usa `Resources.Load<AnimationConfig>(...)` para carregar assets fixos.
   - Se o asset não for encontrado:
      - É logado um `Warning`.
      - O controller usará uma `AnimationConfig` gerada em runtime (nomes padrão).

### 2. Serviços por ator (`ActorId`)

- `AnimationResolver`:
   - Registra `IAnimatorProvider` para o `ActorId` do `IActor`.
   - Permite que outros sistemas resolvam o Animator daquele ator sem depender diretamente do GameObject.

- `AnimationControllerBase`:
   - Registra o próprio controller no `DependencyManager.Instance` para o `ActorId` do `ActorMaster`.
   - No `OnDisable()`, chama `ClearObjectServices(Actor.ActorId)` para limpar serviços desse objeto (seguindo o padrão já utilizado no projeto).

> Importante: o sistema assume que `ActorId` é único e consistente por ator (especialmente em multiplayer local).

---

## Troca de Skin (Animator em Runtime)

O fluxo de troca de skin é totalmente transparente para o controller de animação:

1. **Descoberta inicial do Animator**

   - `AnimationResolver.ResolveAnimator()`:
      - Se existir `ActorSkinController`:
         - Busca `Animator` nos skin instances do `ModelType.ModelRoot`.
         - Usa o primeiro `Animator` encontrado.
      - Caso contrário:
         - Faz `GetComponentInChildren<Animator>(true)` para fallback.

2. **Eventos que disparam atualização de Animator**

   O `AnimationResolver` escuta:

   - Eventos globais via `FilteredEventBus`:
      - `SkinEvents` (quando uma skin é aplicada/atualizada).
      - `SkinInstancesCreatedEvent` (quando as instâncias de modelo são criadas).
   - Eventos locais do `ActorSkinController`:
      - `OnSkinApplied(ISkinConfig config)`
      - `OnSkinInstancesCreated(ModelType modelType, List<GameObject> instances)`

3. **Atualização do Animator**

   - Quando um desses eventos indica mudança no `ModelType.ModelRoot`:
      - `RefreshAnimator()`:
         - Zera `_cachedAnimator`.
         - Resolve um novo `Animator` (`ResolveAnimator()`).
         - Dispara `OnAnimatorChanged(newAnimator)`.

4. **Controllers reagindo à mudança**

   - `AnimationControllerBase` está inscrito em `OnAnimatorChanged`:
      - Quando chamado, atualiza o campo `animator`.
   - Controllers concretos continuam chamando `PlayHash(...)` normalmente, sem precisar saber que a skin mudou.

---

## Configuração de Animações

### `AnimationConfig` (ScriptableObject)

Campos:

- `idleAnimation`  (default `"Idle"`)
- `hitAnimation`   (default `"GetHit"`)
- `deathAnimation` (default `"Die"`)
- `reviveAnimation` (default `"Revive"`)

Para cada campo existe um hash correspondente:

- `IdleHash`, `HitHash`, `DeathHash`, `ReviveHash`

Uso típico dentro de um controller concreto:

```csharp
public class PlayerAnimationController : AnimationControllerBase, IActorAnimationController
{
    public void PlayIdle()  => PlayHash(IdleHash);
    public void PlayHit()   => PlayHash(HitHash);
    public void PlayDeath() => PlayHash(DeathHash);
    public void PlayRevive() => PlayHash(ReviveHash);
}
