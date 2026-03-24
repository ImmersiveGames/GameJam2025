# 📊 ANÁLISE DO MÓDULO SCENERESET - REDUNDÂNCIAS INTERNAS E COESÃO DO RESET LOCAL

**Data:** 24 de março de 2026
**Projeto:** GameJam2025
**Módulo:** SceneReset
**Versão do Relatório:** 1.6
**Status:** ✅ Editado sobre a versão 1.5, preservando a estrutura do relatório e atualizado contra o código validado em runtime após o refactor de `SceneResetHookSourceResolver`

---

## 📋 ÍNDICE

1. [Resumo Executivo](#resumo-executivo)
2. [Estrutura do Módulo](#estrutura-do-módulo)
3. [Redundâncias Internas no SceneReset](#redundâncias-internas-no-scenereset)
4. [Cruzamento com WorldReset](#cruzamento-com-worldreset)
5. [Análise de Sobreposição](#análise-de-sobreposição)
6. [Recomendações de Consolidação](#recomendações-de-consolidação)
7. [Impacto Total Estimado](#impacto-total-estimado)
8. [Conclusão](#conclusão)

---

## 🎯 Resumo Executivo

### Descoberta Principal: **O RESOLVER DE FONTES DE HOOKS DEIXOU DE SER HOTSPOT PRINCIPAL E O PRÓXIMO REFINAMENTO NATURAL FICOU NO PIPELINE LOCAL**

A divisão estrutural continua correta: o reset local permanece claramente separado do macro reset e, após a consolidação validada em runtime, tanto o bloco de hooks quanto a factory de spawn deixaram de concentrar resolução, filtragem, ordenação, execução e validação em poucos tipos grandes.

**Hoje o `SceneReset` é claramente responsável por:**
- controller e serialização local
- runner/facade local
- pipeline local explícito
- fases do reset local
- hooks e infraestrutura de spawn local
- gate lease, catálogo de hooks, fila local e runtime factory em tipos próprios
- resolução/filtro/ordenação/execução de hooks em helpers explícitos
- resolução de dependências e validação de spawn local em helpers explícitos

**Estatísticas atuais (estado validado):**
- `SceneResetContext.cs`: ~240 linhas
- `SceneResetController.cs`: ~199 linhas
- `SceneResetHookCatalog.cs`: ~136 linhas
- `SceneResetHookRunner.cs`: ~105 linhas
- `SceneResetHookSourceResolver.cs`: ~160 linhas
- `SceneResetHookExecution.cs`: ~137 linhas
- `SceneResetRequestQueue.cs`: ~154 linhas
- `SceneResetRuntimeFactory.cs`: ~130 linhas
- `WorldSpawnServiceFactory.cs`: ~70 linhas
- `WorldSpawnFactoryDependenciesResolver.cs`: ~90 linhas
- `WorldSpawnEntryValidator.cs`: ~85 linhas

**Leitura atual:**
- a arquitetura do módulo está mais limpa do que no relatório anterior
- `SceneResetContext` e `SceneResetController` continuam fora da zona crítica
- `SceneResetHookCatalog` e `SceneResetHookRunner` deixaram de concentrar tudo sozinhos
- `WorldSpawnServiceFactory` também deixou de ser hotspot principal após a extração de resolução e validação
- o peso remanescente deixou de ficar concentrado no `SceneResetHookSourceResolver`
- o próximo ponto mais natural do módulo passa a ser o **pipeline local**, com refinamento secundário ainda possível no bloco de scene hooks registrados
- o boundary com `WorldReset` continua saudável
- a validação por runtime foi forte para fluxo macro/local, restart, exit-to-menu, actor hooks no restart, registro de serviços de spawn local e manutenção do comportamento após o refactor do `SceneResetHookSourceResolver`; ainda não é, porém, um teste de alta carga de scene hooks registrados

---

## 📁 Estrutura do Módulo

```text
SceneReset/
├── Bindings/
│   ├── SceneResetController.cs (~199)
│   ├── SceneResetRunner.cs (~102)
│   ├── SceneResetRequestQueue.cs (~154)
│   └── SceneResetRuntimeFactory.cs (~130)
├── Hooks/
│   ├── ISceneResetHook.cs
│   ├── ISceneResetHookOrdered.cs
│   ├── SceneResetHookBase.cs
│   └── SceneResetHookRegistry.cs
├── Runtime/
│   ├── ISceneResetPhase.cs
│   ├── SceneResetContext.cs (~240)
│   ├── SceneResetControllerLocator.cs (~132)
│   ├── SceneResetFacade.cs (~98)
│   ├── SceneResetGateLease.cs (~67)
│   ├── SceneResetHookCatalog.cs (~136)
│   ├── SceneResetHookSourceResolver.cs (~160)
│   ├── SceneResetHookScopeFilter.cs (~99)
│   ├── SceneResetHookOrdering.cs (~34)
│   ├── SceneResetHookRunner.cs (~105)
│   ├── SceneResetHookExecution.cs (~137)
│   ├── SceneResetPipeline.cs (~82)
│   └── Phases/
│       ├── AcquireResetGatePhase.cs
│       ├── BeforeDespawnHooksPhase.cs
│       ├── DespawnPhase.cs
│       ├── AfterDespawnHooksPhase.cs
│       ├── ScopedParticipantsResetPhase.cs (~108)
│       ├── SpawnPhase.cs
│       └── AfterSpawnHooksPhase.cs
└── Spawn/
    ├── IWorldSpawnContext.cs
    ├── IWorldSpawnService.cs
    ├── IWorldSpawnServiceRegistry.cs
    ├── WorldSpawnContext.cs
    ├── WorldSpawnServiceFactory.cs (~70)
    ├── WorldSpawnFactoryDependenciesResolver.cs (~90)
    ├── WorldSpawnEntryValidator.cs (~85)
    └── WorldSpawnServiceRegistry.cs
```

**Observação atualizada:**
A estrutura do módulo agora está mais coerente com as responsabilidades reais. O antigo acúmulo no subsistema de hooks foi redistribuído para tipos auxiliares próprios, o bloco de spawn local passou pela mesma direção com extrações explícitas para resolução de dependências e validação de entries, e o refactor do `SceneResetHookSourceResolver` não introduziu regressão funcional no fluxo validado. O log mais recente reforça que o trilho de actor hooks continua funcional no restart e que o registro dos serviços locais de spawn permanece saudável.

---

## 🔴 Redundâncias Internas no SceneReset

### 1️⃣ `SceneResetContext` segue importante, mas fora do grupo mais grave

**Problema anterior:**
O contexto concentrava estado do ciclo, gate, resolução de hooks, cache de actor hooks, coleta de scoped participants e parte da observabilidade operacional.

**Estado atual:**
Essa concentração já tinha sido reduzida e continua estável:
- gate lifecycle em `SceneResetGateLease`
- coleta/cache de hooks e scoped participants fora do núcleo central
- o contexto ficou como estado do ciclo + delegação operacional

**Impacto:**
- o `Context` permanece relevante
- porém já não é o melhor ponto para atacar o módulo
- reabrir esse arquivo sem nova evidência tende a gerar churn, não ganho estrutural

**Severidade:** 🟡 **MÉDIA-BAIXA**

---

### 2️⃣ `SceneResetController` segue central, mas não é mais hotspot principal

**Problema anterior:**
O controller concentrava lifecycle, fila, composição local, validação de dependências e cleanup operacional.

**Estado atual:**
A situação continua melhor:
- fila sequencial em `SceneResetRequestQueue`
- montagem/validação/cleanup runtime em `SceneResetRuntimeFactory`
- o controller ficou bem mais próximo de entrypoint + lifecycle + delegação

**Impacto:**
- continua sendo tipo central do módulo
- mas já não é o ponto mais denso nem o mais arriscado
- o runtime validado não mostrou regressão nesse trilho, inclusive no ciclo completo de restart

**Severidade:** 🟡 **MÉDIA-BAIXA**

---

### 3️⃣ `SceneResetHookCatalog` deixou de ser hotspot concentrador

**Problema anterior:**
O catálogo concentrava coleta de world hooks, cache de actor hooks, resolução por escopo, coleta e ordenação de scoped participants, além de critérios auxiliares de filtragem/ordenação.

**Estado atual:**
A densidade caiu de forma real:
- filtro por escopo foi extraído para `SceneResetHookScopeFilter`
- ordenação foi extraída para `SceneResetHookOrdering`
- resolução/coleta de fontes foi extraída para `SceneResetHookSourceResolver`
- o catálogo ficou muito mais focado em cache/coleta/delegação

**Impacto:**
- o arquivo deixou de ser o principal hotspot do módulo
- a responsabilidade ficou mais explícita
- a leitura melhorou sem reespalhar lógica para `Context` ou `Controller`

**Severidade:** 🟡 **MÉDIA**

---

### 4️⃣ `SceneResetHookRunner` também deixou de ser hotspot concentrador

**Problema anterior:**
O runner concentrava execução detalhada, ordenação e telemetria dos hooks do pipeline local.

**Estado atual:**
A densidade também caiu:
- execução detalhada foi extraída para `SceneResetHookExecution`
- o runner ficou mais próximo de orquestração da etapa e delegação da execução

**Impacto:**
- o arquivo ficou muito mais compatível com o papel de runner
- o peso operacional não sumiu, mas ficou deslocado para helpers mais claros
- isso reduz risco de churn em alterações futuras do pipeline local

**Severidade:** 🟡 **MÉDIA-BAIXA**

---

### 5️⃣ `SceneResetHookSourceResolver` deixa de ser hotspot principal e passa a refinamento localizado

**Problema anterior:**
Com a consolidação anterior, o maior peso do subsistema de hooks havia migrado para a resolução de fontes:
- coleta de hooks de mundo
- descoberta de fontes registradas
- composição de listas vindas de múltiplas origens
- parte da normalização operacional do catálogo

**Estado atual:**
Após o refactor validado em runtime:
- a responsabilidade do resolver ficou mais explícita
- o comportamento funcional do fluxo não regrediu
- o peso deixou de ser o principal hotspot do módulo
- ainda não houve, porém, teste de alta carga de scene hooks registrados

**Impacto:**
- o arquivo passa a refinamento localizado
- o bloco de hooks fica mais equilibrado do que nas versões anteriores
- o próximo ganho real deixa de estar neste ponto e migra para o pipeline local

**Severidade:** 🟢 **BAIXA**

---

### 6️⃣ Spawn local deixou de ser hotspot principal e passa a refinamento secundário

**Problema anterior:**
`WorldSpawnServiceFactory` concentrava validação de entry, resolução de dependências globais, branching por kind e construção concreta dos serviços de spawn local.

**Estado atual:**
A densidade caiu de forma real:
- resolução de dependências foi extraída para `WorldSpawnFactoryDependenciesResolver`
- validação de entry por kind foi extraída para `WorldSpawnEntryValidator`
- a factory ficou mais próxima de composition point do bloco de spawn
- o contrato externo foi preservado com `Create(...)` retornando `null` em falha para manter compatibilidade com `SceneScopeCompositionRoot`

**Impacto:**
- o bloco de spawn continua relevante, mas já não é o próximo hotspot principal do módulo
- o runtime validado não mostrou regressão no registro de `PlayerSpawnService` e `EaterSpawnService`
- reabrir esse bloco agora sem nova evidência tende a ter ROI menor que atacar `SceneResetHookSourceResolver`

**Severidade:** 🟢 **BAIXA**

---

## 🔄 Cruzamento com WorldReset

A fronteira atual continua correta:
- `WorldReset` decide macro
- `SceneReset` executa local

No estado validado em runtime, o cruzamento permanece saudável:
- `WorldReset` não invadiu o pipeline local
- `SceneReset` não assumiu decisão macro nem pós-condição macro
- a consolidação do bloco de hooks não regrediu a separação entre macro reset, bridge e reset local
- o refactor do bloco de spawn local também não reabriu dependência indevida com o macro reset

**Leitura atualizada:** o que sobra aqui continua sendo acoplamento natural entre owner macro e executor local, e não sobreposição estrutural problemática.

---

## 📊 Análise de Sobreposição

| Área | SceneReset | WorldReset | Situação |
|---|---|---|---|
| Pipeline local determinístico | ✅ | ❌ | Correto em `SceneReset` |
| Queue / lifecycle local | ✅ | ❌ | Correto em `SceneReset` |
| Decisão macro de reset | ❌ | ✅ | Correto em `WorldReset` |
| Pós-condição macro | ❌ | ✅ | Correto em `WorldReset` |
| Catálogo / filtro / ordenação / execução de hooks | ✅ | ❌ | Correto em `SceneReset`, agora melhor distribuído |
| Spawn local | ✅ | ⚠️ | Correto em `SceneReset`, agora mais coeso e secundário |
| Gate / bridge com SceneFlow | ❌ | ❌ | Correto fora de `SceneReset` |

---

## 🛠️ Recomendações de Consolidação

### Prioridade 1
Revisar `SceneResetPipeline` apenas se ainda houver ganho real.

**Leitura atualizada:** depois da consolidação do bloco de hooks e da validação em runtime, o pipeline local passa a ser o próximo ponto natural de refinamento. Ainda assim, ele está saudável e não deve ser reaberto sem objetivo claro.

### Prioridade 2
Só reabrir o bloco de scene hooks registrados se houver nova evidência concreta.

**Leitura atualizada:** o `SceneResetHookSourceResolver` deixou de ser hotspot principal, mas a cobertura de runtime ainda não foi um teste de alta carga de scene hooks registrados. Sem essa evidência, reentrar agora tende a gerar churn.

### Prioridade 3
Só reabrir o bloco de spawn local se houver nova evidência concreta.

**Leitura atualizada:** `WorldSpawnServiceFactory` deixou de ser hotspot principal após a extração de resolução e validação. Sem nova evidência, reentrar agora tende a gerar churn.

### Prioridade 4
Só reabrir `HookCatalog`, `HookRunner`, `Context` ou `Controller` se houver nova evidência concreta.

**Leitura atualizada:** esses arquivos melhoraram de verdade e o runtime validado não mostrou regressão funcional; reentrar neles agora tende a piorar custo/benefício.

---

## 📈 Impacto Total Estimado

- Redundância/overlap interna: **2-5%**
- Hotspot principal atual: `SceneResetPipeline`
- Hotspot secundário: **cobertura ainda limitada de scene hooks registrados**
- `SceneResetHookSourceResolver`: **reduzido de criticidade e validado em runtime após refactor**
- `WorldSpawnServiceFactory`: **reduzido de criticidade e validado em runtime após refactor**
- `SceneResetContext` e `SceneResetController`: **reduzidos de criticidade e estáveis após validação em runtime**
- `SceneResetHookCatalog` e `SceneResetHookRunner`: **reduzidos de criticidade após consolidação do bloco de hooks**
- Complexidade atual: **baixa a média-baixa**, mais explícita, validada em boot → gameplay → restart → exit-to-menu e melhor distribuída do que nas versões anteriores

---

## ✅ Conclusão

O `SceneReset` continua sendo um dos ganhos estruturais mais claros da divisão atual.
Depois das consolidações validadas em runtime, o módulo ficou mais coeso e os hotspots mais graves deixaram de se concentrar tanto nos entrypoints centrais quanto no par `HookCatalog + HookRunner`, no bloco principal de spawn local e no resolver de fontes de hooks.

O problema principal agora não é mais concentração excessiva no núcleo do reset local, no runner principal de hooks, na factory principal de spawn ou no resolver de fontes, e sim **refinamento possível no pipeline local** e, em segundo plano, **ainda pouca cobertura de alta carga para scene hooks registrados**.

**Resumo atualizado:** módulo correto, pipeline correto, boundary correto; os hotspots mais graves foram reduzidos, o bloco de hooks ficou melhor distribuído, o trilho de actor hooks foi revalidado em runtime no restart, o spawn local foi revalidado sem regressão, o `SceneResetHookSourceResolver` foi reduzido sem quebrar o fluxo e o próximo refinamento natural passa a estar no `SceneResetPipeline`.
