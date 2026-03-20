# ADR-0029 â€” Canonical Pooling in NewScripts

- **Status:** Accepted
- **Implementation Status:** Complete
- **Validation Status:** PASS
- **Date:** 2026-03-14
- **Last Validated:** 2026-03-14
- **Scope:** `Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/**`

> **Nota de rollout (2026-03-20):** Este ADR permanece aceito como contrato arquitetural. No snapshot atual do repositorio, o modulo canÃƒÂ´nico precisou de reimplementacao incremental; o estado real de rollout esta rastreado em `Docs/Reports/Audits/2026-03-20/ADR-0029-Pooling-Rollout-Tracker.md`. O status historico "Complete/PASS" nao deve ser lido, isoladamente, como garantia de modulo operacional ja presente neste snapshot.
>
> **Nota de fechamento operacional (2026-03-20):** O rollout incremental iniciado em 2026-03-20 foi concluido. A implementacao canonica atual esta disponivel em `Infrastructure/Pooling/**`, com rastreabilidade operacional no tracker de 2026-03-20, e o modulo esta pronto para consumo por outros modulos sem bootstrap alternativo.

---

## Contexto

O projeto consolidou um canon em `NewScripts` baseado em:

- `GlobalCompositionRoot` como owner da inicializaÃ§Ã£o global
- direct-ref + fail-fast
- eliminaÃ§Ã£o de bootstrap paralelo, singleton estrutural e fallback frouxo
- separaÃ§Ã£o entre infraestrutura compartilhada e mÃ³dulos de domÃ­nio
- integraÃ§Ã£o explÃ­cita com serviÃ§os globais via DI

No legado existia uma infraestrutura de pooling funcional com capacidades Ãºteis:

- prewarm
- rent / return explÃ­citos
- reset do objeto pooled
- expansÃ£o opcional
- auto-return por lifetime
- batch acquire

Essa base tinha valor real, mas nÃ£o podia ser promovida diretamente ao canon porque trazia decisÃµes incompatÃ­veis com `NewScripts`, como:

- ownership por `PersistentSingleton`
- identidade por `string`
- acoplamento do nÃºcleo com domÃ­nio (`IActor`, `spawner`)
- regra hardcoded de posicionamento (`y = 0`)
- lifetime obrigatÃ³rio
- expansÃ£o sem teto explÃ­cito
- reconfiguraÃ§Ã£o implÃ­cita
- mistura entre infraestrutura compartilhada e detalhes de domÃ­nio

---

## Problema

O projeto precisava de uma infraestrutura canÃ´nica de pooling que:

- preservasse o valor funcional do legado
- eliminasse acoplamentos incompatÃ­veis com o canon atual
- fosse genÃ©rica o suficiente para servir mÃºltiplos domÃ­nios
- fosse ownership de infraestrutura compartilhada, nÃ£o de um mÃ³dulo especÃ­fico
- pudesse ser validada de forma standalone, sem depender de um mÃ³dulo consumidor real

---

## Drivers

- aderÃªncia ao canon atual de `NewScripts`
- pooling como infraestrutura compartilhada do projeto
- fail-fast em configuraÃ§Ã£o invÃ¡lida
- ownership claro no `GlobalCompositionRoot`
- eliminaÃ§Ã£o de singleton estrutural
- eliminaÃ§Ã£o de `string` como identidade principal de pool
- separaÃ§Ã£o entre nÃºcleo genÃ©rico e domÃ­nio
- preservaÃ§Ã£o de prewarm, rent/return e auto-return opcional
- capacidade de servir como base para os usos existentes no legado
- evitar regressÃ£o funcional

---

## DecisÃ£o

Foi criada uma infraestrutura canÃ´nica de pooling em:

`Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/**`

Essa infraestrutura Ã© tratada como **infraestrutura compartilhada** do projeto.

### ConsequÃªncia direta

Nenhum mÃ³dulo de domÃ­nio deve criar seu prÃ³prio â€œpool manager estruturalâ€ paralelo.

O pooling canÃ´nico existe e Ã© considerado vÃ¡lido por si sÃ³, independentemente de qual mÃ³dulo venha a consumi-lo depois.

---

## Estrutura adotada

- `Infrastructure/Pooling/Contracts/**`
- `Infrastructure/Pooling/Config/**`
- `Infrastructure/Pooling/Runtime/**`

---

## Ownership

A inicializaÃ§Ã£o e o ownership da infraestrutura de pooling pertencem ao `GlobalCompositionRoot`.

NÃ£o entram no canon:

- `PersistentSingleton`
- `RuntimeInitializeOnLoadMethod` como owner estrutural
- bootstrap estÃ¡tico paralelo
- `Resources.Load`
- singletons privados por mÃ³dulo como base do pooling

O pooling canÃ´nico sobe como serviÃ§o global registrado no DI.

---

## Identidade dos pools

A identidade estrutural de um pool Ã© feita por **referÃªncia direta de asset**, nÃ£o por `string`.

### ConsequÃªncias

- nÃ£o usar `ObjectName` string como chave principal
- nÃ£o usar nome de prefab como identidade estrutural
- a definiÃ§Ã£o canÃ´nica do pool Ã© um asset prÃ³prio

---

## Asset canÃ´nico de definiÃ§Ã£o

A definiÃ§Ã£o canÃ´nica adotada Ã©:

**`PoolDefinitionAsset`**

Campos da versÃ£o atual:

- `prefab`
- `initialSize`
- `canExpand`
- `maxSize`
- `autoReturnSeconds`
- `poolLabel`

### Regras

- um prefab por pool
- `poolLabel` serve para observabilidade, nÃ£o como identidade estrutural
- `autoReturnSeconds <= 0` significa sem retorno automÃ¡tico
- `canExpand = false` limita o pool ao `initialSize`
- `canExpand = true` respeita `maxSize` como teto

---

## NÃºcleo genÃ©rico

O nÃºcleo do pooling Ã© genÃ©rico e nÃ£o conhece domÃ­nio.

### NÃ£o entram no nÃºcleo

- `IActor`
- `spawner`
- regras de gameplay
- regras de Ã¡udio
- regras de UI
- clamp de posiÃ§Ã£o
- qualquer semÃ¢ntica de domÃ­nio

### Regra explÃ­cita

Nada de:

- `y = 0`
- correÃ§Ã£o de posiÃ§Ã£o hardcoded
- contexto de domÃ­nio embutido no core

Se um domÃ­nio precisar de contexto adicional, isso entra por camada externa, nunca pelo nÃºcleo do pooling.

---

## Contrato base de objeto pooled

O contrato base adotado Ã© simples e previsÃ­vel:

- `OnPoolCreated()`
- `OnPoolRent()`
- `OnPoolReturn()`
- `OnPoolDestroyed()`

A base opcional fornecida pelo mÃ³dulo Ã©:

- `IPoolableObject`
- `PooledBehaviour`

Isso preserva hooks Ãºteis de ciclo de vida sem contaminar o nÃºcleo.

---

## Runtime core

A infraestrutura runtime canÃ´nica inclui:

- `IPoolService`
- `PoolService`
- `GameObjectPool`
- `PoolRuntimeHost`
- `PoolRuntimeInstance`
- `PoolAutoReturnTracker`

### Responsabilidades atendidas

- registrar/resolver pools por `PoolDefinitionAsset`
- garantir prewarm
- realizar rent
- realizar return
- aplicar expansÃ£o controlada
- falhar explicitamente ao atingir o teto
- limpar corretamente no shutdown

---

## Prewarm, rent e return

As capacidades funcionais obrigatÃ³rias foram preservadas:

- prewarm do pool
- rent explÃ­cito
- return explÃ­cito
- reset do objeto retornado
- hooks de ciclo de vida

---

## Lifetime / auto-return

O auto-return por tempo Ã© **opcional**.

### SemÃ¢ntica adotada

- `autoReturnSeconds <= 0` â†’ desabilitado
- `autoReturnSeconds > 0` â†’ habilitado

Isso permite tanto usos com retorno manual quanto usos dirigidos apenas por tempo.

---

## ExpansÃ£o

A expansÃ£o opcional foi preservada com regra segura.

### Estrutura adotada

- `initialSize`
- `canExpand`
- `maxSize`

### ConsequÃªncia

NÃ£o existe crescimento indefinido sem teto explÃ­cito.

Quando o limite Ã© atingido, o sistema falha de forma explÃ­cita.

---

## Observabilidade

A infraestrutura possui logs observacionais coerentes com o canon.

Os eventos de observaÃ§Ã£o cobertos incluem:

- pool criado
- prewarm concluÃ­do
- rent
- return
- expansÃ£o
- falha de configuraÃ§Ã£o
- falha por limite
- shutdown / cleanup

---

## Scope global vs scene

A primeira versÃ£o canÃ´nica nasce com ownership **global** e persistente.

Isso Ã© suficiente para a versÃ£o standalone do mÃ³dulo.

Scene-aware pooling continua sendo uma extensÃ£o possÃ­vel, mas nÃ£o Ã© requisito para a completude desta decisÃ£o.

---

## EstratÃ©gia de validaÃ§Ã£o

A validaÃ§Ã£o do pooling canÃ´nico Ã© **standalone**.

### Regra explÃ­cita

O mÃ³dulo nÃ£o depende de Ã¡udio, gameplay, VFX ou qualquer domÃ­nio real para ser considerado vÃ¡lido.

### Forma de validaÃ§Ã£o adotada

A validaÃ§Ã£o foi feita com:

- mocks simples
- um objeto bem simples
- uma cena de testes controlada

Exemplos usados:

- cubo simples pooled
- componente mock herdando de `PooledBehaviour`
- controlador mock para:
  - ensure/register
  - prewarm
  - rent
  - return
  - stress atÃ© o limite
  - shutdown

### Resultado

A validaÃ§Ã£o standalone foi concluÃ­da com **PASS**.

Foram comprovados, de ponta a ponta:

- registro do pool
- prewarm
- rent
- return
- expansÃ£o atÃ© o teto
- falha explÃ­cita ao atingir o limite
- hooks de ciclo de vida
- cleanup correto

---

## RelaÃ§Ã£o com o legado

O pooling canÃ´nico serve como base para as capacidades que existiam no legado, mas sem copiar sua estrutura.

### Preservado conceitualmente

- prewarm
- rent / return
- auto-return opcional
- reset do objeto pooled
- expansÃ£o opcional

### Mantido como capacidade futura possÃ­vel sobre a base atual

- batch acquire
- scene-aware pooling
- tooling/editor
- adapters especializados de domÃ­nio

### NÃ£o preservado como estrutura

- `PersistentSingleton`
- `PoolManager` legado
- `LifetimeManager` legado como singleton estrutural
- identidade por `string`
- `IActor` / `spawner` no nÃºcleo
- `y = 0`
- `Resources.Load`
- reconfiguraÃ§Ã£o implÃ­cita por Ã­ndice
- lifetime obrigatÃ³rio

---

## ConsequÃªncias positivas

- pooling passou a pertencer ao canon de `NewScripts`
- a infraestrutura pode ser usada por mÃºltiplos mÃ³dulos sem duplicaÃ§Ã£o
- dependÃªncias legadas invÃ¡lidas foram eliminadas
- o nÃºcleo ficou genÃ©rico e desacoplado
- o mÃ³dulo pode ser validado isoladamente
- a base atual jÃ¡ suporta os comportamentos essenciais do legado

---

## ConsequÃªncias negativas / trade-offs

- exigiu reescrita, nÃ£o simples cÃ³pia
- a primeira versÃ£o nÃ£o replica literalmente todos os formatos operacionais do legado
- batch acquire e scene-aware continuam como extensÃµes
- a validaÃ§Ã£o inicial Ã© por mock, nÃ£o por um domÃ­nio final de produÃ§Ã£o

---

## CritÃ©rio de sucesso

A promoÃ§Ã£o do pooling ao canon Ã© considerada bem-sucedida porque hoje existe em `NewScripts` uma infraestrutura que:

- inicializa via `GlobalCompositionRoot`
- resolve pools por asset canÃ´nico, nÃ£o por string
- faz prewarm
- suporta rent / return
- suporta expansÃ£o opcional com teto
- suporta auto-return opcional
- faz cleanup correto no shutdown
- nÃ£o depende de singleton estrutural
- nÃ£o conhece domÃ­nio especÃ­fico no nÃºcleo
- nÃ£o altera posiÃ§Ã£o do mundo com regra hardcoded
- foi validada de ponta a ponta em uma cena de testes simples com mocks

---

## ConclusÃ£o

Esta decisÃ£o estÃ¡ **aceita, implementada e validada**.

O mÃ³dulo de pooling estÃ¡ fechado como infraestrutura canÃ´nica standalone de `NewScripts`.
