# Plan - Baseline 4.0 Phase 1

Status: Closed
Date: 2026-03-28
Reference: ADR-0044

## 1. Objective

Congelar a linguagem canonica do Baseline 4.0, alinhar documentacao, fixar ownership final por dominio e definir os invariantes de runtime antes de qualquer alteracao estrutural.

## 2. Phase Output

- vocabulario canonicamente fechado para gameplay/post-run/navigation/audio/UI
- ownership final mapeado por dominio
- invariantes de runtime documentados
- backlog do primeiro corte de implementacao preparado
- listas de reaproveitamento, substituicao e descarte revisadas sob a espinha do ADR-0044

## 3. Modules Touched

- `Docs/ADRs`
- `Docs/Plans`
- `Docs/Blueprints` via referencia documental

## 4. Documentation Changes

- consolidar o blueprint como referencia principal da arquitetura-alvo
- registrar o ADR canonico do Baseline 4.0
- tornar o plano de reorganizacao um apoio operacional subordinado
- manter o baseline conceitual como fonte unica de leitura

## 5. Preparatory Code Changes

Nenhuma alteracao de codigo e esperada nesta fase.

Se surgir necessidade tecnica, ela deve ser tratada apenas como pre-requisito documental para a Fase 2, sem mover ownership nem reestruturar dominios.

## 6. Risks

- reabrir discussoes conceituais que ja estao fechadas
- criar duplicacao de documentos com autoridade concorrente
- tentar antecipar refatoracoes de dominio antes do congelamento da linguagem

## 7. Acceptance Criteria

- ADR-0044 criado e apontando para o blueprint
- blueprint marcado como referencia canonica de arquitetura
- plano de reorganizacao marcado como auxiliar
- fase 1 definida sem mistura de implementacao

## 8. Evidence to Validate

- referencias consistentes entre ADR-0001, ADR-0043, ADR-0044 e o blueprint
- ausencia de conflito entre documentos sobre a espinha conceitual
- backlog inicial da Fase 1 enxuto e alinhado com a coluna dorsal

## 9. Non-goals

- nao implementar audio
- nao reorganizar SceneFlow
- nao fazer cleanup amplo de UI
- nao resolver toda a arquitetura por modulo nesta fase

## 10. Final Note

A Fase 1 existe para congelar o significado do Baseline 4.0 antes do primeiro corte de implementacao.

## 11. Outcome

- ADR-0044 criado e aceito como canonico.
- Blueprint marcado como referencia principal de arquitetura.
- Plano de reorganizacao marcado como apoio operacional secundario.
- Invariantes da fase ficaram documentados sem exigir alteracao de codigo.
