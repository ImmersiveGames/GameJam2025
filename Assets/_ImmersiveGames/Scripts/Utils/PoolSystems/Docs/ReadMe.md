# ♻️ Sistema de Pooling — Guia de Uso (v2.0)

## 📚 Índice
1. [Visão Geral](#visão-geral)
2. [Arquitetura](#arquitetura)
3. [ScriptableObjects de Configuração](#scriptableobjects-de-configuração)
4. [Fluxo de Execução](#fluxo-de-execução)
5. [Extensões para Gameplay](#extensões-para-gameplay)
6. [Integração Passo a Passo](#integração-passo-a-passo)
7. [Boas Práticas e Debug](#boas-práticas-e-debug)

---

## 🎯 Visão Geral

O **PoolSystem** provê reutilização eficiente de objetos (projetis, inimigos, FX) mantendo consistência com o `DependencyManager` e os sistemas reativos (event bus, recursos). Ele foi desenvolvido para suportar multiplayer local, garantindo que ativações múltiplas por frame sejam controladas e rastreáveis.

---

## 🧠 Arquitetura

```
PoolManager (PersistentSingleton)
└── ObjectPool (por PoolData)
    ├── Queue<IPoolable>      → Objetos disponíveis
    ├── List<IPoolable>       → Objetos ativos
    ├── LifetimeManager       → Registro opcional de tempo de vida
    └── Eventos UnityEvent    → OnObjectActivated / OnObjectReturned

IPoolable (contrato)
├── PooledObject             → Base abstrata com hooks virtuais
└── Implementações custom    → Projetis, inimigos, FX
```

* `PoolManager` registra dinamicamente pools a partir de `PoolData`.
* `ObjectPool` gerencia ciclo de vida, expansão e reconfiguração automática quando objetos retornam.
* `LifetimeManager` devolve objetos ao pool após tempo limite, evitando leaks.

---

## 🧾 ScriptableObjects de Configuração

### `PoolData`
* Define `ObjectName`, `InitialPoolSize`, `CanExpand`, `ObjectConfigs` e `ReconfigureOnReturn`.
* Método estático `Validate` protege contra configurações inconsistentes.

### `PoolableObjectData`
* Classe base com `Prefab` e `Lifetime`.
* Pode ser estendida (ex.: `EnemyObjectData`) para incluir metadados específicos (estado inicial de IA, etc.).

---

## 🔁 Fluxo de Execução

1. **Registro** — `PoolManager.RegisterPool(poolData)` cria um `GameObject` com `ObjectPool`, injeta `PoolData` e chama `Initialize()`.
2. **Inicialização** — `ObjectPool.Initialize()` instância `InitialPoolSize` objetos (`CreatePoolable`).
3. **Aquisição** — `GetObject(position, spawner, direction)` retorna `IPoolable` preparado (`PoolableReset`) e opcionalmente ativa.
4. **Ativação** — `ActivatePoolable` chama `IPoolable.Activate`, adiciona à lista de ativos e dispara `OnObjectActivated`.
5. **Retorno** — `ReturnObject` desativa, retorna ao queue, dispara `OnObjectReturned` e reconfigura se necessário.
6. **Expansão** — Se `CanExpand`, `RetrieveOrCreate` instancia novos objetos quando o pool está vazio.
7. **Lifetime** — `PooledObject` registra no `LifetimeManager` se `Lifetime > 0`, que atualiza no `Update()` e chama `ReturnToPool` ao expirar.

---

## 🧩 Extensões para Gameplay

* `IPoolable.Configure` recebe `PoolableObjectData`, `ObjectPool` e `IActor spawner` — útil para atribuir dono no multiplayer local.
* `PooledObject.Spawner` armazena referência ao ator que gerou o objeto (para crédito de kills, etc.).
* `Reconfigure(config)` permite atualizar comportamentos sem recriar instâncias.
* `GetMultipleObjects` facilita spawn de enxames/rajadas respeitando limites.

---

## 🚀 Integração Passo a Passo

1. **Criar Configurações**
   * Crie `PoolableObjectData` (ex.: `EnemyObjectData`) apontando para o prefab que implementa `IPoolable`.
   * Crie `PoolData` listando os objetos e configurando tamanho inicial.

2. **Registrar no Bootstrap**
   ```csharp
   [SerializeField] private PoolData enemyPool;

   private void Awake()
   {
       PoolManager.Instance.RegisterPool(enemyPool);
   }
   ```

3. **Consumir o Pool**
   ```csharp
   var pool = PoolManager.Instance.GetPool(enemyPool.ObjectName);
   var enemy = pool.GetObject(spawnPoint, spawner: _actor);
   ```

4. **Devolver ao Pool**
   ```csharp
   pool.ReturnObject(enemy);
   ```

5. **Configurar Tempo de Vida**
   * Ajuste `PoolableObjectData.Lifetime` (> 0) para usar `LifetimeManager` automaticamente.

---

## ✅ Boas Práticas e Debug

| Situação | Diagnóstico | Ação |
| --- | --- | --- |
| Pool não inicializa | `PoolData.Validate` falhou | Verifique nome, tamanho inicial e prefabs; logs aparecem em `DebugUtility` |
| Prefab não implementa `IPoolable` | `ObjectPool.CreatePoolable` loga erro | Adicione script derivado de `PooledObject` ao prefab |
| Exaustão frequente | `_hasWarnedExhausted` dispara | Habilite `CanExpand` ou aumente `InitialPoolSize`; monitore em builds |
| Objetos não retornam | Lifetime não definido ou `ReturnObject` não chamado | Garanta chamadas em `OnDeactivate` e revise `LifetimeManager` |
| Multiplayer local | Utilize `IActor spawner` nos métodos para rastrear donos e sincronizar estatísticas |

Este sistema promove reutilização eficiente e segura, mantendo altos padrões de arquitetura (responsabilidades claras, validações e logs estruturados).
