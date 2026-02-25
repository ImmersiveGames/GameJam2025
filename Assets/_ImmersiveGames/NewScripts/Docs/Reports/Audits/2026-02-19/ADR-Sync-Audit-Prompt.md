# Prompt — ADR Sync Audit (NewScripts)

> **Uso:** este prompt é para o CODEX executar **apenas leitura** do repositório e produzir um relatório. **Não modificar código.**

## Objetivo
Auditar se a implementação em `Assets/_ImmersiveGames/NewScripts/` está **alinhada** com os ADRs `ADR-0009..ADR-0017`.

> Nota: `ADR-0013` é considerado **Aprovado / Implementado** (estado documental atual). Divergências só devem ser apontadas quando houver quebra objetiva do contrato (gates, ordem de fase, ownership/publicação de eventos), não por detalhes internos não especificados.

A auditoria deve avaliar dois eixos:

1) **Sincronia ADR ↔ código**: o que o ADR estabelece existe no código? está no lugar correto? respeita nomes/contratos (DI, eventos, gates, ordem de fluxo)?
2) **Completude ideal de produção**: mesmo que o ADR “passe”, a solução é completa para produção? (Strict vs Release, modo degradado explícito, invariants verificáveis, observabilidade estável, ausência de fallback silencioso).

> Referências canônicas dentro do repo:
> - `Docs/Standards/Standards.md#politica-strict-vs-release`
> - `Docs/Standards/Standards.md#observability-contract`
> - `Docs/Reports/Evidence/` (evidências arquivadas)
> - `Docs/Reports/LATEST.md` (última evidência canônica)

## Regras de execução (importantes)
- Somente leitura: **não editar, não formatar, não “corrigir”** arquivos.
- Não inferir conteúdos de arquivos ausentes. Se algo não existir: marcar como `NOT_FOUND`.
- Sempre citar **caminho + número de linha** quando afirmar algo do código.
- Se uma parte for ambígua, registrar como `UNCLEAR` e listar o que faltou para provar.

## Política: Strict/Release e fallback
O projeto usa uma política explícita:
- **STRICT (Dev/QA)**: faltas críticas devem falhar (throw/assert), ou interromper fluxo de forma determinística.
- **RELEASE**: pode haver modo degradado, mas ele deve ser **explícito**, observável e rastreável (logs âncora).

A auditoria deve tratar como divergência qualquer **fallback silencioso** para itens considerados críticos pelos ADRs.

> Observação: em Unity, “criar coisas em voo” como fallback pode quebrar continuidade/estado. Portanto, a auditoria deve preferir **fail-fast** em Strict e apenas permitir degradação em Release quando existir configuração e log âncora.

## Entregáveis
Produzir um relatório Markdown com:

### 1) Sumário executivo
- Top 10 divergências por impacto (produção, determinismo, risco de regressão).
- Top 5 pontos “prontos para promoção” (para não virar relatório só de falhas).

### 2) Tabela por ADR (0009–0017)
Colunas mínimas:
- **ADR**
- **Status**: `ALINHADO | PARCIAL | DIVERGENTE | NOT_FOUND | UNCLEAR`
- **Implementação encontrada** (arquivos relevantes)
- **Gaps** (o que falta para o “ideal de produção”)
- **Evidência** (paths/trechos com linhas)

### 3) Auditoria de invariants (Checklist A–F)
Usar os itens A–F como checklist transversal:
A) Fade/LoadingHUD (Strict + Release + degraded mode)  
B) WorldDefinition (Strict + mínimo spawn)  
C) LevelCatalog (Strict + Release)  
D) PostGame (Strict + Release)  
E) Ordem do fluxo (RequestStart após IntroStageComplete)  
F) Gates (ContentSwap respeita scene_transition e sim.gameplay)

Para cada item: `PASS | FAIL | UNCLEAR` + evidência.

### 4) Observabilidade
- Verificar se logs seguem `Docs/Standards/Standards.md#observability-contract`.
- Listar “anchors” ausentes ou inconsistentes.

### 5) Lista de ações sugeridas (sem código)
- Lista priorizada de ações (“o que mudar”) por impacto/risco.
- Cada ação deve apontar qual ADR e qual item do checklist ela destrava.

## Escopo de busca
- Código: `Assets/_ImmersiveGames/NewScripts/`
- Documentação citada pelos ADRs (se existir)
- Não analisar outras pastas, exceto se o ADR explicitamente referencia e elas existirem.

## Comandos sugeridos (não exaustivo)
Use ferramentas de inspeção local. Exemplos:

- `rg -n "ADR-0009|ADR-0010|ADR-0011|ADR-0012|ADR-0013|ADR-0014|ADR-0015|ADR-0016|ADR-0017" Assets/_ImmersiveGames/NewScripts`
- `rg -n "Fade|FadeService|LoadingHud|LoadingHUD|WorldDefinition|LevelCatalog|ContentSwap|IntroStage|RequestStart" Assets/_ImmersiveGames/NewScripts`
- `rg -n "STRICT|Strict|fail-fast|FailFast|DEGRADED|degraded|assert|throw|InvalidOperationException" Assets/_ImmersiveGames/NewScripts`
- `rg -n "flow.scene_transition|sim.gameplay|state.postgame|state.pause" Assets/_ImmersiveGames/NewScripts`
- `find Assets/_ImmersiveGames/NewScripts/Docs -maxdepth 4 -type f`

## Foco por ADR (quick map)
- ADR-0009: Fade + SceneFlow (serviço, adapter, ordem, fail-fast)
- ADR-0010: Loading HUD + SceneFlow (cena additive, controller, fail-fast)
- ADR-0011: WorldDefinition (mínimo de spawn em gameplay)
- ADR-0012: PostGame (ownership, gates, input mode, idempotência)
- ADR-0013: Ciclo de vida (SceneFlow ↔ WorldLifecycle ↔ GameLoop, ordem e gates)
- ADR-0014: Reset targets/grupos (validação e consistência)
- ADR-0015: Baseline 2.0 (evidência canônica e invariants)
- ADR-0016: ContentSwap in-place (respeito a gates)
- ADR-0017: LevelCatalog/LevelManager (fail-fast e resolução por ID)

## Formato de saída
O relatório final deve ficar em:
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/YYYY-MM-DD/ADR-Sync-Audit-NewScripts.md`

Usar `YYYY-MM-DD` da execução.


## Regra adicional — Evidência

- Manter **1 arquivo de evidência por dia** em `Docs/Reports/Evidence/<data>/Baseline-2.2-Evidence-YYYY-MM-DD.md`. Se houver anexos/patches/excerpts adicionais, **mesclar** no snapshot do dia e remover os arquivos extras.
