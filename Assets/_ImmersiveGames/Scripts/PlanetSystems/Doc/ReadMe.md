# 📘 Planet Systems — Documentação Oficial

*(Versão revisada após refatoração e integração com arquitetura moderna)*

## 1. Visão Geral

O módulo **Planet Systems** é responsável por:

* Criar, organizar e gerenciar planetas no jogo.
* Controlar recursos de planetas (descobertos / não descobertos).
* Coordenar detecção, marcação visual e seleção pelo jogador/Eater.
* Integrar-se com os sistemas de UI, áudio, detecção, sensores e damage.

A arquitetura foi projetada seguindo princípios **SOLID**, com ênfase em:

* Composição
* Baixo acoplamento
* Responsabilidades claras
* Eventos como mecanismo de comunicação
* Estrutura intuitiva para não-programadores (prefabs, marcadores, icons)

---

## 2. Estrutura de Arquivos e Responsabilidades

### 2.1. `PlanetsMaster`

**Papel:** raiz lógica de cada planeta.

Responsabilidades:

* Registrar o ator (`IPlanetActor`).
* Coordenar módulo de recurso (`PlanetResourceState`).
* Coordenar entradas de defesa (quando presentes).
* Emitir eventos:

    * `ResourceAssigned`
    * `ResourceDiscoveryChanged`
* Validar obrigatoriedades:

    * `PlanetMotion`
    * `MarkPlanet`
    * Detectáveis

Não controla movimento nem flags visuais.
Não conhece UI.
Não lida com dano diretamente.

---

### 2.2. `PlanetResourceState`

**Papel:** estado interno do recurso.

Armazena:

* Tipo de recurso (`PlanetResourcesSo`)
* Se o recurso foi descoberto ou não

Permite operações:

* `AssignResource()`
* `RevealResource()`
* `ResetDiscovery()`

---

### 2.3. `PlanetResourceDisplay`

**Papel:** exibir o recurso na UI (imagem).

* Observa eventos de `PlanetsMaster`.
* Atualiza:

    * ícone descoberto
    * ícone não descoberto
* Descobre automaticamente o `Image` em filhos caso não configurado.

Nunca deve acessar diretamente o SO de recurso.

---

### 2.4. `PlanetDetectableController`

**Papel:** detectar entrada/saída em sensores.

* Ao ser detectado:

    * Revela o recurso (`_planetMaster.RevealResource()`).
    * Reproduz som (caso configurado).

Não gerencia marcação.
Não gerencia UI.

---

### 2.5. `MarkPlanet`

**Papel:** permitir que um planeta seja marcado/desmarcado visualmente.

* Controla flag visual (`FlagMarkPlanet`).
* Expõe:

    * `IsMarked`
    * `ToggleMark()`
    * `Unmark()`
    * `RefreshFlagMark()`
* Emite eventos:

    * `PlanetMarkedEvent`
    * `PlanetUnmarkedEvent`

---

### 2.6. `PlanetMarkingManager`

**Papel:** dono da regra de “um planeta marcado por vez”.

Regras garantidas:

1. Zero ou um planeta marcado.
2. Marcar um planeta diferente → desmarca o anterior.
3. Clicar no mesmo planeta → desmarca e fica nenhum.

Gerencia apenas estado lógico, não visual.

---

### 2.7. `PlanetOrbitArranger`

**Papel:** organizar planetas em órbitas.

* Calcula:

    * raio da órbita
    * posição inicial
    * velocidade orbital
    * rotação própria
* Configura `PlanetMotion` se existir.

Isolado do resto (não conhece recursos, UI, marcação).

---

### 2.8. `PlanetsManager`

**Papel:** orquestrador global do sistema.

Responsabilidades:

* Instanciar planetas.
* Atribuir recursos.
* Registrar detectáveis.
* Organizar órbitas (via `PlanetOrbitArranger`).
* Rastrear planetas ativos.
* Integrar com danos:

    * Escuta `DeathEvent`
    * Remove planetas destruídos
    * Limpa marcação quando necessário

Nunca cria lógica de marcação.
Nunca acessa UI.

---

## 3. Fluxo Completo do Sistema

### 3.1. Inicialização

1. Cena inicia.
2. `PlanetsManager.Start()` → `InitializePlanetsRoutine()`.
3. Cria planetas (`PlanetsMaster`) como filhos de `planetsRoot`.
4. Atribui cada recurso.
5. Organiza órbitas (`PlanetOrbitArranger`).
6. Dispara `PlanetsInitializationCompletedEvent`.

---

### 3.2. Descoberta do recurso

1. Player/Eater entra no raio de sensor.
2. `PlanetDetectableController.OnEnterDetection()`.
3. Verifica se recurso já foi descoberto.
4. Revela (`PlanetsMaster.RevealResource()`).
5. `PlanetsMaster` emite:

    * `ResourceDiscoveryChanged`
6. `PlanetResourceDisplay` atualiza UI.

---

### 3.3. Marcação de planetas

Passo a passo:

1. Player clica num planeta (via `PlanetInteractService`).
2. `PlanetMarkingManager.TryMarkPlanet()` localiza `MarkPlanet` e chama `ToggleMark()`.
3. `MarkPlanet.Mark()` emite `PlanetMarkedEvent`.
4. `PlanetMarkingManager` recebe e:

    * desmarca o anterior
    * atualiza `_currentlyMarkedPlanet`
    * emite `PlanetMarkingChangedEvent`
5. UI/IA podem reagir a esse evento.

Sempre no máximo um planeta marcado.

---

### 3.4. Morte do planeta (Damage System)

Fluxo real:

1. Planeta leva dano.
2. `DamagePipeline` processa valores.
3. Vida chega a 0.
4. `DamageLifecycleModule` dispara `DeathEvent`.
5. `PlanetsManager.OnPlanetDeath()`:

    * Remove planeta do sistema
    * Força `Unmark()` caso estivesse marcado
    * Remove detectáveis
    * Remove das listas de consulta

Não destrói `GameObject` — a pool/FX cuidam disso.

---

## 4. Boas Práticas

### 4.1. Para Designers

* Planet Prefab deve conter:

    * `PlanetsMaster`
    * `PlanetResourceState`
    * `PlanetDetectableController`
    * `PlanetMotion`
    * `MarkPlanet` com `FlagMarkPlanet`
    * UI 3D opcional (ícone)

### 4.2. Para Programadores

* Nunca marcar planeta diretamente:

    * use `PlanetMarkingManager.TryMarkPlanet()`.
* Nunca mudar sprite manualmente:

    * deixe o `PlanetResourceDisplay` escutar eventos.
* Para detectar planeta marcado no gameplay:

    * `PlanetMarkingManager.Instance.CurrentlyMarkedPlanet`
* Para obter detectável do planeta marcado:

    * `PlanetsManager.GetPlanetMarked()`
* Para saber se recurso foi descoberto:

    * `PlanetsMaster.IsResourceDiscovered`

### 4.3. Naming

* Scripts seguem padrão:

    * Nome do módulo + propósito (ex.: `PlanetOrbitArranger`, `PlanetResourceDisplay`).
* Campos públicos nomeados para designers.

---

## 5. Integração com Outros Sistemas

### 5.1. Damage System

Planetas integram apenas via:

* `ActorId`
* `DeathEvent`
* (opcional futuramente: `ReviveEvent` e Undo)

Não existe duplicação de lógica de morte.
Planeta expira do sistema **somente** quando o módulo de dano diz que morreu.

---

### 5.2. Detecção

Baseado em:

* `IDetectable`
* `IDetector`
* `DetectionType`

`PlanetDetectableController` não cria regras próprias — só responde ao sistema global.

---

### 5.3. UI

* Ícones e sprites dependem somente de:

    * `PlanetResourcesSo`
    * `ResourceAssigned` / `ResourceDiscoveryChanged`

Sem acoplamento a lógica de jogo.

---

## 6. Eventos do Sistema

**Emitidos pelos planetas:**

| Evento                                | Quando                    | Quem Emite             |
| ------------------------------------- | ------------------------- | ---------------------- |
| `ResourceAssigned`                    | Recurso designado         | `PlanetsMaster`        |
| `ResourceDiscoveryChanged`            | Recurso revelado/ocultado | `PlanetsMaster`        |
| `PlanetMarkedEvent`                   | planeta marcado           | `MarkPlanet`           |
| `PlanetUnmarkedEvent`                 | planeta desmarcado        | `MarkPlanet`           |
| `PlanetMarkingChangedEvent`           | troca de planeta marcado  | `PlanetMarkingManager` |
| `PlanetsInitializationCompletedEvent` | fim da inicialização      | `PlanetsManager`       |

**Recebidos pelos planetas:**

| Evento       | Origem        | Resultado                          |
| ------------ | ------------- | ---------------------------------- |
| `DeathEvent` | Damage System | planeta removido do PlanetsManager |

---

## 7. Exemplos de Uso

### 7.1. Obter planeta atualmente marcado

```csharp
var marked = PlanetMarkingManager.Instance.CurrentlyMarkedPlanet;
if (marked != null)
{
    Debug.Log("Planeta marcado: " + marked.name);
}
```

### 7.2. Checar se o jogador revelou um recurso

```csharp
if (planetMaster.IsResourceDiscovered)
{
    // liberar crafting, UI, bônus etc.
}
```

### 7.3. Reagir à morte de planeta

```csharp
EventBus<DeathEvent>.Register(evt =>
{
    Debug.Log("Planeta destruído: " + evt.entityId);
});
```

---

## 8. Ciclo de Vida do Planeta (Resumo)

* **Spawn** → PlanetsManager
* **Recebe Recurso** → `AssignResource()`
* **UI mostra ícone oculto**
* **Detectado por Player/Eater** → recurso revelado
* **Player marca** → UI e IA respondem
* **Sofre dano** → `DamagePipeline` processa
* **Vida chega a 0** → `DeathEvent`
* **PlanetsManager remove** → limpeza completa
* **FX/pool tratam o resto**

---

## 9. Status de Arquitetura

O módulo está agora:

* Seguindo princípios **SOLID**
* Com responsabilidades bem isoladas
* Sem duplicação interna
* Com API clara para:

    * Gameplay
    * UI
    * AI
    * Sensores
    * Damage System

---