# Changelog — Documentação WorldLifecycle

## [Unreleased] — Normalização e Consolidação

### Changed
- ADR de Ciclo de Vida reduzido para decisão arquitetural pura, removendo detalhes operacionais.
- WorldLifecycle.md consolidado como contrato operacional único do reset determinístico.
- Architecture.md ajustado para papel exclusivamente descritivo, sem regras normativas.
- Checklist de validação convertido para formato estritamente prescritivo (Pass/Fail).

### Removed
- Duplicação conceitual entre ADR e WorldLifecycle.
- Explicações arquiteturais dentro do checklist de QA.
- Linguagem normativa duplicada fora de DECISIONS.md.

### Clarified
- Separação explícita de responsabilidades entre:
    - ADR (por quê)
    - Architecture (como é)
    - WorldLifecycle (como funciona)
    - Checklist (como validar)
- DECISIONS.md reafirmado como fonte de verdade normativa.
- WorldLifecycle.md definido como referência operacional única para resets.

### No Functional Changes
- Nenhuma alteração em código ou comportamento de runtime.
- Mudanças restritas à documentação.
