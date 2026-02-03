# Plano de conclusão: Levels/Phases (ADR-0017)

## 1) Direção do sistema
A intenção de existir um **manager de níveis/fases** é **correta** e está alinhada aos ADRs.

- **ADR-0017** (LevelManager / Config / Catalog): catálogo (**LevelCatalog**) + definições (**LevelDefinition**) como fonte de verdade; o manager é a API canônica para **resolver e aplicar** um nível.

Em outras palavras: **o “LevelManager” é a camada que traduz regra + config em um “plano aplicável”**.

## 2) Estado atual (com base nos arquivos enviados)
### 2.1 Componentes já existem (bom sinal)
- `ResourcesLevelCatalogProvider` (catálogo em Resources)
- `ILevelCatalogResolver` + `LevelCatalogResolver` (resolve nível inicial e retorna `LevelPlan`)
- `ILevelManagerService` + `LevelManagerService` (seleção/aplicação, integra com ContentSwap)
- `LevelQaInstaller` + `LevelQaContextMenu` (QA via ContextMenu)
- Assets: `LevelCatalog.asset`, `LevelDefinition_level.1.asset`, `LevelDefinition_level.2.asset`

Isso cobre a espinha dorsal do **ADR-0017**.

### 2.2 Sintoma reportado
No log:

- `[QA][LevelManager] ApplyLevel solicitado.`
- `[WARNING] [LevelManagerService] [LevelManager] ApplySelectedLevel ignorado (selection inválida).`

Isso indica que **nenhum nível aplicável foi resolvido/selecionado** (ou a resolução falhou).

## 3) Hipóteses do “selection inválida” (ordem de probabilidade)
1) **Definição não-resolvida** (ILevelDefinitionProvider não acha a `LevelDefinition` por `levelId`), então o `LevelCatalogResolver` não consegue compor `LevelPlan`.
2) **Catálogo não-carregado no runtime** (asset fora do caminho esperado em `Resources/NewScripts/Config/LevelCatalog`).
3) **QA chama Apply sem seleção** e o *auto-select* não está determinístico/robusto o bastante.

Observação: o seu `LevelCatalog.asset` contém `initialLevelId: level.1` e dois entries (`level.1`, `level.2`). Se a resolução falhar, a causa costuma estar no **provider de LevelDefinition** (caminho/nome/id) ou em regra de elegibilidade.

## 4) Plano de conclusão (incremental, sem regressão)

### Fase A — Congelar contrato (ADR-to-code alignment)
**Objetivo:** garantir que os conceitos e nomes nos ADRs correspondem 1:1 ao que o código faz.

**A1) Definir as invariantes mínimas (documentadas e logáveis)**
- `LevelCatalog` sempre resolve:
  - `initialLevelId` válido **ou** fallback determinístico.
- Um `LevelPlan` válido contém:
  - `levelId` (string canônica, ex.: `level.1`)
  - `contentId` (se houver ContentSwap)
  - `policy` (in-place vs requires-transition; por enquanto pode ser “in-place only”)

**A2) Ajuste de logs “assinatura-chave” (Baseline-friendly)**
Adicionar logs observáveis (prefixo `[OBS][Level]`), por exemplo:
- `LevelCatalogResolved ...`
- `LevelInitialResolved ...`
- `LevelSelected ...`
- `LevelApplyRequested ...`
- `LevelApplied ...`
- `LevelApplySkipped ... reason='...'`

> Aceitação da fase A: lendo um log, dá para explicar claramente “por que o Apply não aconteceu”.

---

### Fase B — Resolver seleção inicial e tornar Apply idempotente
**Objetivo:** `ApplySelectedLevel()` nunca falhar “em silêncio”; ou aplica, ou explica e oferece fallback.

**B1) Tornar seleção inicial determinística e explícita**
No `LevelManagerService.Initialize()`:
- Resolver catálogo e publicar log com lista de `levelIds`.
- Tentar resolver **plano inicial** via resolver.
- Se falhar:
  - fallback para primeiro nível do catálogo
  - se ainda falhar, logar erro + desabilitar manager (modo *safe no-op*).

**B2) `ApplySelectedLevel()` deve sempre logar o estado atual**
Antes de decidir “invalid selection”, logar:
- se catálogo foi carregado
- qual é o `selectedPlan` (se existe)
- resultado de `TryAutoSelectInitial`

**B3) Idempotência**
- Reaplicar o mesmo nível não deve explodir; deve produzir `ApplySkipped reason='AlreadyApplied'` ou `ApplyReapplied reason='...'` conforme decisão.

> Aceitação da fase B: o QA “Apply Level” nunca termina apenas em “selection inválida” sem revelar o motivo raiz.

---

### Fase C — Unificar “aplicar nível” com ContentSwap (in-place) e preparar evolução
**Objetivo:** aplicar nível hoje = **ContentSwap in-place** (sem transição). Amanhã podemos estender.

**C1) Definir explicitamente o modo atual: InPlaceOnly**
- Se o `LevelPlan` exigir algo além de in-place (ex.: load/unload), logar `ApplySkipped reason='RequiresTransition_NotImplemented'`.

**C2) Contrato com ContentSwap**
- `LevelDefinition.contentId` deve casar com o ContentSwap.
- Se `contentId` vazio/nulo:
  - logar e aplicar fallback (ex.: manter conteúdo atual) **ou** recusar com reason explícito.

**C3) Separar “seleção” de “aplicação”**
- Seleção muda estado interno.
- Aplicação executa side-effects (ContentSwap).

> Aceitação da fase C: aplicar `level.1` sempre leva ao `contentId` esperado, com log observável.

---

### Fase D — Limpeza estrutural (reduzir fragmentação)
**Objetivo:** manter um módulo “Levels” coeso e mínimo.

Checklist de limpeza:
- Um único namespace raiz para Levels (ex.: `_ImmersiveGames.NewScripts.Gameplay.Levels`).
- Providers/resolvers/manager agrupados por pasta:
  - `Levels/Config` (ScriptableObjects, providers)
  - `Levels/Runtime` (manager, resolver)
  - `Levels/QA` (installer, context menu)
- Remover duplicatas / variações “noop” que não são necessárias em produção (ou mantê-las claramente como fallback).

> Aceitação da fase D: módulo legível, sem “dois jeitos” de fazer a mesma coisa.

## 5) Evidência e critérios de Done
### Cenários mínimos (Baseline-friendly)
1) Boot → Menu: sem aplicar nível automaticamente (ou aplicar e logar como “frontend no-op” se esse for o contrato).
2) Menu → Gameplay:
   - resolver nível inicial
   - aplicar via ContentSwap in-place
   - logar `LevelApplied levelId='level.1' contentId='...' reason='Startup/Initial'`
3) QA:
   - SelectNext / SelectPrev
   - ApplySelected
   - erro controlado se nível/definição ausente

### Assinaturas-chave sugeridas
- `[OBS][Level] CatalogResolved ...`
- `[OBS][Level] InitialResolved ...`
- `[OBS][Level] Selected ...`
- `[OBS][Level] ApplyRequested ...`
- `[OBS][Level] Applied ...` ou `[OBS][Level] ApplySkipped ...`

## 6) Entradas que eu preciso para implementar sem regressão (quando formos codar)
Para fechar a causa do `selection inválida` e executar as fases acima com segurança, eu preciso dos arquivos *atuais* (sem dedução):

- `ILevelCatalogProvider` (se existir separado do provider)
- `ILevelDefinitionProvider` + implementação concreta
- Classes `LevelCatalog` e `LevelDefinition` (ScriptableObjects)
- Interfaces/implementações de ContentSwap usadas pelo LevelManager:
  - `IContentSwapContextService`
  - `IContentSwapChangeService`
- Qualquer “bridge” de DI/Bootstrap onde o LevelManager é registrado (o log sugere que existe)

> Sem esses arquivos, qualquer correção no fluxo de resolução/aplicação pode virar regressão por mismatch de assinatura.
