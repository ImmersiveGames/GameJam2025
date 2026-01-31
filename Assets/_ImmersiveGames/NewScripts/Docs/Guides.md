# Guias e Checklists — NewScripts

Este arquivo consolida guias operacionais e checklists em um único ponto para reduzir o número de arquivos.

---

## Guia — Criar um novo spawnable

# HowTo – Criar um novo spawnable no NewScripts

Este guia resume o fluxo mínimo para adicionar um novo ator spawnável ao pipeline
do **WorldLifecycle** na `GameplayScene`.

> Objetivo: após um reset de gameplay, o novo ator deve aparecer corretamente em
> cena, ser rastreado pelo `IActorRegistry` e respeitar o ciclo de vida do jogo.

---

## 1. Definir o tipo de ator (ActorKind)

1. Abra o enum `ActorKind` no NewScripts.
2. Adicione um novo valor descritivo, por exemplo:
    - `Companion`, `EnemyShooter`, `ResourceNode`, etc.
3. Compile o projeto para garantir que o enum continua válido.

Se você quiser reutilizar um `ActorKind` existente (ex.: mais um `Eater`), pode
pular este passo.

---

## 2. Criar o prefab do ator

1. Na `GameplayScene`, certifique-se de ter um `WorldRoot` configurado
   corretamente (já usado por `Player` e `Eater`).
2. Crie um novo GameObject na cena, configure os componentes de domínio
   necessários, por exemplo:
    - Componente que implementa `IActor` (ou o mesmo padrão usado por Player/Eater).
    - Componente que implementa `IActorKindProvider`, retornando o `ActorKind`
      correto.
    - Controladores específicos (movimento, IA, etc.), se necessário.
3. Arraste o GameObject para a pasta de prefabs do NewScripts para criar o
   prefab (ex.: `EnemyShooter_NewScripts`).
4. Remova o instance da cena se ele só deve existir via spawn.

---

## 3. Registrar no WorldDefinition da GameplayScene

1. Abra o `WorldDefinition` usado pela `GameplayScene`
   (o mesmo asset onde já estão `Player` e `Eater`).
2. Adicione uma nova entry na lista:
    - **Enabled**: marcado.
    - **Kind**: selecione o `ActorKind` do passo 1 (ou reaproveite um existente).
    - **Prefab**: atribua o prefab criado no passo 2.
3. Defina a ordem (se houver campo de ordem) para respeitar a sequência desejada
   no spawn:
    - Ex.: `Player` (1), `Eater` (2), `SeuNovoAtor` (3).

A partir disso, o `SceneBootstrapper` irá:
- Ler o `WorldDefinition`.
- Criar o `IWorldSpawnService` correspondente.
- Registrar no `IWorldSpawnServiceRegistry` da cena.
- Permitir que o `WorldLifecycleOrchestrator` faça o spawn durante o reset.

---

## 4. Verificar integração com o ciclo de vida

Após configurar o `WorldDefinition`, rode o fluxo padrão:

1. Inicie o jogo (startup → Menu).
2. Vá para a `GameplayScene` via menu (Play).
3. Observe os logs do `WorldLifecycleOrchestrator`:
    - Deve mostrar o novo spawn service sendo executado em `Spawn`.
4. Verifique o `IActorRegistry`:
    - O count de atores deve incluir o seu novo ator.
    - O tipo/Kind deve bater com o que você configurou.

Se você tiver QAs semelhantes ao `WorldLifecycleMultiActorSpawnQa`, pode criar
ou adaptar um QA para validar que:
- Após o reset, existe pelo menos 1 instância do novo ator no registry.

---

## 5. Passos opcionais (quando necessário)

Use apenas se precisar de comportamento mais avançado:

- **Spawn customizado**
  Se o ator precisa de lógica especial de spawn (posição aleatória,
  wave-based, pool, etc.), crie uma classe de spawn service específica seguindo
  o padrão de `PlayerSpawnService` / `EaterSpawnService` e registre no mesmo
  ponto onde os outros serviços são criados (factory/registry usados hoje).

- **Hooks de lifecycle**
  Se o ator precisa reagir a fases específicas do reset (ex.: limpar estado,
  reconfigurar recursos, etc.), reutilize o mesmo padrão de componentes/hooks
  já usado por `Player` e `Eater` para o `OnAfterActorSpawn` e demais fases.

---

## Checklist rápido

Antes de considerar o novo spawnable “ok”:

- [ ] `ActorKind` definido (ou reaproveitado).
- [ ] Prefab com `IActor` + `IActorKindProvider` configurado.
- [ ] Entry adicionada no `WorldDefinition` da `GameplayScene`, `Enabled = true`.
- [ ] Logs do `WorldLifecycleOrchestrator` mostram o service executando o spawn.
- [ ] `IActorRegistry` contém o novo ator após o reset de gameplay.

---

## Checklist — ContentSwap (InPlace-only)

# Checklist — ContentSwap (InPlace-only)

> **Fonte de verdade:** log do Console (Editor/Dev).

## Caso único — InPlace

No objeto **[QA] ContentSwapQA** (DontDestroyOnLoad), executar o ContextMenu:
- `QA/ContentSwap/G01 - InPlace (NoVisuals)`

### Evidências esperadas
1. `ContentSwapRequested` com `mode=InPlace`.
2. `ContentSwapPendingSet` e `ContentSwapCommitted` com `reason` correspondente ao QA.
3. `ContentSwapPendingCleared` quando aplicável.

### Observações
- ContentSwap em NewScripts é **exclusivamente InPlace**.
