# Old Bira's Farm - Framework Integration

## Objetivo

Explicar como os sistemas do jogo serão organizados dentro da framework do estúdio.

---

# Referências

## Framework Repository

[https://github.com/ImmersiveGames/GameJam2025/tree/BiraFarm](https://github.com/ImmersiveGames/GameJam2025/tree/BiraFarm)

## Framework Root

Assets/_ImmersiveGames/NewScripts

# Legacy References

## Original Project Scripts

Location:

Docs/Old Bira Docs/LegacyReference/ScriptsLegacy.zip

Purpose:

Reference-only copy of the original implementation.

Rules:

- Do not compile.
- Do not directly migrate code.
- Use only as behavioral reference.
- New systems must follow the current framework architecture.

---

# Integration Policy

## Regra Principal

O framework e seu baseline são dependências somente leitura para o trabalho de Old Bira's Farm.

Nenhuma implementação do remake deve alterar arquivos, contratos, assets, configurações ou documentação pertencentes ao framework. A manutenção e a evolução do framework são responsabilidade exclusiva de seu proprietário.

## Escopo de Trabalho

Implementações, integrações, adapters, configurações, assets e documentação específicos do remake devem permanecer sob:

```text
Assets/_ImmersiveGames/Old Biras Farm
```

O remake deve consumir os contratos públicos e respeitar os owners, pipelines, stages, adapters, endpoints e lifecycles definidos pelo framework.

Não é permitido contornar uma limitação do framework por meio de:

- cópia ou alteração de código interno do framework;
- dependência de detalhes internos não públicos;
- service locators, fallbacks silenciosos ou bridges paralelas;
- duplicação de ownership ou lifecycle;
- patches no baseline para atender somente ao remake.

## Necessidades ou Bloqueios do Framework

Quando uma funcionalidade do remake exigir mudança ou extensão no framework, nenhuma alteração deve ser feita diretamente.

A necessidade deve ser documentada para discussão com o proprietário do framework, contendo:

1. necessidade funcional do remake;
2. módulo, contrato ou boundary envolvido;
3. comportamento atual e motivo do bloqueio;
4. extensão ou contrato esperado no framework;
5. impacto e critérios de aceite;
6. alternativa temporária, quando existir, desde que não viole a arquitetura.

A implementação dependente deve permanecer bloqueada até que a decisão seja tomada e, quando aplicável, a mudança seja entregue pelo proprietário do framework.

## Regra para Novos Assets e Sistemas

Assets e sistemas trazidos gradualmente do Old Bira original devem ser tratados como fonte de intenção funcional. Eles devem ser adaptados à arquitetura atual, e não transplantados automaticamente com owners, managers, eventos ou dependências legadas.

Antes de cada integração:

1. identificar a intenção funcional;
2. localizar o owner e o contrato público corretos no framework;
3. implementar a adaptação exclusivamente no escopo de Old Bira's Farm;
4. documentar qualquer incompatibilidade ou dependência ausente.

## Autoridade

Esta política é obrigatória para todo trabalho realizado no remake. Em caso de dúvida, preservar o framework sem alterações e registrar a necessidade para decisão externa.

---

# Framework Modules

## Foundation

Descrição:
Responsabilidades:
Observações:

## RunPipeline

Descrição:
Responsabilidades:
Observações:

## GameplayRuntime

Descrição:
Responsabilidades:
Observações:

## SaveRuntime

Descrição:
Responsabilidades:
Observações:

## PreferencesRuntime

Descrição:
Responsabilidades:
Observações:

## FrontendRuntime

Descrição:
Responsabilidades:
Observações:

## AudioRuntime

Descrição:
Responsabilidades:
Observações:

## InputModes

Descrição:
Responsabilidades:
Observações:

---

# Legacy Systems

## Movement.cs

Responsabilidade:
Destino na Framework:
Status:

## CollisionHandler.cs

Responsabilidade:
Destino na Framework:
Status:

## Fuel.cs

Responsabilidade:
Destino na Framework:
Status:

## LevelLoader.cs

Responsabilidade:
Destino na Framework:
Status:

## PlayerData.cs

Responsabilidade:
Destino na Framework:
Status:

---

# Mapping Table

| Legacy System | Framework Destination | Status | Notes |
| ------------- | --------------------- | ------ | ----- |

---

# Systems To Create

Lista de sistemas que ainda não existem ou que precisarão ser implementados.

---

# Open Questions

Lista de dúvidas ainda não respondidas.

---

# Approved Decisions

Registrar toda decisão arquitetural aprovada.

Formato:

## YYYY-MM-DD

### Decisão

### Justificativa

### Impacto

---

# Migration Notes

Observações importantes durante a migração do projeto antigo para a framework.

---

Regras:

* Nunca remover seções sem justificativa.
* Nunca apagar decisões aprovadas.
* Sempre registrar mudanças arquiteturais relevantes.
* Sempre manter o documento organizado e atualizado.
* Sempre priorizar clareza para humanos e agentes de IA.
* Tratar este documento como a fonte oficial da arquitetura de integração do projeto.
