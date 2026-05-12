# Mapa de Migração ADR-0060+ → ADR-0001-0008

Data de Reorganização: 2026-05-12

## Mapeamento Direto

| ADR Antigo | ADR Novo | Consolidação | Status |
|-----------|----------|--|--------|
| ADR-0060 | ADR-0001 | Consolidado com ADR-0067 | ✓ Criado |
| ADR-0061 | ADR-0002 | Consolidado com ADR-0065 (deactivation) | ✓ Criado |
| ADR-0062 | ADR-0003 | Consolidado com ADR-0069 (envelope) | ✓ Criado |
| ADR-0064 | ADR-0004 | Sem consolidação (IntroStage) | ✓ Criado |
| ADR-0063 | ADR-0005 | Sem consolidação | ✓ Criado |
| ADR-0068 | ADR-0006 | Consolidado com ADR-0070 | ✓ Criado |
| ADR-0066 | ADR-0007 | Sem consolidação | ✓ Criado |
| (Novo) | ADR-0008 | SaveSystem Canonical | ✓ Criado |

## Consolidações Específicas

### ADR-0001 (Pipeline Convergence + Explicit Identity)
- Base: ADR-0060
- Incorpora: ADR-0067 (Identidade Explícita de Ciclo e Isolamento contra Foreign Events)
- Razão: Ambos definem os princípios fundamentais de Base 1.1 e são interdependentes

### ADR-0002 (Run Pipeline + Deactivation/Continuity)
- Base: ADR-0061
- Incorpora: ADR-0065 (Deactivation e Continuity: RunResult, RunDecision, PostRun)
- Razão: Deactivation/Continuity é parte integral do Run Pipeline lifecycle

### ADR-0003 (Session Operational Pipeline + Envelope)
- Base: ADR-0062
- Incorpora: ADR-0069 (SessionTransitionEnvelope e SessionOperationalSetup)
- Razão: O envelope é o contrator temporal do Session Pipeline

### ADR-0006 (Route + Scene Composition + Adapters)
- Base: ADR-0068
- Incorpora: ADR-0070 (Rail Canonico de Rotas/Loading/Fade)
- Razão: Ambos definem o contrato de rota e seus adapters associados

## ADRs Históricos (Fora do Escopo Normativo)

Os seguintes ADRs anteriores a ADR-0060 são agora puramente históricos e não devem ser usados como fonte normativa:

- ADR-0001 até ADR-0059 (histórico anterior de Base 1.0 e exploração)

## Atualização de Referências

- **README.md**: Atualizado para apontar para ADR-0001 até ADR-0008 como única fonte normativa
- **AGENTS.md**: Referencia ADRs 0045-0050? Precisará atualização manual em próximo pass
- **Documentação de módulos**: Pode fazer referência cruzada para novos ADRs conforme necessário

## Verificação de Cobertura

### Áreas Cobertas

- [x] Pipeline Convergence (ADR-0001)
- [x] Run Pipeline & Deactivation (ADR-0002)
- [x] Session Operational & Envelope (ADR-0003)
- [x] Session Activity (ADR-0004)
- [x] Module/Command/Adapter Pattern (ADR-0005)
- [x] Route & Scene Composition (ADR-0006)
- [x] Gates, InputModes, Simulation (ADR-0007)
- [x] SaveSystem (ADR-0008)

### Áreas Sem ADR Específico (Futuro)

- Audio Module (referenciado em ADR-0006 como adapter, mas sem políticas explícitas)
- Preferences System
- ActorsSystem detailed policies
- Advanced Session Features

## Próximos Passos

1. **Manter ADRs históricos** em `Docs/ADRs/Historico/` para referência
2. **Verificar referências** em documentação de módulos
3. **Atualizar cruzadas** conforme necessário
4. **Não fazer breaking changes** na implementação - este é apenas um reorg de documentação/normativa


