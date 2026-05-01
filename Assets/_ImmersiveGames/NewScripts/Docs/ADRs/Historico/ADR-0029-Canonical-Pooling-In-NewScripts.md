# ADR-0029 – Canonical Pooling in NewScripts

- **Status:** Accepted
- **Implementation Status:** Complete
- **Validation Status:** PASS
- **Date:** 2026-03-14
- **Last Validated:** 2026-03-14
- **Scope:** `Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/**`

> **Nota de rollout (2026-03-20):** Este ADR permanece aceito como contrato arquitetural. No snapshot atual do repositório, o módulo canônico precisou de reimplementação incremental; o estado real de rollout está rastreado em `Docs/Reports/Audits/2026-03-20/ADR-0029-Pooling-Rollout-Tracker.md`. O status histórico "Complete/PASS" não deve ser lido, isoladamente, como garantia de módulo operacional já presente neste snapshot.
>
> **Nota de fechamento operacional (2026-03-20):** O rollout incremental iniciado em 2026-03-20 foi concluído. A implementação canônica atual está disponível em `Infrastructure/Pooling/**`, com rastreabilidade operacional no tracker de 2026-03-20, e o módulo está pronto para consumo por outros módulos sem bootstrap alternativo.

---

## Contexto

O projeto consolidou um canon em `NewScripts` baseado em:

- `GlobalCompositionRoot` como owner da inicialização global
- direct-ref + fail-fast
- eliminação de bootstrap paralelo, singleton estrutural e fallback frouxo
- separação entre infraestrutura compartilhada e módulos de domínio
- integração explícita com serviços globais via DI

No legado existia uma infraestrutura de pooling funcional com capacidades úteis:

- prewarm
- rent / return explícitos
- reset do objeto pooled
- expansão opcional
- auto-return por lifetime
- batch acquire

Essa base tinha valor real, mas não podia ser promovida diretamente ao canon porque trazia decisões incompatíveis com `NewScripts`, como:

- ownership por `PersistentSingleton`
- identidade por `string`
- acoplamento do núcleo com domínio (`IActor`, `spawner`)
- regra hardcoded de posicionamento (`y = 0`)
- lifetime obrigatório
- expansão sem teto explícito
- reconfiguração implícita
- mistura entre infraestrutura compartilhada e detalhes de domínio

---

## Problema

O projeto precisava de uma infraestrutura canônica de pooling que:

- preservasse o valor funcional do legado
- eliminasse acoplamentos incompatíveis com o canon atual
- fosse genérica o suficiente para servir múltiplos domínios
- fosse ownership de infraestrutura compartilhada, não de um módulo específico
- pudesse ser validada de forma standalone, sem depender de um módulo consumidor real

---

## Drivers

- aderência ao canon atual de `NewScripts`
- pooling como infraestrutura compartilhada do projeto
- fail-fast em configuração inválida
- ownership claro no `GlobalCompositionRoot`
- eliminação de singleton estrutural
- eliminação de `string` como identidade principal de pool
- separação entre núcleo genérico e domínio
- preservação de prewarm, rent/return e auto-return opcional
- capacidade de servir como base para os usos existentes no legado
- evitar regressão funcional

---

## Decisão

Foi criada uma infraestrutura canônica de pooling em:

`Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/**`

Essa infraestrutura é tratada como **infraestrutura compartilhada** do projeto.

### Consequência direta

Nenhum módulo de domínio deve criar seu próprio "pool manager estrutural" paralelo.

O pooling canônico existe e é considerado válido por si só, independentemente de qual módulo venha a consumi-lo depois.

---

## Estrutura adotada

- `Infrastructure/Pooling/Contracts/**`
- `Infrastructure/Pooling/Config/**`
- `Infrastructure/Pooling/Runtime/**`

---

## Ownership

A inicialização e o ownership da infraestrutura de pooling pertencem ao `GlobalCompositionRoot`.

Não entram no canon:

- `PersistentSingleton`
- `RuntimeInitializeOnLoadMethod` como owner estrutural
- bootstrap estático paralelo
- `Resources.Load`
- singletons privados por módulo como base do pooling

O pooling canônico sobe como serviço global registrado no DI.

---

## Identidade dos pools

A identidade estrutural de um pool é feita por **referência direta de asset**, não por `string`.

### Consequências

- não usar `ObjectName` string como chave principal
- não usar nome de prefab como identidade estrutural
- a definição canônica do pool é um asset próprio

---

## Asset canônico de definição

A definição canônica adotada é:

**`PoolDefinitionAsset`**

Campos da versão atual:

- `prefab`
- `initialSize`
- `canExpand`
- `maxSize`
- `autoReturnSeconds`
- `poolLabel`

### Regras

- um prefab por pool
- `poolLabel` serve para observabilidade, não como identidade estrutural
- `autoReturnSeconds <= 0` significa sem retorno automático
- `canExpand = false` limita o pool ao `initialSize`
- `canExpand = true` respeita `maxSize` como teto

---

## Núcleo genérico

O núcleo do pooling é genérico e não conhece domínio.

### Não entram no núcleo

- `IActor`
- `spawner`
- regras de gameplay
- regras de áudio
- regras de UI
- clamp de posição
- qualquer semântica de domínio

### Regra explícita

Nada de:

- `y = 0`
- correção de posição hardcoded
- contexto de domínio embutido no core

Se um domínio precisar de contexto adicional, isso entra por camada externa, nunca pelo núcleo do pooling.

---

## Contrato base de objeto pooled

O contrato base adotado é simples e previsível:

- `OnPoolCreated()`
- `OnPoolRent()`
- `OnPoolReturn()`
- `OnPoolDestroyed()`

A base opcional fornecida pelo módulo é:

- `IPoolableObject`
- `PooledBehaviour`

Isso preserva hooks úteis de ciclo de vida sem contaminar o núcleo.

---

## Runtime core

A infraestrutura runtime canônica inclui:

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
- aplicar expansão controlada
- falhar explicitamente ao atingir o teto
- limpar corretamente no shutdown

---

## Prewarm, rent e return

As capacidades funcionais obrigatórias foram preservadas:

- prewarm do pool
- rent explícito
- return explícito
- reset do objeto retornado
- hooks de ciclo de vida

---

## Lifetime / auto-return

O auto-return por tempo é **opcional**.

### Semântica adotada

- `autoReturnSeconds <= 0` → desabilitado
- `autoReturnSeconds > 0` → habilitado

Isso permite tanto usos com retorno manual quanto usos dirigidos apenas por tempo.

---

## Expansão

A expansão opcional foi preservada com regra segura.

### Estrutura adotada

- `initialSize`
- `canExpand`
- `maxSize`

### Consequência

Não existe crescimento indefinido sem teto explícito.

Quando o limite é atingido, o sistema falha de forma explícita.

---

## Observabilidade

A infraestrutura possui logs observacionais coerentes com o canon.

Os eventos de observação cobertos incluem:

- pool criado
- prewarm concluído
- rent
- return
- expansão
- falha de configuração
- falha por limite
- shutdown / cleanup

---

## Scope global vs scene

A primeira versão canônica nasce com ownership **global** e persistente.

Isso é suficiente para a versão standalone do módulo.

Scene-aware pooling continua sendo uma extensão possível, mas não é requisito para a completude desta decisão.

---

## Estratégia de validação

A validação do pooling canônico é **standalone**.

### Regra explícita

O módulo não depende de áudio, gameplay, VFX ou qualquer domínio real para ser considerado válido.

### Forma de validação adotada

A validação foi feita com:

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
  - stress até o limite
  - shutdown

### Resultado

A validação standalone foi concluída com **PASS**.

Foram comprovados, de ponta a ponta:

- registro do pool
- prewarm
- rent
- return
- expansão até o teto
- falha explícita ao atingir o limite
- hooks de ciclo de vida
- cleanup correto

---

## Relação com o legado

O pooling canônico serve como base para as capacidades que existiam no legado, mas sem copiar sua estrutura.

### Preservado conceitualmente

- prewarm
- rent / return
- auto-return opcional
- reset do objeto pooled
- expansão opcional

### Mantido como capacidade futura possível sobre a base atual

- batch acquire
- scene-aware pooling
- tooling/editor
- adapters especializados de domínio

### Não preservado como estrutura

- `PersistentSingleton`
- `PoolManager` legado
- `LifetimeManager` legado como singleton estrutural
- identidade por `string`
- `IActor` / `spawner` no núcleo
- `y = 0`
- `Resources.Load`
- reconfiguração implícita por índice
- lifetime obrigatório

---

## Consequências positivas

- pooling passou a pertencer ao canon de `NewScripts`
- a infraestrutura pode ser usada por múltiplos módulos sem duplicação
- dependências legadas inválidas foram eliminadas
- o núcleo ficou genérico e desacoplado
- o módulo pode ser validado isoladamente
- a base atual já suporta os comportamentos essenciais do legado

---

## Consequências negativas / trade-offs

- exigiu reescrita, não simples cópia
- a primeira versão não replica literalmente todos os formatos operacionais do legado
- batch acquire e scene-aware continuam como extensões
- a validação inicial é por mock, não por um domínio final de produção

---

## Critério de sucesso

A promoção do pooling ao canon é considerada bem-sucedida porque hoje existe em `NewScripts` uma infraestrutura que:

- inicializa via `GlobalCompositionRoot`
- resolve pools por asset canônico, não por string
- faz prewarm
- suporta rent / return
- suporta expansão opcional com teto
- suporta auto-return opcional
- faz cleanup correto no shutdown
- não depende de singleton estrutural
- não conhece domínio específico no núcleo
- não altera posição do mundo com regra hardcoded
- foi validada de ponta a ponta em uma cena de testes simples com mocks

---

## Conclusão

Esta decisão está **aceita, implementada e validada**.

O módulo de pooling está fechado como infraestrutura canônica standalone de `NewScripts`.

