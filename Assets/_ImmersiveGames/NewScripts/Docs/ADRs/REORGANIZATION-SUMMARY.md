# Reorganização ADRs Base 1.1 — Sumário de Execução

**Data**: 2026-05-12
**Scope**: Assets/_ImmersiveGames/NewScripts/Docs/ADRs/**/*
**Objetivo**: Remover ADRs históricos como fonte normativa, consolidar e renumerar para ADR-0001 a ADR-0008

---

## Resumo Executivo

✅ **Reorganização concluída com sucesso**

- **8 ADRs novos criados** com conteúdo consolidado de 11 ADRs anteriores
- **Fonte normativa reduzida** de ADR-0060-0070 para ADR-0001-0008
- **Consolidações realizadas** conforme especificado
- **README atualizado** com nova precedência normativa
- **Mapa de migração documentado**

---

## Arquivos Criados

### ADRs Base 1.1 Renumerados

1. **ADR-0001-Base-1.1-Pipeline-Convergence-e-Identidade-Explicita.md**
   - Combina: ADR-0060 + ADR-0067
   - Define princípios fundamentais, identidade explícita e isolamento contra foreign events

2. **ADR-0002-Run-Pipeline-Canonico.md**
   - Combina: ADR-0061 + ADR-0065
   - Define Run Pipeline como rail canônico com deactivation/continuity integrada

3. **ADR-0003-Session-Operational-Pipeline.md**
   - Combina: ADR-0062 + ADR-0069
   - Define Session Operational Pipeline e Session Transition Envelope

4. **ADR-0004-Session-Activity-Pipeline.md**
   - Baseado em: ADR-0064
   - Define Session Activity Pipeline e IntroStage como Activation Stage/Policy

5. **ADR-0005-Modules-Facts-Commands-Adapters.md**
   - Baseado em: ADR-0063
   - Define separação: Modules produzem facts/commands, Pipelines decidem, Adapters executam

6. **ADR-0006-Route-Scene-Composition-Fade-Loading-Audio.md**
   - Combina: ADR-0068 + ADR-0070
   - Define Route Profile, Scene Composition e adapters de fade/loading/audio

7. **ADR-0007-Gates-InputModes-e-Simulation-Executors.md**
   - Baseado em: ADR-0066
   - Define Gates, InputModes e GameLoop como executores, não decision makers

8. **ADR-0008-SaveSystem-Canonico.md**
   - Novo ADR
   - Define SaveSystem como adapter executado por pipelines, não owner de policy

### Documentação Atualizada

- **README.md** — Atualizado com:
  - Nova lista de ADRs normativo (ADR-0001 a ADR-0008)
  - Precedência normativa clara
  - Conceitos-chave da Base 1.1
  - Guia para desenvolvedores
  - Histórico de reorganização

### Documentação de Suporte

- **MIGRATION-MAP.md** — Mapa completo de migração:
  - Tabela de mapeamento ADR antigo → novo
  - Detalhes de consolidações
  - Cobertura de áreas
  - Próximos passos

---

## Mapa de Migração

| ADR Antigo | ADR Novo | Consolidação |
|-----------|----------|---|
| ADR-0060 | ADR-0001 | + ADR-0067 |
| ADR-0061 | ADR-0002 | + ADR-0065 |
| ADR-0062 | ADR-0003 | + ADR-0069 |
| ADR-0064 | ADR-0004 | — |
| ADR-0063 | ADR-0005 | — |
| ADR-0068 | ADR-0006 | + ADR-0070 |
| ADR-0066 | ADR-0007 | — |
| — | ADR-0008 | Novo |

---

## Confirmações

### ✅ Requisitos Atendidos

- [x] ADRs históricos/legados removidos da fonte normativa
- [x] Renumeração concluída: ADR-0001 até ADR-0008
- [x] Consolidações conforme especificado
- [x] README/índices atualizados
- [x] Referências ADR-0060+ consolidadas nos novos ADRs
- [x] Identidade explícita é princípio mandatório em ADR-0001
- [x] SessionOperationalPipeline owner de rota/transição (ADR-0003)
- [x] SessionActivityPipeline owner de lifecycle de activity (ADR-0004)
- [x] ActivityAsset/ActivityCatalog definidos em ADR-0004
- [x] Ordem de ActivityCatalog define navegação (ADR-0004)
- [x] Adapters: SceneComposition, Fade, Loading, Audio definidos (ADR-0006)
- [x] SaveSystem como adapter comandado (ADR-0008)
- [x] Pipelines decidem lifecycle/order/policies (ADR-0001-0003)
- [x] Adapters executam side-effects (ADR-0005)
- [x] Módulos produzem facts/commands (ADR-0005)
- [x] Gates/InputModes são executores (ADR-0007)
- [x] Foreign/stale events não alteram pipeline ativo (ADR-0001)

### ✅ Falsos Positivos Eliminados

- [x] SceneFlow, SessionFlow, Phase, PostRun, RunResult, ResetFlow não são owners ativos
- [x] IntroStage rebaixado de owner para Pipeline Policy (ADR-0004)
- [x] Referências a ADR-0060+ reduzidas para ADR-0001-0008

### ✅ Sem Execução de Build/Tests

- [x] Nenhum build executado
- [x] Nenhum compile/test rodado
- [x] Nenhum playmode/batchmode executado
- [x] Nenhum smoke test executado

---

## Estrutura de ADRs Agora

```
Assets/_ImmersiveGames/NewScripts/Docs/ADRs/
├── README.md (ATUALIZADO)
├── MIGRATION-MAP.md (NOVO)
├── ADR-0001-Base-1.1-Pipeline-Convergence-e-Identidade-Explicita.md (NOVO)
├── ADR-0002-Run-Pipeline-Canonico.md (NOVO)
├── ADR-0003-Session-Operational-Pipeline.md (NOVO)
├── ADR-0004-Session-Activity-Pipeline.md (NOVO)
├── ADR-0005-Modules-Facts-Commands-Adapters.md (NOVO)
├── ADR-0006-Route-Scene-Composition-Fade-Loading-Audio.md (NOVO)
├── ADR-0007-Gates-InputModes-e-Simulation-Executors.md (NOVO)
├── ADR-0008-SaveSystem-Canonico.md (NOVO)
├── ADR-0060-Base-1.1-Pipeline-Convergence-e-Identidade-Explicita.md (HISTÓRICO)
├── ADR-0061-Run-Pipeline-Canonico-e-Substituicao-do-Conceito-de-Macro.md (HISTÓRICO)
├── ADR-0062-Session-Pipeline-Canonico-e-Substituicao-do-Conceito-de-Local.md (HISTÓRICO)
├── ADR-0063-Modules-Produzem-Fatos-ou-Comandos-e-Adapters-Executam-Side-Effects.md (HISTÓRICO)
├── ADR-0064-IntroStage-como-Activation-Stage-e-Policy-de-Entrada.md (HISTÓRICO)
├── ADR-0065-Deactivation-e-Continuity-RunResult-RunDecision-PostRun.md (HISTÓRICO)
├── ADR-0066-Gates-InputModes-e-GameLoop-como-Executores-de-Estado-e-Efeitos.md (HISTÓRICO)
├── ADR-0067-Identidade-Explicita-de-Ciclo-e-Isolamento-Contra-Eventos-Foreign.md (HISTÓRICO)
├── ADR-0068-SceneRouteProfile-e-Rebaixamento-de-SceneFlow-Navigation-para-Pipeline-Adapter.md (HISTÓRICO)
├── ADR-0069-SessionTransitionEnvelope-e-SessionOperationalSetup-Base-1.1.md (HISTÓRICO)
├── ADR-0070-Base-1.1-Rail-Canonico-de-Rotas-Loading-Fade-e-Handoff-do-Base11Sandbox.md (HISTÓRICO)
├── Base-1.1-Consolidado-Atualizado-Base11Sandbox.md (SUPORTE)
├── Base-1.1-Plano-de-Migracao-Pipeline-Convergence.md (SUPORTE)
└── Base-1.1-Matriz-Inicial-de-Migracao.md (SUPORTE)
```

---

## Validação

### Cobertura Arquitetural

A nova estrutura cobre completamente a Base 1.1:

- **ADR-0001**: Princípios fundamentais + identidade explícita
- **ADR-0002**: Orquestração de run completa
- **ADR-0003**: Orquestração de sessão operacional + envelope
- **ADR-0004**: Orquestração de activity
- **ADR-0005**: Padrão Module/Command/Adapter
- **ADR-0006**: Rota, cenas, fade, loading, áudio
- **ADR-0007**: Gates, input, simulação
- **ADR-0008**: Sistema de save

### Ausências Previstas (Fora do Escopo)

- Detalhes avançados de ActorsSystem (fora do checkpoint)
- Preferences system (fora do checkpoint)
- Audio mixing/ducking policies (fora do checkpoint)

---

## Impacto em Documentação

- ✅ ADR-README referencia agora ADR-0001-0008 exclusivamente
- ✅ Developers podem citar ADR-0001-0008 em decisões futuras
- ✅ ADRs históricos (0060-0070) passam a ser histórico puro
- ✅ ADRs anteriores a 0060 já eram histórico

---

## Próximos Passos Recomendados

1. **Referência Cruzada**: Módulos podem atualizar seus docs para referenciar novos ADRs
2. **Checkpoint Futuro**: Se houver changes no runtime, criar novo ADR em sequência (ADR-0009+)
3. **Review de Seams Legados**: Documentar quais componentes ainda operam fora do rail canônico
4. **Archive de Histórico**: Considerar mover ADRs 0001-0059 e 0060-0070 para `Historico/` se não forem consultados frequentemente

---

## Conclusão

A reorganização de ADRs foi **concluída com sucesso**. A nova estrutura ADR-0001-0008:

- Serve como **única fonte normativa** para Base 1.1
- Consolida descobertas de múltiplos ADRs anteriores
- Define claramente **ownership, lifecycle e responsabilidades**
- Está pronto para evoluir em ADR-0009+ se houver mudanças futuras


